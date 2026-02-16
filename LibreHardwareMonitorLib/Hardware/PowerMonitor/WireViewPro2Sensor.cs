// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using LibreHardwareMonitor.Interop.PowerMonitor;

namespace LibreHardwareMonitor.Hardware.PowerMonitor;

internal delegate float GetWireViewPro2SensorValue(DeviceData wvp);

internal class WireViewPro2Sensor : Sensor
{
    readonly GetWireViewPro2SensorValue _getValue;

    public WireViewPro2Sensor(string name, int index, SensorType sensorType, Hardware hardware, ISettings settings, GetWireViewPro2SensorValue getValue)
        : base(name, index, sensorType, hardware, settings)
    {
        _getValue = getValue;
    }

    internal void Update(DeviceData wvp)
    {
        float value = _getValue(wvp);

        Value = value;
    }
}
