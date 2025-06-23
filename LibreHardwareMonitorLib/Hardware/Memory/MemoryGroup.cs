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
    //Retry 12x
    private const int RetryCount = 12;

    //Retry every 2.5 seconds
    private const double RetryTime = 2500;
    private static readonly object _lock = new();

    private readonly List<Hardware> _hardware = [];
    private int _elapsedCounter;

    private Timer _timer;

    static MemoryGroup()
    {
        if (Ring0.IsOpen)
        {
            //Assign implementation of IDriver
            DriverManager.Driver = new RAMSPDToolkitDriver(Ring0.KernelDriver);
            SMBusManager.UseWMI = false;
        }
    }

    public MemoryGroup(ISettings settings)
    {
        _hardware.Add(new VirtualMemory(settings));
        _hardware.Add(new TotalMemory(settings));

        //No RAM detected
        if (!DetectThermalSensors(out List<SPDAccessor> accessors))
        {
            //Retry a couple of times
            //SMBus might not be detected right after boot
            _timer = new Timer(RetryTime);

            _timer.Elapsed += (_, _) =>
            {
                if (_elapsedCounter++ >= RetryCount || DetectThermalSensors(out accessors))
                {
                    _timer.Stop();
                    _timer = null;

                    if (accessors != null)
                        AddDimms(accessors, settings);
                }
            };

            _timer.Start();
        }
        else
        {
            AddDimms(accessors, settings);
        }
    }

    public IReadOnlyList<IHardware> Hardware => _hardware;

    public string GetReport()
    {
        return null;
    }

    public void Close()
    {
        foreach (Hardware ram in _hardware)
            ram.Close();
    }

    private static bool DetectThermalSensors(out List<SPDAccessor> accessors)
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
                        if (detector.Accessor is IThermalSensor { HasThermalSensor: true })
                            list.Add(detector.Accessor);

                        ramDetected = true;
                    }
                }
            }

            accessors = list.Count > 0 ? list : [];
            return ramDetected;
        }
    }

    private void AddDimms(List<SPDAccessor> accessors, ISettings settings)
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
