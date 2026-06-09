// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibreHardwareMonitor.PawnIo;

namespace LibreHardwareMonitor.Hardware.Cpu;

internal sealed class Amd1ACpu : AmdCpu
{
    private readonly Processor _processor;
    private readonly Dictionary<SensorType, int> _sensorTypeIndex;
    private readonly RyzenSMU _smu;
    private readonly AmdFamily17 _pawnModule;

    public Amd1ACpu(int processorIndex, CpuId[][] cpuId, ISettings settings) : base(processorIndex, cpuId, settings)
    {
        _pawnModule = new AmdFamily17();

        _sensorTypeIndex = new Dictionary<SensorType, int>();
        foreach (SensorType type in Enum.GetValues(typeof(SensorType)))
        {
            _sensorTypeIndex.Add(type, 0);
        }

        _sensorTypeIndex[SensorType.Load] = _active.Count(x => x.SensorType == SensorType.Load);

        _smu = new RyzenSMU();

        // Add all numa nodes.
        // Register ..1E_2, [10:8] + 1
        _processor = new Processor(this);

        int coreId = 0;
        int lastCoreId = -1;

        foreach (CpuId[] cpu in cpuId.OrderBy(x => x[0].ExtData[0x1e, 1] & 0xFF))
        {
            CpuId thread = cpu[0];

            int coreIdRead = (int)(thread.ExtData[0x1e, 1] & 0xff);
            int nodeId = (int)(thread.ExtData[0x1e, 2] & 0xff);

            if (coreIdRead != lastCoreId)
            {
                coreId++;
            }

            lastCoreId = coreIdRead;

            _processor.AppendThread(thread, nodeId, coreId);
        }

        _processor.CreateSmuSensors();

        Update();
    }

    public override string GetReport()
    {
        StringBuilder r = new();
        r.Append(base.GetReport());
        r.Append(_smu.GetReport());
        return r.ToString();
    }

    public override void Close()
    {
        base.Close();
        _pawnModule.Close();
        _smu.Close();
    }

    public override void Update()
    {
        base.Update();

        _processor.UpdateSensors();

        foreach (NumaNode node in _processor.Nodes)
        {
            NumaNode.UpdateSensors();

            foreach (Core c in node.Cores)
            {
                c.UpdateSensors();
            }
        }

        _processor.UpdateVirtualSensor();
    }

    private class Processor
    {
        private readonly Sensor _busClock;
        private readonly Sensor _avgClock;
        private readonly Sensor _avgClockEffcetive;
        private readonly Sensor _coreVids;

        private readonly Sensor[] _ccdTemperatures;
        private readonly Sensor _coreTemperatureTctlTdie;
        private readonly Amd1ACpu _cpu;
        private readonly Sensor _packagePower;
        private readonly Dictionary<KeyValuePair<uint, RyzenSMU.SmuSensorType>, Sensor> _smuSensors = new();

        private Sensor _ccdsAverageTemperature;
        private Sensor _ccdsMaxTemperature;
        private DateTime _lastSampleTime = new(0);
        private uint _lastPwrValue;

        private float[] _pmTable;
        private PerCoreBaseIndices _perCoreBaseIndices;

        public Processor(Hardware hardware)
        {
            _cpu = (Amd1ACpu)hardware;

            _packagePower = new Sensor("Package", _cpu._sensorTypeIndex[SensorType.Power]++, SensorType.Power, _cpu, _cpu._settings);
            _coreTemperatureTctlTdie = new Sensor("Core (Tctl/Tdie)", _cpu._sensorTypeIndex[SensorType.Temperature]++, SensorType.Temperature, _cpu, _cpu._settings);
            _ccdTemperatures = new Sensor[8];
            _busClock = new Sensor("Bus Speed", _cpu._sensorTypeIndex[SensorType.Clock]++, SensorType.Clock, _cpu, _cpu._settings);
            _avgClock = new Sensor("Cores (Average)", _cpu._sensorTypeIndex[SensorType.Clock]++, SensorType.Clock, _cpu, _cpu._settings);
            _avgClockEffcetive = new Sensor("Cores (Average Effective)", _cpu._sensorTypeIndex[SensorType.Clock]++, SensorType.Clock, _cpu, _cpu._settings);
            _coreVids = new Sensor("Core VIDs", _cpu._sensorTypeIndex[SensorType.Voltage]++, SensorType.Voltage, _cpu, _cpu._settings);

            _cpu.ActivateSensor(_packagePower);
            _cpu.ActivateSensor(_avgClock);
            _cpu.ActivateSensor(_avgClockEffcetive);

            _perCoreBaseIndices = GetPerCoreBaseIndices(_cpu._smu.PmTableVersion);
        }

        public void CreateSmuSensors()
        {
            foreach (KeyValuePair<uint, RyzenSMU.SmuSensorType> sensor in _cpu._smu.GetPmTableStructure())
            {
                _smuSensors.Add(sensor, new Sensor(sensor.Value.Name, _cpu._sensorTypeIndex[sensor.Value.Type]++, sensor.Value.Type, _cpu, _cpu._settings));
            }
        }

        public List<NumaNode> Nodes { get; } = new();

        public float[] PmTable => _pmTable;

        public PerCoreBaseIndices BaseIndices => _perCoreBaseIndices;

        public double BusClockValue
        {
            get
            {
                if (_busClock?.Value.HasValue == true && _busClock.Value > 0)
                    return (double)_busClock.Value;
                return 100.0;
            }
        }

        public void UpdateSensors()
        {
            NumaNode node = Nodes[0];
            Core core = node?.Cores[0];
            CpuId cpuId = core?.Threads.FirstOrDefault()?.Cpu;

            if (cpuId == null)
                return;

            GroupAffinity previousAffinity = ThreadAffinity.Set(cpuId.Affinity);

            // MSRC001_0299
            // TU [19:16]
            // ESU [12:8] -> Unit 15.3 micro Joule per increment
            // PU [3:0]
            _cpu._pawnModule.ReadMsr(MSR_PWR_UNIT, out uint eax, out uint _);
            int esu = (int)((eax >> 8) & 0x1F);
            double energyBaseUnit = Math.Pow(0.5, esu);

            DateTime sampleTime = DateTime.UtcNow;
            // MSRC001_029B
            // total_energy [31:0]
            _cpu._pawnModule.ReadMsr(MSR_PKG_ENERGY_STAT, out eax, out _);
            uint totalEnergy = eax;

            // THM_TCON_CUR_TMP [31:21]
            if (Mutexes.WaitPciBus(10))
            {
                uint temperature = _cpu._pawnModule.ReadSmn(F17H_M01H_THM_TCON_CUR_TMP);

                ThreadAffinity.Set(previousAffinity);

                TimeSpan deltaTime = sampleTime - _lastSampleTime;
                if (_lastSampleTime.Ticks == 0)
                {
                    deltaTime = new(0);
                    _lastSampleTime = sampleTime;
                    _lastPwrValue = totalEnergy;
                }

                _lastSampleTime = sampleTime;

                long pwr;
                if (_lastPwrValue <= totalEnergy)
                    pwr = totalEnergy - _lastPwrValue;
                else
                    pwr = (0xffffffff - _lastPwrValue) + totalEnergy;

                _lastPwrValue = totalEnergy;

                if (deltaTime.Ticks > 0)
                {
                    double energy = energyBaseUnit * pwr;
                    energy /= deltaTime.TotalSeconds;

                    if (!double.IsNaN(energy))
                        _packagePower.Value = (float)energy;
                }

                // CUR_TEMP [31:21]
                // TJ_SEL[17:16], RANGE_SEL[19] signal the 49°C adjustment
                bool tempOffsetFlag = (temperature & F17H_TEMP_RANGE_SEL_MASK) != 0
                                      || (temperature & F17H_TEMP_TJ_SEL_MASK) == F17H_TEMP_TJ_SEL_MASK;
                temperature = (temperature >> 21) * 125;

                float t = temperature * 0.001f;
                if (tempOffsetFlag)
                    t += -49.0f;

                _coreTemperatureTctlTdie.Value = t;
                _cpu.ActivateSensor(_coreTemperatureTctlTdie);

                for (uint i = 0; i < _ccdTemperatures.Length; i++)
                {
                    uint ccd1Offset = F17H_M61H_CCD1_TEMP + i * 0x4;
                    uint ccdRawTemp = _cpu._pawnModule.ReadSmn(ccd1Offset);

                    ccdRawTemp &= 0xFFF;
                    float ccdTemp = ((ccdRawTemp * 125) - 305000) * 0.001f;
                    if (ccdRawTemp > 0 && ccdTemp < 125)
                    {
                        if (_ccdTemperatures[i] == null)
                        {
                            _cpu.ActivateSensor(_ccdTemperatures[i] = new Sensor($"CCD{i + 1} (Tdie)",
                                                                                 _cpu._sensorTypeIndex[SensorType.Temperature]++,
                                                                                 SensorType.Temperature,
                                                                                 _cpu,
                                                                                 _cpu._settings));
                        }

                        _ccdTemperatures[i].Value = ccdTemp;
                    }
                }

                Sensor[] activeCcds = _ccdTemperatures.Where(x => x != null).ToArray();
                if (activeCcds.Length > 1)
                {
                    if (_ccdsMaxTemperature == null)
                    {
                        _cpu.ActivateSensor(_ccdsMaxTemperature = new Sensor("CCDs Max (Tdie)",
                                                                             _cpu._sensorTypeIndex[SensorType.Temperature]++,
                                                                             SensorType.Temperature,
                                                                             _cpu,
                                                                             _cpu._settings));
                    }

                    if (_ccdsAverageTemperature == null)
                    {
                        _cpu.ActivateSensor(_ccdsAverageTemperature = new Sensor("CCDs Average (Tdie)",
                                                                                 _cpu._sensorTypeIndex[SensorType.Temperature]++,
                                                                                 SensorType.Temperature,
                                                                                 _cpu,
                                                                                 _cpu._settings));
                    }

                    _ccdsMaxTemperature.Value = activeCcds.Max(x => x.Value);
                    _ccdsAverageTemperature.Value = activeCcds.Average(x => x.Value);
                }

                Mutexes.ReleasePciBus();
            }

            // MSRC001_0064
            double timeStampCounterMultiplier = GetTimeStampCounterMultiplier();
            if (timeStampCounterMultiplier > 0)
            {
                _busClock.Value = (float)(_cpu.TimeStampCounterFrequency / timeStampCounterMultiplier);
                _cpu.ActivateSensor(_busClock);
            }

            _pmTable = _cpu._smu.GetPmTable();

            if (_pmTable.Length > 0)
            {
                foreach (KeyValuePair<KeyValuePair<uint, RyzenSMU.SmuSensorType>, Sensor> sensor in _smuSensors)
                {
                    if (_pmTable.Length > sensor.Key.Key)
                    {
                        sensor.Value.Value = _pmTable[sensor.Key.Key] * sensor.Key.Value.Scale;
                        _cpu.ActivateSensor(sensor.Value);
                    }
                }
            }
        }

        public void UpdateVirtualSensor()
        {
            if (Nodes == null || Nodes.Count == 0)
                return;

            double clock = Nodes.Average(x => x.CoreClock);
            _avgClock.Value = (float)Math.Round(clock, 0);

            clock = Nodes.Average(x => x.EffectiveClock);
            _avgClockEffcetive.Value = (float)Math.Round(clock, 0);

            double voltage = Nodes.Average(x => x.CoreVoltage);
            if (voltage > 0)
            {
                _coreVids.Value = (float)Math.Round(voltage, 4);
                _cpu.ActivateSensor(_coreVids);
            }
        }

        private double GetTimeStampCounterMultiplier()
        {
            _cpu._pawnModule.ReadMsr(MSR_PSTATE_0, out uint eax, out _);

            // CoreCOF = CpuFid[11:0] * 5MHz
            uint cpuFid = eax & 0xfff;
            return (cpuFid * 5) / 100.0;
        }

        public void AppendThread(CpuId thread, int numaId, int coreId)
        {
            NumaNode node = null;
            foreach (NumaNode n in Nodes)
            {
                if (n.NodeId == numaId)
                {
                    node = n;
                    break;
                }
            }

            if (node == null)
            {
                node = new NumaNode(_cpu, numaId);
                Nodes.Add(node);
            }

            if (thread != null)
                node.AppendThread(thread, coreId);
        }

        public struct PerCoreBaseIndices
        {
            public int Frequency;
            public int Voltage;
            public int Temperature;
            public int Power;
        }

        private static PerCoreBaseIndices GetPerCoreBaseIndices(uint pmTableVersion)
        {
            return pmTableVersion switch
            {
                _ => new PerCoreBaseIndices { Frequency = 349, Voltage = 317, Temperature = 333, Power = 301 }
            };
        }
    }

    private class NumaNode
    {
        private readonly Amd1ACpu _cpu;

        public NumaNode(Amd1ACpu cpu, int id)
        {
            Cores = new List<Core>();
            NodeId = id;
            _cpu = cpu;
        }

        public List<Core> Cores { get; }

        public int NodeId { get; }

        public double CoreClock
        {
            get
            {
                if (Cores == null)
                    return 0;

                return Cores.Average(x => x.CoreClock);
            }
        }

        public double CoreVoltage
        {
            get
            {
                if (Cores == null)
                    return 0;

                return Cores.Average(x => x.CoreVoltage);
            }
        }

        public double EffectiveClock
        {
            get
            {
                if (Cores == null)
                    return 0;

                return Cores.Average(x => x.EffectiveClock);
            }
        }

        public void AppendThread(CpuId thread, int coreId)
        {
            Core core = null;
            foreach (Core c in Cores)
            {
                if (c.CoreId == coreId)
                    core = c;
            }

            if (core == null)
            {
                core = new Core(_cpu, coreId);
                Cores.Add(core);
            }

            if (thread != null)
                core.AppedThread(thread);
        }

        public static void UpdateSensors()
        { }
    }

    private class CpuThread
    {
        private DateTime _sampleTime = new(0);
        private DateTime _lastSampleTime = new(0);
        private ulong _mperf = 0;
        private ulong _aperf = 0;
        private ulong _mperfLast = 0;
        private ulong _aperfLast = 0;
        private ulong _mperfDelta = 0;
        private ulong _aperfDelta = 0;

        private CpuId _cpuId;
        private Amd1ACpu _cpu;
        public CpuId Cpu { get { return _cpuId; } }

        public TimeSpan SampleDuration { get; private set; } = TimeSpan.Zero;
        public double EffectiveClock { get; private set; } = 0;

        public CpuThread(Amd1ACpu cpu, CpuId cpuId)
        {
            _cpu = cpu;
            _cpuId = cpuId;
        }

        public void ReadPerformanceCounter()
        {
            ThreadAffinity.Set(Cpu.Affinity);

            _sampleTime = DateTime.UtcNow;

            _cpu._pawnModule.ReadMsr(MSR_MPERF_RO, out ulong edxeax);
            _mperf = edxeax;
            _cpu._pawnModule.ReadMsr(MSR_APERF_RO, out edxeax);
            _aperf = edxeax;
        }

        public void UpdateMeasurements()
        {
            if (_mperf < _mperfLast || _aperf < _aperfLast)
            {
                _lastSampleTime = new(0);
            }

            if (_lastSampleTime.Ticks == 0)
            {
                _lastSampleTime = _sampleTime;
                _mperfLast = _mperf;
                _aperfLast = _aperf;

                _mperfDelta = 0;
                _aperfDelta = 0;
                return;
            }

            SampleDuration = _sampleTime - _lastSampleTime;
            _lastSampleTime = _sampleTime;

            _mperfDelta = _mperf - _mperfLast;
            _aperfDelta = _aperf - _aperfLast;
            _mperfLast = _mperf;
            _aperfLast = _aperf;

            if (_mperfDelta > 20000e6)
                _mperfDelta = 0;
            if (_aperfDelta > 20000e6)
                _aperfDelta = 0;

            if (_aperfDelta == 0 || _mperfDelta == 0)
            {
                _lastSampleTime = new(0);
                return;
            }

            double freq = (double)_aperfDelta / (SampleDuration.TotalMilliseconds * 1000.0);
            EffectiveClock = Math.Round(freq);
        }

    }

    private class Core
    {
        private readonly Sensor _clock;
        private readonly Sensor _clockEffective;
        private readonly Amd1ACpu _cpu;
        private readonly Sensor _multiplier;
        private readonly Sensor _power;
        private readonly Sensor _temperature;
        private readonly Sensor _vcore;
        private float _pmTableVoltage;

        public double CoreClock { get; set; } = 0;
        public double EffectiveClock { get; set; } = 0;
        public double CoreVoltage => _pmTableVoltage;

        public Core(Amd1ACpu cpu, int id)
        {
            _cpu = cpu;
            CoreId = id;
            _clock = new Sensor("Core #" + CoreId, _cpu._sensorTypeIndex[SensorType.Clock]++, SensorType.Clock, cpu, cpu._settings);
            _clockEffective = new Sensor("Core #" + CoreId + " (Effective)", _cpu._sensorTypeIndex[SensorType.Clock]++, SensorType.Clock, cpu, cpu._settings);
            _temperature = new Sensor("Core #" + CoreId, cpu._sensorTypeIndex[SensorType.Temperature]++, SensorType.Temperature, cpu, cpu._settings);
            _multiplier = new Sensor("Core #" + CoreId, cpu._sensorTypeIndex[SensorType.Factor]++, SensorType.Factor, cpu, cpu._settings);
            _power = new Sensor("Core #" + CoreId + " (SMU)", cpu._sensorTypeIndex[SensorType.Power]++, SensorType.Power, cpu, cpu._settings);
            _vcore = new Sensor("Core #" + CoreId + " VID", cpu._sensorTypeIndex[SensorType.Voltage]++, SensorType.Voltage, cpu, cpu._settings);

            cpu.ActivateSensor(_clock);
            cpu.ActivateSensor(_clockEffective);
            cpu.ActivateSensor(_temperature);
            cpu.ActivateSensor(_multiplier);
            cpu.ActivateSensor(_power);
            cpu.ActivateSensor(_vcore);
        }

        public int CoreId { get; }

        public List<CpuThread> Threads { get; } = new List<CpuThread>();

        public void AppedThread(CpuId cpuId)
        {
            CpuThread t = new CpuThread(_cpu, cpuId);
            Threads.Add(t);
        }

        public void UpdateSensors()
        {
            if (Threads.Count == 0)
                return;

            CpuThread thread = Threads[0];
            GroupAffinity previousAffinity = ThreadAffinity.Set(thread.Cpu.Affinity);

            // MSRC000_00E7, MSRC000_00E8
            foreach (var t in Threads)
            {
                t.ReadPerformanceCounter();
            }

            ThreadAffinity.Set(previousAffinity);

            Threads.ForEach(t => t.UpdateMeasurements());
            EffectiveClock = Threads.Average(x => x.EffectiveClock);
            _clockEffective.Value = (float)EffectiveClock;

            float[] pmTable = _cpu._processor.PmTable;
            var baseIndices = _cpu._processor.BaseIndices;

            if (pmTable != null && pmTable.Length > 0)
            {
                int pmTableCoreIdx = CoreId - 1;

                if (baseIndices.Frequency > 0)
                {
                    int idx = baseIndices.Frequency + pmTableCoreIdx;
                    if (idx < pmTable.Length)
                    {
                        float freqMHz = pmTable[idx] * 1000.0f;
                        if (freqMHz > 100.0f)
                        {
                            CoreClock = Math.Round(freqMHz);
                            _clock.Value = (float)CoreClock;
                            _multiplier.Value = (float)(CoreClock / _cpu._processor.BusClockValue);
                        }
                    }
                }

                if (baseIndices.Voltage > 0)
                {
                    int idx = baseIndices.Voltage + pmTableCoreIdx;
                    if (idx < pmTable.Length)
                    {
                        _pmTableVoltage = pmTable[idx];
                    }
                }

                if (baseIndices.Temperature > 0)
                {
                    int idx = baseIndices.Temperature + pmTableCoreIdx;
                    if (idx < pmTable.Length)
                    {
                        _temperature.Value = pmTable[idx];
                    }
                }

                if (baseIndices.Power > 0)
                {
                    int idx = baseIndices.Power + pmTableCoreIdx;
                    if (idx < pmTable.Length)
                    {
                        float pmPower = pmTable[idx];
                        if (pmPower > 0)
                        {
                            _power.Value = pmPower;
                        }
                    }
                }
            }

            if (_pmTableVoltage > 0.1f && _pmTableVoltage < 3.0f)
            {
                _vcore.Value = _pmTableVoltage;
            }
        }
    }

    private const uint F17H_M01H_THM_TCON_CUR_TMP = 0x00059800;
    private const uint F17H_M61H_CCD1_TEMP = 0x00059b08;
    private const uint F17H_TEMP_RANGE_SEL_MASK = 0x80000;
    private const uint F17H_TEMP_TJ_SEL_MASK = 0x30000;
    private const uint MSR_PKG_ENERGY_STAT = 0xC001029B;
    private const uint MSR_PSTATE_0 = 0xC0010064;
    private const uint MSR_PWR_UNIT = 0xC0010299;
    private const uint MSR_MPERF_RO = 0xC000_00E7;
    private const uint MSR_APERF_RO = 0xC000_00E8;
}
