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
    private const float BytesPerGigaByte = 1024f * 1024f * 1024f;
    private const float BytesPerMegaByte = 1024f * 1024f;

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
    /// Converts a Data sensor value to the GB (2^30 bytes) it is displayed in.
    /// </summary>
    public static float? BytesToGigaBytes(float? valueInBytes)
    {
        return valueInBytes / BytesPerGigaByte;
    }

    /// <summary>
    /// Converts a SmallData sensor value to the MB (2^20 bytes) it is displayed in.
    /// </summary>
    public static float? BytesToMegaBytes(float? valueInBytes)
    {
        return valueInBytes / BytesPerMegaByte;
    }
}
