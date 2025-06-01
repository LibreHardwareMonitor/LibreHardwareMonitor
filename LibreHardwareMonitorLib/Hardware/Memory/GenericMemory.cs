// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using LibreHardwareMonitor.Hardware.Memory.Sensors;
using RAMSPDToolkit.I2CSMBus;
using RAMSPDToolkit.SPD;
using RAMSPDToolkit.SPD.Enums;
using RAMSPDToolkit.SPD.Interfaces;
using RAMSPDToolkit.SPD.Interop.Shared;

namespace LibreHardwareMonitor.Hardware.Memory;

internal abstract class GenericMemory : Hardware
{
    readonly List<SPDThermalSensor> _SPDThermalSensors = new();

    public override HardwareType HardwareType => HardwareType.Memory;

    protected GenericMemory(string name, Identifier identifier, ISettings settings) : base(name, identifier, settings)
    {
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
                        _SPDThermalSensors.Add(sensor);

                        ActivateSensor(sensor);
                    }
                }
            }
        }
    }

    public override void Update()
    {
        foreach (var sensor in _SPDThermalSensors)
        {
            sensor.UpdateSensor();
        }
    }
}
