using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibreHardwareMonitor.UI.Services;

namespace LibreHardwareMonitor.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly HardwareMonitorService _service;

    [ObservableProperty] private double _updateInterval;
    [ObservableProperty] private bool _isCpuEnabled;
    [ObservableProperty] private bool _isGpuEnabled;
    [ObservableProperty] private bool _isMemoryEnabled;
    [ObservableProperty] private bool _isMotherboardEnabled;
    [ObservableProperty] private bool _isStorageEnabled;
    [ObservableProperty] private bool _isNetworkEnabled;
    [ObservableProperty] private bool _isBatteryEnabled;
    [ObservableProperty] private bool _isPsuEnabled;
    [ObservableProperty] private bool _isControllerEnabled;
    [ObservableProperty] private string _statusText = "Monitoring active";

    public SettingsViewModel(HardwareMonitorService service)
    {
        _service = service;
        _updateInterval = service.UpdateIntervalMs / 1000.0;
        _isCpuEnabled = service.Computer.IsCpuEnabled;
        _isGpuEnabled = service.Computer.IsGpuEnabled;
        _isMemoryEnabled = service.Computer.IsMemoryEnabled;
        _isMotherboardEnabled = service.Computer.IsMotherboardEnabled;
        _isStorageEnabled = service.Computer.IsStorageEnabled;
        _isNetworkEnabled = service.Computer.IsNetworkEnabled;
        _isBatteryEnabled = service.Computer.IsBatteryEnabled;
        _isPsuEnabled = service.Computer.IsPsuEnabled;
        _isControllerEnabled = service.Computer.IsControllerEnabled;
    }

    partial void OnUpdateIntervalChanged(double value)
    {
        _service.UpdateIntervalMs = value * 1000.0;
    }

    [RelayCommand]
    private void ApplyHardwareSettings()
    {
        _service.Stop();
        _service.Computer.IsCpuEnabled = IsCpuEnabled;
        _service.Computer.IsGpuEnabled = IsGpuEnabled;
        _service.Computer.IsMemoryEnabled = IsMemoryEnabled;
        _service.Computer.IsMotherboardEnabled = IsMotherboardEnabled;
        _service.Computer.IsStorageEnabled = IsStorageEnabled;
        _service.Computer.IsNetworkEnabled = IsNetworkEnabled;
        _service.Computer.IsBatteryEnabled = IsBatteryEnabled;
        _service.Computer.IsPsuEnabled = IsPsuEnabled;
        _service.Computer.IsControllerEnabled = IsControllerEnabled;
        _service.Start();
        StatusText = "Settings applied. Hardware reloaded.";
    }
}
