// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Threading;

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc;

internal class Nct677X : ISuperIO
{
    private readonly struct TemperatureSourceData
    {
        public TemperatureSourceData(Enum source, ushort register, ushort halfRegister = 0, int halfBit = -1, ushort sourceRegister = 0, ushort? alternateRegister = null)
        {
            Source = source;
            Register = register;
            HalfRegister = halfRegister;
            HalfBit = halfBit;
            SourceRegister = sourceRegister;
            AlternateRegister = alternateRegister;
        }
        public readonly Enum Source;
        public readonly ushort Register;
        public readonly ushort HalfRegister;
        public readonly int HalfBit;
        public readonly ushort SourceRegister;
        public readonly ushort? AlternateRegister;
    }

    private readonly ushort[] _fanCountRegister;
    private readonly ushort[] _fanRpmRegister;
    private readonly byte[] _initialFanControlMode = new byte[7];
    private readonly byte[] _initialFanPwmCommand = new byte[7];
    private readonly bool _isNuvotonVendor;
    private readonly LpcPort _lpcPort;
    private readonly int _maxFanCount;
    private readonly int _minFanCount;
    private readonly int _minFanRpm;
    private readonly ushort _port;
    private readonly bool[] _restoreDefaultFanControlRequired = new bool[7];
    private readonly byte _revision;
    private readonly TemperatureSourceData[] _temperaturesSource;
    private readonly ushort _vBatMonitorControlRegister;
    private readonly ushort[] _voltageRegisters;
    private readonly ushort _voltageVBatRegister;

    public Nct677X(Chip chip, byte revision, ushort port, LpcPort lpcPort)
    {
        Chip = chip;
        _revision = revision;
        _port = port;
        _lpcPort = lpcPort;

        if (chip == Chip.NCT610XD)
        {
            VENDOR_ID_HIGH_REGISTER = 0x80FE;
            VENDOR_ID_LOW_REGISTER = 0x00FE;

            FAN_PWM_OUT_REG = new ushort[] { 0x04A, 0x04B, 0x04C };
            FAN_PWM_COMMAND_REG = new ushort[] { 0x119, 0x129, 0x139 };
            FAN_CONTROL_MODE_REG = new ushort[] { 0x113, 0x123, 0x133 };

            _vBatMonitorControlRegister = 0x0318;
        }
        else if (chip is Chip.NCT6683D or Chip.NCT6686D or Chip.NCT6687D)
        {
            FAN_PWM_OUT_REG = new ushort[] { 0x160, 0x161, 0x162, 0x163, 0x164, 0x165, 0x166, 0x167 };
            FAN_PWM_COMMAND_REG = new ushort[] { 0xA28, 0xA29, 0xA2A, 0xA2B, 0xA2C, 0xA2D, 0xA2E, 0xA2F };
            FAN_CONTROL_MODE_REG = new ushort[] { 0xA00, 0xA00, 0xA00, 0xA00, 0xA00, 0xA00, 0xA00, 0xA00 };
            FAN_PWM_REQUEST_REG = new ushort[] { 0xA01, 0xA01, 0xA01, 0xA01, 0xA01, 0xA01, 0xA01, 0xA01 };
        }
        else
        {
            VENDOR_ID_HIGH_REGISTER = 0x804F;
            VENDOR_ID_LOW_REGISTER = 0x004F;

            FAN_PWM_OUT_REG = chip is Chip.NCT6797D or Chip.NCT6798D or Chip.NCT6799D
                ? new ushort[] { 0x001, 0x003, 0x011, 0x013, 0x015, 0xA09, 0xB09 }
                : new ushort[] { 0x001, 0x003, 0x011, 0x013, 0x015, 0x017, 0x029 };

            FAN_PWM_COMMAND_REG = new ushort[] { 0x109, 0x209, 0x309, 0x809, 0x909, 0xA09, 0xB09 };
            FAN_CONTROL_MODE_REG = new ushort[] { 0x102, 0x202, 0x302, 0x802, 0x902, 0xA02, 0xB02 };

            _vBatMonitorControlRegister = 0x005D;
        }

        _isNuvotonVendor = IsNuvotonVendor();

        if (!_isNuvotonVendor)
            return;

        switch (chip)
        {
            case Chip.NCT6771F:
            case Chip.NCT6776F:
                if (chip == Chip.NCT6771F)
                {
                    Fans = new float?[4];

                    // min value RPM value with 16-bit fan counter
                    _minFanRpm = (int)(1.35e6 / 0xFFFF);
                }
                else
                {
                    Fans = new float?[5];

                    // min value RPM value with 13-bit fan counter
                    _minFanRpm = (int)(1.35e6 / 0x1FFF);
                }

                _fanRpmRegister = new ushort[5];
                for (int i = 0; i < _fanRpmRegister.Length; i++)
                    _fanRpmRegister[i] = (ushort)(0x656 + (i << 1));

                Controls = new float?[3];

                Voltages = new float?[9];
                _voltageRegisters = new ushort[] { 0x020, 0x021, 0x022, 0x023, 0x024, 0x025, 0x026, 0x550, 0x551 };
                _voltageVBatRegister = 0x551;
                _temperaturesSource = new TemperatureSourceData[]
                {
                    new(chip == Chip.NCT6771F ?  SourceNct6771F.PECI_0 : SourceNct6776F.PECI_0, 0x027, 0, -1, 0x621),
                    new(chip == Chip.NCT6771F ?  SourceNct6771F.CPUTIN : SourceNct6776F.CPUTIN, 0x073, 0x074, 7, 0x100),
                    new(chip == Chip.NCT6771F ?  SourceNct6771F.AUXTIN : SourceNct6776F.AUXTIN, 0x075, 0x076, 7, 0x200),
                    new(chip == Chip.NCT6771F ?  SourceNct6771F.SYSTIN : SourceNct6776F.SYSTIN, 0x077, 0x078, 7, 0x300),
                    new(null, 0x150, 0x151, 7, 0x622),
                    new(null, 0x250, 0x251, 7, 0x623),
                    new(null, 0x62B, 0x62E, 0, 0x624),
                    new(null, 0x62C, 0x62E, 1, 0x625),
                    new(null, 0x62D, 0x62E, 2, 0x626)
                };

                Temperatures = new float?[4];
                break;

            case Chip.NCT6779D:
            case Chip.NCT6791D:
            case Chip.NCT6792D:
            case Chip.NCT6792DA:
            case Chip.NCT6793D:
            case Chip.NCT6795D:
            case Chip.NCT6796D:
            case Chip.NCT6796DR:
            case Chip.NCT6797D:
            case Chip.NCT6798D:
            case Chip.NCT6799D:
                switch (chip)
                {
                    case Chip.NCT6779D:
                        Fans = new float?[5];
                        Controls = new float?[5];
                        break;

                    case Chip.NCT6796DR:
                    case Chip.NCT6797D:
                    case Chip.NCT6798D:
                    case Chip.NCT6799D:
                        Fans = new float?[7];
                        Controls = new float?[7];
                        break;

                    default:
                        Fans = new float?[6];
                        Controls = new float?[6];
                        break;
                }

                _fanCountRegister = new ushort[] { 0x4B0, 0x4B2, 0x4B4, 0x4B6, 0x4B8, 0x4BA, 0x4CC };

                // max value for 13-bit fan counter
                _maxFanCount = 0x1FFF;

                // min value that could be transferred to 16-bit RPM registers
                _minFanCount = 0x15;

                Voltages = new float?[15];
                _voltageRegisters = new ushort[] { 0x480, 0x481, 0x482, 0x483, 0x484, 0x485, 0x486, 0x487, 0x488, 0x489, 0x48A, 0x48B, 0x48C, 0x48D, 0x48E };
                _voltageVBatRegister = 0x488;
                var temperaturesSources = new List<TemperatureSourceData>();

                switch (chip)
                {
                    case Chip.NCT6796D:
                    case Chip.NCT6796DR:
                    case Chip.NCT6797D:
                    case Chip.NCT6798D:
                    case Chip.NCT6799D:
                        temperaturesSources.AddRange(new TemperatureSourceData[]
                        {
                            new(SourceNct67Xxd.PECI_0, 0x073, 0x074, 7, 0x100),
                            new(SourceNct67Xxd.CPUTIN, 0x075, 0x076, 7, 0x200, 0x491),
                            new(SourceNct67Xxd.SYSTIN, 0x077, 0x078, 7, 0x300, 0x490),
                            new(SourceNct67Xxd.AUXTIN0, 0x079, 0x07A, 7, 0x800, 0x492),
                            new(SourceNct67Xxd.AUXTIN1, 0x07B, 0x07C, 7, 0x900, 0x493),
                            new(SourceNct67Xxd.AUXTIN2, 0x07D, 0x07E, 7, 0xA00, 0x494),
                            new(SourceNct67Xxd.AUXTIN3, 0x4A0, 0x49E, 6, 0xB00, 0x495),
                            new(SourceNct67Xxd.AUXTIN4, 0x027, 0, -1, 0x621),
                            new(SourceNct67Xxd.TSENSOR, 0x4A2, 0x4A1, 7, 0xC00, 0x496),
                            new(SourceNct67Xxd.SMBUSMASTER0, 0x150, 0x151, 7, 0x622),
                            new(SourceNct67Xxd.SMBUSMASTER1, 0x670, 0, -1, 0xC26),
                            new(SourceNct67Xxd.PECI_1, 0x672, 0, -1, 0xC27),
                            new(SourceNct67Xxd.PCH_CHIP_CPU_MAX_TEMP, 0x674, 0, -1, 0xC28, 0x400),
                            new(SourceNct67Xxd.PCH_CHIP_TEMP, 0x676, 0, -1, 0xC29, 0x401),
                            new(SourceNct67Xxd.PCH_CPU_TEMP,  0x678, 0, -1, 0xC2A, 0x402),
                            new(SourceNct67Xxd.PCH_MCH_TEMP, 0x67A, 0, -1, 0xC2B, 0x404),
                            new(SourceNct67Xxd.AGENT0_DIMM0, 0),
                            new(SourceNct67Xxd.AGENT0_DIMM1, 0),
                            new(SourceNct67Xxd.AGENT1_DIMM0, 0),
                            new(SourceNct67Xxd.AGENT1_DIMM1, 0),
                            new(SourceNct67Xxd.BYTE_TEMP0, 0),
                            new(SourceNct67Xxd.BYTE_TEMP1, 0),
                            new(SourceNct67Xxd.PECI_0_CAL, 0),
                            new(SourceNct67Xxd.PECI_1_CAL, 0),
                            new(SourceNct67Xxd.VIRTUAL_TEMP, 0)
                        });
                        break;

                    default:
                        temperaturesSources.AddRange(new TemperatureSourceData[]
                        {
                            new(SourceNct67Xxd.PECI_0, 0x027, 0, -1, 0x621),
                            new(SourceNct67Xxd.CPUTIN, 0x073, 0x074, 7, 0x100, 0x491),
                            new(SourceNct67Xxd.SYSTIN, 0x075, 0x076, 7, 0x200, 0x490),
                            new(SourceNct67Xxd.AUXTIN0, 0x077, 0x078, 7, 0x300, 0x492),
                            new(SourceNct67Xxd.AUXTIN1, 0x079, 0x07A, 7, 0x800, 0x493),
                            new(SourceNct67Xxd.AUXTIN2, 0x07B, 0x07C, 7, 0x900, 0x494),
                            new(SourceNct67Xxd.AUXTIN3, 0x150, 0x151, 7, 0x622, 0x495)
                        });
                        break;
                }

                _temperaturesSource = temperaturesSources.ToArray();
                Temperatures = new float?[_temperaturesSource.Length];
                break;

            case Chip.NCT610XD:
                Fans = new float?[3];
                Controls = new float?[3];

                _fanRpmRegister = new ushort[3];
                for (int i = 0; i < _fanRpmRegister.Length; i++)
                    _fanRpmRegister[i] = (ushort)(0x030 + (i << 1));

                // min value RPM value with 13-bit fan counter
                _minFanRpm = (int)(1.35e6 / 0x1FFF);

                Voltages = new float?[9];
                _voltageRegisters = new ushort[] { 0x300, 0x301, 0x302, 0x303, 0x304, 0x305, 0x307, 0x308, 0x309 };
                _voltageVBatRegister = 0x308;
                Temperatures = new float?[4];
                _temperaturesSource = new TemperatureSourceData[] {
                    new(SourceNct610X.PECI_0, 0x027, 0, -1, 0x621),
                    new(SourceNct610X.SYSTIN, 0x018, 0x01B, 7, 0x100, 0x018),
                    new(SourceNct610X.CPUTIN, 0x019, 0x11B, 7, 0x200, 0x019),
                    new(SourceNct610X.AUXTIN, 0x01A, 0x21B, 7, 0x300, 0x01A)
                };
                break;

            case Chip.NCT6683D:
            case Chip.NCT6686D:
            case Chip.NCT6687D:
                Fans = new float?[8];
                Controls = new float?[8];
                Voltages = new float?[14];
                Temperatures = new float?[7];

                // CPU
                // System
                // MOS
                // PCH
                // CPU Socket
                // PCIE_1
                // M2_1
                _temperaturesSource = new TemperatureSourceData[] {
                    new(null, 0x100),
                    new(null, 0x102),
                    new(null, 0x104),
                    new(null, 0x106),
                    new(null, 0x108),
                    new(null, 0x10A),
                    new(null, 0x10C)
                };

                // VIN0 +12V
                // VIN1 +5V
                // VIN2 VCore
                // VIN3 SIO
                // VIN4 DRAM
                // VIN5 CPU IO
                // VIN6 CPU SA
                // VIN7 SIO
                // 3VCC I/O +3.3
                // SIO VTT
                // SIO VREF
                // SIO VSB
                // SIO AVSB
                // SIO VBAT
                _voltageRegisters = new ushort[] { 0x120, 0x122, 0x124, 0x126, 0x128, 0x12A, 0x12C, 0x12E, 0x130, 0x13A, 0x13E, 0x136, 0x138, 0x13C };

                // CPU Fan
                // PUMP Fan
                // SYS Fan 1
                // SYS Fan 2
                // SYS Fan 3
                // SYS Fan 4
                // SYS Fan 5
                // SYS Fan 6
                _fanRpmRegister = new ushort[] { 0x140, 0x142, 0x144, 0x146, 0x148, 0x14A, 0x14C, 0x14E };

                _restoreDefaultFanControlRequired = new bool[_fanRpmRegister.Length];
                _initialFanControlMode = new byte[_fanRpmRegister.Length];
                _initialFanPwmCommand = new byte[_fanRpmRegister.Length];

                // initialize
                const ushort initRegister = 0x180;
                byte data = ReadByte(initRegister);
                if ((data & 0x80) == 0)
                {
                    WriteByte(initRegister, (byte)(data | 0x80));
                }

                // enable SIO voltage
                WriteByte(0x1BB, 0x61);
                WriteByte(0x1BC, 0x62);
                WriteByte(0x1BD, 0x63);
                WriteByte(0x1BE, 0x64);
                WriteByte(0x1BF, 0x65);
                break;
        }
    }

    public Chip Chip { get; }

    public float?[] Controls { get; } = Array.Empty<float?>();

    public float?[] Fans { get; } = Array.Empty<float?>();

    public float?[] Temperatures { get; } = Array.Empty<float?>();

    public float?[] Voltages { get; } = Array.Empty<float?>();

    public byte? ReadGpio(int index)
    {
        return null;
    }

    public void WriteGpio(int index, byte value)
    { }

    public void SetControl(int index, byte? value)
    {
        if (!_isNuvotonVendor)
            return;

        if (index < 0 || index >= Controls.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (!Mutexes.WaitIsaBus(10))
            return;

        if (value.HasValue)
        {
            SaveDefaultFanControl(index);

            if (Chip is not Chip.NCT6683D and not Chip.NCT6686D and not Chip.NCT6687D)
            {
                // set manual mode
                WriteByte(FAN_CONTROL_MODE_REG[index], 0);

                // set output value
                WriteByte(FAN_PWM_COMMAND_REG[index], value.Value);
            }
            else
            {
                // Manual mode, bit(1 : set, 0 : unset)
                // bit 0 : CPU Fan
                // bit 1 : PUMP Fan
                // bit 2 : SYS Fan 1
                // bit 3 : SYS Fan 2
                // bit 4 : SYS Fan 3
                // bit 5 : SYS Fan 4
                // bit 6 : SYS Fan 5
                // bit 7 : SYS Fan 6

                byte mode = ReadByte(FAN_CONTROL_MODE_REG[index]);
                byte bitMask = (byte)(0x01 << index);
                mode = (byte)(mode | bitMask);
                WriteByte(FAN_CONTROL_MODE_REG[index], mode);

                WriteByte(FAN_PWM_REQUEST_REG[index], 0x80);
                Thread.Sleep(50);

                WriteByte(FAN_PWM_COMMAND_REG[index], value.Value);
                WriteByte(FAN_PWM_REQUEST_REG[index], 0x40);
                Thread.Sleep(50);
            }
        }
        else
        {
            RestoreDefaultFanControl(index);
        }

        Mutexes.ReleaseIsaBus();
    }

    public void Update()
    {
        if (!_isNuvotonVendor)
            return;

        if (!Mutexes.WaitIsaBus(10))
            return;

        DisableIOSpaceLock();

        for (int i = 0; i < Voltages.Length; i++)
        {
            if (Chip is not Chip.NCT6683D and not Chip.NCT6686D and not Chip.NCT6687D)
            {
                float value = 0.008f * ReadByte(_voltageRegisters[i]);
                bool valid = value > 0;

                // check if battery voltage monitor is enabled
                if (valid && _voltageRegisters[i] == _voltageVBatRegister)
                    valid = (ReadByte(_vBatMonitorControlRegister) & 0x01) > 0;

                Voltages[i] = valid ? value : null;
            }
            else
            {
                float value = 0.001f * ((16 * ReadByte(_voltageRegisters[i])) + (ReadByte((ushort)(_voltageRegisters[i] + 1)) >> 4));

                Voltages[i] = i switch
                {
                    // 12V
                    0 => value * 12.0f,
                    // 5V
                    1 => value * 5.0f,
                    // DRAM
                    4 => value * 2.0f,
                    _ => value
                };
            }
        }

        System.Diagnostics.Debug.WriteLine("Updating temperatures.");
        long temperatureSourceMask = 0;
        for (int i = 0; i < _temperaturesSource.Length; i++)
        {
            TemperatureSourceData ts = _temperaturesSource[i];
            int value;
            SourceNct67Xxd source;
            float? temperature;

            switch (Chip)
            {
                case Chip.NCT6687D:
                case Chip.NCT6686D:
                case Chip.NCT6683D:
                    value = (sbyte)ReadByte(ts.Register);
                    int half = (ReadByte((ushort)(ts.Register + 1)) >> 7) & 0x1;
                    Temperatures[i] = value + (0.5f * half);
                    break;

                case Chip.NCT6796D:
                case Chip.NCT6796DR:
                case Chip.NCT6797D:
                case Chip.NCT6798D:
                case Chip.NCT6799D:
                    if (_temperaturesSource[i].Register == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("Temperature register {0} skipped, address 0.", i);
                        continue;
                    }

                    value = (sbyte)ReadByte(_temperaturesSource[i].Register) << 1;
                    System.Diagnostics.Debug.WriteLine("Temperature register {0} at 0x{1:X3} value (integer): {2}/2", i, ts.Register, value);
                    if (_temperaturesSource[i].HalfBit > 0)
                    {
                        value |= (ReadByte(_temperaturesSource[i].HalfRegister) >> ts.HalfBit) & 0x1;
                        System.Diagnostics.Debug.WriteLine("Temperature register {0} value updated from 0x{1:X3} (fractional): {2}/2", i, ts.HalfRegister, value);
                    }

                    if (ts.SourceRegister > 0)
                    {
                        source = (SourceNct67Xxd)(ReadByte(ts.SourceRegister) & 0x1F);
                        System.Diagnostics.Debug.WriteLine("Temperature register {0} source at 0x{1:X3}: {2:G} ({2:D})", i, ts.SourceRegister, source);
                    }
                    else
                    {
                        source = (SourceNct67Xxd)ts.Source;
                        System.Diagnostics.Debug.WriteLine("Temperature register {0} source register is 0, source set to: {1:G} ({1:D})", i, source);
                    }

                    // Skip reading when already filled, because later values are without fractional
                    if ((temperatureSourceMask & (1L << (byte)source)) > 0)
                    {
                        System.Diagnostics.Debug.WriteLine("Temperature register {0} discarded, because source seen before.", i);
                        continue;
                    }

                    temperature = 0.5f * value;
                    System.Diagnostics.Debug.WriteLine("Temperature register {0} final temperature: {1}.", i, temperature);
                    if (temperature is > 125 or < -55)
                    {
                        temperature = null;
                        System.Diagnostics.Debug.WriteLine("Temperature register {0} discarded: Out of range.", i);
                    }
                    else
                    {
                        temperatureSourceMask |= 1L << (byte)source;
                        System.Diagnostics.Debug.WriteLine("Temperature register {0} accepted.", i);
                    }

                    for (int j = 0; j < Temperatures.Length; j++)
                    {
                        if ((SourceNct67Xxd)_temperaturesSource[j].Source == source)
                        {
                            Temperatures[j] = temperature;
                            System.Diagnostics.Debug.WriteLine("Temperature register {0}, value from source {1:G} ({1:D}), written at position {2}.", i, _temperaturesSource[j].Source, j);
                        }
                    }
                    break;

                default:
                    value = (sbyte)ReadByte(ts.Register) << 1;
                    if (ts.HalfBit > 0)
                    {
                        value |= (ReadByte(ts.HalfRegister) >> ts.HalfBit) & 0x1;
                    }

                    source = (SourceNct67Xxd)ReadByte(ts.SourceRegister);
                    temperatureSourceMask |= 1L << (byte)source;

                    temperature = 0.5f * value;
                    if (temperature is > 125 or < -55)
                        temperature = null;

                    for (int j = 0; j < Temperatures.Length; j++)
                    {
                        if ((SourceNct67Xxd)_temperaturesSource[j].Source == source)
                            Temperatures[j] = temperature;
                    }
                    break;
            }
        }

        for (int i = 0; i < _temperaturesSource.Length; i++)
        {
            TemperatureSourceData ts = _temperaturesSource[i];
            if (!ts.AlternateRegister.HasValue)
            {
                System.Diagnostics.Debug.WriteLine("Alternate temperature register for temperature {0}, {1:G} ({1:D}), skipped, because address is null.", i, ts.Source);
                continue;
            }

            if ((temperatureSourceMask & (1L << (byte)(SourceNct67Xxd)ts.Source)) > 0)
            {
                System.Diagnostics.Debug.WriteLine("Alternate temperature register for temperature {0}, {1:G} ({1:D}), at 0x{2:X3} skipped, because value already set.", i, ts.Source, ts.AlternateRegister.Value);
                continue;
            }

            float? temperature = (sbyte)ReadByte(ts.AlternateRegister.Value);
            System.Diagnostics.Debug.WriteLine("Alternate temperature register for temperature {0}, {1:G} ({1:D}), at 0x{2:X3} final temperature: {3}.", i, ts.Source, ts.AlternateRegister.Value, temperature);

            if (temperature is > 125 or <= 0)
            {
                temperature = null;
                System.Diagnostics.Debug.WriteLine("Alternate Temperature register for temperature {0}, {1:G} ({1:D}), discarded: Out of range.", i, ts.Source);
            }

            Temperatures[i] = temperature;
        }

        for (int i = 0; i < Fans.Length; i++)
        {
            if (Chip is not Chip.NCT6683D and not Chip.NCT6686D and not Chip.NCT6687D)
            {
                if (_fanCountRegister != null)
                {
                    byte high = ReadByte(_fanCountRegister[i]);
                    byte low = ReadByte((ushort)(_fanCountRegister[i] + 1));

                    int count = (high << 5) | (low & 0x1F);
                    if (count < _maxFanCount)
                    {
                        if (count >= _minFanCount)
                        {
                            Fans[i] = 1.35e6f / count;
                        }
                        else
                        {
                            Fans[i] = null;
                        }
                    }
                    else
                    {
                        Fans[i] = 0;
                    }
                }
                else
                {
                    byte high = ReadByte(_fanRpmRegister[i]);
                    byte low = ReadByte((ushort)(_fanRpmRegister[i] + 1));
                    int value = (high << 8) | low;

                    Fans[i] = value > _minFanRpm ? value : 0;
                }
            }
            else
            {
                Fans[i] = (ReadByte(_fanRpmRegister[i]) << 8) | ReadByte((ushort)(_fanRpmRegister[i] + 1));
            }
        }

        for (int i = 0; i < Controls.Length; i++)
        {
            if (Chip is not Chip.NCT6683D and not Chip.NCT6686D and not Chip.NCT6687D)
            {
                int value = ReadByte(FAN_PWM_OUT_REG[i]);
                Controls[i] = value / 2.55f;
            }
            else
            {
                int value = ReadByte(FAN_PWM_OUT_REG[i]);
                Controls[i] = (float)Math.Round(value / 2.55f);
            }
        }

        Mutexes.ReleaseIsaBus();
    }

    public string GetReport()
    {
        StringBuilder r = new();

        r.AppendLine("LPC " + GetType().Name);
        r.AppendLine();
        r.Append("Chip Id: 0x");
        r.AppendLine(Chip.ToString("X"));
        r.Append("Chip Revision: 0x");
        r.AppendLine(_revision.ToString("X", CultureInfo.InvariantCulture));
        r.Append("Base Address: 0x");
        r.AppendLine(_port.ToString("X4", CultureInfo.InvariantCulture));
        r.AppendLine();

        if (!Mutexes.WaitIsaBus(100))
            return r.ToString();

        ushort[] addresses =
        {
            0x000,
            0x010,
            0x020,
            0x030,
            0x040,
            0x050,
            0x060,
            0x070,
            0x0F0,
            0x100,
            0x110,
            0x120,
            0x130,
            0x140,
            0x150,
            0x200,
            0x210,
            0x220,
            0x230,
            0x240,
            0x250,
            0x260,
            0x300,
            0x320,
            0x330,
            0x340,
            0x360,
            0x400,
            0x410,
            0x420,
            0x440,
            0x450,
            0x460,
            0x480,
            0x490,
            0x4B0,
            0x4C0,
            0x4F0,
            0x500,
            0x550,
            0x560,
            0x600,
            0x610,
            0x620,
            0x630,
            0x640,
            0x650,
            0x660,
            0x670,
            0x700,
            0x710,
            0x720,
            0x730,
            0x800,
            0x820,
            0x830,
            0x840,
            0x900,
            0x920,
            0x930,
            0x940,
            0x960,
            0xA00,
            0xA10,
            0xA20,
            0xA30,
            0xA40,
            0xA50,
            0xA60,
            0xA70,
            0xB00,
            0xB10,
            0xB20,
            0xB30,
            0xB50,
            0xB60,
            0xB70,
            0xC00,
            0xC10,
            0xC20,
            0xC30,
            0xC50,
            0xC60,
            0xC70,
            0xD00,
            0xD10,
            0xD20,
            0xD30,
            0xD50,
            0xD60,
            0xE00,
            0xE10,
            0xE20,
            0xE30,
            0xF00,
            0xF10,
            0xF20,
            0xF30,
            0x8040,
            0x80F0
        };

        r.AppendLine("Hardware Monitor Registers");
        r.AppendLine();
        r.AppendLine("        00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F");
        r.AppendLine();

        if (Chip is not Chip.NCT6683D and not Chip.NCT6686D and not Chip.NCT6687D)
        {
            foreach (ushort address in addresses)
            {
                r.Append(" ");
                r.Append(address.ToString("X4", CultureInfo.InvariantCulture));
                r.Append("  ");
                for (ushort j = 0; j <= 0xF; j++)
                {
                    r.Append(" ");
                    r.Append(ReadByte((ushort)(address | j)).ToString("X2", CultureInfo.InvariantCulture));
                }

                r.AppendLine();
            }
        }
        else
        {
            for (int i = 0; i <= 0xFF; i++)
            {
                r.Append(" ");
                r.Append((i << 4).ToString("X4", CultureInfo.InvariantCulture));
                r.Append("  ");
                for (int j = 0; j <= 0xF; j++)
                {
                    ushort address = (ushort)(i << 4 | j);
                    r.Append(" ");
                    r.Append(ReadByte(address).ToString("X2", CultureInfo.InvariantCulture));
                }

                r.AppendLine();
            }
        }

        r.AppendLine();

        Mutexes.ReleaseIsaBus();

        return r.ToString();
    }

    private byte ReadByte(ushort address)
    {
        if (Chip is not Chip.NCT6683D and not Chip.NCT6686D and not Chip.NCT6687D)
        {
            byte bank = (byte)(address >> 8);
            byte register = (byte)(address & 0xFF);
            Ring0.WriteIoPort(_port + ADDRESS_REGISTER_OFFSET, BANK_SELECT_REGISTER);
            Ring0.WriteIoPort(_port + DATA_REGISTER_OFFSET, bank);
            Ring0.WriteIoPort(_port + ADDRESS_REGISTER_OFFSET, register);
            return Ring0.ReadIoPort(_port + DATA_REGISTER_OFFSET);
        }

        byte page = (byte)(address >> 8);
        byte index = (byte)(address & 0xFF);
        Ring0.WriteIoPort(_port + EC_SPACE_PAGE_REGISTER_OFFSET, EC_SPACE_PAGE_SELECT);
        Ring0.WriteIoPort(_port + EC_SPACE_PAGE_REGISTER_OFFSET, page);
        Ring0.WriteIoPort(_port + EC_SPACE_INDEX_REGISTER_OFFSET, index);
        return Ring0.ReadIoPort(_port + EC_SPACE_DATA_REGISTER_OFFSET);
    }

    private void WriteByte(ushort address, byte value)
    {
        if (Chip is not Chip.NCT6683D and not Chip.NCT6686D and not Chip.NCT6687D)
        {
            byte bank = (byte)(address >> 8);
            byte register = (byte)(address & 0xFF);
            Ring0.WriteIoPort(_port + ADDRESS_REGISTER_OFFSET, BANK_SELECT_REGISTER);
            Ring0.WriteIoPort(_port + DATA_REGISTER_OFFSET, bank);
            Ring0.WriteIoPort(_port + ADDRESS_REGISTER_OFFSET, register);
            Ring0.WriteIoPort(_port + DATA_REGISTER_OFFSET, value);
        }
        else
        {
            byte page = (byte)(address >> 8);
            byte index = (byte)(address & 0xFF);
            Ring0.WriteIoPort(_port + EC_SPACE_PAGE_REGISTER_OFFSET, EC_SPACE_PAGE_SELECT);
            Ring0.WriteIoPort(_port + EC_SPACE_PAGE_REGISTER_OFFSET, page);
            Ring0.WriteIoPort(_port + EC_SPACE_INDEX_REGISTER_OFFSET, index);
            Ring0.WriteIoPort(_port + EC_SPACE_DATA_REGISTER_OFFSET, value);
        }
    }

    private bool IsNuvotonVendor()
    {
        return Chip is Chip.NCT6683D or Chip.NCT6686D or Chip.NCT6687D || ((ReadByte(VENDOR_ID_HIGH_REGISTER) << 8) | ReadByte(VENDOR_ID_LOW_REGISTER)) == NUVOTON_VENDOR_ID;
    }

    private void SaveDefaultFanControl(int index)
    {
        if (!_restoreDefaultFanControlRequired[index])
        {
            if (Chip is not Chip.NCT6683D and not Chip.NCT6686D and not Chip.NCT6687D)
            {
                _initialFanControlMode[index] = ReadByte(FAN_CONTROL_MODE_REG[index]);
            }
            else
            {
                byte mode = ReadByte(FAN_CONTROL_MODE_REG[index]);
                byte bitMask = (byte)(0x01 << index);
                _initialFanControlMode[index] = (byte)(mode & bitMask);
            }

            _initialFanPwmCommand[index] = ReadByte(FAN_PWM_COMMAND_REG[index]);
            _restoreDefaultFanControlRequired[index] = true;
        }
    }

    private void RestoreDefaultFanControl(int index)
    {
        if (_restoreDefaultFanControlRequired[index])
        {
            if (Chip is not Chip.NCT6683D and not Chip.NCT6686D and not Chip.NCT6687D)
            {
                WriteByte(FAN_CONTROL_MODE_REG[index], _initialFanControlMode[index]);
                WriteByte(FAN_PWM_COMMAND_REG[index], _initialFanPwmCommand[index]);
            }
            else
            {
                byte mode = ReadByte(FAN_CONTROL_MODE_REG[index]);
                mode = (byte)(mode & ~_initialFanControlMode[index]);
                WriteByte(FAN_CONTROL_MODE_REG[index], mode);

                WriteByte(FAN_PWM_REQUEST_REG[index], 0x80);
                Thread.Sleep(50);

                WriteByte(FAN_PWM_COMMAND_REG[index], _initialFanPwmCommand[index]);
                WriteByte(FAN_PWM_REQUEST_REG[index], 0x40);
                Thread.Sleep(50);
            }

            _restoreDefaultFanControlRequired[index] = false;
        }
    }

    private void DisableIOSpaceLock()
    {
        if (Chip is not Chip.NCT6791D and
            not Chip.NCT6792D and
            not Chip.NCT6792DA and
            not Chip.NCT6793D and
            not Chip.NCT6795D and
            not Chip.NCT6796D and
            not Chip.NCT6796DR and
            not Chip.NCT6797D and
            not Chip.NCT6798D and
            not Chip.NCT6799D)
        {
            return;
        }

        // the lock is disabled already if the vendor ID can be read
        if (IsNuvotonVendor())
            return;

        _lpcPort.WinbondNuvotonFintekEnter();
        _lpcPort.NuvotonDisableIOSpaceLock();
        _lpcPort.WinbondNuvotonFintekExit();
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private enum SourceNct6771F : byte
    {
        SYSTIN = 1,
        CPUTIN = 2,
        AUXTIN = 3,
        PECI_0 = 5
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private enum SourceNct6776F : byte
    {
        SYSTIN = 1,
        CPUTIN = 2,
        AUXTIN = 3,
        PECI_0 = 12
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private enum SourceNct67Xxd : byte
    {
        SYSTIN = 1,
        CPUTIN = 2,
        AUXTIN0 = 3,
        AUXTIN1 = 4,
        AUXTIN2 = 5,
        AUXTIN3 = 6,
        AUXTIN4 = 7,
        SMBUSMASTER0 = 8,
        SMBUSMASTER1 = 9,
        TSENSOR = 10,
        PECI_0 = 16,
        PECI_1 = 17,
        PCH_CHIP_CPU_MAX_TEMP = 18,
        PCH_CHIP_TEMP = 19,
        PCH_CPU_TEMP = 20,
        PCH_MCH_TEMP = 21,
        AGENT0_DIMM0 = 22,
        AGENT0_DIMM1 = 23,
        AGENT1_DIMM0 = 24,
        AGENT1_DIMM1 = 25,
        BYTE_TEMP0 = 26,
        BYTE_TEMP1 = 27,
        PECI_0_CAL = 28,
        PECI_1_CAL = 29,
        VIRTUAL_TEMP = 31
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private enum SourceNct610X : byte
    {
        SYSTIN = 1,
        CPUTIN = 2,
        AUXTIN = 3,
        PECI_0 = 12
    }

    // ReSharper disable InconsistentNaming
    private const uint ADDRESS_REGISTER_OFFSET = 0x05;
    private const byte BANK_SELECT_REGISTER = 0x4E;
    private const uint DATA_REGISTER_OFFSET = 0x06;

    // NCT668X
    private const uint EC_SPACE_PAGE_REGISTER_OFFSET = 0x04;
    private const uint EC_SPACE_INDEX_REGISTER_OFFSET = 0x05;
    private const uint EC_SPACE_DATA_REGISTER_OFFSET = 0x06;
    private const byte EC_SPACE_PAGE_SELECT = 0xFF;

    private const ushort NUVOTON_VENDOR_ID = 0x5CA3;

    private readonly ushort[] FAN_CONTROL_MODE_REG;
    private readonly ushort[] FAN_PWM_COMMAND_REG;
    private readonly ushort[] FAN_PWM_OUT_REG;
    private readonly ushort[] FAN_PWM_REQUEST_REG;

    private readonly ushort VENDOR_ID_HIGH_REGISTER;
    private readonly ushort VENDOR_ID_LOW_REGISTER;

    // ReSharper restore InconsistentNaming
}
