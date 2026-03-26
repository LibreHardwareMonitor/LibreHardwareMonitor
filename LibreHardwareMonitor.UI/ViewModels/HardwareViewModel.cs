using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LibreHardwareMonitor.Hardware;

namespace LibreHardwareMonitor.UI.ViewModels;

public partial class HardwareViewModel : ObservableObject
{
    private readonly IHardware _hardware;

    [ObservableProperty] private string _name;
    [ObservableProperty] private HardwareType _hardwareType;
    [ObservableProperty] private string _iconGlyph;

    public ObservableCollection<SensorViewModel> Sensors { get; } = new();
    public ObservableCollection<SensorGroupViewModel> SensorGroups { get; } = new();
    public ObservableCollection<HardwareViewModel> SubHardware { get; } = new();

    public IHardware Hardware => _hardware;
    public string Identifier => _hardware.Identifier.ToString();

    // Quick-access grouped sensors
    public IEnumerable<SensorViewModel> Temperatures => Sensors.Where(s => s.SensorType == SensorType.Temperature);
    public IEnumerable<SensorViewModel> Loads => Sensors.Where(s => s.SensorType == SensorType.Load);
    public IEnumerable<SensorViewModel> Clocks => Sensors.Where(s => s.SensorType == SensorType.Clock);
    public IEnumerable<SensorViewModel> Voltages => Sensors.Where(s => s.SensorType == SensorType.Voltage);
    public IEnumerable<SensorViewModel> Fans => Sensors.Where(s => s.SensorType == SensorType.Fan);
    public IEnumerable<SensorViewModel> Powers => Sensors.Where(s => s.SensorType == SensorType.Power);

    private IEnumerable<SensorViewModel> RealTemperatures => Temperatures.Where(t => !t.IsReferenceSensor);
    public float? AverageTemperature => RealTemperatures.Any() ? RealTemperatures.Average(t => t.Value) : null;
    public float? MaxTemperature => RealTemperatures.Any() ? RealTemperatures.Max(t => t.Value) : null;
    public float? TotalLoad => Loads.FirstOrDefault(l => l.Name.Contains("Total"))?.Value;
    public float? TotalPower => Powers.Any() ? Powers.Where(p => p.Name.Contains("Package") || p.Name.Contains("Total")).Select(p => p.Value).FirstOrDefault() : null;
    public int SensorCount => Sensors.Count;

    // Display order for sensor groups
    private static readonly SensorType[] GroupOrder =
    {
        SensorType.Temperature,
        SensorType.Load,
        SensorType.Clock,
        SensorType.Frequency,
        SensorType.Power,
        SensorType.Voltage,
        SensorType.Current,
        SensorType.Fan,
        SensorType.Control,
        SensorType.SmallData,
        SensorType.Data,
        SensorType.Throughput,
        SensorType.Level,
        SensorType.Flow,
        SensorType.Energy,
        SensorType.Noise,
        SensorType.Humidity,
    };

    public HardwareViewModel(IHardware hardware)
    {
        _hardware = hardware;
        _name = hardware.Name;
        _hardwareType = hardware.HardwareType;
        _iconGlyph = GetIcon(hardware.HardwareType);

        foreach (ISensor sensor in hardware.Sensors)
            Sensors.Add(new SensorViewModel(sensor));

        foreach (IHardware sub in hardware.SubHardware)
            SubHardware.Add(new HardwareViewModel(sub));

        RebuildSensorGroups();
    }

    private void RebuildSensorGroups()
    {
        // Preserve expand/collapse state across rebuilds
        var previousState = new Dictionary<SensorType, bool>();
        foreach (var g in SensorGroups)
            previousState[g.SensorType] = g.IsExpanded;

        SensorGroups.Clear();

        var grouped = Sensors.GroupBy(s => s.SensorType)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (SensorType type in GroupOrder)
        {
            if (grouped.TryGetValue(type, out var sensors))
            {
                var group = new SensorGroupViewModel(type, new ObservableCollection<SensorViewModel>(sensors));
                if (previousState.TryGetValue(type, out bool wasExpanded))
                    group.IsExpanded = wasExpanded;
                SensorGroups.Add(group);
                grouped.Remove(type);
            }
        }

        // Add any remaining types not in the predefined order
        foreach (var kvp in grouped)
        {
            var group = new SensorGroupViewModel(kvp.Key, new ObservableCollection<SensorViewModel>(kvp.Value));
            if (previousState.TryGetValue(kvp.Key, out bool wasExpanded))
                group.IsExpanded = wasExpanded;
            SensorGroups.Add(group);
        }
    }

    public void Update()
    {
        // Build a lookup for O(1) sensor matching
        var existingById = new Dictionary<string, SensorViewModel>(Sensors.Count);
        foreach (var s in Sensors)
            existingById[s.Identifier] = s;

        bool sensorsChanged = false;

        foreach (ISensor sensor in _hardware.Sensors)
        {
            string id = sensor.Identifier.ToString();
            if (existingById.TryGetValue(id, out var existing))
            {
                existing.Update();
                existingById.Remove(id);
            }
            else
            {
                Sensors.Add(new SensorViewModel(sensor));
                sensorsChanged = true;
            }
        }

        // Remove sensors no longer present
        foreach (var removed in existingById.Values)
        {
            Sensors.Remove(removed);
            sensorsChanged = true;
        }

        // Rebuild groups if sensors were added/removed
        if (sensorsChanged)
            RebuildSensorGroups();
        else
        {
            // Update sensors within existing groups
            foreach (var group in SensorGroups)
                foreach (var s in group.Sensors)
                    s.Update();
        }

        foreach (var sub in SubHardware)
            sub.Update();

        OnPropertyChanged(nameof(AverageTemperature));
        OnPropertyChanged(nameof(MaxTemperature));
        OnPropertyChanged(nameof(TotalLoad));
        OnPropertyChanged(nameof(TotalPower));
        OnPropertyChanged(nameof(SensorCount));
    }

    private static string GetIcon(HardwareType type)
    {
        return type switch
        {
            HardwareType.Cpu => "CPU",
            HardwareType.GpuNvidia => "GPU",
            HardwareType.GpuAmd => "GPU",
            HardwareType.GpuIntel => "GPU",
            HardwareType.Memory => "RAM",
            HardwareType.Motherboard => "MB",
            HardwareType.SuperIO => "IO",
            HardwareType.Storage => "SSD",
            HardwareType.Network => "NET",
            HardwareType.Battery => "BAT",
            HardwareType.Psu => "PSU",
            HardwareType.Cooler => "FAN",
            HardwareType.EmbeddedController => "EC",
            _ => "HW"
        };
    }
}
