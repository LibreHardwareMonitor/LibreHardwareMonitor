// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Management;
using System.Threading;
using LibreHardwareMonitor.Hardware.Motherboard.Smbus;

namespace LibreHardwareMonitor.Hardware.Motherboard
{
    internal sealed class SmbusIO
    {
        public static Func<byte, byte, ushort> GetWord;
        public static Func<byte, byte, List<byte>, int> GetBlock;
 
        public static void DeviceDetect(byte address, ISettings settings)
        {
            string deviceName = Pmbus.Detect(address);
            if (deviceName != "")
                Motherboard._smbusSensorList.Add(new Pmbus(deviceName, settings, address));
        }

        public static void SmbusDetect(ISettings settings)
        {
            try
            {
                string wmiQuery = "SELECT * FROM Win32_PnPSignedDriver WHERE Description LIKE '%%SMBUS%%' OR Description LIKE '%%SM BUS%%'";
                var searcher = new ManagementObjectSearcher(wmiQuery);
                ManagementObjectCollection collection = searcher.Get();
                string manufacturer = "";
                foreach (var obj in collection)
                {
                    manufacturer = obj["Manufacturer"].ToString().ToUpper();
                    if (manufacturer.Equals("INTEL") == true)
                    {
                        wmiQuery = "SELECT * FROM Win32_PnPAllocatedResource";
                        string deviceID = obj["DeviceID"].ToString().Substring(4, 33);

                        var searcher2 = new ManagementObjectSearcher(wmiQuery);
                        ManagementObjectCollection collection2 = searcher2.Get();
                        foreach (var obj2 in collection2)
                        {
                            string dependent = obj2["Dependent"].ToString();
                            string antecedent = obj2["Antecedent"].ToString();

                            if (dependent.IndexOf(deviceID) >= 0 && antecedent.IndexOf("Port") >= 0)
                            {
                                string[] antecedentArray = antecedent.Split('=');
                                if (antecedentArray.Length >= 2)
                                {
                                    string addressString = antecedentArray[1].Replace("\"", "");
                                    if (addressString.Length > 0)
                                    {
                                        ushort startAddress = ushort.Parse(addressString);
                                        IntelSmbus.SetSMBAddress(startAddress);

                                        GetWord = IntelSmbus.GetWord;
                                        GetBlock = IntelSmbus.GetBlock;
                                        
                                        if (Ring0.WaitSmBusMutex(100))
                                        {
                                            for (byte addr = 0x01; addr < 0x7F; addr++)
                                            {
                                                if ((addr >= 0x50) && (addr < 0x60))
                                                    // skip SPD addresses not sure how high they go but should allow for at least 16
                                                    continue;

                                                byte data = IntelSmbus.SmbDetect(addr);
                                                if (data == addr)
                                                {
                                                    DeviceDetect(addr, settings);
                                                }
                                                Thread.Sleep(10);
                                            }
                                            Ring0.ReleaseSmBusMutex();
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (manufacturer.Equals("ADVANCED MICRO DEVICES, INC") == true)
                    {
                        GetWord = AmdSmbus.GetWord;
                        GetBlock = AmdSmbus.GetBlock;

                        if (Ring0.WaitSmBusMutex(100))
                        {
                            for (byte addr = 0x01; addr < 0x7F; addr++)
                            {
                                if ((addr >= 0x50) && (addr < 0x60))
                                    continue;

                                byte data = AmdSmbus.SmbDetect(addr);
                                if (data == addr)
                                {
                                    DeviceDetect(addr, settings);
                                }
                                Thread.Sleep(10);
                            }
                            Ring0.ReleaseSmBusMutex();
                            return;
                        }
                    }
                }
            }
            catch { }
        }
    }
}
