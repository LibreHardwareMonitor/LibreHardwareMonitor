// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using LibreHardwareMonitor.Hardware.Memory.Sensors;
using RAMSPDToolkit.SPD;
using RAMSPDToolkit.SPD.Interfaces;
using RAMSPDToolkit.SPD.Interop.Shared;

namespace LibreHardwareMonitor.Hardware.Memory;

internal sealed class DimmMemory : Hardware
{
    private readonly SpdThermalSensor _thermalSensor;

    public DimmMemory(SPDAccessor accessor, string name, Identifier identifier, ISettings settings)
        : base(name, identifier, settings)
    {
        //Only add thermal sensor if present
        if (accessor is IThermalSensor ts && ts.HasThermalSensor)
        {
            //Check which kind of RAM we have
            switch (accessor.MemoryType())
            {
                case SPDMemoryType.SPD_DDR4_SDRAM:
                case SPDMemoryType.SPD_DDR4E_SDRAM:
                case SPDMemoryType.SPD_LPDDR4_SDRAM:
                case SPDMemoryType.SPD_LPDDR4X_SDRAM:
                case SPDMemoryType.SPD_DDR5_SDRAM:
                case SPDMemoryType.SPD_LPDDR5_SDRAM:
                    _thermalSensor = new SpdThermalSensor($"DIMM #{accessor.Index}",
                                                          accessor.Index,
                                                          SensorType.Temperature,
                                                          this,
                                                          settings,
                                                          accessor as IThermalSensor);
                    break;
            }
        }

        bool hasThermalSensor = _thermalSensor != null;

        //Add thermal sensor
        if (hasThermalSensor)
            ActivateSensor(_thermalSensor);

        //Add other sensors
        CreateSensors(accessor, hasThermalSensor);
    }

    public override HardwareType HardwareType => HardwareType.Memory;

    public override void Update()
    {
        _thermalSensor?.UpdateSensor();
    }

    private void CreateSensors(SPDAccessor accessor, bool hasThermalSensor)
    {
        if (accessor is DDR4Accessor ddr4)
        {
            CreateSensorsDDR4(ddr4, hasThermalSensor);
        }
        else if (accessor is DDR5Accessor ddr5)
        {
            CreateSensorsDDR5(ddr5, hasThermalSensor);
        }
    }

    private void CreateSensorsDDR4(DDR4Accessor accessor, bool hasThermalSensor)
    {
        if (hasThermalSensor)
        {
            //Temperature Resolution (fixed value)
            AddSensor("Temperature Sensor Resolution", 0, false, SensorType.Temperature, accessor.TemperatureResolution);
        }

        //Timings
        AddSensor("tCKAVGmin (Minimum Cycle Time)", 1, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumCycleTime);
        AddSensor("tCKAVGmax (Maximum Cycle Time)", 2, false, SensorType.Timing, (float)accessor.SDRAMTimings.MaximumCycleTime);
        AddSensor("tAA (CAS Latency Time)", 3, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumCASLatencyTime);
        AddSensor("tRCD (RAS to CAS Delay Time)", 4, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumRASToCASDelayTime);
        AddSensor("tRP (Row Precharge Delay Time)", 5, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumRowPrechargeDelayTime);
        AddSensor("tRAS (Active to Precharge Delay Time)", 6, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumActiveToPrechargeDelayTime);
        AddSensor("tRC (Active to Active/Refresh Delay Time)", 7, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumActiveToActiveRefreshDelayTime);
        AddSensor("tRFC1 (Refresh Recovery Delay Time)", 8, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumRefreshRecoveryDelayTime1);
        AddSensor("tRFC2 (Refresh Recovery Delay Time)", 9, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumRefreshRecoveryDelayTime2);
        AddSensor("tRFC4 (Refresh Recovery Delay Time)", 10, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumRefreshRecoveryDelayTime4);
        AddSensor("tFAW (Four Activate Window Time)", 11, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumFourActivateWindowTime);
        AddSensor("tRRD_S (Activate to Activate Delay Time)", 12, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumActivateToActivateDelay_DiffGroup);
        AddSensor("tRRD_L (Activate to Activate Delay Time)", 13, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumActivateToActivateDelay_SameGroup);
        AddSensor("tCCD_L (CAS to CAS Delay Time)", 14, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumCASToCASDelay_SameGroup);
        AddSensor("tWR (Write Recovery Time)", 15, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumWriteRecoveryTime);
        AddSensor("tWTR_S (Write to Read Time)", 16, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumWriteToReadTime_DiffGroup);
        AddSensor("tWTR_L (Write to Read Time)", 17, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumWriteToReadTime_SameGroup);

        //Data
        AddSensor("Capacity", 18, false, SensorType.Data, accessor.GetCapacity());
    }

    private void CreateSensorsDDR5(DDR5Accessor accessor, bool hasThermalSensor)
    {
        if (hasThermalSensor)
        {
            //Temperature Resolution (fixed value)
            AddSensor("Temperature Sensor Resolution", 0, false, SensorType.Temperature, accessor.TemperatureResolution);
        }

        //Timings
        AddSensor("tCKAVGmin (Minimum Cycle Time)", 1, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumCycleTime);
        AddSensor("tCKAVGmax (Maximum Cycle Time)", 2, false, SensorType.Timing, (float)accessor.SDRAMTimings.MaximumCycleTime);
        AddSensor("tAA (CAS Latency Time)", 3, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumCASLatencyTime);
        AddSensor("tRCD (RAS to CAS Delay Time)", 4, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumRASToCASDelayTime);
        AddSensor("tRP (Row Precharge Delay Time)", 5, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumRowPrechargeDelayTime);
        AddSensor("tRAS (Active to Precharge Delay Time)", 6, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumActiveToPrechargeDelayTime);
        AddSensor("tRC (Active to Active/Refresh Delay Time)", 7, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumActiveToActiveRefreshDelayTime);
        AddSensor("tWR (Write Recovery Time)", 8, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumWriteRecoveryTime);
        AddSensor("tRFC1 (Normal Refresh Recovery Time)", 9, false, SensorType.Timing, (float)accessor.SDRAMTimings.NormalRefreshRecoveryTime);
        AddSensor("tRFC2 (Fine Granularity Refresh Recovery Time)", 10, false, SensorType.Timing, (float)accessor.SDRAMTimings.FineGranularityRefreshRecoveryTime);
        AddSensor("tRFCsb (Same Bank Refresh Recovery Time)", 11, false, SensorType.Timing, (float)accessor.SDRAMTimings.SameBankRefreshRecoveryTime);
        AddSensor("tRFC1_dlr (Normal Refresh Recovery Time 3DS)", 12, false, SensorType.Timing, (float)accessor.SDRAMTimings.NormalRefreshRecoveryTime_3DSDifferentLogicalRank);
        AddSensor("tRFC2_dlr (Fine Granularity Refresh Recovery Time 3DS)", 13, false, SensorType.Timing, (float)accessor.SDRAMTimings.FineGranularityRefreshRecoveryTime_3DSDifferentLogicalRank);
        AddSensor("tRFCsb_dlr (Same Bank Refresh Recovery Time 3DS)", 14, false, SensorType.Timing, (float)accessor.SDRAMTimings.SameBankRefreshRecoveryTime_3DSDifferentLogicalRank);

        //Data
        AddSensor("Capacity", 15, false, SensorType.Data, accessor.GetCapacity());
    }

    private void AddSensor(string name, int index, bool defaultHidden, SensorType sensorType, float value)
    {
        var sensor = new Sensor(name, index, defaultHidden, sensorType, this, null, _settings)
        {
            Value = value,
        };

        ActivateSensor(sensor);
    }
}
