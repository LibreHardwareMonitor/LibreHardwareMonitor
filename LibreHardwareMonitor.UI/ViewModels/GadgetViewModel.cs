using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.UI.Services;

namespace LibreHardwareMonitor.UI.ViewModels;

public partial class GadgetItemViewModel : ObservableObject
{
    [ObservableProperty] private string _label;
    [ObservableProperty] private string _value = "--";
    [ObservableProperty] private float _percent;
    [ObservableProperty] private string _accentColor;
    [ObservableProperty] private string _unit;

    public string SensorId { get; }

    public GadgetItemViewModel(string sensorId, string label, string unit, string accentColor)
    {
        SensorId = sensorId;
        _label = label;
        _accentColor = accentColor;
        _unit = unit;
    }
}

public partial class GadgetViewModel : ObservableObject
{
    private readonly HardwareMonitorService _service;
    private readonly StringBuilder _tooltipBuilder = new();
    private bool _loading;

    [ObservableProperty] private bool _isVisible;
    [ObservableProperty] private string _tooltipText = "Libre Hardware Monitor";
    [ObservableProperty] private double _opacity = 0.7;
    [ObservableProperty] private int _fontSize = 13;
    [ObservableProperty] private bool _isPositionLocked;

    public ObservableCollection<GadgetItemViewModel> Items { get; } = new();
    public ObservableCollection<GadgetSensorOption> AvailableSensors { get; } = new();

    private static readonly string[] AccentColors =
    {
        "#3B82F6", "#10B981", "#A78BFA", "#F59E0B", "#EF4444",
        "#22D3EE", "#F97316", "#EC4899", "#6EE7B7", "#FBBF24"
    };

    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LibreHardwareMonitor.UI", "gadget-config.json");

    public GadgetViewModel(HardwareMonitorService service)
    {
        _service = service;
        _service.Updated += OnServiceUpdated;
    }

    private void OnServiceUpdated()
    {
        if (Items.Count == 0) return;
        Dispatcher.UIThread.Post(Update);
    }

    public void RebuildAvailableSensors()
    {
        AvailableSensors.Clear();
        var selectedIds = new HashSet<string>(Items.Select(i => i.SensorId));

        foreach (IHardware hw in _service.Computer.Hardware)
        {
            AddSensorsFromHardware(hw, selectedIds);
            foreach (IHardware sub in hw.SubHardware)
                AddSensorsFromHardware(sub, selectedIds);
        }
    }

    private void AddSensorsFromHardware(IHardware hw, HashSet<string> selectedIds)
    {
        if (NetworkAdapterFilter.IsVirtualAdapter(hw)) return;

        foreach (ISensor sensor in hw.Sensors)
        {
            string id = sensor.Identifier.ToString();
            AvailableSensors.Add(new GadgetSensorOption(
                id,
                $"{hw.Name} \u2014 {sensor.Name}",
                Converters.SensorUnitHelper.GetUnit(sensor.SensorType),
                sensor.SensorType,
                selectedIds.Contains(id)));
        }
    }

    [RelayCommand]
    private void ApplySelection()
    {
        var selected = AvailableSensors.Where(s => s.IsSelected).ToList();
        Items.Clear();

        for (int i = 0; i < selected.Count; i++)
        {
            var opt = selected[i];
            string shortLabel = opt.Label;
            int dash = shortLabel.IndexOf('\u2014');
            if (dash > 0) shortLabel = shortLabel[(dash + 2)..];
            if (shortLabel.Length > 16) shortLabel = shortLabel[..16];

            Items.Add(new GadgetItemViewModel(
                opt.SensorId, shortLabel, opt.Unit,
                AccentColors[i % AccentColors.Length]));
        }

        SaveConfig();
    }

    [RelayCommand]
    private void AddDefaults()
    {
        Items.Clear();
        TryAddDefault(HardwareType.Cpu, SensorType.Load, "CPU Load", "%", 0, "Total");
        TryAddDefault(HardwareType.Cpu, SensorType.Temperature, "CPU Temp", "\u00B0C", 0, "Package", "Average");
        TryAddDefaultGpu(SensorType.Load, "GPU Load", "%", 1, "Core");
        TryAddDefaultGpu(SensorType.Temperature, "GPU Temp", "\u00B0C", 1, "Core");
        TryAddDefault(HardwareType.Memory, SensorType.Load, "RAM", "%", 2);
        SaveConfig();
    }

    private void TryAddDefault(HardwareType hwType, SensorType sType, string label, string unit, int colorIdx, params string[] nameHints)
    {
        var hw = _service.GetHardwareByType(hwType).FirstOrDefault();
        if (hw == null) return;

        var sensor = FindSensorIn(hw, sType, nameHints);
        if (sensor != null)
            Items.Add(new GadgetItemViewModel(sensor.Identifier.ToString(), label, unit, AccentColors[colorIdx]));
    }

    private void TryAddDefaultGpu(SensorType sType, string label, string unit, int colorIdx, params string[] nameHints)
    {
        var gpu = _service.Computer.Hardware
            .FirstOrDefault(h => h.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel);
        if (gpu == null) return;

        var sensor = FindSensorIn(gpu, sType, nameHints);
        if (sensor != null)
            Items.Add(new GadgetItemViewModel(sensor.Identifier.ToString(), label, unit, AccentColors[colorIdx]));
    }

    private static ISensor? FindSensorIn(IHardware hw, SensorType sType, string[] nameHints)
    {
        var sensors = hw.Sensors.Where(s => s.SensorType == sType);
        if (nameHints.Length > 0)
            sensors = sensors.Where(s => nameHints.Any(h => s.Name.Contains(h)));
        return sensors.FirstOrDefault();
    }

    private void Update()
    {
        if (Items.Count == 0) return;

        // Build lookup only for sensors we need
        var needed = new HashSet<string>(Items.Select(i => i.SensorId));
        var found = new Dictionary<string, ISensor>(needed.Count);

        foreach (ISensor s in _service.GetAllSensors())
        {
            string id = s.Identifier.ToString();
            if (needed.Contains(id))
            {
                found[id] = s;
                if (found.Count == needed.Count) break;
            }
        }

        _tooltipBuilder.Clear();

        foreach (var item in Items)
        {
            if (!found.TryGetValue(item.SensorId, out var sensor))
                continue;

            float val = sensor.Value ?? 0;
            item.Value = SensorViewModel.FormatSensorValue(val, sensor.SensorType);

            if (sensor.SensorType == SensorType.Temperature)
                item.Unit = Converters.SensorUnitHelper.TempUnit;

            item.Percent = sensor.SensorType is SensorType.Load or SensorType.Control or SensorType.Level or SensorType.Humidity
                ? val : 0;

            if (_tooltipBuilder.Length > 0) _tooltipBuilder.Append(" | ");
            _tooltipBuilder.Append(item.Label).Append(": ").Append(item.Value).Append(item.Unit);
        }

        TooltipText = _tooltipBuilder.Length > 0 ? _tooltipBuilder.ToString() : "Libre Hardware Monitor";
    }

    [RelayCommand]
    private void SetFontSize(string size)
    {
        if (int.TryParse(size, out int s))
            FontSize = s;
    }

    [RelayCommand]
    private void ToggleVisibility() => IsVisible = !IsVisible;

    [RelayCommand]
    private void RemoveItem(GadgetItemViewModel item)
    {
        Items.Remove(item);
        var match = AvailableSensors.FirstOrDefault(s => s.SensorId == item.SensorId);
        if (match != null) match.IsSelected = false;
        SaveConfig();
    }

    [RelayCommand]
    private void MoveItemUp(GadgetItemViewModel item)
    {
        int idx = Items.IndexOf(item);
        if (idx > 0)
        {
            Items.Move(idx, idx - 1);
            SaveConfig();
        }
    }

    [RelayCommand]
    private void MoveItemDown(GadgetItemViewModel item)
    {
        int idx = Items.IndexOf(item);
        if (idx >= 0 && idx < Items.Count - 1)
        {
            Items.Move(idx, idx + 1);
            SaveConfig();
        }
    }

    partial void OnOpacityChanged(double value) { if (!_loading) SaveConfig(); }
    partial void OnFontSizeChanged(int value) { if (!_loading) SaveConfig(); }
    partial void OnIsPositionLockedChanged(bool value) { if (!_loading) SaveConfig(); }

    private void SaveConfig()
    {
        try
        {
            var config = new GadgetConfig
            {
                Opacity = Opacity,
                FontSize = FontSize,
                IsPositionLocked = IsPositionLocked,
                Sensors = Items.Select(i => new GadgetSensorEntry
                {
                    SensorId = i.SensorId, Label = i.Label, Unit = i.Unit, Color = i.AccentColor
                }).ToList()
            };

            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    public void LoadConfig()
    {
        try
        {
            if (!File.Exists(ConfigPath)) return;
            var config = JsonSerializer.Deserialize<GadgetConfig>(File.ReadAllText(ConfigPath));
            if (config == null) return;

            _loading = true;
            Opacity = config.Opacity;
            FontSize = config.FontSize;
            IsPositionLocked = config.IsPositionLocked;
            _loading = false;

            if (config.Sensors.Count > 0)
            {
                Items.Clear();
                foreach (var e in config.Sensors)
                    Items.Add(new GadgetItemViewModel(e.SensorId, e.Label, e.Unit, e.Color));
            }
        }
        catch { }
    }

    private sealed class GadgetConfig
    {
        public double Opacity { get; set; } = 0.7;
        public int FontSize { get; set; } = 13;
        public bool IsPositionLocked { get; set; }
        public List<GadgetSensorEntry> Sensors { get; set; } = new();
    }

    private sealed class GadgetSensorEntry
    {
        public string SensorId { get; set; } = "";
        public string Label { get; set; } = "";
        public string Unit { get; set; } = "";
        public string Color { get; set; } = "#3B82F6";
    }
}

public partial class GadgetSensorOption : ObservableObject
{
    [ObservableProperty] private bool _isSelected;

    public string SensorId { get; }
    public string Label { get; }
    public string Unit { get; }
    public SensorType Type { get; }

    public GadgetSensorOption(string sensorId, string label, string unit, SensorType type, bool isSelected)
    {
        SensorId = sensorId;
        Label = label;
        Unit = unit;
        Type = type;
        _isSelected = isSelected;
    }
}
