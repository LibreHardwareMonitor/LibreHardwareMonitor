/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2009-2016 Michael Möller <mmoeller@openhardwaremonitor.org>
	
*/

using System;
using System.Drawing;
using System.Collections.Generic;
using OpenHardwareMonitor.Hardware;
using OpenHardwareMonitor.Utilities;

namespace OpenHardwareMonitor.GUI {
  public class SensorNode : Node {
    
    private ISensor sensor;
    private PersistentSettings settings;
    private UnitManager unitManager;
    public string format;
    private bool plot = false;
    private Color? penColor = null;

    public string ValueToString(float? value) {
      if (value.HasValue) {
        if (sensor.SensorType == SensorType.Temperature && 
          unitManager.TemperatureUnit == TemperatureUnit.Fahrenheit) {
          return string.Format("{0:F1} °F", value * 1.8 + 32);
        } else if(sensor.SensorType == SensorType.Throughput){          string result = "-";
          switch (sensor.Name){ 
            case "Connection Speed": {
              switch (value){ 
                case 100000000:
                    result = "100Mbps";
                    break;
                case 1000000000:
                    result = "1Gbps";
                    break;
                default: {
                  if (value < 1024)
                      result = string.Format("{0:F0} bps", value);
                  else if (value < 1048576)
                      result = string.Format("{0:F1} Kbps",value/1024);
                  else if (value < 1073741824)
                      result = string.Format("{0:F1} Mbps",value/1048576);
                  else
                      result = string.Format("{0:F1} Gbps",value/ 1073741824);
                } break;
              }
            }
            break;
            default:{
              if (value<1048576)
                  result = string.Format("{0:F1} KB/s",value/1024);
              else
                  result = string.Format("{0:F1} MB/s",value/1048576);
            }
            break;
          }
          return result;
        } else {
          return string.Format(format, value);
        }                
      } else
        return "-";
    }

    public SensorNode(ISensor sensor, PersistentSettings settings, 
      UnitManager unitManager) : base() {      
      this.sensor = sensor;
      this.settings = settings;
      this.unitManager = unitManager;
      switch (sensor.SensorType) {
        case SensorType.Voltage: format = "{0:F3} V"; break;
        case SensorType.Clock: format = "{0:F0} MHz"; break;
        case SensorType.Load: format = "{0:F1} %"; break;
        case SensorType.Temperature: format = "{0:F1} °C"; break;
        case SensorType.Fan: format = "{0:F0} RPM"; break;
        case SensorType.Flow: format = "{0:F0} L/h"; break;
        case SensorType.Control: format = "{0:F1} %"; break;
        case SensorType.Level: format = "{0:F1} %"; break;
        case SensorType.Power: format = "{0:F1} W"; break;
        case SensorType.Data: format = "{0:F1} GB"; break;
        case SensorType.SmallData: format = "{0:F1} MB"; break;
        case SensorType.Factor: format = "{0:F3}"; break;
        case SensorType.Frequency: format = "{0:F1} Hz"; break;
        case SensorType.Throughput: format = "{0:F1} B/s"; break;
      }

      bool hidden = settings.GetValue(new Identifier(sensor.Identifier, 
        "hidden").ToString(), sensor.IsDefaultHidden);
      base.IsVisible = !hidden;

      this.Plot = settings.GetValue(new Identifier(sensor.Identifier, 
        "plot").ToString(), false);

      string id = new Identifier(sensor.Identifier, "penColor").ToString();
      if (settings.Contains(id))
        this.PenColor = settings.GetValue(id, Color.Black);
    }

    public override string Text {
      get { return sensor.Name; }
      set { sensor.Name = value; }
    }

    public override bool IsVisible {
      get { return base.IsVisible; }
      set { 
        base.IsVisible = value;
        settings.SetValue(new Identifier(sensor.Identifier,
          "hidden").ToString(), !value);
      }
    }

    public Color? PenColor {
      get { return penColor; }
      set {
        penColor = value;

        string id = new Identifier(sensor.Identifier, "penColor").ToString();
        if (value.HasValue)
          settings.SetValue(id, value.Value);
        else
          settings.Remove(id);

        if (PlotSelectionChanged != null)
          PlotSelectionChanged(this, null);
      }
    }

    public bool Plot {
      get { return plot; }
      set { 
        plot = value;
        settings.SetValue(new Identifier(sensor.Identifier, "plot").ToString(), 
          value);
        if (PlotSelectionChanged != null)
          PlotSelectionChanged(this, null);
      }
    }

    public event EventHandler PlotSelectionChanged;

    public ISensor Sensor {
      get { return sensor; }
    }

    public string Value {
      get { return ValueToString(sensor.Value); }
    }

    public string Min {
      get { return ValueToString(sensor.Min); }
    }

    public string Max {
      get { return ValueToString(sensor.Max); }
    }

    public override bool Equals(System.Object obj) {
      if (obj == null) 
        return false;

      SensorNode s = obj as SensorNode;
      if (s == null) 
        return false;

      return (sensor == s.sensor);
    }

    public override int GetHashCode() {
      return sensor.GetHashCode();
    }

  }
}
