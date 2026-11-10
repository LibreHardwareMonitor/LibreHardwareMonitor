// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.LibraryLoader;

namespace LibreHardwareMonitor.Interop;

// Managed binding to the public C API in PresentMon 2.6.0 (API 3.4).
// API declarations: Copyright (C) 2017-2024 Intel Corporation, MIT (Licenses/PresentMon.txt).
// Load the middleware belonging to the installed service, as required by the SDK.
// No PresentMon binaries, ETW consumer, or hardware telemetry are bundled here.
internal sealed class PresentMon : IDisposable
{
    private const int MaxSwapChains = 64;
    private const int FrameCapacity = 256;
    private FreeLibrarySafeHandle _module;
    private IntPtr _session;
    private IntPtr _query;
    private IntPtr _frameQuery;
    private uint _processId;
    private QueryElement[] _elements;
    private byte[] _blob;
    private byte[] _frames;
    private int _stride;
    private int _frameStride;
    private int _timestampOffset;
    private ulong _lastFrameTimestamp;
    private CloseHandleDelegate _closeSession;
    private CloseHandleDelegate _freeQuery;
    private CloseHandleDelegate _freeFrameQuery;
    private TrackProcessDelegate _startTracking;
    private TrackProcessDelegate _stopTracking;
    private PollDelegate _poll;
    private PollDelegate _consumeFrames;

    internal unsafe PresentMon(string path, string controlPipe = null)
    {
        try
        {
            if (!Path.IsPathRooted(path) || !File.Exists(path))
                throw new InvalidOperationException("The registered PresentMon middleware path is unavailable.");

            // Never search the current directory for the middleware or its dependencies.
            fixed (char* pathPointer = path)
            {
                HMODULE module = PInvoke.LoadLibraryEx(pathPointer, default,
                    LOAD_LIBRARY_FLAGS.LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | LOAD_LIBRARY_FLAGS.LOAD_LIBRARY_SEARCH_SYSTEM32);
                _module = new FreeLibrarySafeHandle((IntPtr)module, true);
            }

            if (_module.IsInvalid)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            Check(GetFunction<GetVersionDelegate>("pmGetApiVersion")(out ApiVersion version), "Get API version");
            if (version.Major != 3 || version.Minor < 4)
                throw new InvalidOperationException($"PresentMon API 3.4 or newer 3.x is required; found {version.Major}.{version.Minor}.");

            _closeSession = GetFunction<CloseHandleDelegate>("pmCloseSession");
            _freeQuery = GetFunction<CloseHandleDelegate>("pmFreeDynamicQuery");
            _freeFrameQuery = GetFunction<CloseHandleDelegate>("pmFreeFrameQuery");
            _startTracking = GetFunction<TrackProcessDelegate>("pmStartTrackingProcess");
            _stopTracking = GetFunction<TrackProcessDelegate>("pmStopTrackingProcess");
            _poll = GetFunction<PollDelegate>("pmPollDynamicQuery");
            _consumeFrames = GetFunction<PollDelegate>("pmConsumeFrames");
            int openStatus = controlPipe == null
                ? GetFunction<OpenSessionDelegate>("pmOpenSession")(out _session)
                : GetFunction<OpenSessionWithPipeDelegate>("pmOpenSessionWithPipe")(out _session, controlPipe);
            Check(openStatus, "Open session");
            Check(GetFunction<SetPeriodDelegate>("pmSetEtwFlushPeriod")(_session, 100), "Set ETW flush period");

            _elements = new[]
            {
                new QueryElement { Metric = Metric.SwapChainAddress, Stat = Statistic.NewestPoint }, // SWAP_CHAIN_ADDRESS, NEWEST_POINT (uint64)
                new QueryElement { Metric = Metric.PresentedFps, Stat = Statistic.Average }, // PRESENTED_FPS, AVG (double)
                new QueryElement { Metric = Metric.DisplayedFps, Stat = Statistic.Average } // DISPLAYED_FPS, AVG (double)
            };
            Check(GetFunction<RegisterQueryDelegate>("pmRegisterDynamicQuery")(
                _session, out _query, _elements, (ulong)_elements.Length, 1000, 200), "Register FPS query");
            ulong end = 0;
            foreach (QueryElement element in _elements)
            {
                if (element.DataSize != 8 || element.DataOffset > 4096)
                    throw new InvalidOperationException("Unexpected PresentMon query layout.");
                end = Math.Max(end, element.DataOffset + element.DataSize);
            }

            // Dynamic query records are padded to 16 bytes by the 3.4 middleware.
            _stride = checked((int)((end + 15) & ~15UL));
            _blob = new byte[_stride * MaxSwapChains];

            // Dynamic queries can retain the last value when rendering stops. Use frame
            // timestamps to reject stale results instead of treating cached FPS as live data.
            QueryElement[] frameElements = { new() { Metric = Metric.CpuStartQpc, Stat = Statistic.None } }; // CPU_START_QPC, NONE
            Check(GetFunction<RegisterFrameQueryDelegate>("pmRegisterFrameQuery")(
                _session, out _frameQuery, frameElements, 1, out uint frameSize), "Register freshness query");
            if (frameSize == 0 || frameSize > 4096 || frameElements[0].DataSize != 8 ||
                frameElements[0].DataOffset + 8 > frameSize)
                throw new InvalidOperationException("Unexpected PresentMon frame query layout.");

            _frameStride = (int)frameSize;
            _timestampOffset = (int)frameElements[0].DataOffset;
            _frames = new byte[_frameStride * FrameCapacity];
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    internal static string GetInstalledPath()
    {
        using RegistryKey machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using RegistryKey key = machine.OpenSubKey(@"SOFTWARE\INTEL\PresentMon\Service");
        return key?.GetValue("sharedMiddlewarePath") as string
            ?? throw new InvalidOperationException("PresentMon Service is not installed. Install PresentMon 2.6 or newer.");
    }

    internal void Track(uint processId)
    {
        if (_processId == processId)
            return;

        if (_processId != 0)
        {
            uint previous = _processId;
            _processId = 0;
            Check(_stopTracking(_session, previous), "Stop tracking");
        }

        _lastFrameTimestamp = 0;
        if (processId != 0)
        {
            Check(_startTracking(_session, processId), "Start tracking");
            _processId = processId;
        }
    }

    internal (float? Presented, float? Displayed, ulong SwapChain) Poll()
    {
        if (_processId == 0)
            return default;

        // Bound work even if a client polls slowly or an application produces many frames.
        for (int batch = 0; batch < 16; batch++)
        {
            uint count = FrameCapacity;
            Check(_consumeFrames(_frameQuery, _processId, _frames, ref count), "Read frame timestamps");
            if (count > FrameCapacity)
                throw new InvalidOperationException("Invalid PresentMon frame count.");

            for (int i = 0; i < count; i++)
                _lastFrameTimestamp = Math.Max(_lastFrameTimestamp, BitConverter.ToUInt64(_frames, i * _frameStride + _timestampOffset));

            if (count < FrameCapacity)
                break;
        }

        long now = Stopwatch.GetTimestamp();
        if (_lastFrameTimestamp == 0 || _lastFrameTimestamp > (ulong)now ||
            ((ulong)now - _lastFrameTimestamp) / (double)Stopwatch.Frequency > 2)
            return default;

        uint swapChains = MaxSwapChains;
        Array.Clear(_blob, 0, _blob.Length);
        Check(_poll(_query, _processId, _blob, ref swapChains), "Poll FPS");
        if (swapChains > MaxSwapChains)
            throw new InvalidOperationException("Invalid PresentMon swap chain count.");

        (float? Presented, float? Displayed, ulong SwapChain) result = default;
        for (int i = 0; i < swapChains; i++)
        {
            int offset = i * _stride;
            ulong address = BitConverter.ToUInt64(_blob, offset + (int)_elements[0].DataOffset);
            float? presented = ValidFps(BitConverter.ToDouble(_blob, offset + (int)_elements[1].DataOffset));
            float? displayed = ValidFps(BitConverter.ToDouble(_blob, offset + (int)_elements[2].DataOffset));
            // A process can own multiple swap chains. Select the busiest; never sum them.
            if (address != 0 && presented > 0 && (!result.Presented.HasValue || presented > result.Presented))
                result = (presented, displayed, address);
        }

        return result;
    }

    private static float? ValidFps(double value) =>
        double.IsNaN(value) || double.IsInfinity(value) || value < 0 || value > float.MaxValue ? null : (float)value;

    private T GetFunction<T>(string name) where T : Delegate
    {
        IntPtr address = PInvoke.GetProcAddress(_module, name);
        if (address == IntPtr.Zero)
            throw new EntryPointNotFoundException(name);

        return Marshal.GetDelegateForFunctionPointer<T>(address);
    }

    private static void Check(int status, string operation)
    {
        if (status != 0)
            throw new InvalidOperationException($"PresentMon: {operation} failed (PM_STATUS {status}).");
    }

    public void Dispose()
    {
        if (_session != IntPtr.Zero)
        {
            if (_processId != 0)
                _stopTracking(_session, _processId);
            if (_frameQuery != IntPtr.Zero)
                _freeFrameQuery(_frameQuery);
            if (_query != IntPtr.Zero)
                _freeQuery(_query);
            _closeSession(_session);
        }

        _processId = 0;
        _frameQuery = _query = _session = IntPtr.Zero;
        _module?.Dispose();
        _module = null;
    }

    private enum Metric
    {
        SwapChainAddress = 1,
        CpuStartQpc = 7,
        DisplayedFps = 11,
        PresentedFps = 12
    }

    private enum Statistic
    {
        None = 0,
        Average = 1,
        NewestPoint = 12
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct QueryElement
    {
        public Metric Metric;
        public Statistic Stat;
        public uint DeviceId;
        public uint ArrayIndex;
        public ulong DataOffset;
        public ulong DataSize;
    }

    [StructLayout(LayoutKind.Sequential, Size = 40)]
    private struct ApiVersion
    {
        public ushort Major;
        public ushort Minor;
        public ushort Patch;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int GetVersionDelegate(out ApiVersion version);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int OpenSessionDelegate(out IntPtr session);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int OpenSessionWithPipeDelegate(out IntPtr session, [MarshalAs(UnmanagedType.LPStr)] string pipe);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int CloseHandleDelegate(IntPtr handle);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int TrackProcessDelegate(IntPtr session, uint processId);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int SetPeriodDelegate(IntPtr session, uint periodMs);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int RegisterQueryDelegate(IntPtr session, out IntPtr query, [In, Out] QueryElement[] elements, ulong count, double window, double offset);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int RegisterFrameQueryDelegate(IntPtr session, out IntPtr query, [In, Out] QueryElement[] elements, ulong count, out uint size);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int PollDelegate(IntPtr query, uint processId, [In, Out] byte[] data, ref uint count);
}
