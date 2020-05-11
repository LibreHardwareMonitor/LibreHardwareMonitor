// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace LibreHardwareMonitor.Interop
{
    internal static class NvidiaML
    {
        private const string LinuxDllName = "nvidia-ml";
        private const string WindowsDllName = "nvml.dll";

        private static readonly object SyncRoot = new object();

        private static WindowsNvmlGetHandleDelegate _windowsNvmlDeviceGetHandleByIndex;
        private static WindowsNvmlGetPowerUsageDelegate _windowsNvmlDeviceGetPowerUsage;
        private static WindowsNvmlDeviceGetPcieThroughputDelegate _windowsNvmlDeviceGetPcieThroughputDelegate;
        private static WindowsNvmlGetHandleByPciBusIdDelegate _windowsNvmlDeviceGetHandleByPciBusId;
        private static WindowsNvmlDelegate _windowsNvmlInit;
        private static WindowsNvmlDelegate _windowsNvmlShutdown;

        private static IntPtr WindowsDll;

        internal static bool IsAvailable { get; private set; }

        internal static bool Initialize()
        {
            lock (SyncRoot)
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
                    WindowsDll = Kernel32.LoadLibrary(WindowsDllName);

                    // If there is no dll in the path, then attempt to load it
                    // from program files
                    if (WindowsDll == IntPtr.Zero)
                    {
                        string programFilesDirectory = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
                        string dllPath = Path.Combine(programFilesDirectory, @"NVIDIA Corporation\NVSMI", WindowsDllName);

                        WindowsDll = Kernel32.LoadLibrary(dllPath);
                    }

                    IsAvailable = (WindowsDll != IntPtr.Zero) 
                        && InitialiseDelegates() 
                        && (_windowsNvmlInit() == NvmlReturn.Success);
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
            IntPtr nvmlInit = Kernel32.GetProcAddress(WindowsDll, "nvmlInit_v2");

            if (nvmlInit != IntPtr.Zero)
            {
                _windowsNvmlInit = (WindowsNvmlDelegate)Marshal.GetDelegateForFunctionPointer(nvmlInit, typeof(WindowsNvmlDelegate));
            }
            else
            {
                nvmlInit = Kernel32.GetProcAddress(WindowsDll, "nvmlInit");
                if (nvmlInit != IntPtr.Zero)
                    _windowsNvmlInit = (WindowsNvmlDelegate)Marshal.GetDelegateForFunctionPointer(nvmlInit, typeof(WindowsNvmlDelegate));
                else
                    return false;
            }

            IntPtr nvmlShutdown = Kernel32.GetProcAddress(WindowsDll, "nvmlShutdown");
            if (nvmlShutdown != IntPtr.Zero)
                _windowsNvmlShutdown = (WindowsNvmlDelegate)Marshal.GetDelegateForFunctionPointer(nvmlShutdown, typeof(WindowsNvmlDelegate));
            else
                return false;


            IntPtr nvmlGetHandle = Kernel32.GetProcAddress(WindowsDll, "nvmlDeviceGetHandleByIndex_v2");
            if (nvmlGetHandle != IntPtr.Zero)
            {
                _windowsNvmlDeviceGetHandleByIndex = (WindowsNvmlGetHandleDelegate)Marshal.GetDelegateForFunctionPointer(nvmlGetHandle, typeof(WindowsNvmlGetHandleDelegate));
            }
            else
            {
                nvmlGetHandle = Kernel32.GetProcAddress(WindowsDll, "nvmlDeviceGetHandleByIndex");
                if (nvmlGetHandle != IntPtr.Zero)
                    _windowsNvmlDeviceGetHandleByIndex = (WindowsNvmlGetHandleDelegate)Marshal.GetDelegateForFunctionPointer(nvmlGetHandle, typeof(WindowsNvmlGetHandleDelegate));
                else
                    return false;
            }

            IntPtr nvmlGetPowerUsage = Kernel32.GetProcAddress(WindowsDll, "nvmlDeviceGetPowerUsage");
            if (nvmlGetPowerUsage != IntPtr.Zero)
                _windowsNvmlDeviceGetPowerUsage = (WindowsNvmlGetPowerUsageDelegate)Marshal.GetDelegateForFunctionPointer(nvmlGetPowerUsage, typeof(WindowsNvmlGetPowerUsageDelegate));
            else
                return false;

            IntPtr nvmlGetPcieThroughput = Kernel32.GetProcAddress(WindowsDll, "nvmlDeviceGetPcieThroughput");
            if (nvmlGetPcieThroughput != IntPtr.Zero)
                _windowsNvmlDeviceGetPcieThroughputDelegate = (WindowsNvmlDeviceGetPcieThroughputDelegate)Marshal.GetDelegateForFunctionPointer(nvmlGetPcieThroughput, typeof(WindowsNvmlDeviceGetPcieThroughputDelegate));
            else
                return false;

            IntPtr nvmlGetHandlePciBus = Kernel32.GetProcAddress(WindowsDll, "nvmlDeviceGetHandleByPciBusId_v2");
            if (nvmlGetHandlePciBus != IntPtr.Zero)
                _windowsNvmlDeviceGetHandleByPciBusId = (WindowsNvmlGetHandleByPciBusIdDelegate)Marshal.GetDelegateForFunctionPointer(nvmlGetHandlePciBus, typeof(WindowsNvmlGetHandleByPciBusIdDelegate));
            else
                return false;

            return true;
        }

        internal static void Close()
        {
            lock (SyncRoot)
            {
                if (IsAvailable)
                {
                    if (Software.OperatingSystem.IsUnix)
                    {
                        nvmlShutdown();
                    }
                    else if (WindowsDll != IntPtr.Zero)
                    {
                        _windowsNvmlShutdown();
                        Kernel32.FreeLibrary(WindowsDll);
                    }

                    IsAvailable = false;
                }
            }
        }

        internal static NvmlDevice? NvmlDeviceGetHandleByIndex(int index)
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
                {
                    return nvmlDevice;
                }
            }

            return null;
        }


        internal static NvmlDevice? NvmlDeviceGetHandleByPciBusId(string pciBusId)
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
                {
                    return nvmlDevice;
                }
            }

            return null;
        }

        internal static int? NvmlDeviceGetPowerUsage(NvmlDevice nvmlDevice)
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
                {
                    return powerUsage;
                }
            }

            return null;
        }

        internal static uint? NvmlDeviceGetPcieThroughput(NvmlDevice nvmlDevice, NvmlPcieUtilCounter counter)
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
                {
                    return pcieThroughput;
                }
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

        private delegate NvmlReturn WindowsNvmlDelegate();

        private delegate NvmlReturn WindowsNvmlGetHandleDelegate(int index, out NvmlDevice device);

        private delegate NvmlReturn WindowsNvmlGetHandleByPciBusIdDelegate([MarshalAs(UnmanagedType.LPStr)] string pciBusId, out NvmlDevice device);

        private delegate NvmlReturn WindowsNvmlGetPowerUsageDelegate(NvmlDevice device, out int power);

        private delegate NvmlReturn WindowsNvmlDeviceGetPcieThroughputDelegate(NvmlDevice device, NvmlPcieUtilCounter counter, out uint value);

        internal enum NvmlPcieUtilCounter
        {
            TxBytes = 0,
            RxBytes = 1
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NvmlDevice
        {
            public IntPtr Handle;
        }

        internal enum NvmlReturn
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
            /// An internal driver error occurred
            /// </summary>
            Unknown = 999
        }
    }
}
