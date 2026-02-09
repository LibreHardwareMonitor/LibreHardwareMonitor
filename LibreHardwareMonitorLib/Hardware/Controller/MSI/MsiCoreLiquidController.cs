// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) Florian K. (Blacktempel)
// All Rights Reserved.

using System.Collections.Generic;
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

        AddSensor("Temperature Inlet", 0, SensorType.Temperature, m => m.TemperatureInlet);
        AddSensor("Liquid Temperature", 1, SensorType.Temperature, m => m.TemperatureOutlet); //Msi: "Liquid_Temp"
        AddSensor("Temperature Sensor 1", 2, SensorType.Temperature, m => m.TemperatureSensor1 == 125 ? -100 : m.TemperatureSensor1);
        AddSensor("Temperature Sensor 2", 3, SensorType.Temperature, m => m.TemperatureSensor2 == 125 ? -100 : m.TemperatureSensor2);

        void TryAddFanControl(Sensor fan)
        {
            if (fan != null)
            {
                var fanControl = new Control(fan, _settings, 0, 100);
                fan.Control = fanControl;
                fanControl.ControlModeChanged += OnFanControlModeChanged;
                fanControl.SoftwareControlValueChanged += OnSoftwareControlValueChanged;
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
                    fan.Mode = MsiFanMode.Custom;
                    break;
                case ControlMode.Default:
                    fan.Mode = MsiFanMode.Bios;
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
            fan.ConfigureDuty0 = value;
            fan.ConfigureTemp0 = 0; //Set temperature to 0 to use fixed duty instead of temperature curve

            fan.ConfigureDuty1 = value;
            fan.ConfigureTemp1 = 90;

            //Add failsafe to prevent fan from stopping at high temperatures if user sets low duty
            fan.ConfigureDuty2 = 100; //Max fan
            fan.ConfigureTemp2 = 91;

            fan.ConfigureDuty3 = 100; //Max fan
            fan.ConfigureTemp3 = 95;

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

        msi.Fan1.Mode = (MsiFanMode)data[2];
        msi.Fan1.ConfigureDuty0 = data[3];
        msi.Fan1.ConfigureDuty1 = data[4];
        msi.Fan1.ConfigureDuty2 = data[5];
        msi.Fan1.ConfigureDuty3 = data[6];
        msi.Fan1.ConfigureDuty4 = data[7];
        msi.Fan1.ConfigureDuty5 = data[8];
        msi.Fan1.ConfigureDuty6 = data[9];

        msi.Fan2.Mode = (MsiFanMode)data[10];
        msi.Fan2.ConfigureDuty0 = data[11];
        msi.Fan2.ConfigureDuty1 = data[12];
        msi.Fan2.ConfigureDuty2 = data[13];
        msi.Fan2.ConfigureDuty3 = data[14];
        msi.Fan2.ConfigureDuty4 = data[15];
        msi.Fan2.ConfigureDuty5 = data[16];
        msi.Fan2.ConfigureDuty6 = data[17];

        msi.Fan3.Mode = (MsiFanMode)data[18];
        msi.Fan3.ConfigureDuty0 = data[19];
        msi.Fan3.ConfigureDuty1 = data[20];
        msi.Fan3.ConfigureDuty2 = data[21];
        msi.Fan3.ConfigureDuty3 = data[22];
        msi.Fan3.ConfigureDuty4 = data[23];
        msi.Fan3.ConfigureDuty5 = data[24];
        msi.Fan3.ConfigureDuty6 = data[25];

        msi.Fan4.Mode = (MsiFanMode)data[26];
        msi.Fan4.ConfigureDuty0 = data[27];
        msi.Fan4.ConfigureDuty1 = data[28];
        msi.Fan4.ConfigureDuty2 = data[29];
        msi.Fan4.ConfigureDuty3 = data[30];
        msi.Fan4.ConfigureDuty4 = data[31];
        msi.Fan4.ConfigureDuty5 = data[32];
        msi.Fan4.ConfigureDuty6 = data[33];

        msi.Fan5.Mode = (MsiFanMode)data[34];
        msi.Fan5.ConfigureDuty0 = data[35];
        msi.Fan5.ConfigureDuty1 = data[36];
        msi.Fan5.ConfigureDuty2 = data[37];
        msi.Fan5.ConfigureDuty3 = data[38];
        msi.Fan5.ConfigureDuty4 = data[39];
        msi.Fan5.ConfigureDuty5 = data[40];
        msi.Fan5.ConfigureDuty6 = data[41];

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

        msi.Fan1.Mode = (MsiFanMode)data[2];
        msi.Fan1.ConfigureTemp0 = data[3];
        msi.Fan1.ConfigureTemp1 = data[4];
        msi.Fan1.ConfigureTemp2 = data[5];
        msi.Fan1.ConfigureTemp3 = data[6];
        msi.Fan1.ConfigureTemp4 = data[7];
        msi.Fan1.ConfigureTemp5 = data[8];
        msi.Fan1.ConfigureTemp6 = data[9];

        msi.Fan2.Mode = (MsiFanMode)data[10];
        msi.Fan2.ConfigureTemp0 = data[11];
        msi.Fan2.ConfigureTemp1 = data[12];
        msi.Fan2.ConfigureTemp2 = data[13];
        msi.Fan2.ConfigureTemp3 = data[14];
        msi.Fan2.ConfigureTemp4 = data[15];
        msi.Fan2.ConfigureTemp5 = data[16];
        msi.Fan2.ConfigureTemp6 = data[17];

        msi.Fan3.Mode = (MsiFanMode)data[18];
        msi.Fan3.ConfigureTemp0 = data[19];
        msi.Fan3.ConfigureTemp1 = data[20];
        msi.Fan3.ConfigureTemp2 = data[21];
        msi.Fan3.ConfigureTemp3 = data[22];
        msi.Fan3.ConfigureTemp4 = data[23];
        msi.Fan3.ConfigureTemp5 = data[24];
        msi.Fan3.ConfigureTemp6 = data[25];

        msi.Fan4.Mode = (MsiFanMode)data[26];
        msi.Fan4.ConfigureTemp0 = data[27];
        msi.Fan4.ConfigureTemp1 = data[28];
        msi.Fan4.ConfigureTemp2 = data[29];
        msi.Fan4.ConfigureTemp3 = data[30];
        msi.Fan4.ConfigureTemp4 = data[31];
        msi.Fan4.ConfigureTemp5 = data[32];
        msi.Fan4.ConfigureTemp6 = data[33];

        msi.Fan5.Mode = (MsiFanMode)data[34];
        msi.Fan5.ConfigureTemp0 = data[35];
        msi.Fan5.ConfigureTemp1 = data[36];
        msi.Fan5.ConfigureTemp2 = data[37];
        msi.Fan5.ConfigureTemp3 = data[38];
        msi.Fan5.ConfigureTemp4 = data[39];
        msi.Fan5.ConfigureTemp5 = data[40];
        msi.Fan5.ConfigureTemp6 = data[41];

        return true;
    }

    private void SetFanConfigure(MsiFanControl msi)
    {
        var buffer = GetBuffer();
        buffer[0] = 0xD0;
        buffer[1] = 0x40;

        buffer[2] = (byte)msi.Fan1.Mode;
        buffer[3] = msi.Fan1.ConfigureDuty0;
        buffer[4] = msi.Fan1.ConfigureDuty1;
        buffer[5] = msi.Fan1.ConfigureDuty2;
        buffer[6] = msi.Fan1.ConfigureDuty3;
        buffer[7] = msi.Fan1.ConfigureDuty4;
        buffer[8] = msi.Fan1.ConfigureDuty5;
        buffer[9] = msi.Fan1.ConfigureDuty6;

        buffer[10] = (byte)msi.Fan2.Mode;
        buffer[11] = msi.Fan2.ConfigureDuty0;
        buffer[12] = msi.Fan2.ConfigureDuty1;
        buffer[13] = msi.Fan2.ConfigureDuty2;
        buffer[14] = msi.Fan2.ConfigureDuty3;
        buffer[15] = msi.Fan2.ConfigureDuty4;
        buffer[16] = msi.Fan2.ConfigureDuty5;
        buffer[17] = msi.Fan2.ConfigureDuty6;

        buffer[18] = (byte)msi.Fan3.Mode;
        buffer[19] = msi.Fan3.ConfigureDuty0;
        buffer[20] = msi.Fan3.ConfigureDuty1;
        buffer[21] = msi.Fan3.ConfigureDuty2;
        buffer[22] = msi.Fan3.ConfigureDuty3;
        buffer[23] = msi.Fan3.ConfigureDuty4;
        buffer[24] = msi.Fan3.ConfigureDuty5;
        buffer[25] = msi.Fan3.ConfigureDuty6;

        buffer[26] = (byte)msi.Fan4.Mode;
        buffer[27] = msi.Fan4.ConfigureDuty0;
        buffer[28] = msi.Fan4.ConfigureDuty1;
        buffer[29] = msi.Fan4.ConfigureDuty2;
        buffer[30] = msi.Fan4.ConfigureDuty3;
        buffer[31] = msi.Fan4.ConfigureDuty4;
        buffer[32] = msi.Fan4.ConfigureDuty5;
        buffer[33] = msi.Fan4.ConfigureDuty6;

        buffer[34] = (byte)msi.Fan5.Mode;
        buffer[35] = msi.Fan5.ConfigureDuty0;
        buffer[36] = msi.Fan5.ConfigureDuty1;
        buffer[37] = msi.Fan5.ConfigureDuty2;
        buffer[38] = msi.Fan5.ConfigureDuty3;
        buffer[39] = msi.Fan5.ConfigureDuty4;
        buffer[40] = msi.Fan5.ConfigureDuty5;
        buffer[41] = msi.Fan5.ConfigureDuty6;

        SetData(buffer);
    }

    private void SetFanTemperatureConfigure(MsiFanControl msi)
    {
        var buffer = GetBuffer();
        buffer[0] = 0xD0;
        buffer[1] = 0x41;

        buffer[2] = (byte)msi.Fan1.Mode;
        buffer[3] = msi.Fan1.ConfigureTemp0;
        buffer[4] = msi.Fan1.ConfigureTemp1;
        buffer[5] = msi.Fan1.ConfigureTemp2;
        buffer[6] = msi.Fan1.ConfigureTemp3;
        buffer[7] = msi.Fan1.ConfigureTemp4;
        buffer[8] = msi.Fan1.ConfigureTemp5;
        buffer[9] = msi.Fan1.ConfigureTemp6;

        buffer[10] = (byte)msi.Fan2.Mode;
        buffer[11] = msi.Fan2.ConfigureTemp0;
        buffer[12] = msi.Fan2.ConfigureTemp1;
        buffer[13] = msi.Fan2.ConfigureTemp2;
        buffer[14] = msi.Fan2.ConfigureTemp3;
        buffer[15] = msi.Fan2.ConfigureTemp4;
        buffer[16] = msi.Fan2.ConfigureTemp5;
        buffer[17] = msi.Fan2.ConfigureTemp6;

        buffer[18] = (byte)msi.Fan3.Mode;
        buffer[19] = msi.Fan3.ConfigureTemp0;
        buffer[20] = msi.Fan3.ConfigureTemp1;
        buffer[21] = msi.Fan3.ConfigureTemp2;
        buffer[22] = msi.Fan3.ConfigureTemp3;
        buffer[23] = msi.Fan3.ConfigureTemp4;
        buffer[24] = msi.Fan3.ConfigureTemp5;
        buffer[25] = msi.Fan3.ConfigureTemp6;

        buffer[26] = (byte)msi.Fan4.Mode;
        buffer[27] = msi.Fan4.ConfigureTemp0;
        buffer[28] = msi.Fan4.ConfigureTemp1;
        buffer[29] = msi.Fan4.ConfigureTemp2;
        buffer[30] = msi.Fan4.ConfigureTemp3;
        buffer[31] = msi.Fan4.ConfigureTemp4;
        buffer[32] = msi.Fan4.ConfigureTemp5;
        buffer[33] = msi.Fan4.ConfigureTemp6;

        buffer[34] = (byte)msi.Fan5.Mode;
        buffer[35] = msi.Fan5.ConfigureTemp0;
        buffer[36] = msi.Fan5.ConfigureTemp1;
        buffer[37] = msi.Fan5.ConfigureTemp2;
        buffer[38] = msi.Fan5.ConfigureTemp3;
        buffer[39] = msi.Fan5.ConfigureTemp4;
        buffer[40] = msi.Fan5.ConfigureTemp5;
        buffer[41] = msi.Fan5.ConfigureTemp6;

        SetData(buffer);
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
