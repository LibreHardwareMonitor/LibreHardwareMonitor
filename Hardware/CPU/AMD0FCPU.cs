// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System.Globalization;
using System.Text;
using System.Threading;

namespace LibreHardwareMonitor.Hardware.CPU
{
    internal sealed class AMD0FCPU : AMDCPU
    {
        private readonly Sensor[] _coreTemperatures;
        private readonly Sensor[] _coreClocks;
        private readonly Sensor _busClock;

        private const uint FIDVID_STATUS = 0xC0010042;

        private const byte MISCELLANEOUS_CONTROL_FUNCTION = 3;
        private const ushort MISCELLANEOUS_CONTROL_DEVICE_ID = 0x1103;
        private const uint THERMTRIP_STATUS_REGISTER = 0xE4;

        private readonly byte _thermSenseCoreSelCPU0;
        private readonly byte _thermSenseCoreSelCPU1;
        private readonly uint _miscellaneousControlAddress;

        public AMD0FCPU(int processorIndex, CPUID[][] cpuid, ISettings settings) : base(processorIndex, cpuid, settings)
        {
            float offset = -49.0f;

            // AM2+ 65nm +21 offset
            uint model = cpuid[0][0].Model;
            if (model >= 0x69 && model != 0xc1 && model != 0x6c && model != 0x7c)
                offset += 21;

            if (model < 40)
            {
                // AMD Athlon 64 Processors
                _thermSenseCoreSelCPU0 = 0x0;
                _thermSenseCoreSelCPU1 = 0x4;
            }
            else
            {
                // AMD NPT Family 0Fh Revision F, G have the core selection swapped
                _thermSenseCoreSelCPU0 = 0x4;
                _thermSenseCoreSelCPU1 = 0x0;
            }

            // check if processor supports a digital thermal sensor
            if (cpuid[0][0].ExtData.GetLength(0) > 7 && (cpuid[0][0].ExtData[7, 3] & 1) != 0)
            {
                _coreTemperatures = new Sensor[coreCount];
                for (int i = 0; i < coreCount; i++)
                {
                    _coreTemperatures[i] = new Sensor("Core #" + (i + 1), i, SensorType.Temperature, this, new[] {
                        new ParameterDescription("Offset [°C]", "Temperature offset of the thermal sensor.\n" + "Temperature = Value + Offset.", offset) }, settings);
                }
            }
            else
            {
                _coreTemperatures = new Sensor[0];
            }

            _miscellaneousControlAddress = GetPciAddress(MISCELLANEOUS_CONTROL_FUNCTION, MISCELLANEOUS_CONTROL_DEVICE_ID);
            _busClock = new Sensor("Bus Speed", 0, SensorType.Clock, this, settings);
            _coreClocks = new Sensor[coreCount];
            for (int i = 0; i < _coreClocks.Length; i++)
            {
                _coreClocks[i] = new Sensor(CoreString(i), i + 1, SensorType.Clock, this, settings);
                if (HasTimeStampCounter)
                    ActivateSensor(_coreClocks[i]);
            }
            Update();
        }

        protected override uint[] GetMSRs()
        {
            return new[] { FIDVID_STATUS };
        }

        public override string GetReport()
        {
            StringBuilder r = new StringBuilder();
            r.Append(base.GetReport());
            r.Append("Miscellaneous Control Address: 0x");
            r.AppendLine((_miscellaneousControlAddress).ToString("X", CultureInfo.InvariantCulture));
            r.AppendLine();
            return r.ToString();
        }

        public override void Update()
        {
            base.Update();

            if (_miscellaneousControlAddress != Ring0.InvalidPciAddress)
            {
                for (uint i = 0; i < _coreTemperatures.Length; i++)
                {
                    if (Ring0.WritePciConfig(_miscellaneousControlAddress, THERMTRIP_STATUS_REGISTER, i > 0 ? _thermSenseCoreSelCPU1 : _thermSenseCoreSelCPU0))
                    {
                        uint value;
                        if (Ring0.ReadPciConfig(_miscellaneousControlAddress, THERMTRIP_STATUS_REGISTER, out value))
                        {
                            _coreTemperatures[i].Value = ((value >> 16) & 0xFF) + _coreTemperatures[i].Parameters[0].Value;
                            ActivateSensor(_coreTemperatures[i]);
                        }
                        else
                        {
                            DeactivateSensor(_coreTemperatures[i]);
                        }
                    }
                }
            }

            if (HasTimeStampCounter)
            {
                double newBusClock = 0;

                for (int i = 0; i < _coreClocks.Length; i++)
                {
                    Thread.Sleep(1);

                    uint eax, edx;
                    if (Ring0.RdmsrTx(FIDVID_STATUS, out eax, out edx, 1UL << cpuid[i][0].Thread))
                    {
                        // CurrFID can be found in eax bits 0-5, MaxFID in 16-21
                        // 8-13 hold StartFID, we don't use that here.
                        double curMP = 0.5 * ((eax & 0x3F) + 8);
                        double maxMP = 0.5 * ((eax >> 16 & 0x3F) + 8);
                        _coreClocks[i].Value = (float)(curMP * TimeStampCounterFrequency / maxMP);
                        newBusClock = (float)(TimeStampCounterFrequency / maxMP);
                    }
                    else
                    {
                        // Fail-safe value - if the code above fails, we'll use this instead
                        _coreClocks[i].Value = (float)TimeStampCounterFrequency;
                    }
                }

                if (newBusClock > 0)
                {
                    _busClock.Value = (float)newBusClock;
                    ActivateSensor(_busClock);
                }
            }
        }

    }
}
