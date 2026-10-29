// SPDX-License-Identifier: MPL-2.0

using LibreHardwareMonitor.Avalonia.Mvvm.Services;

namespace LibreHardwareMonitor.Avalonia.UI.Application;

/// <summary>
/// Provides the global implementation.
/// </summary>
public static class Global
{
    /// <summary>
    /// Gets or sets the font manager.
    /// </summary>
    public static IFontService FontManager { get; set; }
}
