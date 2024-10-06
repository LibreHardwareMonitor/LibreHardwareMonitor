// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Globalization;
using System.Text;

namespace LibreHardwareMonitor.Hardware.Cpu.Intel;

/// <summary>
/// Intel CPU
/// </summary>
/// <seealso cref="GenericCpu" />
internal sealed class IntelCpu : GenericCpu
{
    #region Fields

    private Sensor _busClock;
    private Sensor _coreAvg;
    private Sensor[] _coreClocks;
    private Sensor _coreMax;
    private Sensor[] _coreTemperatures;
    private Sensor[] _coreVIDs;
    private Sensor _coreVoltage;
    private Sensor[] _distToTjMaxTemperatures;
    private uint[] _energyStatusMsrs = [
        IntelConstants.MSR_PKG_ENERGY_STATUS,
        IntelConstants.MSR_PP0_ENERGY_STATUS,
        IntelConstants.MSR_PP1_ENERGY_STATUS,
        IntelConstants.MSR_DRAM_ENERGY_STATUS,
        IntelConstants.MSR_PLATFORM_ENERGY_STATUS
    ];
    private uint[] _lastEnergyConsumed;
    private DateTime[] _lastEnergyTime;
    private IntelMicroArchitecture _microArchitecture;
    private float[] _tjMax;
    private double _timeStampCounterMultiplier;
    private Sensor _packageTemperature;
    private Sensor[] _powerSensors;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the energy units multiplier.
    /// </summary>
    /// <value>
    /// The energy units multiplier.
    /// </value>
    public float EnergyUnitsMultiplier { get; private set; }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="IntelCpu"/> class.
    /// </summary>
    /// <param name="processorIndex">Index of the processor.</param>
    /// <param name="cpuId">The cpu identifier.</param>
    /// <param name="settings">The settings.</param>
    public IntelCpu(int processorIndex, CpuId[][] cpuId, ISettings settings) : base(processorIndex, cpuId, settings)
    {
        // Assign TJ Max and Micro Architecture
        SetTjMaxAndMicroArchitecture();

        // Set timeStampCounterMultiplier
        SetTimeStampCounterMultiplier();

        // Sensors
        CreateTemperatureSensors();
        CreateClockSensors();
        CreateVoltageSensors();
        CreatePowerSensors();

        // Initialize
        Initialize();

        // Update
        Update();
    }

    #endregion

    #region Methods

    /// <summary>
    /// Prints the data to a report.
    /// </summary>
    /// <returns></returns>
    /// <inheritdoc />
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

    /// <summary>
    /// Updates all sensors.
    /// </summary>
    /// <inheritdoc />
    public override void Update()
    {
        // Update Generic CPU
        base.Update();

        // Sensors
        UpdateTemperatureSensors();
        UpdateClockSensors();
        UpdateVoltageSensors();
        UpdatePowerSensors();
    }

    /// <summary>
    /// Gets the MSRS.
    /// </summary>
    /// <returns></returns>
    protected override uint[] GetMsrs() =>
    [
        IntelConstants.MSR_PLATFORM_INFO,
        IntelConstants.IA32_PERF_STATUS,
        IntelConstants.IA32_THERM_STATUS_MSR,
        IntelConstants.IA32_TEMPERATURE_TARGET,
        IntelConstants.IA32_PACKAGE_THERM_STATUS,
        IntelConstants.MSR_RAPL_POWER_UNIT,
        IntelConstants.MSR_PKG_ENERGY_STATUS,
        IntelConstants.MSR_DRAM_ENERGY_STATUS,
        IntelConstants.MSR_PP0_ENERGY_STATUS,
        IntelConstants.MSR_PP1_ENERGY_STATUS,
        IntelConstants.MSR_PLATFORM_ENERGY_STATUS
    ];

    /// <summary>
    /// Cores the string.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns></returns>
    protected override string SetCoreName(int index)
    {
        string name = $"Core #{index}";

        // Intel CPU that contains P-Cores and E-Cores
        if (_microArchitecture is
            IntelMicroArchitecture.AlderLake or
            IntelMicroArchitecture.RaptorLake or
            IntelMicroArchitecture.MeteorLake or
            IntelMicroArchitecture.ArrowLake)
        {
            name = CpuId[index].Length > 1
                ? $"P-Core #{index}"
                : $"E-Core #{index}";
        }
        return name;
    }

    /// <summary>
    /// Set the TJ Max and Micro Architecture.
    /// </summary>
    /// <returns></returns>
    private void SetTjMaxAndMicroArchitecture()
    {
        // Set tjMax
        switch (Family)
        {
            case 0x06:
                {
                    switch (Model)
                    {
                        case 0x0F: // Intel Core 2 (65nm)
                            _microArchitecture = IntelMicroArchitecture.Core;
                            _tjMax = Stepping switch
                            {
                                // B2
                                0x06 => CoreCount switch
                                {
                                    2 => SetFloatValues(80 + 10),
                                    4 => SetFloatValues(90 + 10),
                                    _ => SetFloatValues(85 + 10)
                                },
                                // G0
                                0x0B => SetFloatValues(90 + 10),
                                // M0
                                0x0D => SetFloatValues(85 + 10),
                                _ => SetFloatValues(85 + 10)
                            };
                            break;

                        case 0x17: // Intel Core 2 (45nm)
                            _microArchitecture = IntelMicroArchitecture.Core;
                            _tjMax = SetFloatValues(100);
                            break;

                        case 0x1C: // Intel Atom (45nm)
                            _microArchitecture = IntelMicroArchitecture.Atom;
                            _tjMax = Stepping switch
                            {
                                // C0
                                0x02 => SetFloatValues(90),
                                // A0, B0
                                0x0A => SetFloatValues(100),
                                _ => SetFloatValues(90)
                            };
                            break;

                        case 0x1A: // Intel Core i7 LGA1366 (45nm)
                        case 0x1E: // Intel Core i5, i7 LGA1156 (45nm)
                        case 0x1F: // Intel Core i5, i7
                        case 0x25: // Intel Core i3, i5, i7 LGA1156 (32nm)
                        case 0x2C: // Intel Core i7 LGA1366 (32nm) 6 Core
                        case 0x2E: // Intel Xeon Processor 7500 series (45nm)
                        case 0x2F: // Intel Xeon Processor (32nm)
                            _microArchitecture = IntelMicroArchitecture.Nehalem;
                            _tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x2A: // Intel Core i5, i7 2xxx LGA1155 (32nm)
                        case 0x2D: // Next Generation Intel Xeon, i7 3xxx LGA2011 (32nm)
                            _microArchitecture = IntelMicroArchitecture.SandyBridge;
                            _tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x3A: // Intel Core i5, i7 3xxx LGA1155 (22nm)
                        case 0x3E: // Intel Core i7 4xxx LGA2011 (22nm)
                            _microArchitecture = IntelMicroArchitecture.IvyBridge;
                            _tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x3C: // Intel Core i5, i7 4xxx LGA1150 (22nm)
                        case 0x3F: // Intel Xeon E5-2600/1600 v3, Core i7-59xx
                        // LGA2011-v3, Haswell-E (22nm)
                        case 0x45: // Intel Core i5, i7 4xxxU (22nm)
                        case 0x46:
                            _microArchitecture = IntelMicroArchitecture.Haswell;
                            _tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x3D: // Intel Core M-5xxx (14nm)
                        case 0x47: // Intel i5, i7 5xxx, Xeon E3-1200 v4 (14nm)
                        case 0x4F: // Intel Xeon E5-26xx v4
                        case 0x56: // Intel Xeon D-15xx
                            _microArchitecture = IntelMicroArchitecture.Broadwell;
                            _tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x36: // Intel Atom S1xxx, D2xxx, N2xxx (32nm)
                            _microArchitecture = IntelMicroArchitecture.Atom;
                            _tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x37: // Intel Atom E3xxx, Z3xxx (22nm)
                        case 0x4A:
                        case 0x4D: // Intel Atom C2xxx (22nm)
                        case 0x5A:
                        case 0x5D:
                            _microArchitecture = IntelMicroArchitecture.Silvermont;
                            _tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x4E:
                        case 0x5E: // Intel Core i5, i7 6xxxx LGA1151 (14nm)
                        case 0x55: // Intel Core X i7, i9 7xxx LGA2066 (14nm)
                            _microArchitecture = IntelMicroArchitecture.Skylake;
                            _tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x4C: // Intel Airmont (Cherry Trail, Braswell)
                            _microArchitecture = IntelMicroArchitecture.Airmont;
                            _tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x8E: // Intel Core i5, i7 7xxxx (14nm) (Kaby Lake) and 8xxxx (14nm++) (Coffee Lake)
                        case 0x9E:
                            _microArchitecture = IntelMicroArchitecture.KabyLake;
                            _tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x5C: // Goldmont (Apollo Lake)
                        case 0x5F: // (Denverton)
                            _microArchitecture = IntelMicroArchitecture.Goldmont;
                            _tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x7A: // Goldmont plus (Gemini Lake)
                            _microArchitecture = IntelMicroArchitecture.GoldmontPlus;
                            _tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x66: // Intel Core i3 8xxx (10nm) (Cannon Lake)
                            _microArchitecture = IntelMicroArchitecture.CannonLake;
                            _tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x7D: // Intel Core i3, i5, i7 10xxx (10nm) (Ice Lake)
                        case 0x7E:
                        case 0x6A: // Ice Lake server
                        case 0x6C:
                            _microArchitecture = IntelMicroArchitecture.IceLake;
                            _tjMax = GetTjMaxFromMsr();
                            break;

                        case 0xA5:
                        case 0xA6: // Intel Core i3, i5, i7 10xxxU (14nm)
                            _microArchitecture = IntelMicroArchitecture.CometLake;
                            _tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x86: // Tremont (10nm) (Elkhart Lake, Skyhawk Lake)
                            _microArchitecture = IntelMicroArchitecture.Tremont;
                            _tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x8C: // Tiger Lake (Intel 10 nm SuperFin, Gen. 11)
                        case 0x8D:
                            _microArchitecture = IntelMicroArchitecture.TigerLake;
                            _tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x97: // Alder Lake (Intel 7 (10ESF), Gen. 12)
                        case 0x9A: // Alder Lake-L (Intel 7 (10ESF), Gen. 12)
                        case 0xBE: // Alder Lake-N (Intel 7 (10ESF), Gen. 12)
                            _microArchitecture = IntelMicroArchitecture.AlderLake;
                            _tjMax = GetTjMaxFromMsr();
                            break;

                        case 0xB7: // Raptor Lake (Intel 7 (10ESF), Gen. 13)
                        case 0xBA: // Raptor Lake-P (Intel 7 (10ESF), Gen. 13)
                        case 0xBF: // Raptor Lake-N (Intel 7 (10ESF), Gen. 13)
                            _microArchitecture = IntelMicroArchitecture.RaptorLake;
                            _tjMax = GetTjMaxFromMsr();
                            break;

                        case 0xAC: // Meteor Lake (Intel 4, TSMC N5/N6, Gen. 14)
                        case 0xAA: // Meteor Lake-L (Intel 4, TSMC N5/N6, Gen. 14)
                            _microArchitecture = IntelMicroArchitecture.MeteorLake;
                            _tjMax = GetTjMaxFromMsr();
                            break;

                        case 0x9C: // Jasper Lake (10nm)
                            _microArchitecture = IntelMicroArchitecture.JasperLake;
                            _tjMax = GetTjMaxFromMsr();
                            break;

                        case 0xA7: // Intel Core i5, i6, i7 11xxx (14nm) (Rocket Lake)
                            _microArchitecture = IntelMicroArchitecture.RocketLake;
                            _tjMax = GetTjMaxFromMsr();
                            break;

                        default:
                            _microArchitecture = IntelMicroArchitecture.Unknown;
                            _tjMax = SetFloatValues(100);
                            break;
                    }
                }

                break;
            case 0x0F:
                switch (Model)
                {
                    case 0x00: // Pentium 4 (180nm)
                    case 0x01: // Pentium 4 (130nm)
                    case 0x02: // Pentium 4 (130nm)
                    case 0x03: // Pentium 4, Celeron D (90nm)
                    case 0x04: // Pentium 4, Pentium D, Celeron D (90nm)
                    case 0x06: // Pentium 4, Pentium D, Celeron D (65nm)
                        _microArchitecture = IntelMicroArchitecture.NetBurst;
                        _tjMax = SetFloatValues(100);
                        break;

                    default:
                        _microArchitecture = IntelMicroArchitecture.Unknown;
                        _tjMax = SetFloatValues(100);
                        break;
                }

                break;
            default:
                _microArchitecture = IntelMicroArchitecture.Unknown;
                _tjMax = SetFloatValues(100);
                break;
        }
    }

    /// <summary>
    /// Sets the time stamp counter multiplier.
    /// </summary>
    /// <returns></returns>
    private void SetTimeStampCounterMultiplier()
    {
        switch (_microArchitecture)
        {
            case IntelMicroArchitecture.Atom:
            case IntelMicroArchitecture.Core:
            case IntelMicroArchitecture.NetBurst:
                if (Ring0.ReadMsr(IntelConstants.IA32_PERF_STATUS, out uint _, out uint edx))
                {
                    _timeStampCounterMultiplier = ((edx >> 8) & 0x1f) + (0.5 * ((edx >> 14) & 1));
                }
                break;
            case IntelMicroArchitecture.Airmont:
            case IntelMicroArchitecture.AlderLake:
            case IntelMicroArchitecture.ArrowLake:
            case IntelMicroArchitecture.Broadwell:
            case IntelMicroArchitecture.CannonLake:
            case IntelMicroArchitecture.CometLake:
            case IntelMicroArchitecture.Goldmont:
            case IntelMicroArchitecture.GoldmontPlus:
            case IntelMicroArchitecture.Haswell:
            case IntelMicroArchitecture.IceLake:
            case IntelMicroArchitecture.IvyBridge:
            case IntelMicroArchitecture.JasperLake:
            case IntelMicroArchitecture.KabyLake:
            case IntelMicroArchitecture.Nehalem:
            case IntelMicroArchitecture.MeteorLake:
            case IntelMicroArchitecture.RaptorLake:
            case IntelMicroArchitecture.RocketLake:
            case IntelMicroArchitecture.SandyBridge:
            case IntelMicroArchitecture.Silvermont:
            case IntelMicroArchitecture.Skylake:
            case IntelMicroArchitecture.TigerLake:
            case IntelMicroArchitecture.Tremont:
                if (Ring0.ReadMsr(IntelConstants.MSR_PLATFORM_INFO, out uint eax, out uint _))
                {
                    _timeStampCounterMultiplier = (eax >> 8) & 0xff;
                }
                break;
            case IntelMicroArchitecture.Unknown:
            default:
                _timeStampCounterMultiplier = 0;
                break;
        }
    }

    /// <summary>
    /// Create CPU temperature sensors.
    /// </summary>
    /// <returns></returns>
    private void CreateTemperatureSensors()
    {
        int coreSensorId = 0;
        uint[,] cpu0Data = Cpu0.Data;

        // Check if processor supports a digital thermal sensor at core level
        if (cpu0Data.GetLength(0) > 6 && (cpu0Data[6, 0] & 1) != 0 && _microArchitecture != IntelMicroArchitecture.Unknown)
        {
            _coreTemperatures = new Sensor[CoreCount];
            for (int i = 0; i < _coreTemperatures.Length; i++)
            {
                _coreTemperatures[i] = new Sensor(SetCoreName(i),
                    coreSensorId,
                    SensorType.Temperature,
                    this,
                    [
                        new ParameterDescription("TjMax [°C]", "TjMax temperature of the core sensor.\n" + "Temperature = TjMax - TSlope * Value.", _tjMax[i]),
                        new ParameterDescription("TSlope [°C]", "Temperature slope of the digital thermal sensor.\n" + "Temperature = TjMax - TSlope * Value.", 1)
                    ],
                    Settings);

                ActivateSensor(_coreTemperatures[i]);
                coreSensorId++;
            }
        }
        else
        {
            _coreTemperatures = [];
        }

        // Check if processor supports a digital thermal sensor at package level
        if (cpu0Data.GetLength(0) > 6 && (cpu0Data[6, 0] & 0x40) != 0 && _microArchitecture != IntelMicroArchitecture.Unknown)
        {
            _packageTemperature = new Sensor("CPU Package",
                coreSensorId,
                SensorType.Temperature,
                this,
                [
                    new ParameterDescription("TjMax [°C]", "TjMax temperature of the package sensor.\n" + "Temperature = TjMax - TSlope * Value.", _tjMax[0]),
                    new ParameterDescription("TSlope [°C]", "Temperature slope of the digital thermal sensor.\n" + "Temperature = TjMax - TSlope * Value.", 1)
                ],
                Settings);

            ActivateSensor(_packageTemperature);
            coreSensorId++;
        }

        // Dist to TjMax sensor
        if (cpu0Data.GetLength(0) > 6 && (cpu0Data[6, 0] & 1) != 0 && _microArchitecture != IntelMicroArchitecture.Unknown)
        {
            _distToTjMaxTemperatures = new Sensor[CoreCount];
            for (int i = 0; i < _distToTjMaxTemperatures.Length; i++)
            {
                _distToTjMaxTemperatures[i] = new Sensor(SetCoreName(i) + " Distance to TjMax", coreSensorId, SensorType.Temperature, this, Settings);
                ActivateSensor(_distToTjMaxTemperatures[i]);
                coreSensorId++;
            }
        }
        else
        {
            _distToTjMaxTemperatures = [];
        }

        // Core temp avg and max value
        // Note: only available when the cpu has more than 1 core
        if (cpu0Data.GetLength(0) > 6 && (cpu0Data[6, 0] & 0x40) != 0 && _microArchitecture != IntelMicroArchitecture.Unknown && CoreCount > 1)
        {
            _coreMax = new Sensor("Core Max", coreSensorId, SensorType.Temperature, this, Settings);
            ActivateSensor(_coreMax);
            coreSensorId++;

            _coreAvg = new Sensor("Core Average", coreSensorId, SensorType.Temperature, this, Settings);
            ActivateSensor(_coreAvg);
        }
        else
        {
            _coreMax = null;
            _coreAvg = null;
        }
    }

    /// <summary>
    /// Update CPU temperature sensors.
    /// </summary>
    /// <returns></returns>
    private void UpdateTemperatureSensors()
    {
        // Core counters
        uint eax;
        float coreMax = float.MinValue;
        float coreAvg = 0;

        // Cycle through cores
        for (int i = 0; i < _coreTemperatures.Length; i++)
        {
            var coreTemp = _coreTemperatures[i];
            var distToTjMax = _distToTjMaxTemperatures[i];

            // Get the latest reading
            if (Ring0.ReadMsr(IntelConstants.IA32_THERM_STATUS_MSR, out eax, out _, CpuId[i][0].Affinity) && (eax & 0x80000000) != 0)
            {
                // Get the dist from tjMax from bits 22:16
                float deltaT = (eax & 0x007F0000) >> 16;
                float tjMax = coreTemp.Parameters[0].Value;
                float tSlope = coreTemp.Parameters[1].Value;

                // Core Temp
                coreTemp.Value = tjMax - (tSlope * deltaT);

                // Core Average
                coreAvg += (float)coreTemp.Value;

                // Core Max
                if (coreMax < coreTemp.Value)
                {
                    coreMax = (float)coreTemp.Value;
                }

                // Distance to TJ Max
                distToTjMax.Value = deltaT;
            }
            else
            {
                coreTemp.Value = null;
                distToTjMax.Value = null;
            }
        }

        // Calculate average cpu temperature over all cores
        if (_coreMax is not null && !coreMax.Equals(float.MinValue))
        {
            _coreMax.Value = coreMax;
            coreAvg /= _coreTemperatures.Length;
            _coreAvg.Value = coreAvg;
        }

        // Package temperature
        if (_packageTemperature is null) return;

        // if reading is valid
        if (Ring0.ReadMsr(IntelConstants.IA32_PACKAGE_THERM_STATUS, out eax, out _, Cpu0.Affinity) && (eax & 0x80000000) != 0)
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

    /// <summary>
    /// Create CPU clock sensors.
    /// </summary>
    /// <returns></returns>
    private void CreateClockSensors()
    {
        _busClock = new Sensor("Bus Speed", 0, SensorType.Clock, this, Settings);
        _coreClocks = new Sensor[CoreCount];
        for (int i = 0; i < _coreClocks.Length; i++)
        {
            _coreClocks[i] = new Sensor(SetCoreName(i), i + 1, SensorType.Clock, this, Settings);
            if (HasTimeStampCounter && _microArchitecture != IntelMicroArchitecture.Unknown)
            {
                ActivateSensor(_coreClocks[i]);
            }
        }
    }

    /// <summary>
    /// Update CPU clock sensors.
    /// </summary>
    /// <returns></returns>
    private void UpdateClockSensors()
    {
        uint eax;

        // If there is a valid time stamp counter
        if (!HasTimeStampCounter || !(_timeStampCounterMultiplier > 0)) return;

        // Evaluate
        double newBusClock = 0;
        for (int i = 0; i < _coreClocks.Length; i++)
        {
            System.Threading.Thread.Sleep(1);
            if (Ring0.ReadMsr(IntelConstants.IA32_PERF_STATUS, out eax, out _, CpuId[i][0].Affinity))
            {
                newBusClock = TimeStampCounterFrequency / _timeStampCounterMultiplier;
                switch (_microArchitecture)
                {
                    case IntelMicroArchitecture.Nehalem:
                        _coreClocks[i].Value = (float)((eax & 0xff) * newBusClock);
                        break;
                    case IntelMicroArchitecture.Airmont:
                    case IntelMicroArchitecture.AlderLake:
                    case IntelMicroArchitecture.Broadwell:
                    case IntelMicroArchitecture.CannonLake:
                    case IntelMicroArchitecture.CometLake:
                    case IntelMicroArchitecture.Goldmont:
                    case IntelMicroArchitecture.GoldmontPlus:
                    case IntelMicroArchitecture.Haswell:
                    case IntelMicroArchitecture.IceLake:
                    case IntelMicroArchitecture.IvyBridge:
                    case IntelMicroArchitecture.JasperLake:
                    case IntelMicroArchitecture.KabyLake:
                    case IntelMicroArchitecture.MeteorLake:
                    case IntelMicroArchitecture.RaptorLake:
                    case IntelMicroArchitecture.RocketLake:
                    case IntelMicroArchitecture.SandyBridge:
                    case IntelMicroArchitecture.Silvermont:
                    case IntelMicroArchitecture.Skylake:
                    case IntelMicroArchitecture.TigerLake:
                    case IntelMicroArchitecture.Tremont:
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

        // Bus clock
        if (!(newBusClock > 0)) return;
        _busClock.Value = (float)newBusClock;
        ActivateSensor(_busClock);
    }

    /// <summary>
    /// Create CPU voltage sensors.
    /// </summary>
    /// <returns></returns>
    private void CreateVoltageSensors()
    {
        uint eax;

        // ReSharper disable once ShiftExpressionRealShiftCountIsZero
        // Core Voltages
        if (Ring0.ReadMsr(IntelConstants.IA32_PERF_STATUS, out eax, out uint _) && ((eax >> 32) & 0xFFFF) > 0)
        {
            _coreVoltage = new Sensor("CPU Core", 0, SensorType.Voltage, this, Settings);
            ActivateSensor(_coreVoltage);
        }

        // Core VIDs
        _coreVIDs = new Sensor[CoreCount];
        for (int i = 0; i < _coreVIDs.Length; i++)
        {
            _coreVIDs[i] = new Sensor(SetCoreName(i), i + 1, SensorType.Voltage, this, Settings);
            ActivateSensor(_coreVIDs[i]);
        }
    }

    /// <summary>
    /// Update CPU voltage sensors.
    /// </summary>
    /// <returns></returns>
    private void UpdateVoltageSensors()
    {
        // Core voltage
        if (_coreVoltage is not null && Ring0.ReadMsr(IntelConstants.IA32_PERF_STATUS, out _, out uint edx))
        {
            // ReSharper disable once ShiftExpressionRealShiftCountIsZero
            _coreVoltage.Value = ((edx >> 32) & 0xFFFF) / (float)(1 << 13);
        }

        // Core VIDs
        for (int i = 0; i < _coreVIDs.Length; i++)
        {
            // ReSharper disable once ShiftExpressionRealShiftCountIsZero
            if (Ring0.ReadMsr(IntelConstants.IA32_PERF_STATUS, out _, out edx, CpuId[i][0].Affinity) && ((edx >> 32) & 0xFFFF) > 0)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                _coreVIDs[i].Value = ((edx >> 32) & 0xFFFF) / (float)(1 << 13);
                ActivateSensor(_coreVIDs[i]);
            }
            else
            {
                DeactivateSensor(_coreVIDs[i]);
            }
        }
    }

    /// <summary>
    /// Create CPU power sensors.
    /// </summary>
    /// <returns></returns>
    private void CreatePowerSensors()
    {
        uint eax;
        if (_microArchitecture is
            IntelMicroArchitecture.Atom or
            IntelMicroArchitecture.ArrowLake or
            IntelMicroArchitecture.Core or
            IntelMicroArchitecture.Nehalem or
            IntelMicroArchitecture.NetBurst or
            IntelMicroArchitecture.Unknown)
        {
            return;
        }

        // Energy unit multiplier
        if (Ring0.ReadMsr(IntelConstants.MSR_RAPL_POWER_UNIT, out eax, out uint _))
        {
            EnergyUnitsMultiplier = _microArchitecture switch
            {
                IntelMicroArchitecture.Silvermont or IntelMicroArchitecture.Airmont => 1.0e-6f * (1 << (int)((eax >> 8) & 0x1F)),
                _ => 1.0f / (1 << (int)((eax >> 8) & 0x1F))
            };
        }
        if (EnergyUnitsMultiplier == 0) return;

        // Power sensors
        _powerSensors = new Sensor[_energyStatusMsrs.Length];
        _lastEnergyTime = new DateTime[_energyStatusMsrs.Length];
        _lastEnergyConsumed = new uint[_energyStatusMsrs.Length];
        string[] powerSensorLabels = ["CPU Package", "CPU Cores", "CPU Graphics", "CPU Memory", "CPU Platform"];

        for (int i = 0; i < _energyStatusMsrs.Length; i++)
        {
            if (!Ring0.ReadMsr(_energyStatusMsrs[i], out eax, out uint _)) continue;

            // Don't show the "GPU Graphics" sensor on windows, it will show up under the GPU instead.
            if (i == 2 && !Software.OperatingSystem.IsUnix) continue;

            _lastEnergyTime[i] = DateTime.UtcNow;
            _lastEnergyConsumed[i] = eax;
            _powerSensors[i] = new Sensor(powerSensorLabels[i], i, SensorType.Power, this, Settings);

            // Sensor
            ActivateSensor(_powerSensors[i]);
        }
    }

    /// <summary>
    /// Updates CPU power sensors.
    /// </summary>
    /// <returns></returns>
    private void UpdatePowerSensors()
    {
        uint eax;
        if (_powerSensors is null) return;

        // Power sensors
        foreach (Sensor sensor in _powerSensors)
        {
            if (sensor == null) continue;
            if (!Ring0.ReadMsr(_energyStatusMsrs[sensor.Index], out eax, out _)) continue;

            DateTime time = DateTime.UtcNow;
            uint energyConsumed = eax;
            float deltaTime = (float)(time - _lastEnergyTime[sensor.Index]).TotalSeconds;
            if (deltaTime < 0.01) continue;

            // Set sensor
            sensor.Value = EnergyUnitsMultiplier * unchecked(energyConsumed - _lastEnergyConsumed[sensor.Index]) / deltaTime;
            _lastEnergyTime[sensor.Index] = time;
            _lastEnergyConsumed[sensor.Index] = energyConsumed;
        }
    }

    /// <summary>
    /// Sets float values.
    /// </summary>
    /// <param name="f">The f.</param>
    /// <returns></returns>
    private float[] SetFloatValues(float f)
    {
        float[] result = new float[CoreCount];
        for (int i = 0; i < CoreCount; i++)
        {
            result[i] = f;
        }
        return result;
    }

    /// <summary>
    /// Gets the tj maximum from MSR.
    /// </summary>
    /// <returns></returns>
    private float[] GetTjMaxFromMsr()
    {
        float[] result = new float[CoreCount];
        for (int i = 0; i < CoreCount; i++)
        {
            result[i] = Ring0.ReadMsr(IntelConstants.IA32_TEMPERATURE_TARGET, out uint eax, out uint _, CpuId[i][0].Affinity)
                ? (eax >> 16) & 0xFF
                : 100;
        }
        return result;
    }

    #endregion
}
