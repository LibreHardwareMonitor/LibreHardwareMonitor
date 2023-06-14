// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Globalization;
using System.Text;
using System.Threading;

namespace LibreHardwareMonitor.Hardware.Cpu;

internal sealed class Amd0FCpu : AmdCpu
{
    private readonly Sensor _busClock;
    private readonly Sensor[] _coreClocks;
    private readonly Sensor[] _coreTemperatures;
    private readonly uint _miscellaneousControlAddress;

    private readonly byte _thermSenseCoreSelCPU0;
    private readonly byte _thermSenseCoreSelCPU1;

    public Amd0FCpu(int processorIndex, CpuId[][] cpuId, ISettings settings) : base(processorIndex, cpuId, settings)
    {
        float offset = -49.0f;

        // AM2+ 65nm +21 offset
        uint model = cpuId[0][0].Model;
        if (model is >= 0x69 and not 0xc1 and not 0x6c and not 0x7c)
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
        if (cpuId[0][0].ExtData.GetLength(0) > 7 && (cpuId[0][0].ExtData[7, 3] & 1) != 0)
        {
            _coreTemperatures = new Sensor[_coreCount];
            for (int i = 0; i < _coreCount; i++)
            {
                _coreTemperatures[i] = new Sensor("Core #" + (i + 1),
                                                  i,
                                                  SensorType.Temperature,
                                                  this,
                                                  new[] { new ParameterDescription("Offset [°C]", "Temperature offset of the thermal sensor.\nTemperature = Value + Offset.", offset) },
                                                  settings);
            }
        }
        else
        {
            _coreTemperatures = Array.Empty<Sensor>();
        }

        _miscellaneousControlAddress = GetPciAddress(MISCELLANEOUS_CONTROL_FUNCTION, MISCELLANEOUS_CONTROL_DEVICE_ID);
        _busClock = new Sensor("Bus Speed", 0, SensorType.Clock, this, settings);
        _coreClocks = new Sensor[_coreCount];
        for (int i = 0; i < _coreClocks.Length; i++)
        {
            _coreClocks[i] = new Sensor(CoreString(i), i + 1, SensorType.Clock, this, settings);
            if (HasTimeStampCounter)
                ActivateSensor(_coreClocks[i]);
        }

        Update();
    }

    protected override uint[] GetMsrs()
    {
        return new[] { FIDVID_STATUS };
    }

    public override string GetReport()
    {
        StringBuilder r = new();
        r.Append(base.GetReport());
        r.Append("Miscellaneous Control Address: 0x");
        r.AppendLine(_miscellaneousControlAddress.ToString("X", CultureInfo.InvariantCulture));
        r.AppendLine();
        return r.ToString();
    }

    public override void Update()
    {
        base.Update();

        if (Mutexes.WaitPciBus(10))
        {
            if (_miscellaneousControlAddress != Interop.Ring0.INVALID_PCI_ADDRESS)
            {
                for (uint i = 0; i < _coreTemperatures.Length; i++)
                {
                    if (Ring0.WritePciConfig(_miscellaneousControlAddress,
                                             THERMTRIP_STATUS_REGISTER,
                                             i > 0 ? _thermSenseCoreSelCPU1 : _thermSenseCoreSelCPU0))
                    {
                        if (Ring0.ReadPciConfig(_miscellaneousControlAddress, THERMTRIP_STATUS_REGISTER, out uint value))
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

            Mutexes.ReleasePciBus();
        }

        if (HasTimeStampCounter)
        {
            double newBusClock = 0;

            for (int i = 0; i < _coreClocks.Length; i++)
            {
                Thread.Sleep(1);

                if (Ring0.ReadMsr(FIDVID_STATUS, out uint eax, out uint _, _cpuId[i][0].Affinity))
                {
                    // CurrFID can be found in eax bits 0-5, MaxFID in 16-21
                    // 8-13 hold StartFID, we don't use that here.
                    double curMp = 0.5 * ((eax & 0x3F) + 8);
                    double maxMp = 0.5 * ((eax >> 16 & 0x3F) + 8);
                    _coreClocks[i].Value = (float)(curMp * TimeStampCounterFrequency / maxMp);
                    newBusClock = (float)(TimeStampCounterFrequency / maxMp);
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

    // ReSharper disable InconsistentNaming
    private const uint FIDVID_STATUS = 0xC0010042;
    private const ushort MISCELLANEOUS_CONTROL_DEVICE_ID = 0x1103;
    private const byte MISCELLANEOUS_CONTROL_FUNCTION = 3;
    private const uint THERMTRIP_STATUS_REGISTER = 0xE4;
    // ReSharper restore InconsistentNaming
}
