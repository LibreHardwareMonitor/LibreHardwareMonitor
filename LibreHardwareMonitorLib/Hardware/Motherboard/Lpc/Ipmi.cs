// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc;

// Ported from ipmiutil
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
unsafe struct IpmiSdr
{
    [MarshalAs(UnmanagedType.U2)]
    public ushort recid;
    [MarshalAs(UnmanagedType.U1)]
    public byte sdrver;
    [MarshalAs(UnmanagedType.U1)]
    public byte rectype;
    [MarshalAs(UnmanagedType.U1)]
    public byte reclen;
    [MarshalAs(UnmanagedType.U1)]
    public byte sens_ownid;
    [MarshalAs(UnmanagedType.U1)]
    public byte sens_ownlun;
    [MarshalAs(UnmanagedType.U1)]
    public byte sens_num;
    [MarshalAs(UnmanagedType.U1)]
    public byte entity_id;
    [MarshalAs(UnmanagedType.U1)]
    public byte entity_inst;
    [MarshalAs(UnmanagedType.U1)]
    public byte sens_init;
    [MarshalAs(UnmanagedType.U1)]
    public byte sens_capab;
    [MarshalAs(UnmanagedType.U1)]
    public byte sens_type;
    [MarshalAs(UnmanagedType.U1)]
    public byte ev_type;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 6)]
    public string data1;
    [MarshalAs(UnmanagedType.U1)]
    public byte sens_units;
    [MarshalAs(UnmanagedType.U1)]
    public byte sens_base;
    [MarshalAs(UnmanagedType.U1)]
    public byte sens_mod;
    [MarshalAs(UnmanagedType.U1)]
    public byte linear;
    [MarshalAs(UnmanagedType.U1)]
    public byte m;
    [MarshalAs(UnmanagedType.U1)]
    public byte m_t;
    [MarshalAs(UnmanagedType.U1)]
    public byte b;
    [MarshalAs(UnmanagedType.U1)]
    public byte b_a;
    [MarshalAs(UnmanagedType.U1)]
    public byte a_ax;
    [MarshalAs(UnmanagedType.U1)]
    public byte rx_bx;
    [MarshalAs(UnmanagedType.U1)]
    public byte flags;
    [MarshalAs(UnmanagedType.U1)]
    public byte nom_reading;
    [MarshalAs(UnmanagedType.U1)]
    public byte norm_max;
    [MarshalAs(UnmanagedType.U1)]
    public byte norm_min;
    [MarshalAs(UnmanagedType.U1)]
    public byte sens_max_reading;
    [MarshalAs(UnmanagedType.U1)]
    public byte sens_min_reading;
    [MarshalAs(UnmanagedType.U1)]
    public byte unr_threshold;
    [MarshalAs(UnmanagedType.U1)]
    public byte ucr_threshold;
    [MarshalAs(UnmanagedType.U1)]
    public byte unc_threshold;
    [MarshalAs(UnmanagedType.U1)]
    public byte lnr_threshold;
    [MarshalAs(UnmanagedType.U1)]
    public byte lcr_threshold;
    [MarshalAs(UnmanagedType.U1)]
    public byte lnc_threshold;
    [MarshalAs(UnmanagedType.U1)]
    public byte pos_hysteresis;
    [MarshalAs(UnmanagedType.U1)]
    public byte neg_hysteresis;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 3)]
    public string data3;
    [MarshalAs(UnmanagedType.U1)]
    public byte id_strlen;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    public string id_string;
}

internal class Ipmi : ISuperIO
{
    public Chip Chip { get; }

    public float?[] Controls { get; } = Array.Empty<float?>();

    public float?[] Fans { get; } = Array.Empty<float?>();

    public float?[] Temperatures { get; } = Array.Empty<float?>();

    public float?[] Voltages { get; } = Array.Empty<float?>();

    public readonly List<string> FanNames = new();
    public readonly List<string> TemperatureNames = new();
    public readonly List<string> VoltageNames = new();

    private List<float> _controls = new();
    private List<float> _fans = new();
    private List<float> _temperatures = new();
    private List<float> _voltages = new();

    private ManagementObject _ipmi;

    const byte COMMAND_GET_SDR_REPOSITORY_INFO = 0x20;
    const byte COMMAND_RESERVE_SDR_REPOSITORY = 0x22;
    const byte COMMAND_GET_SDR = 0x23;
    const byte COMMAND_GET_SENSOR_READING = 0x2d;

    const byte NETWORK_FUNCTION_SENSOR_EVENT = 0x4;
    const byte NETWORK_FUNCTION_STORAGE = 0xa;

    public Ipmi()
    {
        Chip = Chip.IPMI;

        ManagementClass ipmiClass = new ManagementClass("root\\WMI", "Microsoft_IPMI", null);

        foreach (ManagementObject ipmi in ipmiClass.GetInstances())
        {
            _ipmi = ipmi;
        }

        // Perform an early update to count the number of sensors and get their names
        Update();

        Controls = new float?[_controls.Count];
        Fans = new float?[_fans.Count];
        Temperatures = new float?[_temperatures.Count];
        Voltages = new float?[_voltages.Count];
    }

    public string GetReport()
    {
        StringBuilder sb = new();
        Update(sb);
        return sb.ToString();
    }

    public void SetControl(int index, byte? value) => throw new NotImplementedException();

    public void Update() { Update(null); }
    private void Update(StringBuilder? debugSB = null)
    {
        _controls.Clear();
        _fans.Clear();
        _temperatures.Clear();
        _voltages.Clear();

        byte[] sdrInfo = RunIPMICommand(COMMAND_GET_SDR_REPOSITORY_INFO, NETWORK_FUNCTION_STORAGE, new byte[] { });
        int recordCount = sdrInfo[3] * 256 + sdrInfo[2];

        byte recordLower = 0;
        byte recordUpper = 0;
        for (int i = 0; i < recordCount; ++i)
        {
            byte[] sdrRaw = RunIPMICommand(COMMAND_GET_SDR, NETWORK_FUNCTION_STORAGE, new byte[] { 0, 0, recordLower, recordUpper, 0, 0xff });

            recordLower = sdrRaw[1];
            recordUpper = sdrRaw[2];

            // Long records unsupported
            if (sdrRaw[0] == 0)
            {
                IpmiSdr sdr;
                unsafe
                {
                    fixed (byte* pSdr = sdrRaw)
                    {
                        sdr = (IpmiSdr)Marshal.PtrToStructure((IntPtr)pSdr + 3, typeof(IpmiSdr));
                    }
                }

                if (sdr.rectype == 1)
                {
                    byte[] reading = RunIPMICommand(COMMAND_GET_SENSOR_READING, NETWORK_FUNCTION_SENSOR_EVENT, new byte[] { sdr.sens_num });

                    if (reading[0] == 0)
                    {
                        switch (sdr.sens_type)
                        {
                            case 1:
                                _temperatures.Add(RawToFloat(reading[1], sdr));
                                if (Temperatures.Length == 0)
                                {
                                    TemperatureNames.Add(sdr.id_string.Replace(" Temp", ""));
                                }
                                break;

                            case 2:
                                _voltages.Add(RawToFloat(reading[1], sdr));
                                if (Voltages.Length == 0)
                                {
                                    VoltageNames.Add(sdr.id_string);
                                }
                                break;

                            case 4:
                                _fans.Add(RawToFloat(reading[1], sdr));
                                if (Fans.Length == 0)
                                {
                                    FanNames.Add(sdr.id_string);
                                }
                                break;

                            default:
                                break;
                        }
                    }

                    if (debugSB != null)
                    {
                        debugSB.AppendLine("IPMI sensor " + i + " reading: " + BitConverter.ToString(reading).Replace("-", "") + " info: " + BitConverter.ToString(sdrRaw).Replace("-", ""));
                    }
                }
            }
        }

        for (int i = 0; i < Math.Min(_temperatures.Count, Temperatures.Length); ++i)
        {
            Temperatures[i] = _temperatures[i];
        }
        for (int i = 0; i < Math.Min(_voltages.Count, Voltages.Length); ++i)
        {
            Voltages[i] = _voltages[i];
        }
        for (int i = 0; i < Math.Min(_fans.Count, Fans.Length); ++i)
        {
            Fans[i] = _fans[i];
        }
    }
    public static bool IsBmcPresent()
    {
        ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\WMI", "select * from Microsoft_IPMI where Active='True'");
        return searcher.Get().Count > 0;
    }

    public byte? ReadGpio(int index)
    {
        return null;
    }

    public void WriteGpio(int index, byte value)
    {
    }

    private byte[] RunIPMICommand(byte command, byte networkFunction, byte[] requestData)
    {
        ManagementBaseObject inParams = _ipmi.GetMethodParameters("RequestResponse");
        inParams["NetworkFunction"] = networkFunction;
        inParams["Lun"] = 0;
        inParams["ResponderAddress"] = 0x20;
        inParams["Command"] = command;
        inParams["RequestDataSize"] = requestData.Length;
        inParams["RequestData"] = requestData;
        ManagementBaseObject outParams = _ipmi.InvokeMethod("RequestResponse", inParams, null);
        return (byte[])outParams["ResponseData"];
    }

    // Ported from ipmiutil
    float RawToFloat(byte sensorReading, IpmiSdr sdr)
    {
        double floatval = (double)sensorReading;
        int m, b, a;
        int ax;
        int rx, b_exp;
        int signval;

        m = sdr.m + ((sdr.m_t & 0xc0) << 2);
        b = sdr.b + ((sdr.b_a & 0xc0) << 2);
        if (Convert.ToBoolean(b & 0x0200)) b = (b - 0x0400);  /*negative*/
        if (Convert.ToBoolean(m & 0x0200)) m = (m - 0x0400);  /*negative*/
        rx = (sdr.rx_bx & 0xf0) >> 4;
        if (Convert.ToBoolean(rx & 0x08)) rx = (rx - 0x10); /*negative*/
        a = (sdr.b_a & 0x3f) + ((sdr.a_ax & 0xf0) << 2);
        ax = (sdr.a_ax & 0x0c) >> 2;
        b_exp = (sdr.rx_bx & 0x0f);
        if (Convert.ToBoolean(b_exp & 0x08)) b_exp = (b_exp - 0x10);  /*negative*/
        
        if ((sdr.sens_units & 0xc0) != 0)
        {  
            if (Convert.ToBoolean(sensorReading & 0x80)) signval = (sensorReading - 0x100);
            else signval = sensorReading;
            floatval = (double)signval;
        }
        floatval *= (double)m;

        floatval += (b * Math.Pow(10, b_exp));
        floatval *= Math.Pow(10, rx);

        if (sdr.linear != 0)
        { 
            throw new NotImplementedException();
        }

        return (float)floatval;
    }

}
