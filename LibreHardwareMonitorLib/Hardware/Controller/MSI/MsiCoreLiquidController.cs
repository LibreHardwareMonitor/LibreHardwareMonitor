// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) Florian K. (Blacktempel)
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Controller.MSI;

internal class MsiCoreLiquidController : Hardware
{
    private const int MutexTimeout = 500;
    private const int DataLength = 64;

    private readonly MsiDevice _msiDevice;
    private readonly HidDevice _hidDevice;

    private readonly List<MsiSensor> _sensors = [];

    private Sensor _fan1;
    private Sensor _fan2;
    private Sensor _fan3;
    private Sensor _fan4;
    private Sensor _fan5;

    public MsiCoreLiquidController(MsiDevice msiDevice, HidDevice hidDevice, ISettings settings)
        : base(msiDevice.Name, new(hidDevice), settings)
    {
        _msiDevice = msiDevice;
        _hidDevice = hidDevice;

        CreateSensors();
    }

    public MsiDevice MsiDevice => _msiDevice;

    public override HardwareType HardwareType => HardwareType.Cooler;

    public override void Update()
    {
        var msi = new MsiFanControl();

        if (GetCoolerStatus(msi) && GetFanConfigure(msi) && GetFanTemperatureConfigure(msi))
        {
            _sensors.ForEach(s => s.Update(msi));
        }
    }

    public override string GetReport()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"{MsiDevice.Name} - ({MsiDevice.VendorId:X4}:{MsiDevice.ProductIdController:X4})");

        foreach (var sensor in _sensors)
        {
            sb.AppendLine($"{sensor.Name}: {sensor.Value?.ToString() ?? "No value"}");
        }

        return sb.ToString();
    }

    private void CreateSensors()
    {
        if (MsiDevice.DeviceType == MsiDeviceType.S360)
        {
            _fan1 = AddSensor("Radiator Fan", 10, SensorType.Fan, m => m.Fan1.Speed);
            _fan4 = AddSensor("Pump Fan", 13, SensorType.Fan, m => m.Fan4.Speed);
            _fan5 = AddSensor("Pump", 14, SensorType.Fan, m => m.Fan5.Speed);
        }
        else //May need other mapping for different devices
        {
            _fan1 = AddSensor("Fan 1", 10, SensorType.Fan, m => m.Fan1.Speed);
            _fan2 = AddSensor("Fan 2", 11, SensorType.Fan, m => m.Fan2.Speed);
            _fan3 = AddSensor("Fan 3", 12, SensorType.Fan, m => m.Fan3.Speed);
            _fan4 = AddSensor("Fan 4", 13, SensorType.Fan, m => m.Fan4.Speed);
            _fan5 = AddSensor("Fan 5", 14, SensorType.Fan, m => m.Fan5.Speed);
        }

        AddSensor("Inlet Temperature", 0, SensorType.Temperature, m => m.TemperatureInlet);
        AddSensor("Liquid Temperature", 1, SensorType.Temperature, m => m.TemperatureOutlet); //Msi: "Liquid_Temp"
        AddSensor("Temperature Sensor 1", 2, SensorType.Temperature, m => m.TemperatureSensor1 == 125 ? -100 : m.TemperatureSensor1);
        AddSensor("Temperature Sensor 2", 3, SensorType.Temperature, m => m.TemperatureSensor2 == 125 ? -100 : m.TemperatureSensor2);

        void TryAddFanControl(Sensor fan)
        {
            if (fan != null)
            {
                var ctrl = new Control(fan, _settings, 0, 100);
                fan.Control = ctrl;
                ctrl.ControlModeChanged += OnFanControlModeChanged;
                ctrl.SoftwareControlValueChanged += OnSoftwareControlValueChanged;

                MsiSensor fanControl = null;

                switch (fan)
                {
                    case var f when f == _fan1:
                        fanControl = AddSensor($"{fan.Name} Control", 50, SensorType.Control, m => m.Fan1.ConfigureDuty.Item0);
                        break;
                    case var f when f == _fan2:
                        fanControl = AddSensor($"{fan.Name} Control", 51, SensorType.Control, m => m.Fan2.ConfigureDuty.Item0);
                        break;
                    case var f when f == _fan3:
                        fanControl = AddSensor($"{fan.Name} Control", 52, SensorType.Control, m => m.Fan3.ConfigureDuty.Item0);
                        break;
                    case var f when f == _fan4:
                        fanControl = AddSensor($"{fan.Name} Control", 53, SensorType.Control, m => m.Fan4.ConfigureDuty.Item0);
                        break;
                    case var f when f == _fan5:
                        fanControl = AddSensor($"{fan.Name} Control", 54, SensorType.Control, m => m.Fan5.ConfigureDuty.Item0);
                        break;
                }

                if (fanControl != null)
                {
                    fanControl.Control = ctrl;

                    _sensors.Add(fanControl);

                    ActivateSensor(fanControl);
                }
            }
        }

        //Fan Controls
        TryAddFanControl(_fan1);
        TryAddFanControl(_fan2);
        TryAddFanControl(_fan3);
        TryAddFanControl(_fan4);
        TryAddFanControl(_fan5);
    }

    private MsiSensor AddSensor(string name, int index, SensorType sensorType, GetMsiSensorValue getValue)
    {
        var sensor = new MsiSensor(name, index, sensorType, this, _settings, getValue);

        _sensors.Add(sensor);

        ActivateSensor(sensor);

        return sensor;
    }

    private void OnFanControlModeChanged(Control control)
    {
        var msi = new MsiFanControl();

        if (GetFanConfigure(msi) && GetFanTemperatureConfigure(msi))
        {
            var fan = GetFanFromControl(msi, control);

            if (fan == null)
            {
                return;
            }

            switch (control.ControlMode)
            {
                case ControlMode.Software:
                    fan.ConfigureDuty.Mode = MsiFanMode.Custom;
                    break;
                case ControlMode.Default:
                    fan.ConfigureDuty.Mode = MsiFanMode.Bios;
                    break;
                default:
                    return;
            }

            SetFanConfigure(msi);
        }
    }

    private void OnSoftwareControlValueChanged(Control control)
    {
        var msi = new MsiFanControl();

        if (GetFanConfigure(msi) && GetFanTemperatureConfigure(msi))
        {
            var fan = GetFanFromControl(msi, control);

            if (fan == null)
            {
                return;
            }

            byte value = (byte)control.SoftwareValue;

            if (value < 0)
            {
                value = 0;
            }
            else if (value > 100)
            {
                value = 100;
            }

            //Set requested speed in % and CPU temperature in degrees Celsius
            fan.ConfigureDuty.Item0 = value;
            fan.ConfigureTemp.Item0 = 0; //Set temperature to 0 to use fixed duty instead of temperature curve

            fan.ConfigureDuty.Item1 = value;
            fan.ConfigureTemp.Item1 = 90;

            //Add failsafe to prevent fan from stopping at high temperatures if user sets low duty
            fan.ConfigureDuty.Item2 = 100; //Max fan
            fan.ConfigureTemp.Item2 = 91;

            fan.ConfigureDuty.Item3 = 100; //Max fan
            fan.ConfigureTemp.Item3 = 95;

            SetFanConfigure(msi);
            SetFanTemperatureConfigure(msi);
        }
    }

    private MsiFan GetFanFromControl(MsiFanControl msi, Control control)
    {
        MsiFan fan = null;

        if (control.Sensor == _fan1)
            fan = msi.Fan1;
        else if (control.Sensor == _fan2)
            fan = msi.Fan2;
        else if (control.Sensor == _fan3)
            fan = msi.Fan3;
        else if (control.Sensor == _fan4)
            fan = msi.Fan4;
        else if (control.Sensor == _fan5)
            fan = msi.Fan5;

        return fan;
    }

    private bool GetCoolerStatus(MsiFanControl msi)
    {
        var buffer = GetBuffer();
        buffer[0] = 0xD0;
        buffer[1] = 0x31;

        var data = GetData(buffer);

        if (data == null || data[1] != buffer[1])
        {
            return false;
        }

        msi.Fan1.Speed = GetInt16(data, 2);
        msi.Fan2.Speed = GetInt16(data, 4);
        msi.Fan3.Speed = GetInt16(data, 6);
        msi.Fan4.Speed = GetInt16(data, 8);
        msi.Fan5.Speed = GetInt16(data, 10);

        msi.TemperatureInlet = data[12];
        msi.TemperatureOutlet = data[14];
        msi.TemperatureSensor1 = GetInt16(data, 16);
        msi.TemperatureSensor2 = GetInt16(data, 18);

        msi.Fan1.Duty = GetInt16(data, 22);
        msi.Fan2.Duty = GetInt16(data, 24);
        msi.Fan3.Duty = GetInt16(data, 26);
        msi.Fan4.Duty = GetInt16(data, 28);
        msi.Fan5.Duty = GetInt16(data, 30);

        return true;
    }

    private bool GetFanConfigure(MsiFanControl msi)
    {
        var buffer = GetBuffer();
        buffer[0] = 0xD0;
        buffer[1] = 0x32;

        var data = GetData(buffer);

        if (data == null || data[1] != buffer[1])
        {
            return false;
        }

        int startIndex = 2;

        msi.Fan1.ConfigureDuty = BytesToStruct<MsiFanConfigure>(data, startIndex);
        msi.Fan2.ConfigureDuty = BytesToStruct<MsiFanConfigure>(data, startIndex += Marshal.SizeOf<MsiFanConfigure>());
        msi.Fan3.ConfigureDuty = BytesToStruct<MsiFanConfigure>(data, startIndex += Marshal.SizeOf<MsiFanConfigure>());
        msi.Fan4.ConfigureDuty = BytesToStruct<MsiFanConfigure>(data, startIndex += Marshal.SizeOf<MsiFanConfigure>());
        msi.Fan5.ConfigureDuty = BytesToStruct<MsiFanConfigure>(data, startIndex += Marshal.SizeOf<MsiFanConfigure>());

        return true;
    }

    private bool GetFanTemperatureConfigure(MsiFanControl msi)
    {
        var buffer = GetBuffer();
        buffer[0] = 0xD0;
        buffer[1] = 0x33;

        var data = GetData(buffer);

        if (data == null || (data[1] != buffer[1] && data[1] != 0x32))
        {
            return false;
        }

        int startIndex = 2;

        msi.Fan1.ConfigureTemp = BytesToStruct<MsiFanConfigure>(data, startIndex);
        msi.Fan2.ConfigureTemp = BytesToStruct<MsiFanConfigure>(data, startIndex += Marshal.SizeOf<MsiFanConfigure>());
        msi.Fan3.ConfigureTemp = BytesToStruct<MsiFanConfigure>(data, startIndex += Marshal.SizeOf<MsiFanConfigure>());
        msi.Fan4.ConfigureTemp = BytesToStruct<MsiFanConfigure>(data, startIndex += Marshal.SizeOf<MsiFanConfigure>());
        msi.Fan5.ConfigureTemp = BytesToStruct<MsiFanConfigure>(data, startIndex += Marshal.SizeOf<MsiFanConfigure>());

        return true;
    }

    private void SetFanConfigure(MsiFanControl msi)
    {
        var buffer = GetBuffer();
        buffer[0] = 0xD0;
        buffer[1] = 0x40;

        int startIndex = 2;

        StructToBytes(msi.Fan1.ConfigureDuty, buffer, startIndex);
        StructToBytes(msi.Fan2.ConfigureDuty, buffer, startIndex += Marshal.SizeOf<MsiFanConfigure>());
        StructToBytes(msi.Fan3.ConfigureDuty, buffer, startIndex += Marshal.SizeOf<MsiFanConfigure>());
        StructToBytes(msi.Fan4.ConfigureDuty, buffer, startIndex += Marshal.SizeOf<MsiFanConfigure>());
        StructToBytes(msi.Fan5.ConfigureDuty, buffer, startIndex += Marshal.SizeOf<MsiFanConfigure>());

        SetData(buffer);
    }

    private void SetFanTemperatureConfigure(MsiFanControl msi)
    {
        var buffer = GetBuffer();
        buffer[0] = 0xD0;
        buffer[1] = 0x41;

        int startIndex = 2;

        StructToBytes(msi.Fan1.ConfigureTemp, buffer, startIndex);
        StructToBytes(msi.Fan2.ConfigureTemp, buffer, startIndex += Marshal.SizeOf<MsiFanConfigure>());
        StructToBytes(msi.Fan3.ConfigureTemp, buffer, startIndex += Marshal.SizeOf<MsiFanConfigure>());
        StructToBytes(msi.Fan4.ConfigureTemp, buffer, startIndex += Marshal.SizeOf<MsiFanConfigure>());
        StructToBytes(msi.Fan5.ConfigureTemp, buffer, startIndex += Marshal.SizeOf<MsiFanConfigure>());

        SetData(buffer);
    }

    private static T BytesToStruct<T>(byte[] data, int startIndex) where T : struct
    {
        int size = Marshal.SizeOf<T>();

        if (startIndex < 0 || startIndex + size > data.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        }

        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = handle.AddrOfPinnedObject() + startIndex;
            return Marshal.PtrToStructure<T>(ptr);
        }
        finally
        {
            handle.Free();
        }
    }

    private static void StructToBytes<T>(in T value, byte[] buffer, int startIndex) where T : struct
    {
        int size = Marshal.SizeOf<T>();

        if (startIndex < 0 || startIndex + size > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        }

        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = handle.AddrOfPinnedObject() + startIndex;
            Marshal.StructureToPtr(value, ptr, false);
        }
        finally
        {
            handle.Free();
        }
    }

    private byte[] GetData(byte[] inbuf)
    {
        HidStream stream = null;

        if (!Mutexes.WaitUsbSensors(MutexTimeout))
        {
            return null;
        }

        try
        {
            var outBuf = new byte[DataLength];

            if (!_hidDevice.TryOpen(out stream))
            {
                return null;
            }

            stream.Write(inbuf);
            Thread.Sleep(10); //Msi is using that interval
            var readResult = stream.Read(outBuf);

            if (readResult <= 0)
            {
                return null;
            }

            return outBuf;
        }
        finally
        {
            stream?.Close();
            stream?.Dispose();

            Mutexes.ReleaseUsbSensors();
        }
    }

    private void SetData(byte[] buf)
    {
        HidStream stream = null;

        if (!Mutexes.WaitUsbSensors(MutexTimeout))
        {
            return;
        }

        try
        {
            if (!_hidDevice.TryOpen(out stream))
            {
                return;
            }

            stream.Write(buf);
        }
        finally
        {
            stream?.Close();
            stream?.Dispose();

            Mutexes.ReleaseUsbSensors();
        }
    }

    private byte[] GetBuffer()
    {
        return new byte[DataLength];
    }

    private static int GetInt16(byte[] buffer, int offset)
    {
        return buffer[offset] + (buffer[offset + 1] << 8);
    }
}
