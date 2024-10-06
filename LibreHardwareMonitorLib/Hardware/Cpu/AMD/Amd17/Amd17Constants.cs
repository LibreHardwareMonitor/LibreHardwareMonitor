namespace LibreHardwareMonitor.Hardware.Cpu.AMD.Amd17
{
    internal class Amd17Constants
    {
        // ReSharper disable InconsistentNaming
        internal const uint COFVID_STATUS = 0xC0010071;
        internal const uint F17H_M01H_SVI = 0x0005A000;
        internal const uint F17H_M01H_THM_TCON_CUR_TMP = 0x00059800;
        internal const uint F17H_M70H_CCD1_TEMP = 0x00059954;
        internal const uint F17H_M61H_CCD1_TEMP = 0x00059b08;
        internal const uint F17H_TEMP_OFFSET_FLAG = 0x80000;
        internal const uint FAMILY_17H_PCI_CONTROL_REGISTER = 0x60;
        internal const uint HWCR = 0xC0010015;
        internal const uint MSR_CORE_ENERGY_STAT = 0xC001029A;
        internal const uint MSR_HARDWARE_PSTATE_STATUS = 0xC0010293;
        internal const uint MSR_PKG_ENERGY_STAT = 0xC001029B;
        internal const uint MSR_PSTATE_0 = 0xC0010064;
        internal const uint MSR_PWR_UNIT = 0xC0010299;
        internal const uint PERF_CTL_0 = 0xC0010000;
        internal const uint PERF_CTR_0 = 0xC0010004;
        // ReSharper restore InconsistentNaming
    }
}
