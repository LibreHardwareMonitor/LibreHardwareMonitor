// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using LibreHardwareMonitor.Utilities;

namespace LibreHardwareMonitor.UI;

public enum TemperatureUnit
{
    Celsius = 0,
    Fahrenheit = 1
}

public class UnitManager
{

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
}