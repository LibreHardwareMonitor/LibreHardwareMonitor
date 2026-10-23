// SPDX-License-Identifier: MPL-2.0

using LibreHardwareMonitor.Hardware;

namespace LibreHardwareMonitor.Avalonia.Monitoring.Models;

/// <summary>
/// Describes one computer, hardware, category or sensor tree node.
/// </summary>
public sealed class SensorTreeNodeSnapshot
{
    /// <summary>
    /// Initializes a sensor tree node snapshot.
    /// </summary>
    /// <param name="identifier">The stable tree node identifier.</param>
    /// <param name="name">The display name supplied by the device or category.</param>
    /// <param name="kind">The role of the node in the tree.</param>
    /// <param name="hardwareType">The hardware type for hardware nodes.</param>
    /// <param name="sensorType">The measurement type for category and sensor nodes.</param>
    /// <param name="value">The latest sensor value, when available.</param>
    /// <param name="min">The minimum sensor value, when available.</param>
    /// <param name="max">The maximum sensor value, when available.</param>
    /// <param name="children">The child nodes.</param>
    public SensorTreeNodeSnapshot(
        string identifier,
        string name,
        SensorTreeNodeKind kind,
        HardwareType? hardwareType,
        SensorType? sensorType,
        float? value,
        float? min,
        float? max,
        IReadOnlyList<SensorTreeNodeSnapshot> children)
    {
        Identifier = identifier;
        Name = name;
        Kind = kind;
        HardwareType = hardwareType;
        SensorType = sensorType;
        Value = value;
        Min = min;
        Max = max;
        Children = children;
    }

    /// <summary>
    /// Gets the stable tree node identifier.
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    /// Gets the display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the tree node kind.
    /// </summary>
    public SensorTreeNodeKind Kind { get; }

    /// <summary>
    /// Gets the hardware type for a hardware node.
    /// </summary>
    public HardwareType? HardwareType { get; }

    /// <summary>
    /// Gets the measurement type for a category or sensor.
    /// </summary>
    public SensorType? SensorType { get; }

    /// <summary>
    /// Gets the latest sensor value.
    /// </summary>
    public float? Value { get; }

    /// <summary>
    /// Gets the minimum recorded sensor value.
    /// </summary>
    public float? Min { get; }

    /// <summary>
    /// Gets the maximum recorded sensor value.
    /// </summary>
    public float? Max { get; }

    /// <summary>
    /// Gets the child nodes.
    /// </summary>
    public IReadOnlyList<SensorTreeNodeSnapshot> Children { get; }
}
