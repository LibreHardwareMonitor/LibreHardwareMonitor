// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Management;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Hardware.Gpu.PowerMonitor.Interop;

namespace LibreHardwareMonitor.Hardware.Gpu.PowerMonitor;

/// <summary>
/// Thermal Grizzly WireView Pro II power monitor.
/// </summary>
public sealed class WireViewPro2 : Hardware, IPowerMonitor
{
    const byte VendorID = 239;
    const byte ProductID = 5;

    string _portName;
    SerialPort _serialPort;
    readonly int _baudRate;

    readonly List<WireViewPro2Sensor> _sensors = new();

    public WireViewPro2(string portName, ISettings settings, int baud = 115200)
        : base("WireView Pro II", new Identifier("gpu-powermonitor", portName), settings)
    {
        _portName = portName;
        _baudRate = baud;

        Connect();

        CreateSensors();
    }

    public override HardwareType HardwareType => HardwareType.GpuPowerMonitor;

    public bool IsConnected { get; private set; }

    public string UniqueID { get; private set; }

    public VendorDataStruct? VendorData { get; private set; }

    public static WireViewPro2 TryFindDevice(ISettings settings)
    {
        var matches = new List<string>();

        using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(COM%)%'"))
        {
            foreach (var obj in searcher.Get())
            {
                if (!(obj["PNPDeviceID"] is string temp))
                {
                    temp = string.Empty;
                }

                if (temp.StartsWith("USB\\VID_0483&PID_5740", StringComparison.OrdinalIgnoreCase)
                 && obj["Name"] is string str)
                {
                    int num1 = str.LastIndexOf("(COM", StringComparison.OrdinalIgnoreCase);
                    int num2 = str.LastIndexOf(")", StringComparison.OrdinalIgnoreCase);

                    if (num1 >= 0 && num2 > num1)
                    {
                        var subStr = str.Substring(num1 + 1, num2 - num1 - 1);

                        matches.Add(subStr);
                    }
                }
            }
        }

        WireViewPro2 wireViewPro2 = null;

        foreach (var port in matches)
        {
            if (port == null)
            {
                continue;
            }

            try
            {
                wireViewPro2 = new WireViewPro2(port, settings);
                if (wireViewPro2.IsConnected)
                {
                    break;
                }
                else
                {
                    wireViewPro2.Dispose();
                    wireViewPro2 = null;
                }
            }
            catch
            {
                wireViewPro2?.Dispose();
                wireViewPro2 = null;
            }
        }

        return wireViewPro2;
    }

    public void Dispose()
    {
        Disconnect();
    }

    public override void Update()
    {
        var sensorValues = ReadSensorValues();

        if (sensorValues.HasValue)
        {
            var deviceData = MapSensorStructure(sensorValues.Value);

            _sensors.ForEach(wvps => wvps.Update(deviceData));
        }
    }

    public DeviceConfigStruct? ReadConfig()
    {
        if (!IsConnected)
        {
            return null;
        }

        _serialPort.DiscardInBuffer();
        _serialPort.Write(new byte[1] { (byte)UsbCmd.CMD_READ_CONFIG }, 0, 1);

        var bytes = ReadExact(Marshal.SizeOf<DeviceConfigStruct>());

        if (bytes == null)
        {
            return null;
        }
        else
        {
            return BytesToStructure<DeviceConfigStruct>(bytes);
        }
    }

    public void WriteConfig(DeviceConfigStruct config)
    {
        if (!IsConnected)
        {
            return;
        }

        var bytes = StructureToBytes(config);

        var buffer = new byte[64];
        buffer[0] = (byte)UsbCmd.CMD_WRITE_CONFIG;

        _serialPort.DiscardInBuffer();

        for (byte i = 0; i < bytes.Length; i += 62)
        {
            var num = Math.Min(62, bytes.Length - i);

            buffer[1] = i;

            Buffer.BlockCopy(bytes, i, buffer, 2, num);
            _serialPort.Write(buffer, 0, num + 2);
        }
    }

    public void NonVolatileMemoryCommand(NVM_CMD cmd)
    {
        if (!IsConnected)
        {
            return;
        }

        _serialPort.DiscardInBuffer();
        _serialPort.Write(new byte[6]
        {
            (byte)UsbCmd.CMD_NVM_CONFIG,
            (byte)85, //Magic
            (byte)170, //Magic
            (byte)85, //Magic
            (byte)170, //Magic
            (byte)cmd,
        }, 0, 6);
    }

    void CreateSensors()
    {
        //"Default" temperature sensors
        AddSensor("Onboard Temp In", 0, false, SensorType.Temperature, dd => (float)dd.OnboardTempInC);
        AddSensor("Onboard Temp Out", 1, false, SensorType.Temperature, dd => (float)dd.OnboardTempOutC);

        //External temperature sensors, requires shipped temperature sensors to be connected
        AddSensor("External Temp 1", 2, false, SensorType.Temperature, dd => (float)dd.ExternalTemp1C);
        AddSensor("External Temp 2", 3, false, SensorType.Temperature, dd => (float)dd.ExternalTemp2C);

        //Pin voltages
        AddSensor("Pin 1 Voltage", 10, false, SensorType.Voltage, dd => (float)dd.PinVoltage[0]);
        AddSensor("Pin 2 Voltage", 11, false, SensorType.Voltage, dd => (float)dd.PinVoltage[1]);
        AddSensor("Pin 3 Voltage", 12, false, SensorType.Voltage, dd => (float)dd.PinVoltage[2]);
        AddSensor("Pin 4 Voltage", 13, false, SensorType.Voltage, dd => (float)dd.PinVoltage[3]);
        AddSensor("Pin 5 Voltage", 14, false, SensorType.Voltage, dd => (float)dd.PinVoltage[4]);
        AddSensor("Pin 6 Voltage", 15, false, SensorType.Voltage, dd => (float)dd.PinVoltage[5]);

        //Pin currents
        AddSensor("Total Current", 20, false, SensorType.Voltage, dd => (float)dd.SumCurrentA);
        AddSensor("Pin 1 Current", 21, false, SensorType.Current, dd => (float)dd.PinCurrent[0]);
        AddSensor("Pin 2 Current", 22, false, SensorType.Current, dd => (float)dd.PinCurrent[1]);
        AddSensor("Pin 3 Current", 23, false, SensorType.Current, dd => (float)dd.PinCurrent[2]);
        AddSensor("Pin 4 Current", 24, false, SensorType.Current, dd => (float)dd.PinCurrent[3]);
        AddSensor("Pin 5 Current", 25, false, SensorType.Current, dd => (float)dd.PinCurrent[4]);
        AddSensor("Pin 6 Current", 26, false, SensorType.Current, dd => (float)dd.PinCurrent[5]);

        //Power
        AddSensor("Total Power", 30, false, SensorType.Power, dd => (float)dd.SumPowerW);
    }

    private void AddSensor(string name, int index, bool defaultHidden, SensorType sensorType, GetWireViewPro2SensorValue getValue)
    {
        var sensor = new WireViewPro2Sensor(name, index, sensorType, this, _settings, getValue);

        _sensors.Add(sensor);

        ActivateSensor(sensor);
    }

    void Connect()
    {
        if (IsConnected)
        {
            return;
        }

        _serialPort = new SerialPort(_portName, _baudRate, Parity.None, 8, StopBits.One);
        _serialPort.ReadTimeout = 1000;
        _serialPort.WriteTimeout = 1000;
        _serialPort.Open();

        var vendorData = ReadVendorData();

        if (vendorData.HasValue
         && vendorData.Value.VendorId == VendorID
         && vendorData.Value.ProductId == ProductID)
        {
            VendorData = vendorData.Value;
            UniqueID = ReadUniqueID();

            IsConnected = true;
        }
    }

    void Disconnect()
    {
        if (!IsConnected)
        {
            return;
        }

        _serialPort.Close();

        IsConnected = false;

        VendorData = null;
        UniqueID = null;
    }

    VendorDataStruct? ReadVendorData()
    {
        _serialPort.DiscardInBuffer();
        _serialPort.Write(new byte[1] { (byte)UsbCmd.CMD_READ_VENDOR_DATA }, 0, 1);

        var bytes = ReadExact(Marshal.SizeOf<VendorDataStruct>());

        if (bytes == null)
        {
            return null;
        }
        else
        {
            return BytesToStructure<VendorDataStruct>(bytes);
        }
    }

    string ReadUniqueID()
    {
        _serialPort.DiscardInBuffer();
        _serialPort.Write(new byte[1] { (byte)UsbCmd.CMD_READ_UID }, 0, 1);

        var bytes = ReadExact(12);

        if (bytes == null)
        {
            return null;
        }
        else
        {
            return BitConverter.ToString(bytes).Replace("-", string.Empty);
        }
    }

    SensorStruct? ReadSensorValues()
    {
        if (!IsConnected)
        {
            return null;
        }

        _serialPort.DiscardInBuffer();

        _serialPort.Write(new byte[1] { (byte)UsbCmd.CMD_READ_SENSOR_VALUES }, 0, 1);

        var bytes = ReadExact(Marshal.SizeOf<SensorStruct>());

        if (bytes == null)
        {
            return null;
        }
        else
        {
            return BytesToStructure<SensorStruct>(bytes);
        }
    }

    DeviceData MapSensorStructure(SensorStruct sensorStruct)
    {
        var deviceData = new DeviceData()
        {
            Connected = true,
            HardwareRevision = $"{VendorData?.VendorId}{VendorData?.ProductId}",
            FirmwareVersion = $"{VendorData?.FwVersion}",
            OnboardTempInC = sensorStruct.Ts[(int)SensorTs.SENSOR_TS_IN] / 10.0,
            OnboardTempOutC = sensorStruct.Ts[(int)SensorTs.SENSOR_TS_OUT] / 10.0,
            ExternalTemp1C = sensorStruct.Ts[(int)SensorTs.SENSOR_TS3] / 10.0,
            ExternalTemp2C = sensorStruct.Ts[(int)SensorTs.SENSOR_TS4] / 10.0,
        };

        switch (sensorStruct.HpwrCapability)
        {
            case HpwrCapability.PSU_CAP_600W:
                deviceData.PsuCapabilityW = 600;
                break;
            case HpwrCapability.PSU_CAP_450W:
                deviceData.PsuCapabilityW = 450;
                break;
            case HpwrCapability.PSU_CAP_300W:
                deviceData.PsuCapabilityW = 300;
                break;
            case HpwrCapability.PSU_CAP_150W:
                deviceData.PsuCapabilityW = 150;
                break;
        }

        for (int i = 0; i < 6; ++i)
        {
            deviceData.PinVoltage[i] = sensorStruct.PowerReadings[i].Voltage / 1000.0;
            deviceData.PinCurrent[i] = sensorStruct.PowerReadings[i].Current / 1000.0;
        }

        return deviceData;
    }

    byte[] ReadExact(int size)
    {
        var buffer = new byte[size];

        int offset = 0;
        int tickCount = Environment.TickCount;

        while (offset < size && (Environment.TickCount - tickCount) < _serialPort.ReadTimeout)
        {
            try
            {
                if (_serialPort.BytesToRead > 0)
                {
                    offset += _serialPort.Read(buffer, offset, size - offset);
                }

                if (offset != size)
                {
                    return null;
                }
                else
                {
                    return buffer;
                }
            }
            catch (TimeoutException)
            {
                //Ignore timeout exceptions and continue reading
            }
        }

        return null;
    }

    T BytesToStructure<T>(byte[] bytes)
        where T : struct
    {
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);

        try
        {
            return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    byte[] StructureToBytes<T>(T value)
        where T : struct
    {
        int size = Marshal.SizeOf<T>();

        var bytes = new byte[size];
        var ptr = Marshal.AllocHGlobal(size);

        try
        {
            Marshal.StructureToPtr(value, ptr, false);
            Marshal.Copy(ptr, bytes, 0, size);
            return bytes;
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }
}
