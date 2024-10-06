using System;
using System.Collections.Generic;
using System.Linq;

namespace LibreHardwareMonitor.Hardware.Cpu.AMD.Amd17
{
    /// <summary>
    /// AMD 17 Processor
    /// </summary>
    internal class Amd17Processor
    {
        private readonly Amd17Cpu _cpu;
        private readonly Dictionary<KeyValuePair<uint, RyzenSMU.SmuSensorType>, Sensor> _smuSensors = [];

        private Sensor _busClock;
        private Sensor[] _ccdTemperatures;
        private Sensor _coreTemperatureTctl;
        private Sensor _coreTemperatureTctlTdie;
        private Sensor _coreTemperatureTdie;
        private Sensor _coreVoltage;
        private Sensor _packagePower;
        private Sensor _socVoltage;

        private Sensor _ccdsAverageTemperature;
        private Sensor _ccdsMaxTemperature;
        private DateTime _lastPwrTime = new(0);
        private uint _lastPwrValue;

        /// <summary>
        /// Gets the nodes.
        /// </summary>
        /// <value>
        /// The nodes.
        /// </value>
        public List<Amd17NumaNode> Nodes { get; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="Amd17Processor"/> class.
        /// </summary>
        /// <param name="hardware">The hardware.</param>
        public Amd17Processor(Hardware hardware)
        {
            _cpu = (Amd17Cpu)hardware;

            // Sensors
            CreateTemperatureSensors();
            CreateClockSensors();
            CreateVoltageSensors();
            CreatePowerSensors();
            CreateSmuSensors();
        }

        /// <summary>
        /// Appends the thread.
        /// </summary>
        /// <param name="thread">The thread.</param>
        /// <param name="numaId">The numa identifier.</param>
        /// <param name="coreId">The core identifier.</param>
        public void AppendThread(CpuId thread, int numaId, int coreId)
        {
            Amd17NumaNode node = null;
            foreach (Amd17NumaNode n in Nodes)
            {
                if (n.NodeId != numaId) continue;
                node = n;
                break;
            }

            if (node == null)
            {
                node = new Amd17NumaNode(_cpu, numaId);
                Nodes.Add(node);
            }

            if (thread != null)
            {
                node.AppendThread(thread, coreId);
            }
        }

        /// <summary>
        /// Updates the sensors.
        /// </summary>
        public void UpdateSensors()
        {
            UpdateTemperatureSensors();
            UpdateClockSensors();
            UpdateVoltageSensors();
            UpdatePowerSensors();
            UpdateSmuSensors();
        }

        /// <summary>
        /// Create CPU temperature sensors.
        /// </summary>
        /// <returns></returns>
        private void CreateTemperatureSensors()
        {
            _coreTemperatureTctl = new Sensor("Core (Tctl)",
                _cpu.SensorTypeIndex[SensorType.Temperature]++,
                SensorType.Temperature,
                _cpu,
                _cpu.Settings);
            _coreTemperatureTdie = new Sensor("Core (Tdie)",
                _cpu.SensorTypeIndex[SensorType.Temperature]++,
                SensorType.Temperature,
                _cpu,
                _cpu.Settings);
            _coreTemperatureTctlTdie = new Sensor("Core (Tctl/Tdie)",
                _cpu.SensorTypeIndex[SensorType.Temperature]++,
                SensorType.Temperature,
                _cpu,
                _cpu.Settings);

            // Hardcoded until there's a way to get max CCDs.
            _ccdTemperatures = new Sensor[8];
        }

        /// <summary>
        /// Update CPU temperature sensors.
        /// </summary>
        /// <returns></returns>
        private void UpdateTemperatureSensors()
        {
            Amd17NumaNode node = Nodes[0];
            Amd17Core core = node?.Cores[0];
            CpuId cpuId = core?.Threads[0];
            if (cpuId == null) return;

            // Get thread affinity
            GroupAffinity previousAffinity = ThreadAffinity.Set(cpuId.Affinity);

            // Block
            if (!Mutexes.WaitPciBus(10)) return;

            // THM_TCON_CUR_TMP
            // CUR_TEMP [31:21]
            Ring0.WritePciConfig(0x00, Amd17Constants.FAMILY_17H_PCI_CONTROL_REGISTER, Amd17Constants.F17H_M01H_THM_TCON_CUR_TMP);
            Ring0.ReadPciConfig(0x00, Amd17Constants.FAMILY_17H_PCI_CONTROL_REGISTER + 4, out uint temperature);

            // Restore thread affinity
            ThreadAffinity.Set(previousAffinity);

            // current temp Bit [31:21]
            // If bit 19 of the Temperature Control register is set, there is an additional offset of 49 degrees C.
            bool tempOffsetFlag = (temperature & Amd17Constants.F17H_TEMP_OFFSET_FLAG) != 0;
            temperature = (temperature >> 21) * 125;
            float offset = 0.0f;

            // Offset table: https://github.com/torvalds/linux/blob/master/drivers/hwmon/k10temp.c#L78
            if (string.IsNullOrWhiteSpace(cpuId.Name))
            {
                offset = 0;
            }
            else if (cpuId.Name.Contains("1600X") || cpuId.Name.Contains("1700X") || cpuId.Name.Contains("1800X"))
            {
                offset = -20.0f;
            }
            else if (cpuId.Name.Contains("Threadripper 19") || cpuId.Name.Contains("Threadripper 29"))
            {
                offset = -27.0f;
            }
            else if (cpuId.Name.Contains("2700X"))
            {
                offset = -10.0f;
            }

            float tempValue = temperature * 0.001f;
            if (tempOffsetFlag) tempValue += -49.0f;

            // Evaluate offset
            if (offset < 0)
            {
                _coreTemperatureTctl.Value = tempValue;
                _cpu.ActivateSensor(_coreTemperatureTctl);

                _coreTemperatureTdie.Value = tempValue + offset;
                _cpu.ActivateSensor(_coreTemperatureTdie);
            }
            else
            {
                // Zen 2 doesn't have an offset so Tdie and Tctl are the same.
                _coreTemperatureTctlTdie.Value = tempValue;
                _cpu.ActivateSensor(_coreTemperatureTctlTdie);
            }

            // Tested only on R5 3600 & Threadripper 3960X, 5900X, 7900X
            // Get support
            bool supportsPerCcdTemperatures = cpuId.Model switch
            {
                0x31 => true, // Threadripper 3000.
                0x71 or 0x21 => true, // Zen 2, Zen 3
                0x61 or 0x44 => true, // Zen 4, Zen 5
                _ => false
            };
            if (supportsPerCcdTemperatures)
            {
                for (uint i = 0; i < _ccdTemperatures.Length; i++)
                {
                    // Raphael or GraniteRidge
                    uint f17HCcdValue = cpuId.Model is 0x61 or 0x44
                        ? Amd17Constants.F17H_M61H_CCD1_TEMP
                        : Amd17Constants.F17H_M70H_CCD1_TEMP;
                    Ring0.WritePciConfig(0x00, Amd17Constants.FAMILY_17H_PCI_CONTROL_REGISTER, f17HCcdValue + i * 0x4);
                    Ring0.ReadPciConfig(0x00, Amd17Constants.FAMILY_17H_PCI_CONTROL_REGISTER + 4, out uint ccdRawTemp);


                    // Zen 2 reports 95 degrees C max, but it might exceed that.
                    ccdRawTemp &= 0xFFF;
                    float ccdTemp = (ccdRawTemp * 125 - 305000) * 0.001f;
                    if (ccdRawTemp <= 0 || !(ccdTemp < 125)) continue;
                    if (_ccdTemperatures[i] == null)
                    {
                        _cpu.ActivateSensor(_ccdTemperatures[i] =
                            new Sensor($"CCD{i + 1} (Tdie)",
                                _cpu.SensorTypeIndex[SensorType.Temperature]++,
                                SensorType.Temperature,
                                _cpu,
                                _cpu.Settings));
                    }
                    _ccdTemperatures[i].Value = ccdTemp;
                }

                // Process Active CCD Sensors
                Sensor[] activeCcds = _ccdTemperatures.Where(x => x is not null).ToArray();
                if (activeCcds.Length > 1)
                {
                    // No need to get the max / average CCD temp if there is only one CCD.
                    // Max Temp
                    if (_ccdsMaxTemperature == null)
                    {
                        _cpu.ActivateSensor(_ccdsMaxTemperature =
                            new Sensor("CCDs Max (Tdie)",
                                _cpu.SensorTypeIndex[SensorType.Temperature]++,
                                SensorType.Temperature,
                                _cpu,
                                _cpu.Settings));
                    }
                    _ccdsMaxTemperature.Value = activeCcds.Max(x => x.Value);

                    // Average Temp
                    if (_ccdsAverageTemperature == null)
                    {
                        _cpu.ActivateSensor(_ccdsAverageTemperature =
                            new Sensor("CCDs Average (Tdie)",
                                _cpu.SensorTypeIndex[SensorType.Temperature]++,
                                SensorType.Temperature,
                                _cpu,
                                _cpu.Settings));
                    }
                    _ccdsAverageTemperature.Value = activeCcds.Average(x => x.Value);
                }
            }

            // Release
            Mutexes.ReleasePciBus();
        }

        /// <summary>
        /// Create CPU clock sensors.
        /// </summary>
        /// <returns></returns>
        private void CreateClockSensors()
        {
            _busClock = new Sensor("Bus Speed",
                _cpu.SensorTypeIndex[SensorType.Clock]++,
                SensorType.Clock,
                _cpu,
                _cpu.Settings);
        }

        /// <summary>
        /// Update CPU clock sensors.
        /// </summary>
        /// <returns></returns>
        private void UpdateClockSensors()
        {
            double timeStampCounterMultiplier = GetTimeStampCounterMultiplier();
            if (!(timeStampCounterMultiplier > 0)) return;

            _busClock.Value = (float)(_cpu.TimeStampCounterFrequency / timeStampCounterMultiplier);
            _cpu.ActivateSensor(_busClock);
        }

        /// <summary>
        /// Create CPU voltage sensors.
        /// </summary>
        /// <returns></returns>
        private void CreateVoltageSensors()
        {
            _coreVoltage = new Sensor("Core (SVI2 TFN)",
                _cpu.SensorTypeIndex[SensorType.Voltage]++,
                SensorType.Voltage,
                _cpu,
                _cpu.Settings);
            _socVoltage = new Sensor("SoC (SVI2 TFN)",
                _cpu.SensorTypeIndex[SensorType.Voltage]++,
                SensorType.Voltage,
                _cpu,
                _cpu.Settings);
        }

        /// <summary>
        /// Update CPU voltage sensors.
        /// </summary>
        /// <returns></returns>
        private void UpdateVoltageSensors()
        {
            Amd17NumaNode node = Nodes[0];
            Amd17Core core = node?.Cores[0];
            CpuId cpuId = core?.Threads[0];
            if (cpuId == null) return;

            // Get thread affinity
            GroupAffinity previousAffinity = ThreadAffinity.Set(cpuId.Affinity);

            // SMU SVI
            uint smuSvi0Tfn = 0;
            uint smuSvi0TelPlane0 = 0;
            uint smuSvi0TelPlane1 = 0;

            // Block
            if (Mutexes.WaitPciBus(10))
            {
                // SVI0_TFN_PLANE0 [0]
                Ring0.WritePciConfig(0x00, Amd17Constants.FAMILY_17H_PCI_CONTROL_REGISTER, Amd17Constants.F17H_M01H_SVI + 0x8);

                // SVI0_TFN_PLANE1 [1]
                Ring0.ReadPciConfig(0x00, Amd17Constants.FAMILY_17H_PCI_CONTROL_REGISTER + 4, out smuSvi0Tfn);

                // TODO: find a better way because these will probably keep changing in the future.
                uint sviPlane0Offset;
                uint sviPlane1Offset;
                switch (cpuId.Model)
                {
                    case 0x31: // Threadripper 3000.
                        sviPlane0Offset = Amd17Constants.F17H_M01H_SVI + 0x14;
                        sviPlane1Offset = Amd17Constants.F17H_M01H_SVI + 0x10;
                        break;

                    case 0x71: // Zen 2.
                    case 0x21: // Zen 3.
                        sviPlane0Offset = Amd17Constants.F17H_M01H_SVI + 0x10;
                        sviPlane1Offset = Amd17Constants.F17H_M01H_SVI + 0xC;
                        break;

                    case 0x61: //Zen 4
                    case 0x44: //Zen 5
                        sviPlane0Offset = Amd17Constants.F17H_M01H_SVI + 0x10;
                        sviPlane1Offset = Amd17Constants.F17H_M01H_SVI + 0xC;
                        break;

                    default: // Zen and Zen+.
                        sviPlane0Offset = Amd17Constants.F17H_M01H_SVI + 0xC;
                        sviPlane1Offset = Amd17Constants.F17H_M01H_SVI + 0x10;
                        break;
                }

                // SVI0_PLANE0_VDDCOR [24:16]
                Ring0.WritePciConfig(0x00, Amd17Constants.FAMILY_17H_PCI_CONTROL_REGISTER, sviPlane0Offset);

                // SVI0_PLANE0_IDDCOR [7:0]
                Ring0.ReadPciConfig(0x00, Amd17Constants.FAMILY_17H_PCI_CONTROL_REGISTER + 4, out smuSvi0TelPlane0);

                // SVI0_PLANE1_VDDCOR [24:16]
                Ring0.WritePciConfig(0x00, Amd17Constants.FAMILY_17H_PCI_CONTROL_REGISTER, sviPlane1Offset);

                // SVI0_PLANE1_IDDCOR [7:0]
                Ring0.ReadPciConfig(0x00, Amd17Constants.FAMILY_17H_PCI_CONTROL_REGISTER + 4, out smuSvi0TelPlane1);

                // Restore thread affinity
                ThreadAffinity.Set(previousAffinity);

                // Release
                Mutexes.ReleasePciBus();
            }


            // Readout not working for Ryzen 7000/9000.
            if (cpuId.Model is 0x61 or 0x44)
            {
                smuSvi0Tfn |= 0x01 | 0x02;
            }

            // Core (0x01).
            const double vidStep = 0.00625;
            uint svi0PlaneXVddCor;
            if ((smuSvi0Tfn & 0x01) == 0)
            {
                svi0PlaneXVddCor = smuSvi0TelPlane0 >> 16 & 0xff;
                _coreVoltage.Value = (float)(1.550 - vidStep * svi0PlaneXVddCor);
                _cpu.ActivateSensor(_coreVoltage);
            }

            // SoC (0x02), not every Zen cpu has this voltage.
            if (cpuId.Model is 0x11 or 0x21 or 0x71 or 0x31 || (smuSvi0Tfn & 0x02) == 0)
            {
                svi0PlaneXVddCor = smuSvi0TelPlane1 >> 16 & 0xff;
                _socVoltage.Value = (float)(1.550 - vidStep * svi0PlaneXVddCor);
                _cpu.ActivateSensor(_socVoltage);
            }
        }

        /// <summary>
        /// Create CPU power sensors.
        /// </summary>
        /// <returns></returns>
        private void CreatePowerSensors()
        {
            _packagePower = new Sensor("Package",
                _cpu.SensorTypeIndex[SensorType.Power]++,
                SensorType.Power,
                _cpu,
                _cpu.Settings);
            _cpu.ActivateSensor(_packagePower);
        }

        /// <summary>
        /// Updates CPU power sensors.
        /// </summary>
        /// <returns></returns>
        private void UpdatePowerSensors()
        {
            Amd17NumaNode node = Nodes[0];
            Amd17Core core = node?.Cores[0];
            CpuId cpuId = core?.Threads[0];
            if (cpuId is null) return;

            // MSRC001_0299
            // TU [19:16]
            // ESU [12:8] -> Unit 15.3 micro Joule per increment
            // PU [3:0]
            Ring0.ReadMsr(Amd17Constants.MSR_PWR_UNIT, out uint _, out uint _);

            // MSRC001_029B
            // total_energy [31:0]
            DateTime sampleTime = DateTime.Now;
            Ring0.ReadMsr(Amd17Constants.MSR_PKG_ENERGY_STAT, out uint eax, out _);
            uint totalEnergy = eax;

            // Block
            if (!Mutexes.WaitPciBus(10)) return;

            // Power consumption
            // power.Value = (float) ((double)pu * 0.125);
            // esu = 15.3 micro Joule per increment
            if (_lastPwrTime.Ticks == 0)
            {
                _lastPwrTime = sampleTime;
                _lastPwrValue = totalEnergy;
            }

            // Ticks diff
            TimeSpan time = sampleTime - _lastPwrTime;
            long pwr = _lastPwrValue <= totalEnergy
                ? totalEnergy - _lastPwrValue
                : 0xffffffff - _lastPwrValue + totalEnergy;

            // Update for next sample
            _lastPwrTime = sampleTime;
            _lastPwrValue = totalEnergy;

            // Assign
            double energy = 15.3e-6 * pwr;
            energy /= time.TotalSeconds;
            if (!double.IsNaN(energy))
            {
                _packagePower.Value = (float)energy;
            }

            // Release
            Mutexes.ReleasePciBus();
        }

        /// <summary>
        /// Creates the AMD SMU sensors.
        /// </summary>
        private void CreateSmuSensors()
        {
            foreach (KeyValuePair<uint, RyzenSMU.SmuSensorType> sensor in _cpu.SMU.GetPmTableStructure())
            {
                var currentSensor = sensor.Value;
                _smuSensors.Add(sensor,
                    new Sensor(currentSensor.Name,
                        _cpu.SensorTypeIndex[currentSensor.Type]++,
                        currentSensor.Type,
                        _cpu,
                        _cpu.Settings));
            }
        }

        /// <summary>
        /// Updates the AMD SMU sensors.
        /// </summary>
        private void UpdateSmuSensors()
        {
            if (!_cpu.SMU.IsPmTableLayoutDefined()) return;

            float[] smuData = _cpu.SMU.GetPmTable();
            foreach (KeyValuePair<KeyValuePair<uint, RyzenSMU.SmuSensorType>, Sensor> sensor in _smuSensors)
            {
                if (smuData.Length <= sensor.Key.Key) continue;
                var key = sensor.Key;
                var currentSensor = sensor.Value;

                // Assign value
                currentSensor.Value = smuData[key.Key] * key.Value.Scale;
                if (currentSensor.Value == 0) continue;

                // Activate
                _cpu.ActivateSensor(currentSensor);
            }
        }

        /// <summary>
        /// Gets the time stamp counter multiplier.
        /// </summary>
        /// <returns></returns>
        private double GetTimeStampCounterMultiplier()
        {
            Ring0.ReadMsr(Amd17Constants.MSR_PSTATE_0, out uint eax, out _);
            uint cpuDfsId = eax >> 8 & 0x3f;
            uint cpuFid = eax & 0xff;
            return 2.0 * cpuFid / cpuDfsId;
        }
    }
}
