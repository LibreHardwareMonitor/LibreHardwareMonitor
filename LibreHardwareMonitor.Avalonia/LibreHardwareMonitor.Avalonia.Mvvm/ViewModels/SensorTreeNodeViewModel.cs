// SPDX-License-Identifier: MPL-2.0

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LibreHardwareMonitor.Avalonia.Monitoring.Models;
using LibreHardwareMonitor.Avalonia.Mvvm.Formatting;
using LibreHardwareMonitor.Avalonia.Mvvm.Services;

namespace LibreHardwareMonitor.Avalonia.Mvvm.ViewModels;

/// <summary>
/// Presents one computer, hardware, category or sensor node in the grid.
/// </summary>
public sealed class SensorTreeNodeViewModel : ObservableObject
{
    private string _name;
    private string _iconName;
    private string _value;
    private string _min;
    private string _max;
    private bool _isExpanded;

    /// <summary>
    /// Initializes the node from a captured sensor tree snapshot.
    /// </summary>
    /// <param name="snapshot">The initial node state.</param>
    public SensorTreeNodeViewModel(SensorTreeNodeSnapshot snapshot)
    {
        Identifier = snapshot.Identifier;
        Kind = snapshot.Kind;

        _isExpanded = Kind == SensorTreeNodeKind.Computer;

        Update(snapshot);
    }

    /// <summary>
    /// Gets the stable identifier used to reconcile sensor updates.
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    /// Gets the node's role in the sensor tree.
    /// </summary>
    public SensorTreeNodeKind Kind { get; }

    /// <summary>
    /// Gets the child hardware, categories, or sensors.
    /// </summary>
    public ObservableCollection<SensorTreeNodeViewModel> Children { get; } = new();

    /// <summary>
    /// Gets or sets whether the node is expanded in the grid.
    /// </summary>
    public bool IsExpanded
    {
        get { return _isExpanded; }
        set { SetProperty(ref _isExpanded, value); }
    }

    /// <summary>
    /// Gets the name displayed for this node.
    /// </summary>
    public string Name
    {
        get { return _name; }
        private set { SetProperty(ref _name, value); }
    }

    /// <summary>
    /// Gets the icon asset name for this node.
    /// </summary>
    public string IconName
    {
        get { return _iconName; }
        private set { SetProperty(ref _iconName, value); }
    }

    /// <summary>
    /// Gets the formatted current sensor value.
    /// </summary>
    public string Value
    {
        get { return _value; }
        private set { SetProperty(ref _value, value); }
    }

    /// <summary>
    /// Gets the formatted minimum sensor value.
    /// </summary>
    public string Min
    {
        get { return _min; }
        private set { SetProperty(ref _min, value); }
    }

    /// <summary>
    /// Gets the formatted maximum sensor value.
    /// </summary>
    public string Max
    {
        get { return _max; }
        private set { SetProperty(ref _max, value); }
    }

    /// <summary>
    /// Updates the node and reconciles its children from a new snapshot.
    /// </summary>
    /// <param name="snapshot">The latest node state.</param>
    public void Update(SensorTreeNodeSnapshot snapshot)
    {
        Name = snapshot.Name;
        IconName = SensorIconName.GetName(snapshot);

        if (Kind == SensorTreeNodeKind.Sensor)
        {
            Value = SensorValueFormatter.Format(snapshot.SensorType, snapshot.Value);
            Min = SensorValueFormatter.Format(snapshot.SensorType, snapshot.Min);
            Max = SensorValueFormatter.Format(snapshot.SensorType, snapshot.Max);
        }
        else
        {
            Value = string.Empty;
            Min = string.Empty;
            Max = string.Empty;
        }

        SensorTreeReconciler.Reconcile(Children, snapshot.Children);
    }
}
