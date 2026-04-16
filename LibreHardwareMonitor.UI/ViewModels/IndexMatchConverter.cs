using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace LibreHardwareMonitor.UI.ViewModels;

public class IndexMatchConverter : IValueConverter
{
    private readonly int _targetIndex;

    public static readonly IndexMatchConverter Dashboard = new(0);
    public static readonly IndexMatchConverter Hardware = new(1);
    public static readonly IndexMatchConverter Charts = new(2);
    public static readonly IndexMatchConverter Settings = new(3);

    public IndexMatchConverter(int targetIndex)
    {
        _targetIndex = targetIndex;
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index)
            return index == _targetIndex;
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true)
            return _targetIndex;
        return -1;
    }
}
