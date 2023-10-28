// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc;

internal class Ipmi : ISuperIO
{
    // ReSharper disable InconsistentNaming
    private const byte COMMAND_FAN_LEVEL = 0x70;
    private const byte COMMAND_FAN_MODE = 0x45;
    private const byte COMMAND_GET_SDR = 0x23;
    private const byte COMMAND_GET_SDR_REPOSITORY_INFO = 0x20;
    private const byte COMMAND_GET_SENSOR_READING = 0x2d;

    private const byte FAN_MODE_FULL = 0x01;
    private const byte FAN_MODE_OPTIMAL = 0x02;

    private const byte NETWORK_FUNCTION_SENSOR_EVENT = 0x04;
    private const byte NETWORK_FUNCTION_STORAGE = 0x0a;
    private const byte NETWORK_FUNCTION_SUPERMICRO = 0x30;
    // ReSharper restore InconsistentNaming

    private readonly List<string> _controlNames = new();
    private readonly List<float> _controls = new();
    private readonly List<string> _fanNames = new();
    private readonly List<float> _fans = new();

    private readonly ManagementObject _ipmi;
    private readonly Manufacturer _manufacturer;

    private readonly List<Interop.Ipmi.Sdr> _sdrs = new();
    private readonly List<string> _temperatureNames = new();
    private readonly List<float> _temperatures = new();
    private readonly List<string> _voltageNames = new();
    private readonly List<float> _voltages = new();

    private bool _touchedFans;

    public Ipmi(Manufacturer manufacturer)
    {
        Chip = Chip.IPMI;
        _manufacturer = manufacturer;

        using ManagementClass ipmiClass = new("root\\WMI", "Microsoft_IPMI", null);

        foreach (ManagementBaseObject ipmi in ipmiClass.GetInstances())
        {
            if (ipmi is ManagementObject managementObject)
                _ipmi = managementObject;
        }

        // Fan control is exposed for Supermicro only as it differs between IPMI implementations
        if (_manufacturer == Manufacturer.Supermicro)
        {
            _controlNames.Add("CPU Fan");
            _controlNames.Add("System Fan");
        }

        // Perform an early update to count the number of sensors and get their names
        Update();

        Controls = new float?[_controls.Count];
        Fans = new float?[_fans.Count];
        Temperatures = new float?[_temperatures.Count];
        Voltages = new float?[_voltages.Count];
    }

    public Chip Chip { get; }

    public float?[] Controls { get; }

    public float?[] Fans { get; }

    public float?[] Temperatures { get; }

    public float?[] Voltages { get; }

    public string GetReport()
    {
        StringBuilder sb = new();
        Update(sb);
        return sb.ToString();
    }

    public void SetControl(int index, byte? value)
    {
        if (_manufacturer == Manufacturer.Supermicro)
        {
            if (value != null || _touchedFans)
            {
                _touchedFans = true;

                if (value == null)
                {
                    RunIPMICommand(COMMAND_FAN_MODE, NETWORK_FUNCTION_SUPERMICRO, new byte[] { 0x01 /* Set */, FAN_MODE_OPTIMAL });
                }
                else
                {
                    byte[] fanMode = RunIPMICommand(COMMAND_FAN_MODE, NETWORK_FUNCTION_SUPERMICRO, new byte[] { 0x00 });
                    if (fanMode == null || fanMode.Length < 2 || fanMode[0] != 0 || fanMode[1] != FAN_MODE_FULL)
                        RunIPMICommand(COMMAND_FAN_MODE, NETWORK_FUNCTION_SUPERMICRO, new byte[] { 0x01 /* Set */, FAN_MODE_FULL });

                    float speed = (float)value / 255.0f * 100.0f;
                    RunIPMICommand(COMMAND_FAN_LEVEL, NETWORK_FUNCTION_SUPERMICRO, new byte[] { 0x66, 0x01 /* Set */, (byte)index, (byte)speed });
                }
            }
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public void Update()
    {
        Update(null);
    }

    private unsafe void Update(StringBuilder stringBuilder)
    {
        _fans.Clear();
        _temperatures.Clear();
        _voltages.Clear();
        _controls.Clear();

        if (_sdrs.Count == 0 || stringBuilder != null)
        {
            byte[] sdrInfo = RunIPMICommand(COMMAND_GET_SDR_REPOSITORY_INFO, NETWORK_FUNCTION_STORAGE, new byte[] { });
            if (sdrInfo?[0] == 0)
            {
                int recordCount = (sdrInfo[3] * 256) + sdrInfo[2];

                byte recordLower = 0;
                byte recordUpper = 0;
                for (int i = 0; i < recordCount; ++i)
                {
                    byte[] sdrRaw = RunIPMICommand(COMMAND_GET_SDR, NETWORK_FUNCTION_STORAGE, new byte[] { 0, 0, recordLower, recordUpper, 0, 0xff });
                    if (sdrRaw?.Length >= 3 && sdrRaw[0] == 0)
                    {
                        recordLower = sdrRaw[1];
                        recordUpper = sdrRaw[2];

                        fixed (byte* pSdr = sdrRaw)
                        {
                            Interop.Ipmi.Sdr sdr = (Interop.Ipmi.Sdr)Marshal.PtrToStructure((IntPtr)pSdr + 3, typeof(Interop.Ipmi.Sdr));
                            _sdrs.Add(sdr);
                            stringBuilder?.AppendLine("IPMI sensor " + i + " num: " + sdr.sens_num + " info: " + BitConverter.ToString(sdrRaw).Replace("-", ""));
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        foreach (Interop.Ipmi.Sdr sdr in _sdrs)
        {
            if (sdr.rectype == 1)
            {
                byte[] reading = RunIPMICommand(COMMAND_GET_SENSOR_READING, NETWORK_FUNCTION_SENSOR_EVENT, new[] { sdr.sens_num });
                if (reading?.Length > 1 && reading[0] == 0)
                {
                    switch (sdr.sens_type)
                    {
                        case 1:
                            _temperatures.Add(RawToFloat(reading[1], sdr));
                            if (Temperatures == null || Temperatures.Length == 0)
                                _temperatureNames.Add(sdr.id_string.Replace(" Temp", ""));

                            break;

                        case 2:
                            _voltages.Add(RawToFloat(reading[1], sdr));
                            if (Voltages == null || Voltages.Length == 0)
                                _voltageNames.Add(sdr.id_string);

                            break;

                        case 4:
                            _fans.Add(RawToFloat(reading[1], sdr));
                            if (Fans == null || Fans.Length == 0)
                                _fanNames.Add(sdr.id_string);

                            break;
                    }

                    stringBuilder?.AppendLine("IPMI sensor num: " + sdr.sens_num + " reading: " + BitConverter.ToString(reading).Replace("-", ""));
                }
            }
        }

        if (_manufacturer == Manufacturer.Supermicro)
        {
            for (int i = 0; i < _controlNames.Count; ++i)
            {
                byte[] fanLevel = RunIPMICommand(COMMAND_FAN_LEVEL, NETWORK_FUNCTION_SUPERMICRO, new byte[] { 0x66, 0x00 /* Get */, (byte)i });
                if (fanLevel?.Length >= 2 && fanLevel[0] == 0)
                {
                    _controls.Add(fanLevel[1]);

                    stringBuilder?.AppendLine("IPMI fan " + i + ": " + BitConverter.ToString(fanLevel).Replace("-", ""));
                }
            }
        }

        if (Temperatures != null)
        {
            for (int i = 0; i < Math.Min(_temperatures.Count, Temperatures.Length); ++i)
                Temperatures[i] = _temperatures[i];
        }

        if (Voltages != null)
        {
            for (int i = 0; i < Math.Min(_voltages.Count, Voltages.Length); ++i)
                Voltages[i] = _voltages[i];
        }

        if (Fans != null)
        {
            for (int i = 0; i < Math.Min(_fans.Count, Fans.Length); ++i)
                Fans[i] = _fans[i];
        }

        if (Controls != null)
        {
            for (int i = 0; i < Math.Min(_controls.Count, Controls.Length); ++i)
                Controls[i] = _controls[i];
        }
    }

    public IEnumerable<Temperature> GetTemperatures()
    {
        for (int i = 0; i < _temperatureNames.Count; i++)
            yield return new Temperature(_temperatureNames[i], i);
    }

    public IEnumerable<Fan> GetFans()
    {
        for (int i = 0; i < _fanNames.Count; i++)
            yield return new Fan(_fanNames[i], i);
    }

    public IEnumerable<Voltage> GetVoltages()
    {
        for (int i = 0; i < _voltageNames.Count; i++)
            yield return new Voltage(_voltageNames[i], i);
    }

    public IEnumerable<Control> GetControls()
    {
        for (int i = 0; i < _controlNames.Count; i++)
            yield return new Control(_controlNames[i], i);
    }

    public static bool IsBmcPresent()
    {
        try
        {
            using ManagementObjectSearcher searcher = new("root\\WMI", "SELECT * FROM Microsoft_IPMI WHERE Active='True'");
            return searcher.Get().Count > 0;
        }
        catch
        {
            return false;
        }
    }

    public byte? ReadGpio(int index)
    {
        return null;
    }

    public void WriteGpio(int index, byte value)
    { }

    private byte[] RunIPMICommand(byte command, byte networkFunction, byte[] requestData)
    {
        using ManagementBaseObject inParams = _ipmi.GetMethodParameters("RequestResponse");

        inParams["NetworkFunction"] = networkFunction;
        inParams["Lun"] = 0;
        inParams["ResponderAddress"] = 0x20;
        inParams["Command"] = command;
        inParams["RequestDataSize"] = requestData.Length;
        inParams["RequestData"] = requestData;

        using ManagementBaseObject outParams = _ipmi.InvokeMethod("RequestResponse", inParams, null);
        return (byte[])outParams["ResponseData"];
    }

    // Ported from ipmiutil
    // Bare minimum to read Supermicro X13 IPMI sensors, may need expanding for other boards
    private static float RawToFloat(byte sensorReading, Interop.Ipmi.Sdr sdr)
    {
        double reading = sensorReading;

        int m = sdr.m + ((sdr.m_t & 0xc0) << 2);
        if (Convert.ToBoolean(m & 0x0200))
            m -= 0x0400;

        int b = sdr.b + ((sdr.b_a & 0xc0) << 2);
        if (Convert.ToBoolean(b & 0x0200))
            b -= 0x0400;

        int rx = (sdr.rx_bx & 0xf0) >> 4;
        if (Convert.ToBoolean(rx & 0x08))
            rx -= 0x10;

        int bExp = sdr.rx_bx & 0x0f;
        if (Convert.ToBoolean(bExp & 0x08))
            bExp -= 0x10;

        if ((sdr.sens_units & 0xc0) != 0)
            reading = Convert.ToBoolean(sensorReading & 0x80) ? sensorReading - 0x100 : sensorReading;

        reading *= m;
        reading += b * Math.Pow(10, bExp);
        reading *= Math.Pow(10, rx);

        if (sdr.linear != 0)
            throw new NotImplementedException();

        return (float)reading;
    }
}
