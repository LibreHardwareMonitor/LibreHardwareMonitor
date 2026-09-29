// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using LibreHardwareMonitor.Windows.Forms.Utilities;

namespace LibreHardwareMonitor.Windows.Forms.UI;

public enum TemperatureUnit
{
    Celsius = 0,
    Fahrenheit = 1
}

public class UnitManager
{
    private const float BytesPerKiloByte = 1024f;
    private const float BytesPerMegaByte = 1024f * 1024f;
    private const float BytesPerGigaByte = 1024f * 1024f * 1024f;
    private const float BytesPerTeraByte = 1024f * 1024f * 1024f * 1024f;

    private readonly PersistentSettings _settings;
    private TemperatureUnit _temperatureUnit;

    public UnitManager(PersistentSettings settings)
    {
        _settings = settings;
        _temperatureUnit = (TemperatureUnit)settings.GetValue("TemperatureUnit", (int)TemperatureUnit.Celsius);
    }

    public TemperatureUnit TemperatureUnit
    {
        get { return _temperatureUnit; }
        set
        {
            _temperatureUnit = value;
            _settings.SetValue("TemperatureUnit", (int)_temperatureUnit);
        }
    }

    public static float? CelsiusToFahrenheit(float? valueInCelsius)
    {
        return valueInCelsius * 1.8f + 32;
    }

    /// <summary>
    /// Converts a value in bytes to GB (2^30 bytes)
    /// </summary>
    public static float? BytesToGigaBytes(float? valueInBytes)
    {
        return valueInBytes / BytesPerGigaByte;
    }

    /// <summary>
    /// Scales and formats data, starting at KB.
    /// </summary>
    public static string BytesToString(float? valueInBytes)
    {
        return FormatScaledBytes(valueInBytes, "B");
    }

    /// <summary>
    /// Scales and formats data rate, starting at KB/s.
    /// </summary>
    public static string BytesPerSecondToString(float? valueInBytesPerSecond)
    {
        return FormatScaledBytes(valueInBytesPerSecond, "B/s");
    }

    private static string FormatScaledBytes(float? value, string unit)
    {
        if (!value.HasValue)
            return "-";

        if (value < BytesPerMegaByte)
            return $"{value / BytesPerKiloByte:F1} K{unit}";

        if (value < BytesPerGigaByte)
            return $"{value / BytesPerMegaByte:F1} M{unit}";

        if (value < BytesPerTeraByte)
            return $"{value / BytesPerGigaByte:F1} G{unit}";

        return $"{value / BytesPerTeraByte:F1} T{unit}";
    }
}
