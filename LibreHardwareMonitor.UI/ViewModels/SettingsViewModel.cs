using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.UI.Converters;
using LibreHardwareMonitor.UI.Services;

namespace LibreHardwareMonitor.UI.ViewModels;

internal static class TopLevelHelper
{
    public static TopLevel? GetMainWindow() =>
        (Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;
}

/// <summary>A sensor entry in the settings sensor management tree.</summary>
public partial class ConfigSensorViewModel : ObservableObject
{
    private readonly SensorConfigService _config;

    [ObservableProperty] private string _name;
    [ObservableProperty] private string _customName;
    [ObservableProperty] private bool _isVisible;
    [ObservableProperty] private bool _isEditing;

    public string Identifier { get; }
    public string OriginalName { get; }
    public string Unit { get; }

    public ConfigSensorViewModel(ISensor sensor, SensorConfigService config)
    {
        _config = config;
        Identifier = sensor.Identifier.ToString();
        OriginalName = sensor.Name;
        Unit = SensorUnitHelper.GetUnit(sensor.SensorType);
        string? custom = config.GetCustomName(Identifier);
        _name = custom ?? sensor.Name;
        _customName = custom ?? "";
        _isVisible = config.IsSensorVisible(Identifier);
    }

    partial void OnIsVisibleChanged(bool value)
    {
        _config.SetSensorVisible(Identifier, value);
    }

    [RelayCommand]
    private void StartEditing()
    {
        CustomName = _config.GetCustomName(Identifier) ?? OriginalName;
        IsEditing = true;
    }

    [RelayCommand]
    private void ConfirmRename()
    {
        IsEditing = false;
        string newName = string.IsNullOrWhiteSpace(CustomName) || CustomName == OriginalName
            ? OriginalName
            : CustomName.Trim();

        if (newName == OriginalName)
            _config.SetCustomName(Identifier, null);
        else
            _config.SetCustomName(Identifier, newName);

        Name = newName;
    }

    [RelayCommand]
    private void CancelEditing()
    {
        IsEditing = false;
        CustomName = _config.GetCustomName(Identifier) ?? "";
    }

    [RelayCommand]
    private void ResetName()
    {
        _config.SetCustomName(Identifier, null);
        Name = OriginalName;
        CustomName = "";
        IsEditing = false;
    }
}

/// <summary>A sensor group in the settings sensor management tree.</summary>
public partial class ConfigSensorGroupViewModel : ObservableObject
{
    private readonly SensorConfigService _config;
    private readonly string _hardwareId;

    [ObservableProperty] private string _groupName;
    [ObservableProperty] private string _groupIcon;
    [ObservableProperty] private bool _isVisible;
    [ObservableProperty] private bool _isExpanded;

    public string GroupKey { get; }
    public string ExpandIcon => IsExpanded ? "\u25BC" : "\u25B6";
    public ObservableCollection<ConfigSensorViewModel> Sensors { get; }

    public ConfigSensorGroupViewModel(
        SensorType type, string hardwareId,
        IEnumerable<ConfigSensorViewModel> sensors,
        SensorConfigService config)
    {
        _config = config;
        _hardwareId = hardwareId;
        GroupKey = type.ToString();
        _groupName = SensorGroupViewModel.GetGroupNameStatic(type);
        _groupIcon = SensorGroupViewModel.GetGroupIconStatic(type);
        _isVisible = config.IsGroupVisible(hardwareId, GroupKey);
        _isExpanded = false;
        Sensors = new ObservableCollection<ConfigSensorViewModel>(sensors);
    }

    partial void OnIsVisibleChanged(bool value)
    {
        _config.SetGroupVisible(_hardwareId, GroupKey, value);
        foreach (var s in Sensors)
            s.IsVisible = value;
    }

    [RelayCommand]
    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
        OnPropertyChanged(nameof(ExpandIcon));
    }
}

/// <summary>A hardware device in the settings sensor management tree.</summary>
public partial class ConfigHardwareViewModel : ObservableObject
{
    private readonly IHardware _hardware;
    private readonly SensorConfigService _config;

    [ObservableProperty] private string _name;
    [ObservableProperty] private string _iconGlyph;
    [ObservableProperty] private bool _isExpanded;
    [ObservableProperty] private string _exportStatus = "";

    public string ExpandIcon => IsExpanded ? "\u25BC" : "\u25B6";
    public ObservableCollection<ConfigSensorGroupViewModel> SensorGroups { get; }

    public ConfigHardwareViewModel(IHardware hardware, string icon,
        IEnumerable<ConfigSensorGroupViewModel> groups, SensorConfigService config)
    {
        _hardware = hardware;
        _config = config;
        _name = hardware.Name;
        _iconGlyph = icon;
        _isExpanded = false;
        SensorGroups = new ObservableCollection<ConfigSensorGroupViewModel>(groups);
    }

    [RelayCommand]
    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
        OnPropertyChanged(nameof(ExpandIcon));
    }

    [RelayCommand]
    private async Task ExportDeviceNames()
    {
        try
        {
            var topLevel = TopLevelHelper.GetMainWindow();
            if (topLevel == null)
            {
                ExportStatus = "Cannot open file dialog.";
                return;
            }

            string safeName = _hardware.Name.Replace(" ", "-").Replace("/", "-");
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = $"Export Sensor Names \u2014 {_hardware.Name}",
                DefaultExtension = "json",
                SuggestedFileName = $"sensor-names-{safeName}",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("JSON files") { Patterns = new[] { "*.json" } }
                }
            });

            if (file == null) return;

            string json = _config.ExportDeviceNamingProfile(_hardware);
            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(json);

            ExportStatus = $"Exported to {file.Name}";
        }
        catch (System.Exception ex)
        {
            ExportStatus = $"Export failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ImportDeviceNames()
    {
        try
        {
            var topLevel = TopLevelHelper.GetMainWindow();
            if (topLevel == null)
            {
                ExportStatus = "Cannot open file dialog.";
                return;
            }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = $"Import Sensor Names for {_hardware.Name}",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("JSON files") { Patterns = new[] { "*.json" } }
                }
            });

            if (files.Count == 0) return;

            await using var stream = await files[0].OpenReadAsync();
            using var reader = new StreamReader(stream);
            string json = await reader.ReadToEndAsync();

            int applied = _config.ImportNamingProfile(json);
            _config.Save();

            foreach (var group in SensorGroups)
            {
                foreach (var sensor in group.Sensors)
                {
                    string? custom = _config.GetCustomName(sensor.Identifier);
                    sensor.Name = custom ?? sensor.OriginalName;
                }
            }

            ExportStatus = $"Imported {applied} name(s) from {files[0].Name}";
        }
        catch (System.Exception ex)
        {
            ExportStatus = $"Import failed: {ex.Message}";
        }
    }
}

public partial class SettingsViewModel : ObservableObject
{
    private readonly HardwareMonitorService _service;
    private readonly SensorConfigService _sensorConfig;
    private readonly SensorLogService _logService;

    /// <summary>Raised after sensor config is saved so other views can refresh.</summary>
    public event System.Action? SensorConfigSaved;

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
    [ObservableProperty] private bool _useFahrenheit;
    [ObservableProperty] private bool _minimizeToTray;
    [ObservableProperty] private bool _isAutoStartEnabled;
    [ObservableProperty] private bool _isLogging;
    [ObservableProperty] private int _logInterval = 5;
    [ObservableProperty] private string _logFilePath = "";

    public ObservableCollection<ConfigHardwareViewModel> HardwareTree { get; } = new();

    public GadgetViewModel? Gadget { get; set; }

    public SettingsViewModel(HardwareMonitorService service, SensorConfigService sensorConfig, SensorLogService logService)
    {
        _service = service;
        _sensorConfig = sensorConfig;
        _logService = logService;
        _logFilePath = logService.LogFilePath;
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
        _useFahrenheit = sensorConfig.UseFahrenheit;
        _minimizeToTray = sensorConfig.MinimizeToTray;
        Converters.SensorUnitHelper.UseFahrenheit = _useFahrenheit;
        _isAutoStartEnabled = StartupManager.IsEnabled();
    }

    partial void OnUpdateIntervalChanged(double value)
    {
        _service.UpdateIntervalMs = value * 1000.0;
    }

    partial void OnUseFahrenheitChanged(bool value)
    {
        _sensorConfig.UseFahrenheit = value;
        Converters.SensorUnitHelper.UseFahrenheit = value;
    }

    partial void OnMinimizeToTrayChanged(bool value)
    {
        _sensorConfig.MinimizeToTray = value;
    }

    partial void OnIsAutoStartEnabledChanged(bool value)
    {
        StartupManager.SetEnabled(value);
    }

    [RelayCommand]
    private void ToggleLogging()
    {
        if (IsLogging)
        {
            _logService.Stop();
            IsLogging = false;
            StatusText = "Logging stopped.";
        }
        else
        {
            _logService.LogIntervalSeconds = LogInterval;
            _logService.LogFilePath = LogFilePath;
            _logService.Start();
            IsLogging = _logService.IsLogging;
            StatusText = IsLogging
                ? $"Logging to {LogFilePath} every {LogInterval}s."
                : "Failed to start logging.";
        }
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

    [RelayCommand]
    private void SaveSensorConfig()
    {
        _sensorConfig.Save();
        SensorConfigSaved?.Invoke();
        StatusText = "Sensor configuration saved.";
    }

    [RelayCommand]
    private async Task ImportNamingProfile()
    {
        var topLevel = TopLevelHelper.GetMainWindow();
        if (topLevel == null)
        {
            StatusText = "Cannot open file dialog.";
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import Sensor Naming Profile",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("JSON files") { Patterns = new[] { "*.json" } }
            }
        });

        if (files.Count == 0) return;

        await using var stream = await files[0].OpenReadAsync();
        using var reader = new StreamReader(stream);
        string json = await reader.ReadToEndAsync();

        int applied = _sensorConfig.ImportNamingProfile(json);
        _sensorConfig.Save();

        RebuildHardwareTree();
        StatusText = $"Imported {applied} sensor name(s) from {files[0].Name}.";
    }

    /// <summary>Build the hardware tree for sensor management. Call after hardware is loaded.</summary>
    public void RebuildHardwareTree()
    {
        HardwareTree.Clear();

        foreach (IHardware hw in _service.Computer.Hardware)
        {
            if (NetworkAdapterFilter.IsVirtualAdapter(hw))
                continue;

            AddHardwareNode(hw);

            foreach (IHardware sub in hw.SubHardware)
            {
                if (NetworkAdapterFilter.IsVirtualAdapter(sub))
                    continue;
                AddHardwareNode(sub);
            }
        }
    }

    private void AddHardwareNode(IHardware hw)
    {
        if (hw.Sensors.Length == 0)
            return;

        string hwId = hw.Identifier.ToString();
        var grouped = hw.Sensors
            .GroupBy(s => s.SensorType)
            .ToDictionary(g => g.Key, g => g.ToList());

        var groups = new List<ConfigSensorGroupViewModel>();

        foreach (SensorType type in HardwareViewModel.GroupOrder)
        {
            if (grouped.TryGetValue(type, out var sensors))
            {
                var sensorVMs = sensors.Select(s => new ConfigSensorViewModel(s, _sensorConfig));
                groups.Add(new ConfigSensorGroupViewModel(type, hwId, sensorVMs, _sensorConfig));
                grouped.Remove(type);
            }
        }

        foreach (var kvp in grouped)
        {
            var sensorVMs = kvp.Value.Select(s => new ConfigSensorViewModel(s, _sensorConfig));
            groups.Add(new ConfigSensorGroupViewModel(kvp.Key, hwId, sensorVMs, _sensorConfig));
        }

        string icon = HardwareViewModel.GetIconStatic(hw.HardwareType);
        HardwareTree.Add(new ConfigHardwareViewModel(hw, icon, groups, _sensorConfig));
    }
}
