using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LibreHardwareMonitor.UI.Services;
using LibreHardwareMonitor.UI.ViewModels;

namespace LibreHardwareMonitor.UI;

public partial class App : Application
{
    private HardwareMonitorService? _service;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _service = new HardwareMonitorService();
        var mainViewModel = new MainViewModel(_service);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };

            desktop.ShutdownRequested += (_, _) =>
            {
                _service.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
