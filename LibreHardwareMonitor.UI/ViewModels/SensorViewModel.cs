using CommunityToolkit.Mvvm.ComponentModel;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.UI.Converters;

namespace LibreHardwareMonitor.UI.ViewModels;

public partial class SensorViewModel : ObservableObject
{
    private readonly ISensor _sensor;

    [ObservableProperty] private string _name;
    [ObservableProperty] private float _value;
    [ObservableProperty] private float _min;
    [ObservableProperty] private float _max;
    [ObservableProperty] private SensorType _sensorType;
    [ObservableProperty] private string _formattedValue = "N/A";
    [ObservableProperty] private string _formattedMin = "N/A";
    [ObservableProperty] private string _formattedMax = "N/A";
    [ObservableProperty] private string _unit = "";

    public ISensor Sensor => _sensor;
    public string Identifier => _sensor.Identifier.ToString();
    public string HardwareName => _sensor.Hardware.Name;
    public string DisplayName => $"{_sensor.Hardware.Name} — {_sensor.Name}";

    /// <summary>
    /// Returns true if this sensor is a reference/threshold value (not an actual reading).
    /// </summary>
    public bool IsReferenceSensor
    {
        get
        {
            string n = _sensor.Name;
            return n.Contains("Warning") || n.Contains("Critical") || n.Contains("Lifetime") ||
                   n.Contains("Threshold") || n.Contains("Limit");
        }
    }

    public SensorViewModel(ISensor sensor)
    {
        _sensor = sensor;
        _name = sensor.Name;
        _sensorType = sensor.SensorType;
        _unit = SensorUnitHelper.GetUnit(sensor.SensorType);
        Update();
    }

    public void Update()
    {
        float rawValue = _sensor.Value ?? 0;
        float rawMin = _sensor.Min ?? 0;
        float rawMax = _sensor.Max ?? 0;

        bool isTemp = _sensor.SensorType == SensorType.Temperature;
        float newValue = isTemp ? SensorUnitHelper.ConvertTemp(rawValue) : rawValue;
        float newMin = isTemp ? SensorUnitHelper.ConvertTemp(rawMin) : rawMin;
        float newMax = isTemp ? SensorUnitHelper.ConvertTemp(rawMax) : rawMax;

        if (newValue == Value && newMin == Min && newMax == Max && _sensor.Name == Name)
            return;

        Value = newValue;
        Min = newMin;
        Max = newMax;
        Name = _sensor.Name;
        if (isTemp) Unit = SensorUnitHelper.TempUnit;

        FormattedValue = _sensor.Value is float v ? FormatSensorValue(v, _sensor.SensorType) : "N/A";
        FormattedMin = _sensor.Min is float mn ? FormatSensorValue(mn, _sensor.SensorType) : "N/A";
        FormattedMax = _sensor.Max is float mx ? FormatSensorValue(mx, _sensor.SensorType) : "N/A";
    }

    public static string FormatSensorValue(float v, SensorType type)
    {
        if (type == SensorType.Temperature)
            v = SensorUnitHelper.ConvertTemp(v);

        return type switch
        {
            SensorType.Voltage => $"{v:F3}",
            SensorType.Current => $"{v:F3}",
            SensorType.Power => $"{v:F1}",
            SensorType.Clock => $"{v:F0}",
            SensorType.Temperature => $"{v:F1}",
            SensorType.Load => $"{v:F1}",
            SensorType.Frequency => $"{v:F1}",
            SensorType.Fan => $"{v:F0}",
            SensorType.Flow => $"{v:F1}",
            SensorType.Control => $"{v:F1}",
            SensorType.Level => $"{v:F1}",
            SensorType.Data => $"{v:F1}",
            SensorType.SmallData => $"{v:F1}",
            SensorType.Throughput => SensorUnitHelper.FormatThroughput(v),
            SensorType.Energy => $"{v:F0}",
            SensorType.Noise => $"{v:F1}",
            SensorType.Humidity => $"{v:F1}",
            _ => $"{v:F1}"
        };
    }

}
