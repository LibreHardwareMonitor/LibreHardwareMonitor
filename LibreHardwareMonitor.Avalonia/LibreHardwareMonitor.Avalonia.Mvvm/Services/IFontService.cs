// SPDX-License-Identifier: MPL-2.0

namespace LibreHardwareMonitor.Avalonia.Mvvm.Services;

/// <summary>
/// Defines the contract for font service operations.
/// </summary>
public interface IFontService
{
    /// <summary>
    /// Gets or sets the global font family.
    /// </summary>
    string GlobalFontFamily { get; set; }

    /// <summary>
    /// Gets or sets the global font size.
    /// </summary>
    double GlobalFontSize { get; set; }

    /// <summary>
    /// Retrieves system font families.
    /// </summary>
    /// <returns>The requested system font families entries.</returns>
    IReadOnlyList<string> GetSystemFontFamilies();
}
