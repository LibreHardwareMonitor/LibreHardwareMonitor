using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibreHardwareMonitor.UI.Services;

namespace LibreHardwareMonitor.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly HardwareMonitorService _service;
    private readonly SensorConfigService _sensorConfig;
    private readonly SensorLogService _logService;
    private readonly DateTime _startTime = DateTime.Now;

    [ObservableProperty] private int _selectedViewIndex;
    [ObservableProperty] private object _activeView;
    [ObservableProperty] private string _machineName = Environment.MachineName;
    [ObservableProperty] private string _statusMessage = "Monitoring active";
    [ObservableProperty] private string _uptimeText = "Starting...";

    public DashboardViewModel Dashboard { get; }
    public HardwareDetailViewModel HardwareDetail { get; }
    public ChartsViewModel Charts { get; }
    public SettingsViewModel Settings { get; }

    public MainViewModel(HardwareMonitorService service)
    {
        _service = service;
        _sensorConfig = new SensorConfigService();
        _logService = new SensorLogService(service);
        Dashboard = new DashboardViewModel(service);
        HardwareDetail = new HardwareDetailViewModel(service, _sensorConfig);
        Charts = new ChartsViewModel(service);
        Settings = new SettingsViewModel(service, _sensorConfig, _logService);
        _activeView = Dashboard;

        _service.Updated += OnServiceUpdated;
        Settings.SensorConfigSaved += () => HardwareDetail.RebuildCategories(_service);

        try
        {
            _service.Start();
            HardwareDetail.RebuildCategories(_service);
            Settings.RebuildHardwareTree();
            Dashboard.Update();
            UpdateUptime();
        }
        catch (Exception ex)
        {
            _statusMessage = $"Error: {ex.Message}. Try running as Administrator.";
            Debug.WriteLine($"Failed to start monitoring: {ex}");
        }
    }

    private void OnServiceUpdated()
    {
        Dispatcher.UIThread.Post(() =>
        {
            Charts.Update();
            _logService.OnUpdate();

            switch (SelectedViewIndex)
            {
                case 0:
                    Dashboard.Update();
                    break;
                case 1:
                    HardwareDetail.Update();
                    break;
            }

            UpdateUptime();
        });
    }

    private void UpdateUptime()
    {
        TimeSpan elapsed = DateTime.Now - _startTime;
        UptimeText = elapsed.TotalHours >= 1
            ? $"Monitoring for {elapsed.Hours}h {elapsed.Minutes}m"
            : $"Monitoring for {elapsed.Minutes}m {elapsed.Seconds}s";
    }

    [RelayCommand]
    private void NavigateTo(string view)
    {
        SelectedViewIndex = view switch
        {
            "Dashboard" => 0,
            "Hardware" => 1,
            "Charts" => 2,
            "Settings" => 3,
            _ => 0
        };

        ActiveView = SelectedViewIndex switch
        {
            0 => Dashboard,
            1 => (object)HardwareDetail,
            2 => Charts,
            3 => Settings,
            _ => Dashboard
        };

        if (SelectedViewIndex == 3)
            Settings.Gadget?.RebuildAvailableSensors();
    }

    [RelayCommand]
    private async Task ShowAbout()
    {
        var mainWindow = TopLevelHelper.GetMainWindow();
        if (mainWindow is Window owner)
            await new Views.AboutWindow().ShowDialog(owner);
    }
}
