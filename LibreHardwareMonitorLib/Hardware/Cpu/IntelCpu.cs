// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Globalization;
using System.Text;

namespace LibreHardwareMonitor.Hardware.CPU
{
    internal sealed class IntelCpu : GenericCpu
    {
        private enum Microarchitecture
        {
            Unknown,
            NetBurst,
            Core,
            Atom,
            Nehalem,
            SandyBridge,
            IvyBridge,
            Haswell,
            Broadwell,
            Silvermont,
            Skylake,
            Airmont,
            KabyLake,
            ApolloLake,
            CoffeeLake
        }

        private readonly Sensor[] _coreTemperatures;
        private readonly Sensor _packageTemperature;
        private readonly Sensor[] _coreClocks;
        private readonly Sensor _busClock;
        private readonly Sensor[] _powerSensors;
        private readonly Sensor[] _distToTjMaxTemperatures;
        private readonly Sensor _coreMax;
        private readonly Sensor _coreAvg;

        private readonly Microarchitecture _microarchitecture;
        private readonly double _timeStampCounterMultiplier;

        private const uint IA32_THERM_STATUS_MSR = 0x019C;
        private const uint IA32_TEMPERATURE_TARGET = 0x01A2;
        private const uint IA32_PERF_STATUS = 0x0198;
        private const uint MSR_PLATFORM_INFO = 0xCE;
        private const uint IA32_PACKAGE_THERM_STATUS = 0x1B1;
        private const uint MSR_RAPL_POWER_UNIT = 0x606;
        private const uint MSR_PKG_ENERY_STATUS = 0x611;
        private const uint MSR_DRAM_ENERGY_STATUS = 0x619;
        private const uint MSR_PP0_ENERY_STATUS = 0x639;
        private const uint MSR_PP1_ENERY_STATUS = 0x641;

        private readonly uint[] _energyStatusMSRs = { MSR_PKG_ENERY_STATUS, MSR_PP0_ENERY_STATUS, MSR_PP1_ENERY_STATUS, MSR_DRAM_ENERGY_STATUS };
        private readonly string[] _powerSensorLabels = { "CPU Package", "CPU Cores", "CPU Graphics", "CPU DRAM" };
        private float _energyUnitMultiplier = 0;
        private DateTime[] _lastEnergyTime;
        private uint[] _lastEnergyConsumed;

        private float[] Floats(float f)
        {
            float[] result = new float[coreCount];
            for (int i = 0; i < coreCount; i++)
                result[i] = f;
            return result;
        }

        private float[] GetTjMaxFromMSR()
        {
            uint eax, edx;
            float[] result = new float[coreCount];
            for (int i = 0; i < coreCount; i++)
            {
                if (Ring0.RdmsrTx(IA32_TEMPERATURE_TARGET, out eax, out edx, 1UL << cpuid[i][0].Thread))
                    result[i] = (eax >> 16) & 0xFF;
                else
                    result[i] = 100;
            }
            return result;
        }

        public IntelCpu(int processorIndex, CpuID[][] cpuid, ISettings settings) : base(processorIndex, cpuid, settings)
        {
            // set tjMax
            float[] tjMax;
            switch (family)
            {
                case 0x06:
                    {
                        switch (model)
                        {
                            case 0x0F: // Intel Core 2 (65nm)
                                _microarchitecture = Microarchitecture.Core;
                                switch (stepping)
                                {
                                    case 0x06: // B2
                                        switch (coreCount)
                                        {
                                            case 2:
                                                tjMax = Floats(80 + 10);
                                                break;
                                            case 4:
                                                tjMax = Floats(90 + 10);
                                                break;
                                            default:
                                                tjMax = Floats(85 + 10);
                                                break;
                                        }
                                        break;
                                    case 0x0B: // G0
                                        tjMax = Floats(90 + 10);
                                        break;
                                    case 0x0D: // M0
                                        tjMax = Floats(85 + 10);
                                        break;
                                    default:
                                        tjMax = Floats(85 + 10);
                                        break;
                                }
                                break;
                            case 0x17: // Intel Core 2 (45nm)
                                _microarchitecture = Microarchitecture.Core;
                                tjMax = Floats(100);
                                break;
                            case 0x1C: // Intel Atom (45nm)
                                _microarchitecture = Microarchitecture.Atom;
                                switch (stepping)
                                {
                                    case 0x02: // C0
                                        tjMax = Floats(90);
                                        break;
                                    case 0x0A: // A0, B0
                                        tjMax = Floats(100);
                                        break;
                                    default:
                                        tjMax = Floats(90);
                                        break;
                                }
                                break;
                            case 0x1A: // Intel Core i7 LGA1366 (45nm)
                            case 0x1E: // Intel Core i5, i7 LGA1156 (45nm)
                            case 0x1F: // Intel Core i5, i7
                            case 0x25: // Intel Core i3, i5, i7 LGA1156 (32nm)
                            case 0x2C: // Intel Core i7 LGA1366 (32nm) 6 Core
                            case 0x2E: // Intel Xeon Processor 7500 series (45nm)
                            case 0x2F: // Intel Xeon Processor (32nm)
                                _microarchitecture = Microarchitecture.Nehalem;
                                tjMax = GetTjMaxFromMSR();
                                break;
                            case 0x2A: // Intel Core i5, i7 2xxx LGA1155 (32nm)
                            case 0x2D: // Next Generation Intel Xeon, i7 3xxx LGA2011 (32nm)
                                _microarchitecture = Microarchitecture.SandyBridge;
                                tjMax = GetTjMaxFromMSR();
                                break;
                            case 0x3A: // Intel Core i5, i7 3xxx LGA1155 (22nm)
                            case 0x3E: // Intel Core i7 4xxx LGA2011 (22nm)
                                _microarchitecture = Microarchitecture.IvyBridge;
                                tjMax = GetTjMaxFromMSR();
                                break;
                            case 0x3C: // Intel Core i5, i7 4xxx LGA1150 (22nm)
                            case 0x3F: // Intel Xeon E5-2600/1600 v3, Core i7-59xx
                                       // LGA2011-v3, Haswell-E (22nm)
                            case 0x45: // Intel Core i5, i7 4xxxU (22nm)
                            case 0x46:
                                _microarchitecture = Microarchitecture.Haswell;
                                tjMax = GetTjMaxFromMSR();
                                break;
                            case 0x3D: // Intel Core M-5xxx (14nm)
                            case 0x47: // Intel i5, i7 5xxx, Xeon E3-1200 v4 (14nm)
                            case 0x4F: // Intel Xeon E5-26xx v4
                            case 0x56: // Intel Xeon D-15xx
                                _microarchitecture = Microarchitecture.Broadwell;
                                tjMax = GetTjMaxFromMSR();
                                break;
                            case 0x36: // Intel Atom S1xxx, D2xxx, N2xxx (32nm)
                                _microarchitecture = Microarchitecture.Atom;
                                tjMax = GetTjMaxFromMSR();
                                break;
                            case 0x37: // Intel Atom E3xxx, Z3xxx (22nm)
                            case 0x4A:
                            case 0x4D: // Intel Atom C2xxx (22nm)
                            case 0x5A:
                            case 0x5D:
                                _microarchitecture = Microarchitecture.Silvermont;
                                tjMax = GetTjMaxFromMSR();
                                break;
                            case 0x4E:
                            case 0x5E: // Intel Core i5, i7 6xxxx LGA1151 (14nm)
                            case 0x55: // Intel Core X i7, i9 7xxx LGA2066 (14nm)
                                _microarchitecture = Microarchitecture.Skylake;
                                tjMax = GetTjMaxFromMSR();
                                break;
                            case 0x4C:
                                _microarchitecture = Microarchitecture.Airmont;
                                tjMax = GetTjMaxFromMSR();
                                break;
                            case 0x8E:
                            case 0x9E: // Intel Core i5, i7 7xxxx (14nm)
                                _microarchitecture = Microarchitecture.KabyLake;
                                tjMax = GetTjMaxFromMSR();
                                break;
                            case 0x5C: // Intel ApolloLake
                                _microarchitecture = Microarchitecture.ApolloLake;
                                tjMax = GetTjMaxFromMSR();
                                break;
                            case 0xAE: // Intel Core i5, i7 8xxxx (14nm++)
                                _microarchitecture = Microarchitecture.CoffeeLake;
                                tjMax = GetTjMaxFromMSR();
                                break;
                            default:
                                _microarchitecture = Microarchitecture.Unknown;
                                tjMax = Floats(100);
                                break;
                        }
                    }
                    break;
                case 0x0F:
                    {
                        switch (model)
                        {
                            case 0x00: // Pentium 4 (180nm)
                            case 0x01: // Pentium 4 (130nm)
                            case 0x02: // Pentium 4 (130nm)
                            case 0x03: // Pentium 4, Celeron D (90nm)
                            case 0x04: // Pentium 4, Pentium D, Celeron D (90nm)
                            case 0x06: // Pentium 4, Pentium D, Celeron D (65nm)
                                _microarchitecture = Microarchitecture.NetBurst;
                                tjMax = Floats(100);
                                break;
                            default:
                                _microarchitecture = Microarchitecture.Unknown;
                                tjMax = Floats(100);
                                break;
                        }
                    }
                    break;
                default:
                    _microarchitecture = Microarchitecture.Unknown;
                    tjMax = Floats(100);
                    break;
            }

            // set timeStampCounterMultiplier
            switch (_microarchitecture)
            {
                case Microarchitecture.NetBurst:
                case Microarchitecture.Atom:
                case Microarchitecture.Core:
                    {
                        uint eax, edx;
                        if (Ring0.Rdmsr(IA32_PERF_STATUS, out eax, out edx))
                        {
                            _timeStampCounterMultiplier = ((edx >> 8) & 0x1f) + 0.5 * ((edx >> 14) & 1);
                        }
                    }
                    break;
                case Microarchitecture.Nehalem:
                case Microarchitecture.SandyBridge:
                case Microarchitecture.IvyBridge:
                case Microarchitecture.Haswell:
                case Microarchitecture.Broadwell:
                case Microarchitecture.Silvermont:
                case Microarchitecture.Skylake:
                case Microarchitecture.Airmont:
                case Microarchitecture.ApolloLake:
                case Microarchitecture.KabyLake:
                case Microarchitecture.CoffeeLake:
                    {
                        uint eax, edx;
                        if (Ring0.Rdmsr(MSR_PLATFORM_INFO, out eax, out edx))
                        {
                            _timeStampCounterMultiplier = (eax >> 8) & 0xff;
                        }
                    }
                    break;
                default:
                    _timeStampCounterMultiplier = 0;
                    break;
            }

            int coreSensorId = 0;

            // check if processor supports a digital thermal sensor at core level
            if (cpuid[0][0].Data.GetLength(0) > 6 && (cpuid[0][0].Data[6, 0] & 1) != 0 && _microarchitecture != Microarchitecture.Unknown)
            {
                _coreTemperatures = new Sensor[coreCount];
                for (int i = 0; i < _coreTemperatures.Length; i++)
                {
                    _coreTemperatures[i] = new Sensor(CoreString(i), coreSensorId, SensorType.Temperature, this, new[] {
                        new ParameterDescription("TjMax [°C]", "TjMax temperature of the core sensor.\n" + "Temperature = TjMax - TSlope * Value.", tjMax[i]),
                        new ParameterDescription("TSlope [°C]", "Temperature slope of the digital thermal sensor.\n" + "Temperature = TjMax - TSlope * Value.", 1)}, settings);
                    ActivateSensor(_coreTemperatures[i]);
                    coreSensorId++;
                }
            }
            else
                _coreTemperatures = new Sensor[0];

            // check if processor supports a digital thermal sensor at package level
            if (cpuid[0][0].Data.GetLength(0) > 6 && (cpuid[0][0].Data[6, 0] & 0x40) != 0 && _microarchitecture != Microarchitecture.Unknown)
            {
                _packageTemperature = new Sensor("CPU Package", coreSensorId, SensorType.Temperature, this, new[] {
                    new ParameterDescription("TjMax [°C]", "TjMax temperature of the package sensor.\n" + "Temperature = TjMax - TSlope * Value.", tjMax[0]),
                    new ParameterDescription("TSlope [°C]", "Temperature slope of the digital thermal sensor.\n" + "Temperature = TjMax - TSlope * Value.", 1)}, settings);
                ActivateSensor(_packageTemperature);
                coreSensorId++;
            }

            // dist to tjmax sensor
            if (cpuid[0][0].Data.GetLength(0) > 6 && (cpuid[0][0].Data[6, 0] & 1) != 0 && _microarchitecture != Microarchitecture.Unknown)
            {
                _distToTjMaxTemperatures = new Sensor[coreCount];
                for (int i = 0; i < _distToTjMaxTemperatures.Length; i++)
                {
                    _distToTjMaxTemperatures[i] = new Sensor(CoreString(i) + " Distance to TjMax", coreSensorId, SensorType.Temperature, this, settings);
                    ActivateSensor(_distToTjMaxTemperatures[i]);
                    coreSensorId++;
                }
            }
            else
                _distToTjMaxTemperatures = new Sensor[0];

            //core temp avg and max value
            //is only available when the cpu has more than 1 core
            if (cpuid[0][0].Data.GetLength(0) > 6 && (cpuid[0][0].Data[6, 0] & 0x40) != 0 && _microarchitecture != Microarchitecture.Unknown && coreCount > 1)
            {
                _coreMax = new Sensor("Core Max", coreSensorId, SensorType.Temperature, this, settings);
                ActivateSensor(_coreMax);
                coreSensorId++;

                _coreAvg = new Sensor("Core Average", coreSensorId, SensorType.Temperature, this, settings);
                ActivateSensor(_coreAvg);
                coreSensorId++;
            }
            else
            {
                _coreMax = null;
                _coreAvg = null;
            }

            _busClock = new Sensor("Bus Speed", 0, SensorType.Clock, this, settings);
            _coreClocks = new Sensor[coreCount];
            for (int i = 0; i < _coreClocks.Length; i++)
            {
                _coreClocks[i] = new Sensor(CoreString(i), i + 1, SensorType.Clock, this, settings);
                if (HasTimeStampCounter && _microarchitecture != Microarchitecture.Unknown)
                    ActivateSensor(_coreClocks[i]);
            }

            if (_microarchitecture == Microarchitecture.SandyBridge ||
                _microarchitecture == Microarchitecture.IvyBridge ||
                _microarchitecture == Microarchitecture.Haswell ||
                _microarchitecture == Microarchitecture.Broadwell ||
                _microarchitecture == Microarchitecture.Skylake ||
                _microarchitecture == Microarchitecture.Silvermont ||
                _microarchitecture == Microarchitecture.Airmont ||
                _microarchitecture == Microarchitecture.KabyLake ||
                _microarchitecture == Microarchitecture.ApolloLake)
            {
                _powerSensors = new Sensor[_energyStatusMSRs.Length];
                _lastEnergyTime = new DateTime[_energyStatusMSRs.Length];
                _lastEnergyConsumed = new uint[_energyStatusMSRs.Length];

                uint eax, edx;
                if (Ring0.Rdmsr(MSR_RAPL_POWER_UNIT, out eax, out edx))
                    switch (_microarchitecture)
                    {
                        case Microarchitecture.Silvermont:
                        case Microarchitecture.Airmont:
                            _energyUnitMultiplier = 1.0e-6f * (1 << (int)((eax >> 8) & 0x1F));
                            break;
                        default:
                            _energyUnitMultiplier = 1.0f / (1 << (int)((eax >> 8) & 0x1F));
                            break;
                    }
                if (_energyUnitMultiplier != 0)
                {
                    for (int i = 0; i < _energyStatusMSRs.Length; i++)
                    {
                        if (!Ring0.Rdmsr(_energyStatusMSRs[i], out eax, out edx))
                            continue;

                        _lastEnergyTime[i] = DateTime.UtcNow;
                        _lastEnergyConsumed[i] = eax;
                        _powerSensors[i] = new Sensor(_powerSensorLabels[i], i,
                          SensorType.Power, this, settings);
                        ActivateSensor(_powerSensors[i]);
                    }
                }
            }

            Update();
        }

        protected override uint[] GetMSRs()
        {
            return new[] {
                MSR_PLATFORM_INFO,
                IA32_PERF_STATUS ,
                IA32_THERM_STATUS_MSR,
                IA32_TEMPERATURE_TARGET,
                IA32_PACKAGE_THERM_STATUS,
                MSR_RAPL_POWER_UNIT,
                MSR_PKG_ENERY_STATUS,
                MSR_DRAM_ENERGY_STATUS,
                MSR_PP0_ENERY_STATUS,
                MSR_PP1_ENERY_STATUS
            };
        }

        public override string GetReport()
        {
            StringBuilder r = new StringBuilder();
            r.Append(base.GetReport());
            r.Append("Microarchitecture: ");
            r.AppendLine(_microarchitecture.ToString());
            r.Append("Time Stamp Counter Multiplier: ");
            r.AppendLine(_timeStampCounterMultiplier.ToString(CultureInfo.InvariantCulture));
            r.AppendLine();
            return r.ToString();
        }

        public override void Update()
        {
            base.Update();

            float core_max = float.MinValue;
            float core_avg = 0;

            for (int i = 0; i < _coreTemperatures.Length; i++)
            {
                uint eax, edx;
                // if reading is valid
                if (Ring0.RdmsrTx(IA32_THERM_STATUS_MSR, out eax, out edx, 1UL << cpuid[i][0].Thread) && (eax & 0x80000000) != 0)
                {
                    // get the dist from tjMax from bits 22:16
                    float deltaT = ((eax & 0x007F0000) >> 16);
                    float tjMax = _coreTemperatures[i].Parameters[0].Value;
                    float tSlope = _coreTemperatures[i].Parameters[1].Value;
                    _coreTemperatures[i].Value = tjMax - tSlope * deltaT;

                    core_avg += (float)_coreTemperatures[i].Value;
                    if (core_max < _coreTemperatures[i].Value)
                        core_max = (float)_coreTemperatures[i].Value;

                    _distToTjMaxTemperatures[i].Value = deltaT;

                }
                else
                {
                    _coreTemperatures[i].Value = null;
                    _distToTjMaxTemperatures[i].Value = null;
                }
            }

            //calculate average cpu temperature over all cores
            if (_coreMax != null && core_max != float.MinValue)
            {
                _coreMax.Value = core_max;
                core_avg /= _coreTemperatures.Length;
                _coreAvg.Value = core_avg;
            }

            if (_packageTemperature != null)
            {
                uint eax, edx;
                // if reading is valid
                if (Ring0.RdmsrTx(IA32_PACKAGE_THERM_STATUS, out eax, out edx, 1UL << cpuid[0][0].Thread) && (eax & 0x80000000) != 0)
                {
                    // get the dist from tjMax from bits 22:16
                    float deltaT = ((eax & 0x007F0000) >> 16);
                    float tjMax = _packageTemperature.Parameters[0].Value;
                    float tSlope = _packageTemperature.Parameters[1].Value;
                    _packageTemperature.Value = tjMax - tSlope * deltaT;
                }
                else
                    _packageTemperature.Value = null;
            }

            if (HasTimeStampCounter && _timeStampCounterMultiplier > 0)
            {
                double newBusClock = 0;
                uint eax, edx;
                for (int i = 0; i < _coreClocks.Length; i++)
                {
                    System.Threading.Thread.Sleep(1);
                    if (Ring0.RdmsrTx(IA32_PERF_STATUS, out eax, out edx, 1UL << cpuid[i][0].Thread))
                    {
                        newBusClock = TimeStampCounterFrequency / _timeStampCounterMultiplier;
                        switch (_microarchitecture)
                        {
                            case Microarchitecture.Nehalem:
                                {
                                    uint multiplier = eax & 0xff;
                                    _coreClocks[i].Value = (float)(multiplier * newBusClock);
                                }
                                break;
                            case Microarchitecture.SandyBridge:
                            case Microarchitecture.IvyBridge:
                            case Microarchitecture.Haswell:
                            case Microarchitecture.Broadwell:
                            case Microarchitecture.Silvermont:
                            case Microarchitecture.Skylake:
                            case Microarchitecture.ApolloLake:
                            case Microarchitecture.KabyLake:
                            case Microarchitecture.CoffeeLake:
                                {
                                    uint multiplier = (eax >> 8) & 0xff;
                                    _coreClocks[i].Value = (float)(multiplier * newBusClock);
                                }
                                break;
                            default:
                                {
                                    double multiplier = ((eax >> 8) & 0x1f) + 0.5 * ((eax >> 14) & 1);
                                    _coreClocks[i].Value = (float)(multiplier * newBusClock);
                                }
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

                    uint eax, edx;
                    if (!Ring0.Rdmsr(_energyStatusMSRs[sensor.Index], out eax, out edx))
                        continue;

                    DateTime time = DateTime.UtcNow;
                    uint energyConsumed = eax;
                    float deltaTime = (float)(time - _lastEnergyTime[sensor.Index]).TotalSeconds;
                    if (deltaTime < 0.01)
                        continue;

                    sensor.Value = _energyUnitMultiplier * unchecked(energyConsumed - _lastEnergyConsumed[sensor.Index]) / deltaTime;
                    _lastEnergyTime[sensor.Index] = time;
                    _lastEnergyConsumed[sensor.Index] = energyConsumed;
                }
            }
        }
    }
}
