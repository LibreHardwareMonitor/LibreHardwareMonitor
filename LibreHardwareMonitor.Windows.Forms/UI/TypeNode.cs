// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.Windows.Forms.Localization;
using LibreHardwareMonitor.Windows.Forms.Utilities;

namespace LibreHardwareMonitor.Windows.Forms.UI;

public sealed class TypeNode : Node, IExpandPersistNode
{
    private readonly PersistentSettings _settings;
    private readonly string _expandedIdentifier;
    private bool _expanded;

    public TypeNode(SensorType sensorType, Identifier parentId, PersistentSettings settings)
    {
        SensorType = sensorType;
        _expandedIdentifier = new Identifier(parentId, SensorType.ToString(), ".expanded").ToString();
        _settings = settings;

        switch (sensorType)
        {
            case SensorType.Voltage:
                Image = Utilities.EmbeddedResources.GetImage("voltage.png");
                break;
            case SensorType.Current:
                Image = Utilities.EmbeddedResources.GetImage("voltage.png");
                break;
            case SensorType.Energy:
                Image = Utilities.EmbeddedResources.GetImage("battery.png");
                break;
            case SensorType.Clock:
                Image = Utilities.EmbeddedResources.GetImage("clock.png");
                break;
            case SensorType.Load:
                Image = Utilities.EmbeddedResources.GetImage("load.png");
                break;
            case SensorType.Temperature:
                Image = Utilities.EmbeddedResources.GetImage("temperature.png");
                break;
            case SensorType.Fan:
                Image = Utilities.EmbeddedResources.GetImage("fan.png");
                break;
            case SensorType.Flow:
                Image = Utilities.EmbeddedResources.GetImage("flow.png");
                break;
            case SensorType.Control:
                Image = Utilities.EmbeddedResources.GetImage("control.png");
                break;
            case SensorType.Level:
                Image = Utilities.EmbeddedResources.GetImage("level.png");
                break;
            case SensorType.Power:
                Image = Utilities.EmbeddedResources.GetImage("power.png");
                break;
            case SensorType.Data:
                Image = Utilities.EmbeddedResources.GetImage("data.png");
                break;
            case SensorType.SmallData:
                Image = Utilities.EmbeddedResources.GetImage("data.png");
                break;
            case SensorType.Factor:
                Image = Utilities.EmbeddedResources.GetImage("factor.png");
                break;
            case SensorType.Frequency:
                Image = Utilities.EmbeddedResources.GetImage("clock.png");
                break;
            case SensorType.Throughput:
                Image = Utilities.EmbeddedResources.GetImage("throughput.png");
                break;
            case SensorType.TimeSpan:
                Image = Utilities.EmbeddedResources.GetImage("time.png");
                break;
            case SensorType.Timing:
                Image = Utilities.EmbeddedResources.GetImage("time.png");
                break;
            case SensorType.Noise:
                Image = Utilities.EmbeddedResources.GetImage("loudspeaker.png");
                break;
            case SensorType.Conductivity:
                Image = Utilities.EmbeddedResources.GetImage("voltage.png");
                break;
            case SensorType.Humidity:
                Image = Utilities.EmbeddedResources.GetImage("humidity.png");
                break;
        }

        NodeAdded += TypeNode_NodeAdded;
        NodeRemoved += TypeNode_NodeRemoved;
        _expanded = settings.GetValue(_expandedIdentifier, true);
    }

    /// <summary>
    /// The group heading is derived from the current UI culture on every read, so a simple
    /// repaint is enough to hot-switch the language. English is used as the fallback.
    /// </summary>
    public override string Text
    {
        get { return LocalizationManager.GetGroupText(SensorType, EnglishTitle); }
        set { /* group headings always follow the current language */ }
    }

    private string EnglishTitle
    {
        get
        {
            switch (SensorType)
            {
                case SensorType.Voltage: return "Voltages";
                case SensorType.Current: return "Currents";
                case SensorType.Energy: return "Capacities";
                case SensorType.Clock: return "Clocks";
                case SensorType.Load: return "Load";
                case SensorType.Temperature: return "Temperatures";
                case SensorType.Fan: return "Fans";
                case SensorType.Flow: return "Flows";
                case SensorType.Control: return "Controls";
                case SensorType.Level: return "Levels";
                case SensorType.Power: return "Powers";
                case SensorType.Data: return "Data";
                case SensorType.SmallData: return "Data";
                case SensorType.Factor: return "Factors";
                case SensorType.Frequency: return "Frequencies";
                case SensorType.Throughput: return "Throughput";
                case SensorType.TimeSpan: return "Times";
                case SensorType.Timing: return "Timings";
                case SensorType.Noise: return "Noise Levels";
                case SensorType.Conductivity: return "Conductivities";
                case SensorType.Humidity: return "Humidity Levels";
                default: return string.Empty;
            }
        }
    }

    private void TypeNode_NodeRemoved(Node node)
    {
        node.IsVisibleChanged -= Node_IsVisibleChanged;
        Node_IsVisibleChanged(null);
    }

    private void TypeNode_NodeAdded(Node node)
    {
        node.IsVisibleChanged += Node_IsVisibleChanged;
        Node_IsVisibleChanged(null);
    }

    private void Node_IsVisibleChanged(Node node)
    {
        foreach (Node n in Nodes)
        {
            if (n.IsVisible)
            {
                IsVisible = true;
                return;
            }
        }
        IsVisible = false;
    }

    public SensorType SensorType { get; }

    public bool Expanded
    {
        get => _expanded;
        set
        {
            _expanded = value;
            _settings.SetValue(_expandedIdentifier, _expanded);
        }
    }
}
