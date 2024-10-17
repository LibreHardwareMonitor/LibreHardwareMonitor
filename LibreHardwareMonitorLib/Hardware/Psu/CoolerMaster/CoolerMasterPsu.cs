// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) 2020 Wilken Gottwalt<wilken.gottwalt@posteo.net>
// Copyright (C) 2023 Jannis234
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

// Implemented after the Linux kernel driver corsair_psu by Wilken Gottwalt and contributers
// Implemented after the Linux kernel driver cm_psu by Jannis234 and contributers https://github.com/Jannis234/cm-psu



using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using HidSharp;
using HidSharp.Utility;
using LibreHardwareMonitor.Hardware.Motherboard.Lpc;
using LibreHardwareMonitor.Hardware.Psu.Corsair;
using static LibreHardwareMonitor.Interop.Ftd2xx;

namespace LibreHardwareMonitor.Hardware.Psu.CoolerMaster;


internal sealed class CoolerMasterPsu : Hardware
{

    private readonly HidDevice _device;
    private readonly List<PsuSensor> _sensors = new();
    private HidStream stream;
    private bool _running;


    public CoolerMasterPsu(HidDevice device, ISettings settings, int index)
        : base("CoolerMaster PSU", new Identifier("psu", "coolermaster", index.ToString()), settings)
    {
        _device = device;

        if (_device.TryOpen(out stream))
        {
            _running = true;

            stream = device.Open();

            Task.Run(ReadStream);
        }

        AddSensors(settings);
    }


    public override HardwareType HardwareType => HardwareType.Psu;


    public override void Update()
    {

        _sensors.ForEach(s => s.Update(stream));
    }


    private void ReadStream()
    {
        byte[] inputReportBuffer = new byte[_device.GetMaxInputReportLength()];

        while (_running)
        {
            IAsyncResult ar = null;

            while (_running)
            {
                ar ??= stream.BeginRead(inputReportBuffer, 0, inputReportBuffer.Length, null, null);

                if (ar.IsCompleted)
                {
                    int byteCount = stream.EndRead(ar);
                    ar = null;
                    Decode(inputReportBuffer);

                }
                else
                {
                    ar.AsyncWaitHandle.WaitOne(1000);
                }
            }
        }
    }


    private void Decode(byte[] data)
    {
        float Value1;
        float Value2;

        try
        {
            // Extract values
            String valueString = System.Text.Encoding.ASCII.GetString(data, 4, 10).Replace("]", "");
            String[] Values = valueString.Split('/');
            Value1 = float.Parse(Values[0]);
            if (Values.Length == 2) Value2 = float.Parse(Values[1]); else Value2 = 0;

            // Extract channel
            int Channel = data[3] - 48;
            if (Channel == 4) Channel = 5;

            switch (data[2])
            {
                case 0x56: // Volts
                    UpdateSensor((Rail)Channel, SensorType.Voltage, Value1);
                    break;

                case 0x49: // Current
                    UpdateSensor((Rail)Channel, SensorType.Current, Value1);
                    break;

                case 0x50: // Power
                    if (Channel == 2)
                    {
                        UpdateSensor("Power In", SensorType.Power, Value1);
                        UpdateSensor("Power Out", SensorType.Power, Value2);
                    }
                    break;

                case 0x54: // Temp
                    if (Channel == 1)
                        UpdateSensor("Case", SensorType.Temperature, Value1);
                    else
                        UpdateSensor("VRM", SensorType.Temperature, Value1);
                    break;

                case 0x52:
                    UpdateSensor("Case", SensorType.Fan, Value1);
                    break;

                default:
                    //   ReadingType = "Unknown";
                    break;
            }
        }
        catch
        {
        }
    }


    private void UpdateSensor(Rail rail, SensorType sensorType, float Value)
    {
        if (rail != null && sensorType != null)
        {
            foreach (PsuSensor sensor in _sensors)
            {
                if (sensor.rail == rail && sensor.SensorType == sensorType)
                {
                    sensor.Value = Value;
                }
            }
        }
    }


    private void UpdateSensor(String name, SensorType sensorType, float Value)
    {
        if (name != null && sensorType != null)
        {
            foreach (PsuSensor sensor in _sensors)
            {
                if (sensor.Name == name && sensor.SensorType == sensorType)
                {
                    sensor.Value = Value;
                }
            }
        }
    }


    public override void Close()
    {
        _running = false;
        stream.Close();
        base.Close();
    }


    private void AddSensors(ISettings settings)
    {
        SensorIndices indices = new();
        _sensors.Add(new PsuSensor("VRM", indices, SensorType.Temperature, this, settings));
        _sensors.Add(new PsuSensor("Case", indices, SensorType.Temperature, this,settings));

        _sensors.Add(new PsuSensor("Case", indices, SensorType.Fan, this, settings));

        _sensors.Add(new PsuSensor("Input", indices, SensorType.Voltage, this, settings, Rail._Input));
        _sensors.Add(new PsuSensor("+12V", indices, SensorType.Voltage, this, settings, Rail._12V ));
        _sensors.Add(new PsuSensor("+5V", indices, SensorType.Voltage, this, settings, Rail._5V));
        _sensors.Add(new PsuSensor("+3.3V", indices, SensorType.Voltage, this, settings, Rail._3V));
  
        _sensors.Add(new PsuSensor("Input", indices, SensorType.Current, this, settings, Rail._Input));
        _sensors.Add(new PsuSensor("+12V", indices, SensorType.Current, this, settings, Rail._12V));
        _sensors.Add(new PsuSensor("+5V", indices, SensorType.Current, this,  settings, Rail._5V));
        _sensors.Add(new PsuSensor("+3.3V", indices,  SensorType.Current, this, settings, Rail._3V));


        _sensors.Add(new PsuSensor("Power In", indices, SensorType.Power, this, settings, Rail._Input));
        _sensors.Add(new PsuSensor("Power Out", indices, SensorType.Power, this, settings, Rail._Input));
    }


    private class PsuSensor : Sensor
    {
        private readonly UsbApi.Command _cmd;
        public readonly Rail rail;

        float parsedvalue;

        public PsuSensor(string name, SensorIndices indices, SensorType type, CoolerMasterPsu hardware, ISettings settings, Rail rail = Rail._12V, bool noHistory = false)
            : base(name, indices.NextIndex(type), false, type, hardware, null, settings, noHistory)
        {
            this.rail = rail;

            hardware.ActivateSensor(this);
        }

        public void Update(HidStream stream)
        {
            //Value = parsedvalue;
        }
    }


    private enum Rail : byte
    {
        _Input = 1,
        _5V = 2,
        _3V = 3,
        _12V = 5,
    }
}


