// SPDX-License-Identifier: MPL-2.0

using Avalonia;
using LibreHardwareMonitor.Avalonia.Localization.Services;
using LibreHardwareMonitor.Avalonia.UI.Application;

namespace LibreHardwareMonitor.Avalonia;

/// <summary>
/// Provides the desktop application entry point.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Starts the application using the language selected from the operating system.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    [STAThread]
    private static void Main(string[] args)
    {
        AppCulture.SetCurrent(AppCulture.GetSystemDefaultName());

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// Configures the desktop renderer and application services.
    /// </summary>
    /// <returns>The configured Avalonia application builder.</returns>
    private static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
