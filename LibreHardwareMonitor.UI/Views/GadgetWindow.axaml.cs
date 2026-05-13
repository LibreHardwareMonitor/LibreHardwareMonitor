using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using LibreHardwareMonitor.UI.ViewModels;

namespace LibreHardwareMonitor.UI.Views;

public partial class GadgetWindow : Window
{
    public GadgetWindow()
    {
        InitializeComponent();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed &&
            DataContext is GadgetViewModel { IsPositionLocked: false })
        {
            BeginMoveDrag(e);
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
