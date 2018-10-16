using System;
using System.IO;
using System.Runtime.InteropServices;

namespace OpenHardwareMonitor.Hardware.Nvidia {
    internal enum NvmlReturn
    {
        /// <summary>
        /// The operation was successful
        /// </summary>
        Success = 0,
        /// <summary>
        /// NVML was not first initialized with nvmlInit()
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
        /// NVML Shared Library couldn't be found or loaded
        /// </summary>
        LibraryNotFound = 12,
        /// <summary>
        /// Local version of NVML doesn't implement this function
        /// </summary>
        FunctionNotFound = 13,
        /// <summary>
        /// infoROM is corrupted
        /// </summary>
        CorruptedInfoROM = 14,
        /// <summary>
        /// The GPU has fallen off the bus or has otherwise become inaccessible
        /// </summary>
        GPUIsLost = 15,
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
        LibRMVersionMismatch = 18,
        /// <summary>
        /// An operation cannot be performed because the GPU is currently in use
        /// </summary>
        InUse = 19,
        /// <summary>
        /// An internal driver error occurred
        /// </summary>
        Unknown = 999
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NvmlDevice
    {
        public IntPtr Handle;
    }

    internal class NVML {
        internal bool Initialised { get; }

        private const string WindowsDllName = "nvml";
        private const string LinuxDllName = "nvidia-ml";

        internal NVML() {
            if (Software.OperatingSystem.IsLinux) {
                try {
                    Initialised = (LinuxNvmlInit() == NvmlReturn.Success);
                }
                catch (DllNotFoundException) { return; }
                catch (EntryPointNotFoundException) {
                    try {
                        Initialised = (LinuxNvmlInitLegacy() == NvmlReturn.Success);
                    }
                    catch (EntryPointNotFoundException) { return; }
                }
            }
            else if (IsNvmlCompatibleWindowsVersion()) {
                var programFilesDirectory = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
                var dllPath = Path.Combine(programFilesDirectory, @"NVIDIA Corporation\NVSMI\");
                SetDllDirectory(dllPath);

                try {
                    Initialised = (WindowsNvmlInit() == NvmlReturn.Success);
                }
                catch (DllNotFoundException) { return; }
                catch (EntryPointNotFoundException) {
                    try {
                        Initialised = (WindowsNvmlInitLegacy() == NvmlReturn.Success);
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

        internal void Close() {
            if (Initialised) {
                if (Software.OperatingSystem.IsLinux)
                    LinuxNvmlShutdown();
                else
                    WindowsNvmlShutdown();
            }  
        }

        internal NvmlDevice? NvmlDeviceGetHandleByIndex(int index) {
            if (Initialised) {
                var nvmlDevice = new NvmlDevice();
                if (Software.OperatingSystem.IsLinux) {
                    try {
                        if (LinuxNvmlDeviceGetHandleByIndex(Convert.ToUInt32(index), ref nvmlDevice) == NvmlReturn.Success)
                            return nvmlDevice;
                    }
                    catch (EntryPointNotFoundException) {
                        if (LinuxNvmlDeviceGetHandleByIndexLegacy(Convert.ToUInt32(index), ref nvmlDevice) == NvmlReturn.Success)
                            return nvmlDevice;
                    }
                }
                else {
                    try {
                        if (WindowsNvmlDeviceGetHandleByIndex(Convert.ToUInt32(index), ref nvmlDevice) == NvmlReturn.Success)
                            return nvmlDevice;
                    }
                    catch (EntryPointNotFoundException) {
                        if (WindowsNvmlDeviceGetHandleByIndexLegacy(Convert.ToUInt32(index), ref nvmlDevice) == NvmlReturn.Success)
                            return nvmlDevice;
                    }
                }
            }

            return null;
        }

        internal uint? NvmlDeviceGetPowerUsage(NvmlDevice nvmlDevice) {
            if (Initialised) {
                var powerUsage = 0u;
                if (Software.OperatingSystem.IsLinux) {
                    if (LinuxNvmlDeviceGetPowerUsage(nvmlDevice, ref powerUsage) == NvmlReturn.Success)
                        return powerUsage;
                }
                else if (WindowsNvmlDeviceGetPowerUsage(nvmlDevice, ref powerUsage) == NvmlReturn.Success)
                    return powerUsage;
            }

            return null;
        }

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        [DllImport(WindowsDllName, EntryPoint = "nvmlInit_v2")]
        private static extern NvmlReturn WindowsNvmlInit();

        [DllImport(WindowsDllName, EntryPoint = "nvmlInit")]
        private static extern NvmlReturn WindowsNvmlInitLegacy();

        [DllImport(WindowsDllName, EntryPoint = "nvmlShutdown")]
        private static extern NvmlReturn WindowsNvmlShutdown();

        [DllImport(WindowsDllName, EntryPoint = "nvmlDeviceGetHandleByIndex_v2")]
        private static extern NvmlReturn WindowsNvmlDeviceGetHandleByIndex(uint index, ref NvmlDevice device);

        [DllImport(WindowsDllName, EntryPoint = "nvmlDeviceGetHandleByIndex")]
        private static extern NvmlReturn WindowsNvmlDeviceGetHandleByIndexLegacy(uint index, ref NvmlDevice device);

        [DllImport(WindowsDllName, EntryPoint = "nvmlDeviceGetPowerUsage")]
        private static extern NvmlReturn WindowsNvmlDeviceGetPowerUsage(NvmlDevice device, ref uint power);

        [DllImport(LinuxDllName, EntryPoint = "nvmlInit_v2")]
        private static extern NvmlReturn LinuxNvmlInit();

        [DllImport(LinuxDllName, EntryPoint = "nvmlInit")]
        private static extern NvmlReturn LinuxNvmlInitLegacy();

        [DllImport(LinuxDllName, EntryPoint = "nvmlShutdown")]
        private static extern NvmlReturn LinuxNvmlShutdown();

        [DllImport(LinuxDllName, EntryPoint = "nvmlDeviceGetHandleByIndex_v2")]
        private static extern NvmlReturn LinuxNvmlDeviceGetHandleByIndex(uint index, ref NvmlDevice device);

        [DllImport(LinuxDllName, EntryPoint = "nvmlDeviceGetHandleByIndex")]
        private static extern NvmlReturn LinuxNvmlDeviceGetHandleByIndexLegacy(uint index, ref NvmlDevice device);

        [DllImport(LinuxDllName, EntryPoint = "nvmlDeviceGetPowerUsage")]
        private static extern NvmlReturn LinuxNvmlDeviceGetPowerUsage(NvmlDevice device, ref uint power);
    }
}
