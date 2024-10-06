namespace LibreHardwareMonitor.Hardware.Cpu.Intel
{
    internal class IntelConstants
    {
        internal const uint IA32_PACKAGE_THERM_STATUS = 0x1B1;
        internal const uint IA32_PERF_STATUS = 0x0198;
        internal const uint IA32_TEMPERATURE_TARGET = 0x01A2;
        internal const uint IA32_THERM_STATUS_MSR = 0x019C;

        internal const uint MSR_DRAM_ENERGY_STATUS = 0x619;
        internal const uint MSR_PKG_ENERGY_STATUS = 0x611;
        internal const uint MSR_PLATFORM_INFO = 0xCE;
        internal const uint MSR_PP0_ENERGY_STATUS = 0x639;
        internal const uint MSR_PP1_ENERGY_STATUS = 0x641;
        internal const uint MSR_PLATFORM_ENERGY_STATUS = 0x64D;
        internal const uint MSR_RAPL_POWER_UNIT = 0x606;
    }
}
