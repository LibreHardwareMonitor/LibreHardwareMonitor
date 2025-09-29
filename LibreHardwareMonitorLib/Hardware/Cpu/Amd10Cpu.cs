// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using LibreHardwareMonitor.PawnIo;

namespace LibreHardwareMonitor.Hardware.Cpu;

internal sealed class Amd10Cpu : AmdCpu
{
    private readonly Sensor _busClock;
    private readonly Sensor[] _coreClocks;
    private readonly Sensor _coreTemperature;
    private readonly Sensor _coreVoltage;
    private readonly Sensor[] _cStatesResidency;
    private readonly bool _hasSmuTemperatureRegister;
    private readonly bool _isSvi2;
    private readonly Sensor _northbridgeVoltage;
    private readonly FileStream _temperatureStream;
    private readonly double _timeStampCounterMultiplier;

    private readonly AmdFamily10 _pawnModule;

    public Amd10Cpu(int processorIndex, CpuId[][] cpuId, ISettings settings) : base(processorIndex, cpuId, settings)
    {
        _pawnModule = new AmdFamily10();

        // AMD family 1Xh processors support only one temperature sensor
        _coreTemperature = new Sensor("CPU Cores", 0, SensorType.Temperature, this, new[] { new ParameterDescription("Offset [°C]", "Temperature offset.", 0) }, settings);
        _coreVoltage = new Sensor("CPU Cores", 0, SensorType.Voltage, this, settings);
        ActivateSensor(_coreVoltage);
        _northbridgeVoltage = new Sensor("Northbridge", 0, SensorType.Voltage, this, settings);
        ActivateSensor(_northbridgeVoltage);

        _isSvi2 = (_family == 0x15 && _model >= 0x10) || _family == 0x16;

        if (_family == 0x15)
        {
                switch (_model & 0xF0)
                {
                    case 0x60:
                    case 0x70:
                        _hasSmuTemperatureRegister = true;
                        break;
                }
        }

        // get the pci address for the Miscellaneous Control registers
        _busClock = new Sensor("Bus Speed", 0, SensorType.Clock, this, settings);
        _coreClocks = new Sensor[_coreCount];
        for (int i = 0; i < _coreClocks.Length; i++)
        {
            _coreClocks[i] = new Sensor(CoreString(i), i + 1, SensorType.Clock, this, settings);
            if (HasTimeStampCounter)
                ActivateSensor(_coreClocks[i]);
        }

        // set affinity to the first thread for all frequency estimations
        GroupAffinity previousAffinity = ThreadAffinity.Set(cpuId[0][0].Affinity);

        _timeStampCounterMultiplier = MeasureTimeStampCounterMultiplier();

        // restore the thread affinity.
        ThreadAffinity.Set(previousAffinity);

        // the file reader for lm-sensors support on Linux
        _temperatureStream = null;

        if (Software.OperatingSystem.IsUnix)
        {
            foreach (string path in Directory.GetDirectories("/sys/class/hwmon/"))
            {
                string name = null;
                try
                {
                    using StreamReader reader = new(path + "/device/name");

                    name = reader.ReadLine();
                }
                catch (IOException)
                { }

                _temperatureStream = name switch
                {
                    "k10temp" => new FileStream(path + "/device/temp1_input", FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                    _ => _temperatureStream
                };
            }
        }

        if (_pawnModule.HaveCstateResidencyInfo())
        {
            _cStatesResidency = new[] { new Sensor("CPU Package C2", 0, SensorType.Level, this, settings), new Sensor("CPU Package C3", 1, SensorType.Level, this, settings) };
            ActivateSensor(_cStatesResidency[0]);
            ActivateSensor(_cStatesResidency[1]);
        }

        Update();
    }

    private double MeasureTimeStampCounterMultiplier()
    {
        _pawnModule.MeasureTscMultiplier(out var ctrPerTick, out var cofVid);

        double coreMultiplier = GetCoreMultiplier((uint)cofVid);

        double coreFrequency = 1e-6 * ((double)ctrPerTick * Stopwatch.Frequency);
        double busFrequency = coreFrequency / coreMultiplier;
        return 0.25 * Math.Round(4 * TimeStampCounterFrequency / busFrequency);
    }

    public override string GetReport()
    {
        StringBuilder r = new();
        r.Append(base.GetReport());
        r.Append("Time Stamp Counter Multiplier: ");
        r.AppendLine(_timeStampCounterMultiplier.ToString(CultureInfo.InvariantCulture));
        if (_family == 0x14)
        {
            uint value = _pawnModule.ReadMiscCtl(Index, CLOCK_POWER_TIMING_CONTROL_0_REGISTER);
            r.Append("PCI Register D18F3xD4: ");
            r.AppendLine(value.ToString("X8", CultureInfo.InvariantCulture));
        }

        r.AppendLine();
        return r.ToString();
    }

    private double GetCoreMultiplier(uint cofVidEax)
    {
        uint cpuDid;
        uint cpuFid;

        switch (_family)
        {
            case 0x10:
            case 0x11:
            case 0x15:
            case 0x16:
                // 8:6 CpuDid: current core divisor ID
                // 5:0 CpuFid: current core frequency ID
                cpuDid = (cofVidEax >> 6) & 7;
                cpuFid = cofVidEax & 0x1F;
                return 0.5 * (cpuFid + 0x10) / (1 << (int)cpuDid);

            case 0x12:
                // 8:4 CpuFid: current CPU core frequency ID
                // 3:0 CpuDid: current CPU core divisor ID
                cpuFid = (cofVidEax >> 4) & 0x1F;
                cpuDid = cofVidEax & 0xF;
                double divisor = cpuDid switch
                {
                    0 => 1,
                    1 => 1.5,
                    2 => 2,
                    3 => 3,
                    4 => 4,
                    5 => 6,
                    6 => 8,
                    7 => 12,
                    8 => 16,
                    _ => 1
                };
                return (cpuFid + 0x10) / divisor;

            case 0x14:
                // 8:4: current CPU core divisor ID most significant digit
                // 3:0: current CPU core divisor ID least significant digit
                uint divisorIdMsd = (cofVidEax >> 4) & 0x1F;
                uint divisorIdLsd = cofVidEax & 0xF;
                uint value = _pawnModule.ReadMiscCtl(Index, CLOCK_POWER_TIMING_CONTROL_0_REGISTER);
                uint frequencyId = value & 0x1F;
                return (frequencyId + 0x10) / (divisorIdMsd + (divisorIdLsd * 0.25) + 1);

            default:
                return 1;
        }
    }

    private static string ReadFirstLine(Stream stream)
    {
        StringBuilder stringBuilder = new();

        try
        {
            stream.Seek(0, SeekOrigin.Begin);
            int b = stream.ReadByte();
            while (b is not -1 and not 10)
            {
                stringBuilder.Append((char)b);
                b = stream.ReadByte();
            }
        }
        catch
        { }

        return stringBuilder.ToString();
    }

    public override void Update()
    {
        base.Update();

        if (_temperatureStream == null)
        {
            bool isValueValid = true;
            uint value = 0;
            try
            {
                if (_hasSmuTemperatureRegister)
                    ReadSmuRegister(SMU_REPORTED_TEMP_CTRL_OFFSET, out value);
                else
                    value = _pawnModule.ReadMiscCtl(Index, REPORTED_TEMPERATURE_CONTROL_REGISTER);
            }
            catch
            {
                isValueValid = false;
            }

            if (isValueValid)
            {
                if ((_family == 0x15 || _family == 0x16) && (value & 0x30000) == 0x3000)
                {
                    if (_family == 0x15 && (_model & 0xF0) == 0x00)
                    {
                        _coreTemperature.Value = (((value >> 21) & 0x7FC) / 8.0f) + _coreTemperature.Parameters[0].Value - 49;
                    }
                    else
                    {
                        _coreTemperature.Value = (((value >> 21) & 0x7FF) / 8.0f) + _coreTemperature.Parameters[0].Value - 49;
                    }
                }
                else
                {
                    _coreTemperature.Value = (((value >> 21) & 0x7FF) / 8.0f) + _coreTemperature.Parameters[0].Value;
                }

                ActivateSensor(_coreTemperature);
            }
            else
            {
                DeactivateSensor(_coreTemperature);
            }
        }
        else
        {
            string s = ReadFirstLine(_temperatureStream);
            try
            {
                _coreTemperature.Value = 0.001f * long.Parse(s, CultureInfo.InvariantCulture);
                ActivateSensor(_coreTemperature);
            }
            catch
            {
                DeactivateSensor(_coreTemperature);
            }
        }

        if (HasTimeStampCounter)
        {
            double newBusClock = 0;
            float maxCoreVoltage = 0, maxNbVoltage = 0;

            for (int i = 0; i < _coreClocks.Length; i++)
            {
                Thread.Sleep(1);

                if (_pawnModule.ReadMsr(COFVID_STATUS, out uint curEax, out uint _, _cpuId[i][0].Affinity))
                {
                    double multiplier = GetCoreMultiplier(curEax);

                    _coreClocks[i].Value = (float)(multiplier * TimeStampCounterFrequency / _timeStampCounterMultiplier);
                    newBusClock = (float)(TimeStampCounterFrequency / _timeStampCounterMultiplier);
                }
                else
                {
                    _coreClocks[i].Value = (float)TimeStampCounterFrequency;
                }

                float SVI2Volt(uint vid) => vid < 0b1111_1000 ? 1.5500f - (0.00625f * vid) : 0;

                float SVI1Volt(uint vid) => vid < 0x7C ? 1.550f - (0.0125f * vid) : 0;

                float newCoreVoltage, newNbVoltage;
                uint coreVid60 = (curEax >> 9) & 0x7F;
                if (_isSvi2)
                {
                    newCoreVoltage = SVI2Volt((curEax >> 13 & 0x80) | coreVid60);
                    newNbVoltage = SVI2Volt(curEax >> 24);
                }
                else
                {
                    newCoreVoltage = SVI1Volt(coreVid60);
                    newNbVoltage = SVI1Volt(curEax >> 25);
                }

                if (newCoreVoltage > maxCoreVoltage)
                    maxCoreVoltage = newCoreVoltage;

                if (newNbVoltage > maxNbVoltage)
                    maxNbVoltage = newNbVoltage;
            }

            _coreVoltage.Value = maxCoreVoltage;
            _northbridgeVoltage.Value = maxNbVoltage;

            if (newBusClock > 0)
            {
                _busClock.Value = (float)newBusClock;
                ActivateSensor(_busClock);
            }
        }

        if (_cStatesResidency != null)
        {
            var results = _pawnModule.ReadCstateResidency();
            for (int i = 0; i < _cStatesResidency.Length; i++)
            {
                _cStatesResidency[i].Value = results[i] / 256f * 100;
            }
        }
    }

    private bool ReadSmuRegister(uint address, out uint value)
    {
        value = 0;
        if (!Mutexes.WaitPciBus(10))
            return false;

        try
        {
            value = _pawnModule.ReadSmu(address);
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            Mutexes.ReleasePciBus();
        }
    }

    public override void Close()
    {
        base.Close();
        _temperatureStream?.Close();
        _pawnModule.Close();
    }

    // ReSharper disable InconsistentNaming
    private const uint CLOCK_POWER_TIMING_CONTROL_0_REGISTER = 0xD4;
    private const uint REPORTED_TEMPERATURE_CONTROL_REGISTER = 0xA4;
    private const uint COFVID_STATUS = 0xC0010071;
    private const uint SMU_REPORTED_TEMP_CTRL_OFFSET = 0xD8200CA4;
    // ReSharper restore InconsistentNaming
}
