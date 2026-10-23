// SPDX-License-Identifier: MPL-2.0

using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace LibreHardwareMonitor.Avalonia.UI.Converters;

/// <summary>
/// Converts sensor tree icon asset names into cached Avalonia bitmaps.
/// </summary>
public sealed class SensorIconConverter : IValueConverter
{
    /// <summary>
    /// Caches decoded icons by their asset name.
    /// </summary>
    private readonly Dictionary<string, Bitmap> _icons = new();

    /// <summary>
    /// Loads the icon identified by the binding value.
    /// </summary>
    /// <param name="value">The icon asset name.</param>
    /// <param name="targetType">The target binding type.</param>
    /// <param name="parameter">An optional converter parameter.</param>
    /// <param name="culture">The binding culture.</param>
    /// <returns>The decoded icon, or <see langword="null"/> when no icon is assigned.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string name)
            return null;

        if (_icons.TryGetValue(name, out Bitmap icon))
            return icon;

        Uri uri = new("avares://LibreHardwareMonitor.Avalonia.UI/Assets/" + name);
        using Stream stream = AssetLoader.Open(uri);

        icon = new Bitmap(stream);
        _icons.Add(name, icon);

        return icon;
    }

    /// <summary>
    /// Rejects conversion from an image back to an asset name.
    /// </summary>
    /// <param name="value">The bound image value.</param>
    /// <param name="targetType">The target binding type.</param>
    /// <param name="parameter">An optional converter parameter.</param>
    /// <param name="culture">The binding culture.</param>
    /// <returns>This method does not return a value.</returns>
    /// <exception cref="NotSupportedException">Back conversion is not supported.</exception>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
