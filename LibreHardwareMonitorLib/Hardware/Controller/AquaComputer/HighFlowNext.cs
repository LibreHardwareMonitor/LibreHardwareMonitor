// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Controller.AquaComputer;

internal sealed class HighFlowNext : Hardware
{
    private readonly byte[] _rawData = new byte[1025];
    private readonly HidStream _stream;
    private readonly Sensor[] _temperatures = new Sensor[2];
    private readonly Sensor[] _flows = new Sensor[1];
    private readonly Sensor[] _levels = new Sensor[1];
    private readonly Sensor[] _powers = new Sensor[1];
    private readonly Sensor[] _conductivities = new Sensor[1];
    private readonly Sensor[] _voltages = new Sensor[2];
    private readonly Sensor[] _alarms = new Sensor[4];

    public HighFlowNext(HidDevice dev, ISettings settings) : base("high flow NEXT", new Identifier("aquacomputer", "hfn", dev.GetSerialNumber().Replace(" ", "")), settings)
    {
        if (dev.TryOpen(out _stream))
        {
            // Reading output report instead of feature report, as the measurements are in the output report.
            _stream.Read(_rawData);

            FirmwareVersion = ReadUInt16BE(_rawData, 13);

            _temperatures[0] = new Sensor("Water Temperature", 0, SensorType.Temperature, this, settings);
            ActivateSensor(_temperatures[0]);

            _temperatures[1] = new Sensor("External Temperature", 1, SensorType.Temperature, this, settings);
            ActivateSensor(_temperatures[1]);

            _flows[0] = new Sensor("Flow", 0, SensorType.Flow, this, settings);
            ActivateSensor(_flows[0]);

            _levels[0] = new Sensor("Water Quality", 0, SensorType.Level, this, settings);
            ActivateSensor(_levels[0]);

            _powers[0] = new Sensor("Dissipated Power", 0, SensorType.Power, this, settings);
            ActivateSensor(_powers[0]);

            _conductivities[0] = new Sensor("Conductivity", 0, SensorType.Conductivity, this, settings);
            ActivateSensor(_conductivities[0]);

            _voltages[0] = new Sensor("VCC", 0, SensorType.Voltage, this, settings);
            ActivateSensor(_voltages[0]);

            _voltages[1] = new Sensor("VCC USB", 1, SensorType.Voltage, this, settings);
            ActivateSensor(_voltages[1]);

            _alarms[0] = new Sensor("Flow Alarm", 0, true, SensorType.Factor, this, null, settings);
            ActivateSensor(_alarms[0]);

            _alarms[1] = new Sensor("Water Temperature Alarm", 1, true, SensorType.Factor, this, null, settings);
            ActivateSensor(_alarms[0]);

            _alarms[2] = new Sensor("External Temperature Alarm", 2, true, SensorType.Factor, this, null, settings);
            ActivateSensor(_alarms[0]);

            _alarms[3] = new Sensor("Water Quality Alarm", 3, true, SensorType.Factor, this, null, settings);
            ActivateSensor(_alarms[0]);
        }
    }

    public ushort FirmwareVersion { get; }

    public override HardwareType HardwareType
    {
        get { return HardwareType.Cooler; }
    }

    public override void Close()
    {
        _stream.Close();
        base.Close();
    }

    public override void Update()
    {
        // Reading output report instead of feature report, as the measurements are in the output report.
        _stream.Read(_rawData);

        _temperatures[0].Value = ReadUInt16BE(_rawData, 85) / 100f; // Water Temperature

        // External Temperature.
        ushort rawExtTempValue = ReadUInt16BE(_rawData, 87);
        bool externalTempSensorConnected = rawExtTempValue != short.MaxValue;

        if (externalTempSensorConnected)
        {
            _temperatures[1].Value = rawExtTempValue / 100f; 
        }
        else
        {
            // No external temp sensor connected.
            _temperatures[1].Value = null;
        }

        _flows[0].Value = ReadUInt16BE(_rawData, 81) / 10f; // Flow


        _levels[0].Value = ReadUInt16BE(_rawData, 89) / 100f; // Water Quality

        // Dissipated Power.
        if (externalTempSensorConnected)
        {
            _powers[0].Value = ReadUInt16BE(_rawData, 91);
        }
        else
        {
            // Power calculation requires the external temp sensor to be connected.
            _powers[0].Value = null;
        }

        _conductivities[0].Value = ReadUInt16BE(_rawData, 95) / 10f; // Conductivity

        _voltages[0].Value = ReadUInt16BE(_rawData, 97) / 100f; // VCC
        _voltages[1].Value = ReadUInt16BE(_rawData, 99) / 100f; // VCC USB

        _alarms[0].Value = (_rawData[116] & 0x02) >> 1; // Flow alarm
        _alarms[1].Value = (_rawData[116] & 0x04) >> 2; // Water temperature alarm
        _alarms[2].Value = (_rawData[116] & 0x08) >> 3; // External temperature alarm
        _alarms[3].Value = (_rawData[116] & 0x10) >> 4; // Water quality alarm

        // Unused:
        // _rawData[101..104] -> Total pumped volume liters
        // _rawData[105..109] -> Internal impulse counter from flow meter
    }

    private ushort ReadUInt16BE(byte[] value, int startIndex)
    {
        return (ushort)(value[startIndex + 1] | (value[startIndex] << 8));
    }
}
