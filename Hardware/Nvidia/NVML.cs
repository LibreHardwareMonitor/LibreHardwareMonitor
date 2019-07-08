using System;
using System.IO;
using System.Runtime.InteropServices;

namespace OpenHardwareMonitor.Hardware.Nvidia {
    internal class NVML {
        private const string LinuxDllName = "nvidia-ml";

        private delegate NvmlReturn WindowsNvmlDelegate();
        private WindowsNvmlDelegate WindowsNvmlInit;
        private WindowsNvmlDelegate WindowsNvmlShutdown;

        private delegate NvmlReturn WindowsNvmlGetHandleDelegate(int index, out NvmlDevice device);
        private WindowsNvmlGetHandleDelegate WindowsNvmlDeviceGetHandleByIndex;

        private delegate NvmlReturn WindowsNvmlGetPowerUsageDelegate(NvmlDevice device, out int power);
        private WindowsNvmlGetPowerUsageDelegate WindowsNvmlDeviceGetPowerUsage;

        private readonly IntPtr windowsDll;

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
                    catch (EntryPointNotFoundException) {
                        return;
                    }
                }
            }
            else if (IsNvmlCompatibleWindowsVersion()) {
                // Attempt to load the Nvidia Management Library from the
                // windows standard search order for applications. This will
                // help installations that either have the library in
                // %windir%/system32 or provide their own library
                windowsDll = LoadLibrary("nvml.dll");

                // If there is no dll in the path, then attempt to load it
                // from program files
                if (windowsDll == IntPtr.Zero)
                {
                    var programFilesDirectory = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
                    var dllPath = Path.Combine(programFilesDirectory, @"NVIDIA Corporation\NVSMI\nvml.dll");

                    windowsDll = LoadLibrary(dllPath);
                }

                if (windowsDll == IntPtr.Zero)
                    return;

                Initialised = InitialiseDelegates() && (WindowsNvmlInit() == NvmlReturn.Success);
            }
        }

        private static bool IsNvmlCompatibleWindowsVersion()
        {
            return Software.OperatingSystem.Is64Bit &&
                ((Environment.OSVersion.Version.Major > 6) || (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 1));
        }

        private bool InitialiseDelegates()
        {
            var nvmlInit = GetProcAddress(windowsDll, "nvmlInit_v2");
            if (nvmlInit != IntPtr.Zero)
                WindowsNvmlInit = (WindowsNvmlDelegate)Marshal.GetDelegateForFunctionPointer(nvmlInit, typeof(WindowsNvmlDelegate));
            else {
                nvmlInit = GetProcAddress(windowsDll, "nvmlInit");
                if (nvmlInit != IntPtr.Zero)
                    WindowsNvmlInit = (WindowsNvmlDelegate)Marshal.GetDelegateForFunctionPointer(nvmlInit, typeof(WindowsNvmlDelegate));
                else
                    return false;
            }

            var nvmlShutdown = GetProcAddress(windowsDll, "nvmlShutdown");
            if (nvmlShutdown != IntPtr.Zero)
                WindowsNvmlShutdown = (WindowsNvmlDelegate)Marshal.GetDelegateForFunctionPointer(nvmlShutdown, typeof(WindowsNvmlDelegate));
            else
                return false;

            var nvmlGetHandle = GetProcAddress(windowsDll, "nvmlDeviceGetHandleByIndex_v2");
            if (nvmlGetHandle != IntPtr.Zero)
                WindowsNvmlDeviceGetHandleByIndex = (WindowsNvmlGetHandleDelegate)Marshal.GetDelegateForFunctionPointer(nvmlGetHandle, typeof(WindowsNvmlGetHandleDelegate));
            else {
                nvmlGetHandle = GetProcAddress(windowsDll, "nvmlDeviceGetHandleByIndex");
                if (nvmlGetHandle != IntPtr.Zero)
                    WindowsNvmlDeviceGetHandleByIndex = (WindowsNvmlGetHandleDelegate)Marshal.GetDelegateForFunctionPointer(nvmlGetHandle, typeof(WindowsNvmlGetHandleDelegate));
                else 
                    return false;
            }

            var nvmlGetPowerUsage = GetProcAddress(windowsDll, "nvmlDeviceGetPowerUsage");
            if (nvmlGetPowerUsage != IntPtr.Zero)
                WindowsNvmlDeviceGetPowerUsage = (WindowsNvmlGetPowerUsageDelegate)Marshal.GetDelegateForFunctionPointer(nvmlGetPowerUsage, typeof(WindowsNvmlGetPowerUsageDelegate));
            else
                return false;

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
                NvmlDevice nvmlDevice;
                if (Software.OperatingSystem.IsLinux) {
                    try {
                        if (LinuxNvmlDeviceGetHandleByIndex(index, out nvmlDevice) == NvmlReturn.Success)
                            return nvmlDevice;
                    }
                    catch (EntryPointNotFoundException) {
                        if (LinuxNvmlDeviceGetHandleByIndexLegacy(index, out nvmlDevice) == NvmlReturn.Success)
                            return nvmlDevice;
                    }
                }
                else if (WindowsNvmlDeviceGetHandleByIndex(index, out nvmlDevice) == NvmlReturn.Success)
                    return nvmlDevice;
            }

            return null;
        }

        internal int? NvmlDeviceGetPowerUsage(NvmlDevice nvmlDevice) {
            if (Initialised) {
                int powerUsage;
                if (Software.OperatingSystem.IsLinux) {
                    if (LinuxNvmlDeviceGetPowerUsage(nvmlDevice, out powerUsage) == NvmlReturn.Success)
                        return powerUsage;
                }
                else if (WindowsNvmlDeviceGetPowerUsage(nvmlDevice, out powerUsage) == NvmlReturn.Success)
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
