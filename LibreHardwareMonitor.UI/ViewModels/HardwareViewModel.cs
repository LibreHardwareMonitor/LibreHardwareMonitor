using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LibreHardwareMonitor.Hardware;

namespace LibreHardwareMonitor.UI.ViewModels;

public partial class HardwareViewModel : ObservableObject
{
    private readonly IHardware _hardware;
    private readonly Services.SensorConfigService? _sensorConfig;

    [ObservableProperty] private string _name;
    [ObservableProperty] private HardwareType _hardwareType;
    [ObservableProperty] private string _iconGlyph;

    public ObservableCollection<SensorViewModel> Sensors { get; } = new();
    public ObservableCollection<SensorGroupViewModel> SensorGroups { get; } = new();
    public ObservableCollection<HardwareViewModel> SubHardware { get; } = new();

    private IEnumerable<SensorViewModel> ByType(SensorType t) => Sensors.Where(s => s.SensorType == t);
    private IEnumerable<SensorViewModel> RealTemperatures => ByType(SensorType.Temperature).Where(t => !t.IsReferenceSensor);
    public float? MaxTemperature => RealTemperatures.Any() ? RealTemperatures.Max(t => t.Value) : null;
    public float? TotalLoad => ByType(SensorType.Load).FirstOrDefault(l => l.Name.Contains("Total"))?.Value;
    public float? TotalPower => ByType(SensorType.Power).Where(p => p.Name.Contains("Package") || p.Name.Contains("Total")).Select(p => (float?)p.Value).FirstOrDefault();
    public int SensorCount => Sensors.Count;

    /// <summary>Shared display order for sensor type groups.</summary>
    public static readonly SensorType[] GroupOrder =
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

    public HardwareViewModel(IHardware hardware, Services.SensorConfigService? sensorConfig = null)
    {
        _hardware = hardware;
        _sensorConfig = sensorConfig;
        _name = hardware.Name;
        _hardwareType = hardware.HardwareType;
        _iconGlyph = GetIconStatic(hardware.HardwareType);

        foreach (ISensor sensor in hardware.Sensors)
        {
            if (_sensorConfig != null && !_sensorConfig.IsSensorVisible(sensor.Identifier.ToString()))
                continue;

            var vm = new SensorViewModel(sensor);
            string? customName = _sensorConfig?.GetCustomName(sensor.Identifier.ToString());
            if (customName != null)
                vm.Name = customName;

            Sensors.Add(vm);
        }

        foreach (IHardware sub in hardware.SubHardware)
            SubHardware.Add(new HardwareViewModel(sub, sensorConfig));

        RebuildSensorGroups();
    }

    private void RebuildSensorGroups()
    {
        var grouped = Sensors.GroupBy(s => s.SensorType)
            .ToDictionary(g => g.Key, g => g.ToList());

        var desiredTypes = new List<SensorType>();
        foreach (SensorType type in GroupOrder)
        {
            if (grouped.ContainsKey(type))
                desiredTypes.Add(type);
        }
        foreach (var key in grouped.Keys)
        {
            if (!desiredTypes.Contains(key))
                desiredTypes.Add(key);
        }

        var existingGroups = new Dictionary<SensorType, SensorGroupViewModel>();
        foreach (var g in SensorGroups)
            existingGroups[g.SensorType] = g;

        // Remove groups whose type no longer has sensors
        for (int i = SensorGroups.Count - 1; i >= 0; i--)
        {
            if (!grouped.ContainsKey(SensorGroups[i].SensorType))
                SensorGroups.RemoveAt(i);
        }

        for (int i = 0; i < desiredTypes.Count; i++)
        {
            SensorType type = desiredTypes[i];
            if (existingGroups.TryGetValue(type, out var existing))
            {
                SyncSensorCollection(existing.Sensors, grouped[type]);

                int currentIdx = SensorGroups.IndexOf(existing);
                if (currentIdx != i)
                    SensorGroups.Move(currentIdx, i);
            }
            else
            {
                var newGroup = new SensorGroupViewModel(type,
                    new ObservableCollection<SensorViewModel>(grouped[type]));
                SensorGroups.Insert(i, newGroup);
            }
        }
    }

    private static void SyncSensorCollection(
        ObservableCollection<SensorViewModel> target,
        List<SensorViewModel> desired)
    {
        var desiredById = new HashSet<string>(desired.Select(s => s.Identifier));
        var existingIds = new HashSet<string>(target.Select(s => s.Identifier));

        for (int i = target.Count - 1; i >= 0; i--)
        {
            if (!desiredById.Contains(target[i].Identifier))
                target.RemoveAt(i);
        }

        foreach (var s in desired)
        {
            if (!existingIds.Contains(s.Identifier))
                target.Add(s);
        }
    }

    public void Update()
    {
        var existingById = new Dictionary<string, SensorViewModel>(Sensors.Count);
        foreach (var s in Sensors)
            existingById[s.Identifier] = s;

        bool sensorsChanged = false;

        foreach (ISensor sensor in _hardware.Sensors)
        {
            string id = sensor.Identifier.ToString();

            if (_sensorConfig != null && !_sensorConfig.IsSensorVisible(id))
            {
                if (existingById.TryGetValue(id, out var hidden))
                {
                    Sensors.Remove(hidden);
                    existingById.Remove(id);
                    sensorsChanged = true;
                }
                continue;
            }

            if (existingById.TryGetValue(id, out var existing))
            {
                existing.Update();
                existingById.Remove(id);
            }
            else
            {
                var vm = new SensorViewModel(sensor);
                string? customName = _sensorConfig?.GetCustomName(id);
                if (customName != null)
                    vm.Name = customName;
                Sensors.Add(vm);
                sensorsChanged = true;
            }
        }

        foreach (var removed in existingById.Values)
        {
            Sensors.Remove(removed);
            sensorsChanged = true;
        }

        if (sensorsChanged)
            RebuildSensorGroups();

        foreach (var sub in SubHardware)
            sub.Update();

        OnPropertyChanged(nameof(MaxTemperature));
        OnPropertyChanged(nameof(TotalLoad));
        OnPropertyChanged(nameof(TotalPower));
        OnPropertyChanged(nameof(SensorCount));
    }

    public static string GetIconStatic(HardwareType type)
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
