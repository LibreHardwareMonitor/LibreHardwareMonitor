namespace LibreHardwareMonitor.Interop.PowerMonitor;

using WVP2C = WireViewPro2Constants;

public static class StructureConversion
{
    //1 <-> 2

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

    //1 <-> 3

    public static DeviceConfigStructV3 ConvertConfigV1ToV3(DeviceConfigStructV1 configV1)
    {
        DeviceConfigStructV2 configV2 = ConvertConfigV1ToV2(configV1);
        return ConvertConfigV2ToV3(configV2);
    }

    public static DeviceConfigStructV1 ConvertConfigV3ToV1(DeviceConfigStructV3 configV3)
    {
        DeviceConfigStructV2 configV2 = ConvertConfigV3ToV2(configV3);
        return ConvertConfigV2ToV1(configV2);
    }

    //2 <-> 3

    public static DeviceConfigStructV3 ConvertConfigV2ToV3(DeviceConfigStructV2 configV2)
    {
        DeviceConfigStructV3 configV3 = new DeviceConfigStructV3
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
            Average = configV2.Average,
            Ui = new UiConfigStructV2
            {
                DefaultScreen = Screen.ScreenMain,
                CurrentScale = configV2.Ui.CurrentScale,
                PowerScale = configV2.Ui.PowerScale,
                DisplayRotation = configV2.Ui.DisplayRotation,
                TimeoutMode = configV2.Ui.TimeoutMode,
                CycleScreens = configV2.Ui.CycleScreens,
                CycleTime = configV2.Ui.CycleTime,
                Timeout = configV2.Ui.Timeout,
                PrimaryColor = configV2.Ui.Theme == Theme.ThemeTg1 ? WVP2C.THEME_PRIMARY_COLOR_TG1 : configV2.Ui.Theme == Theme.ThemeTg2 ? WVP2C.THEME_PRIMARY_COLOR_TG2 : WVP2C.THEME_PRIMARY_COLOR_TG3,
                SecondaryColor = configV2.Ui.Theme == Theme.ThemeTg1 ? WVP2C.THEME_SECONDARY_COLOR_TG1 : configV2.Ui.Theme == Theme.ThemeTg2 ? WVP2C.THEME_SECONDARY_COLOR_TG2 : WVP2C.THEME_SECONDARY_COLOR_TG3,
                HighlightColor = configV2.Ui.Theme == Theme.ThemeTg1 ? WVP2C.THEME_HIGHLIGHT_COLOR_TG1 : configV2.Ui.Theme == Theme.ThemeTg2 ? WVP2C.THEME_HIGHLIGHT_COLOR_TG2 : WVP2C.THEME_HIGHLIGHT_COLOR_TG3,
                BackgroundColor = configV2.Ui.Theme == Theme.ThemeTg1 ? WVP2C.THEME_BACKGROUND_COLOR_TG1 : configV2.Ui.Theme == Theme.ThemeTg2 ? WVP2C.THEME_BACKGROUND_COLOR_TG2 : WVP2C.THEME_BACKGROUND_COLOR_TG3,
                BackgroundBitmapId = configV2.Ui.Theme == Theme.ThemeTg1 ? (byte)THEME_BACKGROUND.ThermalGrizzlyOrange : configV2.Ui.Theme == Theme.ThemeTg2 ? (byte)THEME_BACKGROUND.ThermalGrizzlyDark : (byte)THEME_BACKGROUND.Disabled,
                FanBitmapId = configV2.Ui.Theme == Theme.ThemeTg1 ? (byte)THEME_FAN.ThermalGrizzlyOrange : configV2.Ui.Theme == Theme.ThemeTg2 ? (byte)THEME_FAN.ThermalGrizzlyDark : (byte)THEME_FAN.ThermalGrizzlyBlackWhite,
                DisplayInversion = DISPLAY_INVERSION.DISPLAY_INVERSION_OFF // Default off
            }
        };
        return configV3;
    }

    public static DeviceConfigStructV2 ConvertConfigV3ToV2(DeviceConfigStructV3 configV3)
    {
        DeviceConfigStructV2 configV2 = new DeviceConfigStructV2
        {
            Crc = configV3.Crc,
            Version = configV3.Version,
            FriendlyName = configV3.FriendlyName,
            FanConfig = configV3.FanConfig,
            BacklightDuty = configV3.BacklightDuty,
            FaultDisplayEnable = configV3.FaultDisplayEnable,
            FaultBuzzerEnable = configV3.FaultBuzzerEnable,
            FaultSoftPowerEnable = configV3.FaultSoftPowerEnable,
            FaultHardPowerEnable = configV3.FaultHardPowerEnable,
            TsFaultThreshold = configV3.TsFaultThreshold,
            OcpFaultThreshold = configV3.OcpFaultThreshold,
            WireOcpFaultThreshold = configV3.WireOcpFaultThreshold,
            OppFaultThreshold = configV3.OppFaultThreshold,
            CurrentImbalanceFaultThreshold = configV3.CurrentImbalanceFaultThreshold,
            CurrentImbalanceFaultMinLoad = configV3.CurrentImbalanceFaultMinLoad,
            ShutdownWaitTime = configV3.ShutdownWaitTime,
            LoggingInterval = configV3.LoggingInterval,
            Average = configV3.Average,
            Ui = new UiConfigStructV1
            {
                Theme = configV3.Ui.BackgroundBitmapId == (int)THEME_BACKGROUND.ThermalGrizzlyOrange ? Theme.ThemeTg1 : configV3.Ui.BackgroundBitmapId == (int)THEME_BACKGROUND.ThermalGrizzlyDark ? Theme.ThemeTg2 : Theme.ThemeTg3, // Infer theme from bitmap (best effort)
                CurrentScale = configV3.Ui.CurrentScale,
                PowerScale = configV3.Ui.PowerScale,
                DisplayRotation = configV3.Ui.DisplayRotation,
                TimeoutMode = configV3.Ui.TimeoutMode,
                CycleScreens = configV3.Ui.CycleScreens,
                CycleTime = configV3.Ui.CycleTime,
                Timeout = configV3.Ui.Timeout
            }
        };
        return configV2;
    }
}
