// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using LibreHardwareMonitor.Interop;

// ReSharper disable InconsistentNaming

namespace LibreHardwareMonitor.Hardware.Controller.TBalancer;

internal class TBalancerGroup : IGroup
{
    private readonly List<TBalancer> _hardware = new();
    private readonly StringBuilder _report = new();

    public TBalancerGroup(ISettings settings)
    {
        uint numDevices;

        try
        {
            if (!Ftd2xx.DllExists())
            {
                _report.AppendLine("Status: missing DLL");
                return;
            }

            if (Ftd2xx.FT_CreateDeviceInfoList(out numDevices) != Ftd2xx.FT_STATUS.FT_OK)
            {
                _report.AppendLine("Status: FT_CreateDeviceInfoList failed");
                return;
            }
        }
        catch (Exception e) when (e is DllNotFoundException or ArgumentNullException or EntryPointNotFoundException or BadImageFormatException)
        {
            return;
        }

        Ftd2xx.FT_DEVICE_INFO_NODE[] info = new Ftd2xx.FT_DEVICE_INFO_NODE[numDevices];
        if (Ftd2xx.FT_GetDeviceInfoList(info, ref numDevices) != Ftd2xx.FT_STATUS.FT_OK)
        {
            _report.AppendLine("Status: FT_GetDeviceInfoList failed");
            return;
        }

        // make sure numDevices is not larger than the info array
        if (numDevices > info.Length)
            numDevices = (uint)info.Length;

        for (int i = 0; i < numDevices; i++)
        {
            _report.Append("Device Index: ");
            _report.AppendLine(i.ToString(CultureInfo.InvariantCulture));
            _report.Append("Device Type: ");
            _report.AppendLine(info[i].Type.ToString());

            // the T-Balancer always uses an FT232BM
            if (info[i].Type != Ftd2xx.FT_DEVICE.FT_DEVICE_232BM)
            {
                _report.AppendLine("Status: Wrong device type");
                continue;
            }

            Ftd2xx.FT_STATUS status = Ftd2xx.FT_Open(i, out Ftd2xx.FT_HANDLE handle);
            if (status != Ftd2xx.FT_STATUS.FT_OK)
            {
                _report.AppendLine("Open Status: " + status);
                continue;
            }

            Ftd2xx.FT_SetBaudRate(handle, 19200);
            Ftd2xx.FT_SetDataCharacteristics(handle, 8, 1, 0);
            Ftd2xx.FT_SetFlowControl(handle, Ftd2xx.FT_FLOW_CONTROL.FT_FLOW_RTS_CTS, 0x11, 0x13);
            Ftd2xx.FT_SetTimeouts(handle, 1000, 1000);
            Ftd2xx.FT_Purge(handle, Ftd2xx.FT_PURGE.FT_PURGE_ALL);

            status = Ftd2xx.Write(handle, new byte[] { 0x38 });
            if (status != Ftd2xx.FT_STATUS.FT_OK)
            {
                _report.AppendLine("Write Status: " + status);
                Ftd2xx.FT_Close(handle);
                continue;
            }

            bool isValid = false;
            byte protocolVersion = 0;

            int j = 0;
            while (Ftd2xx.BytesToRead(handle) == 0 && j < 2)
            {
                Thread.Sleep(100);
                j++;
            }

            if (Ftd2xx.BytesToRead(handle) > 0)
            {
                if (Ftd2xx.ReadByte(handle) == TBalancer.StartFlag)
                {
                    while (Ftd2xx.BytesToRead(handle) < 284 && j < 5)
                    {
                        Thread.Sleep(100);
                        j++;
                    }

                    int length = Ftd2xx.BytesToRead(handle);
                    if (length >= 284)
                    {
                        byte[] data = new byte[285];
                        data[0] = TBalancer.StartFlag;
                        for (int k = 1; k < data.Length; k++)
                            data[k] = Ftd2xx.ReadByte(handle);

                        // check protocol version 2X (protocols seen: 2C, 2A, 28)
                        isValid = (data[274] & 0xF0) == 0x20;
                        protocolVersion = data[274];
                        if (!isValid)
                        {
                            _report.Append("Status: Wrong Protocol Version: 0x");
                            _report.AppendLine(protocolVersion.ToString("X", CultureInfo.InvariantCulture));
                        }
                    }
                    else
                    {
                        _report.AppendLine("Status: Wrong Message Length: " + length);
                    }
                }
                else
                {
                    _report.AppendLine("Status: Wrong Startflag");
                }
            }
            else
            {
                _report.AppendLine("Status: No Response");
            }

            Ftd2xx.FT_Purge(handle, Ftd2xx.FT_PURGE.FT_PURGE_ALL);
            Ftd2xx.FT_Close(handle);

            if (isValid)
            {
                _report.AppendLine("Status: OK");
                _hardware.Add(new TBalancer(i, protocolVersion, settings));
            }

            if (i < numDevices - 1)
                _report.AppendLine();
        }
    }

    public IReadOnlyList<IHardware> Hardware => _hardware;

    public string GetReport()
    {
        if (_report.Length > 0)
        {
            StringBuilder r = new();
            r.AppendLine("FTD2XX");
            r.AppendLine();
            r.Append(_report);
            r.AppendLine();
            return r.ToString();
        }

        return null;
    }

    public void Close()
    {
        foreach (TBalancer balancer in _hardware)
            balancer.Close();
    }
}