using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.UI.Services;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace LibreHardwareMonitor.UI.ViewModels;

public partial class ChartSensorInfo : ObservableObject
{
    [ObservableProperty] private string _name;
    [ObservableProperty] private string _currentValue;
    [ObservableProperty] private SKColor _color;

    public ChartSensorInfo(string name, string currentValue, SKColor color)
    {
        _name = name;
        _currentValue = currentValue;
        _color = color;
    }
}

public partial class SelectableSensor : ObservableObject
{
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private string _displayName;

    public string Identifier { get; }

    public SelectableSensor(ISensor sensor, bool isSelected = false)
    {
        Identifier = sensor.Identifier.ToString();
        _displayName = $"{sensor.Hardware.Name} \u2014 {sensor.Name}";
        _isSelected = isSelected;
    }
}

public partial class ChartsViewModel : ObservableObject
{
    private readonly HardwareMonitorService _service;
    private readonly Dictionary<string, List<DateTimePoint>> _seriesData = new();
    private const int MaxSelectedSensors = 20;

    [ObservableProperty] private string _selectedCategory = "Temperatures";
    [ObservableProperty] private string _yAxisUnit = "\u00B0C";
    [ObservableProperty] private string _selectionInfo = "0/20 sensors selected";
    [ObservableProperty] private string _selectedTimeWindow = "2 min";
    [ObservableProperty] private float _strokeThickness = 2.5f;

    // skipcq: MVVMTK0034 — field read for computed property
    private int MaxPoints => _selectedTimeWindow switch
    {
        "2 min" => 120,
        "5 min" => 300,
        "15 min" => 900,
        "1 hour" => 3600,
        _ => 120
    };

    public string[] TimeWindows { get; } = { "2 min", "5 min", "15 min", "1 hour" };

    public ObservableCollection<ISeries> Series { get; } = new();
    public ObservableCollection<ChartSensorInfo> SensorLegend { get; } = new();
    public ObservableCollection<SelectableSensor> AvailableSensors { get; } = new();

    private readonly Dictionary<string, HashSet<string>> _selectedSensorsByCategory = new();
    private bool _suppressSelectionHandler;

    public Axis[] XAxes { get; } =
    {
        new DateTimeAxis(TimeSpan.FromSeconds(1), date => date.ToString("HH:mm:ss"))
        {
            Name = "Time",
            NamePaint = new SolidColorPaint(new SKColor(0x6E, 0x76, 0x81)),
            NameTextSize = 12,
            LabelsPaint = new SolidColorPaint(new SKColor(0x8B, 0x94, 0x9E)),
            TextSize = 11,
            SeparatorsPaint = new SolidColorPaint(new SKColor(0x1E, 0x27, 0x36)) { StrokeThickness = 1 },
            TicksPaint = new SolidColorPaint(new SKColor(0x2A, 0x31, 0x42)),
        }
    };

    public Axis[] YAxes { get; }

    public string[] Categories { get; } = { "Temperatures", "Loads", "Clocks", "Fans", "Power", "Voltages" };

    private static readonly SKColor[] ChartColors =
    {
        new(0x3B, 0x82, 0xF6),  // Blue
        new(0x10, 0xB9, 0x81),  // Green
        new(0xF5, 0x9E, 0x0B),  // Yellow
        new(0xEF, 0x44, 0x44),  // Red
        new(0xA7, 0x8B, 0xFA),  // Purple
        new(0x22, 0xD3, 0xEE),  // Cyan
        new(0xF9, 0x73, 0x16),  // Orange
        new(0xEC, 0x48, 0x99),  // Pink
        new(0x6E, 0xE7, 0xB7),  // Mint
        new(0xFB, 0xBF, 0x24),  // Amber
        new(0x94, 0xA3, 0xB8),  // Slate
        new(0xE8, 0x79, 0xF9),  // Fuchsia
        new(0x2D, 0xD4, 0xBF),  // Teal
        new(0xF4, 0x72, 0xB6),  // Rose
        new(0xA3, 0xE6, 0x35),  // Lime
        new(0xFD, 0x96, 0x44),  // Tangerine
        new(0x81, 0x8C, 0xF8),  // Indigo
        new(0x4A, 0xDE, 0x80),  // Emerald
        new(0xFB, 0x92, 0x3C),  // Apricot
        new(0x38, 0xBD, 0xF8),  // Sky
    };

    private readonly Axis _yAxis;

    public ChartsViewModel(HardwareMonitorService service)
    {
        _service = service;
        _yAxis = new Axis
        {
            Name = Converters.SensorUnitHelper.TempUnit,
            NamePaint = new SolidColorPaint(new SKColor(0x6E, 0x76, 0x81)),
            NameTextSize = 13,
            LabelsPaint = new SolidColorPaint(new SKColor(0x8B, 0x94, 0x9E)),
            TextSize = 12,
            SeparatorsPaint = new SolidColorPaint(new SKColor(0x1E, 0x27, 0x36)) { StrokeThickness = 1 },
            TicksPaint = new SolidColorPaint(new SKColor(0x2A, 0x31, 0x42)),
            MinLimit = 0,
        };
        YAxes = new[] { _yAxis };
    }

    [RelayCommand]
    private void SelectCategory(string category)
    {
        SelectedCategory = category;
        UpdateYAxisUnit(category);
        RebuildAvailableSensors();
        RebuildSeries();
        Update();
    }

    [RelayCommand]
    private void SelectTimeWindow(string window)
    {
        SelectedTimeWindow = window;
        RebuildSeries();
    }

    partial void OnStrokeThicknessChanged(float value)
    {
        foreach (var series in Series)
        {
            if (series is LineSeries<DateTimePoint> line && line.Stroke is SolidColorPaint paint)
                paint.StrokeThickness = value;
        }
    }

    private void UpdateYAxisUnit(string category)
    {
        string unit = category switch
        {
            "Temperatures" => Converters.SensorUnitHelper.TempUnit,
            "Loads" => "%",
            "Clocks" => "MHz",
            "Fans" => "RPM",
            "Power" => "W",
            "Voltages" => "V",
            _ => ""
        };
        YAxisUnit = unit;
        _yAxis.Name = unit;

        _yAxis.MinLimit = category switch
        {
            "Loads" => 0,
            "Temperatures" => 0,
            _ => null
        };

        _yAxis.MaxLimit = category == "Loads" ? 100 : null;
    }

    public void Update()
    {
        if (SelectedCategory == "Temperatures" && _yAxis.Name != Converters.SensorUnitHelper.TempUnit)
            UpdateYAxisUnit(SelectedCategory);

        if (Series.Count == 0)
        {
            RebuildAvailableSensors();
            RebuildSeries();
        }

        var selectedIds = GetSelectedIds();
        if (selectedIds.Count == 0) return;

        var sensorType = GetSensorType(SelectedCategory);
        var sensors = _service.GetSensorsByType(sensorType)
            .Where(s => s.Value.HasValue && selectedIds.Contains(s.Identifier.ToString()))
            .ToList();

        var now = DateTime.Now;
        int legendIdx = 0;

        bool isTemp = sensorType == SensorType.Temperature;

        foreach (var sensor in sensors)
        {
            string id = sensor.Identifier.ToString();
            if (!_seriesData.TryGetValue(id, out var data))
                continue;

            float val = sensor.Value ?? 0;
            if (isTemp) val = Converters.SensorUnitHelper.ConvertTemp(val);

            data.Add(new DateTimePoint(now, val));

            if (data.Count > MaxPoints)
                data.RemoveRange(0, data.Count - MaxPoints);

            if (legendIdx < SensorLegend.Count)
                SensorLegend[legendIdx].CurrentValue = $"{val:F1} {YAxisUnit}";
            legendIdx++;
        }
    }

    private void RebuildAvailableSensors()
    {
        _suppressSelectionHandler = true;

        foreach (var s in AvailableSensors)
            s.PropertyChanged -= OnSensorSelectionChanged;

        AvailableSensors.Clear();

        var sensorType = GetSensorType(SelectedCategory);
        var sensors = _service.GetSensorsByType(sensorType)
            .Where(s => s.Value.HasValue)
            .ToList();

        if (!_selectedSensorsByCategory.TryGetValue(SelectedCategory, out var selected))
        {
            selected = new HashSet<string>(
                sensors.Take(10).Select(s => s.Identifier.ToString()));
            _selectedSensorsByCategory[SelectedCategory] = selected;
        }

        foreach (var sensor in sensors)
        {
            var selectable = new SelectableSensor(sensor, selected.Contains(sensor.Identifier.ToString()));
            selectable.PropertyChanged += OnSensorSelectionChanged;
            AvailableSensors.Add(selectable);
        }

        _suppressSelectionHandler = false;
        UpdateSelectionInfo();
    }

    private void OnSensorSelectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressSelectionHandler || e.PropertyName != nameof(SelectableSensor.IsSelected))
            return;

        var sel = (SelectableSensor)sender!;

        if (!_selectedSensorsByCategory.TryGetValue(SelectedCategory, out var selected))
        {
            selected = new HashSet<string>();
            _selectedSensorsByCategory[SelectedCategory] = selected;
        }

        if (sel.IsSelected)
        {
            if (selected.Count >= MaxSelectedSensors)
            {
                // At cap — reject selection
                _suppressSelectionHandler = true;
                sel.IsSelected = false;
                _suppressSelectionHandler = false;
                return;
            }
            selected.Add(sel.Identifier);
        }
        else
        {
            selected.Remove(sel.Identifier);
        }

        UpdateSelectionInfo();
        RebuildSeries();
    }

    private void UpdateSelectionInfo()
    {
        var selected = _selectedSensorsByCategory.GetValueOrDefault(SelectedCategory);
        int count = selected?.Count ?? 0;
        SelectionInfo = $"{count}/{MaxSelectedSensors} sensors selected";
    }

    private HashSet<string> GetSelectedIds()
    {
        return _selectedSensorsByCategory.GetValueOrDefault(SelectedCategory) ?? new HashSet<string>();
    }

    private void RebuildSeries()
    {
        Series.Clear();
        _seriesData.Clear();
        SensorLegend.Clear();

        var selectedIds = GetSelectedIds();
        if (selectedIds.Count == 0) return;

        var sensorType = GetSensorType(SelectedCategory);
        var sensors = _service.GetSensorsByType(sensorType)
            .Where(s => s.Value.HasValue && selectedIds.Contains(s.Identifier.ToString()))
            .ToList();

        var now = DateTime.Now;
        bool isTemp = sensorType == SensorType.Temperature;

        for (int i = 0; i < sensors.Count; i++)
        {
            var sensor = sensors[i];
            string id = sensor.Identifier.ToString();
            float val = sensor.Value ?? 0;
            if (isTemp) val = Converters.SensorUnitHelper.ConvertTemp(val);

            var data = new List<DateTimePoint>(MaxPoints + 1);
            data.Add(new DateTimePoint(now, val));
            _seriesData[id] = data;

            var color = ChartColors[i % ChartColors.Length];
            string label = $"{sensor.Hardware.Name} \u2014 {sensor.Name}";

            Series.Add(new LineSeries<DateTimePoint>
            {
                Name = label,
                Values = data,
                Stroke = new SolidColorPaint(color) { StrokeThickness = StrokeThickness },
                Fill = new SolidColorPaint(color.WithAlpha(20)),
                GeometryFill = null,
                GeometryStroke = null,
                LineSmoothness = 0.5,
                AnimationsSpeed = TimeSpan.FromMilliseconds(100),
            });

            SensorLegend.Add(new ChartSensorInfo(
                label,
                $"{val:F1} {YAxisUnit}",
                color
            ));
        }
    }

    private static SensorType GetSensorType(string category)
    {
        return category switch
        {
            "Temperatures" => SensorType.Temperature,
            "Loads" => SensorType.Load,
            "Clocks" => SensorType.Clock,
            "Fans" => SensorType.Fan,
            "Power" => SensorType.Power,
            "Voltages" => SensorType.Voltage,
            _ => SensorType.Temperature
        };
    }
}
