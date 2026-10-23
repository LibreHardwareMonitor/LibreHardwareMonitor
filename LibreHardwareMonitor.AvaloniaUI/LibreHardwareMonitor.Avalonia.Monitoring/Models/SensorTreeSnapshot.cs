// SPDX-License-Identifier: MPL-2.0

namespace LibreHardwareMonitor.Avalonia.Monitoring.Models;

/// <summary>
/// Captures the hardware sensor tree at a particular time.
/// </summary>
public sealed class SensorTreeSnapshot
{
    /// <summary>
    /// Initializes a sensor tree snapshot.
    /// </summary>
    /// <param name="updated">The time when the snapshot was captured.</param>
    /// <param name="roots">The top-level tree nodes.</param>
    public SensorTreeSnapshot(DateTime updated, IReadOnlyList<SensorTreeNodeSnapshot> roots)
    {
        Updated = updated;
        Roots = roots;
    }

    /// <summary>
    /// Gets the time when this snapshot was captured.
    /// </summary>
    public DateTime Updated { get; }

    /// <summary>
    /// Gets the top-level tree nodes.
    /// </summary>
    public IReadOnlyList<SensorTreeNodeSnapshot> Roots { get; }
}
