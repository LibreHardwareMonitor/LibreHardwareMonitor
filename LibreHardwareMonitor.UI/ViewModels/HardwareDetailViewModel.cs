using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.UI.Services;

namespace LibreHardwareMonitor.UI.ViewModels;

public partial class HardwareDetailViewModel : ObservableObject
{
    private readonly HardwareMonitorService _service;
    private readonly SensorConfigService _sensorConfig;

    public ObservableCollection<HardwareCategoryViewModel> HardwareCategories { get; } = new();

    public HardwareDetailViewModel(HardwareMonitorService service, SensorConfigService sensorConfig)
    {
        _service = service;
        _sensorConfig = sensorConfig;
    }

    public void Update()
    {
        foreach (var cat in HardwareCategories)
        {
            if (!cat.IsExpanded) continue;
            foreach (var hw in cat.Items)
                hw.Update();
        }
    }

    public void RebuildCategories(HardwareMonitorService service)
    {
        HardwareCategories.Clear();

        var hardware = service.Computer.Hardware.ToList();
        var categoryOrder = new (string Name, string Icon, HardwareType[] Types)[]
        {
            ("Processors", "\u2699", new[] { HardwareType.Cpu }),
            ("Graphics", "\u25A0", new[] { HardwareType.GpuNvidia, HardwareType.GpuAmd, HardwareType.GpuIntel }),
            ("Memory", "\u2593", new[] { HardwareType.Memory }),
            ("Motherboard", "\u2338", new[] { HardwareType.Motherboard, HardwareType.SuperIO }),
            ("Storage", "\u25A8", new[] { HardwareType.Storage }),
            ("Network", "\u2301", new[] { HardwareType.Network }),
            ("Power", "\u26A1", new[] { HardwareType.Psu, HardwareType.Battery }),
            ("Cooling", "\u2744", new[] { HardwareType.Cooler }),
            ("Controllers", "\u2318", new[] { HardwareType.EmbeddedController }),
        };

        foreach (var (name, icon, types) in categoryOrder)
        {
            var items = new List<HardwareViewModel>();

            foreach (var h in hardware)
            {
                if (types.Contains(h.HardwareType) && !Services.NetworkAdapterFilter.IsVirtualAdapter(h))
                    items.Add(new HardwareViewModel(h, _sensorConfig));

                foreach (IHardware sub in h.SubHardware)
                {
                    if (types.Contains(sub.HardwareType) && !Services.NetworkAdapterFilter.IsVirtualAdapter(sub))
                        items.Add(new HardwareViewModel(sub, _sensorConfig));
                }
            }

            if (items.Count > 0)
                HardwareCategories.Add(new HardwareCategoryViewModel(name, icon, items));
        }
    }
}

public partial class HardwareCategoryViewModel : ObservableObject
{
    [ObservableProperty] private string _categoryName;
    [ObservableProperty] private string _categoryIcon;
    [ObservableProperty] private bool _isExpanded;

    public string ExpandIcon => IsExpanded ? "\u25BC" : "\u25B6";

    public ObservableCollection<HardwareViewModel> Items { get; }

    public HardwareCategoryViewModel(string name, string icon, IEnumerable<HardwareViewModel> items)
    {
        _categoryName = name;
        _categoryIcon = icon;
        _isExpanded = true;
        Items = new ObservableCollection<HardwareViewModel>(items);
    }

    [RelayCommand]
    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
        OnPropertyChanged(nameof(ExpandIcon));
    }
}

public partial class SensorGroupViewModel : ObservableObject
{
    [ObservableProperty] private SensorType _sensorType;
    [ObservableProperty] private string _groupName;
    [ObservableProperty] private string _groupIcon;
    [ObservableProperty] private string _accentColor;
    [ObservableProperty] private bool _isExpanded = true;

    public ObservableCollection<SensorViewModel> Sensors { get; }

    public string ExpandIcon => IsExpanded ? "\u25BC" : "\u25B6";

    public SensorGroupViewModel(SensorType type, ObservableCollection<SensorViewModel> sensors)
    {
        _sensorType = type;
        _groupName = GetGroupNameStatic(type);
        _groupIcon = GetGroupIconStatic(type);
        _accentColor = GetGroupColor(type);
        Sensors = sensors;
    }

    [RelayCommand]
    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
        OnPropertyChanged(nameof(ExpandIcon));
    }

    public static string GetGroupNameStatic(SensorType type)
    {
        return type switch
        {
            SensorType.Voltage => "Voltages",
            SensorType.Current => "Currents",
            SensorType.Power => "Power",
            SensorType.Clock => "Clocks",
            SensorType.Temperature => "Temperatures",
            SensorType.Load => "Utilization",
            SensorType.Frequency => "Frequencies",
            SensorType.Fan => "Fans",
            SensorType.Flow => "Flow Rates",
            SensorType.Control => "Fan Controls",
            SensorType.Level => "Levels",
            SensorType.Data => "Data",
            SensorType.SmallData => "Memory",
            SensorType.Throughput => "Throughput",
            SensorType.Energy => "Energy",
            SensorType.Noise => "Noise",
            SensorType.Humidity => "Humidity",
            _ => type.ToString()
        };
    }

    public static string GetGroupIconStatic(SensorType type)
    {
        return type switch
        {
            SensorType.Temperature => "\u2103",
            SensorType.Clock => "\u23F1",
            SensorType.Voltage => "\u26A1",
            SensorType.Current => "\u2301",
            SensorType.Power => "\u2622",
            SensorType.Load => "\u25A3",
            SensorType.Fan => "\u2744",
            SensorType.Flow => "\u2248",
            SensorType.Control => "\u2699",
            SensorType.Level => "\u2584",
            SensorType.Data => "\u25A8",
            SensorType.SmallData => "\u25A6",
            SensorType.Throughput => "\u21C5",
            SensorType.Frequency => "\u223F",
            SensorType.Energy => "\u23E9",
            SensorType.Noise => "\u266A",
            SensorType.Humidity => "\u2602",
            _ => "\u2022"
        };
    }

    private static string GetGroupColor(SensorType type)
    {
        return type switch
        {
            SensorType.Temperature => "#EF4444",
            SensorType.Clock => "#3B82F6",
            SensorType.Voltage => "#F59E0B",
            SensorType.Current => "#F97316",
            SensorType.Power => "#F59E0B",
            SensorType.Load => "#10B981",
            SensorType.Fan => "#22D3EE",
            SensorType.Control => "#8B949E",
            SensorType.Level => "#A78BFA",
            SensorType.Data => "#6EE7B7",
            SensorType.SmallData => "#A78BFA",
            SensorType.Throughput => "#22D3EE",
            SensorType.Frequency => "#3B82F6",
            SensorType.Flow => "#22D3EE",
            SensorType.Energy => "#FBBF24",
            SensorType.Noise => "#EC4899",
            SensorType.Humidity => "#22D3EE",
            _ => "#8B949E"
        };
    }
}
