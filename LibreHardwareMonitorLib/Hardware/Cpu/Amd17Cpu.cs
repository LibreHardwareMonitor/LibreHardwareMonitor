// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

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
            private readonly Sensor _coreTemperatureTctl;
            private readonly Sensor _coreTemperatureTdie;
            private readonly Sensor _coreVoltage;
            private readonly Amd17Cpu _hw;
            private readonly Sensor _packagePower;
            private readonly Sensor _socVoltage;
            private DateTime _lastPwrTime = new DateTime(0);
            private uint _lastPwrValue;

            public Processor(Hardware hw)
            {
                _hw = (Amd17Cpu)hw;
                Nodes = new List<NumaNode>();

                _packagePower = new Sensor("Package Power", _hw._sensorPower++, SensorType.Power, _hw, _hw._settings);
                _coreTemperatureTctl = new Sensor("Core (Tctl)", _hw._sensorTemperatures++, SensorType.Temperature, _hw, _hw._settings);
                _coreTemperatureTdie = new Sensor("Core (Tdie)", _hw._sensorTemperatures++, SensorType.Temperature, _hw, _hw._settings);
                _coreVoltage = new Sensor("Core (SVI2 TFN)", _hw._sensorVoltage++, SensorType.Voltage, _hw, _hw._settings);
                _socVoltage = new Sensor("SoC (SVI2 TFN)", _hw._sensorVoltage++, SensorType.Voltage, _hw, _hw._settings);

                _hw.ActivateSensor(_packagePower);
                _hw.ActivateSensor(_coreTemperatureTctl);
                _hw.ActivateSensor(_coreTemperatureTdie);
                _hw.ActivateSensor(_coreVoltage);
            }

            public List<NumaNode> Nodes { get; }

            public void UpdateSensors()
            {
                var node = Nodes[0];
                Core core = node?.Cores[0];
                CpuId cpu = core?.Threads[0];
                if (cpu == null)
                    return;


                ulong mask = Ring0.ThreadAffinitySet(1UL << cpu.Thread);

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

                // THM_TCON_CUR_TMP
                // CUR_TEMP [31:21]
                Ring0.WritePciConfig(Ring0.GetPciAddress(0, 0, 0), FAMILY_17H_PCI_CONTROL_REGISTER, F17H_M01H_THM_TCON_CUR_TMP);
                Ring0.ReadPciConfig(Ring0.GetPciAddress(0, 0, 0), FAMILY_17H_PCI_CONTROL_REGISTER + 4, out uint temperature);

                // SVI0_TFN_PLANE0 [0]
                // SVI0_TFN_PLANE1 [1]
                Ring0.WritePciConfig(Ring0.GetPciAddress(0, 0, 0), FAMILY_17H_PCI_CONTROL_REGISTER, F17H_M01H_SVI + 0x8);
                Ring0.ReadPciConfig(Ring0.GetPciAddress(0, 0, 0), FAMILY_17H_PCI_CONTROL_REGISTER + 4, out uint smuSvi0Tfn);

                // SVI0_PLANE0_VDDCOR [24:16]
                // SVI0_PLANE0_IDDCOR [7:0]
                Ring0.WritePciConfig(Ring0.GetPciAddress(0, 0, 0), FAMILY_17H_PCI_CONTROL_REGISTER, F17H_M01H_SVI + 0xc);
                Ring0.ReadPciConfig(Ring0.GetPciAddress(0, 0, 0), FAMILY_17H_PCI_CONTROL_REGISTER + 4, out uint smuSvi0TelPlane0);

                // SVI0_PLANE1_VDDCOR [24:16]
                // SVI0_PLANE1_IDDCOR [7:0]
                Ring0.WritePciConfig(Ring0.GetPciAddress(0, 0, 0), FAMILY_17H_PCI_CONTROL_REGISTER, F17H_M01H_SVI + 0x10);
                Ring0.ReadPciConfig(Ring0.GetPciAddress(0, 0, 0), FAMILY_17H_PCI_CONTROL_REGISTER + 4, out uint smuSvi0TelPlane1);

                Ring0.ThreadAffinitySet(mask);

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
                //If bit 19 of the Temperature Control register is set, there is an additional offset of 49 degrees C.
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

                _coreTemperatureTctl.Value = t;
                _coreTemperatureTdie.Value = t + offset;

                // voltage
                double vidStep = 0.00625;
                double vcc;
                uint svi0PlaneXVddCor;

                // Core
                if ((smuSvi0Tfn & 0x01) == 0)
                {
                    svi0PlaneXVddCor = ((cpu.Model == 0x71 ? smuSvi0TelPlane1 : smuSvi0TelPlane0) >> 16) & 0xff;
                    vcc = 1.550 - vidStep * svi0PlaneXVddCor;
                    _coreVoltage.Value = (float)vcc;
                }

                // SoC
                // not every zen cpu has this voltage
                if (cpu.Model == 0x71 || (smuSvi0Tfn & 0x02) == 0)
                {
                    svi0PlaneXVddCor = ((cpu.Model == 0x71 ? smuSvi0TelPlane0 : smuSvi0TelPlane1) >> 16) & 0xff;
                    vcc = 1.550 - vidStep * svi0PlaneXVddCor;
                    _socVoltage.Value = (float)vcc;

                    _hw.ActivateSensor(_socVoltage);
                }
            }

            public void AppendThread(CpuId thread, int numaId, int coreId)
            {
                NumaNode node = null;
                foreach (var n in Nodes)
                {
                    if (n.NodeId == numaId)
                    {
                        node = n;
                        break;
                    }
                }

                if (node == null)
                {
                    node = new NumaNode(_hw, numaId);
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
                foreach (var c in Cores)
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


                ulong mask = Ring0.ThreadAffinitySet(1UL << cpu.Thread);

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
                Ring0.ThreadAffinitySet(mask);

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
