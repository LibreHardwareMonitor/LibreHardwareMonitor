// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) 2020 Wilken Gottwalt<wilken.gottwalt@posteo.net>
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.
// Implemented after the Linux kernel driver corsair_psu by Wilken Gottwalt and contributers

using System.Collections.Generic;
using System.Globalization;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Psu.Corsair;

internal sealed class CorsairPsu : Hardware
{
    private readonly List<CompositeSensor> _compositeSensors = [];
    private readonly HidDevice _device;
    private readonly List<PsuSensor> _sensors = [];

    public CorsairPsu(HidDevice device, ISettings settings, int index)
        : base("Corsair PSU", new Identifier("psu", "corsair", index.ToString()), settings)
    {
        _device = device;
        using HidStream stream = device.Open();

        UsbApi.Init(stream);
        UsbApi.FirmwareInfo fwInfo = UsbApi.FwInfo(stream);
        Name = $"{CultureInfo.InvariantCulture.TextInfo.ToTitleCase(fwInfo.Vendor.ToLowerInvariant())} {fwInfo.Product}";

        AddSensors(UsbApi.GetOptionalCommands(stream), UsbApi.GetCriticals(stream), settings);
    }

    public override HardwareType HardwareType => HardwareType.Psu;

    public override IDictionary<string, string> Properties
    {
        get
        {
            SortedDictionary<string, string> properties = new();

            using HidStream stream = _device.Open();

            float? mode = UsbApi.GetValue(stream, UsbApi.Command.OCPMODE, 0);
            if (mode.HasValue)
                properties.Add("Over-current protection", mode > 1.0 ? "multi-rail" : "single-rail");

            return properties;
        }
    }

    public override void Update()
    {
        using HidStream stream = _device.Open();
        _sensors.ForEach(s => s.Update(stream));
    }

    private void AddSensors(UsbApi.OptionalCommands optionalCommands, UsbApi.Criticals criticals, ISettings settings)
    {
        int sensorIndex = 0;

        _sensors.Add(new PsuSensorWithLimits("VRM",
                                             sensorIndex++,
                                             SensorType.Temperature,
                                             this,
                                             settings,
                                             UsbApi.Command.TEMP0,
                                             null,
                                             criticals.TempMax[0]));

        _sensors.Add(new PsuSensorWithLimits("Case",
                                             sensorIndex++,
                                             SensorType.Temperature,
                                             this,
                                             settings,
                                             UsbApi.Command.TEMP1,
                                             null,
                                             criticals.TempMax[1]));

        _sensors.Add(new PsuSensor("Case", sensorIndex++, SensorType.Fan, this, settings, UsbApi.Command.FAN_RPM));

        _sensors.Add(new PsuSensor("Input", sensorIndex++, SensorType.Voltage, this, settings, UsbApi.Command.IN_VOLTS));
        _sensors.Add(new PsuSensorWithLimits("+12V",
                                             sensorIndex++,
                                             SensorType.Voltage,
                                             this,
                                             settings,
                                             UsbApi.Command.RAIL_VOLTS,
                                             criticals.VoltageMin[(byte)Rail._12V],
                                             criticals.VoltageMax[(byte)Rail._12V]));

        _sensors.Add(new PsuSensorWithLimits("+5V",
                                             sensorIndex++,
                                             SensorType.Voltage,
                                             this,
                                             settings,
                                             UsbApi.Command.RAIL_VOLTS,
                                             criticals.VoltageMin[(byte)Rail._5V],
                                             criticals.VoltageMax[(byte)Rail._5V],
                                             Rail._5V));

        _sensors.Add(new PsuSensorWithLimits("+3.3V",
                                             sensorIndex++,
                                             SensorType.Voltage,
                                             this,
                                             settings,
                                             UsbApi.Command.RAIL_VOLTS,
                                             criticals.VoltageMin[(byte)Rail._3V],
                                             criticals.VoltageMax[(byte)Rail._3V],
                                             Rail._3V));

        if (optionalCommands.HasFlag(UsbApi.OptionalCommands.InputCurrent))
        {
            _sensors.Add(new PsuSensor("Input", sensorIndex++, SensorType.Current, this, settings, UsbApi.Command.IN_AMPS));
        }

        _sensors.Add(new PsuSensorWithLimits("+12V",
                                             sensorIndex++,
                                             SensorType.Current,
                                             this,
                                             settings,
                                             UsbApi.Command.RAIL_AMPS,
                                             null,
                                             criticals.CurrentMax[(byte)Rail._12V]));

        _sensors.Add(new PsuSensorWithLimits("+5V",
                                             sensorIndex++,
                                             SensorType.Current,
                                             this,
                                             settings,
                                             UsbApi.Command.RAIL_AMPS,
                                             null,
                                             criticals.CurrentMax[(byte)Rail._5V],
                                             Rail._5V));

        _sensors.Add(new PsuSensorWithLimits("+3.3V",
                                             sensorIndex++,
                                             SensorType.Current,
                                             this,
                                             settings,
                                             UsbApi.Command.RAIL_AMPS,
                                             null,
                                             criticals.CurrentMax[(byte)Rail._3V],
                                             Rail._3V));

        PsuSensor[] powerRails =
        [
            new("+12V", sensorIndex++, SensorType.Power, this, settings, UsbApi.Command.RAIL_WATTS),
            new("+5V", sensorIndex++, SensorType.Power, this, settings, UsbApi.Command.RAIL_WATTS, Rail._5V),
            new("+3.3V", sensorIndex++, SensorType.Power, this, settings, UsbApi.Command.RAIL_WATTS, Rail._3V)
        ];

        _sensors.AddRange(powerRails);
        _sensors.Add(new PsuSensor("Total watts", sensorIndex++, SensorType.Power, this, settings, UsbApi.Command.TOTAL_WATTS));
        _compositeSensors.Add(new CompositeSensor("Total Output",
                                                  sensorIndex++,
                                                  SensorType.Power,
                                                  this,
                                                  settings,
                                                  powerRails,
                                                  (acc, sensor) => acc + sensor.Value ?? 0f));

        ActivateSensor(_compositeSensors[_compositeSensors.Count - 1]);

        _sensors.Add(new PsuSensor("Uptime", sensorIndex++, SensorType.TimeSpan, this, settings, UsbApi.Command.UPTIME, Rail._12V, true));
        _sensors.Add(new PsuSensor("Total uptime", sensorIndex, SensorType.TimeSpan, this, settings, UsbApi.Command.TOTAL_UPTIME, Rail._12V, true));
    }

    private class PsuSensor : Sensor
    {
        private readonly UsbApi.Command _cmd;
        private readonly byte _rail;

        public PsuSensor(string name, int index, SensorType type, CorsairPsu hardware, ISettings settings, UsbApi.Command cmd, Rail rail = Rail._12V, bool noHistory = false)
            : base(name, index, false, type, hardware, null, settings, noHistory)
        {
            _cmd = cmd;
            _rail = (byte)rail;

            hardware.ActivateSensor(this);
        }

        public void Update(HidStream stream)
        {
            Value = UsbApi.GetValue(stream, _cmd, _rail);
        }
    }

    private class PsuSensorWithLimits
    (
        string name,
        int index,
        SensorType type,
        CorsairPsu hardware,
        ISettings settings,
        UsbApi.Command cmd,
        float? lowCritical,
        float? highCritical,
        Rail rail = Rail._12V)
        : PsuSensor(name, index, type, hardware, settings, cmd, rail), ICriticalSensorLimits
    {
        public float? CriticalHighLimit { get; } = highCritical;

        public float? CriticalLowLimit { get; } = lowCritical;
    }

    private enum Rail : byte
    {
        _12V = 0,
        _5V = 1,
        _3V = 2
    }
}
