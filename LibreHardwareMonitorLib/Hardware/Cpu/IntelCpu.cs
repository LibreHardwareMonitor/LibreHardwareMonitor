// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace LibreHardwareMonitor.Hardware.Cpu;

internal sealed class IntelCpu : GenericCpu
{
    private readonly Sensor _busClock;
    private readonly Sensor _coreAvg;
    private readonly Sensor[] _coreClocks;
    private readonly Sensor _coreMax;
    private readonly Sensor[] _coreTemperatures;
    private readonly Sensor[] _coreVIDs;
    private readonly Sensor _coreVoltage;
    private readonly Sensor[] _distToTjMaxTemperatures;

    private readonly uint[] _energyStatusMsrs = { MSR_PKG_ENERGY_STATUS, MSR_PP0_ENERGY_STATUS, MSR_PP1_ENERGY_STATUS, MSR_DRAM_ENERGY_STATUS };
    private readonly uint[] _lastEnergyConsumed;
    private readonly DateTime[] _lastEnergyTime;

    private readonly MicroArchitecture _microArchitecture;
    private readonly Sensor _packageTemperature;
    private readonly Sensor[] _powerSensors;
    private readonly double _timeStampCounterMultiplier;

    public IntelCpu(int processorIndex, CpuId[][] cpuId, ISettings settings) : base(processorIndex, cpuId, settings)
    {
        uint eax;

        // set tjMax
        float[] tjMax;
        switch (_family)
        {
            case 0x06:
                {
                    switch (_model)
                    {
                        case 0x0F: // Intel Core 2 (65nm)
                            _microArchitecture = MicroArchitecture.Core;
                            tjMax = _stepping switch
                            {
                                // B2
                                0x06 => _coreCount switch
                                {
                                    2 => Floats(80 + 10),
                                    4 => Floats(90 + 10),
                                    _ => Floats(85 + 10)
                                },
                                // G0
                                0x0B => Floats(90 + 10),
                                // M0
                                0x0D => Floats(85 + 10),
                                _ => Floats(85 + 10)
                            };
                            break;

                        case 0x17: // Intel Core 2 (45nm)
                            _microArchitecture = MicroArchitecture.Core;
                            tjMax = Floats(100);
                            break;

                        case 0x1C: // Intel Atom (45nm)
                            _microArchitecture = MicroArchitecture.Atom;
                            tjMax = _stepping switch
                            {
                                // C0
                                0x02 => Floats(90),
                                // A0, B0
                                0x0A => Floats(100),
                                _ => Floats(90)
                            };
                            break;

                        case 0x1A: // Intel Core i7 LGA1366 (45nm)
                        case 0x1E: // Intel Core i5, i7 LGA1156 (45nm)
                        case 0x1F: // Intel Core i5, i7
                        case 0x25: // Intel Core i3, i5, i7 LGA1156 (32nm)
                        case 0x2C: // Intel Core i7 LGA1366 (32nm) 6 Core
                        case 0x2E: // Intel Xeon Processor 7500 series (45nm)
                        case 0x2F: // Intel Xeon Processor (32nm)
                            _microArchitecture = MicroArchitecture.Nehalem;
                            tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x2A: // Intel Core i5, i7 2xxx LGA1155 (32nm)
                        case 0x2D: // Next Generation Intel Xeon, i7 3xxx LGA2011 (32nm)
                            _microArchitecture = MicroArchitecture.SandyBridge;
                            tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x3A: // Intel Core i5, i7 3xxx LGA1155 (22nm)
                        case 0x3E: // Intel Core i7 4xxx LGA2011 (22nm)
                            _microArchitecture = MicroArchitecture.IvyBridge;
                            tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x3C: // Intel Core i5, i7 4xxx LGA1150 (22nm)
                        case 0x3F: // Intel Xeon E5-2600/1600 v3, Core i7-59xx
                        // LGA2011-v3, Haswell-E (22nm)
                        case 0x45: // Intel Core i5, i7 4xxxU (22nm)
                        case 0x46:
                            _microArchitecture = MicroArchitecture.Haswell;
                            tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x3D: // Intel Core M-5xxx (14nm)
                        case 0x47: // Intel i5, i7 5xxx, Xeon E3-1200 v4 (14nm)
                        case 0x4F: // Intel Xeon E5-26xx v4
                        case 0x56: // Intel Xeon D-15xx
                            _microArchitecture = MicroArchitecture.Broadwell;
                            tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x36: // Intel Atom S1xxx, D2xxx, N2xxx (32nm)
                            _microArchitecture = MicroArchitecture.Atom;
                            tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x37: // Intel Atom E3xxx, Z3xxx (22nm)
                        case 0x4A:
                        case 0x4D: // Intel Atom C2xxx (22nm)
                        case 0x5A:
                        case 0x5D:
                            _microArchitecture = MicroArchitecture.Silvermont;
                            tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x4E:
                        case 0x5E: // Intel Core i5, i7 6xxxx LGA1151 (14nm)
                        case 0x55: // Intel Core X i7, i9 7xxx LGA2066 (14nm)
                            _microArchitecture = MicroArchitecture.Skylake;
                            tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x4C: // Intel Airmont (Cherry Trail, Braswell)
                            _microArchitecture = MicroArchitecture.Airmont;
                            tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x8E: // Intel Core i5, i7 7xxxx (14nm) (Kaby Lake) and 8xxxx (14nm++) (Coffee Lake)
                        case 0x9E:
                            _microArchitecture = MicroArchitecture.KabyLake;
                            tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x5C: // Goldmont (Apollo Lake)
                        case 0x5F: // (Denverton)
                            _microArchitecture = MicroArchitecture.Goldmont;
                            tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x7A: // Goldmont plus (Gemini Lake)
                            _microArchitecture = MicroArchitecture.GoldmontPlus;
                            tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x66: // Intel Core i3 8xxx (10nm) (Cannon Lake)
                            _microArchitecture = MicroArchitecture.CannonLake;
                            tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x7D: // Intel Core i3, i5, i7 10xxx (10nm) (Ice Lake)
                        case 0x7E:
                        case 0x6A: // Ice Lake server
                        case 0x6C:
                            _microArchitecture = MicroArchitecture.IceLake;
                            tjMax = GetTjMaxFromMsr();
                            break;

                        case 0xA5:
                        case 0xA6: // Intel Core i3, i5, i7 10xxxU (14nm)
                            _microArchitecture = MicroArchitecture.CometLake;
                            tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x86: // Tremont (10nm) (Elkhart Lake, Skyhawk Lake)
                            _microArchitecture = MicroArchitecture.Tremont;
                            tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x8C: // Tiger Lake (10nm)
                        case 0x8D:
                            _microArchitecture = MicroArchitecture.TigerLake;
                            tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x97: // Alder Lake (7/10nm)
                        case 0x9A: // Alder Lake-L (7/10nm)
                        case 0xBE: // Alder Lake-N (7/10nm)
                            _microArchitecture = MicroArchitecture.AlderLake;
                            tjMax = GetTjMaxFromMsr();
                            break;

                        case 0xB7: // Raptor Lake (7nm)
                        case 0xBA: // Raptor Lake-P (7nm)
                        case 0xBF: // Raptor Lake-N (7nm)
                            _microArchitecture = MicroArchitecture.RaptorLake;
                            tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x9C: // Jasper Lake (10nm)
                            _microArchitecture = MicroArchitecture.JasperLake;
                            tjMax = GetTjMaxFromMsr();
                            break;

                        case 0xA7: // Intel Core i5, i6, i7 11xxx (14nm) (Rocket Lake)
                            _microArchitecture = MicroArchitecture.RocketLake;
                            tjMax = GetTjMaxFromMsr();
                            break;

                        default:
                            _microArchitecture = MicroArchitecture.Unknown;
                            tjMax = Floats(100);
                            break;
                    }
                }

                break;
            case 0x0F:
                switch (_model)
                {
                    case 0x00: // Pentium 4 (180nm)
                    case 0x01: // Pentium 4 (130nm)
                    case 0x02: // Pentium 4 (130nm)
                    case 0x03: // Pentium 4, Celeron D (90nm)
                    case 0x04: // Pentium 4, Pentium D, Celeron D (90nm)
                    case 0x06: // Pentium 4, Pentium D, Celeron D (65nm)
                        _microArchitecture = MicroArchitecture.NetBurst;
                        tjMax = Floats(100);
                        break;

                    default:
                        _microArchitecture = MicroArchitecture.Unknown;
                        tjMax = Floats(100);
                        break;
                }

                break;
            default:
                _microArchitecture = MicroArchitecture.Unknown;
                tjMax = Floats(100);
                break;
        }

        // set timeStampCounterMultiplier
        switch (_microArchitecture)
        {
            case MicroArchitecture.Atom:
            case MicroArchitecture.Core:
            case MicroArchitecture.NetBurst:
                if (Ring0.ReadMsr(IA32_PERF_STATUS, out uint _, out uint edx))
                    _timeStampCounterMultiplier = ((edx >> 8) & 0x1f) + (0.5 * ((edx >> 14) & 1));

                break;
            case MicroArchitecture.Airmont:
            case MicroArchitecture.AlderLake:
            case MicroArchitecture.Broadwell:
            case MicroArchitecture.CannonLake:
            case MicroArchitecture.CometLake:
            case MicroArchitecture.Goldmont:
            case MicroArchitecture.GoldmontPlus:
            case MicroArchitecture.Haswell:
            case MicroArchitecture.IceLake:
            case MicroArchitecture.IvyBridge:
            case MicroArchitecture.JasperLake:
            case MicroArchitecture.KabyLake:
            case MicroArchitecture.Nehalem:
            case MicroArchitecture.RaptorLake:
            case MicroArchitecture.RocketLake:
            case MicroArchitecture.SandyBridge:
            case MicroArchitecture.Silvermont:
            case MicroArchitecture.Skylake:
            case MicroArchitecture.TigerLake:
            case MicroArchitecture.Tremont:
                if (Ring0.ReadMsr(MSR_PLATFORM_INFO, out eax, out uint _))
                    _timeStampCounterMultiplier = (eax >> 8) & 0xff;

                break;
            default:
                _timeStampCounterMultiplier = 0;
                break;
        }

        int coreSensorId = 0;

        // check if processor supports a digital thermal sensor at core level
        if (cpuId[0][0].Data.GetLength(0) > 6 && (cpuId[0][0].Data[6, 0] & 1) != 0 && _microArchitecture != MicroArchitecture.Unknown)
        {
            _coreTemperatures = new Sensor[_coreCount];
            for (int i = 0; i < _coreTemperatures.Length; i++)
            {
                _coreTemperatures[i] = new Sensor(CoreString(i),
                                                  coreSensorId,
                                                  SensorType.Temperature,
                                                  this,
                                                  new[]
                                                  {
                                                      new ParameterDescription("TjMax [°C]", "TjMax temperature of the core sensor.\n" + "Temperature = TjMax - TSlope * Value.", tjMax[i]),
                                                      new ParameterDescription("TSlope [°C]", "Temperature slope of the digital thermal sensor.\n" + "Temperature = TjMax - TSlope * Value.", 1)
                                                  },
                                                  settings);

                ActivateSensor(_coreTemperatures[i]);
                coreSensorId++;
            }
        }
        else
            _coreTemperatures = Array.Empty<Sensor>();

        // check if processor supports a digital thermal sensor at package level
        if (cpuId[0][0].Data.GetLength(0) > 6 && (cpuId[0][0].Data[6, 0] & 0x40) != 0 && _microArchitecture != MicroArchitecture.Unknown)
        {
            _packageTemperature = new Sensor("CPU Package",
                                             coreSensorId,
                                             SensorType.Temperature,
                                             this,
                                             new[]
                                             {
                                                 new ParameterDescription("TjMax [°C]", "TjMax temperature of the package sensor.\n" + "Temperature = TjMax - TSlope * Value.", tjMax[0]),
                                                 new ParameterDescription("TSlope [°C]", "Temperature slope of the digital thermal sensor.\n" + "Temperature = TjMax - TSlope * Value.", 1)
                                             },
                                             settings);

            ActivateSensor(_packageTemperature);
            coreSensorId++;
        }

        // dist to tjmax sensor
        if (cpuId[0][0].Data.GetLength(0) > 6 && (cpuId[0][0].Data[6, 0] & 1) != 0 && _microArchitecture != MicroArchitecture.Unknown)
        {
            _distToTjMaxTemperatures = new Sensor[_coreCount];
            for (int i = 0; i < _distToTjMaxTemperatures.Length; i++)
            {
                _distToTjMaxTemperatures[i] = new Sensor(CoreString(i) + " Distance to TjMax", coreSensorId, SensorType.Temperature, this, settings);
                ActivateSensor(_distToTjMaxTemperatures[i]);
                coreSensorId++;
            }
        }
        else
            _distToTjMaxTemperatures = Array.Empty<Sensor>();

        //core temp avg and max value
        //is only available when the cpu has more than 1 core
        if (cpuId[0][0].Data.GetLength(0) > 6 && (cpuId[0][0].Data[6, 0] & 0x40) != 0 && _microArchitecture != MicroArchitecture.Unknown && _coreCount > 1)
        {
            _coreMax = new Sensor("Core Max", coreSensorId, SensorType.Temperature, this, settings);
            ActivateSensor(_coreMax);
            coreSensorId++;

            _coreAvg = new Sensor("Core Average", coreSensorId, SensorType.Temperature, this, settings);
            ActivateSensor(_coreAvg);
        }
        else
        {
            _coreMax = null;
            _coreAvg = null;
        }

        _busClock = new Sensor("Bus Speed", 0, SensorType.Clock, this, settings);
        _coreClocks = new Sensor[_coreCount];
        for (int i = 0; i < _coreClocks.Length; i++)
        {
            _coreClocks[i] = new Sensor(CoreString(i), i + 1, SensorType.Clock, this, settings);
            if (HasTimeStampCounter && _microArchitecture != MicroArchitecture.Unknown)
                ActivateSensor(_coreClocks[i]);
        }

        if (_microArchitecture is MicroArchitecture.Airmont or
            MicroArchitecture.AlderLake or
            MicroArchitecture.Broadwell or
            MicroArchitecture.CannonLake or
            MicroArchitecture.CometLake or
            MicroArchitecture.Goldmont or
            MicroArchitecture.GoldmontPlus or
            MicroArchitecture.Haswell or
            MicroArchitecture.IceLake or
            MicroArchitecture.IvyBridge or
            MicroArchitecture.JasperLake or
            MicroArchitecture.KabyLake or
            MicroArchitecture.RaptorLake or
            MicroArchitecture.RocketLake or
            MicroArchitecture.SandyBridge or
            MicroArchitecture.Silvermont or
            MicroArchitecture.Skylake or
            MicroArchitecture.TigerLake or
            MicroArchitecture.Tremont)
        {
            _powerSensors = new Sensor[_energyStatusMsrs.Length];
            _lastEnergyTime = new DateTime[_energyStatusMsrs.Length];
            _lastEnergyConsumed = new uint[_energyStatusMsrs.Length];

            if (Ring0.ReadMsr(MSR_RAPL_POWER_UNIT, out eax, out uint _))
            {
                EnergyUnitsMultiplier = _microArchitecture switch
                {
                    MicroArchitecture.Silvermont or MicroArchitecture.Airmont => 1.0e-6f * (1 << (int)((eax >> 8) & 0x1F)),
                    _ => 1.0f / (1 << (int)((eax >> 8) & 0x1F))
                };
            }

            if (EnergyUnitsMultiplier != 0)
            {
                string[] powerSensorLabels = { "CPU Package", "CPU Cores", "CPU Graphics", "CPU Memory" };

                for (int i = 0; i < _energyStatusMsrs.Length; i++)
                {
                    if (!Ring0.ReadMsr(_energyStatusMsrs[i], out eax, out uint _))
                        continue;

                    // Don't show the "GPU Graphics" sensor on windows, it will show up under the GPU instead.
                    if (i == 2 && !Software.OperatingSystem.IsUnix)
                        continue;

                    _lastEnergyTime[i] = DateTime.UtcNow;
                    _lastEnergyConsumed[i] = eax;
                    _powerSensors[i] = new Sensor(powerSensorLabels[i],
                                                  i,
                                                  SensorType.Power,
                                                  this,
                                                  settings);

                    ActivateSensor(_powerSensors[i]);
                }
            }
        }

        if (Ring0.ReadMsr(IA32_PERF_STATUS, out eax, out uint _) && ((eax >> 32) & 0xFFFF) > 0)
        {
            _coreVoltage = new Sensor("CPU Core", 0, SensorType.Voltage, this, settings);
            ActivateSensor(_coreVoltage);
        }

        _coreVIDs = new Sensor[_coreCount];
        for (int i = 0; i < _coreVIDs.Length; i++)
        {
            _coreVIDs[i] = new Sensor(CoreString(i), i + 1, SensorType.Voltage, this, settings);
            ActivateSensor(_coreVIDs[i]);
        }

        Update();
    }

    public float EnergyUnitsMultiplier { get; }

    private float[] Floats(float f)
    {
        float[] result = new float[_coreCount];
        for (int i = 0; i < _coreCount; i++)
            result[i] = f;

        return result;
    }

    private float[] GetTjMaxFromMsr()
    {
        float[] result = new float[_coreCount];
        for (int i = 0; i < _coreCount; i++)
        {
            if (Ring0.ReadMsr(IA32_TEMPERATURE_TARGET, out uint eax, out uint _, _cpuId[i][0].Affinity))
                result[i] = (eax >> 16) & 0xFF;
            else
                result[i] = 100;
        }

        return result;
    }

    protected override uint[] GetMsrs()
    {
        return new[]
        {
            MSR_PLATFORM_INFO,
            IA32_PERF_STATUS,
            IA32_THERM_STATUS_MSR,
            IA32_TEMPERATURE_TARGET,
            IA32_PACKAGE_THERM_STATUS,
            MSR_RAPL_POWER_UNIT,
            MSR_PKG_ENERGY_STATUS,
            MSR_DRAM_ENERGY_STATUS,
            MSR_PP0_ENERGY_STATUS,
            MSR_PP1_ENERGY_STATUS
        };
    }

    public override string GetReport()
    {
        StringBuilder r = new();
        r.Append(base.GetReport());
        r.Append("MicroArchitecture: ");
        r.AppendLine(_microArchitecture.ToString());
        r.Append("Time Stamp Counter Multiplier: ");
        r.AppendLine(_timeStampCounterMultiplier.ToString(CultureInfo.InvariantCulture));
        r.AppendLine();
        return r.ToString();
    }

    public override void Update()
    {
        base.Update();

        float coreMax = float.MinValue;
        float coreAvg = 0;
        uint eax;

        for (int i = 0; i < _coreTemperatures.Length; i++)
        {
            // if reading is valid
            if (Ring0.ReadMsr(IA32_THERM_STATUS_MSR, out eax, out _, _cpuId[i][0].Affinity) && (eax & 0x80000000) != 0)
            {
                // get the dist from tjMax from bits 22:16
                float deltaT = (eax & 0x007F0000) >> 16;
                float tjMax = _coreTemperatures[i].Parameters[0].Value;
                float tSlope = _coreTemperatures[i].Parameters[1].Value;
                _coreTemperatures[i].Value = tjMax - (tSlope * deltaT);

                coreAvg += (float)_coreTemperatures[i].Value;
                if (coreMax < _coreTemperatures[i].Value)
                    coreMax = (float)_coreTemperatures[i].Value;

                _distToTjMaxTemperatures[i].Value = deltaT;
            }
            else
            {
                _coreTemperatures[i].Value = null;
                _distToTjMaxTemperatures[i].Value = null;
            }
        }

        //calculate average cpu temperature over all cores
        if (_coreMax != null && coreMax != float.MinValue)
        {
            _coreMax.Value = coreMax;
            coreAvg /= _coreTemperatures.Length;
            _coreAvg.Value = coreAvg;
        }

        if (_packageTemperature != null)
        {
            // if reading is valid
            if (Ring0.ReadMsr(IA32_PACKAGE_THERM_STATUS, out eax, out _, _cpuId[0][0].Affinity) && (eax & 0x80000000) != 0)
            {
                // get the dist from tjMax from bits 22:16
                float deltaT = (eax & 0x007F0000) >> 16;
                float tjMax = _packageTemperature.Parameters[0].Value;
                float tSlope = _packageTemperature.Parameters[1].Value;
                _packageTemperature.Value = tjMax - (tSlope * deltaT);
            }
            else
            {
                _packageTemperature.Value = null;
            }
        }

        if (HasTimeStampCounter && _timeStampCounterMultiplier > 0)
        {
            double newBusClock = 0;
            for (int i = 0; i < _coreClocks.Length; i++)
            {
                System.Threading.Thread.Sleep(1);
                if (Ring0.ReadMsr(IA32_PERF_STATUS, out eax, out _, _cpuId[i][0].Affinity))
                {
                    newBusClock = TimeStampCounterFrequency / _timeStampCounterMultiplier;
                    switch (_microArchitecture)
                    {
                        case MicroArchitecture.Nehalem:
                            _coreClocks[i].Value = (float)((eax & 0xff) * newBusClock);
                            break;
                        case MicroArchitecture.Airmont:
                        case MicroArchitecture.AlderLake:
                        case MicroArchitecture.Broadwell:
                        case MicroArchitecture.CannonLake:
                        case MicroArchitecture.CometLake:
                        case MicroArchitecture.Goldmont:
                        case MicroArchitecture.GoldmontPlus:
                        case MicroArchitecture.Haswell:
                        case MicroArchitecture.IceLake:
                        case MicroArchitecture.IvyBridge:
                        case MicroArchitecture.JasperLake:
                        case MicroArchitecture.KabyLake:
                        case MicroArchitecture.RaptorLake:
                        case MicroArchitecture.RocketLake:
                        case MicroArchitecture.SandyBridge:
                        case MicroArchitecture.Silvermont:
                        case MicroArchitecture.Skylake:
                        case MicroArchitecture.TigerLake:
                        case MicroArchitecture.Tremont:
                            _coreClocks[i].Value = (float)(((eax >> 8) & 0xff) * newBusClock);
                            break;
                        default:
                            _coreClocks[i].Value = (float)((((eax >> 8) & 0x1f) + (0.5 * ((eax >> 14) & 1))) * newBusClock);
                            break;
                    }
                }
                else
                {
                    // if IA32_PERF_STATUS is not available, assume TSC frequency
                    _coreClocks[i].Value = (float)TimeStampCounterFrequency;
                }
            }

            if (newBusClock > 0)
            {
                _busClock.Value = (float)newBusClock;
                ActivateSensor(_busClock);
            }
        }

        if (_powerSensors != null)
        {
            foreach (Sensor sensor in _powerSensors)
            {
                if (sensor == null)
                    continue;

                if (!Ring0.ReadMsr(_energyStatusMsrs[sensor.Index], out eax, out _))
                    continue;

                DateTime time = DateTime.UtcNow;
                uint energyConsumed = eax;
                float deltaTime = (float)(time - _lastEnergyTime[sensor.Index]).TotalSeconds;
                if (deltaTime < 0.01)
                    continue;

                sensor.Value = EnergyUnitsMultiplier * unchecked(energyConsumed - _lastEnergyConsumed[sensor.Index]) / deltaTime;
                _lastEnergyTime[sensor.Index] = time;
                _lastEnergyConsumed[sensor.Index] = energyConsumed;
            }
        }

        if (_coreVoltage != null && Ring0.ReadMsr(IA32_PERF_STATUS, out _, out uint edx))
        {
            _coreVoltage.Value = ((edx >> 32) & 0xFFFF) / (float)(1 << 13);
        }

        for (int i = 0; i < _coreVIDs.Length; i++)
        {
            if (Ring0.ReadMsr(IA32_PERF_STATUS, out _, out edx, _cpuId[i][0].Affinity) && ((edx >> 32) & 0xFFFF) > 0)
            {
                _coreVIDs[i].Value = ((edx >> 32) & 0xFFFF) / (float)(1 << 13);
                ActivateSensor(_coreVIDs[i]);
            }
            else
            {
                DeactivateSensor(_coreVIDs[i]);
            }
        }
    }

    [SuppressMessage("ReSharper", "IdentifierTypo")]
    private enum MicroArchitecture
    {
        Airmont,
        AlderLake,
        Atom,
        Broadwell,
        CannonLake,
        CometLake,
        Core,
        Goldmont,
        GoldmontPlus,
        Haswell,
        IceLake,
        IvyBridge,
        JasperLake,
        KabyLake,
        Nehalem,
        NetBurst,
        RocketLake,
        SandyBridge,
        Silvermont,
        Skylake,
        TigerLake,
        Tremont,
        RaptorLake,
        Unknown
    }

    // ReSharper disable InconsistentNaming
    private const uint IA32_PACKAGE_THERM_STATUS = 0x1B1;
    private const uint IA32_PERF_STATUS = 0x0198;
    private const uint IA32_TEMPERATURE_TARGET = 0x01A2;
    private const uint IA32_THERM_STATUS_MSR = 0x019C;

    private const uint MSR_DRAM_ENERGY_STATUS = 0x619;
    private const uint MSR_PKG_ENERGY_STATUS = 0x611;
    private const uint MSR_PLATFORM_INFO = 0xCE;
    private const uint MSR_PP0_ENERGY_STATUS = 0x639;
    private const uint MSR_PP1_ENERGY_STATUS = 0x641;

    private const uint MSR_RAPL_POWER_UNIT = 0x606;
    // ReSharper restore InconsistentNaming
}
