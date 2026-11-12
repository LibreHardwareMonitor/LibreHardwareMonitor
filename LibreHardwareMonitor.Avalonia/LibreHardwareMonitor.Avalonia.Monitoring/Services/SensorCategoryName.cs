// SPDX-License-Identifier: MPL-2.0

using LibreHardwareMonitor.Hardware;
using LanguageResources = LibreHardwareMonitor.Avalonia.Localization.Resources.Resources;

namespace LibreHardwareMonitor.Avalonia.Monitoring.Services;

/// <summary>
/// Provides localized names for groups of hardware sensors.
/// </summary>
public static class SensorCategoryName
{
    /// <summary>
    /// Gets the translated name for a sensor measurement type.
    /// </summary>
    /// <param name="sensorType">The sensor measurement type.</param>
    /// <returns>The translated category name.</returns>
    public static string GetName(SensorType sensorType)
    {
        switch (sensorType)
        {
            case SensorType.Voltage: return LanguageResources.GroupVoltages;
            case SensorType.Current: return LanguageResources.GroupCurrents;
            case SensorType.Energy: return LanguageResources.GroupCapacities;
            case SensorType.Clock: return LanguageResources.GroupClocks;
            case SensorType.Load: return LanguageResources.GroupLoad;
            case SensorType.Temperature: return LanguageResources.GroupTemperatures;
            case SensorType.Fan: return LanguageResources.GroupFans;
            case SensorType.Flow: return LanguageResources.GroupFlows;
            case SensorType.Control: return LanguageResources.GroupControls;
            case SensorType.Level: return LanguageResources.GroupLevels;
            case SensorType.Power: return LanguageResources.GroupPowers;
            case SensorType.Data: return LanguageResources.GroupData;
            case SensorType.Factor: return LanguageResources.GroupFactors;
            case SensorType.Frequency: return LanguageResources.GroupFrequencies;
            case SensorType.Throughput: return LanguageResources.GroupThroughput;
            case SensorType.TimeSpan: return LanguageResources.GroupTimes;
            case SensorType.Timing: return LanguageResources.GroupTimings;
            case SensorType.Noise: return LanguageResources.GroupNoise;
            case SensorType.Conductivity: return LanguageResources.GroupConductivities;
            case SensorType.Humidity: return LanguageResources.GroupHumidity;
            default: return LanguageResources.GroupOther;
        }
    }
}
