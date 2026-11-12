// SPDX-License-Identifier: MPL-2.0

using BlackSharp.MVVM.ComponentModel;
using BlackSharp.UI.Avalonia.Global;
using LibreHardwareMonitor.Avalonia.Mvvm.Services;

namespace LibreHardwareMonitor.Avalonia.UI.Services;

/// <summary>
/// Provides the font service implementation.
/// </summary>
public sealed class FontService : ViewModelBase, IFontService
{
    /// <inheritdoc/>
    public string GlobalFontFamily
    {
        get => GlobalFontSettings.Current.FontFamily.Name;
        set
        {
            GlobalFontSettings.Current.FontFamily = GlobalFontSettings.ResolveFontFamily(value);
            GlobalResourceManager.GlobalFontFamily = GlobalFontSettings.Current.FontFamily;

            NotifyAllPropertyChanged();
        }
    }

    /// <inheritdoc/>
    public double GlobalFontSize
    {
        get => GlobalFontSettings.Current.FontSize;
        set
        {
            GlobalFontSettings.Current.FontSize = value;
            GlobalResourceManager.GlobalFontSize = GlobalFontSettings.Current.FontSize;

            NotifyAllPropertyChanged();
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetSystemFontFamilies()
    {
        return GlobalFontSettings.GetSystemFontFamilies();
    }
}
