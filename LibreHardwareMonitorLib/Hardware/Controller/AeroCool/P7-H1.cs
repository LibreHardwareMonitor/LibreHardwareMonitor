// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using HidLibrary;

namespace LibreHardwareMonitor.Hardware.Controller.AeroCool
{
    internal class P7H1 : Hardware
    {
        private readonly HidDevice _device;
        private readonly float[] _speeds = new float[5];
        private bool _running;

        private readonly Sensor[] _rpm = new Sensor[5];
        private const byte REPORT_ID = 0x0;

        public int HubNumber { get; private set; }

        public P7H1(HidDevice dev, ISettings settings) : base("AeroCool P7-H1", new Identifier(dev.DevicePath), settings)
        {
            _device = dev;
            HubNumber = _device.Attributes.ProductId - 0x1000;
            Name = $"AeroCool P7-H1 #{HubNumber}";
            _device.OpenDevice();
            _device.MonitorDeviceEvents = true;
            _running = true;
            _device.Read(OnDataReady);

            for (int i=0; i<5; i++)
            {
                _rpm[i] = new Sensor($"Fan #{i+1}", i, SensorType.Fan, this, settings);
                ActivateSensor(_rpm[i]);
            }
        }

        public override HardwareType HardwareType
        {
            get
            {
                return HardwareType.AeroCool;
            }
        }

        public override void Close()
        {
            _running = false;
            _device.CloseDevice();
            base.Close();
        }

        public override void Update()
        {
            for (int i = 0; i < 5; i++)
            {
                _rpm[i].Value = _speeds[i];
            }
        }

        private void OnDataReady(HidDeviceData report)
        {
            if (!_running) // Do not register eventhandler again if device stopped
                return;
            if (report.Status == HidDeviceData.ReadStatus.Success)
            {
                byte[] rawData = report.Data;
                if(rawData.Length == 16 && rawData[0] == REPORT_ID)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        int speed = rawData[i * 3 + 2] * 256 + rawData[i * 3 + 3];
                        _speeds[i] = (float)speed;
                    }
                }
            }
            _device.Read(OnDataReady);
        }
    }
}
