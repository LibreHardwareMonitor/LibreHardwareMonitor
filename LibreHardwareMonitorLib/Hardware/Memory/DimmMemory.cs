// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using LibreHardwareMonitor.Hardware.Memory.Sensors;
using RAMSPDToolkit.SPD;
using RAMSPDToolkit.SPD.Enums;
using RAMSPDToolkit.SPD.Interfaces;
using RAMSPDToolkit.SPD.Interop.Shared;

namespace LibreHardwareMonitor.Hardware.Memory;

internal sealed class DimmMemory : Hardware
{
    private readonly SpdThermalSensor _thermalSensor;

    public DimmMemory(SPDAccessor accessor, string name, Identifier identifier, ISettings settings)
        : base(name, identifier, settings)
    {
        //Check which kind of RAM we have
        switch (accessor.MemoryType())
        {
            case SPDMemoryType.SPD_DDR4_SDRAM:
            case SPDMemoryType.SPD_DDR4E_SDRAM:
            case SPDMemoryType.SPD_LPDDR4_SDRAM:
            case SPDMemoryType.SPD_LPDDR4X_SDRAM:
                _thermalSensor = new SpdThermalSensor($"DIMM #{accessor.Index}",
                                                      accessor.Index,
                                                      SensorType.Temperature,
                                                      this,
                                                      settings,
                                                      accessor as IThermalSensor);

                break;
            case SPDMemoryType.SPD_DDR5_SDRAM:
            case SPDMemoryType.SPD_LPDDR5_SDRAM:
                //Check if we are on correct page or if write protection is not enabled
                if (accessor.PageData.HasFlag(PageData.ThermalData) || !accessor.HasSPDWriteProtection)
                {
                    _thermalSensor = new SpdThermalSensor($"DIMM #{accessor.Index}",
                                                          accessor.Index,
                                                          SensorType.Temperature,
                                                          this,
                                                          settings,
                                                          accessor as IThermalSensor);
                }

                break;
        }

        //Add thermal sensor
        if (_thermalSensor != null)
            ActivateSensor(_thermalSensor);
    }

    public override HardwareType HardwareType => HardwareType.Memory;

    public override void Update()
    {
        _thermalSensor?.UpdateSensor();
    }
}
