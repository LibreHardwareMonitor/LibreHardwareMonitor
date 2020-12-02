// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Linq;
using System.Threading;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Controller.Nzxt
{
    /**
     * Support for the Kraken X3 devices from NZXT
     */
    internal sealed class KrakenX3 : Hardware
    {
        // Some fixed messages to send to the pump for basic monitoring and control
        private static readonly byte[] _getFirmwareInfo = { 0x10, 0x01 };
        private static readonly byte[] _initialize1 = { 0x70, 0x02, 0x01, 0xb8, 0x0b };
        private static readonly byte[] _initialize2 = { 0x70, 0x01 };
        private static readonly byte[][] _setPumpTargetMap = new byte[101][]; // Sacrifice memory to speed this up with a lookup instead of a copy operation

        private readonly Sensor _pump;
        private readonly Control _pumpControl;
        private readonly Sensor _pumpRpm;
        private readonly byte[] _rawData = new byte[64];
        private readonly HidStream _stream;
        private readonly Sensor _temperature;

        private volatile bool _controlling;

        static KrakenX3()
        {
            byte[] setPumpSpeedHeader = { 0x72, 0x01, 0x00, 0x00 };

            for (byte speed = 0; speed < _setPumpTargetMap.Length; speed++)
                _setPumpTargetMap[speed] = setPumpSpeedHeader.Concat(Enumerable.Repeat(speed, 40).Concat(new byte[20])).ToArray();
        }

        public KrakenX3(HidDevice dev, ISettings settings) : base("Nzxt Kraken X3", new Identifier("nzxt", "krakenx3", dev.GetSerialNumber().TrimStart('0')), settings)
        {
            if (dev.TryOpen(out _stream))
            {
                _stream.ReadTimeout = 5000; // The NZXT device returns with data that we need periodically without writing... 
                _stream.Write(_initialize1);
                _stream.Write(_initialize2);

                _stream.Write(_getFirmwareInfo);
                do
                {
                    _stream.Read(_rawData);
                    if (_rawData[0] == 0x11 && _rawData[1] == 0x01)
                    {
                        FirmwareVersion = $"{_rawData[0x11]}.{_rawData[0x11]}.{_rawData[0x13]}";
                    }
                }
                while (FirmwareVersion == null);

                Name = "Nzxt Kraken X3";

                _pump = new Sensor("Pump Control", 0, SensorType.Control, this, new ParameterDescription[0], settings);
                _pumpControl = new Control(_pump, settings, 0, 100);
                _pump.Control = _pumpControl;
                _pumpControl.ControlModeChanged += SoftwareControlValueChanged;
                _pumpControl.SoftwareControlValueChanged += SoftwareControlValueChanged;
                SoftwareControlValueChanged(_pumpControl);
                ActivateSensor(_pump);

                _pumpRpm = new Sensor("Pump", 0, SensorType.Fan, this, new ParameterDescription[0], settings);
                ActivateSensor(_pumpRpm);

                _temperature = new Sensor("Internal Water", 0, SensorType.Temperature, this, new ParameterDescription[0], settings);
                ActivateSensor(_temperature);

                ThreadPool.UnsafeQueueUserWorkItem(ContinuousRead, _rawData);
            }
        }

        public string FirmwareVersion { get; private set; }

        public override HardwareType HardwareType => HardwareType.Cooler;

        public string Status => FirmwareVersion != "2.1.0" ? $"Status: Untested Firmware Version {FirmwareVersion}! Please consider Updating to Version 2.1.0" : "Status: OK";

        private void SoftwareControlValueChanged(Control control)
        {
            if (control.ControlMode == ControlMode.Software)
            {
                float value = control.SoftwareValue;
                byte pumpSpeedIndex = (byte)(value > 100 ? 100 : (value < 0) ? 0 : value); // Clamp the value, anything out of range will fail

                _controlling = true;
                _stream.Write(_setPumpTargetMap[pumpSpeedIndex]);
                _pump.Value = value;
            }
            else if (control.ControlMode == ControlMode.Default)
            {
                // There isn't a "default" mode with this pump, but a safe setting is 40%
                _stream.Write(_setPumpTargetMap[40]);
            }
        }

        public override void Close()
        {
            base.Close();
            _stream.Close();
        }

        private void ContinuousRead(object state)
        {
            byte[] buffer = new byte[_rawData.Length];
            while (_stream.CanRead)
            {
                try
                {
                    _stream.Read(buffer); // This is a blocking call, will wait for bytes to become available

                    lock (_rawData)
                    {
                        Array.Copy(buffer, _rawData, buffer.Length);
                    }
                }
                catch (TimeoutException)
                {
                    // Don't care, just make sure the stream is still open
                    Thread.Sleep(500);
                }
                catch (ObjectDisposedException)
                {
                    // Could be unplugged, or the app is stopping...
                }
            }
        }

        public override void Update()
        {
            // The NZXT Kraken X3 series sends updates periodically. We have to read it in a seperate thread, this call just reads that data.
            lock (_rawData)
            {
                if (_rawData[0] == 0x75 && _rawData[1] == 0x02)
                {
                    _temperature.Value = _rawData[15] + _rawData[16] / 10.0f;
                    _pumpRpm.Value = (_rawData[18] << 8) | _rawData[17];

                    // The following logic makes sure the pump is set to the controlling value. This pump sometimes sets itself to 0% when instructed to a value.
                    if (!_controlling)
                    {
                        _pump.Value = _rawData[19];
                    }
                    else if (_pump.Value != _rawData[19])
                    {
                        float value = _pump.Value.GetValueOrDefault();
                        byte pumpSpeedIndex = (byte)(value > 100 ? 100 : (value < 0) ? 0 : value); // Clamp the value, anything out of range will fail
                        _stream.Write(_setPumpTargetMap[pumpSpeedIndex]);
                    }
                    else
                    {
                        _controlling = false;
                    }
                }
            }
        }
    }
}
