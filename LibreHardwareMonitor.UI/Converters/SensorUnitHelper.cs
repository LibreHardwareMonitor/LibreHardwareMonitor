using LibreHardwareMonitor.Hardware;

namespace LibreHardwareMonitor.UI.Converters;

public static class SensorUnitHelper
{
    public static string GetUnit(SensorType type)
    {
        return type switch
        {
            SensorType.Voltage => "V",
            SensorType.Current => "A",
            SensorType.Power => "W",
            SensorType.Clock => "MHz",
            SensorType.Temperature => "\u00B0C",
            SensorType.Load => "%",
            SensorType.Frequency => "Hz",
            SensorType.Fan => "RPM",
            SensorType.Flow => "L/h",
            SensorType.Control => "%",
            SensorType.Level => "%",
            SensorType.Data => "GB",
            SensorType.SmallData => "MB",
            SensorType.Throughput => "",
            SensorType.Energy => "mWh",
            SensorType.Noise => "dBA",
            SensorType.Humidity => "%",
            _ => ""
        };
    }

    public static string FormatThroughput(float bytesPerSec)
    {
        return bytesPerSec switch
        {
            >= 1_073_741_824 => $"{bytesPerSec / 1_073_741_824:F1} GB/s",
            >= 1_048_576 => $"{bytesPerSec / 1_048_576:F1} MB/s",
            >= 1024 => $"{bytesPerSec / 1024:F1} KB/s",
            _ => $"{bytesPerSec:F0} B/s"
        };
    }
}
