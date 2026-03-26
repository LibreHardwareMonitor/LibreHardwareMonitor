using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

public partial class ChartsViewModel : ObservableObject
{
    private readonly HardwareMonitorService _service;
    private readonly Dictionary<string, List<DateTimePoint>> _seriesData = new();
    private const int MaxPoints = 120;

    [ObservableProperty] private string _selectedCategory = "Temperatures";
    [ObservableProperty] private string _yAxisUnit = "°C";

    public ObservableCollection<ISeries> Series { get; } = new();
    public ObservableCollection<ChartSensorInfo> SensorLegend { get; } = new();

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
    };

    private readonly Axis _yAxis;

    public ChartsViewModel(HardwareMonitorService service)
    {
        _service = service;
        _yAxis = new Axis
        {
            Name = "°C",
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
        RebuildSeries();
    }

    private void UpdateYAxisUnit(string category)
    {
        string unit = category switch
        {
            "Temperatures" => "°C",
            "Loads" => "%",
            "Clocks" => "MHz",
            "Fans" => "RPM",
            "Power" => "W",
            "Voltages" => "V",
            _ => ""
        };
        YAxisUnit = unit;
        _yAxis.Name = unit;

        // Set sensible min limits
        _yAxis.MinLimit = category switch
        {
            "Loads" => 0,
            "Temperatures" => 0,
            _ => null
        };

        // Set max limits for percentage
        _yAxis.MaxLimit = category == "Loads" ? 100 : null;
    }

    public void Update()
    {
        // Build series on first update
        if (Series.Count == 0)
            RebuildSeries();

        var sensorType = GetSensorType(SelectedCategory);
        var sensors = _service.GetSensorsByType(sensorType)
            .Where(s => s.Value.HasValue)
            .Take(10)
            .ToList();

        var now = DateTime.Now;

        for (int i = 0; i < sensors.Count; i++)
        {
            var sensor = sensors[i];
            string id = sensor.Identifier.ToString();
            if (!_seriesData.TryGetValue(id, out var data))
                continue; // Skip sensors not in our series

            data.Add(new DateTimePoint(now, sensor.Value ?? 0));

            if (data.Count > MaxPoints)
                data.RemoveRange(0, data.Count - MaxPoints);

            // Update legend current value
            if (i < SensorLegend.Count)
                SensorLegend[i].CurrentValue = $"{sensor.Value ?? 0:F1} {YAxisUnit}";
        }
    }

    private void RebuildSeries()
    {
        Series.Clear();
        _seriesData.Clear();
        SensorLegend.Clear();

        var sensorType = GetSensorType(SelectedCategory);
        var sensors = _service.GetSensorsByType(sensorType)
            .Where(s => s.Value.HasValue)
            .Take(10)
            .ToList();

        var now = DateTime.Now;

        for (int i = 0; i < sensors.Count; i++)
        {
            var sensor = sensors[i];
            string id = sensor.Identifier.ToString();
            var data = new List<DateTimePoint>(MaxPoints + 1);

            // Seed with initial data point so chart renders immediately
            data.Add(new DateTimePoint(now, sensor.Value ?? 0));
            _seriesData[id] = data;

            var color = ChartColors[i % ChartColors.Length];
            string label = $"{sensor.Hardware.Name} — {sensor.Name}";

            Series.Add(new LineSeries<DateTimePoint>
            {
                Name = label,
                Values = data,
                Stroke = new SolidColorPaint(color) { StrokeThickness = 2.5f },
                Fill = new SolidColorPaint(color.WithAlpha(20)),
                GeometryFill = null,
                GeometryStroke = null,
                LineSmoothness = 0.5,
                AnimationsSpeed = TimeSpan.FromMilliseconds(100),
            });

            SensorLegend.Add(new ChartSensorInfo(
                label,
                $"{sensor.Value ?? 0:F1} {YAxisUnit}",
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
