// SPDX-License-Identifier: MPL-2.0

using LibreHardwareMonitor.Avalonia.Mvvm.Services;

namespace LibreHardwareMonitor.Avalonia.UI.MockData;

/// <summary>
/// Provides the mock font service implementation.
/// </summary>
internal sealed class MockFontService : IFontService
{
    /// <summary>
    /// Stores the known font families entries used by this component.
    /// </summary>
    private static readonly IReadOnlyList<string> FontFamilies =
    [
        "Segoe UI",
        "Arial",
        "Consolas",
    ];

    /// <inheritdoc/>
    public string GlobalFontFamily { get; set; } = "Segoe UI";

    /// <inheritdoc/>
    public double GlobalFontSize { get; set; } = 12;

    /// <inheritdoc/>
    public IReadOnlyList<string> GetSystemFontFamilies()
    {
        return FontFamilies;
    }
}
