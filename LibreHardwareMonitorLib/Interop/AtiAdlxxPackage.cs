// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace LibreHardwareMonitor.Interop;

/// <summary>
/// ADL (<c>atiadlxx.dll</c>) taken from the driver package of one specific adapter.
/// </summary>
/// <remarks>
/// Windows keeps a single <c>atiadlxx.dll</c> in System32, installed by the most recent AMD driver. When two different AMD driver
/// packages are installed (e.g. a Radeon VII driver forced on an Instinct MI50 next to a current Radeon), that newer DLL does not
/// know the older adapter and every per-adapter call fails with ADL_ERR_INVALID_ADL_IDX. The DLL shipped in the adapter's own
/// driver package does work, so it is loaded from the driver store and used for the power management log (PMLog) sensors only.
/// </remarks>
internal sealed class AtiAdlxxPackage : IDisposable
{
    private const uint LOAD_WITH_ALTERED_SEARCH_PATH = 0x8;

    private readonly IntPtr _context;
    private readonly Delegates _api;
    private uint _device;
    private bool _pmLogStarted;

    private AtiAdlxxPackage(IntPtr context, Delegates api, int adapterIndex, uint device, ushort[] sensors)
    {
        _context = context;
        _api = api;
        _device = device;
        AdapterIndex = adapterIndex;
        Sensors = sensors;
    }

    /// <summary>Adapter index inside this DLL (it differs from the index reported by the System32 DLL).</summary>
    public int AdapterIndex { get; }

    /// <summary>PMLog sensors supported by the adapter.</summary>
    public ushort[] Sensors { get; }

    /// <summary>
    /// Opens the ADL of the driver package installed for <paramref name="adapter" /> and checks that it supports PMLog for it.
    /// Returns <c>null</c> if that is not the case. Nothing is loaded when the package cannot be found.
    /// </summary>
    public static AtiAdlxxPackage TryOpen(AtiAdlxx.ADLAdapterInfo adapter, StringBuilder report)
    {
        IntPtr buffer = IntPtr.Zero;
        try
        {
            string dllPath = FindAdlInDriverPackage(adapter.PNPString);
            if (dllPath == null)
                return null;

            report.AppendLine("Driver package ADL: " + dllPath);

            // The module is intentionally never unloaded: ADL keeps worker threads running.
            IntPtr module = LoadLibraryEx(dllPath, IntPtr.Zero, LOAD_WITH_ALTERED_SEARCH_PATH);
            if (module == IntPtr.Zero)
                return null;

            Delegates api = Delegates.Load(module);
            if (api == null)
                return null;

            IntPtr context = IntPtr.Zero;
            if (api.Main_Control_Create(AtiAdlxx.Main_Memory_Alloc, 1, ref context) != AtiAdlxx.ADLStatus.ADL_OK)
                return null;

            int count = 0;
            if (api.NumberOfAdapters_Get(context, ref count) != AtiAdlxx.ADLStatus.ADL_OK || count <= 0)
            {
                api.Main_Control_Destroy(context);
                return null;
            }

            int elementSize = Marshal.SizeOf<AtiAdlxx.ADLAdapterInfo>();
            buffer = Marshal.AllocHGlobal(count * elementSize);
            if (api.AdapterInfo_Get(context, buffer, count * elementSize) != AtiAdlxx.ADLStatus.ADL_OK)
            {
                api.Main_Control_Destroy(context);
                return null;
            }

            // Find the same adapter in this DLL: by UDID first, then by PNP string.
            int adapterIndex = -1;
            for (int pass = 0; pass < 2 && adapterIndex < 0; pass++)
            {
                for (int i = 0; i < count; i++)
                {
                    var info = Marshal.PtrToStructure<AtiAdlxx.ADLAdapterInfo>(IntPtr.Add(buffer, i * elementSize));
                    string found = pass == 0 ? info.UDID : info.PNPString;
                    string wanted = pass == 0 ? adapter.UDID : adapter.PNPString;
                    if (!string.IsNullOrEmpty(found) && string.Equals(found, wanted, StringComparison.OrdinalIgnoreCase))
                    {
                        adapterIndex = info.AdapterIndex;
                        break;
                    }
                }
            }

            uint device = 0;
            var support = new AtiAdlxx.ADLPMLogSupportInfo();
            if (adapterIndex < 0 || api.PMLog_Device_Create(context, adapterIndex, ref device) != AtiAdlxx.ADLStatus.ADL_OK)
            {
                api.Main_Control_Destroy(context);
                return null;
            }

            AtiAdlxx.ADLStatus supportStatus = api.PMLog_Support_Get(context, adapterIndex, ref support);
            report.AppendLine("Driver package PMLog Support_Get: " + supportStatus);

            ushort[] sensors = supportStatus == AtiAdlxx.ADLStatus.ADL_OK && support.usSensors != null
                ? support.usSensors.TakeWhile(s => s != (ushort)AtiAdlxx.ADLPMLogSensors.ADL_SENSOR_MAXTYPES).ToArray()
                : new ushort[0];

            if (sensors.Length == 0)
            {
                api.PMLog_Device_Destroy(context, device);
                api.Main_Control_Destroy(context);
                return null;
            }

            report.AppendLine("Driver package Sensors Supported: " + sensors.Length.ToString(CultureInfo.InvariantCulture));
            return new AtiAdlxxPackage(context, api, adapterIndex, device, sensors);
        }
        catch (Exception e) when (e is ArgumentException or IOException or UnauthorizedAccessException or MarshalDirectiveException or BadImageFormatException or EntryPointNotFoundException or DllNotFoundException)
        {
            return null;
        }
        finally
        {
            if (buffer != IntPtr.Zero)
                Marshal.FreeHGlobal(buffer);
        }
    }

    /// <summary>Starts the PMLog; <paramref name="output" /> then points to the driver-owned log buffer.</summary>
    public bool TryStartPmLog(uint sampleRate, out AtiAdlxx.ADLPMLogSupportInfo supportInfo, out AtiAdlxx.ADLPMLogStartOutput output)
    {
        supportInfo = new AtiAdlxx.ADLPMLogSupportInfo { usSensors = new ushort[AtiAdlxx.ADL_PMLOG_MAX_SENSORS], iReserved = new int[16] };
        output = new AtiAdlxx.ADLPMLogStartOutput();

        var input = new AtiAdlxx.ADLPMLogStartInput { usSensors = new ushort[AtiAdlxx.ADL_PMLOG_MAX_SENSORS], iReserved = new int[15], ulSampleRate = sampleRate };
        Array.Copy(Sensors, supportInfo.usSensors, Sensors.Length);
        Array.Copy(Sensors, input.usSensors, Sensors.Length);

        _pmLogStarted = _api.PMLog_Start(_context, AdapterIndex, ref input, ref output, _device) == AtiAdlxx.ADLStatus.ADL_OK;
        return _pmLogStarted;
    }

    public void Dispose()
    {
        try
        {
            if (_pmLogStarted)
                _api.PMLog_Stop(_context, AdapterIndex, _device);

            if (_device != 0)
                _api.PMLog_Device_Destroy(_context, _device);

            _api.Main_Control_Destroy(_context);
        }
        catch (Exception)
        { }

        _pmLogStarted = false;
        _device = 0;
    }

    /// <summary>
    /// PNP string (PCI\VEN_xxxx&amp;DEV_xxxx...\instance) -> driver INF installed for the device -> driver store folder -> atiadlxx.dll.
    /// </summary>
    private static string FindAdlInDriverPackage(string pnpString)
    {
        if (string.IsNullOrEmpty(pnpString))
            return null;

        string infName;
        using (RegistryKey enumKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\" + pnpString))
        {
            string driver = enumKey?.GetValue("Driver") as string;
            if (string.IsNullOrEmpty(driver))
                return null;

            using RegistryKey classKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\" + driver);
            infName = classKey?.GetValue("InfPath") as string;
        }

        if (string.IsNullOrEmpty(infName))
            return null;

        var location = new StringBuilder(260);
        if (!SetupGetInfDriverStoreLocation(infName, IntPtr.Zero, null, location, location.Capacity, out _))
            return null;

        string packageDirectory = Path.GetDirectoryName(location.ToString());
        string repository = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "DriverStore", "FileRepository") + Path.DirectorySeparatorChar;

        // Only load libraries from the (protected) driver store.
        if (packageDirectory == null || !(packageDirectory + Path.DirectorySeparatorChar).StartsWith(repository, StringComparison.OrdinalIgnoreCase) || !Directory.Exists(packageDirectory))
            return null;

        string fileName = IntPtr.Size == 8 ? "atiadlxx.dll" : "atiadlxy.dll";
        return Directory.EnumerateFiles(packageDirectory, fileName, SearchOption.AllDirectories).FirstOrDefault();
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern IntPtr LoadLibraryEx(string fileName, IntPtr file, uint flags);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern IntPtr GetProcAddress(IntPtr module, string name);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "SetupGetInfDriverStoreLocationW")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern bool SetupGetInfDriverStoreLocation(string fileName, IntPtr alternatePlatformInfo, string localeName, StringBuilder returnBuffer, int returnBufferSize, out int requiredSize);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate AtiAdlxx.ADLStatus MainControlCreate(AtiAdlxx.ADL_Main_Memory_AllocDelegate callback, int connectedAdapters, ref IntPtr context);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate AtiAdlxx.ADLStatus MainControlDestroy(IntPtr context);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate AtiAdlxx.ADLStatus NumberOfAdaptersGet(IntPtr context, ref int numAdapters);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate AtiAdlxx.ADLStatus AdapterInfoGet(IntPtr context, IntPtr adapterInfo, int size);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate AtiAdlxx.ADLStatus PMLogDeviceCreate(IntPtr context, int adapterIndex, ref uint device);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate AtiAdlxx.ADLStatus PMLogDeviceDestroy(IntPtr context, uint device);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate AtiAdlxx.ADLStatus PMLogSupportGet(IntPtr context, int adapterIndex, ref AtiAdlxx.ADLPMLogSupportInfo info);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate AtiAdlxx.ADLStatus PMLogStart(IntPtr context, int adapterIndex, ref AtiAdlxx.ADLPMLogStartInput input, ref AtiAdlxx.ADLPMLogStartOutput output, uint device);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate AtiAdlxx.ADLStatus PMLogStop(IntPtr context, int adapterIndex, uint device);

    private sealed class Delegates
    {
        public MainControlCreate Main_Control_Create;
        public MainControlDestroy Main_Control_Destroy;
        public NumberOfAdaptersGet NumberOfAdapters_Get;
        public AdapterInfoGet AdapterInfo_Get;
        public PMLogDeviceCreate PMLog_Device_Create;
        public PMLogDeviceDestroy PMLog_Device_Destroy;
        public PMLogSupportGet PMLog_Support_Get;
        public PMLogStart PMLog_Start;
        public PMLogStop PMLog_Stop;

        public static Delegates Load(IntPtr module)
        {
            var d = new Delegates
            {
                Main_Control_Create = Get<MainControlCreate>(module, "ADL2_Main_Control_Create"),
                Main_Control_Destroy = Get<MainControlDestroy>(module, "ADL2_Main_Control_Destroy"),
                NumberOfAdapters_Get = Get<NumberOfAdaptersGet>(module, "ADL2_Adapter_NumberOfAdapters_Get"),
                AdapterInfo_Get = Get<AdapterInfoGet>(module, "ADL2_Adapter_AdapterInfo_Get"),
                PMLog_Device_Create = Get<PMLogDeviceCreate>(module, "ADL2_Device_PMLog_Device_Create"),
                PMLog_Device_Destroy = Get<PMLogDeviceDestroy>(module, "ADL2_Device_PMLog_Device_Destroy"),
                PMLog_Support_Get = Get<PMLogSupportGet>(module, "ADL2_Adapter_PMLog_Support_Get"),
                PMLog_Start = Get<PMLogStart>(module, "ADL2_Adapter_PMLog_Start"),
                PMLog_Stop = Get<PMLogStop>(module, "ADL2_Adapter_PMLog_Stop")
            };

            return d.Main_Control_Create != null && d.Main_Control_Destroy != null && d.NumberOfAdapters_Get != null && d.AdapterInfo_Get != null &&
                   d.PMLog_Device_Create != null && d.PMLog_Device_Destroy != null && d.PMLog_Support_Get != null && d.PMLog_Start != null && d.PMLog_Stop != null
                ? d
                : null;
        }

        private static T Get<T>(IntPtr module, string name) where T : class
        {
            IntPtr address = GetProcAddress(module, name);
            return address == IntPtr.Zero ? null : Marshal.GetDelegateForFunctionPointer(address, typeof(T)) as T;
        }
    }
}
