// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using HidLibrary;

namespace LibreHardwareMonitor.Hardware.Controller.AquaComputer
{
    internal class MPS : Hardware
    {
        #region USB
        private HidDevice _device;
        private byte[] _rawData;
        public UInt16 FirmwareVersion { get; private set; }
        #endregion

        private readonly Sensor _pumpFlow;
        private readonly Sensor[] _temperatures = new Sensor[2];

        private const byte MPS_REPORT_ID = 0x2;

        private UInt16 _externalTemperature = 0;

        private sealed class MPSDataIndexes
        {
            public const int PumpFlow = 35;
            public const int ExternalTemperature = 43;
            public const int InternalWaterTemperature = 45;
        }

        public MPS(HidDevice dev, ISettings settings) : base("MPS", new Identifier(dev.DevicePath), settings)
        {
            _device = dev;

            do
            {
                _device.ReadFeatureData(out _rawData, MPS_REPORT_ID);
            } while (_rawData[0] != MPS_REPORT_ID);

            Name = $"MPS";
            FirmwareVersion = ExtractFirmwareVersion();

            _temperatures[0] = new Sensor("External", 0, SensorType.Temperature, this, new ParameterDescription[0], settings);
            ActivateSensor(_temperatures[0]);
            _temperatures[1] = new Sensor("Internal Water", 1, SensorType.Temperature, this, new ParameterDescription[0], settings);
            ActivateSensor(_temperatures[1]);

            _pumpFlow = new Sensor("Pump", 0, SensorType.Flow, this, new ParameterDescription[0], settings);
            ActivateSensor(_pumpFlow);
        }

        public override HardwareType HardwareType
        {
            get
            {
                return HardwareType.AquaComputer;
            }
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
            _device.CloseDevice();
            base.Close();
        }

        public override void Update()
        {
            _device.ReadFeatureData(out _rawData, MPS_REPORT_ID);

            if (_rawData[0] != MPS_REPORT_ID)
                return;

            _pumpFlow.Value = BitConverter.ToUInt16(_rawData, MPSDataIndexes.PumpFlow) / 10f;

            _externalTemperature = BitConverter.ToUInt16(_rawData, MPSDataIndexes.ExternalTemperature);
            //sensor reading returns Int16.MaxValue (32767), when not connected
            if (_externalTemperature != Int16.MaxValue)
            {
                _temperatures[0].Value = _externalTemperature / 100f;
            }
            else
                _temperatures[0].Value = null;

            _temperatures[1].Value = BitConverter.ToUInt16(_rawData, MPSDataIndexes.InternalWaterTemperature) / 100f;

        }

        private ushort ExtractFirmwareVersion()
        {
            return BitConverter.ToUInt16(_rawData, 3);
        }
    }
}
