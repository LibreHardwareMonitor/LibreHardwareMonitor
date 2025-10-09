using System;
using System.Runtime.InteropServices;
using Windows.Win32;

namespace LibreHardwareMonitor.Interop;

internal static class IntelGcl
{
    public const int CTL_FAN_COUNT = 5;
    public const uint CTL_IMPL_MAJOR_VERSION = 1;
    public const uint CTL_IMPL_MINOR_VERSION = 1;
    public const uint CTL_IMPL_VERSION = (CTL_IMPL_MAJOR_VERSION << 16) | CTL_IMPL_MINOR_VERSION;
    public const int CTL_MAX_DEVICE_NAME_LEN = 100;
    public const int CTL_MAX_RESERVED_SIZE = 112;
    public const int CTL_PSU_COUNT = 5;
    public const int MAX_DEVICES = 64;
    public const int MAX_STRING_LENGTH = 256;

    private const string DllName = "ControlLib.dll";

    static IntelGcl()
    {
        IsAvailable = GclMethodExists(nameof(ctlInit)) && GclMethodExists(nameof(ctlEnumerateDevices));
    }

    public enum ctl_data_type_t
    {
        CTL_DATA_TYPE_INT8 = 0,
        CTL_DATA_TYPE_UINT8 = 1,
        CTL_DATA_TYPE_INT16 = 2,
        CTL_DATA_TYPE_UINT16 = 3,
        CTL_DATA_TYPE_INT32 = 4,
        CTL_DATA_TYPE_UINT32 = 5,
        CTL_DATA_TYPE_INT64 = 6,
        CTL_DATA_TYPE_UINT64 = 7,
        CTL_DATA_TYPE_FLOAT = 8,
        CTL_DATA_TYPE_DOUBLE = 9,
        CTL_DATA_TYPE_STRING_ASCII = 10,
        CTL_DATA_TYPE_STRING_UTF16 = 11,
        CTL_DATA_TYPE_STRING_UTF132 = 12,
        CTL_DATA_TYPE_UNKNOWN = 0x4800FFFF
    }

    public enum ctl_device_type_t
    {
        CTL_DEVICE_TYPE_GRAPHICS = 1,
        CTL_DEVICE_TYPE_SYSTEM = 2,
        CTL_DEVICE_TYPE_MAX
    }

    public enum ctl_fan_speed_mode_t
    {
        CTL_FAN_SPEED_MODE_DEFAULT = 0,
        CTL_FAN_SPEED_MODE_FIXED = 1,
        CTL_FAN_SPEED_MODE_TABLE = 2,
        CTL_FAN_SPEED_MODE_MAX
    }

    public enum ctl_fan_speed_units_t
    {
        CTL_FAN_SPEED_UNITS_RPM = 0,
        CTL_FAN_SPEED_UNITS_PERCENT = 1,
        CTL_FAN_SPEED_UNITS_MAX
    }

    public enum ctl_freq_domain_t
    {
        CTL_FREQ_DOMAIN_GPU = 0,
        CTL_FREQ_DOMAIN_MEMORY = 1,
        CTL_FREQ_DOMAIN_MEDIA = 2,
        CTL_FREQ_DOMAIN_MAX
    }

    // Initialization flags
    public enum ctl_init_flag_t : uint
    {
        CTL_INIT_FLAG_USE_LEVEL_ZERO = 1 << 0, // CTL_BIT(0) - Required for telemetry
        CTL_INIT_FLAG_MAX = 0x80000000
    }

    public enum ctl_psu_type_t
    {
        CTL_PSU_TYPE_PSU_NONE = 0,
        CTL_PSU_TYPE_PSU_PCIE = 1,
        CTL_PSU_TYPE_PSU_6PIN = 2,
        CTL_PSU_TYPE_PSU_8PIN = 3
    }

    // Enums
    public enum ctl_result_t
    {
        CTL_RESULT_SUCCESS = 0x00000000,
        CTL_RESULT_SUCCESS_STILL_OPEN_BY_ANOTHER_CALLER = 0x00000001,
        CTL_RESULT_ERROR_SUCCESS_END = 0x0000FFFF,
        CTL_RESULT_ERROR_GENERIC_START = 0x40000000,
        CTL_RESULT_ERROR_NOT_INITIALIZED = 0x40000001,
        CTL_RESULT_ERROR_ALREADY_INITIALIZED = 0x40000002,
        CTL_RESULT_ERROR_DEVICE_LOST = 0x40000003,
        CTL_RESULT_ERROR_OUT_OF_HOST_MEMORY = 0x40000004,
        CTL_RESULT_ERROR_OUT_OF_DEVICE_MEMORY = 0x40000005,
        CTL_RESULT_ERROR_INSUFFICIENT_PERMISSIONS = 0x40000006,
        CTL_RESULT_ERROR_NOT_AVAILABLE = 0x40000007,
        CTL_RESULT_ERROR_UNINITIALIZED = 0x40000008,
        CTL_RESULT_ERROR_UNSUPPORTED_VERSION = 0x40000009,
        CTL_RESULT_ERROR_UNSUPPORTED_FEATURE = 0x4000000a,
        CTL_RESULT_ERROR_INVALID_ARGUMENT = 0x4000000b,
        CTL_RESULT_ERROR_INVALID_API_HANDLE = 0x4000000c,
        CTL_RESULT_ERROR_INVALID_NULL_HANDLE = 0x4000000d,
        CTL_RESULT_ERROR_INVALID_NULL_POINTER = 0x4000000e,
        CTL_RESULT_ERROR_INVALID_SIZE = 0x4000000f,
        CTL_RESULT_ERROR_UNSUPPORTED_SIZE = 0x40000010,
        CTL_RESULT_ERROR_UNSUPPORTED_ALIGNMENT = 0x40000011,
        CTL_RESULT_ERROR_INVALID_SYNCHRONIZATION_OBJECT = 0x40000012,
        CTL_RESULT_ERROR_INVALID_ENUMERATION = 0x40000013,
        CTL_RESULT_ERROR_UNSUPPORTED_ENUMERATION = 0x40000014,
        CTL_RESULT_ERROR_UNSUPPORTED_IMAGE_FORMAT = 0x40000015,
        CTL_RESULT_ERROR_INVALID_NATIVE_BINARY = 0x40000016,
        CTL_RESULT_ERROR_INVALID_GLOBAL_NAME = 0x40000017,
        CTL_RESULT_ERROR_INVALID_KERNEL_NAME = 0x40000018,
        CTL_RESULT_ERROR_INVALID_FUNCTION_NAME = 0x40000019,
        CTL_RESULT_ERROR_INVALID_GROUP_SIZE_DIMENSION = 0x4000001a,
        CTL_RESULT_ERROR_INVALID_GLOBAL_WIDTH_DIMENSION = 0x4000001b,
        CTL_RESULT_ERROR_INVALID_KERNEL_ARGUMENT_INDEX = 0x4000001c,
        CTL_RESULT_ERROR_INVALID_KERNEL_ARGUMENT_SIZE = 0x4000001d,
        CTL_RESULT_ERROR_INVALID_KERNEL_ATTRIBUTE_VALUE = 0x4000001e,
        CTL_RESULT_ERROR_INVALID_MODULE_UNLINKED = 0x4000001f,
        CTL_RESULT_ERROR_INVALID_COMMAND_LIST_TYPE = 0x40000020,
        CTL_RESULT_ERROR_OVERLAPPING_REGIONS = 0x40000021,
        CTL_RESULT_ERROR_UNKNOWN = 0x4000FFFF
    }

    public enum ctl_units_t
    {
        CTL_UNITS_FREQUENCY_MHZ = 0,
        CTL_UNITS_OPERATIONS_GTS = 1,
        CTL_UNITS_OPERATIONS_MTS = 2,
        CTL_UNITS_VOLTAGE_VOLTS = 3,
        CTL_UNITS_POWER_WATTS = 4,
        CTL_UNITS_TEMPERATURE_CELSIUS = 5,
        CTL_UNITS_ENERGY_JOULES = 6,
        CTL_UNITS_TIME_SECONDS = 7,
        CTL_UNITS_MEMORY_BYTES = 8,
        CTL_UNITS_ANGULAR_SPEED_RPM = 9,
        CTL_UNITS_POWER_MILLIWATTS = 10,
        CTL_UNITS_PERCENT = 11,
        CTL_UNITS_MEM_SPEED_GBPS = 12,
        CTL_UNITS_VOLTAGE_MILLIVOLTS = 13,
        CTL_UNITS_BANDWIDTH_MBPS = 14,
        CTL_UNITS_UNKNOWN = 0x4800FFFF
    }

    public static ctl_api_handle_t ApiHandle { get; private set; }

    // Public interface
    public static bool IsAvailable { get; private set; }

    public static bool IsInitialized { get; private set; }

    // P/Invoke declarations
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int ctlInit(ref ctl_init_args_t pInitDesc, ref ctl_api_handle_t phAPIHandle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int ctlEnumerateDevices(ctl_api_handle_t hAPIHandle, ref uint pCount, [Out] ctl_device_adapter_handle_t[] phDevices);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern int ctlGetDeviceProperties(ctl_device_adapter_handle_t hDAhandle, ref ctl_device_adapter_properties_t pProperties);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern int ctlPowerTelemetryGet(ctl_device_adapter_handle_t hDeviceHandle, ref ctl_power_telemetry_t pTelemetryInfo);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern int ctlEnumFans(ctl_device_adapter_handle_t hDAhandle, ref uint pCount, [Out] ctl_fan_handle_t[] phFan);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern int ctlFanGetState(ctl_fan_handle_t hFan, ctl_fan_speed_units_t units, ref int pSpeed);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern int ctlFanGetProperties(ctl_fan_handle_t hFan, ref ctl_fan_properties_t pProperties);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern int ctlEnumFrequencyDomains(ctl_device_adapter_handle_t hDAhandle, ref uint pCount, [Out] ctl_freq_handle_t[] phFrequency);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern int ctlFrequencyGetProperties(ctl_freq_handle_t hFrequency, ref ctl_freq_properties_t pProperties);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern int ctlFrequencyGetState(ctl_freq_handle_t hFrequency, ref ctl_freq_state_t pState);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int ctlClose(ctl_api_handle_t hAPIHandle);

    private static bool GclMethodExists(string gclMethod)
    {
        using FreeLibrarySafeHandle module = PInvoke.LoadLibrary(DllName);
        if (!module.IsInvalid)
        {
            bool result = PInvoke.GetProcAddress(module, gclMethod) != IntPtr.Zero;
            return result;
        }

        return false;
    }

    public static bool Initialize()
    {
        if (!IsAvailable)
            return false;

        if (IsInitialized)
            return true;

        var initArgs = new ctl_init_args_t();
        initArgs.Size = (uint)Marshal.SizeOf(typeof(ctl_init_args_t));
        initArgs.Version = 0;
        initArgs.AppVersion = CTL_IMPL_VERSION;
        initArgs.flags = (uint)ctl_init_flag_t.CTL_INIT_FLAG_USE_LEVEL_ZERO;

        var apiHandle = new ctl_api_handle_t();
        int result = ctlInit(ref initArgs, ref apiHandle);

        if (result != (int)ctl_result_t.CTL_RESULT_SUCCESS)
            return false;

        ApiHandle = apiHandle;
        IsInitialized = true;
        return true;
    }

    public static ctl_device_adapter_handle_t[] GetDeviceHandles()
    {
        if (!IsInitialized && (!Initialize()))
            return Array.Empty<ctl_device_adapter_handle_t>();

        // First call to get the device count
        uint count = 0;
        int result = ctlEnumerateDevices(ApiHandle, ref count, null);
        count = Math.Min(count, MAX_DEVICES);

        if (result != (int)ctl_result_t.CTL_RESULT_SUCCESS || count == 0)
            return Array.Empty<ctl_device_adapter_handle_t>();

        // Second call to get the actual device handles
        var handles = new ctl_device_adapter_handle_t[count];
        result = ctlEnumerateDevices(ApiHandle, ref count, handles);

        if (result != (int)ctl_result_t.CTL_RESULT_SUCCESS)
            return Array.Empty<ctl_device_adapter_handle_t>();

        return handles;
    }

    public static void Cleanup()
    {
        if (IsInitialized)
        {
            try
            {
                ctlClose(ApiHandle);
            }
            catch
            {
                // Ignore cleanup errors
            }
            finally
            {
                IsInitialized = false;
                ApiHandle = new ctl_api_handle_t();
            }
        }
    }

    // Unions
    [StructLayout(LayoutKind.Explicit)]
    public struct ctl_data_value_t
    {
        [FieldOffset(0)]
        public sbyte data8;

        [FieldOffset(0)]
        public byte datau8;

        [FieldOffset(0)]
        public short data16;

        [FieldOffset(0)]
        public ushort datau16;

        [FieldOffset(0)]
        public int data32;

        [FieldOffset(0)]
        public uint datau32;

        [FieldOffset(0)]
        public long data64;

        [FieldOffset(0)]
        public ulong datau64;

        [FieldOffset(0)]
        public float datafloat;

        [FieldOffset(0)]
        public double datadouble;
    }

    // Structures
    [StructLayout(LayoutKind.Sequential)]
    public struct ctl_api_handle_t
    {
        private IntPtr pNext;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ctl_application_id_t
    {
        public uint Data1;
        public ushort Data2;
        public ushort Data3;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Data4;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ctl_init_args_t
    {
        public uint Size;
        public byte Version;
        public uint AppVersion;
        public uint flags;
        public uint SupportedVersion;
        public ctl_application_id_t ApplicationUID;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ctl_device_adapter_handle_t
    {
        private IntPtr pNext;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ctl_fan_handle_t
    {
        private IntPtr pNext;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ctl_fan_speed_t
    {
        public uint Size;
        public byte Version;
        public int speed;
        public ctl_fan_speed_units_t units;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ctl_fan_properties_t
    {
        public uint Size;
        public byte Version;
        public bool canControl;
        public uint supportedModes;
        public uint supportedUnits;
        public int maxRPM;
        public int maxPoints;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ctl_freq_handle_t
    {
        private IntPtr pNext;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ctl_freq_properties_t
    {
        public uint Size;
        public byte Version;
        public ctl_freq_domain_t type;
        public bool canControl;
        public double min;
        public double max;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ctl_freq_state_t
    {
        public uint Size;
        public byte Version;
        public double currentVoltage;
        public double request;
        public double tdp;
        public double efficient;
        public double actual;
        public uint throttleReasons;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ctl_device_adapter_properties_t
    {
        public uint Size;
        public byte Version;
        public IntPtr pDeviceID;
        public uint device_id_size;
        public ctl_device_type_t device_type;
        public uint supported_subfunction_flags;
        public ulong driver_version;
        public ctl_firmware_version_t firmware_version;
        public uint pci_vendor_id;
        public uint pci_device_id;
        public uint rev_id;
        public uint num_eus_per_sub_slice;
        public uint num_sub_slices_per_slice;
        public uint num_slices;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CTL_MAX_DEVICE_NAME_LEN)]
        public string name;

        public uint graphics_adapter_properties;
        public uint Frequency;
        public ushort pci_subsys_id;
        public ushort pci_subsys_vendor_id;
        public ctl_adapter_bdf_t adapter_bdf;
        public uint num_xe_cores;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = CTL_MAX_RESERVED_SIZE)]
        public byte[] reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ctl_adapter_bdf_t
    {
        public byte bus;
        public byte device;
        public byte function;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ctl_firmware_version_t
    {
        public ulong major_version;
        public ulong minor_version;
        public ulong build_number;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ctl_oc_telemetry_item_t
    {
        public bool bSupported;
        public ctl_units_t units;
        public ctl_data_type_t type;
        public ctl_data_value_t value;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ctl_psu_info_t
    {
        public bool bSupported;
        public ctl_psu_type_t psuType;
        public ctl_oc_telemetry_item_t energyCounter;
        public ctl_oc_telemetry_item_t voltage;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ctl_power_telemetry_t
    {
        public uint Size;
        public byte Version;
        public ctl_oc_telemetry_item_t timeStamp;
        public ctl_oc_telemetry_item_t gpuEnergyCounter;
        public ctl_oc_telemetry_item_t gpuVoltage;
        public ctl_oc_telemetry_item_t gpuCurrentClockFrequency;
        public ctl_oc_telemetry_item_t gpuCurrentTemperature;
        public ctl_oc_telemetry_item_t globalActivityCounter;
        public ctl_oc_telemetry_item_t renderComputeActivityCounter;
        public ctl_oc_telemetry_item_t mediaActivityCounter;
        public bool gpuPowerLimited;
        public bool gpuTemperatureLimited;
        public bool gpuCurrentLimited;
        public bool gpuVoltageLimited;
        public bool gpuUtilizationLimited;
        public ctl_oc_telemetry_item_t vramEnergyCounter;
        public ctl_oc_telemetry_item_t vramVoltage;
        public ctl_oc_telemetry_item_t vramCurrentClockFrequency;
        public ctl_oc_telemetry_item_t vramCurrentEffectiveFrequency;
        public ctl_oc_telemetry_item_t vramReadBandwidthCounter;
        public ctl_oc_telemetry_item_t vramWriteBandwidthCounter;
        public ctl_oc_telemetry_item_t vramCurrentTemperature;
        public bool vramPowerLimited;
        public bool vramTemperatureLimited;
        public bool vramCurrentLimited;
        public bool vramVoltageLimited;
        public bool vramUtilizationLimited;
        public ctl_oc_telemetry_item_t totalCardEnergyCounter;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = CTL_PSU_COUNT)]
        public ctl_psu_info_t[] psu;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = CTL_FAN_COUNT)]
        public ctl_oc_telemetry_item_t[] fanSpeed;

        public ctl_oc_telemetry_item_t gpuVrTemp;
        public ctl_oc_telemetry_item_t vramVrTemp;
        public ctl_oc_telemetry_item_t saVrTemp;
        public ctl_oc_telemetry_item_t gpuEffectiveClock;
        public ctl_oc_telemetry_item_t gpuOverVoltagePercent;
        public ctl_oc_telemetry_item_t gpuPowerPercent;
        public ctl_oc_telemetry_item_t gpuTemperaturePercent;
        public ctl_oc_telemetry_item_t vramReadBandwidth;
        public ctl_oc_telemetry_item_t vramWriteBandwidth;
    }
}
