// SPDX-License-Identifier: MPL-2.0

namespace LibreHardwareMonitor.Avalonia.Monitoring.Models;

/// <summary>
/// Identifies the role of a node in the hardware sensor tree.
/// </summary>
public enum SensorTreeNodeKind
{
    /// <summary>
    /// The computer root node.
    /// </summary>
    Computer,

    /// <summary>
    /// A hardware device or component.
    /// </summary>
    Hardware,

    /// <summary>
    /// A group of sensors sharing a measurement type.
    /// </summary>
    Category,

    /// <summary>
    /// An individual sensor reading.
    /// </summary>
    Sensor
}
