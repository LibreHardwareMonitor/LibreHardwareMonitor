namespace LibreHardwareMonitor.Interop.PowerMonitor;

public static class StructureConversion
{
    public static DeviceConfigStructV1 ConvertConfigV2ToV1(DeviceConfigStructV2 configV2)
    {
        DeviceConfigStructV1 configV1 = new DeviceConfigStructV1
        {
            Crc = configV2.Crc,
            Version = configV2.Version,
            FriendlyName = configV2.FriendlyName,
            FanConfig = configV2.FanConfig,
            BacklightDuty = configV2.BacklightDuty,
            FaultDisplayEnable = configV2.FaultDisplayEnable,
            FaultBuzzerEnable = configV2.FaultBuzzerEnable,
            FaultSoftPowerEnable = configV2.FaultSoftPowerEnable,
            FaultHardPowerEnable = configV2.FaultHardPowerEnable,
            TsFaultThreshold = configV2.TsFaultThreshold,
            OcpFaultThreshold = configV2.OcpFaultThreshold,
            WireOcpFaultThreshold = configV2.WireOcpFaultThreshold,
            OppFaultThreshold = configV2.OppFaultThreshold,
            CurrentImbalanceFaultThreshold = configV2.CurrentImbalanceFaultThreshold,
            CurrentImbalanceFaultMinLoad = configV2.CurrentImbalanceFaultMinLoad,
            ShutdownWaitTime = configV2.ShutdownWaitTime,
            LoggingInterval = configV2.LoggingInterval,
            Ui = configV2.Ui
        };
        return configV1;
    }

    public static DeviceConfigStructV2 ConvertConfigV1ToV2(DeviceConfigStructV1 configV1)
    {
        DeviceConfigStructV2 configV2 = new DeviceConfigStructV2
        {
            Crc = configV1.Crc,
            Version = configV1.Version,
            FriendlyName = configV1.FriendlyName,
            FanConfig = configV1.FanConfig,
            BacklightDuty = configV1.BacklightDuty,
            FaultDisplayEnable = configV1.FaultDisplayEnable,
            FaultBuzzerEnable = configV1.FaultBuzzerEnable,
            FaultSoftPowerEnable = configV1.FaultSoftPowerEnable,
            FaultHardPowerEnable = configV1.FaultHardPowerEnable,
            TsFaultThreshold = configV1.TsFaultThreshold,
            OcpFaultThreshold = configV1.OcpFaultThreshold,
            WireOcpFaultThreshold = configV1.WireOcpFaultThreshold,
            OppFaultThreshold = configV1.OppFaultThreshold,
            CurrentImbalanceFaultThreshold = configV1.CurrentImbalanceFaultThreshold,
            CurrentImbalanceFaultMinLoad = configV1.CurrentImbalanceFaultMinLoad,
            ShutdownWaitTime = configV1.ShutdownWaitTime,
            LoggingInterval = configV1.LoggingInterval,
            Average = AVG.AVG_1417MS, // Default value
            Ui = configV1.Ui
        };
        return configV2;
    }
}
