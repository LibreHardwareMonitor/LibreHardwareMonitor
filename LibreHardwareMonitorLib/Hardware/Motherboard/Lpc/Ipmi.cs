// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Management;

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc;
internal class Ipmi : ISuperIO
{
    public Chip Chip { get; }

    public float?[] Controls { get; } = Array.Empty<float?>();

    public float?[] Fans { get; } = Array.Empty<float?>();

    public float?[] Temperatures { get; } = Array.Empty<float?>();

    public float?[] Voltages { get; } = Array.Empty<float?>();

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
    }

    public string GetReport() => throw new NotImplementedException();
    public void SetControl(int index, byte? value) => throw new NotImplementedException();
    public void Update()
    {
        byte[] getSDRInfoResult = RunIPMICommand(COMMAND_GET_SDR_REPOSITORY_INFO, new byte[] { });
        int recordCount = getSDRInfoResult[3] * 256 + getSDRInfoResult[2];

      /*  // Reserve SDR Repository
        byte[] reserveSDRResult = IPMICommand(0x22, new byte[] { });
        byte reserveLower = reserveSDRResult[1];
        byte reserveUpper = reserveSDRResult[2];*/

        byte recordLower = 0;
        byte recordUpper = 0;
        for (int i = 0; i < recordCount; ++i)
        {
            byte[] getSDRResult = RunIPMICommand(COMMAND_GET_SDR, new byte[] { 0, 0, recordLower, recordUpper, 0, 0xff });
            recordLower = getSDRResult[1];
            recordUpper = getSDRResult[2];
            System.Diagnostics.Debug.WriteLine(BitConverter.ToString(getSDRResult).Replace("-", ""));
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
