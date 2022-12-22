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
            for (int i = 0; i < _temperatures.Length; i++) {
                _temperatures[i] = new Sensor($"Sensor {i+1}", i, SensorType.Temperature, this, settings);
                ActivateSensor(_temperatures[i]);
            }

            for (int i = 0; i < _colorSensors.Length; i++) {
                int control = (i / 3) + 1;
                string color = (i % 3) switch {
                    0 => "Red",
                    1 => "Green",
                    2 => "Blue",
                    _ => "Invalid"
                };
                _colorSensors[i] = new Sensor($"Controller {control} {color}", ColorCount + i, SensorType.Level, this, settings);
                ActivateSensor(_colorSensors[i]);
            }

            Update();
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
            if (_rawData[0] != 0x1) {
                return $"Status: Invalid header {_rawData[0]}";
            }

            if (FirmwareVersion < 1009)
            {
                return $"Status: Untested Firmware Version {FirmwareVersion}! Please consider Updating to Version 1009";
            }

            return "Status: OK";
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

        byte[] firmwareBytes = new byte[2] { _rawData[22], _rawData[21] };
        FirmwareVersion = BitConverter.ToUInt16(firmwareBytes, 0);

        byte[] sensorsBytes = _rawData.Skip(HeaderSize + SensorOffset).Take(2 * _temperatures.Length).ToArray();
        for (int i = 0; i < _temperatures.Length; i++) {
            Array.Reverse(sensorsBytes, i * 2, 2);
            _temperatures[i].Value = BitConverter.ToUInt16(sensorsBytes, i * 2) / 100.0f;
        }

        byte[] colorsBytes = _rawData.Skip(HeaderSize + ColorsOffset).Take(2 * _colorSensors.Length).ToArray();
        for (int i = 0; i < _colorSensors.Length; i++)
        {
            Array.Reverse(colorsBytes, i * 2, 2);
            _colorSensors[i].Value = BitConverter.ToUInt16(colorsBytes, i * 2) / 81.90f;
        }
    }
}
