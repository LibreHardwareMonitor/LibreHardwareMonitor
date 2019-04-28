namespace OpenHardwareMonitor.Hardware.Nvidia {
    internal enum NvmlReturn {
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
}
