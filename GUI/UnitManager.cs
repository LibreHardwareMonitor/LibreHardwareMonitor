// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Collections.Generic;

namespace OpenHardwareMonitor.GUI
{

    public enum TemperatureUnit
    {
        Celsius = 0,
        Fahrenheit = 1
    }

    public class UnitManager
    {

        private PersistentSettings _settings;
        private TemperatureUnit _temperatureUnit;

        public UnitManager(PersistentSettings settings)
        {
            this._settings = settings;
            this._temperatureUnit = (TemperatureUnit)settings.GetValue("TemperatureUnit", (int)TemperatureUnit.Celsius);
        }

        public TemperatureUnit TemperatureUnit
        {
            get { return _temperatureUnit; }
            set
            {
                this._temperatureUnit = value;
                this._settings.SetValue("TemperatureUnit", (int)_temperatureUnit);
            }
        }

        public static float? CelsiusToFahrenheit(float? valueInCelsius)
        {
            return valueInCelsius * 1.8f + 32;
        }
    }
}
