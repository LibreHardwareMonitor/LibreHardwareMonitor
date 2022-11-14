// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Management.Instrumentation;
using LibreHardwareMonitor.Hardware;

namespace LibreHardwareMonitor.Wmi;

[InstrumentationClass(InstrumentationType.Instance)]
public class Sensor : IWmiObject
{
    private readonly ISensor _sensor;

    #region WMI Exposed

    public string SensorType { get; }
    public string Identifier { get; }
    public string Parent { get; }
    public string Name { get; }
    public float Value { get; private set; }
    public float Min { get; private set; }
    public float Max { get; private set; }
    public int Index { get; }

    #endregion

    public Sensor(ISensor sensor)
    {
        Name = sensor.Name;
        Index = sensor.Index;

        SensorType = sensor.SensorType.ToString();
        Identifier = sensor.Identifier.ToString();
        Parent = sensor.Hardware.Identifier.ToString();

        _sensor = sensor;
    }

    public void Update()
    {
        Value = _sensor.Value ?? 0;

        if (_sensor.Min != null)
            Min = (float)_sensor.Min;

        if (_sensor.Max != null)
            Max = (float)_sensor.Max;
    }
}