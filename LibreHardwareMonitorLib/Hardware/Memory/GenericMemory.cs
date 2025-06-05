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
    #region Constructor

    protected GenericMemory(string name, Identifier identifier, ISettings settings)
        : base(name, identifier, settings)
    {
        //No RAM detected
        if (!DetectThermalSensors(settings))
        {
            //Retry a couple of times
            //SMBus might not be detected right after boot
            _Timer = new Timer(RETRY_TIME);

            _Timer.Elapsed += (e, o) =>
            {
                if (_ElapsedCounter++ >= RETRY_COUNT || DetectThermalSensors(settings))
                {
                    _Timer.Stop();
                    _Timer = null;
                }
            };

            _Timer.Start();
        }
    }

    #endregion

    #region Fields

    //Retry every 5 seconds
    const double RETRY_TIME = 5000;

    //Retry 12x (one minute)
    const int RETRY_COUNT = 12;

    object _Lock = new object();

    readonly List<SPDThermalSensor> _SPDThermalSensors = new();

    Timer _Timer;
    int _ElapsedCounter = 0;

    #endregion

    #region Properties

    public override HardwareType HardwareType => HardwareType.Memory;

    #endregion

    #region Public

    public override void Update()
    {
        lock (_Lock)
        {
            foreach (var sensor in _SPDThermalSensors)
            {
                sensor.UpdateSensor();
            }
        }
    }

    #endregion

    #region Private

    private bool DetectThermalSensors(ISettings settings)
    {
        lock (_Lock)
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
                            _SPDThermalSensors.Add(sensor);

                            ActivateSensor(sensor);
                        }

                        ramDetected = true;
                    }
                }
            }

            return ramDetected;
        }
    }

    #endregion
}
