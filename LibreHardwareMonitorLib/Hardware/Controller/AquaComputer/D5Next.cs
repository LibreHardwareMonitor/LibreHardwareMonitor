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
    private readonly Sensor[] _rpmSensors = new Sensor[1];
    private readonly HidStream _stream;
    private readonly Sensor[] _temperatures = new Sensor[1];

    public D5Next(HidDevice dev, ISettings settings) : base("D5Next", new Identifier(dev.DevicePath), settings)
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
    }
}
