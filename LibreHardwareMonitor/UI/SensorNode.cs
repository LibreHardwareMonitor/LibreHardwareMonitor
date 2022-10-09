// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Drawing;
using System.Globalization;
using System.Text;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.Utilities;

namespace LibreHardwareMonitor.UI;

public class SensorNode : Node
{
    private readonly PersistentSettings _settings;
    private readonly UnitManager _unitManager;
    private Color? _penColor;
    private bool _plot;

    public SensorNode(ISensor sensor, PersistentSettings settings, UnitManager unitManager)
    {
        Sensor = sensor;
        _settings = settings;
        _unitManager = unitManager;

        switch (sensor.SensorType)
        {
            case SensorType.Voltage:
                Format = "{0:F3} V";
                break;
            case SensorType.Current:
                Format = "{0:F3} A";
                break;
            case SensorType.Clock:
                Format = "{0:F1} MHz";
                break;
            case SensorType.Load:
                Format = "{0:F1} %";
                break;
            case SensorType.Temperature:
                Format = "{0:F1} °C";
                break;
            case SensorType.Fan:
                Format = "{0:F0} RPM";
                break;
            case SensorType.Flow:
                Format = "{0:F1} L/h";
                break;
            case SensorType.Control:
                Format = "{0:F1} %";
                break;
            case SensorType.Level:
                Format = "{0:F1} %";
                break;
            case SensorType.Power:
                Format = "{0:F1} W";
                break;
            case SensorType.Data:
                Format = "{0:F1} GB";
                break;
            case SensorType.SmallData:
                Format = "{0:F1} MB";
                break;
            case SensorType.Factor:
                Format = "{0:F3}";
                break;
            case SensorType.Frequency:
                Format = "{0:F1} Hz";
                break;
            case SensorType.Throughput:
                Format = "{0:F1} B/s";
                break;
            case SensorType.TimeSpan:
                Format = "{0:g}";
                break;
            case SensorType.Energy:
                Format = "{0:F0} mWh";
                break;
            case SensorType.Noise:
                Format = "{0:F0} dBA";
                break;
        }

        bool hidden = settings.GetValue(new Identifier(sensor.Identifier, "hidden").ToString(), sensor.IsDefaultHidden);
        base.IsVisible = !hidden;
        Plot = settings.GetValue(new Identifier(sensor.Identifier, "plot").ToString(), false);
        string id = new Identifier(sensor.Identifier, "penColor").ToString();

        if (settings.Contains(id))
            PenColor = settings.GetValue(id, Color.Black);
    }

    public event EventHandler PlotSelectionChanged;

    public string Format { get; set; } = "";

    public override bool IsVisible
    {
        get { return base.IsVisible; }
        set
        {
            base.IsVisible = value;
            _settings.SetValue(new Identifier(Sensor.Identifier, "hidden").ToString(), !value);
        }
    }

    public string Max
    {
        get { return ValueToString(Sensor.Max); }
    }

    public string Min
    {
        get { return ValueToString(Sensor.Min); }
    }

    public Color? PenColor
    {
        get { return _penColor; }
        set
        {
            _penColor = value;

            string id = new Identifier(Sensor.Identifier, "penColor").ToString();
            if (value.HasValue)
                _settings.SetValue(id, value.Value);
            else
                _settings.Remove(id);

            PlotSelectionChanged?.Invoke(this, null);
        }
    }

    public bool Plot
    {
        get { return _plot; }
        set
        {
            _plot = value;
            _settings.SetValue(new Identifier(Sensor.Identifier, "plot").ToString(), value);
            PlotSelectionChanged?.Invoke(this, null);
        }
    }

    public ISensor Sensor { get; }

    public override string Text
    {
        get { return Sensor.Name; }
        set { Sensor.Name = value; }
    }

    public override string ToolTip
    {
        get
        {
            StringBuilder stringBuilder = new();

            if (Sensor is ICriticalSensorLimits criticalSensorLimits)
                OptionallyAppendCriticalRange(stringBuilder, criticalSensorLimits.CriticalLowLimit, criticalSensorLimits.CriticalHighLimit, "critical");

            if (Sensor is ISensorLimits sensorLimits)
                OptionallyAppendCriticalRange(stringBuilder, sensorLimits.LowLimit, sensorLimits.HighLimit, "normal");

            return stringBuilder.ToString();
        }
    }

    public string Value
    {
        get { return ValueToString(Sensor.Value); }
    }

    public string ValueToString(float? value)
    {
        if (value.HasValue)
        {
            switch (Sensor.SensorType)
            {
                case SensorType.Temperature when _unitManager.TemperatureUnit == TemperatureUnit.Fahrenheit:
                    {
                        return $"{value * 1.8 + 32:F1} °F";
                    }
                case SensorType.Throughput:
                    {
                        string result;
                        switch (Sensor.Name)
                        {
                            case "Connection Speed":
                                {
                                    switch (value)
                                    {
                                        case 100000000:
                                            {
                                                result = "100Mbps";
                                                break;
                                            }
                                        case 1000000000:
                                            {
                                                result = "1Gbps";
                                                break;
                                            }
                                        default:
                                            {
                                                if (value < 1024)
                                                    result = $"{value:F0} bps";
                                                else if (value < 1048576)
                                                    result = $"{value / 1024:F1} Kbps";
                                                else if (value < 1073741824)
                                                    result = $"{value / 1048576:F1} Mbps";
                                                else
                                                    result = $"{value / 1073741824:F1} Gbps";
                                            }

                                            break;
                                    }

                                    break;
                                }
                            default:
                                {
                                    const int _1MB = 1048576;

                                    result = value < _1MB ? $"{value / 1024:F1} KB/s" : $"{value / _1MB:F1} MB/s";

                                    break;
                                }
                        }

                        return result;
                    }
                case SensorType.TimeSpan:
                    {
                        return value.HasValue ? string.Format(Format, TimeSpan.FromSeconds(value.Value)) : "-";
                    }
                default:
                    {
                        return string.Format(Format, value);
                    }
            }
        }

        return "-";
    }

    private void OptionallyAppendCriticalRange(StringBuilder str, float? min, float? max, string kind)
    {
        if (min.HasValue)
        {
            str.AppendLine(max.HasValue
                               ? $"{CultureInfo.CurrentUICulture.TextInfo.ToTitleCase(kind)} range: {ValueToString(min)} to {ValueToString(max)}."
                               : $"Minimal {kind} value: {ValueToString(min)}.");
        }
        else if (max.HasValue)
        {
            str.AppendLine($"Maximal {kind} value: {ValueToString(max)}.");
        }
    }

    public override bool Equals(object obj)
    {
        if (obj == null)
            return false;

        if (!(obj is SensorNode s))
            return false;


        return (Sensor == s.Sensor);
    }

    public override int GetHashCode()
    {
        return Sensor.GetHashCode();
    }
}