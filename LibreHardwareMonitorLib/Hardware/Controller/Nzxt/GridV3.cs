// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Controller.Nzxt
{
    /**
     * Support for the GRID+ V3 devices from NZXT
     */
    internal sealed class GridV3 : Hardware
    {
        // Some initialization messages to send to the controller. No visible effects but NZXT CAM send them.
        private static readonly byte[] _initialize1 = { 0x01, 0x5c };
        private static readonly byte[] _initialize2 = { 0x01, 0x5d };
        private static readonly byte[] _initialize3 = { 0x01, 0x59 };

        private readonly HidStream _stream;
        private readonly Dictionary<int, byte[]> _rawData = new Dictionary<int, byte[]>();

        private const int FansCount = 6;
        private readonly Sensor[] _rpmSensors = new Sensor[FansCount];
        private readonly Sensor[] _voltages = new Sensor[FansCount];
        private readonly Sensor[] _currents = new Sensor[FansCount];
        private readonly Sensor[] _powers = new Sensor[FansCount];

        private readonly Control[] _fanControls = new Control[FansCount];

        public GridV3(HidDevice dev, ISettings settings) : base("NZXT GRID+ V3", new Identifier("nzxt", "gridv3", dev.GetSerialNumber().TrimStart('0')), settings)
        {
            if (dev.TryOpen(out _stream))
            {
                for (int fanID = 0; fanID < FansCount; fanID++)
                    _rawData[fanID] = new byte[21];

                _stream.Write(_initialize1);
                _stream.Write(_initialize2);
                _stream.Write(_initialize3);

                do
                {
                    _stream.Read(_rawData[0]);
                    if (_rawData[0][0] == 0x04)
                    {
                        FirmwareVersion = $"{_rawData[0][11]}.{_rawData[0][14]}";
                    }
                }
                while (FirmwareVersion == null);

                Name = "NZXT GRID+ V3";

                // Initialize all sensors and controls for all fans
                for (int i = 0; i < FansCount; i++)
                {
                    _rpmSensors[i] = new Sensor($"Fan {i + 1}", i, SensorType.Fan, this, Array.Empty<ParameterDescription>(), settings);
                    _voltages[i] = new Sensor($"Fan {i + 1}", i, SensorType.Voltage, this, Array.Empty<ParameterDescription>(), settings);
                    _currents[i] = new Sensor($"Fan {i + 1}", i, SensorType.Current, this, Array.Empty<ParameterDescription>(), settings);
                    _powers[i] = new Sensor($"Fan {i + 1}", i, SensorType.Power, this, Array.Empty<ParameterDescription>(), settings);

                    _fanControls[i] = new Control(_rpmSensors[i], settings, 0, 100);

                    _rpmSensors[i].Control = _fanControls[i];
                    _fanControls[i].ControlModeChanged += SoftwareControlValueChanged;
                    _fanControls[i].SoftwareControlValueChanged += SoftwareControlValueChanged;
                    SoftwareControlValueChanged(_fanControls[i]);

                    ActivateSensor(_rpmSensors[i]);
                    ActivateSensor(_voltages[i]);
                    ActivateSensor(_currents[i]);
                    ActivateSensor(_powers[i]);
                }

                ThreadPool.UnsafeQueueUserWorkItem(ContinuousRead, _rawData);
            }
        }

        public string FirmwareVersion { get; }

        public override HardwareType HardwareType => HardwareType.Cooler;

        private void SoftwareControlValueChanged(Control control)
        {
            if (control.ControlMode == ControlMode.Software)
            {
                float value = control.SoftwareValue;
                byte fanSpeed = (byte)(value > 100 ? 100 : (value < 0) ? 0 : value); // Clamp the value, anything out of range will fail

                //_controlling = true;
                byte[] msg = new byte[65];
                msg[0] = 0x02;
                msg[1] = 0x4d;
                msg[2] = (byte)control.Sensor.Index;
                msg[3] = 0x00;
                msg[4] = fanSpeed;

                _stream.Write(msg);
            }
            else if (control.ControlMode == ControlMode.Default)
            {
                // There isn't a "default" mode, but let's say a safe setting is 40%
                byte[] msg = new byte[65];
                msg[0] = 0x02;
                msg[1] = 0x4d;
                msg[2] = (byte)control.Sensor.Index;
                msg[3] = 0x00;
                msg[4] = 40;

                _stream.Write(msg);
            }
        }

        public override void Close()
        {
            _stream?.Close();
            base.Close();
        }

        private void ContinuousRead(object state)
        {
            byte[] buffer = new byte[_rawData[0].Length];
            while (_stream.CanRead)
            {
                try
                {
                    _stream.Read(buffer); // This is a blocking call, will wait for bytes to become available
                    if (buffer[0] == 0x04)
                    {
                        lock (_rawData)
                        {
                            int fanID = (buffer[15] >> 4) & 0x0f;
                            Array.Copy(buffer, _rawData[fanID], buffer.Length);
                        }
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
                    return;
                }
            }
        }

        public override void Update()
        {
            // The NZXT GRID+ V3 series sends updates periodically. We have to read it in a seperate thread, this call just reads that data.
            lock (_rawData)
            {
                for (int fanID = 0; fanID < FansCount; fanID++)
                {
                    _rpmSensors[fanID].Value = (_rawData[fanID][3] << 8) | _rawData[fanID][4];
                    _voltages[fanID].Value = _rawData[fanID][7] + _rawData[fanID][8] / 100.0f;
                    _currents[fanID].Value = _rawData[fanID][9] + _rawData[fanID][10] / 100.0f;
                    _powers[fanID].Value = _currents[fanID].Value * _voltages[fanID].Value;
                }
            }
        }
    }
}
