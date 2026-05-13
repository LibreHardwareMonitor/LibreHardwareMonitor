using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace LibreHardwareMonitor.UI.ViewModels;

public class PercentToWidthConverter : IValueConverter
{
    public static readonly PercentToWidthConverter Instance = new();

    private const double MaxWidth = 192;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is float percent)
            return Math.Clamp(percent / 100.0 * MaxWidth, 0, MaxWidth);
        return 0.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class LogButtonConverter : IValueConverter
{
    public static readonly LogButtonConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "Stop Logging" : "Start Logging";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class LogButtonBgConverter : IValueConverter
{
    public static readonly LogButtonBgConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true
            ? SolidColorBrush.Parse("#EF4444")
            : SolidColorBrush.Parse("#F59E0B");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class TempUnitConverter : IValueConverter
{
    public static readonly TempUnitConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => LibreHardwareMonitor.UI.Converters.SensorUnitHelper.TempUnit;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class NonZeroConverter : IValueConverter
{
    public static readonly NonZeroConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is float f) return f > 0;
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
