// SPDX-License-Identifier: MPL-2.0

using LibreHardwareMonitor.Avalonia.Monitoring.Models;
using LibreHardwareMonitor.Hardware;

namespace LibreHardwareMonitor.Avalonia.Monitoring.Services;

/// <summary>
/// Reads enabled hardware and publishes sensor tree snapshots.
/// </summary>
public sealed class SensorMonitor : IDisposable
{
    /// <summary>
    /// Cancels the background polling loop during shutdown.
    /// </summary>
    private readonly CancellationTokenSource _cancellation = new();

    /// <summary>
    /// Owns the hardware devices queried by this monitor.
    /// </summary>
    private readonly Computer _computer = new()
    {
        IsBatteryEnabled = true,
        IsControllerEnabled = true,
        IsCpuEnabled = true,
        IsGpuEnabled = true,
        IsMemoryEnabled = true,
        IsMotherboardEnabled = true,
        IsNetworkEnabled = true,
        IsPowerMonitorEnabled = true,
        IsPsuEnabled = true,
        IsStorageEnabled = true
    };

    /// <summary>
    /// Tracks the running polling task so monitoring starts only once.
    /// </summary>
    private Task _worker;

    /// <summary>
    /// Occurs when a fresh hardware tree is available.
    /// </summary>
    public event Action<SensorTreeSnapshot> SnapshotAvailable;

    /// <summary>
    /// Occurs when hardware polling fails.
    /// </summary>
    public event Action<string> ErrorOccurred;

    /// <summary>
    /// Starts hardware polling on a background task.
    /// </summary>
    public void Start()
    {
        if (_worker != null)
        {
            return;
        }

        _worker = Task.Run(() => RunAsync(_cancellation.Token));
    }

    /// <summary>
    /// Requests cancellation of the polling loop.
    /// </summary>
    public void Dispose()
    {
        _cancellation.Cancel();
    }

    /// <summary>
    /// Opens the hardware library and publishes snapshots until cancelled.
    /// </summary>
    /// <param name="cancellationToken">The token controlling the polling loop.</param>
    /// <returns>A task that completes when polling stops.</returns>
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            _computer.Open();

            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (IHardware hardware in _computer.Hardware)
                {
                    UpdateHardware(hardware);
                }

                SnapshotAvailable?.Invoke(CreateSnapshot());

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            ErrorOccurred?.Invoke(exception.Message);
        }
        finally
        {
            _computer.Close();
        }
    }

    /// <summary>
    /// Refreshes a hardware device and all its child devices.
    /// </summary>
    /// <param name="hardware">The hardware device to refresh.</param>
    private static void UpdateHardware(IHardware hardware)
    {
        hardware.Update();

        foreach (IHardware subHardware in hardware.SubHardware)
        {
            UpdateHardware(subHardware);
        }
    }

    /// <summary>
    /// Creates an immutable tree from the current hardware readings.
    /// </summary>
    /// <returns>The current sensor tree snapshot.</returns>
    private SensorTreeSnapshot CreateSnapshot()
    {
        List<SensorTreeNodeSnapshot> hardware = new();

        foreach (IHardware item in _computer.Hardware)
        {
            hardware.Add(CreateHardwareNode(item));
        }

        SensorTreeNodeSnapshot computer = new("computer",
            Environment.MachineName,
            SensorTreeNodeKind.Computer,
            null,
            null,
            null,
            null,
            null,
            hardware);

        return new SensorTreeSnapshot(DateTime.Now, new[] { computer });
    }

    /// <summary>
    /// Creates a tree node for a hardware device, its sensors and child devices.
    /// </summary>
    /// <param name="hardware">The hardware device to capture.</param>
    /// <returns>The captured hardware tree node.</returns>
    private static SensorTreeNodeSnapshot CreateHardwareNode(IHardware hardware)
    {
        List<SensorTreeNodeSnapshot> children = new();

        foreach (IGrouping<SensorType, ISensor> group in hardware.Sensors
                     .Where(sensor => !sensor.IsDefaultHidden)
                     .OrderBy(sensor => sensor.SensorType)
                     .ThenBy(sensor => sensor.Index)
                     .GroupBy(sensor => sensor.SensorType))
        {
            List<SensorTreeNodeSnapshot> sensors = new();

            foreach (ISensor sensor in group)
            {
                sensors.Add(new SensorTreeNodeSnapshot(sensor.Identifier.ToString(),
                    sensor.Name,
                    SensorTreeNodeKind.Sensor,
                    null,
                    sensor.SensorType,
                    sensor.Value,
                    sensor.Min,
                    sensor.Max,
                    Array.Empty<SensorTreeNodeSnapshot>()));
            }

            children.Add(new SensorTreeNodeSnapshot(hardware.Identifier + "/" + group.Key,
                SensorCategoryName.GetName(group.Key),
                SensorTreeNodeKind.Category,
                null,
                group.Key,
                null,
                null,
                null,
                sensors));
        }

        foreach (IHardware subHardware in hardware.SubHardware)
        {
            children.Add(CreateHardwareNode(subHardware));
        }

        return new SensorTreeNodeSnapshot(hardware.Identifier.ToString(),
            hardware.Name,
            SensorTreeNodeKind.Hardware,
            hardware.HardwareType,
            null,
            null,
            null,
            null,
            children);
    }
}
