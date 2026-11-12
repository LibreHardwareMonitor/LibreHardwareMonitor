// SPDX-License-Identifier: MPL-2.0

using LibreHardwareMonitor.Avalonia.Monitoring.Models;
using LibreHardwareMonitor.Hardware;

namespace LibreHardwareMonitor.Avalonia.Mvvm.Services;

/// <summary>
/// Maps sensor tree node kinds to icon asset names.
/// </summary>
internal static class SensorIconName
{
    /// <summary>
    /// Gets the icon asset name for a sensor tree node.
    /// </summary>
    /// <param name="node">The node to represent.</param>
    /// <returns>The asset name, or <see langword="null"/> for a sensor value.</returns>
    public static string GetName(SensorTreeNodeSnapshot node)
    {
        switch (node.Kind)
        {
            case SensorTreeNodeKind.Computer:
                return "computer.png";
            case SensorTreeNodeKind.Hardware:
                return GetHardwareName(node.HardwareType);
            case SensorTreeNodeKind.Category:
                return GetCategoryName(node.SensorType);
            default:
                return null;
        }
    }

    /// <summary>
    /// Gets the icon asset name for a hardware type.
    /// </summary>
    /// <param name="hardwareType">The hardware type.</param>
    /// <returns>The matching hardware icon asset name.</returns>
    private static string GetHardwareName(HardwareType? hardwareType)
    {
        switch (hardwareType)
        {
            case HardwareType.Cpu: return "cpu.png";
            case HardwareType.GpuNvidia: return "nvidia.png";
            case HardwareType.GpuAmd: return "amd.png";
            case HardwareType.GpuIntel: return "intel.png";
            case HardwareType.Storage: return "hdd.png";
            case HardwareType.Motherboard: return "mainboard.png";
            case HardwareType.SuperIO:
            case HardwareType.EmbeddedController: return "chip.png";
            case HardwareType.Memory: return "ram.png";
            case HardwareType.Network: return "nic.png";
            case HardwareType.Cooler: return "fan.png";
            case HardwareType.Psu: return "power-supply.png";
            case HardwareType.Battery: return "battery.png";
            case HardwareType.PowerMonitor: return "powermonitor.png";
            default: return "chip.png";
        }
    }

    /// <summary>
    /// Gets the icon asset name for a sensor category.
    /// </summary>
    /// <param name="sensorType">The sensor measurement type.</param>
    /// <returns>The matching category icon asset name.</returns>
    private static string GetCategoryName(SensorType? sensorType)
    {
        switch (sensorType)
        {
            case SensorType.Voltage: return "voltage.png";
            case SensorType.Current: return "voltage.png";
            case SensorType.Energy: return "battery.png";
            case SensorType.Clock: return "clock.png";
            case SensorType.Load: return "load.png";
            case SensorType.Temperature: return "temperature.png";
            case SensorType.Fan: return "fan.png";
            case SensorType.Flow: return "flow.png";
            case SensorType.Control: return "control.png";
            case SensorType.Level: return "level.png";
            case SensorType.Power: return "power.png";
            case SensorType.Data: return "data.png";
            case SensorType.Factor: return "factor.png";
            case SensorType.Frequency: return "clock.png";
            case SensorType.Throughput: return "throughput.png";
            case SensorType.TimeSpan: return "time.png";
            case SensorType.Timing: return "time.png";
            case SensorType.Noise: return "loudspeaker.png";
            case SensorType.Conductivity: return "voltage.png";
            case SensorType.Humidity: return "humidity.png";
            default: return "chip.png";
        }
    }
}
