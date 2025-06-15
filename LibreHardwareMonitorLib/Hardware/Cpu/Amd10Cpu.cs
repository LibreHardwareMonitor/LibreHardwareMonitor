// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace LibreHardwareMonitor.Hardware.Cpu;

internal sealed class Amd10Cpu : AmdCpu
{
    private readonly Sensor _busClock;
    private readonly Sensor[] _coreClocks;
    private readonly Sensor _coreTemperature;
    private readonly Sensor _coreVoltage;
    private readonly byte _cStatesIoOffset;
    private readonly Sensor[] _cStatesResidency;
    private readonly bool _hasSmuTemperatureRegister;
    private readonly bool _isSvi2;
    private readonly uint _miscellaneousControlAddress;
    private readonly Sensor _northbridgeVoltage;
    private readonly FileStream _temperatureStream;
    private readonly double _timeStampCounterMultiplier;

    public Amd10Cpu(int processorIndex, CpuId[][] cpuId, ISettings settings) : base(processorIndex, cpuId, settings)
    {
        // AMD family 1Xh processors support only one temperature sensor
        ushort miscellaneousControlDeviceId;
        _coreTemperature = new Sensor("CPU Cores", 0, SensorType.Temperature, this, new[] { new ParameterDescription("Offset [°C]", "Temperature offset.", 0) }, settings);
        _coreVoltage = new Sensor("CPU Cores", 0, SensorType.Voltage, this, settings);
        ActivateSensor(_coreVoltage);
        _northbridgeVoltage = new Sensor("Northbridge", 0, SensorType.Voltage, this, settings);
        ActivateSensor(_northbridgeVoltage);

        _isSvi2 = (_family == 0x15 && _model >= 0x10) || _family == 0x16;

        switch (_family)
        {
            case 0x10:
                miscellaneousControlDeviceId = FAMILY_10H_MISCELLANEOUS_CONTROL_DEVICE_ID;
                break;
            case 0x11:
                miscellaneousControlDeviceId = FAMILY_11H_MISCELLANEOUS_CONTROL_DEVICE_ID;
                break;
            case 0x12:
                miscellaneousControlDeviceId = FAMILY_12H_MISCELLANEOUS_CONTROL_DEVICE_ID;
                break;
            case 0x14:
                miscellaneousControlDeviceId = FAMILY_14H_MISCELLANEOUS_CONTROL_DEVICE_ID;
                break;
            case 0x15:
                switch (_model & 0xF0)
                {
                    case 0x00:
                        miscellaneousControlDeviceId = FAMILY_15H_MODEL_00_MISC_CONTROL_DEVICE_ID;
                        break;
                    case 0x10:
                        miscellaneousControlDeviceId = FAMILY_15H_MODEL_10_MISC_CONTROL_DEVICE_ID;
                        break;
                    case 0x30:
                        miscellaneousControlDeviceId = FAMILY_15H_MODEL_30_MISC_CONTROL_DEVICE_ID;
                        break;
                    case 0x70:
                        miscellaneousControlDeviceId = FAMILY_15H_MODEL_70_MISC_CONTROL_DEVICE_ID;
                        _hasSmuTemperatureRegister = true;
                        break;
                    case 0x60:
                        miscellaneousControlDeviceId = FAMILY_15H_MODEL_60_MISC_CONTROL_DEVICE_ID;
                        _hasSmuTemperatureRegister = true;
                        break;
                    default:
                        miscellaneousControlDeviceId = 0;
                        break;
                }

                break;
            case 0x16:
                miscellaneousControlDeviceId = (_model & 0xF0) switch
                {
                    0x00 => FAMILY_16H_MODEL_00_MISC_CONTROL_DEVICE_ID,
                    0x30 => FAMILY_16H_MODEL_30_MISC_CONTROL_DEVICE_ID,
                    _ => 0
                };
                break;
            default:
                miscellaneousControlDeviceId = 0;
                break;
        }

        // get the pci address for the Miscellaneous Control registers
        _miscellaneousControlAddress = GetPciAddress(MISCELLANEOUS_CONTROL_FUNCTION, miscellaneousControlDeviceId);
        _busClock = new Sensor("Bus Speed", 0, SensorType.Clock, this, settings);
        _coreClocks = new Sensor[_coreCount];
        for (int i = 0; i < _coreClocks.Length; i++)
        {
            _coreClocks[i] = new Sensor(CoreString(i), i + 1, SensorType.Clock, this, settings);
            if (HasTimeStampCounter)
                ActivateSensor(_coreClocks[i]);
        }

        bool corePerformanceBoostSupport = (cpuId[0][0].ExtData[7, 3] & (1 << 9)) > 0;

        // set affinity to the first thread for all frequency estimations
        GroupAffinity previousAffinity = ThreadAffinity.Set(cpuId[0][0].Affinity);

        // disable core performance boost
        Ring0.ReadMsr(HWCR, out uint hwcrEax, out uint hwcrEdx);
        if (corePerformanceBoostSupport)
            Ring0.WriteMsr(HWCR, hwcrEax | (1 << 25), hwcrEdx);

        Ring0.ReadMsr(PERF_CTL_0, out uint ctlEax, out uint ctlEdx);
        Ring0.ReadMsr(PERF_CTR_0, out uint ctrEax, out uint ctrEdx);

        _timeStampCounterMultiplier = EstimateTimeStampCounterMultiplier();

        // restore the performance counter registers
        Ring0.WriteMsr(PERF_CTL_0, ctlEax, ctlEdx);
        Ring0.WriteMsr(PERF_CTR_0, ctrEax, ctrEdx);

        // restore core performance boost
        if (corePerformanceBoostSupport)
            Ring0.WriteMsr(HWCR, hwcrEax, hwcrEdx);

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

        uint addr = Ring0.GetPciAddress(0, 20, 0);
        if (Ring0.ReadPciConfig(addr, 0, out uint dev))
        {
            Ring0.ReadPciConfig(addr, 8, out uint rev);

            _cStatesIoOffset = dev switch
            {
                0x43851002 => (byte)((rev & 0xFF) < 0x40 ? 0xB3 : 0x9C),
                0x780B1022 or 0x790B1022 => 0x9C,
                _ => _cStatesIoOffset
            };
        }

        if (_cStatesIoOffset != 0)
        {
            _cStatesResidency = new[] { new Sensor("CPU Package C2", 0, SensorType.Level, this, settings), new Sensor("CPU Package C3", 1, SensorType.Level, this, settings) };
            ActivateSensor(_cStatesResidency[0]);
            ActivateSensor(_cStatesResidency[1]);
        }

        Update();
    }

    private double EstimateTimeStampCounterMultiplier()
    {
        // preload the function
        EstimateTimeStampCounterMultiplier(0);
        EstimateTimeStampCounterMultiplier(0);

        // estimate the multiplier
        List<double> estimate = new(3);
        for (int i = 0; i < 3; i++)
            estimate.Add(EstimateTimeStampCounterMultiplier(0.025));

        estimate.Sort();
        return estimate[1];
    }

    private double EstimateTimeStampCounterMultiplier(double timeWindow)
    {
        // select event "076h CPU Clocks not Halted" and enable the counter
        Ring0.WriteMsr(PERF_CTL_0,
                       (1 << 22) | // enable performance counter
                       (1 << 17) | // count events in user mode
                       (1 << 16) | // count events in operating-system mode
                       0x76,
                       0x00000000);

        // set the counter to 0
        Ring0.WriteMsr(PERF_CTR_0, 0, 0);

        long ticks = (long)(timeWindow * Stopwatch.Frequency);

        long timeBegin = Stopwatch.GetTimestamp() +
                         (long)Math.Ceiling(0.001 * ticks);

        long timeEnd = timeBegin + ticks;
        while (Stopwatch.GetTimestamp() < timeBegin)
        { }

        Ring0.ReadMsr(PERF_CTR_0, out uint lsbBegin, out uint msbBegin);

        while (Stopwatch.GetTimestamp() < timeEnd)
        { }

        Ring0.ReadMsr(PERF_CTR_0, out uint lsbEnd, out uint msbEnd);
        Ring0.ReadMsr(COFVID_STATUS, out uint eax, out uint _);
        double coreMultiplier = GetCoreMultiplier(eax);

        ulong countBegin = ((ulong)msbBegin << 32) | lsbBegin;
        ulong countEnd = ((ulong)msbEnd << 32) | lsbEnd;

        double coreFrequency = 1e-6 * ((double)(countEnd - countBegin) * Stopwatch.Frequency) / (timeEnd - timeBegin);
        double busFrequency = coreFrequency / coreMultiplier;
        return 0.25 * Math.Round(4 * TimeStampCounterFrequency / busFrequency);
    }

    protected override uint[] GetMsrs()
    {
        return new[] { PERF_CTL_0, PERF_CTR_0, HWCR, P_STATE_0, COFVID_STATUS };
    }

    public override string GetReport()
    {
        StringBuilder r = new();
        r.Append(base.GetReport());
        r.Append("Miscellaneous Control Address: 0x");
        r.AppendLine(_miscellaneousControlAddress.ToString("X", CultureInfo.InvariantCulture));
        r.Append("Time Stamp Counter Multiplier: ");
        r.AppendLine(_timeStampCounterMultiplier.ToString(CultureInfo.InvariantCulture));
        if (_family == 0x14)
        {
            Ring0.ReadPciConfig(_miscellaneousControlAddress, CLOCK_POWER_TIMING_CONTROL_0_REGISTER, out uint value);
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
                Ring0.ReadPciConfig(_miscellaneousControlAddress, CLOCK_POWER_TIMING_CONTROL_0_REGISTER, out uint value);
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
            if (_miscellaneousControlAddress != Interop.Ring0.INVALID_PCI_ADDRESS)
            {
                bool isValueValid = _hasSmuTemperatureRegister
                    ? ReadSmuRegister(SMU_REPORTED_TEMP_CTRL_OFFSET, out uint value)
                    : Ring0.ReadPciConfig(_miscellaneousControlAddress, REPORTED_TEMPERATURE_CONTROL_REGISTER, out value);

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

                if (Ring0.ReadMsr(COFVID_STATUS, out uint curEax, out uint _, _cpuId[i][0].Affinity))
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
            for (int i = 0; i < _cStatesResidency.Length; i++)
            {
                Ring0.WriteIoPort(CSTATES_IO_PORT, (byte)(_cStatesIoOffset + i));
                _cStatesResidency[i].Value = Ring0.ReadIoPort(CSTATES_IO_PORT + 1) / 256f * 100;
            }
        }
    }

    private static bool ReadSmuRegister(uint address, out uint value)
    {
        if (Mutexes.WaitPciBus(10))
        {
            if (!Ring0.WritePciConfig(0, 0xB8, address))
            {
                value = 0;

                Mutexes.ReleasePciBus();
                return false;
            }

            bool result = Ring0.ReadPciConfig(0, 0xBC, out value);

            Mutexes.ReleasePciBus();
            return result;
        }

        value = 0;
        return false;
    }

    public override void Close()
    {
        _temperatureStream?.Close();
        base.Close();
    }

    // ReSharper disable InconsistentNaming
    private const uint CLOCK_POWER_TIMING_CONTROL_0_REGISTER = 0xD4;
    private const uint COFVID_STATUS = 0xC0010071;
    private const uint CSTATES_IO_PORT = 0xCD6;
    private const uint SMU_REPORTED_TEMP_CTRL_OFFSET = 0xD8200CA4;
    private const uint HWCR = 0xC0010015;
    private const byte MISCELLANEOUS_CONTROL_FUNCTION = 3;
    private const uint P_STATE_0 = 0xC0010064;
    private const uint PERF_CTL_0 = 0xC0010000;
    private const uint PERF_CTR_0 = 0xC0010004;
    private const uint REPORTED_TEMPERATURE_CONTROL_REGISTER = 0xA4;

    private const ushort FAMILY_10H_MISCELLANEOUS_CONTROL_DEVICE_ID = 0x1203;
    private const ushort FAMILY_11H_MISCELLANEOUS_CONTROL_DEVICE_ID = 0x1303;
    private const ushort FAMILY_12H_MISCELLANEOUS_CONTROL_DEVICE_ID = 0x1703;
    private const ushort FAMILY_14H_MISCELLANEOUS_CONTROL_DEVICE_ID = 0x1703;
    private const ushort FAMILY_15H_MODEL_00_MISC_CONTROL_DEVICE_ID = 0x1603;
    private const ushort FAMILY_15H_MODEL_10_MISC_CONTROL_DEVICE_ID = 0x1403;
    private const ushort FAMILY_15H_MODEL_30_MISC_CONTROL_DEVICE_ID = 0x141D;
    private const ushort FAMILY_15H_MODEL_60_MISC_CONTROL_DEVICE_ID = 0x1573;
    private const ushort FAMILY_15H_MODEL_70_MISC_CONTROL_DEVICE_ID = 0x15B3;
    private const ushort FAMILY_16H_MODEL_00_MISC_CONTROL_DEVICE_ID = 0x1533;
    private const ushort FAMILY_16H_MODEL_30_MISC_CONTROL_DEVICE_ID = 0x1583;
    // ReSharper restore InconsistentNaming
}
