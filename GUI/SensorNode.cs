// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Drawing;
using System.Collections.Generic;
using OpenHardwareMonitor.Hardware;
using OpenHardwareMonitor.Utilities;

namespace OpenHardwareMonitor.GUI
{
    public class SensorNode : Node
    {
        private ISensor _sensor;
        private PersistentSettings _settings;
        private UnitManager _unitManager;
        private bool _plot = false;
        private Color? _penColor = null;
        public string Format { get; set; } = "";

        public string ValueToString(float? value)
        {
            if (value.HasValue)
            {
                if (_sensor.SensorType == SensorType.Temperature && _unitManager.TemperatureUnit == TemperatureUnit.Fahrenheit)
                {
                    return string.Format("{0:F1} °F", value * 1.8 + 32);
                }
                else if (_sensor.SensorType == SensorType.Throughput)
                {
                    string result = "-";
                    switch (_sensor.Name)
                    {
                        case "Connection Speed":
                            {
                                switch (value)
                                {
                                    case 100000000:
                                        result = "100Mbps";
                                        break;
                                    case 1000000000:
                                        result = "1Gbps";
                                        break;
                                    default:
                                        {
                                            if (value < 1024)
                                                result = string.Format("{0:F0} bps", value);
                                            else if (value < 1048576)
                                                result = string.Format("{0:F1} Kbps", value / 1024);
                                            else if (value < 1073741824)
                                                result = string.Format("{0:F1} Mbps", value / 1048576);
                                            else
                                                result = string.Format("{0:F1} Gbps", value / 1073741824);
                                        }
                                        break;
                                }
                            }
                            break;
                        default:
                            {
                                if (value < 1048576)
                                    result = string.Format("{0:F1} KB/s", value / 1024);
                                else
                                    result = string.Format("{0:F1} MB/s", value / 1048576);
                            }
                            break;
                    }
                    return result;
                }
                else
                {
                    return string.Format(Format, value);
                }
            }
            else
                return "-";
        }

        public SensorNode(ISensor sensor, PersistentSettings settings, UnitManager unitManager) : base()
        {
            _sensor = sensor;
            _settings = settings;
            _unitManager = unitManager;
            switch (sensor.SensorType)
            {
                case SensorType.Voltage: Format = "{0:F3} V"; break;
                case SensorType.Clock: Format = "{0:F0} MHz"; break;
                case SensorType.Load: Format = "{0:F1} %"; break;
                case SensorType.Temperature: Format = "{0:F1} °C"; break;
                case SensorType.Fan: Format = "{0:F0} RPM"; break;
                case SensorType.Flow: Format = "{0:F0} L/h"; break;
                case SensorType.Control: Format = "{0:F1} %"; break;
                case SensorType.Level: Format = "{0:F1} %"; break;
                case SensorType.Power: Format = "{0:F1} W"; break;
                case SensorType.Data: Format = "{0:F1} GB"; break;
                case SensorType.SmallData: Format = "{0:F1} MB"; break;
                case SensorType.Factor: Format = "{0:F3}"; break;
                case SensorType.Frequency: Format = "{0:F1} Hz"; break;
                case SensorType.Throughput: Format = "{0:F1} B/s"; break;
            }
            bool hidden = settings.GetValue(new Identifier(sensor.Identifier, "hidden").ToString(), sensor.IsDefaultHidden);
            base.IsVisible = !hidden;
            Plot = settings.GetValue(new Identifier(sensor.Identifier, "plot").ToString(), false);
            string id = new Identifier(sensor.Identifier, "penColor").ToString();
            if (settings.Contains(id))
                PenColor = settings.GetValue(id, Color.Black);
        }

        public override string Text
        {
            get { return _sensor.Name; }
            set { _sensor.Name = value; }
        }

        public override bool IsVisible
        {
            get { return base.IsVisible; }
            set
            {
                base.IsVisible = value;
                _settings.SetValue(new Identifier(_sensor.Identifier, "hidden").ToString(), !value);
            }
        }

        public Color? PenColor
        {
            get { return _penColor; }
            set
            {
                _penColor = value;

                string id = new Identifier(_sensor.Identifier, "penColor").ToString();
                if (value.HasValue)
                    _settings.SetValue(id, value.Value);
                else
                    _settings.Remove(id);

                if (PlotSelectionChanged != null)
                    PlotSelectionChanged(this, null);
            }
        }

        public bool Plot
        {
            get { return _plot; }
            set
            {
                _plot = value;
                _settings.SetValue(new Identifier(_sensor.Identifier, "plot").ToString(), value);
                if (PlotSelectionChanged != null)
                    PlotSelectionChanged(this, null);
            }
        }

        public event EventHandler PlotSelectionChanged;

        public ISensor Sensor
        {
            get { return _sensor; }
        }

        public string Value
        {
            get { return ValueToString(_sensor.Value); }
        }

        public string Min
        {
            get { return ValueToString(_sensor.Min); }
        }

        public string Max
        {
            get { return ValueToString(_sensor.Max); }
        }

        public override bool Equals(System.Object obj)
        {
            if (obj == null)
                return false;

            SensorNode s = obj as SensorNode;
            if (s == null)
                return false;

            return (_sensor == s._sensor);
        }

        public override int GetHashCode()
        {
            return _sensor.GetHashCode();
        }
    }
}
