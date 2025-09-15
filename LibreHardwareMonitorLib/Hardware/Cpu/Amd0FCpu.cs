// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Threading;
using LibreHardwareMonitor.PawnIo;

namespace LibreHardwareMonitor.Hardware.Cpu;

internal sealed class Amd0FCpu : AmdCpu
{
    private readonly Sensor _busClock;
    private readonly Sensor[] _coreClocks;
    private readonly Sensor[] _coreTemperatures;

    private readonly AmdFamily0F _pawnModule;

    /// <inheritdoc />
    public override void Close()
    {
        base.Close();
        _pawnModule.Close();
    }

    public Amd0FCpu(int processorIndex, CpuId[][] cpuId, ISettings settings) : base(processorIndex, cpuId, settings)
    {
        _pawnModule = new AmdFamily0F();

        float offset = -49.0f;

        // AM2+ 65nm +21 offset
        uint model = cpuId[0][0].Model;
        if (model is >= 0x69 and not 0xc1 and not 0x6c and not 0x7c)
            offset += 21;

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
                                                  [new ParameterDescription("Offset [°C]", "Temperature offset of the thermal sensor.\nTemperature = Value + Offset.", offset)],
                                                  settings);
            }
        }
        else
        {
            _coreTemperatures = [];
        }

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

    public override void Update()
    {
        base.Update();

        if (Mutexes.WaitPciBus(10))
        {
            for (uint i = 0; i < _coreTemperatures.Length; i++)
            {
                uint value;

                try
                {
                    value = _pawnModule.GetThermtrip(Index, i);
                }
                catch
                {
                    DeactivateSensor(_coreTemperatures[i]);
                    continue;
                }

                _coreTemperatures[i].Value = ((value >> 16) & 0xFF) + _coreTemperatures[i].Parameters[0].Value;
                ActivateSensor(_coreTemperatures[i]);
            }

            Mutexes.ReleasePciBus();
        }

        if (HasTimeStampCounter)
        {
            double newBusClock = 0;

            for (int i = 0; i < _coreClocks.Length; i++)
            {
                Thread.Sleep(1);

                if (_pawnModule.ReadMsr(FIDVID_STATUS, out uint eax, out uint _, _cpuId[i][0].Affinity))
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
    // ReSharper restore InconsistentNaming
}
