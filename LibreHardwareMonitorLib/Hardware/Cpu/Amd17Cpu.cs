// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibreHardwareMonitor.Hardware.Cpu;

internal sealed class Amd17Cpu : AmdCpu
{
    private readonly Processor _processor;
    private readonly Dictionary<SensorType, int> _sensorTypeIndex;
    private readonly RyzenSMU _smu;

    public Amd17Cpu(int processorIndex, CpuId[][] cpuId, ISettings settings) : base(processorIndex, cpuId, settings)
    {
        _sensorTypeIndex = new Dictionary<SensorType, int>();
        foreach (SensorType type in Enum.GetValues(typeof(SensorType)))
        {
            _sensorTypeIndex.Add(type, 0);
        }

        _sensorTypeIndex[SensorType.Load] = _active.Count(x => x.SensorType == SensorType.Load);

        _smu = new RyzenSMU(_family, _model, _packageType);

        // Add all numa nodes.
        // Register ..1E_2, [10:8] + 1
        _processor = new Processor(this);

        // Add all numa nodes.
        int coreId = 0;
        int lastCoreId = -1; // Invalid id.

        // Ryzen 3000's skip some core ids.
        // So start at 1 and count upwards when the read core changes.
        foreach (CpuId[] cpu in cpuId.OrderBy(x => x[0].ExtData[0x1e, 1] & 0xFF))
        {
            CpuId thread = cpu[0];

            // CPUID_Fn8000001E_EBX, Register ..1E_1, [7:0]
            // threads per core =  CPUID_Fn8000001E_EBX[15:8] + 1
            // CoreId: core ID =  CPUID_Fn8000001E_EBX[7:0]
            int coreIdRead = (int)(thread.ExtData[0x1e, 1] & 0xff);

            // CPUID_Fn8000001E_ECX, Node Identifiers, Register ..1E_2
            // NodesPerProcessor =  CPUID_Fn8000001E_ECX[10:8]
            // nodeID =  CPUID_Fn8000001E_ECX[7:0]
            int nodeId = (int)(thread.ExtData[0x1e, 2] & 0xff);

            if (coreIdRead != lastCoreId)
            {
                coreId++;
            }

            lastCoreId = coreIdRead;

            _processor.AppendThread(thread, nodeId, coreId);
        }

        Update();
    }

    protected override uint[] GetMsrs()
    {
        return new[] { PERF_CTL_0, PERF_CTR_0, HWCR, MSR_PSTATE_0, COFVID_STATUS };
    }

    public override string GetReport()
    {
        StringBuilder r = new();
        r.Append(base.GetReport());
        r.Append(_smu.GetReport());
        return r.ToString();
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
    }

    private class Processor
    {
        private readonly Sensor _busClock;
        private readonly Sensor[] _ccdTemperatures;
        private readonly Sensor _coreTemperatureTctl;
        private readonly Sensor _coreTemperatureTctlTdie;
        private readonly Sensor _coreTemperatureTdie;
        private readonly Sensor _coreVoltage;
        private readonly Amd17Cpu _cpu;
        private readonly Sensor _packagePower;
        private readonly Dictionary<KeyValuePair<uint, RyzenSMU.SmuSensorType>, Sensor> _smuSensors = new();
        private readonly Sensor _socVoltage;

        private Sensor _ccdsAverageTemperature;
        private Sensor _ccdsMaxTemperature;
        private DateTime _lastPwrTime = new(0);
        private uint _lastPwrValue;

        public Processor(Hardware hardware)
        {
            _cpu = (Amd17Cpu)hardware;

            _packagePower = new Sensor("Package", _cpu._sensorTypeIndex[SensorType.Power]++, SensorType.Power, _cpu, _cpu._settings);
            _coreTemperatureTctl = new Sensor("Core (Tctl)", _cpu._sensorTypeIndex[SensorType.Temperature]++, SensorType.Temperature, _cpu, _cpu._settings);
            _coreTemperatureTdie = new Sensor("Core (Tdie)", _cpu._sensorTypeIndex[SensorType.Temperature]++, SensorType.Temperature, _cpu, _cpu._settings);
            _coreTemperatureTctlTdie = new Sensor("Core (Tctl/Tdie)", _cpu._sensorTypeIndex[SensorType.Temperature]++, SensorType.Temperature, _cpu, _cpu._settings);
            _ccdTemperatures = new Sensor[8]; // Hardcoded until there's a way to get max CCDs.
            _coreVoltage = new Sensor("Core (SVI2 TFN)", _cpu._sensorTypeIndex[SensorType.Voltage]++, SensorType.Voltage, _cpu, _cpu._settings);
            _socVoltage = new Sensor("SoC (SVI2 TFN)", _cpu._sensorTypeIndex[SensorType.Voltage]++, SensorType.Voltage, _cpu, _cpu._settings);
            _busClock = new Sensor("Bus Speed", _cpu._sensorTypeIndex[SensorType.Clock]++, SensorType.Clock, _cpu, _cpu._settings);

            _cpu.ActivateSensor(_packagePower);

            foreach (KeyValuePair<uint, RyzenSMU.SmuSensorType> sensor in _cpu._smu.GetPmTableStructure())
            {
                _smuSensors.Add(sensor, new Sensor(sensor.Value.Name, _cpu._sensorTypeIndex[sensor.Value.Type]++, sensor.Value.Type, _cpu, _cpu._settings));
            }
        }

        public List<NumaNode> Nodes { get; } = new();

        public void UpdateSensors()
        {
            NumaNode node = Nodes[0];
            Core core = node?.Cores[0];
            CpuId cpuId = core?.Threads[0];

            if (cpuId == null)
                return;

            GroupAffinity previousAffinity = ThreadAffinity.Set(cpuId.Affinity);

            // MSRC001_0299
            // TU [19:16]
            // ESU [12:8] -> Unit 15.3 micro Joule per increment
            // PU [3:0]
            Ring0.ReadMsr(MSR_PWR_UNIT, out uint _, out uint _);

            // MSRC001_029B
            // total_energy [31:0]
            DateTime sampleTime = DateTime.Now;
            Ring0.ReadMsr(MSR_PKG_ENERGY_STAT, out uint eax, out _);

            uint totalEnergy = eax;

            uint smuSvi0Tfn = 0;
            uint smuSvi0TelPlane0 = 0;
            uint smuSvi0TelPlane1 = 0;

            if (Mutexes.WaitPciBus(10))
            {
                // THM_TCON_CUR_TMP
                // CUR_TEMP [31:21]
                Ring0.WritePciConfig(0x00, FAMILY_17H_PCI_CONTROL_REGISTER, F17H_M01H_THM_TCON_CUR_TMP);
                Ring0.ReadPciConfig(0x00, FAMILY_17H_PCI_CONTROL_REGISTER + 4, out uint temperature);

                // SVI0_TFN_PLANE0 [0]
                // SVI0_TFN_PLANE1 [1]
                Ring0.WritePciConfig(0x00, FAMILY_17H_PCI_CONTROL_REGISTER, F17H_M01H_SVI + 0x8);
                Ring0.ReadPciConfig(0x00, FAMILY_17H_PCI_CONTROL_REGISTER + 4, out smuSvi0Tfn);

                bool supportsPerCcdTemperatures = false;

                // TODO: find a better way because these will probably keep changing in the future.

                uint sviPlane0Offset;
                uint sviPlane1Offset;
                switch (cpuId.Model)
                {
                    case 0x31: // Threadripper 3000.
                        sviPlane0Offset = F17H_M01H_SVI + 0x14;
                        sviPlane1Offset = F17H_M01H_SVI + 0x10;
                        supportsPerCcdTemperatures = true;
                        break;

                    case 0x71: // Zen 2.
                    case 0x21: // Zen 3.
                        sviPlane0Offset = F17H_M01H_SVI + 0x10;
                        sviPlane1Offset = F17H_M01H_SVI + 0xC;
                        supportsPerCcdTemperatures = true;
                        break;

                    case 0x61:
                        sviPlane0Offset = F17H_M01H_SVI + 0x10;
                        sviPlane1Offset = F17H_M01H_SVI + 0xC;
                        supportsPerCcdTemperatures = true;
                        break;

                    default: // Zen and Zen+.
                        sviPlane0Offset = F17H_M01H_SVI + 0xC;
                        sviPlane1Offset = F17H_M01H_SVI + 0x10;
                        break;
                }

                // SVI0_PLANE0_VDDCOR [24:16]
                // SVI0_PLANE0_IDDCOR [7:0]
                Ring0.WritePciConfig(0x00, FAMILY_17H_PCI_CONTROL_REGISTER, sviPlane0Offset);
                Ring0.ReadPciConfig(0x00, FAMILY_17H_PCI_CONTROL_REGISTER + 4, out smuSvi0TelPlane0);

                // SVI0_PLANE1_VDDCOR [24:16]
                // SVI0_PLANE1_IDDCOR [7:0]
                Ring0.WritePciConfig(0x00, FAMILY_17H_PCI_CONTROL_REGISTER, sviPlane1Offset);
                Ring0.ReadPciConfig(0x00, FAMILY_17H_PCI_CONTROL_REGISTER + 4, out smuSvi0TelPlane1);

                ThreadAffinity.Set(previousAffinity);

                // power consumption
                // power.Value = (float) ((double)pu * 0.125);
                // esu = 15.3 micro Joule per increment
                if (_lastPwrTime.Ticks == 0)
                {
                    _lastPwrTime = sampleTime;
                    _lastPwrValue = totalEnergy;
                }

                // ticks diff
                TimeSpan time = sampleTime - _lastPwrTime;
                long pwr;
                if (_lastPwrValue <= totalEnergy)
                    pwr = totalEnergy - _lastPwrValue;
                else
                    pwr = (0xffffffff - _lastPwrValue) + totalEnergy;

                // update for next sample
                _lastPwrTime = sampleTime;
                _lastPwrValue = totalEnergy;

                double energy = 15.3e-6 * pwr;
                energy /= time.TotalSeconds;

                if (!double.IsNaN(energy))
                    _packagePower.Value = (float)energy;

                // current temp Bit [31:21]
                // If bit 19 of the Temperature Control register is set, there is an additional offset of 49 degrees C.
                bool tempOffsetFlag = (temperature & F17H_TEMP_OFFSET_FLAG) != 0;
                temperature = (temperature >> 21) * 125;

                float offset = 0.0f;

                // Offset table: https://github.com/torvalds/linux/blob/master/drivers/hwmon/k10temp.c#L78
                if (string.IsNullOrWhiteSpace(cpuId.Name))
                    offset = 0;
                else if (cpuId.Name.Contains("1600X") || cpuId.Name.Contains("1700X") || cpuId.Name.Contains("1800X"))
                    offset = -20.0f;
                else if (cpuId.Name.Contains("Threadripper 19") || cpuId.Name.Contains("Threadripper 29"))
                    offset = -27.0f;
                else if (cpuId.Name.Contains("2700X"))
                    offset = -10.0f;

                float t = temperature * 0.001f;
                if (tempOffsetFlag)
                    t += -49.0f;

                if (offset < 0)
                {
                    _coreTemperatureTctl.Value = t;
                    _coreTemperatureTdie.Value = t + offset;

                    _cpu.ActivateSensor(_coreTemperatureTctl);
                    _cpu.ActivateSensor(_coreTemperatureTdie);
                }
                else
                {
                    // Zen 2 doesn't have an offset so Tdie and Tctl are the same.
                    _coreTemperatureTctlTdie.Value = t;
                    _cpu.ActivateSensor(_coreTemperatureTctlTdie);
                }

                // Tested only on R5 3600 & Threadripper 3960X, 5900X, 7900X
                if (supportsPerCcdTemperatures)
                {
                    for (uint i = 0; i < _ccdTemperatures.Length; i++)
                    {
                        if (cpuId.Model == 0x61)
                            Ring0.WritePciConfig(0x00, FAMILY_17H_PCI_CONTROL_REGISTER, F17H_M61H_CCD1_TEMP + (i * 0x4));
                        else
                            Ring0.WritePciConfig(0x00, FAMILY_17H_PCI_CONTROL_REGISTER, F17H_M70H_CCD1_TEMP + (i * 0x4));
                        Ring0.ReadPciConfig(0x00, FAMILY_17H_PCI_CONTROL_REGISTER + 4, out uint ccdRawTemp);

                        ccdRawTemp &= 0xFFF;
                        float ccdTemp = ((ccdRawTemp * 125) - 305000) * 0.001f;
                        if (ccdRawTemp > 0 && ccdTemp < 125) // Zen 2 reports 95 degrees C max, but it might exceed that.
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
                        // No need to get the max / average ccds temp if there is only one CCD.

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
                }

                Mutexes.ReleasePciBus();
            }

            // voltage
            const double vidStep = 0.00625;
            double vcc;
            uint svi0PlaneXVddCor;

            if (cpuId.Model is 0x61) // Readout not working for Ryzen 7000.
                smuSvi0Tfn |= 0x01 | 0x02;

            // Core (0x01).
            if ((smuSvi0Tfn & 0x01) == 0)
            {
                svi0PlaneXVddCor = (smuSvi0TelPlane0 >> 16) & 0xff;
                vcc = 1.550 - (vidStep * svi0PlaneXVddCor);
                _coreVoltage.Value = (float)vcc;

                _cpu.ActivateSensor(_coreVoltage);
            }

            // SoC (0x02), not every Zen cpu has this voltage.
            if (cpuId.Model is 0x11 or 0x21 or 0x71 or 0x31 || (smuSvi0Tfn & 0x02) == 0)
            {
                svi0PlaneXVddCor = (smuSvi0TelPlane1 >> 16) & 0xff;
                vcc = 1.550 - (vidStep * svi0PlaneXVddCor);
                _socVoltage.Value = (float)vcc;

                _cpu.ActivateSensor(_socVoltage);
            }

            double timeStampCounterMultiplier = GetTimeStampCounterMultiplier();
            if (timeStampCounterMultiplier > 0)
            {
                _busClock.Value = (float)(_cpu.TimeStampCounterFrequency / timeStampCounterMultiplier);
                _cpu.ActivateSensor(_busClock);
            }

            if (_cpu._smu.IsPmTableLayoutDefined())
            {
                float[] smuData = _cpu._smu.GetPmTable();

                foreach (KeyValuePair<KeyValuePair<uint, RyzenSMU.SmuSensorType>, Sensor> sensor in _smuSensors)
                {
                    if (smuData.Length > sensor.Key.Key)
                    {
                        sensor.Value.Value = smuData[sensor.Key.Key] * sensor.Key.Value.Scale;
                        if (sensor.Value.Value != 0)
                            _cpu.ActivateSensor(sensor.Value);
                    }
                }
            }
        }

        private double GetTimeStampCounterMultiplier()
        {
            Ring0.ReadMsr(MSR_PSTATE_0, out uint eax, out _);
            uint cpuDfsId = (eax >> 8) & 0x3f;
            uint cpuFid = eax & 0xff;
            return 2.0 * cpuFid / cpuDfsId;
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
    }

    private class NumaNode
    {
        private readonly Amd17Cpu _cpu;

        public NumaNode(Amd17Cpu cpu, int id)
        {
            Cores = new List<Core>();
            NodeId = id;
            _cpu = cpu;
        }

        public List<Core> Cores { get; }

        public int NodeId { get; }

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
                core.Threads.Add(thread);
        }

        public static void UpdateSensors()
        { }
    }

    private class Core
    {
        private readonly Sensor _clock;
        private readonly Amd17Cpu _cpu;
        private readonly Sensor _multiplier;
        private readonly Sensor _power;
        private readonly Sensor _vcore;
        private ISensor _busSpeed;
        private DateTime _lastPwrTime = new(0);
        private uint _lastPwrValue;

        public Core(Amd17Cpu cpu, int id)
        {
            _cpu = cpu;
            Threads = new List<CpuId>();
            CoreId = id;
            _clock = new Sensor("Core #" + CoreId, _cpu._sensorTypeIndex[SensorType.Clock]++, SensorType.Clock, cpu, cpu._settings);
            _multiplier = new Sensor("Core #" + CoreId, cpu._sensorTypeIndex[SensorType.Factor]++, SensorType.Factor, cpu, cpu._settings);
            _power = new Sensor("Core #" + CoreId + " (SMU)", cpu._sensorTypeIndex[SensorType.Power]++, SensorType.Power, cpu, cpu._settings);
            _vcore = new Sensor("Core #" + CoreId + " VID", cpu._sensorTypeIndex[SensorType.Voltage]++, SensorType.Voltage, cpu, cpu._settings);

            cpu.ActivateSensor(_clock);
            cpu.ActivateSensor(_multiplier);
            cpu.ActivateSensor(_power);
            cpu.ActivateSensor(_vcore);
        }

        public int CoreId { get; }

        public List<CpuId> Threads { get; }

        public void UpdateSensors()
        {
            // CPUID cpu = threads.FirstOrDefault();
            CpuId cpu = Threads[0];
            if (cpu == null)
                return;

            GroupAffinity previousAffinity = ThreadAffinity.Set(cpu.Affinity);

            // MSRC001_0299
            // TU [19:16]
            // ESU [12:8] -> Unit 15.3 micro Joule per increment
            // PU [3:0]
            Ring0.ReadMsr(MSR_PWR_UNIT, out _, out _);

            // MSRC001_029A
            // total_energy [31:0]
            DateTime sampleTime = DateTime.Now;
            Ring0.ReadMsr(MSR_CORE_ENERGY_STAT, out uint eax, out _);
            uint totalEnergy = eax;

            // MSRC001_0293
            // CurHwPstate [24:22]
            // CurCpuVid [21:14]
            // CurCpuDfsId [13:8]
            // CurCpuFid [7:0]
            Ring0.ReadMsr(MSR_HARDWARE_PSTATE_STATUS, out eax, out _);
            int curCpuVid = (int)((eax >> 14) & 0xff);
            int curCpuDfsId = (int)((eax >> 8) & 0x3f);
            int curCpuFid = (int)(eax & 0xff);

            // MSRC001_0064 + x
            // IddDiv [31:30]
            // IddValue [29:22]
            // CpuVid [21:14]
            // CpuDfsId [13:8]
            // CpuFid [7:0]
            // Ring0.ReadMsr(MSR_PSTATE_0 + (uint)CurHwPstate, out eax, out edx);
            // int IddDiv = (int)((eax >> 30) & 0x03);
            // int IddValue = (int)((eax >> 22) & 0xff);
            // int CpuVid = (int)((eax >> 14) & 0xff);
            ThreadAffinity.Set(previousAffinity);

            // clock
            // CoreCOF is (Core::X86::Msr::PStateDef[CpuFid[7:0]] / Core::X86::Msr::PStateDef[CpuDfsId]) * 200
            double clock = 200.0;
            _busSpeed ??= _cpu.Sensors.FirstOrDefault(x => x.Name == "Bus Speed");
            if (_busSpeed?.Value.HasValue == true && _busSpeed.Value > 0)
                clock = (double)(_busSpeed.Value * 2);

            _clock.Value = (float)(curCpuFid / (double)curCpuDfsId * clock);

            // multiplier
            _multiplier.Value = (float)(curCpuFid / (double)curCpuDfsId * 2.0);

            // Voltage
            const double vidStep = 0.00625;
            double vcc = 1.550 - (vidStep * curCpuVid);
            _vcore.Value = (float)vcc;

            // power consumption
            // power.Value = (float) ((double)pu * 0.125);
            // esu = 15.3 micro Joule per increment
            if (_lastPwrTime.Ticks == 0)
            {
                _lastPwrTime = sampleTime;
                _lastPwrValue = totalEnergy;
            }

            // ticks diff
            TimeSpan time = sampleTime - _lastPwrTime;
            long pwr;
            if (_lastPwrValue <= totalEnergy)
                pwr = totalEnergy - _lastPwrValue;
            else
                pwr = (0xffffffff - _lastPwrValue) + totalEnergy;

            // update for next sample
            _lastPwrTime = sampleTime;
            _lastPwrValue = totalEnergy;

            double energy = 15.3e-6 * pwr;
            energy /= time.TotalSeconds;

            if (!double.IsNaN(energy))
                _power.Value = (float)energy;
        }
    }

    // ReSharper disable InconsistentNaming
    private const uint COFVID_STATUS = 0xC0010071;
    private const uint F17H_M01H_SVI = 0x0005A000;
    private const uint F17H_M01H_THM_TCON_CUR_TMP = 0x00059800;
    private const uint F17H_M70H_CCD1_TEMP = 0x00059954;
    private const uint F17H_M61H_CCD1_TEMP = 0x00059b08;
    private const uint F17H_TEMP_OFFSET_FLAG = 0x80000;
    private const uint FAMILY_17H_PCI_CONTROL_REGISTER = 0x60;
    private const uint HWCR = 0xC0010015;
    private const uint MSR_CORE_ENERGY_STAT = 0xC001029A;
    private const uint MSR_HARDWARE_PSTATE_STATUS = 0xC0010293;
    private const uint MSR_PKG_ENERGY_STAT = 0xC001029B;
    private const uint MSR_PSTATE_0 = 0xC0010064;
    private const uint MSR_PWR_UNIT = 0xC0010299;
    private const uint PERF_CTL_0 = 0xC0010000;
    private const uint PERF_CTR_0 = 0xC0010004;
    // ReSharper restore InconsistentNaming
}
