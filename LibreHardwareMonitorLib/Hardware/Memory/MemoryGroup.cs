// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Timers;
using RAMSPDToolkit.I2CSMBus;
using RAMSPDToolkit.SPD;
using RAMSPDToolkit.SPD.Enums;
using RAMSPDToolkit.SPD.Interfaces;
using RAMSPDToolkit.SPD.Interop.Shared;
using RAMSPDToolkit.Windows.Driver;

namespace LibreHardwareMonitor.Hardware.Memory;

internal class MemoryGroup : IGroup
{
    private static object _lock = new object();

    //Retry every 2.5 seconds
    private const double RetryTime = 2500;

    //Retry 12x
    private const int RetryCount = 12;

    private Timer _timer;
    private int _elapsedCounter = 0;

    private static RAMSPDToolkitDriver _ramSPDToolkitDriver;

    private List<Hardware> _hardware = new();

    public IReadOnlyList<IHardware> Hardware => _hardware;

    static MemoryGroup()
    {
        if (Ring0.IsOpen)
        {
            //Assign implementation of IDriver
            _ramSPDToolkitDriver = new RAMSPDToolkitDriver(Ring0.KernelDriver);
            DriverManager.Driver = _ramSPDToolkitDriver;

            SMBusManager.UseWMI = false;
        }
    }

    public MemoryGroup(ISettings settings)
    {
        _hardware.Add(new DimmVirtualMemory(settings));
        _hardware.Add(new DimmTotalMemory(settings));

        List<SPDAccessor> accessors = null;

        //No RAM detected
        if (!DetectThermalSensors(settings, out accessors))
        {
            //Retry a couple of times
            //SMBus might not be detected right after boot
            _timer = new Timer(RetryTime);

            _timer.Elapsed += (e, o) =>
            {
                if (_elapsedCounter++ >= RetryCount || DetectThermalSensors(settings, out accessors))
                {
                    _timer.Stop();
                    _timer = null;

                    if (accessors != null)
                    {
                        AddDIMMs(accessors, settings);
                    }
                }
            };

            _timer.Start();
        }
        else //RAM detected
        {
            AddDIMMs(accessors, settings);
        }
    }

    public string GetReport()
    {
        return null;
    }

    public void Close()
    {
        foreach (Hardware ram in _hardware)
            ram.Close();
    }

    private bool DetectThermalSensors(ISettings settings, out List<SPDAccessor> accessors)
    {
        lock (_lock)
        {
            var list = new List<SPDAccessor>();

            bool ramDetected = false;

            SMBusManager.DetectSMBuses();

            //Go through detected SMBuses
            foreach (var smbus in SMBusManager.RegisteredSMBuses)
            {
                //Go through possible RAM slots
                for (byte i = SPDConstants.SPD_BEGIN; i <= SPDConstants.SPD_END; ++i)
                {
                    //Detect type of RAM, if available
                    var detector = new SPDDetector(smbus, i);

                    //RAM available and detected
                    if (detector.Accessor != null)
                    {
                        //We are only interested in modules with thermal sensor
                        if (detector.Accessor is IThermalSensor ts
                         && ts.HasThermalSensor)
                        {
                            list.Add(detector.Accessor);
                        }

                        ramDetected = true;
                    }
                }
            }

            if (list.Count > 0)
            {
                accessors = list;
            }
            else
            {
                accessors = null;
            }

            return ramDetected;
        }
    }

    private void AddDIMMs(List<SPDAccessor> accessors, ISettings settings)
    {
        foreach (var ram in accessors)
        {
            //Default value
            string name = $"DIMM #{ram.Index}";

            //Check if we can switch to the correct page
            if (ram.ChangePage(PageData.ModulePartNumber))
            {
                name = $"{ram.GetModuleManufacturerString()} - {ram.ModulePartNumber()} (#{ram.Index})";
            }

            var memory = new DimmMemory(ram, name, new Identifier("ram"), settings);

            _hardware.Add(memory);
        }
    }
}
