// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace LibreHardwareMonitor.Interop;

internal static class NvidiaML
{
    private const string LinuxDllName = "nvidia-ml";
    private const string WindowsDllName = "nvml.dll";

    private static readonly object _syncRoot = new();

    private static IntPtr _windowsDll;

    private static WindowsNvmlGetHandleDelegate _windowsNvmlDeviceGetHandleByIndex;
    private static WindowsNvmlGetHandleByPciBusIdDelegate _windowsNvmlDeviceGetHandleByPciBusId;
    private static WindowsNvmlDeviceGetPcieThroughputDelegate _windowsNvmlDeviceGetPcieThroughputDelegate;
    private static WindowsNvmlDeviceGetPciInfo _windowsNvmlDeviceGetPciInfo;
    private static WindowsNvmlGetPowerUsageDelegate _windowsNvmlDeviceGetPowerUsage;
    private static WindowsNvmlDelegate _windowsNvmlInit;
    private static WindowsNvmlDelegate _windowsNvmlShutdown;

    public enum NvmlPcieUtilCounter
    {
        TxBytes = 0,
        RxBytes = 1
    }

    public enum NvmlReturn
    {
        /// <summary>
        /// The operation was successful
        /// </summary>
        Success = 0,

        /// <summary>
        /// NvidiaML was not first initialized with nvmlInit()
        /// </summary>
        Uninitialized = 1,

        /// <summary>
        /// A supplied argument is invalid
        /// </summary>
        InvalidArgument = 2,

        /// <summary>
        /// The requested operation is not available on target device
        /// </summary>
        NotSupported = 3,

        /// <summary>
        /// The current user does not have permission for operation
        /// </summary>
        NoPermission = 4,

        /// <summary>
        /// A query to find an object was unsuccessful
        /// </summary>
        NotFound = 6,

        /// <summary>
        /// An input argument is not large enough
        /// </summary>
        InsufficientSize = 7,

        /// <summary>
        /// A device's external power cables are not properly attached
        /// </summary>
        InsufficientPower = 8,

        /// <summary>
        /// NVIDIA driver is not loaded
        /// </summary>
        DriverNotLoaded = 9,

        /// <summary>
        /// User provided timeout passed
        /// </summary>
        TimeOut = 10,

        /// <summary>
        /// NVIDIA Kernel detected an interrupt issue with a GPU
        /// </summary>
        IRQIssue = 11,

        /// <summary>
        /// NvidiaML Shared Library couldn't be found or loaded
        /// </summary>
        LibraryNotFound = 12,

        /// <summary>
        /// Local version of NvidiaML doesn't implement this function
        /// </summary>
        FunctionNotFound = 13,

        /// <summary>
        /// infoROM is corrupted
        /// </summary>
        CorruptedInfoRom = 14,

        /// <summary>
        /// The GPU has fallen off the bus or has otherwise become inaccessible
        /// </summary>
        GpuIsLost = 15,

        /// <summary>
        /// The GPU requires a reset before it can be used again
        /// </summary>
        ResetRequired = 16,

        /// <summary>
        /// The GPU control device has been blocked by the operating system/cgroups
        /// </summary>
        OperatingSystem = 17,

        /// <summary>
        /// RM detects a driver/library version mismatch
        /// </summary>
        LibRmVersionMismatch = 18,

        /// <summary>
        /// An operation cannot be performed because the GPU is currently in use
        /// </summary>
        InUse = 19,

        /// <summary>
        /// An public driver error occurred
        /// </summary>
        Unknown = 999
    }

    public static bool IsAvailable { get; private set; }

    public static bool Initialize()
    {
        lock (_syncRoot)
        {
            if (IsAvailable)
            {
                return true;
            }

            if (Software.OperatingSystem.IsUnix)
            {
                try
                {
                    IsAvailable = nvmlInit() == NvmlReturn.Success;
                }
                catch (DllNotFoundException)
                { }
                catch (EntryPointNotFoundException)
                {
                    try
                    {
                        IsAvailable = nvmlInitLegacy() == NvmlReturn.Success;
                    }
                    catch (EntryPointNotFoundException)
                    { }
                }
            }
            else if (IsNvmlCompatibleWindowsVersion())
            {
                // Attempt to load the Nvidia Management Library from the
                // windows standard search order for applications. This will
                // help installations that either have the library in
                // %windir%/system32 or provide their own library
                _windowsDll = Kernel32.LoadLibrary(WindowsDllName);

                // If there is no dll in the path, then attempt to load it
                // from program files
                if (_windowsDll == IntPtr.Zero)
                {
                    string programFilesDirectory = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
                    string dllPath = Path.Combine(programFilesDirectory, @"NVIDIA Corporation\NVSMI", WindowsDllName);

                    _windowsDll = Kernel32.LoadLibrary(dllPath);
                }

                IsAvailable = (_windowsDll != IntPtr.Zero) && InitialiseDelegates() && (_windowsNvmlInit() == NvmlReturn.Success);
            }

            return IsAvailable;
        }
    }

    private static bool IsNvmlCompatibleWindowsVersion()
    {
        return Software.OperatingSystem.Is64Bit && ((Environment.OSVersion.Version.Major > 6) || (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 1));
    }

    private static bool InitialiseDelegates()
    {
        IntPtr nvmlInit = Kernel32.GetProcAddress(_windowsDll, "nvmlInit_v2");

        if (nvmlInit != IntPtr.Zero)
        {
            _windowsNvmlInit = (WindowsNvmlDelegate)Marshal.GetDelegateForFunctionPointer(nvmlInit, typeof(WindowsNvmlDelegate));
        }
        else
        {
            nvmlInit = Kernel32.GetProcAddress(_windowsDll, "nvmlInit");
            if (nvmlInit != IntPtr.Zero)
                _windowsNvmlInit = (WindowsNvmlDelegate)Marshal.GetDelegateForFunctionPointer(nvmlInit, typeof(WindowsNvmlDelegate));
            else
                return false;
        }

        IntPtr nvmlShutdown = Kernel32.GetProcAddress(_windowsDll, "nvmlShutdown");
        if (nvmlShutdown != IntPtr.Zero)
            _windowsNvmlShutdown = (WindowsNvmlDelegate)Marshal.GetDelegateForFunctionPointer(nvmlShutdown, typeof(WindowsNvmlDelegate));
        else
            return false;

        IntPtr nvmlGetHandle = Kernel32.GetProcAddress(_windowsDll, "nvmlDeviceGetHandleByIndex_v2");
        if (nvmlGetHandle != IntPtr.Zero)
            _windowsNvmlDeviceGetHandleByIndex = (WindowsNvmlGetHandleDelegate)Marshal.GetDelegateForFunctionPointer(nvmlGetHandle, typeof(WindowsNvmlGetHandleDelegate));
        else
        {
            nvmlGetHandle = Kernel32.GetProcAddress(_windowsDll, "nvmlDeviceGetHandleByIndex");
            if (nvmlGetHandle != IntPtr.Zero)
                _windowsNvmlDeviceGetHandleByIndex = (WindowsNvmlGetHandleDelegate)Marshal.GetDelegateForFunctionPointer(nvmlGetHandle, typeof(WindowsNvmlGetHandleDelegate));
            else
                return false;
        }

        IntPtr nvmlGetPowerUsage = Kernel32.GetProcAddress(_windowsDll, "nvmlDeviceGetPowerUsage");
        if (nvmlGetPowerUsage != IntPtr.Zero)
            _windowsNvmlDeviceGetPowerUsage = (WindowsNvmlGetPowerUsageDelegate)Marshal.GetDelegateForFunctionPointer(nvmlGetPowerUsage, typeof(WindowsNvmlGetPowerUsageDelegate));
        else
            return false;

        IntPtr nvmlGetPcieThroughput = Kernel32.GetProcAddress(_windowsDll, "nvmlDeviceGetPcieThroughput");
        if (nvmlGetPcieThroughput != IntPtr.Zero)
            _windowsNvmlDeviceGetPcieThroughputDelegate = (WindowsNvmlDeviceGetPcieThroughputDelegate)Marshal.GetDelegateForFunctionPointer(nvmlGetPcieThroughput, typeof(WindowsNvmlDeviceGetPcieThroughputDelegate));
        else
            return false;

        IntPtr nvmlGetHandlePciBus = Kernel32.GetProcAddress(_windowsDll, "nvmlDeviceGetHandleByPciBusId_v2");
        if (nvmlGetHandlePciBus != IntPtr.Zero)
            _windowsNvmlDeviceGetHandleByPciBusId = (WindowsNvmlGetHandleByPciBusIdDelegate)Marshal.GetDelegateForFunctionPointer(nvmlGetHandlePciBus, typeof(WindowsNvmlGetHandleByPciBusIdDelegate));
        else
            return false;

        IntPtr nvmlDeviceGetPciInfo = Kernel32.GetProcAddress(_windowsDll, "nvmlDeviceGetPciInfo_v2");
        if (nvmlDeviceGetPciInfo != IntPtr.Zero)
            _windowsNvmlDeviceGetPciInfo = (WindowsNvmlDeviceGetPciInfo)Marshal.GetDelegateForFunctionPointer(nvmlDeviceGetPciInfo, typeof(WindowsNvmlDeviceGetPciInfo));
        else
            return false;

        return true;
    }

    public static void Close()
    {
        lock (_syncRoot)
        {
            if (IsAvailable)
            {
                if (Software.OperatingSystem.IsUnix)
                {
                    nvmlShutdown();
                }
                else if (_windowsDll != IntPtr.Zero)
                {
                    _windowsNvmlShutdown();
                    Kernel32.FreeLibrary(_windowsDll);
                }

                IsAvailable = false;
            }
        }
    }

    public static NvmlDevice? NvmlDeviceGetHandleByIndex(int index)
    {
        if (IsAvailable)
        {
            NvmlDevice nvmlDevice;
            if (Software.OperatingSystem.IsUnix)
            {
                try
                {
                    if (nvmlDeviceGetHandleByIndex(index, out nvmlDevice) == NvmlReturn.Success)
                        return nvmlDevice;
                }
                catch (EntryPointNotFoundException)
                {
                    if (nvmlDeviceGetHandleByIndexLegacy(index, out nvmlDevice) == NvmlReturn.Success)
                        return nvmlDevice;
                }
            }
            else if (_windowsNvmlDeviceGetHandleByIndex(index, out nvmlDevice) == NvmlReturn.Success)
                return nvmlDevice;
        }

        return null;
    }

    public static NvmlDevice? NvmlDeviceGetHandleByPciBusId(string pciBusId)
    {
        if (IsAvailable)
        {
            NvmlDevice nvmlDevice;
            if (Software.OperatingSystem.IsUnix)
            {
                if (nvmlDeviceGetHandleByPciBusId(pciBusId, out nvmlDevice) == NvmlReturn.Success)
                    return nvmlDevice;
            }
            else if (_windowsNvmlDeviceGetHandleByPciBusId(pciBusId, out nvmlDevice) == NvmlReturn.Success)
                return nvmlDevice;
        }

        return null;
    }

    public static int? NvmlDeviceGetPowerUsage(NvmlDevice nvmlDevice)
    {
        if (IsAvailable)
        {
            int powerUsage;
            if (Software.OperatingSystem.IsUnix)
            {
                if (nvmlDeviceGetPowerUsage(nvmlDevice, out powerUsage) == NvmlReturn.Success)
                    return powerUsage;
            }
            else if (_windowsNvmlDeviceGetPowerUsage(nvmlDevice, out powerUsage) == NvmlReturn.Success)
                return powerUsage;
        }

        return null;
    }

    public static uint? NvmlDeviceGetPcieThroughput(NvmlDevice nvmlDevice, NvmlPcieUtilCounter counter)
    {
        if (IsAvailable)
        {
            uint pcieThroughput;
            if (Software.OperatingSystem.IsUnix)
            {
                if (nvmlDeviceGetPcieThroughput(nvmlDevice, counter, out pcieThroughput) == NvmlReturn.Success)
                    return pcieThroughput;
            }
            else if (_windowsNvmlDeviceGetPcieThroughputDelegate(nvmlDevice, counter, out pcieThroughput) == NvmlReturn.Success)
                return pcieThroughput;
        }

        return null;
    }

    public static NvmlPciInfo? NvmlDeviceGetPciInfo(NvmlDevice nvmlDevice)
    {
        if (IsAvailable)
        {
            var pci = new NvmlPciInfo();

            if (Software.OperatingSystem.IsUnix)
            {
                if (nvmlDeviceGetPciInfo(nvmlDevice, ref pci) == NvmlReturn.Success)
                    return pci;
            }
            else if (_windowsNvmlDeviceGetPciInfo(nvmlDevice, ref pci) == NvmlReturn.Success)
                return pci;
        }

        return null;
    }

    [DllImport(LinuxDllName, EntryPoint = "nvmlInit_v2", ExactSpelling = true)]
    private static extern NvmlReturn nvmlInit();

    [DllImport(LinuxDllName, EntryPoint = "nvmlInit", ExactSpelling = true)]
    private static extern NvmlReturn nvmlInitLegacy();

    [DllImport(LinuxDllName, EntryPoint = "nvmlShutdown", ExactSpelling = true)]
    private static extern NvmlReturn nvmlShutdown();

    [DllImport(LinuxDllName, EntryPoint = "nvmlDeviceGetHandleByIndex_v2", ExactSpelling = true)]
    private static extern NvmlReturn nvmlDeviceGetHandleByIndex(int index, out NvmlDevice device);

    [DllImport(LinuxDllName, EntryPoint = "nvmlDeviceGetHandleByPciBusId_v2", ExactSpelling = true)]
    private static extern NvmlReturn nvmlDeviceGetHandleByPciBusId([MarshalAs(UnmanagedType.LPStr)] string pciBusId, out NvmlDevice device);

    [DllImport(LinuxDllName, EntryPoint = "nvmlDeviceGetHandleByIndex", ExactSpelling = true)]
    private static extern NvmlReturn nvmlDeviceGetHandleByIndexLegacy(int index, out NvmlDevice device);

    [DllImport(LinuxDllName, EntryPoint = "nvmlDeviceGetPowerUsage", ExactSpelling = true)]
    private static extern NvmlReturn nvmlDeviceGetPowerUsage(NvmlDevice device, out int power);

    [DllImport(LinuxDllName, EntryPoint = "nvmlDeviceGetPcieThroughput", ExactSpelling = true)]
    private static extern NvmlReturn nvmlDeviceGetPcieThroughput(NvmlDevice device, NvmlPcieUtilCounter counter, out uint value);

    [DllImport(LinuxDllName, EntryPoint = "nvmlDeviceGetPciInfo_v2")]
    private static extern NvmlReturn nvmlDeviceGetPciInfo(NvmlDevice device, ref NvmlPciInfo pci);

    private delegate NvmlReturn WindowsNvmlDelegate();

    private delegate NvmlReturn WindowsNvmlGetHandleDelegate(int index, out NvmlDevice device);

    private delegate NvmlReturn WindowsNvmlGetHandleByPciBusIdDelegate([MarshalAs(UnmanagedType.LPStr)] string pciBusId, out NvmlDevice device);

    private delegate NvmlReturn WindowsNvmlGetPowerUsageDelegate(NvmlDevice device, out int power);

    private delegate NvmlReturn WindowsNvmlDeviceGetPcieThroughputDelegate(NvmlDevice device, NvmlPcieUtilCounter counter, out uint value);

    private delegate NvmlReturn WindowsNvmlDeviceGetPciInfo(NvmlDevice device, ref NvmlPciInfo pci);

    [StructLayout(LayoutKind.Sequential)]
    public struct NvmlDevice
    {
        public IntPtr Handle;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NvmlPciInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string busId;

        public uint domain;

        public uint bus;

        public uint device;

        public ushort pciVendorId;

        public ushort pciDeviceId;

        public uint pciSubSystemId;

        public uint reserved0;
        public uint reserved1;
        public uint reserved2;
        public uint reserved3;
    }
}