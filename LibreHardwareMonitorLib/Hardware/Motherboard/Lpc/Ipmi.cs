// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Management;
using System.Text;

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc;
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
    const byte COMMAND_GET_SDR = 0x23;

    public Ipmi()
    {
        Chip = Chip.IPMI;

        ManagementClass ipmiClass = new ManagementClass("root\\WMI", "Microsoft_IPMI", null);

        foreach (ManagementObject ipmi in ipmiClass.GetInstances())
        {
            _ipmi = ipmi;
        }

        // Have to perform an early update to count the number of sensors and get their names
        Update();

        Controls = new float?[_controls.Count];
        Fans = new float?[_fans.Count];
        Temperatures = new float?[_temperatures.Count];
        Voltages = new float?[_voltages.Count];
    }

    public string GetReport() => throw new NotImplementedException();
    public void SetControl(int index, byte? value) => throw new NotImplementedException();
    public void Update()
    {
        _controls.Clear();
        _fans.Clear();
        _temperatures.Clear();
        _voltages.Clear();

        byte[] sdrInfo = RunIPMICommand(COMMAND_GET_SDR_REPOSITORY_INFO, new byte[] { });
        int recordCount = sdrInfo[3] * 256 + sdrInfo[2];

        byte recordLower = 0;
        byte recordUpper = 0;
        for (int i = 0; i < recordCount; ++i)
        {
            byte[] sdr = RunIPMICommand(COMMAND_GET_SDR, new byte[] { 0, 0, recordLower, recordUpper, 0, 0xff });
            recordLower = sdr[1];
            recordUpper = sdr[2];

            if (sdr[6] == 1)
            {
                byte sensorType = sdr[15];
                string sensorName = Encoding.UTF8.GetString(sdr, 51, sdr.Length - 51);
                if (sensorName.IndexOf((char)0) >= 0)
                {
                    sensorName = sensorName.Remove(sensorName.IndexOf((char)0));
                }

                switch (sensorType)
                {
                    case 1:
                        _temperatures.Add(1.0f);
                        if (Temperatures.Length == 0)
                        {
                            TemperatureNames.Add(sensorName);
                        }
                        break;
                }
            }

            //System.Diagnostics.Debug.WriteLine(BitConverter.ToString(getSDRResult).Replace("-", ""));
        }

        for (int i = 0; i < Math.Min(_temperatures.Count, Temperatures.Length); ++i)
        {
            Temperatures[i] = _temperatures[i];
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

    private byte[] RunIPMICommand(int command, byte[] requestData)
    {
        ManagementBaseObject inParams = _ipmi.GetMethodParameters("RequestResponse");
        inParams["NetworkFunction"] = 0xa;
        inParams["Lun"] = 0;
        inParams["ResponderAddress"] = 0x20;
        inParams["Command"] = command;
        inParams["RequestDataSize"] = requestData.Length;
        inParams["RequestData"] = requestData;
        ManagementBaseObject outParams = _ipmi.InvokeMethod("RequestResponse", inParams, null);
        return (byte[])outParams["ResponseData"];
    }
}
