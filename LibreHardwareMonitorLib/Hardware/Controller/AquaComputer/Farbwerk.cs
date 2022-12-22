// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Linq;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Controller.AquaComputer;
//TODO:
//Implement set RGB Controls

internal sealed class Farbwerk : Hardware
{
    private const int FeatureID = 3;

    private const int HeaderSize = 27;
    private const int SensorOffset = 20;
    private const int ColorsOffset = 40;

    private const int TemperatureCount = 4;
    private const int ColorCount = 4;
    private const int ColorValueCount = ColorCount * 3;

    private readonly byte[] _rawData = new byte[140];
    private readonly HidStream _stream;
    private readonly Sensor[] _temperatures = new Sensor[TemperatureCount];
    private readonly Sensor[] _colorSensors = new Sensor[ColorValueCount];

    public Farbwerk(HidDevice dev, ISettings settings) : base("Farbwerk", new Identifier(dev.DevicePath), settings)
    {
        if (dev.TryOpen(out _stream))
        {
            _stream.Read(_rawData);

            byte[] firmwareBytes = new byte[2] { _rawData[22], _rawData[21] };
            FirmwareVersion = BitConverter.ToUInt16(firmwareBytes, 0);

            for (int i = _temperatures.Length - 1; i >= 0; i--) {
                _temperatures[i] = new Sensor($"Sensor {i+1}", 0, SensorType.Temperature, this, Array.Empty<ParameterDescription>(), settings);
                ActivateSensor(_temperatures[i]);
            }

            for (int i = _colorSensors.Length - 1; i >= 0; i--)
            {
                int control = (i / 3) + 1;
                string color = (i % 3) switch {
                    0 => "Red",
                    1 => "Green",
                    2 => "Blue",
                    _ => "Invalid"
                };
                _colorSensors[i] = new Sensor($"Controller {control} {color}", 0, SensorType.Level, this, Array.Empty<ParameterDescription>(), settings);
                ActivateSensor(_colorSensors[i]);
            }
        }
    }

    public ushort FirmwareVersion { get; private set; }

    public override HardwareType HardwareType
    {
        get { return HardwareType.EmbeddedController; }
    }

    public string Status
    {
        get
        {
            FirmwareVersion = BitConverter.ToUInt16(_rawData, 50);
            return FirmwareVersion < 1009 ? $"Status: Untested Firmware Version {FirmwareVersion}! Please consider Updating to Version 1009" : "Status: OK";
        }
    }

    public override void Close()
    {
        _stream.Close();

        base.Close();
    }

    public override void Update()
    {
        int length = _stream.Read(_rawData);

        if (length != _rawData.Length || _rawData[0] != 0x1)
        {
            return;
        }

        byte[] sensors = _rawData.Skip(HeaderSize + SensorOffset).Take(2 * _temperatures.Length).ToArray();
        for (int i = 0; i < _temperatures.Length; i++) {
            Array.Reverse(sensors, i * 2, 2);
            _temperatures[i].Value = BitConverter.ToUInt16(sensors, i * 2) / 100.0f;
        }

        byte[] colors = _rawData.Skip(HeaderSize + ColorsOffset).Take(2 * _colorSensors.Length).ToArray();
        for (int i = 0; i < _colorSensors.Length; i++)
        {
            Array.Reverse(colors, i * 2, 2);
            _colorSensors[i].Value = BitConverter.ToUInt16(colors, i * 2) / 81.90f;
        }
    }
}
