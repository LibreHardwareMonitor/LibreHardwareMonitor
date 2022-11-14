using System;
using System.Runtime.InteropServices;
using System.Text;

namespace LibreHardwareMonitor.Interop;

internal static class NvApi
{
    public const int MAX_CLOCKS_PER_GPU = 0x120;
    public const int MAX_COOLERS_PER_GPU = 20;
    public const int MAX_FAN_CONTROLLER_ITEMS = 32;
    public const int MAX_FAN_COOLERS_STATUS_ITEMS = 32;
    public const int MAX_GPU_PUBLIC_CLOCKS = 32;
    public const int MAX_GPU_UTILIZATIONS = 8;
    public const int MAX_MEMORY_VALUES_PER_GPU = 5;
    public const int MAX_PHYSICAL_GPUS = 64;
    public const int MAX_POWER_TOPOLOGIES = 4;
    public const int MAX_THERMAL_SENSORS_PER_GPU = 3;
    public const int MAX_USAGES_PER_GPU = 8;

    public const int SHORT_STRING_MAX = 64;
    public const int THERMAL_SENSOR_RESERVED_COUNT = 8;
    public const int THERMAL_SENSOR_TEMPERATURE_COUNT = 32;

    private const string DllName = "nvapi.dll";
    private const string DllName64 = "nvapi64.dll";

    public static readonly NvAPI_EnumNvidiaDisplayHandleDelegate NvAPI_EnumNvidiaDisplayHandle;
    public static readonly NvAPI_EnumPhysicalGPUsDelegate NvAPI_EnumPhysicalGPUs;
    public static readonly NvAPI_GetDisplayDriverVersionDelegate NvAPI_GetDisplayDriverVersion;
    public static readonly NvAPI_GetPhysicalGPUsFromDisplayDelegate NvAPI_GetPhysicalGPUsFromDisplay;
    public static readonly NvAPI_GPU_ClientFanCoolersGetControlDelegate NvAPI_GPU_ClientFanCoolersGetControl;
    public static readonly NvAPI_GPU_ClientFanCoolersGetStatusDelegate NvAPI_GPU_ClientFanCoolersGetStatus;
    public static readonly NvAPI_GPU_ClientFanCoolersSetControlDelegate NvAPI_GPU_ClientFanCoolersSetControl;
    public static readonly NvAPI_GPU_ClientPowerTopologyGetStatusDelegate NvAPI_GPU_ClientPowerTopologyGetStatus;
    public static readonly NvAPI_GPU_GetAllClockFrequenciesDelegate NvAPI_GPU_GetAllClockFrequencies;
    public static readonly NvAPI_GPU_GetAllClocksDelegate NvAPI_GPU_GetAllClocks;
    public static readonly NvAPI_GPU_GetBusIdDelegate NvAPI_GPU_GetBusId;
    public static readonly NvAPI_GPU_GetCoolerSettingsDelegate NvAPI_GPU_GetCoolerSettings;
    public static readonly NvAPI_GPU_GetDynamicPstatesInfoExDelegate NvAPI_GPU_GetDynamicPstatesInfoEx;
    public static readonly NvAPI_GPU_GetMemoryInfoDelegate NvAPI_GPU_GetMemoryInfo;
    public static readonly NvAPI_GPU_GetPCIIdentifiersDelegate NvAPI_GPU_GetPCIIdentifiers;
    public static readonly NvAPI_GPU_GetTachReadingDelegate NvAPI_GPU_GetTachReading;
    public static readonly NvAPI_GPU_GetThermalSettingsDelegate NvAPI_GPU_GetThermalSettings;
    public static readonly NvAPI_GPU_GetUsagesDelegate NvAPI_GPU_GetUsages;
    public static readonly NvAPI_GPU_SetCoolerLevelsDelegate NvAPI_GPU_SetCoolerLevels;
    public static readonly NvAPI_GPU_GetThermalSensorsDelegate NvAPI_GPU_ThermalGetSensors;

    private static readonly NvAPI_GetInterfaceVersionStringDelegate _nvAPI_GetInterfaceVersionString;
    private static readonly NvAPI_GPU_GetFullNameDelegate _nvAPI_GPU_GetFullName;

    static NvApi()
    {
        NvAPI_InitializeDelegate nvApiInitialize;

        try
        {
            if (!DllExists())
                return;

            GetDelegate(0x0150E828, out nvApiInitialize);
        }
        catch (Exception e) when (e is DllNotFoundException or ArgumentNullException or EntryPointNotFoundException or BadImageFormatException)
        {
            return;
        }

        if (nvApiInitialize() == NvStatus.OK)
        {
            GetDelegate(0xE3640A56, out NvAPI_GPU_GetThermalSettings);
            GetDelegate(0xCEEE8E9F, out _nvAPI_GPU_GetFullName);
            GetDelegate(0x9ABDD40D, out NvAPI_EnumNvidiaDisplayHandle);
            GetDelegate(0x34EF9506, out NvAPI_GetPhysicalGPUsFromDisplay);
            GetDelegate(0xE5AC921F, out NvAPI_EnumPhysicalGPUs);
            GetDelegate(0x5F608315, out NvAPI_GPU_GetTachReading);
            GetDelegate(0x1BD69F49, out NvAPI_GPU_GetAllClocks);
            GetDelegate(0x60DED2ED, out NvAPI_GPU_GetDynamicPstatesInfoEx);
            GetDelegate(0x189A1FDF, out NvAPI_GPU_GetUsages);
            GetDelegate(0xDA141340, out NvAPI_GPU_GetCoolerSettings);
            GetDelegate(0x891FA0AE, out NvAPI_GPU_SetCoolerLevels);
            GetDelegate(0x774AA982, out NvAPI_GPU_GetMemoryInfo);
            GetDelegate(0xF951A4D1, out NvAPI_GetDisplayDriverVersion);
            GetDelegate(0x01053FA5, out _nvAPI_GetInterfaceVersionString);
            GetDelegate(0x2DDFB66E, out NvAPI_GPU_GetPCIIdentifiers);
            GetDelegate(0x1BE0B8E5, out NvAPI_GPU_GetBusId);
            GetDelegate(0x35AED5E8, out NvAPI_GPU_ClientFanCoolersGetStatus);
            GetDelegate(0xDCB616C3, out NvAPI_GPU_GetAllClockFrequencies);
            GetDelegate(0x814B209F, out NvAPI_GPU_ClientFanCoolersGetControl);
            GetDelegate(0xA58971A5, out NvAPI_GPU_ClientFanCoolersSetControl);
            GetDelegate(0x0EDCF624E, out NvAPI_GPU_ClientPowerTopologyGetStatus);
            GetDelegate(0x65FE3AAD, out NvAPI_GPU_ThermalGetSensors);

            IsAvailable = true;
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate NvStatus NvAPI_EnumNvidiaDisplayHandleDelegate(int thisEnum, ref NvDisplayHandle displayHandle);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate NvStatus NvAPI_EnumPhysicalGPUsDelegate([Out] NvPhysicalGpuHandle[] gpuHandles, out int gpuCount);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate NvStatus NvAPI_GetDisplayDriverVersionDelegate(NvDisplayHandle displayHandle, [In, Out] ref NvDisplayDriverVersion nvDisplayDriverVersion);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate NvStatus NvAPI_GetInterfaceVersionStringDelegate(StringBuilder version);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate NvStatus NvAPI_GetPhysicalGPUsFromDisplayDelegate(NvDisplayHandle displayHandle, [Out] NvPhysicalGpuHandle[] gpuHandles, out uint gpuCount);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate NvStatus NvAPI_GPU_ClientFanCoolersGetControlDelegate(NvPhysicalGpuHandle gpuHandle, ref NvFanCoolerControl control);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate NvStatus NvAPI_GPU_ClientFanCoolersGetStatusDelegate(NvPhysicalGpuHandle gpuHandle, ref NvFanCoolersStatus fanCoolersStatus);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate NvStatus NvAPI_GPU_ClientFanCoolersSetControlDelegate(NvPhysicalGpuHandle gpuHandle, ref NvFanCoolerControl control);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate NvStatus NvAPI_GPU_ClientPowerTopologyGetStatusDelegate(NvPhysicalGpuHandle gpuHandle, ref NvPowerTopology powerTopology);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate NvStatus NvAPI_GPU_GetAllClockFrequenciesDelegate(NvPhysicalGpuHandle gpuHandle, ref NvGpuClockFrequencies clockFrequencies);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate NvStatus NvAPI_GPU_GetAllClocksDelegate(NvPhysicalGpuHandle gpuHandle, ref NvClocks nvClocks);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate NvStatus NvAPI_GPU_GetBusIdDelegate(NvPhysicalGpuHandle gpuHandle, out uint busId);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate NvStatus NvAPI_GPU_GetCoolerSettingsDelegate(NvPhysicalGpuHandle gpuHandle, NvCoolerTarget coolerTarget, ref NvCoolerSettings NvCoolerSettings);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate NvStatus NvAPI_GPU_GetDynamicPstatesInfoExDelegate(NvPhysicalGpuHandle gpuHandle, ref NvDynamicPStatesInfo nvPStates);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate NvStatus NvAPI_GPU_GetMemoryInfoDelegate(NvDisplayHandle displayHandle, ref NvMemoryInfo nvMemoryInfo);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate NvStatus NvAPI_GPU_GetPCIIdentifiersDelegate(NvPhysicalGpuHandle gpuHandle, out uint deviceId, out uint subSystemId, out uint revisionId, out uint extDeviceId);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate NvStatus NvAPI_GPU_GetTachReadingDelegate(NvPhysicalGpuHandle gpuHandle, out int value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate NvStatus NvAPI_GPU_GetThermalSensorsDelegate(NvPhysicalGpuHandle gpuHandle, ref NvThermalSensors nvThermalSensors);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate NvStatus NvAPI_GPU_GetThermalSettingsDelegate(NvPhysicalGpuHandle gpuHandle, int sensorIndex, ref NvThermalSettings NvThermalSettings);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate NvStatus NvAPI_GPU_GetUsagesDelegate(NvPhysicalGpuHandle gpuHandle, ref NvUsages nvUsages);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate NvStatus NvAPI_GPU_SetCoolerLevelsDelegate(NvPhysicalGpuHandle gpuHandle, int coolerIndex, ref NvCoolerLevels NvCoolerLevels);

    public enum NvFanControlMode : uint
    {
        Auto = 0,
        Manual = 1
    }

    public enum NvLevelPolicy : uint
    {
        None = 0,
        Manual = 1,
        Performance = 2,
        TemperatureDiscrete = 4,
        TemperatureContinuous = 8,
        Silent = 16,
        Auto = 32
    }

    public enum NvPowerTopologyDomain : uint
    {
        Gpu = 0,
        Board
    }

    public enum NvUtilizationDomain
    {
        Gpu, // Core
        FrameBuffer, // Memory Controller
        VideoEngine, // Video Engine
        BusInterface // Bus
    }

    public static bool IsAvailable { get; }

    [DllImport(DllName, EntryPoint = "nvapi_QueryInterface", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern IntPtr NvAPI32_QueryInterface(uint interfaceId);

    [DllImport(DllName64, EntryPoint = "nvapi_QueryInterface", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern IntPtr NvAPI64_QueryInterface(uint interfaceId);

    public static NvStatus NvAPI_GPU_GetFullName(NvPhysicalGpuHandle gpuHandle, out string name)
    {
        StringBuilder builder = new(SHORT_STRING_MAX);
        NvStatus status = _nvAPI_GPU_GetFullName?.Invoke(gpuHandle, builder) ?? NvStatus.FunctionNotFound;

        name = builder.ToString();
        return status;
    }

    public static NvStatus NvAPI_GetInterfaceVersionString(out string version)
    {
        StringBuilder builder = new(SHORT_STRING_MAX);
        NvStatus status = _nvAPI_GetInterfaceVersionString?.Invoke(builder) ?? NvStatus.FunctionNotFound;

        version = builder.ToString();
        return status;
    }

    private static void GetDelegate<T>(uint id, out T newDelegate) where T : class
    {
        IntPtr ptr = Environment.Is64BitOperatingSystem ? NvAPI64_QueryInterface(id) : NvAPI32_QueryInterface(id);

        if (ptr != IntPtr.Zero)
            newDelegate = Marshal.GetDelegateForFunctionPointer(ptr, typeof(T)) as T;
        else
            newDelegate = null;
    }

    public static bool DllExists()
    {
        IntPtr module = Kernel32.LoadLibrary(Environment.Is64BitOperatingSystem ? DllName64 : DllName);
        if (module == IntPtr.Zero)
            return false;

        Kernel32.FreeLibrary(module);
        return true;
    }

    internal static int MAKE_NVAPI_VERSION<T>(int ver)
    {
        return Marshal.SizeOf<T>() | (ver << 16);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct NvFanCoolerControl
    {
        public uint Version;
        private readonly uint _reserved;
        public uint Count;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U4)]
        private readonly uint[] _reserved2;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_FAN_CONTROLLER_ITEMS)]
        public NvFanCoolerControlItem[] Items;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct NvFanCoolerControlItem
    {
        public uint CoolerId;
        public uint Level;
        public NvFanControlMode ControlMode;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U4)]
        private readonly uint[] _reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct NvPowerTopology
    {
        public int Version;
        public uint Count;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_POWER_TOPOLOGIES, ArraySubType = UnmanagedType.Struct)]
        public NvPowerTopologyEntry[] Entries;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct NvPowerTopologyEntry
    {
        public NvPowerTopologyDomain Domain;
        private readonly uint _reserved;
        public uint PowerUsage;
        private readonly uint _reserved1;
    }

    internal enum NvStatus
    {
        OK = 0,
        Error = -1,
        LibraryNotFound = -2,
        NoImplementation = -3,
        ApiNotInitialized = -4,
        InvalidArgument = -5,
        NvidiaDeviceNotFound = -6,
        EndEnumeration = -7,
        InvalidHandle = -8,
        IncompatibleStructVersion = -9,
        HandleInvalidated = -10,
        OpenGlContextNotCurrent = -11,
        NoGlExpert = -12,
        InstrumentationDisabled = -13,
        ExpectedLogicalGpuHandle = -100,
        ExpectedPhysicalGpuHandle = -101,
        ExpectedDisplayHandle = -102,
        InvalidCombination = -103,
        NotSupported = -104,
        PortIdNotFound = -105,
        ExpectedUnattachedDisplayHandle = -106,
        InvalidPerfLevel = -107,
        DeviceBusy = -108,
        NvPersistFileNotFound = -109,
        PersistDataNotFound = -110,
        ExpectedTvDisplay = -111,
        ExpectedTvDisplayOnConnector = -112,
        NoActiveSliTopology = -113,
        SliRenderingModeNotAllowed = -114,
        ExpectedDigitalFlatPanel = -115,
        ArgumentExceedMaxSize = -116,
        DeviceSwitchingNotAllowed = -117,
        TestingClocksNotSupported = -118,
        UnknownUnderscanConfig = -119,
        TimeoutReconfiguringGpuTopo = -120,
        DataNotFound = -121,
        ExpectedAnalogDisplay = -122,
        NoVidLink = -123,
        RequiresReboot = -124,
        InvalidHybridMode = -125,
        MixedTargetTypes = -126,
        Syswow64NotSupported = -127,
        ImplicitSetGpuTopologyChangeNotAllowed = -128,
        RequestUserToCloseNonMigratableApps = -129,
        OutOfMemory = -130,
        WasStillDrawing = -131,
        FileNotFound = -132,
        TooManyUniqueStateObjects = -133,
        InvalidCall = -134,
        D3D101LibraryNotFound = -135,
        FunctionNotFound = -136
    }

    internal enum NvThermalController
    {
        None = 0,
        GpuInternal,
        Adm1032,
        Max6649,
        Max1617,
        Lm99,
        Lm89,
        Lm64,
        Adt7473,
        SbMax6649,
        VBiosEvt,
        OS,
        Unknown = -1
    }

    internal enum NvThermalTarget
    {
        None = 0,
        Gpu = 1,
        Memory = 2,
        PowerSupply = 4,
        Board = 8,
        VisualComputingBoard = 9,
        VisualComputingInlet = 10,
        VisualComputingOutlet = 11,
        All = 15,
        Unknown = -1
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct NvSensor
    {
        public NvThermalController Controller;
        public uint DefaultMinTemp;
        public uint DefaultMaxTemp;
        public uint CurrentTemp;
        public NvThermalTarget Target;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct NvThermalSettings
    {
        public uint Version;
        public uint Count;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_THERMAL_SENSORS_PER_GPU)]
        public NvSensor[] Sensor;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NvDisplayHandle
    {
        private readonly IntPtr ptr;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NvPhysicalGpuHandle
    {
        private readonly IntPtr ptr;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct NvClocks
    {
        public uint Version;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_CLOCKS_PER_GPU)]
        public uint[] Clock;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct NvDynamicPStatesInfo
    {
        public uint Version;
        public uint Flags;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_GPU_UTILIZATIONS)]
        public NvDynamicPState[] Utilizations;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct NvDynamicPState
    {
        public bool IsPresent;
        public int Percentage;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct NvUsages
    {
        public uint Version;
        private readonly uint _reserved;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public NvUsagesEntry[] Entries;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct NvUsagesEntry
    {
        public uint IsPresent;
        public uint Percentage;
        private readonly uint _reserved1;
        private readonly uint _reserved2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct NvCooler
    {
        public int Type;
        public int Controller;
        public int DefaultMin;
        public int DefaultMax;
        public int CurrentMin;
        public int CurrentMax;
        public int CurrentLevel;
        public int DefaultPolicy;
        public int CurrentPolicy;
        public int Target;
        public int ControlType;
        public int Active;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct NvFanCoolersStatus
    {
        public uint Version;
        public uint Count;

        public ulong Reserved1;
        public ulong Reserved2;
        public ulong Reserved3;
        public ulong Reserved4;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_FAN_COOLERS_STATUS_ITEMS)]
        internal NvFanCoolersStatusItem[] Items;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct NvFanCoolersStatusItem
    {
        public uint CoolerId;
        public uint CurrentRpm;
        public uint CurrentMinLevel;
        public uint CurrentMaxLevel;
        public uint CurrentLevel;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U4)]
        private readonly uint[] _reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct NvCoolerSettings
    {
        public uint Version;
        public uint Count;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_COOLERS_PER_GPU)]
        public NvCooler[] Cooler;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct NvLevel
    {
        public int Level;
        public NvLevelPolicy Policy;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct NvCoolerLevels
    {
        public uint Version;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_COOLERS_PER_GPU)]
        public NvLevel[] Levels;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct NvThermalSensors
    {
        internal uint Version;
        internal uint Mask;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = THERMAL_SENSOR_RESERVED_COUNT)]
        internal int[] Reserved;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = THERMAL_SENSOR_TEMPERATURE_COUNT)]
        internal int[] Temperatures;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct NvMemoryInfo
    {
        public uint Version;

        public uint DedicatedVideoMemory;

        public uint AvailableDedicatedVideoMemory;

        public uint SystemVideoMemory;

        public uint SharedSystemMemory;

        public uint CurrentAvailableDedicatedVideoMemory;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct NvDisplayDriverVersion
    {
        public uint Version;
        public uint DriverVersion;
        public uint BldChangeListNum;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SHORT_STRING_MAX)]
        public string BuildBranch;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SHORT_STRING_MAX)]
        public string Adapter;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct NvGpuClockFrequencies
    {
        public uint Version;
        private readonly uint _reserved;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_GPU_PUBLIC_CLOCKS)]
        public NvGpuClockFrequenciesDomain[] Clocks;
    }

    internal enum NvGpuPublicClockId
    {
        Graphics = 0,
        Memory = 4,
        Processor = 7,
        Video = 8,
        Undefined = MAX_CLOCKS_PER_GPU
    }

    internal enum NvGpuClockFrequenciesClockType
    {
        CurrentFrequency,
        BaseClock,
        BoostClock,
        ClockTypeNumber
    }

    internal enum NvCoolerTarget
    {
        None = 0,
        Gpu,
        Memory,
        PowerSupply = 4,
        All = 7 // This cooler cools all of the components related to its target gpu.
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct NvGpuClockFrequenciesDomain
    {
        private readonly uint _isPresent;
        public uint Frequency;

        public bool IsPresent => (_isPresent & 1) != 0;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate NvStatus NvAPI_InitializeDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate NvStatus NvAPI_GPU_GetFullNameDelegate(NvPhysicalGpuHandle gpuHandle, StringBuilder name);
}
