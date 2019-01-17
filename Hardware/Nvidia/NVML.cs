using System;
using System.IO;
using System.Runtime.InteropServices;

namespace OpenHardwareMonitor.Hardware.Nvidia {
    internal class NVML {
        private const string LinuxDllName = "nvidia-ml";

        private delegate NvmlReturn WindowsNvmlDelegate();

        private WindowsNvmlDelegate WindowsNvmlInit;
        private WindowsNvmlDelegate WindowsNvmlInitLegacy;
        private WindowsNvmlDelegate WindowsNvmlShutdown;

        private delegate NvmlReturn WindowsNvmlGetHandleDelegate(int index, ref NvmlDevice device);

        private WindowsNvmlGetHandleDelegate WindowsNvmlDeviceGetHandleByIndex;
        private WindowsNvmlGetHandleDelegate WindowsNvmlDeviceGetHandleByIndexLegacy;

        private delegate NvmlReturn WindowsNvmlGetPowerUsageDelegate(NvmlDevice device, ref int power);

        private WindowsNvmlGetPowerUsageDelegate WindowsNvmlDeviceGetPowerUsage;

        private IntPtr windowsDll;

        internal bool Initialised { get; }

        internal NVML() {
            if (Software.OperatingSystem.IsLinux) {
                try {
                    Initialised = (LinuxNvmlInit() == NvmlReturn.Success);
                }
                catch (DllNotFoundException) {
                    return;
                }
                catch (EntryPointNotFoundException) {
                    try {
                        Initialised = (LinuxNvmlInitLegacy() == NvmlReturn.Success);
                    }
                    catch (EntryPointNotFoundException) { return; }
                }
            }
            else if (IsNvmlCompatibleWindowsVersion()) {
                var programFilesDirectory = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
                var dllPath = Path.Combine(programFilesDirectory, @"NVIDIA Corporation\NVSMI\nvml.dll");

                windowsDll = LoadLibrary(dllPath);

                if (windowsDll == IntPtr.Zero) {
                    return;
                }

                var delegatesInitialised = InitialiseDelegates();

                try {
                    Initialised = delegatesInitialised && (WindowsNvmlInit() == NvmlReturn.Success);
                }
                catch (DllNotFoundException) {
                    return;
                }
                catch (EntryPointNotFoundException) {
                    try {
                        Initialised = delegatesInitialised && (WindowsNvmlInitLegacy() == NvmlReturn.Success);
                    }
                    catch (EntryPointNotFoundException) { return; }
                }
            }
        }

        private static bool IsNvmlCompatibleWindowsVersion()
        {
            return Software.OperatingSystem.Is64Bit &&
                ((Environment.OSVersion.Version.Major > 6) || (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 1));
        }

        private bool InitialiseDelegates()
        {
            var nvmlInitv2 = GetProcAddress(windowsDll, "nvmlInit_v2");
            if (nvmlInitv2 == IntPtr.Zero)
                return false;

            WindowsNvmlInit = (WindowsNvmlDelegate)Marshal.GetDelegateForFunctionPointer(nvmlInitv2, typeof(WindowsNvmlDelegate));

            var nvmlInit = GetProcAddress(windowsDll, "nvmlInit");
            if (nvmlInit == IntPtr.Zero)
                return false;

            WindowsNvmlInitLegacy = (WindowsNvmlDelegate)Marshal.GetDelegateForFunctionPointer(nvmlInit, typeof(WindowsNvmlDelegate));

            var nvmlShutdown = GetProcAddress(windowsDll, "nvmlShutdown");
            if (nvmlShutdown == IntPtr.Zero)
                return false;

            WindowsNvmlShutdown = (WindowsNvmlDelegate)Marshal.GetDelegateForFunctionPointer(nvmlShutdown, typeof(WindowsNvmlDelegate));

            var nvmlGetHandlev2 = GetProcAddress(windowsDll, "nvmlDeviceGetHandleByIndex_v2");
            if (nvmlGetHandlev2 == IntPtr.Zero)
                return false;

            WindowsNvmlDeviceGetHandleByIndex = (WindowsNvmlGetHandleDelegate)Marshal.GetDelegateForFunctionPointer(nvmlGetHandlev2, typeof(WindowsNvmlGetHandleDelegate));

            var nvmlGetHandle = GetProcAddress(windowsDll, "nvmlDeviceGetHandleByIndex");
            if (nvmlGetHandle == IntPtr.Zero)
                return false;

            WindowsNvmlDeviceGetHandleByIndexLegacy = (WindowsNvmlGetHandleDelegate)Marshal.GetDelegateForFunctionPointer(nvmlGetHandle, typeof(WindowsNvmlGetHandleDelegate));

            var nvmlGetPowerUsage = GetProcAddress(windowsDll, "nvmlDeviceGetPowerUsage");
            if (nvmlGetPowerUsage == IntPtr.Zero)
                return false;

            WindowsNvmlDeviceGetPowerUsage = (WindowsNvmlGetPowerUsageDelegate)Marshal.GetDelegateForFunctionPointer(nvmlGetPowerUsage, typeof(WindowsNvmlGetPowerUsageDelegate));

            return true;
        }

        internal void Close() {
            if (Initialised) {
                if (Software.OperatingSystem.IsLinux)
                    LinuxNvmlShutdown();
                else if (windowsDll != IntPtr.Zero) {
                    WindowsNvmlShutdown();
                    FreeLibrary(windowsDll);
                }
            }  
        }

        internal NvmlDevice? NvmlDeviceGetHandleByIndex(int index) {
            if (Initialised) {
                var nvmlDevice = new NvmlDevice();
                if (Software.OperatingSystem.IsLinux) {
                    try {
                        if (LinuxNvmlDeviceGetHandleByIndex(index, ref nvmlDevice) == NvmlReturn.Success)
                            return nvmlDevice;
                    }
                    catch (EntryPointNotFoundException) {
                        if (LinuxNvmlDeviceGetHandleByIndexLegacy(index, ref nvmlDevice) == NvmlReturn.Success)
                            return nvmlDevice;
                    }
                }
                else {
                    try {
                        if (WindowsNvmlDeviceGetHandleByIndex(index, ref nvmlDevice) == NvmlReturn.Success)
                            return nvmlDevice;
                    }
                    catch (EntryPointNotFoundException) {
                        if (WindowsNvmlDeviceGetHandleByIndexLegacy(index, ref nvmlDevice) == NvmlReturn.Success)
                            return nvmlDevice;
                    }
                }
            }

            return null;
        }

        internal int? NvmlDeviceGetPowerUsage(NvmlDevice nvmlDevice) {
            if (Initialised) {
                var powerUsage = 0;
                if (Software.OperatingSystem.IsLinux) {
                    if (LinuxNvmlDeviceGetPowerUsage(nvmlDevice, ref powerUsage) == NvmlReturn.Success)
                        return powerUsage;
                }
                else if (WindowsNvmlDeviceGetPowerUsage(nvmlDevice, ref powerUsage) == NvmlReturn.Success)
                    return powerUsage;
            }

            return null;
        }

        [DllImport("kernel32")]
        private static extern IntPtr LoadLibrary(string dllPath);

        [DllImport("kernel32")]
        private static extern IntPtr GetProcAddress(IntPtr module, string methodName);

        [DllImport("kernel32")]
        private static extern bool FreeLibrary(IntPtr module);

        [DllImport(LinuxDllName, EntryPoint = "nvmlInit_v2")]
        private static extern NvmlReturn LinuxNvmlInit();

        [DllImport(LinuxDllName, EntryPoint = "nvmlInit")]
        private static extern NvmlReturn LinuxNvmlInitLegacy();

        [DllImport(LinuxDllName, EntryPoint = "nvmlShutdown")]
        private static extern NvmlReturn LinuxNvmlShutdown();

        [DllImport(LinuxDllName, EntryPoint = "nvmlDeviceGetHandleByIndex_v2")]
        private static extern NvmlReturn LinuxNvmlDeviceGetHandleByIndex(int index, ref NvmlDevice device);

        [DllImport(LinuxDllName, EntryPoint = "nvmlDeviceGetHandleByIndex")]
        private static extern NvmlReturn LinuxNvmlDeviceGetHandleByIndexLegacy(int index, ref NvmlDevice device);

        [DllImport(LinuxDllName, EntryPoint = "nvmlDeviceGetPowerUsage")]
        private static extern NvmlReturn LinuxNvmlDeviceGetPowerUsage(NvmlDevice device, ref int power);
    }
}
