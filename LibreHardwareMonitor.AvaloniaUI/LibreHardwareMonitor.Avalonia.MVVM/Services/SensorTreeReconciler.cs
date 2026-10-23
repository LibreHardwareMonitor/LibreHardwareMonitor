// SPDX-License-Identifier: MPL-2.0

using System.Collections.ObjectModel;
using LibreHardwareMonitor.Avalonia.Monitoring.Models;
using LibreHardwareMonitor.Avalonia.MVVM.ViewModels;

namespace LibreHardwareMonitor.Avalonia.MVVM.Services;

/// <summary>
/// Reconciles a visible sensor tree with the latest monitoring snapshot.
/// </summary>
internal static class SensorTreeReconciler
{
    /// <summary>
    /// Updates, inserts, moves, or removes nodes to match the latest snapshot order.
    /// </summary>
    /// <param name="nodes">The existing visible nodes.</param>
    /// <param name="snapshots">The latest snapshot nodes.</param>
    public static void Reconcile(
        ObservableCollection<SensorTreeNodeViewModel> nodes,
        IReadOnlyList<SensorTreeNodeSnapshot> snapshots)
    {
        // Reconcile the nodes with the snapshots
        for (int index = 0; index < snapshots.Count; ++index)
        {
            SensorTreeNodeSnapshot snapshot = snapshots[index];
            int existingIndex = -1;

            // Find the existing node with the same identifier
            for (int current = index; current < nodes.Count; ++current)
            {
                if (nodes[current].Identifier == snapshot.Identifier)
                {
                    existingIndex = current;
                    break;
                }
            }

            if (existingIndex < 0)
            {
                nodes.Insert(index, new SensorTreeNodeViewModel(snapshot));
            }
            else
            {
                if (existingIndex != index)
                {
                    nodes.Move(existingIndex, index);
                }

                nodes[index].Update(snapshot);
            }
        }

        // Remove any extra nodes that are no longer present in the snapshots
        while (nodes.Count > snapshots.Count)
        {
            nodes.RemoveAt(nodes.Count - 1);
        }
    }
}
