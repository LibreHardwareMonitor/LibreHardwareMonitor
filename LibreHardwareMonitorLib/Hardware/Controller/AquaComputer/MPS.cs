// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.IO;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Controller.AquaComputer;

internal sealed class MPS : Hardware
{
    public const int ExternalTemperature = 43;
    public const int InternalWaterTemperature = 45;
    public const int PumpFlow = 35;
    private const byte MPS_REPORT_ID = 0x2;

    private readonly Sensor _pumpFlow;
    private readonly byte[] _rawData = new byte[64];
    private readonly HidStream _stream;
    private readonly Sensor[] _temperatures = new Sensor[2];

    private ushort _externalTemperature;

    public MPS(HidDevice dev, ISettings settings) : base("MPS", new Identifier(dev.DevicePath), settings)
    {
        if (dev.TryOpen(out _stream))
        {
            do
            {
                _rawData[0] = MPS_REPORT_ID;
                _stream.GetFeature(_rawData);
            }
            while (_rawData[0] != MPS_REPORT_ID);

            Name = "MPS";
            FirmwareVersion = ExtractFirmwareVersion();

            _temperatures[0] = new Sensor("External", 0, SensorType.Temperature, this, Array.Empty<ParameterDescription>(), settings);
            ActivateSensor(_temperatures[0]);
            _temperatures[1] = new Sensor("Internal Water", 1, SensorType.Temperature, this, Array.Empty<ParameterDescription>(), settings);
            ActivateSensor(_temperatures[1]);

            _pumpFlow = new Sensor("Pump", 0, SensorType.Flow, this, Array.Empty<ParameterDescription>(), settings);
            ActivateSensor(_pumpFlow);
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
            FirmwareVersion = ExtractFirmwareVersion();
            if (FirmwareVersion < 1012)
            {
                return $"Status: Untested Firmware Version {FirmwareVersion}! Please consider Updating to Version 1012";
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
        try
        {
            _rawData[0] = MPS_REPORT_ID;
            _stream.GetFeature(_rawData);
        }
        catch (IOException)
        {
            return;
        }

        if (_rawData[0] != MPS_REPORT_ID)
            return;

        _pumpFlow.Value = BitConverter.ToUInt16(_rawData, PumpFlow) / 10f;

        _externalTemperature = BitConverter.ToUInt16(_rawData, ExternalTemperature);
        //sensor reading returns Int16.MaxValue (32767), when not connected
        if (_externalTemperature != short.MaxValue)
        {
            _temperatures[0].Value = _externalTemperature / 100f;
        }
        else
        {
            _temperatures[0].Value = null;
        }

        _temperatures[1].Value = BitConverter.ToUInt16(_rawData, InternalWaterTemperature) / 100f;

    }

    private ushort ExtractFirmwareVersion()
    {
        return BitConverter.ToUInt16(_rawData, 3);
    }
}
