// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Controller.AquaComputer;

internal sealed class D5Next : Hardware
{
    //Available Reports, found them by looking at the below methods
    //var test = dev.GetRawReportDescriptor();
    //var test2 = dev.GetReportDescriptor();

    // ID 1; Length 158; INPUT
    // ID 2; Length 11; OUTPUT
    // ID 3; Length 1025; <-- works FEATURE
    // ID 8; Length 1025; <-- works FEATURE
    // ID 12; Length 1025; <-- 0xC FEATURE

    private readonly byte[] _rawData = new byte[1025];
    private readonly Sensor[] _rpmSensors = new Sensor[2];
    private readonly HidStream _stream;
    private readonly Sensor[] _temperatures = new Sensor[1];
    private readonly Sensor[] _voltages = new Sensor[4];
    private readonly Sensor[] _powers = new Sensor[2];
    private readonly Sensor[] _flows = new Sensor[1];
    private readonly Sensor[] _fanControl = new Sensor[2];

    public D5Next(HidDevice dev, ISettings settings) : base("D5Next", new Identifier(dev), settings)
    {
        if (dev.TryOpen(out _stream))
        {
            //Reading output report instead of feature report, as the measurements are in the output report
            _stream.Read(_rawData);

            Name = "D5Next";
            FirmwareVersion = Convert.ToUInt16(_rawData[14] | (_rawData[13] << 8));
            _temperatures[0] = new Sensor("Water Temperature", 0, SensorType.Temperature, this, Array.Empty<ParameterDescription>(), settings);
            ActivateSensor(_temperatures[0]);

            _rpmSensors[0] = new Sensor("Pump", 0, SensorType.Fan, this, Array.Empty<ParameterDescription>(), settings);
            ActivateSensor(_rpmSensors[0]);

            _rpmSensors[1] = new Sensor("Fan", 1, SensorType.Fan, this, settings);
            ActivateSensor(_rpmSensors[1]);

            _voltages[0] = new Sensor("Fan Voltage", 0, SensorType.Voltage, this, settings);
            ActivateSensor(_voltages[0]);

            _voltages[1] = new Sensor("Pump Voltage", 1, SensorType.Voltage, this, settings);
            ActivateSensor(_voltages[1]);

            _voltages[2] = new Sensor("+5V Voltage", 2, SensorType.Voltage, this, settings);
            ActivateSensor(_voltages[2]);

            _voltages[3] = new Sensor("+12V Voltage", 3, SensorType.Voltage, this, settings);
            ActivateSensor(_voltages[3]);
           
            _powers[0] = new Sensor("Fan Power", 0, SensorType.Power, this, settings);
            ActivateSensor(_powers[0]);

            _powers[1] = new Sensor("Pump Power", 1, SensorType.Power, this, settings);
            ActivateSensor(_powers[1]);

            _flows[0] = new Sensor("Viritual Flow", 0, SensorType.Flow, this, settings);
            ActivateSensor(_flows[0]);

            _fanControl[0] = new Sensor("Fan Control", 0, SensorType.Control, this, settings);
            ActivateSensor(_fanControl[0]);

            _fanControl[1] = new Sensor("Pump Control", 1, SensorType.Control, this, settings);
            ActivateSensor(_fanControl[1]);
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
        //Reading output report instead of feature report, as the measurements are in the output report
        _stream.Read(_rawData);
        _temperatures[0].Value = (_rawData[88] | (_rawData[87] << 8)) / 100f; //Water Temp
        _rpmSensors[0].Value = _rawData[117] | (_rawData[116] << 8); //Pump RPM
        _rpmSensors[1].Value = ReadUInt16BE(_rawData, 103); //Fan RPM
        _voltages[0].Value = ReadUInt16BE(_rawData, 97) / 100f; //Fan Voltage
        _voltages[1].Value = ReadUInt16BE(_rawData, 110) / 100f; //Pump Voltage
        _voltages[2].Value = ReadUInt16BE(_rawData, 57) / 100f; //+5V Voltage
        _voltages[3].Value = ReadUInt16BE(_rawData, 55) / 100f; //+12V Voltage
        _powers[0].Value = ReadUInt16BE(_rawData, 101) / 100f; //Fan Power Consumption
        _powers[1].Value = ReadUInt16BE(_rawData, 114) / 100f; //Pump Power Consumption
        _flows[0].Value = ReadUInt16BE(_rawData, 89) / 10f; // Viritual Flow
        _fanControl[0].Value = ReadUInt16BE(_rawData, 95) / 100f; // Fan Control in % (0-100)
        _fanControl[1].Value = ReadUInt16BE(_rawData, 108) / 100f; // Pump Control in % (0-100)
    }

    private ushort ReadUInt16BE(byte[] value, int startIndex)
    {
        return (ushort)(value[startIndex + 1] | (value[startIndex] << 8));
    }
}
