// SPDX-License-Identifier: MPL-2.0

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
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
    /// Toggles the hardware tree node represented by the double-clicked row.
    /// </summary>
    /// <param name="sender">The data grid that raised the event.</param>
    /// <param name="e">The double-tap event data.</param>
    private void SensorDataGridDoubleTapped(object sender, TappedEventArgs e)
    {
        if (sender is not DataGrid dataGrid ||
            dataGrid.HierarchicalModel is not IHierarchicalModel model ||
            e.Source is not Visual visual)
        {
            return;
        }

        for (Visual current = visual; current is not null; current = current.GetVisualParent())
        {
            if (current is DataGridRow row && row.DataContext is HierarchicalNode node)
            {
                if (!node.IsLeaf)
                {
                    model.Toggle(node);
                    e.Handled = true;
                }

                return;
            }

            if (ReferenceEquals(current, dataGrid))
            {
                return;
            }
        }
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
