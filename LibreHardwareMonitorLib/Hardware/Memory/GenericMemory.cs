// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using LibreHardwareMonitor.Interop;

namespace LibreHardwareMonitor.Hardware.Memory
{
    internal sealed class GenericMemory : Hardware
    {
        private readonly Sensor _physicalMemoryAvailable;
        private readonly Sensor _physicalMemoryLoad;
        private readonly Sensor _physicalMemoryUsed;
        private readonly Sensor _virtualMemoryAvailable;
        private readonly Sensor _virtualMemoryLoad;
        private readonly Sensor _virtualMemoryUsed;

        private readonly List<DimmSensor> _dimmSensorList = new List<DimmSensor>();

        public GenericMemory(string name, ISettings settings) : base(name, new Identifier("ram"), settings)
        {
            _physicalMemoryUsed = new Sensor("Memory Used", 0, SensorType.Data, this, settings);
            ActivateSensor(_physicalMemoryUsed);

            _physicalMemoryAvailable = new Sensor("Memory Available", 1, SensorType.Data, this, settings);
            ActivateSensor(_physicalMemoryAvailable);

            _physicalMemoryLoad = new Sensor("Memory", 0, SensorType.Load, this, settings);
            ActivateSensor(_physicalMemoryLoad);

            _virtualMemoryUsed = new Sensor("Virtual Memory Used", 2, SensorType.Data, this, settings);
            ActivateSensor(_virtualMemoryUsed);

            _virtualMemoryAvailable = new Sensor("Virtual Memory Available", 3, SensorType.Data, this, settings);
            ActivateSensor(_virtualMemoryAvailable);

            _virtualMemoryLoad = new Sensor("Virtual Memory", 1, SensorType.Load, this, settings);
            ActivateSensor(_virtualMemoryLoad);

            AddDimm(settings);
        }

        private void AddDimm(ISettings settings)
        {
            try
            {
                string wmiQuery = "SELECT * FROM Win32_PnPSignedDriver WHERE Description LIKE '%%SMBUS%%' OR Description LIKE '%%SM BUS%%'";
                var searcher = new ManagementObjectSearcher(wmiQuery);
                var collection = searcher.Get();
                string manufacturer = "";
                foreach (var obj in collection)
                {
                    manufacturer = obj["Manufacturer"].ToString().ToUpper();
                    if (manufacturer.Equals("INTEL") == true)
                    {
                        wmiQuery = "SELECT * FROM Win32_PnPAllocatedResource";
                        string deviceID = obj["DeviceID"].ToString().Substring(4, 33);

                        var searcher2 = new ManagementObjectSearcher(wmiQuery);
                        var collection2 = searcher2.Get();
                        foreach (var obj2 in collection2)
                        {
                            string dependent = obj2["Dependent"].ToString();
                            string antecedent = obj2["Antecedent"].ToString();

                            if (dependent.IndexOf(deviceID) >= 0 && antecedent.IndexOf("Port") >= 0)
                            {
                                var antecedentArray = antecedent.Split('=');
                                if (antecedentArray.Length >= 2)
                                {
                                    string addressString = antecedentArray[1].Replace("\"", "");
                                    if (addressString.Length > 0)
                                    {
                                        ushort startAddress = ushort.Parse(addressString);
                                        IntelDimmSensor.SetSMBAddress(startAddress);

                                        if (Ring0.WaitSmBusMutex(100))
                                        {
                                            int index = 0;
                                            for (byte addr = 0x18; addr < 0x20; addr++)
                                            {
                                                var data = IntelDimmSensor.SmbDetect(addr);
                                                if (data == addr)
                                                {
                                                    var sensor = new IntelDimmSensor("DIMM #" + index, index, this, settings, addr);
                                                    _dimmSensorList.Add(sensor);
                                                    ActivateSensor(sensor);
                                                }
                                                Thread.Sleep(10);
                                                index++;
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
                        if (Ring0.WaitSmBusMutex(100))
                        {
                            int index = 0;
                            for (byte addr = 0x18; addr < 0x20; addr++)
                            {
                                var data = AmdDimmSensor.SmbDetect(addr);
                                if (data == addr)
                                {
                                    var sensor = new AmdDimmSensor("DIMM #" + index, index, this, settings, addr);
                                    _dimmSensorList.Add(sensor);
                                    ActivateSensor(sensor);
                                }
                                Thread.Sleep(10);
                                index++;
                            }
                            Ring0.ReleaseSmBusMutex();
                            return;
                        }
                    }
                }
            }
            catch { }
        }

        public override HardwareType HardwareType
        {
            get { return HardwareType.Memory; }
        }

        public override void Update()
        {
            Kernel32.MEMORYSTATUSEX status = new Kernel32.MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<Kernel32.MEMORYSTATUSEX>() };

            if (!Kernel32.GlobalMemoryStatusEx(ref status))
                return;


            _physicalMemoryUsed.Value = (float)(status.ullTotalPhys - status.ullAvailPhys) / (1024 * 1024 * 1024);
            _physicalMemoryAvailable.Value = (float)status.ullAvailPhys / (1024 * 1024 * 1024);
            _physicalMemoryLoad.Value = 100.0f - (100.0f * status.ullAvailPhys) / status.ullTotalPhys;

            _virtualMemoryUsed.Value = (float)(status.ullTotalPageFile - status.ullAvailPageFile) / (1024 * 1024 * 1024);
            _virtualMemoryAvailable.Value = (float)status.ullAvailPageFile / (1024 * 1024 * 1024);
            _virtualMemoryLoad.Value = 100.0f - (100.0f * status.ullAvailPageFile) / status.ullTotalPageFile;

            if (!Ring0.WaitSmBusMutex(10))
                return;

            for (int i = 0; i < _dimmSensorList.Count; i++)
            {
                _dimmSensorList[i].UpdateSensor();
                Thread.Sleep(10);
            }

            Ring0.ReleaseSmBusMutex();
        }
    }
}
