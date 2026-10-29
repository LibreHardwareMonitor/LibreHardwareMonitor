// SPDX-License-Identifier: MPL-2.0

using System.Globalization;
using LibreHardwareMonitor.Hardware;
using LanguageResources = LibreHardwareMonitor.Avalonia.Localization.Resources.Resources;

namespace LibreHardwareMonitor.Avalonia.Mvvm.Formatting;

/// <summary>
/// Formats sensor values for the selected user interface language.
/// </summary>
public static class SensorValueFormatter
{
    /// <summary>
    /// Formats a sensor value with the unit for its measurement type.
    /// </summary>
    /// <param name="sensorType">The sensor measurement type.</param>
    /// <param name="value">The value to format, when available.</param>
    /// <returns>The localized sensor value text.</returns>
    public static string Format(SensorType? sensorType, float? value)
    {
        if (!sensorType.HasValue)
            return string.Empty;

        if (!value.HasValue)
            return LanguageResources.ValueUnavailable;

        if (sensorType == SensorType.Data)
            return FormatBytes(value.Value, false);

        if (sensorType == SensorType.Throughput)
            return FormatBytes(value.Value, true);

        if (sensorType == SensorType.TimeSpan)
            return TimeSpan.FromSeconds(value.Value).ToString("g", CultureInfo.CurrentCulture);

        string format;

        switch (sensorType)
        {
            case SensorType.Voltage: format = LanguageResources.ValueVoltageFormat; break;
            case SensorType.Current: format = LanguageResources.ValueCurrentFormat; break;
            case SensorType.Power: format = LanguageResources.ValuePowerFormat; break;
            case SensorType.Clock: format = LanguageResources.ValueClockFormat; break;
            case SensorType.Temperature: format = LanguageResources.ValueTemperatureFormat; break;
            case SensorType.Load:
            case SensorType.Control:
            case SensorType.Level: format = LanguageResources.ValuePercentFormat; break;
            case SensorType.Fan: format = LanguageResources.ValueFanFormat; break;
            case SensorType.Flow: format = LanguageResources.ValueFlowFormat; break;
            case SensorType.Factor: format = LanguageResources.ValueFactorFormat; break;
            case SensorType.Frequency: format = LanguageResources.ValueFrequencyFormat; break;
            case SensorType.Timing: format = LanguageResources.ValueTimingFormat; break;
            case SensorType.Energy: format = LanguageResources.ValueEnergyFormat; break;
            case SensorType.Noise: format = LanguageResources.ValueNoiseFormat; break;
            case SensorType.Conductivity: format = LanguageResources.ValueConductivityFormat; break;
            case SensorType.Humidity: format = LanguageResources.ValueHumidityFormat; break;
            default: format = LanguageResources.ValueNumberFormat; break;
        }

        return string.Format(CultureInfo.CurrentCulture, format, value.Value);
    }

    /// <summary>
    /// Scales a byte count and formats it as data or throughput.
    /// </summary>
    /// <param name="value">The byte count to format.</param>
    /// <param name="perSecond">Whether the value represents bytes per second.</param>
    /// <returns>The localized scaled byte count.</returns>
    private static string FormatBytes(float value, bool perSecond)
    {
        string[] units =
        {
            LanguageResources.UnitBytes,
            LanguageResources.UnitKilobytes,
            LanguageResources.UnitMegabytes,
            LanguageResources.UnitGigabytes,
            LanguageResources.UnitTerabytes,
            LanguageResources.UnitPetabytes
        };

        double amount = value;
        int unit = 0;

        while (Math.Abs(amount) >= 1024 && unit < units.Length - 1)
        {
            amount /= 1024;
            unit++;
        }

        string format = perSecond ? LanguageResources.ValueThroughputFormat : LanguageResources.ValueDataFormat;

        return string.Format(CultureInfo.CurrentCulture, format, amount, units[unit]);
    }
}
