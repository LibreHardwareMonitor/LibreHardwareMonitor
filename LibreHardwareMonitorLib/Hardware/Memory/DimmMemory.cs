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
        AddSensor("Minimum Cycle Time (tCKAVGmin)"                       ,  1, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumCycleTime                        );
        AddSensor("Maximum Cycle Time (tCKAVGmax)"                       ,  2, false, SensorType.Timing, (float)accessor.SDRAMTimings.MaximumCycleTime                        );
        AddSensor("Minimum CAS Latency Time (tAA min)"                   ,  3, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumCASLatencyTime                   );
        AddSensor("Minimum RAS to CAS Delay Time (tRCD min)"             ,  4, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumRASToCASDelayTime                );
        AddSensor("Minimum Row Precharge Delay Time (tRP min)"           ,  5, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumRowPrechargeDelayTime            );
        AddSensor("Minimum Active to Precharge Delay Time (tRAS min)"    ,  6, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumActiveToPrechargeDelayTime       );
        AddSensor("Minimum Active to Active/Refresh Delay Time (tRC min)",  7, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumActiveToActiveRefreshDelayTime   );
        AddSensor("Minimum Refresh Recovery Delay Time (tRFC1 min)"      ,  8, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumRefreshRecoveryDelayTime1        );
        AddSensor("Minimum Refresh Recovery Delay Time (tRFC2 min)"      ,  9, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumRefreshRecoveryDelayTime2        );
        AddSensor("Minimum Refresh Recovery Delay Time (tRFC4 min)"      , 10, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumRefreshRecoveryDelayTime4        );
        AddSensor("Minimum Four Activate Window Time (tFAW min)"         , 11, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumFourActivateWindowTime           );
        AddSensor("Minimum Activate to Activate Delay Time (tRRD_S min)" , 12, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumActivateToActivateDelay_DiffGroup);
        AddSensor("Minimum Activate to Activate Delay Time (tRRD_L min)" , 13, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumActivateToActivateDelay_SameGroup);
        AddSensor("Minimum CAS to CAS Delay Time (tCCD_L min)"           , 14, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumCASToCASDelay_SameGroup          );
        AddSensor("Minimum Write Recovery Time (tWR min)"                , 15, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumWriteRecoveryTime                );
        AddSensor("Minimum Write to Read Time (tWTR smin)"               , 16, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumWriteToReadTime_DiffGroup        );
        AddSensor("Minimum Write to Read Time (tWTR lmin)"               , 17, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumWriteToReadTime_SameGroup        );

    }

    private void CreateSensorsDDR5(DDR5Accessor accessor, bool hasThermalSensor)
    {
        if (hasThermalSensor)
        {
            //Temperature Resolution (fixed value)
            AddSensor("Temperature Sensor Resolution", 0, false, SensorType.Temperature, accessor.TemperatureResolution);
        }

        //Timings
        AddSensor("Minimum Cycle Time (tCKAVGmin)"                        ,  1, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumCycleTime                                          );
        AddSensor("Maximum Cycle Time (tCKAVGmax)"                        ,  2, false, SensorType.Timing, (float)accessor.SDRAMTimings.MaximumCycleTime                                          );
        AddSensor("Minimum CAS Latency Time (tAA min)"                    ,  3, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumCASLatencyTime                                     );
        AddSensor("Minimum RAS to CAS Delay Time (tRCD min)"              ,  4, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumRASToCASDelayTime                                  );
        AddSensor("Minimum Row Precharge Delay Time (tRP min)"            ,  5, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumRowPrechargeDelayTime                              );
        AddSensor("Minimum Active to Precharge Delay Time (tRAS min)"     ,  6, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumActiveToPrechargeDelayTime                         );
        AddSensor("Minimum Active to Active/Refresh Delay Time (tRC min)" ,  7, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumActiveToActiveRefreshDelayTime                     );
        AddSensor("Minimum Write Recovery Time (tWR min)"                 ,  8, false, SensorType.Timing, (float)accessor.SDRAMTimings.MinimumWriteRecoveryTime                                  );
        AddSensor("Normal Refresh Recovery Time (tRFC1 min)"              ,  9, false, SensorType.Timing, (float)accessor.SDRAMTimings.NormalRefreshRecoveryTime                                 );
        AddSensor("Fine Granularity Refresh Recovery Time (tRFC2 min)"    , 10, false, SensorType.Timing, (float)accessor.SDRAMTimings.FineGranularityRefreshRecoveryTime                        );
        AddSensor("Same Bank Refresh Recovery Time (tRFCsb min)"          , 11, false, SensorType.Timing, (float)accessor.SDRAMTimings.SameBankRefreshRecoveryTime                               );
        AddSensor("Normal Refresh Recovery Time 3DS (tRFC1_dlr)"          , 12, false, SensorType.Timing, (float)accessor.SDRAMTimings.NormalRefreshRecoveryTime_3DSDifferentLogicalRank         );
        AddSensor("Fine Granularity Refresh Recovery Time 3DS (tRFC2_dlr)", 13, false, SensorType.Timing, (float)accessor.SDRAMTimings.FineGranularityRefreshRecoveryTime_3DSDifferentLogicalRank);
        AddSensor("Same Bank Refresh Recovery Time 3DS (tRFCsb_dlr)"      , 14, false, SensorType.Timing, (float)accessor.SDRAMTimings.SameBankRefreshRecoveryTime_3DSDifferentLogicalRank       );
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
