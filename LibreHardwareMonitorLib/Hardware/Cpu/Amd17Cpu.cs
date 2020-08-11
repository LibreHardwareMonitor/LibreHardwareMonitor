// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibreHardwareMonitor.Hardware.CPU
{
    internal sealed class Amd17Cpu : AmdCpu
    {
        private readonly Processor _cpu;
        private int _sensorClock;
        private int _sensorMulti;
        private int _sensorPower;

        // counter, to create sensor index values
        private int _sensorTemperatures;
        private int _sensorVoltage;

        public Amd17Cpu(int processorIndex, CpuId[][] cpuId, ISettings settings) : base(processorIndex, cpuId, settings)
        {
            // add all numa nodes
            // Register ..1E_2, [10:8] + 1
            _cpu = new Processor(this);

            // add all numa nodes
            const int initialCoreId = 1_000_000_000;

            int coreId = 1;
            int lastCoreId = initialCoreId;

            // Ryzen 3000's skip some core ids.
            // So start at 1 and count upwards when the read core changes.
            foreach (CpuId[] cpu in cpuId.OrderBy(x => x[0].ExtData[0x1e, 1] & 0xFF))
            {
                CpuId thread = cpu[0];

                // coreID
                // Register ..1E_1, [7:0]
                int coreIdRead = (int)(thread.ExtData[0x1e, 1] & 0xff);

                // nodeID
                // Register ..1E_2, [7:0]
                int nodeId = (int)(thread.ExtData[0x1e, 2] & 0xff);

                _cpu.AppendThread(thread, nodeId, coreId);

                if (lastCoreId != initialCoreId && coreIdRead != lastCoreId)
                {
                    coreId++;
                }

                lastCoreId = coreIdRead;
            }

            Update();
        }

        protected override uint[] GetMsrs()
        {
            return new[] { PERF_CTL_0, PERF_CTR_0, HWCR, MSR_PSTATE_0, COFVID_STATUS };
        }

        public override string GetReport()
        {
            StringBuilder r = new StringBuilder();
            r.Append(base.GetReport());
            r.Append("Ryzen");
            return r.ToString();
        }

        public override void Update()
        {
            base.Update();

            _cpu.UpdateSensors();
            foreach (NumaNode node in _cpu.Nodes)
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
            private readonly Sensor[] _ccdTemperatures;
            private readonly Sensor _coreTemperatureTctl;
            private readonly Sensor _coreTemperatureTctlTdie;
            private readonly Sensor _coreTemperatureTdie;
            private readonly Sensor _coreVoltage;
            private readonly Amd17Cpu _hardware;
            private readonly Sensor _packagePower;
            private readonly Sensor _socVoltage;
            private Sensor _ccdsAverageTemperature;
            private Sensor _ccdsMaxTemperature;
            private DateTime _lastPwrTime = new DateTime(0);
            private uint _lastPwrValue;
            private Sensor _busClock;

            public Processor(Hardware hardware)
            {
                _hardware = (Amd17Cpu)hardware;
                Nodes = new List<NumaNode>();

                _packagePower = new Sensor("Package Power", _hardware._sensorPower++, SensorType.Power, _hardware, _hardware._settings);
                _coreTemperatureTctl = new Sensor("Core (Tctl)", _hardware._sensorTemperatures++, SensorType.Temperature, _hardware, _hardware._settings);
                _coreTemperatureTdie = new Sensor("Core (Tdie)", _hardware._sensorTemperatures++, SensorType.Temperature, _hardware, _hardware._settings);
                _coreTemperatureTctlTdie = new Sensor("Core (Tctl/Tdie)", _hardware._sensorTemperatures++, SensorType.Temperature, _hardware, _hardware._settings);
                _ccdTemperatures = new Sensor[8]; // Hardcoded until there's a way to get max CCDs.
                _coreVoltage = new Sensor("Core (SVI2 TFN)", _hardware._sensorVoltage++, SensorType.Voltage, _hardware, _hardware._settings);
                _socVoltage = new Sensor("SoC (SVI2 TFN)", _hardware._sensorVoltage++, SensorType.Voltage, _hardware, _hardware._settings);
                _busClock = new Sensor("Bus Speed", _hardware._sensorClock++, SensorType.Clock, _hardware, _hardware._settings);

                _hardware.ActivateSensor(_packagePower);
            }

            public List<NumaNode> Nodes { get; }

            public void UpdateSensors()
            {
                NumaNode node = Nodes[0];
                Core core = node?.Cores[0];
                CpuId cpu = core?.Threads[0];
                if (cpu == null)
                    return;


                GroupAffinity previousAffinity = ThreadAffinity.Set(cpu.Affinity);

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

                if (Ring0.WaitPciBusMutex(10))
                {
                    // THM_TCON_CUR_TMP
                    // CUR_TEMP [31:21]
                    Ring0.WritePciConfig(0x00, FAMILY_17H_PCI_CONTROL_REGISTER, F17H_M01H_THM_TCON_CUR_TMP);
                    Ring0.ReadPciConfig(0x00, FAMILY_17H_PCI_CONTROL_REGISTER + 4, out uint temperature);

                    // SVI0_TFN_PLANE0 [0]
                    // SVI0_TFN_PLANE1 [1]
                    Ring0.WritePciConfig(0x00, FAMILY_17H_PCI_CONTROL_REGISTER, F17H_M01H_SVI + 0x8);
                    Ring0.ReadPciConfig(0x00, FAMILY_17H_PCI_CONTROL_REGISTER + 4, out smuSvi0Tfn);

                    bool isZen2 = false;

                    // TODO: find a better way because these will probably keep changing in the future.

                    uint sviPlane0Offset;
                    uint sviPlane1Offset;
                    switch (cpu.Model)
                    {
                        case 0x31: // Threadripper 3000.
                        {
                            sviPlane0Offset = F17H_M01H_SVI + 0x14;
                            sviPlane1Offset = F17H_M01H_SVI + 0x10;
                            isZen2 = true;
                            break;
                        }
                        case 0x71: // Zen 2.
                        {
                            sviPlane0Offset = F17H_M01H_SVI + 0x10;
                            sviPlane1Offset = F17H_M01H_SVI + 0xC;
                            isZen2 = true;
                            break;
                        }
                        default: // Zen and Zen+.
                        {
                            sviPlane0Offset = F17H_M01H_SVI + 0xC;
                            sviPlane1Offset = F17H_M01H_SVI + 0x10;
                            break;
                        }
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
                    if (string.IsNullOrWhiteSpace(cpu.Name))
                        offset = 0;
                    else if (cpu.Name.Contains("1600X") || cpu.Name.Contains("1700X") || cpu.Name.Contains("1800X"))
                        offset = -20.0f;
                    else if (cpu.Name.Contains("Threadripper 19") || cpu.Name.Contains("Threadripper 29"))
                        offset = -27.0f;
                    else if (cpu.Name.Contains("2700X"))
                        offset = -10.0f;

                    float t = temperature * 0.001f;
                    if (tempOffsetFlag)
                        t += -49.0f;

                    if (offset < 0)
                    {
                        _coreTemperatureTctl.Value = t;
                        _coreTemperatureTdie.Value = t + offset;

                        _hardware.ActivateSensor(_coreTemperatureTctl);
                        _hardware.ActivateSensor(_coreTemperatureTdie);
                    }
                    else
                    {
                        // Zen 2 doesn't have an offset so Tdie and Tctl are the same.
                        _coreTemperatureTctlTdie.Value = t;
                        _hardware.ActivateSensor(_coreTemperatureTctlTdie);
                    }

                    // Tested only on R5 3600 & Threadripper 3960X.
                    if (isZen2)
                    {
                        for (uint i = 0; i < _ccdTemperatures.Length; i++)
                        {
                            Ring0.WritePciConfig(0x00, FAMILY_17H_PCI_CONTROL_REGISTER, F17H_M70H_CCD1_TEMP + (i * 0x4));
                            Ring0.ReadPciConfig(0x00, FAMILY_17H_PCI_CONTROL_REGISTER + 4, out uint ccdRawTemp);

                            ccdRawTemp &= 0xFFF;
                            float ccdTemp = ((ccdRawTemp * 125) - 305000) * 0.001f;
                            if (ccdRawTemp > 0 && ccdTemp < 125)  // Zen 2 reports 95 degrees C max, but it might exceed that.
                            {
                                if (_ccdTemperatures[i] == null)
                                {
                                    _hardware.ActivateSensor(_ccdTemperatures[i] = new Sensor($"CCD{i + 1} (Tdie)",
                                                                                              _hardware._sensorTemperatures++,
                                                                                              SensorType.Temperature,
                                                                                              _hardware,
                                                                                              _hardware._settings));
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
                                _hardware.ActivateSensor(_ccdsMaxTemperature = new Sensor("CCDs Max (Tdie)",
                                                                                          _hardware._sensorTemperatures++,
                                                                                          SensorType.Temperature,
                                                                                          _hardware,
                                                                                          _hardware._settings));
                            }

                            if (_ccdsAverageTemperature == null)
                            {
                                _hardware.ActivateSensor(_ccdsAverageTemperature = new Sensor("CCDs Average (Tdie)",
                                                                                              _hardware._sensorTemperatures++,
                                                                                              SensorType.Temperature,
                                                                                              _hardware,
                                                                                              _hardware._settings));
                            }

                            _ccdsMaxTemperature.Value = activeCcds.Max(x => x.Value);
                            _ccdsAverageTemperature.Value = activeCcds.Average(x => x.Value);
                        }
                    }
                    Ring0.ReleasePciBusMutex();
                }

                // voltage
                const double vidStep = 0.00625;
                double vcc;
                uint svi0PlaneXVddCor;

                // Core (0x01).
                if ((smuSvi0Tfn & 0x01) == 0)
                {
                    svi0PlaneXVddCor = (smuSvi0TelPlane0 >> 16) & 0xff;
                    vcc = 1.550 - vidStep * svi0PlaneXVddCor;
                    _coreVoltage.Value = (float)vcc;

                    _hardware.ActivateSensor(_coreVoltage);
                }

                // SoC (0x02), not every Zen cpu has this voltage.
                if (cpu.Model == 0x71 || cpu.Model == 0x31 || (smuSvi0Tfn & 0x02) == 0)
                {
                    svi0PlaneXVddCor = (smuSvi0TelPlane1 >> 16) & 0xff;
                    vcc = 1.550 - vidStep * svi0PlaneXVddCor;
                    _socVoltage.Value = (float)vcc;

                    _hardware.ActivateSensor(_socVoltage);
                }

                double timeStampCounterMultiplier = GetTimeStampCounterMultiplier();
                if (timeStampCounterMultiplier > 0)
                {
                    _busClock.Value = (float)(_hardware.TimeStampCounterFrequency / timeStampCounterMultiplier);
                    _hardware.ActivateSensor(_busClock);
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
                    node = new NumaNode(_hardware, numaId);
                    Nodes.Add(node);
                }

                if (thread != null)
                    node.AppendThread(thread, coreId);
            }
        }

        private class NumaNode
        {
            private readonly Amd17Cpu _hw;

            public NumaNode(Hardware hw, int id)
            {
                Cores = new List<Core>();
                NodeId = id;
                _hw = (Amd17Cpu)hw;
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
                    core = new Core(_hw, coreId);
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
            private readonly Sensor _multiplier;
            private readonly Sensor _power;
            private readonly Sensor _vcore;
            private DateTime _lastPwrTime = new DateTime(0);
            private uint _lastPwrValue;

            public Core(Hardware hw, int id)
            {
                Threads = new List<CpuId>();
                CoreId = id;
                Amd17Cpu cpu = (Amd17Cpu)hw;
                _clock = new Sensor("Core #" + CoreId, cpu._sensorClock++, SensorType.Clock, cpu, cpu._settings);
                _multiplier = new Sensor("Core #" + CoreId, cpu._sensorMulti++, SensorType.Factor, cpu, cpu._settings);
                _power = new Sensor("Core #" + CoreId + " (SMU)", cpu._sensorPower++, SensorType.Power, cpu, cpu._settings);
                _vcore = new Sensor("Core #" + CoreId + " VID", cpu._sensorVoltage++, SensorType.Voltage, cpu, cpu._settings);

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


                var previousAffinity = ThreadAffinity.Set(cpu.Affinity);

                // MSRC001_0299
                // TU [19:16]
                // ESU [12:8] -> Unit 15.3 micro Joule per increment
                // PU [3:0]
                Ring0.ReadMsr(MSR_PWR_UNIT, out _, out _);

                // MSRC001_029A
                // total_energy [31:0]
                DateTime sampleTime = DateTime.Now;
                uint eax;
                Ring0.ReadMsr(MSR_CORE_ENERGY_STAT, out eax, out _);
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
                _clock.Value = (float)(curCpuFid / (double)curCpuDfsId * 200.0);

                // multiplier
                _multiplier.Value = (float)(curCpuFid / (double)curCpuDfsId * 2.0);

                // Voltage
                const double vidStep = 0.00625;
                double vcc = 1.550 - vidStep * curCpuVid;
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
}
