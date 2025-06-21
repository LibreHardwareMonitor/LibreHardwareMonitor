// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Timers;
using LibreHardwareMonitor.Hardware.Memory.Sensors;
using RAMSPDToolkit.I2CSMBus;
using RAMSPDToolkit.SPD;
using RAMSPDToolkit.SPD.Enums;
using RAMSPDToolkit.SPD.Interfaces;
using RAMSPDToolkit.SPD.Interop.Shared;

namespace LibreHardwareMonitor.Hardware.Memory;

internal abstract class GenericMemory : Hardware
{
    //Retry every 5 seconds
    private const double RetryTime = 5000;

    //Retry 12x (one minute)
    private const int RetryCount = 12;

    private static object _lock;

    private readonly List<SPDThermalSensor> _spdThermalSensors = new();

    private Timer _timer;
    private int _elapsedCounter = 0;

    public override HardwareType HardwareType => HardwareType.Memory;

    static GenericMemory()
    {
        _lock = new object();
    }

    protected GenericMemory(string name, Identifier identifier, ISettings settings)
        : base(name, identifier, settings)
    {
        //No RAM detected
        if (!DetectThermalSensors(settings))
        {
            //Retry a couple of times
            //SMBus might not be detected right after boot
            _timer = new Timer(RetryTime);

            _timer.Elapsed += (e, o) =>
            {
                if (_elapsedCounter++ >= RetryCount || DetectThermalSensors(settings))
                {
                    _timer.Stop();
                    _timer = null;
                }
            };

            _timer.Start();
        }
    }

    public override void Update()
    {
        lock (_lock)
        {
            foreach (var sensor in _spdThermalSensors)
            {
                sensor.UpdateSensor();
            }
        }
    }

    private bool DetectThermalSensors(ISettings settings)
    {
        lock (_lock)
        {
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
                        SPDThermalSensor sensor = null;
                        string str = string.Empty;

                        //Check if we can switch to the correct page and read some manufacturer data
                        if (detector.Accessor.ChangePage(PageData.ModulePartNumber))
                        {
                            var manufacturer = detector.Accessor.GetModuleManufacturerString();
                            var ramModel     = detector.Accessor.ModulePartNumber();

                            str = $" Manufacturer: {manufacturer} - Model: {ramModel}";
                        }

                        //Check which kind of RAM we have
                        switch (detector.SPDMemoryType)
                        {
                            case SPDMemoryType.SPD_DDR4_SDRAM:
                            case SPDMemoryType.SPD_DDR4E_SDRAM:
                            case SPDMemoryType.SPD_LPDDR4_SDRAM:
                            case SPDMemoryType.SPD_LPDDR4X_SDRAM:
                                sensor = new SPDThermalSensor($"DIMM #{i - SPDConstants.SPD_BEGIN}{str}",
                                                              i - SPDConstants.SPD_BEGIN,
                                                              SensorType.Temperature,
                                                              this,
                                                              settings,
                                                              detector.Accessor as IThermalSensor);
                                break;
                            case SPDMemoryType.SPD_DDR5_SDRAM:
                            case SPDMemoryType.SPD_LPDDR5_SDRAM:
                                //Check if we are on correct page or if write protection is not enabled
                                if (detector.Accessor.GetPageData().HasFlag(PageData.ThermalData) || !smbus.HasSPDWriteProtection)
                                {
                                    sensor = new SPDThermalSensor($"DIMM #{i - SPDConstants.SPD_BEGIN}{str}",
                                                                  i - SPDConstants.SPD_BEGIN,
                                                                  SensorType.Temperature,
                                                                  this,
                                                                  settings,
                                                                  detector.Accessor as IThermalSensor);
                                }
                                break;
                        }

                        //Add thermal sensor
                        if (sensor != null)
                        {
                            _spdThermalSensors.Add(sensor);

                            ActivateSensor(sensor);
                        }

                        ramDetected = true;
                    }
                }
            }

            return ramDetected;
        }
    }
}
