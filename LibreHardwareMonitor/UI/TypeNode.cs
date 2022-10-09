// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.Utilities;

namespace LibreHardwareMonitor.UI;

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
                Text = "Voltages";
                break;
            case SensorType.Current:
                Image = Utilities.EmbeddedResources.GetImage("voltage.png");
                Text = "Currents";
                break;
            case SensorType.Energy:
                Image = Utilities.EmbeddedResources.GetImage("battery.png");
                Text = "Capacities";
                break;
            case SensorType.Clock:
                Image = Utilities.EmbeddedResources.GetImage("clock.png");
                Text = "Clocks";
                break;
            case SensorType.Load:
                Image = Utilities.EmbeddedResources.GetImage("load.png");
                Text = "Load";
                break;
            case SensorType.Temperature:
                Image = Utilities.EmbeddedResources.GetImage("temperature.png");
                Text = "Temperatures";
                break;
            case SensorType.Fan:
                Image = Utilities.EmbeddedResources.GetImage("fan.png");
                Text = "Fans";
                break;
            case SensorType.Flow:
                Image = Utilities.EmbeddedResources.GetImage("flow.png");
                Text = "Flows";
                break;
            case SensorType.Control:
                Image = Utilities.EmbeddedResources.GetImage("control.png");
                Text = "Controls";
                break;
            case SensorType.Level:
                Image = Utilities.EmbeddedResources.GetImage("level.png");
                Text = "Levels";
                break;
            case SensorType.Power:
                Image = Utilities.EmbeddedResources.GetImage("power.png");
                Text = "Powers";
                break;
            case SensorType.Data:
                Image = Utilities.EmbeddedResources.GetImage("data.png");
                Text = "Data";
                break;
            case SensorType.SmallData:
                Image = Utilities.EmbeddedResources.GetImage("data.png");
                Text = "Data";
                break;
            case SensorType.Factor:
                Image = Utilities.EmbeddedResources.GetImage("factor.png");
                Text = "Factors";
                break;
            case SensorType.Frequency:
                Image = Utilities.EmbeddedResources.GetImage("clock.png");
                Text = "Frequencies";
                break;
            case SensorType.Throughput:
                Image = Utilities.EmbeddedResources.GetImage("throughput.png");
                Text = "Throughput";
                break;
            case SensorType.TimeSpan:
                Image = Utilities.EmbeddedResources.GetImage("time.png");
                Text = "Times";
                break;
            case SensorType.Noise:
                Image = Utilities.EmbeddedResources.GetImage("loudspeaker.png");
                Text = "Noise Levels";
                break;
        }

        NodeAdded += TypeNode_NodeAdded;
        NodeRemoved += TypeNode_NodeRemoved;
        _expanded = settings.GetValue(_expandedIdentifier, true);
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