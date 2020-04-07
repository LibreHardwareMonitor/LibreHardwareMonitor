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
        private HidDevice _device;
        private byte[] _rawData = new byte[0];
        private object datalock = new object();

        private readonly Sensor[] _rpm = new Sensor[5];
        private const byte REPORT_ID = 0x0;

        public int HubNumber { get; private set; }

        public P7H1(HidDevice dev, ISettings settings) : base("P7-H1", new Identifier(dev.DevicePath), settings)
        {
            _device = dev;
            HubNumber = _device.Attributes.ProductId - 0x1000;
            Name = $"P7-H1 #{HubNumber}";
            _device.OpenDevice();
            _device.MonitorDeviceEvents = true;
            _device.Read(OnDataReady);

            for (int i=0; i<5; i++)
            {
                _rpm[i] = new Sensor($"Fan #{i}", i, SensorType.Fan, this, settings);
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
            _device.CloseDevice();
            base.Close();
        }

        public override void Update()
        {
            lock (datalock)
            {
                for (int i = 0; i < 5; i++)
                {
                    int speed = _rawData[i * 3 + 2] * 256 + _rawData[i * 3 + 3];
                    _rpm[i].Value = speed;
                }
            }
        }

        private void OnDataReady(HidDeviceData report)
        {
            _device.Read(OnDataReady);
            if(report.Status == HidDeviceData.ReadStatus.Success)
            {
                if(report.Data.Length > 0 && report.Data[0] == REPORT_ID)
                {
                    lock (datalock)
                    {
                        _rawData = report.Data;
                    }
                }
            }

        }

    }
}
