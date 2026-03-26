using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LibreHardwareMonitor.UI.Services;
using LibreHardwareMonitor.UI.ViewModels;
using LibreHardwareMonitor.UI.Views;

namespace LibreHardwareMonitor.UI;

public partial class App : Application
{
    private HardwareMonitorService? _service;
    private GadgetWindow? _gadgetWindow;
    private GadgetViewModel? _gadgetVm;
    private TrayIcon? _trayIcon;

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

            _gadgetVm = new GadgetViewModel(_service);
            _gadgetVm.LoadConfig();
            if (_gadgetVm.Items.Count == 0)
                _gadgetVm.AddDefaultsCommand.Execute(null);

            _gadgetWindow = new GadgetWindow { DataContext = _gadgetVm };
            mainViewModel.Settings.Gadget = _gadgetVm;

            SetupTrayIcon(desktop);

            desktop.ShutdownRequested += (_, _) =>
            {
                _trayIcon?.Dispose();
                _service.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void SetupTrayIcon(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var menu = new NativeMenu();

        var showMain = new NativeMenuItem("Show Main Window");
        showMain.Click += (_, _) =>
        {
            desktop.MainWindow?.Show();
            desktop.MainWindow?.Activate();
        };

        var toggleWidget = new NativeMenuItem("Toggle Widget");
        toggleWidget.Click += (_, _) => ToggleGadget();

        var lockWidget = new NativeMenuItem(_gadgetVm!.IsPositionLocked ? "Unlock Widget" : "Lock Widget");
        lockWidget.Click += (_, _) =>
        {
            _gadgetVm!.IsPositionLocked = !_gadgetVm.IsPositionLocked;
            lockWidget.Header = _gadgetVm.IsPositionLocked ? "Unlock Widget" : "Lock Widget";
        };

        var exit = new NativeMenuItem("Exit");
        exit.Click += (_, _) => desktop.Shutdown();

        menu.Add(showMain);
        menu.Add(toggleWidget);
        menu.Add(lockWidget);
        menu.Add(new NativeMenuItemSeparator());
        menu.Add(exit);

        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://LibreHardwareMonitor.UI/Assets/Icons/tray-icon.ico"))),
            ToolTipText = "Libre Hardware Monitor",
            Menu = menu,
            IsVisible = true
        };

        _trayIcon.Clicked += (_, _) => ToggleGadget();

        _service!.Updated += () =>
        {
            if (_trayIcon != null && _gadgetVm != null)
                _trayIcon.ToolTipText = _gadgetVm.TooltipText;
        };
    }

    private void ToggleGadget()
    {
        if (_gadgetWindow == null) return;

        if (_gadgetWindow.IsVisible)
            _gadgetWindow.Hide();
        else
            _gadgetWindow.Show();
    }
}
