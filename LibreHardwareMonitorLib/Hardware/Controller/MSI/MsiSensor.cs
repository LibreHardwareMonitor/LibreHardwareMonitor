// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) Florian K. (Blacktempel)
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware.Controller.MSI;

internal delegate float GetMsiSensorValue(MsiFanControl msi);

internal class MsiSensor : Sensor
{
    private readonly GetMsiSensorValue _getValue;

    public MsiSensor(string name, int index, SensorType sensorType, Hardware hardware, ISettings settings, GetMsiSensorValue getValue)
        : base(name, index, sensorType, hardware, settings)
    {
        _getValue = getValue;
    }

    internal void Update(MsiFanControl msi)
    {
        float value = _getValue(msi);

        Value = value;
    }
}
