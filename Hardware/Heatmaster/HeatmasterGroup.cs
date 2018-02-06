/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2010 Michael Möller <mmoeller@openhardwaremonitor.org>
	
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Security;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using OperatingSystem = OpenHardwareMonitor.Software.OperatingSystem;

namespace OpenHardwareMonitor.Hardware.Heatmaster
{
    internal class HeatmasterGroup : IGroup
    {
        private readonly List<Heatmaster> hardware = new List<Heatmaster>();
        private readonly StringBuilder report = new StringBuilder();

        public HeatmasterGroup(ISettings settings)
        {
            // No implementation for Heatmaster on Unix systems
            if (OperatingSystem.IsLinux)
                return;

            var portNames = GetRegistryPortNames();
            for (var i = 0; i < portNames.Length; i++)
            {
                var isValid = false;
                try
                {
                    using (var serialPort =
                        new SerialPort(portNames[i], 38400, Parity.None, 8, StopBits.One))
                    {
                        serialPort.NewLine = ((char) 0x0D).ToString();
                        report.Append("Port Name: ");
                        report.AppendLine(portNames[i]);

                        try
                        {
                            serialPort.Open();
                        }
                        catch (UnauthorizedAccessException)
                        {
                            report.AppendLine("Exception: Access Denied");
                        }

                        if (serialPort.IsOpen)
                        {
                            serialPort.DiscardInBuffer();
                            serialPort.DiscardOutBuffer();
                            serialPort.Write(new byte[] {0xAA}, 0, 1);

                            var j = 0;
                            while (serialPort.BytesToRead == 0 && j < 10)
                            {
                                Thread.Sleep(20);
                                j++;
                            }
                            if (serialPort.BytesToRead > 0)
                            {
                                var flag = false;
                                while (serialPort.BytesToRead > 0 && !flag) flag |= serialPort.ReadByte() == 0xAA;
                                if (flag)
                                {
                                    serialPort.WriteLine("[0:0]RH");
                                    try
                                    {
                                        var k = 0;
                                        var revision = 0;
                                        while (k < 5)
                                        {
                                            var line = ReadLine(serialPort, 100);
                                            if (line.StartsWith("-[0:0]RH:",
                                                StringComparison.Ordinal))
                                            {
                                                revision = int.Parse(line.Substring(9),
                                                    CultureInfo.InvariantCulture);
                                                break;
                                            }
                                            k++;
                                        }
                                        isValid = revision == 770;
                                        if (!isValid)
                                            report.Append("Status: Wrong Hardware Revision " +
                                                          revision.ToString(CultureInfo.InvariantCulture));
                                    }
                                    catch (TimeoutException)
                                    {
                                        report.AppendLine("Status: Timeout Reading Revision");
                                    }
                                }
                                else
                                {
                                    report.AppendLine("Status: Wrong Startflag");
                                }
                            }
                            else
                            {
                                report.AppendLine("Status: No Response");
                            }
                            serialPort.DiscardInBuffer();
                        }
                        else
                        {
                            report.AppendLine("Status: Port not Open");
                        }
                    }
                }
                catch (Exception e)
                {
                    report.AppendLine(e.ToString());
                }

                if (isValid)
                {
                    report.AppendLine("Status: OK");
                    hardware.Add(new Heatmaster(portNames[i], settings));
                }
                report.AppendLine();
            }
        }

        public IHardware[] Hardware => hardware.ToArray();

        public string GetReport()
        {
            if (report.Length > 0)
            {
                var r = new StringBuilder();
                r.AppendLine("Serial Port Heatmaster");
                r.AppendLine();
                r.Append(report);
                r.AppendLine();
                return r.ToString();
            }
            return null;
        }

        public void Close()
        {
            foreach (var heatmaster in hardware)
                heatmaster.Close();
        }

        private static string ReadLine(SerialPort port, int timeout)
        {
            var i = 0;
            var builder = new StringBuilder();
            while (i < timeout)
            {
                while (port.BytesToRead > 0)
                {
                    var b = (byte) port.ReadByte();
                    switch (b)
                    {
                        case 0xAA: return ((char) b).ToString();
                        case 0x0D: return builder.ToString();
                        default:
                            builder.Append((char) b);
                            break;
                    }
                }
                i++;
                Thread.Sleep(1);
            }
            throw new TimeoutException();
        }

        private static string[] GetRegistryPortNames()
        {
            var result = new List<string>();
            string[] paths = {"", "&MI_00"};
            try
            {
                foreach (var path in paths)
                {
                    var key = Registry.LocalMachine.OpenSubKey(
                        @"SYSTEM\CurrentControlSet\Enum\USB\VID_10C4&PID_EA60" + path);
                    if (key != null)
                        foreach (var subKeyName in key.GetSubKeyNames())
                        {
                            var subKey =
                                key.OpenSubKey(subKeyName + "\\" + "Device Parameters");
                            var name = subKey?.GetValue("PortName") as string;
                            if (name != null && !result.Contains(name))
                                result.Add(name);
                        }
                }
            }
            catch (SecurityException)
            {
            }
            return result.ToArray();
        }
    }
}