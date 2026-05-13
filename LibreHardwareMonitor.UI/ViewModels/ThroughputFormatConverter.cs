using System;
using System.Globalization;
using Avalonia.Data.Converters;
using LibreHardwareMonitor.UI.Converters;

namespace LibreHardwareMonitor.UI.ViewModels;

public class ThroughputFormatConverter : IValueConverter
{
    public static readonly ThroughputFormatConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not float bytes) return "0 B/s";
        return SensorUnitHelper.FormatThroughput(bytes);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
