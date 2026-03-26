using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using LibreHardwareMonitor.Hardware;

namespace LibreHardwareMonitor.UI.Converters;

public class TemperatureToColorConverter : IValueConverter
{
    public static readonly TemperatureToColorConverter Instance = new();

    private static readonly IBrush DefaultBrush = new SolidColorBrush(Color.Parse("#8B949E")).ToImmutable();
    private static readonly IBrush GreenBrush = new SolidColorBrush(Color.Parse("#10B981")).ToImmutable();
    private static readonly IBrush CyanBrush = new SolidColorBrush(Color.Parse("#22D3EE")).ToImmutable();
    private static readonly IBrush YellowBrush = new SolidColorBrush(Color.Parse("#F59E0B")).ToImmutable();
    private static readonly IBrush OrangeBrush = new SolidColorBrush(Color.Parse("#F97316")).ToImmutable();
    private static readonly IBrush RedBrush = new SolidColorBrush(Color.Parse("#EF4444")).ToImmutable();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not float temp)
            return DefaultBrush;

        return temp switch
        {
            < 40 => GreenBrush,
            < 60 => CyanBrush,
            < 75 => YellowBrush,
            < 85 => OrangeBrush,
            _    => RedBrush,
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class LoadToColorConverter : IValueConverter
{
    public static readonly LoadToColorConverter Instance = new();

    private static readonly IBrush DefaultBrush = new SolidColorBrush(Color.Parse("#8B949E")).ToImmutable();
    private static readonly IBrush GreenBrush = new SolidColorBrush(Color.Parse("#10B981")).ToImmutable();
    private static readonly IBrush BlueBrush = new SolidColorBrush(Color.Parse("#3B82F6")).ToImmutable();
    private static readonly IBrush YellowBrush = new SolidColorBrush(Color.Parse("#F59E0B")).ToImmutable();
    private static readonly IBrush OrangeBrush = new SolidColorBrush(Color.Parse("#F97316")).ToImmutable();
    private static readonly IBrush RedBrush = new SolidColorBrush(Color.Parse("#EF4444")).ToImmutable();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not float load)
            return DefaultBrush;

        return load switch
        {
            < 30 => GreenBrush,
            < 60 => BlueBrush,
            < 80 => YellowBrush,
            < 95 => OrangeBrush,
            _    => RedBrush,
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class PercentToAngleConverter : IValueConverter
{
    public static readonly PercentToAngleConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is float percent)
            return percent / 100.0 * 270.0; // 270 degree sweep
        return 0.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class SensorValueFormatConverter : IValueConverter
{
    public static readonly SensorValueFormatConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not float val)
            return "N/A";

        string format = parameter as string ?? "F1";
        return val.ToString(format, CultureInfo.InvariantCulture);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class HardwareTypeToIconConverter : IValueConverter
{
    public static readonly HardwareTypeToIconConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not HardwareType type)
            return "\uE950"; // generic chip

        return type switch
        {
            HardwareType.Cpu => "\uE950",
            HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel => "\uE7F4",
            HardwareType.Memory => "\uE964",
            HardwareType.Motherboard => "\uEBC6",
            HardwareType.Storage => "\uEDA2",
            HardwareType.Network => "\uE839",
            HardwareType.Battery => "\uE83F",
            HardwareType.Psu => "\uE945",
            HardwareType.Cooler => "\uE9CA",
            _ => "\uE950"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class BoolToOpacityConverter : IValueConverter
{
    public static readonly BoolToOpacityConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? 1.0 : 0.5;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class SensorTypeUnitConverter : IValueConverter
{
    public static readonly SensorTypeUnitConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not SensorType type)
            return "";

        return SensorUnitHelper.GetUnit(type);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
