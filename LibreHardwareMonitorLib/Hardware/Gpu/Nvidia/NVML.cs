// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace LibreHardwareMonitor.Hardware.Gpu
{
    internal class NVML
    {
        private const string LinuxDllName = "nvidia-ml";

        private delegate NvmlReturn WindowsNvmlDelegate();
        private WindowsNvmlDelegate _windowsNvmlInit;
        private WindowsNvmlDelegate _windowsNvmlShutdown;

        private delegate NvmlReturn WindowsNvmlGetHandleDelegate(int index, out NvmlDevice device);
        private WindowsNvmlGetHandleDelegate _windowsNvmlDeviceGetHandleByIndex;

        private delegate NvmlReturn WindowsNvmlGetPowerUsageDelegate(NvmlDevice device, out int power);
        private WindowsNvmlGetPowerUsageDelegate _windowsNvmlDeviceGetPowerUsage;

        private readonly IntPtr _windowsDll;

        internal bool Initialised { get; }

        internal NVML()
        {
            if (Software.OperatingSystem.IsLinux)
            {
                try
                {
                    Initialised = (LinuxNvmlInit() == NvmlReturn.Success);
                }
                catch (DllNotFoundException)
                {
                    return;
                }
                catch (EntryPointNotFoundException)
                {
                    try
                    {
                        Initialised = (LinuxNvmlInitLegacy() == NvmlReturn.Success);
                    }
                    catch (EntryPointNotFoundException)
                    {
                        return;
                    }
                }
            }
            else if (IsNvmlCompatibleWindowsVersion())
            {
                // Attempt to load the Nvidia Management Library from the
                // windows standard search order for applications. This will
                // help installations that either have the library in
                // %windir%/system32 or provide their own library
                _windowsDll = LoadLibrary("nvml.dll");

                // If there is no dll in the path, then attempt to load it
                // from program files
                if (_windowsDll == IntPtr.Zero)
                {
                    var programFilesDirectory = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
                    var dllPath = Path.Combine(programFilesDirectory, @"NVIDIA Corporation\NVSMI\nvml.dll");
                    _windowsDll = LoadLibrary(dllPath);
                }

                if (_windowsDll == IntPtr.Zero)
                    return;

                Initialised = InitialiseDelegates() && (_windowsNvmlInit() == NvmlReturn.Success);
            }
        }

        private static bool IsNvmlCompatibleWindowsVersion()
        {
            return Software.OperatingSystem.Is64Bit && ((Environment.OSVersion.Version.Major > 6) || (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 1));
        }

        private bool InitialiseDelegates()
        {
            var nvmlInit = GetProcAddress(_windowsDll, "nvmlInit_v2");
            if (nvmlInit != IntPtr.Zero)
                _windowsNvmlInit = (WindowsNvmlDelegate)Marshal.GetDelegateForFunctionPointer(nvmlInit, typeof(WindowsNvmlDelegate));
            else
            {
                nvmlInit = GetProcAddress(_windowsDll, "nvmlInit");
                if (nvmlInit != IntPtr.Zero)
                    _windowsNvmlInit = (WindowsNvmlDelegate)Marshal.GetDelegateForFunctionPointer(nvmlInit, typeof(WindowsNvmlDelegate));
                else
                    return false;
            }

            var nvmlShutdown = GetProcAddress(_windowsDll, "nvmlShutdown");
            if (nvmlShutdown != IntPtr.Zero)
                _windowsNvmlShutdown = (WindowsNvmlDelegate)Marshal.GetDelegateForFunctionPointer(nvmlShutdown, typeof(WindowsNvmlDelegate));
            else
                return false;

            var nvmlGetHandle = GetProcAddress(_windowsDll, "nvmlDeviceGetHandleByIndex_v2");
            if (nvmlGetHandle != IntPtr.Zero)
                _windowsNvmlDeviceGetHandleByIndex = (WindowsNvmlGetHandleDelegate)Marshal.GetDelegateForFunctionPointer(nvmlGetHandle, typeof(WindowsNvmlGetHandleDelegate));
            else
            {
                nvmlGetHandle = GetProcAddress(_windowsDll, "nvmlDeviceGetHandleByIndex");
                if (nvmlGetHandle != IntPtr.Zero)
                    _windowsNvmlDeviceGetHandleByIndex = (WindowsNvmlGetHandleDelegate)Marshal.GetDelegateForFunctionPointer(nvmlGetHandle, typeof(WindowsNvmlGetHandleDelegate));
                else
                    return false;
            }

            var nvmlGetPowerUsage = GetProcAddress(_windowsDll, "nvmlDeviceGetPowerUsage");
            if (nvmlGetPowerUsage != IntPtr.Zero)
                _windowsNvmlDeviceGetPowerUsage = (WindowsNvmlGetPowerUsageDelegate)Marshal.GetDelegateForFunctionPointer(nvmlGetPowerUsage, typeof(WindowsNvmlGetPowerUsageDelegate));
            else
                return false;

            return true;
        }

        internal void Close()
        {
            if (Initialised)
            {
                if (Software.OperatingSystem.IsLinux)
                    LinuxNvmlShutdown();
                else if (_windowsDll != IntPtr.Zero)
                {
                    _windowsNvmlShutdown();
                    FreeLibrary(_windowsDll);
                }
            }
        }

        internal NvmlDevice? NvmlDeviceGetHandleByIndex(int index)
        {
            if (Initialised)
            {
                NvmlDevice nvmlDevice;
                if (Software.OperatingSystem.IsLinux)
                {
                    try
                    {
                        if (LinuxNvmlDeviceGetHandleByIndex(index, out nvmlDevice) == NvmlReturn.Success)
                            return nvmlDevice;
                    }
                    catch (EntryPointNotFoundException)
                    {
                        if (LinuxNvmlDeviceGetHandleByIndexLegacy(index, out nvmlDevice) == NvmlReturn.Success)
                            return nvmlDevice;
                    }
                }
                else if (_windowsNvmlDeviceGetHandleByIndex(index, out nvmlDevice) == NvmlReturn.Success)
                    return nvmlDevice;
            }

            return null;
        }

        internal int? NvmlDeviceGetPowerUsage(NvmlDevice nvmlDevice)
        {
            if (Initialised)
            {
                int powerUsage;
                if (Software.OperatingSystem.IsLinux)
                {
                    if (LinuxNvmlDeviceGetPowerUsage(nvmlDevice, out powerUsage) == NvmlReturn.Success)
                        return powerUsage;
                }
                else if (_windowsNvmlDeviceGetPowerUsage(nvmlDevice, out powerUsage) == NvmlReturn.Success)
                    return powerUsage;
            }

            return null;
        }

        [DllImport("kernel32")]
        private static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)]string dllPath);

        [DllImport("kernel32", ExactSpelling = true)]
        private static extern IntPtr GetProcAddress(IntPtr module, string methodName);

        [DllImport("kernel32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr module);

        [DllImport(LinuxDllName, EntryPoint = "nvmlInit_v2", ExactSpelling = true)]
        private static extern NvmlReturn LinuxNvmlInit();

        [DllImport(LinuxDllName, EntryPoint = "nvmlInit", ExactSpelling = true)]
        private static extern NvmlReturn LinuxNvmlInitLegacy();

        [DllImport(LinuxDllName, EntryPoint = "nvmlShutdown", ExactSpelling = true)]
        private static extern NvmlReturn LinuxNvmlShutdown();

        [DllImport(LinuxDllName, EntryPoint = "nvmlDeviceGetHandleByIndex_v2", ExactSpelling = true)]
        private static extern NvmlReturn LinuxNvmlDeviceGetHandleByIndex(int index, out NvmlDevice device);

        [DllImport(LinuxDllName, EntryPoint = "nvmlDeviceGetHandleByIndex", ExactSpelling = true)]
        private static extern NvmlReturn LinuxNvmlDeviceGetHandleByIndexLegacy(int index, out NvmlDevice device);

        [DllImport(LinuxDllName, EntryPoint = "nvmlDeviceGetPowerUsage", ExactSpelling = true)]
        private static extern NvmlReturn LinuxNvmlDeviceGetPowerUsage(NvmlDevice device, out int power);
    }
}
