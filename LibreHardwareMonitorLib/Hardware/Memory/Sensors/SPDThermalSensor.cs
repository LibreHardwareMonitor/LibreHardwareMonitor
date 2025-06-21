// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using RAMSPDToolkit.SPD.Interfaces;

namespace LibreHardwareMonitor.Hardware.Memory.Sensors
{
    internal class SPDThermalSensor : Sensor
    {
        private IThermalSensor _thermalSensor;

        public SPDThermalSensor(string name, int index, SensorType sensorType, Hardware hardware, ISettings settings, IThermalSensor thermalSensor)
            : base(name, index, sensorType, hardware, settings)
        {
            _thermalSensor = thermalSensor;
        }

        public bool UpdateSensor()
        {
            if (!_thermalSensor.UpdateTemperature())
            {
                return false;
            }

            Value = _thermalSensor.Temperature;

            return true;
        }
    }
}
