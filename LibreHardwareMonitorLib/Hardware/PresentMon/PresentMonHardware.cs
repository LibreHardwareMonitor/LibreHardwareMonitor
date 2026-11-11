// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using Windows.Win32;

namespace LibreHardwareMonitor.Hardware.PresentMon;

internal sealed class PresentMonHardware : Hardware
{
    private readonly object _lock = new();
    private readonly Func<uint> _getProcessId;
    private readonly Sensor _presentedFps;
    private readonly Sensor _displayedFps;
    private Interop.PresentMon _client;
    private uint _processId;
    private ulong _swapChain;
    private long _nextAttempt;
    private string _status = "Waiting for the first update.";
    private bool _closed;

    public PresentMonHardware(ISettings settings, Func<uint> getProcessId)
        : base("PresentMon", new Identifier("presentmon"), settings)
    {
        _getProcessId = getProcessId;
        _presentedFps = new Sensor("Presented FPS", 0, SensorType.Factor, this, settings);
        _displayedFps = new Sensor("Displayed FPS", 1, SensorType.Factor, this, settings);
        ActivateSensor(_presentedFps);
        ActivateSensor(_displayedFps);
    }

    public override HardwareType HardwareType => HardwareType.PresentMon;

    public override IDictionary<string, string> Properties
    {
        get
        {
            lock (_lock)
            {
                return new SortedDictionary<string, string>
                {
                    ["Process ID"] = _processId.ToString(CultureInfo.InvariantCulture),
                    ["Swap chain"] = "0x" + _swapChain.ToString("X", CultureInfo.InvariantCulture),
                    ["Status"] = _status,
                    ["Window"] = "1000 ms; 200 ms offset"
                };
            }
        }
    }

    public override void Update()
    {
        lock (_lock)
        {
            if (_closed)
                return;

            if (RuntimeInformation.ProcessArchitecture != Architecture.X64)
            {
                _status = "PresentMon integration requires a Windows x64 process.";
                return;
            }

            uint processId = _getProcessId();
            if (processId == 0)
                PInvoke.GetWindowThreadProcessId(PInvoke.GetForegroundWindow(), out processId);

            if (processId != _processId)
            {
                ReleaseClient();
                _processId = processId;
                ResetHistory();
            }

            if (processId == 0)
            {
                _status = "No foreground application.";
                SetUnavailable();
                return;
            }

            if (Stopwatch.GetTimestamp() < _nextAttempt)
                return;

            try
            {
                if (_client == null)
                {
                    _client = new Interop.PresentMon(Interop.PresentMon.GetInstalledPath());
                    _client.Track(processId);
                    _status = "Waiting for frame data.";
                    // Allow the one-second statistics window to fill after changing targets.
                    _nextAttempt = Stopwatch.GetTimestamp() + Stopwatch.Frequency;
                    return;
                }

                (float? presented, float? displayed, ulong swapChain) = _client.Poll();
                if (swapChain != 0 && _swapChain != 0 && swapChain != _swapChain)
                    ResetHistory();

                _swapChain = swapChain;
                _presentedFps.Value = presented;
                _displayedFps.Value = displayed;
                _status = presented.HasValue ? "Tracking." : "No recent frame data.";
            }
            catch (Exception exception) when (exception is InvalidOperationException or Win32Exception or
                                             IOException or UnauthorizedAccessException or SecurityException or
                                             DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
            {
                _status = exception.Message;
                ReleaseClient();
                SetUnavailable();
                // Retry after installation/service restart without continuously loading the DLL.
                _nextAttempt = Stopwatch.GetTimestamp() + 5 * Stopwatch.Frequency;
            }
        }
    }

    private void SetUnavailable()
    {
        _presentedFps.Value = null;
        _displayedFps.Value = null;
        _swapChain = 0;
    }

    private void ResetHistory()
    {
        SetUnavailable();
        foreach (Sensor sensor in new[] { _presentedFps, _displayedFps })
        {
            sensor.ResetMin();
            sensor.ResetMax();
            sensor.ClearValues();
        }
    }

    private void ReleaseClient()
    {
        _client?.Dispose();
        _client = null;
    }

    public override string GetReport()
    {
        lock (_lock)
        {
            return $"PresentMon\nProcess ID: {_processId}\nSwap chain: 0x{_swapChain:X}\n{_status}\n";
        }
    }

    public override void Close()
    {
        lock (_lock)
        {
            _closed = true;
            ReleaseClient();
            SetUnavailable();
        }

        base.Close();
    }
}
