using System;
using System.Collections.Generic;
using System.Linq;

namespace LibreHardwareMonitor.Hardware.Cpu.AMD.Amd17
{
    /// <summary>
    /// AMD 17 Core
    /// </summary>
    internal class Amd17Core
    {
        #region Fields

        private readonly Sensor _clock;
        private readonly Amd17Cpu _cpu;
        private readonly Sensor _multiplier;
        private readonly Sensor _power;
        private readonly Sensor _vcore;
        private ISensor _busSpeed;
        private DateTime _lastPwrTime = new(0);
        private uint _lastPwrValue;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the core identifier.
        /// </summary>
        /// <value>
        /// The core identifier.
        /// </value>
        public int CoreId { get; }

        /// <summary>
        /// Gets the threads.
        /// </summary>
        /// <value>
        /// The threads.
        /// </value>
        public List<CpuId> Threads { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Amd17Core"/> class.
        /// </summary>
        /// <param name="cpu">The cpu.</param>
        /// <param name="id">The identifier.</param>
        public Amd17Core(Amd17Cpu cpu, int id)
        {
            _cpu = cpu;
            Threads = new List<CpuId>();
            CoreId = id;
            _clock = new Sensor("Core #" + CoreId, _cpu.SensorTypeIndex[SensorType.Clock]++, SensorType.Clock, cpu, cpu.Settings);
            _multiplier = new Sensor("Core #" + CoreId, cpu.SensorTypeIndex[SensorType.Factor]++, SensorType.Factor, cpu, cpu.Settings);
            _power = new Sensor("Core #" + CoreId + " (SMU)", cpu.SensorTypeIndex[SensorType.Power]++, SensorType.Power, cpu, cpu.Settings);
            _vcore = new Sensor("Core #" + CoreId + " VID", cpu.SensorTypeIndex[SensorType.Voltage]++, SensorType.Voltage, cpu, cpu.Settings);

            cpu.ActivateSensor(_clock);
            cpu.ActivateSensor(_multiplier);
            cpu.ActivateSensor(_power);
            cpu.ActivateSensor(_vcore);
        }

        #endregion

        /// <summary>
        /// Updates the sensors.
        /// </summary>
        public void UpdateSensors()
        {
            // CPUID cpu = threads.FirstOrDefault();
            CpuId cpu = Threads[0];
            if (cpu == null) return;

            // Processor affinity
            GroupAffinity previousAffinity = ThreadAffinity.Set(cpu.Affinity);

            // MSRC001_0299
            // TU [19:16]
            // ESU [12:8] -> Unit 15.3 micro Joule per increment
            // PU [3:0]
            Ring0.ReadMsr(Amd17Constants.MSR_PWR_UNIT, out _, out _);

            // MSRC001_029A
            // total_energy [31:0]
            DateTime sampleTime = DateTime.Now;
            Ring0.ReadMsr(Amd17Constants.MSR_CORE_ENERGY_STAT, out uint eax, out _);
            uint totalEnergy = eax;

            // MSRC001_0293
            // CurHwPstate [24:22]
            // CurCpuVid [21:14]
            // CurCpuDfsId [13:8]
            // CurCpuFid [7:0]
            Ring0.ReadMsr(Amd17Constants.MSR_HARDWARE_PSTATE_STATUS, out eax, out _);
            int curCpuVid = (int)(eax >> 14 & 0xff);
            int curCpuDfsId = (int)(eax >> 8 & 0x3f);
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
            long pwr = _lastPwrValue <= totalEnergy
                ? totalEnergy - _lastPwrValue
                : 0xffffffff - _lastPwrValue + totalEnergy;

            // update for next sample
            _lastPwrTime = sampleTime;
            _lastPwrValue = totalEnergy;

            double energy = 15.3e-6 * pwr;
            energy /= time.TotalSeconds;

            if (!double.IsNaN(energy))
            {
                _power.Value = (float)energy;
            }
        }
    }
}
