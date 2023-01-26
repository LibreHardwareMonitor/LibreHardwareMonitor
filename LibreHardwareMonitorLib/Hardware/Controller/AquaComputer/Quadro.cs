// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Controller.AquaComputer;

internal sealed class Quadro : Hardware
{
    private readonly byte[] _rawData = new byte[210];
    private readonly HidStream _stream;

    private readonly Sensor[] _rpmSensors = new Sensor[4];
    private readonly Sensor[] _temperatures = new Sensor[4];
    private readonly Sensor[] _voltages = new Sensor[5];
    private readonly Sensor[] _currents = new Sensor[4];
    private readonly Sensor[] _powers = new Sensor[4];
    private readonly Sensor[] _flows = new Sensor[1];

    public Quadro(HidDevice dev, ISettings settings) : base("Quadro", new Identifier(dev.DevicePath), settings)
    {
        if (dev.TryOpen(out _stream))
        {
            //Reading output report instead of feature report, as the measurements are in the output report
            _stream.Read(_rawData);
                
            Name = "QUADRO";
            FirmwareVersion = GetConvertedValue(QuadroDataIndexes.FIRMWARE_VERSION).GetValueOrDefault(0);

            // Initialize the 4 temperature sensors
            for (int i = 0; i < 4; i++)
            {
                _temperatures[i] = new Sensor($"Temperature {i+1}", i, SensorType.Temperature, this, Array.Empty<ParameterDescription>(), settings);
                ActivateSensor(_temperatures[i]);
            }

            // Initialize the input voltage sensor
            _voltages[0] = new Sensor("Input", 0, SensorType.Voltage, this, Array.Empty<ParameterDescription>(), settings);
            ActivateSensor(_voltages[0]);

            // Initialize the flow sensor
            _flows[0] = new Sensor("Flow", 0, SensorType.Flow, this, Array.Empty<ParameterDescription>(), settings);
            ActivateSensor(_flows[0]);

            // Initialize the 4 fan voltage sensors
            for (int i = 1; i < 5; i++)
            {
                _voltages[i] = new Sensor($"Fan {i}", i, SensorType.Voltage, this, Array.Empty<ParameterDescription>(), settings);
                ActivateSensor(_voltages[i]);
            }
                
            // Initialize the 4 fan current sensors
            for (int i = 0; i < 4; i++)
            {
                _currents[i] = new Sensor($"Fan {i+1}", i, SensorType.Current, this, Array.Empty<ParameterDescription>(), settings);
                ActivateSensor(_currents[i]);
            }
                
            // Initialize the 4 fan power sensors
            for (int i = 0; i < 4; i++)
            {
                _powers[i] = new Sensor($"Fan {i+1}", i, SensorType.Power, this, Array.Empty<ParameterDescription>(), settings);
                ActivateSensor(_powers[i]);
            }

            // Initialize the 4 fan speed sensors
            for (int i = 0; i < 4; i++)
            {
                _rpmSensors[i] = new Sensor($"Fan {i + 1}", i, SensorType.Fan, this, Array.Empty<ParameterDescription>(), settings);
                ActivateSensor(_rpmSensors[i]);
            }

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
            
        _temperatures[0].Value = GetConvertedValue(QuadroDataIndexes.TEMP_1) / 100f; // Temp 1
        _temperatures[1].Value = GetConvertedValue(QuadroDataIndexes.TEMP_2) / 100f; // Temp 2
        _temperatures[2].Value = GetConvertedValue(QuadroDataIndexes.TEMP_3) / 100f; // Temp 3
        _temperatures[3].Value = GetConvertedValue(QuadroDataIndexes.TEMP_4) / 100f; // Temp 4

        _voltages[0].Value = GetConvertedValue(QuadroDataIndexes.VOLTAGE) / 100f; // Input voltage

        _flows[0].Value = GetConvertedValue(QuadroDataIndexes.FLOW) / 10f; // Flow

        _voltages[1].Value = GetConvertedValue(QuadroDataIndexes.FAN_VOLTAGE_1) / 100f; // Fan 1 voltage
        _voltages[2].Value = GetConvertedValue(QuadroDataIndexes.FAN_VOLTAGE_2) / 100f; // Fan 2 voltage
        _voltages[3].Value = GetConvertedValue(QuadroDataIndexes.FAN_VOLTAGE_3) / 100f; // Fan 3 voltage
        _voltages[4].Value = GetConvertedValue(QuadroDataIndexes.FAN_VOLTAGE_4) / 100f; // Fan 4 voltage

        _currents[0].Value = GetConvertedValue(QuadroDataIndexes.FAN_CURRENT_1) / 1000f; // Fan 1 current
        _currents[1].Value = GetConvertedValue(QuadroDataIndexes.FAN_CURRENT_2) / 1000f; // Fan 2 current
        _currents[2].Value = GetConvertedValue(QuadroDataIndexes.FAN_CURRENT_3) / 1000f; // Fan 3 current
        _currents[3].Value = GetConvertedValue(QuadroDataIndexes.FAN_CURRENT_4) / 1000f; // Fan 4 current

        _powers[0].Value = GetConvertedValue(QuadroDataIndexes.FAN_POWER_1) / 100f; // Fan 1 power
        _powers[1].Value = GetConvertedValue(QuadroDataIndexes.FAN_POWER_2) / 100f; // Fan 2 power
        _powers[2].Value = GetConvertedValue(QuadroDataIndexes.FAN_POWER_3) / 100f; // Fan 3 power
        _powers[3].Value = GetConvertedValue(QuadroDataIndexes.FAN_POWER_4) / 100f; // Fan 4 power

        _rpmSensors[0].Value = GetConvertedValue(QuadroDataIndexes.FAN_SPEED_1); // Fan 1 speed
        _rpmSensors[1].Value = GetConvertedValue(QuadroDataIndexes.FAN_SPEED_2); // Fan 2 speed
        _rpmSensors[2].Value = GetConvertedValue(QuadroDataIndexes.FAN_SPEED_3); // Fan 3 speed
        _rpmSensors[3].Value = GetConvertedValue(QuadroDataIndexes.FAN_SPEED_4); // Fan 4 speed
            
    }

    private sealed class QuadroDataIndexes
    {
        public const int FIRMWARE_VERSION = 13;
            
        public const int TEMP_1 = 52;
        public const int TEMP_2 = 54;
        public const int TEMP_3 = 56;
        public const int TEMP_4 = 58;

        public const int VOLTAGE = 108;

        public const int FLOW = 110;

        public const int FAN_VOLTAGE_1 = 114;
        public const int FAN_VOLTAGE_2 = 127;
        public const int FAN_VOLTAGE_3 = 140;
        public const int FAN_VOLTAGE_4 = 153;

        public const int FAN_CURRENT_1 = 116;
        public const int FAN_CURRENT_2 = 129;
        public const int FAN_CURRENT_3 = 142;
        public const int FAN_CURRENT_4 = 155;

        public const int FAN_POWER_1 = 118;
        public const int FAN_POWER_2 = 131;
        public const int FAN_POWER_3 = 144;
        public const int FAN_POWER_4 = 157;

        public const int FAN_SPEED_1 = 120;
        public const int FAN_SPEED_2 = 133;
        public const int FAN_SPEED_3 = 146;
        public const int FAN_SPEED_4 = 159;

    }

    private ushort? GetConvertedValue(int index)
    {
        if (_rawData[index] == sbyte.MaxValue)
            return null;
            
        return Convert.ToUInt16(_rawData[index + 1] | (_rawData[index] << 8));
    }
}
