// SPDX-License-Identifier: MPL-2.0

using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using LibreHardwareMonitor.Avalonia.Monitoring.Services;
using LibreHardwareMonitor.Avalonia.Mvvm.ViewModels;
using LibreHardwareMonitor.Avalonia.UI.MockData;
using LibreHardwareMonitor.Avalonia.UI.Services;
using LibreHardwareMonitor.Avalonia.UI.Views;
using AvaloniaApplication = Avalonia.Application;

namespace LibreHardwareMonitor.Avalonia.UI.Application;

/// <summary>
/// Creates the main window and starts sensor monitoring for the desktop application.
/// </summary>
public sealed partial class App : AvaloniaApplication
{
    /// <summary>
    /// Keeps the sensor monitor alive for the desktop application's lifetime.
    /// </summary>
    private SensorMonitor _monitor;

    /// <summary>
    /// Loads the application styles and resources.
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        if (Design.IsDesignMode)
        {
            Global.FontManager = MainWindowDesignData.FontService;

            RequestedThemeVariant = ThemeVariant.Dark;
        }
        else
        {
            Global.FontManager = new FontService();
        }
    }

    /// <summary>
    /// Creates the main window and connects live sensor updates.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            MainWindowViewModel viewModel = new();
            desktop.MainWindow = new MainWindow(viewModel);

            _monitor = new SensorMonitor();

            _monitor.SnapshotAvailable += snapshot => Dispatcher.UIThread.Post(() => viewModel.Apply(snapshot));
            _monitor.ErrorOccurred += message => Dispatcher.UIThread.Post(() => viewModel.ShowError(message));

            desktop.Exit += (_, _) => _monitor.Dispose();

            _monitor.Start();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
