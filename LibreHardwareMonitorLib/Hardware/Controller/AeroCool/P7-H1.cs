// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Threading.Tasks;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Controller.AeroCool;

internal sealed class P7H1 : Hardware
{
    private const byte REPORT_ID = 0x0;
    private readonly HidDevice _device;

    private readonly Sensor[] _rpm = new Sensor[5];
    private readonly float[] _speeds = new float[5];
    private readonly HidStream _stream;
    private bool _running;

    public P7H1(HidDevice dev, ISettings settings) : base("AeroCool P7-H1", new Identifier(dev.DevicePath), settings)
    {
        _device = dev;
        HubNumber = _device.ProductID - 0x1000;
        Name = $"AeroCool P7-H1 #{HubNumber}";

        if (_device.TryOpen(out _stream))
        {
            _running = true;

            Task.Run(ReadStream);

            for (int i = 0; i < 5; i++)
            {
                _rpm[i] = new Sensor($"Fan #{i + 1}", i, SensorType.Fan, this, settings);
                ActivateSensor(_rpm[i]);
            }
        }
    }

    public override HardwareType HardwareType
    {
        get { return HardwareType.Cooler; }
    }

    public int HubNumber { get; }

    private void ReadStream()
    {
        byte[] inputReportBuffer = new byte[_device.GetMaxInputReportLength()];

        while (_running)
        {
            IAsyncResult ar = null;

            while (_running)
            {
                ar ??= _stream.BeginRead(inputReportBuffer, 0, inputReportBuffer.Length, null, null);

                if (ar.IsCompleted)
                {
                    int byteCount = _stream.EndRead(ar);
                    ar = null;

                    if (byteCount == 16 && inputReportBuffer[0] == REPORT_ID)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            _speeds[i] = (inputReportBuffer[(i * 3) + 2] * 256) + inputReportBuffer[(i * 3) + 3];
                        }
                    }
                }
                else
                {
                    ar.AsyncWaitHandle.WaitOne(1000);
                }
            }
        }
    }

    public override void Close()
    {
        _running = false;
        _stream.Close();
        base.Close();
    }

    public override void Update()
    {
        for (int i = 0; i < 5; i++)
        {
            _rpm[i].Value = _speeds[i];
        }
    }
}