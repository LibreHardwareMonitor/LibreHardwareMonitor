// SPDX-License-Identifier: MPL-2.0

using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using LibreHardwareMonitor.Avalonia.Localization.Services;
using LibreHardwareMonitor.Avalonia.Monitoring.Models;
using LibreHardwareMonitor.Avalonia.Mvvm.Services;
using LanguageResources = LibreHardwareMonitor.Avalonia.Localization.Resources.Resources;

namespace LibreHardwareMonitor.Avalonia.Mvvm.ViewModels;

/// <summary>
/// Holds the current sensor tree and monitoring status for the main window.
/// </summary>
public sealed class MainWindowViewModel : ObservableObject
{
    private string _status = LanguageResources.StatusSearching;

    /// <summary>
    /// Gets the root nodes of the sensor tree.
    /// </summary>
    public ObservableCollection<SensorTreeNodeViewModel> Hardware { get; } = new();

    /// <summary>
    /// Gets the latest localized monitoring status.
    /// </summary>
    public string Status
    {
        get { return _status; }
        private set { SetProperty(ref _status, value); }
    }

    /// <summary>
    /// Applies a new sensor snapshot while preserving existing node state.
    /// </summary>
    /// <param name="snapshot">The latest sensor tree snapshot.</param>
    public void Apply(SensorTreeSnapshot snapshot)
    {
        SensorTreeReconciler.Reconcile(Hardware, snapshot.Roots);

        string format = snapshot.Roots.Count > 0 && snapshot.Roots[0].Children.Count == 0
            ? LanguageResources.StatusUpdatedNoHardwareFormat
            : LanguageResources.StatusUpdatedFormat;

        CultureInfo systemCulture = AppCulture.SystemFormattingCulture;

        Status = string.Format(systemCulture, format, snapshot.Updated.ToString("T", systemCulture));
    }

    /// <summary>
    /// Displays a localized monitoring failure with the underlying error detail.
    /// </summary>
    /// <param name="message">The error detail supplied by hardware monitoring.</param>
    public void ShowError(string message)
    {
        Status = string.Format(CultureInfo.CurrentCulture, LanguageResources.StatusMonitoringUnavailableFormat, message);
    }
}
