// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Controller.AquaComputer;
//TODO:
//Check tested and fix unknown variables in Update()
//Check if property "Variant" is valid interpreted
//Implement Fan Control in SetControl()

internal sealed class AquastreamXT : Hardware
{
    private readonly Sensor _fanControl;
    private readonly Sensor[] _frequencies = new Sensor[2];
    private readonly Sensor _pumpFlow;
    private readonly Sensor _pumpPower;
    private readonly byte[] _rawData = new byte[64];
    private readonly Sensor[] _rpmSensors = new Sensor[2];
    private readonly HidStream _stream;
    private readonly Sensor[] _temperatures = new Sensor[3];
    private readonly Sensor[] _voltages = new Sensor[2];

    public AquastreamXT(HidDevice dev, ISettings settings) : base("Aquastream XT", new Identifier(dev.DevicePath), settings)
    {
        if (dev.TryOpen(out _stream))
        {
            do
            {
                _rawData[0] = 0x4;
                _stream.GetFeature(_rawData);
            }
            while (_rawData[0] != 0x4);

            Name = $"Aquastream XT {Variant}";
            FirmwareVersion = BitConverter.ToUInt16(_rawData, 50);

            _temperatures[0] = new Sensor("External Fan VRM", 0, SensorType.Temperature, this, Array.Empty<ParameterDescription>(), settings);
            ActivateSensor(_temperatures[0]);
            _temperatures[1] = new Sensor("External", 1, SensorType.Temperature, this, Array.Empty<ParameterDescription>(), settings);
            ActivateSensor(_temperatures[1]);
            _temperatures[2] = new Sensor("Internal Water", 2, SensorType.Temperature, this, Array.Empty<ParameterDescription>(), settings);
            ActivateSensor(_temperatures[2]);

            _voltages[0] = new Sensor("External Fan", 1, SensorType.Voltage, this, Array.Empty<ParameterDescription>(), settings);
            ActivateSensor(_voltages[0]);
            _voltages[1] = new Sensor("Pump", 2, SensorType.Voltage, this, Array.Empty<ParameterDescription>(), settings);
            ActivateSensor(_voltages[1]);

            _pumpPower = new Sensor("Pump", 0, SensorType.Power, this, Array.Empty<ParameterDescription>(), settings);
            ActivateSensor(_pumpPower);

            _pumpFlow = new Sensor("Pump", 0, SensorType.Flow, this, Array.Empty<ParameterDescription>(), settings);
            ActivateSensor(_pumpFlow);

            _rpmSensors[0] = new Sensor("External Fan", 0, SensorType.Fan, this, Array.Empty<ParameterDescription>(), settings);
            ActivateSensor(_rpmSensors[0]);
            _rpmSensors[1] = new Sensor("Pump", 1, SensorType.Fan, this, Array.Empty<ParameterDescription>(), settings);
            ActivateSensor(_rpmSensors[1]);

            _fanControl = new Sensor("External Fan", 0, SensorType.Control, this, Array.Empty<ParameterDescription>(), settings);
            _fanControl.Control = new Control(_fanControl, settings, 0, 100);

            ActivateSensor(_fanControl);
            _frequencies[0] = new Sensor("Pump Frequency", 0, SensorType.Frequency, this, Array.Empty<ParameterDescription>(), settings);
            ActivateSensor(_frequencies[0]);
            _frequencies[1] = new Sensor("Pump MaxFrequency", 1, SensorType.Frequency, this, Array.Empty<ParameterDescription>(), settings);
            ActivateSensor(_frequencies[1]);
        }
    }

    public ushort FirmwareVersion { get; private set; }

    public override HardwareType HardwareType
    {
        get { return HardwareType.Cooler; }
    }

    public string Status
    {
        get
        {
            FirmwareVersion = BitConverter.ToUInt16(_rawData, 50);
            return FirmwareVersion < 1008 ? $"Status: Untested Firmware Version {FirmwareVersion}! Please consider Updating to Version 1018" : "Status: OK";
        }
    }

    //TODO: Check if valid
    public string Variant
    {
        get
        {
            MODE mode = (MODE)_rawData[33];

            if (mode.HasFlag(MODE.MODE_PUMP_ADV))
                return "Ultra + Internal Flow Sensor";

            if (mode.HasFlag(MODE.MODE_FAN_CONTROLLER))
                return "Ultra";

            if (mode.HasFlag(MODE.MODE_FAN_AMP))
                return "Advanced";

            return "Standard";
        }
    }

    public override void Close()
    {
        _stream.Close();

        base.Close();
    }

    //TODO: Check tested and fix unknown variables
    public override void Update()
    {
        try
        {
            _rawData[0] = 0x4;
            _stream.GetFeature(_rawData);
        }
        catch (IOException)
        {
            return;
        }
       

        if (_rawData[0] != 0x4)
            return;

        //var rawSensorsFan = BitConverter.ToUInt16(rawData, 1);                        //unknown - redundant?
        //var rawSensorsExt = BitConverter.ToUInt16(rawData, 3);                        //unknown - redundant?
        //var rawSensorsWater = BitConverter.ToUInt16(rawData, 5);                      //unknown - redundant?

        _voltages[0].Value = BitConverter.ToUInt16(_rawData, 7) / 61f; //External Fan Voltage: tested - OK
        _voltages[1].Value = BitConverter.ToUInt16(_rawData, 9) / 61f; //Pump Voltage: tested - OK
        _pumpPower.Value = _voltages[1].Value * BitConverter.ToInt16(_rawData, 11) / 625f; //Pump Voltage * Pump Current: tested - OK

        _temperatures[0].Value = BitConverter.ToUInt16(_rawData, 13) / 100f; //External Fan VRM Temperature: untested
        _temperatures[1].Value = BitConverter.ToUInt16(_rawData, 15) / 100f; //External Temperature Sensor: untested
        _temperatures[2].Value = BitConverter.ToUInt16(_rawData, 17) / 100f; //Internal Water Temperature Sensor: tested - OK

        _frequencies[0].Value = (1f / BitConverter.ToInt16(_rawData, 19)) * 750000; //Pump Frequency: tested - OK
        _rpmSensors[1].Value = _frequencies[0].Value * 60f; //Pump RPM: tested - OK
        _frequencies[1].Value = (1f / BitConverter.ToUInt16(_rawData, 21)) * 750000; //Pump Max Frequency: tested - OK

        _pumpFlow.Value = BitConverter.ToUInt32(_rawData, 23); //Internal Pump Flow Sensor: unknown

        _rpmSensors[0].Value = BitConverter.ToUInt32(_rawData, 27); //External Fan RPM: untested

        _fanControl.Value = 100f / byte.MaxValue * _rawData[31]; //External Fan Control: tested, External Fan Voltage scales by this value - OK
    }

    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private enum MODE : byte
    {
        MODE_PUMP_ADV = 1,
        MODE_FAN_AMP = 2,
        MODE_FAN_CONTROLLER = 4
    }
}
