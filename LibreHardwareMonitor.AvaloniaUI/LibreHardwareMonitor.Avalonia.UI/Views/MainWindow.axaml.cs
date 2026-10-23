// SPDX-License-Identifier: MPL-2.0

using Avalonia.Controls;
using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Markup.Xaml;
using BlackSharp.UI.Avalonia.Controls;
using LibreHardwareMonitor.Avalonia.Mvvm.ViewModels;
using LibreHardwareMonitor.Avalonia.UI.MockData;

namespace LibreHardwareMonitor.Avalonia.UI.Views;

/// <summary>
/// Presents hardware and sensors in a hierarchical data grid.
/// </summary>
public sealed partial class MainWindow : DecoratedWindow
{
    /// <summary>
    /// Initializes the window for XAML preview and runtime construction.
    /// </summary>
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);

        if (Design.IsDesignMode)
        {
            InitializeViewModel(MainWindowDesignData.MainWindow);
        }
    }

    /// <summary>
    /// Initializes the window with live sensor presentation state.
    /// </summary>
    /// <param name="viewModel">The sensor tree and status to display.</param>
    public MainWindow(MainWindowViewModel viewModel) : this()
    {
        InitializeViewModel(viewModel);
    }

    /// <summary>
    /// Binds the view model and creates the hierarchical data grid model.
    /// </summary>
    /// <param name="viewModel">The sensor tree to display.</param>
    private void InitializeViewModel(MainWindowViewModel viewModel)
    {
        DataContext = viewModel;

        HierarchicalOptions<SensorTreeNodeViewModel> options = new()
        {
            ChildrenSelector = node => node.Children,
            IsLeafSelector = node => node.Children.Count == 0,
            IsExpandedSelector = node => node.IsExpanded,
            IsExpandedSetter = (node, isExpanded) => node.IsExpanded = isExpanded,
            VirtualizeChildren = true,
            AutoExpandRoot = true
        };

        HierarchicalModel<SensorTreeNodeViewModel> model = new(options);
        model.SetRoots(viewModel.Hardware);

        DataGrid sensorDataGrid = this.FindControl<DataGrid>("SensorDataGrid");
        sensorDataGrid.HierarchicalModel = model;
    }
}
