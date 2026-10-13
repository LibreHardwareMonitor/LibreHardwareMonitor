// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Globalization;
using System.Text;

// ReSharper disable once InconsistentNaming

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc;

internal class F718XX : ISuperIO
{
    private readonly LpcPort _port;
    private readonly ushort _address;
    private readonly bool _hasFanPwmEnable;
    private readonly byte[] _initialFanPwmControl = new byte[4];
    private readonly byte[] _initialFanPwmEnable = new byte[4];
    private readonly ushort[] _initialFanTarget = new ushort[4];
    private readonly bool[] _restoreDefaultFanPwmControlRequired = new bool[4];
    private byte _initialFanFaultT;
    private bool _restoreFanFaultTRequired;

    public F718XX(LpcPort port, Chip chip, ushort address)
    {
        _port = port;
        _address = address;
        Chip = chip;

        // F71878AD (F71868A), F71889AD and F71889ED share the F71882FG fan register map. Their fans
        // only follow the PWM register once the per-fan bits in FAN_PWM_ENABLE_REG select manual
        // duty-cycle mode.
        _hasFanPwmEnable = chip is Chip.F71878AD or Chip.F71889AD or Chip.F71889ED;

        Voltages = new float?[chip == Chip.F71858 ? 3 : 9];
        Temperatures = new float?[chip == Chip.F71808E ? 2 : 3];
        Fans = new float?[chip is Chip.F71882 or Chip.F71858 ? 4 : 3];
        Controls = new float?[chip is Chip.F71878AD or Chip.F71889AD or Chip.F71889ED ? 3 : (chip == Chip.F71882 ? 4 : 0)];
    }

    public Chip Chip { get; }

    public float?[] Controls { get; }

    public float?[] Fans { get; }

    public float?[] Temperatures { get; }

    public float?[] Voltages { get; }

    public byte? ReadGpio(int index)
    {
        return null;
    }

    public void WriteGpio(int index, byte value)
    { }

    public void SetControl(int index, byte? value)
    {
        if (index < 0 || index >= Controls.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (!Mutexes.WaitIsaBus(10))
            return;

        if (value.HasValue)
        {
            SaveDefaultFanPwmControl(index);

            if (_hasFanPwmEnable)
                SetManualDutyCycleMode(index);

            WriteByte(FAN_PWM_REG[index], value.Value);
        }
        else
        {
            RestoreDefaultFanPwmControl(index);
        }

        Mutexes.ReleaseIsaBus();
    }

    public string GetReport()
    {
        StringBuilder r = new();

        r.AppendLine("LPC " + GetType().Name);
        r.AppendLine();
        r.Append("Base Address: 0x");
        r.AppendLine(_address.ToString("X4", CultureInfo.InvariantCulture));
        r.AppendLine();

        if (!Mutexes.WaitIsaBus(100))
            return r.ToString();

        r.AppendLine("Hardware Monitor Registers");
        r.AppendLine();
        r.AppendLine("      00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F");
        r.AppendLine();
        for (int i = 0; i <= 0xF; i++)
        {
            r.Append(" ");
            r.Append((i << 4).ToString("X2", CultureInfo.InvariantCulture));
            r.Append("  ");
            for (int j = 0; j <= 0xF; j++)
            {
                r.Append(" ");
                r.Append(ReadByte((byte)((i << 4) | j)).ToString("X2",
                                                                 CultureInfo.InvariantCulture));
            }

            r.AppendLine();
        }

        r.AppendLine();

        Mutexes.ReleaseIsaBus();
        return r.ToString();
    }

    public void Update()
    {
        if (!Mutexes.WaitIsaBus(10))
            return;

        for (int i = 0; i < Voltages.Length; i++)
        {
            if (Chip == Chip.F71808E && i == 6)
            {
                // 0x26 is reserved on F71808E
                Voltages[i] = 0;
            }
            else
            {
                int value = ReadByte((byte)(VOLTAGE_BASE_REG + i));
                Voltages[i] = 0.008f * value;
            }
        }

        for (int i = 0; i < Temperatures.Length; i++)
        {
            switch (Chip)
            {
                case Chip.F71858:
                    {
                        int tableMode = 0x3 & ReadByte(TEMPERATURE_CONFIG_REG);
                        int high = ReadByte((byte)(TEMPERATURE_BASE_REG + (2 * i)));
                        int low = ReadByte((byte)(TEMPERATURE_BASE_REG + (2 * i) + 1));
                        if (high is not 0xbb and not 0xcc)
                        {
                            int bits = 0;
                            switch (tableMode)
                            {
                                case 0:
                                    break;
                                case 1:
                                    bits = 0;
                                    break;
                                case 2:
                                    bits = (high & 0x80) << 8;
                                    break;
                                case 3:
                                    bits = (low & 0x01) << 15;
                                    break;
                            }

                            bits |= high << 7;
                            bits |= (low & 0xe0) >> 1;
                            short value = (short)(bits & 0xfff0);
                            Temperatures[i] = value / 128.0f;
                        }
                        else
                        {
                            Temperatures[i] = null;
                        }
                    }

                    break;
                default:
                    {
                        sbyte value = (sbyte)ReadByte((byte)(TEMPERATURE_BASE_REG + (2 * (i + 1))));
                        if (value is < sbyte.MaxValue and > 0)
                            Temperatures[i] = value;
                        else
                            Temperatures[i] = null;
                    }

                    break;
            }
        }

        for (int i = 0; i < Fans.Length; i++)
        {
            int value = ReadByte(FAN_TACHOMETER_REG[i]) << 8;
            value |= ReadByte((byte)(FAN_TACHOMETER_REG[i] + 1));

            if (value > 0)
                Fans[i] = value < 0x0fff ? 1.5e6f / value : 0;
            else
                Fans[i] = null;
        }

        byte fanPwmEnable = _hasFanPwmEnable ? ReadByte(FAN_PWM_ENABLE_REG) : (byte)0;

        for (int i = 0; i < Controls.Length; i++)
        {
            if (_hasFanPwmEnable)
                Controls[i] = ReadFanPwmEnableControl(i, fanPwmEnable);
            else
                Controls[i] = ReadByte(FAN_PWM_REG[i]) * 100.0f / 0xFF;
        }

        Mutexes.ReleaseIsaBus();
    }

    private float ReadFanPwmEnableControl(int index, byte fanPwmEnable)
    {
        if ((fanPwmEnable & FanDutyCycleModeBit(index)) != 0)
            return ReadByte(FAN_PWM_REG[index]) * 100.0f / 0xFF;

        // RPM mode: the fan target and full speed registers hold tachometer counts
        // (1.5 MHz / RPM), so the duty is the ratio of the full speed count to the target count.
        ushort target = ReadWord(FAN_TARGET_REG[index]);
        ushort fullSpeed = ReadWord(FAN_FULL_SPEED_REG[index]);
        if (target == 0 || fullSpeed == 0)
            return 0;

        return Math.Min(100.0f, fullSpeed * 100.0f / target);
    }

    /// <inheritdoc />
    public void Close()
    {
        _port.Close();
    }

    private void SaveDefaultFanPwmControl(int index)
    {
        if (_restoreDefaultFanPwmControlRequired[index])
            return;

        if (_hasFanPwmEnable)
        {
            if (!_restoreFanFaultTRequired)
            {
                // Bit 7 selects a second bank for the fan registers; the writes below must land in bank 0.
                // The Linux driver clears it at probe on the F71889 parts and SpeedFan does on the F71878AD.
                _initialFanFaultT = ReadByte(FAN_FAULT_T_REG);
                WriteByte(FAN_FAULT_T_REG, (byte)(_initialFanFaultT & ~FAN_PROG_SEL));
                _restoreFanFaultTRequired = true;
            }

            _initialFanPwmEnable[index] = (byte)(ReadByte(FAN_PWM_ENABLE_REG) & FanPwmEnableMask(index));

            // The PWM register is the low byte of the fan target word, so this covers both the
            // duty (duty-cycle mode) and the RPM target (RPM mode).
            _initialFanTarget[index] = ReadWord(FAN_TARGET_REG[index]);
        }
        else
        {
            _initialFanPwmControl[index] = ReadByte(FAN_PWM_REG[index]);
        }

        _restoreDefaultFanPwmControlRequired[index] = true;
    }

    private void RestoreDefaultFanPwmControl(int index)
    {
        if (!_restoreDefaultFanPwmControlRequired[index])
            return;

        if (_hasFanPwmEnable)
        {
            byte fanPwmEnable = ReadByte(FAN_PWM_ENABLE_REG);
            WriteByte(FAN_PWM_ENABLE_REG, (byte)((fanPwmEnable & ~FanPwmEnableMask(index)) | _initialFanPwmEnable[index]));
            WriteWord(FAN_TARGET_REG[index], _initialFanTarget[index]);
        }
        else
        {
            WriteByte(FAN_PWM_REG[index], _initialFanPwmControl[index]);
        }

        _restoreDefaultFanPwmControlRequired[index] = false;

        if (_restoreFanFaultTRequired && Array.TrueForAll(_restoreDefaultFanPwmControlRequired, required => !required))
        {
            WriteByte(FAN_FAULT_T_REG, _initialFanFaultT);
            _restoreFanFaultTRequired = false;
        }
    }

    private void SetManualDutyCycleMode(int index)
    {
        // Both bits set: the fan ignores the automatic curve and the RPM target and
        // drives the output with the duty in the PWM register.
        byte fanPwmEnable = ReadByte(FAN_PWM_ENABLE_REG);
        WriteByte(FAN_PWM_ENABLE_REG, (byte)(fanPwmEnable | FanPwmEnableMask(index)));
    }

    // Two bits per fan in FAN_PWM_ENABLE_REG: bit 2n = duty-cycle (1) or RPM (0) mode,
    // bit 2n+1 = manual (1) or automatic (0) control.
    private static byte FanDutyCycleModeBit(int index)
    {
        return (byte)(0x01 << (2 * index));
    }

    private static byte FanPwmEnableMask(int index)
    {
        return (byte)(0x03 << (2 * index));
    }

    private byte ReadByte(byte register)
    {
        _port.WriteIoPort((ushort)(_address + ADDRESS_REGISTER_OFFSET), register);
        return _port.ReadIoPort((ushort)(_address + DATA_REGISTER_OFFSET));
    }

    private void WriteByte(byte register, byte value)
    {
        _port.WriteIoPort((ushort)(_address + ADDRESS_REGISTER_OFFSET), register);
        _port.WriteIoPort((ushort)(_address + DATA_REGISTER_OFFSET), value);
    }

    private ushort ReadWord(byte register)
    {
        return (ushort)((ReadByte(register) << 8) | ReadByte((byte)(register + 1)));
    }

    private void WriteWord(byte register, ushort value)
    {
        WriteByte(register, (byte)(value >> 8));
        WriteByte((byte)(register + 1), (byte)(value & 0xFF));
    }

    // ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles

    private const byte ADDRESS_REGISTER_OFFSET = 0x05;
    private const byte DATA_REGISTER_OFFSET = 0x06;
    private const byte TEMPERATURE_BASE_REG = 0x70;
    private const byte TEMPERATURE_CONFIG_REG = 0x69;

    private const byte VOLTAGE_BASE_REG = 0x20;
    private const byte FAN_PWM_ENABLE_REG = 0x96;
    private const byte FAN_FAULT_T_REG = 0x9F;
    private const byte FAN_PROG_SEL = 0x80;
    private readonly byte[] FAN_PWM_REG = { 0xA3, 0xB3, 0xC3, 0xD3 };
    private readonly byte[] FAN_TACHOMETER_REG = { 0xA0, 0xB0, 0xC0, 0xD0 };
    private readonly byte[] FAN_TARGET_REG = { 0xA2, 0xB2, 0xC2, 0xD2 };
    private readonly byte[] FAN_FULL_SPEED_REG = { 0xA4, 0xB4, 0xC4, 0xD4 };

    // ReSharper restore InconsistentNaming
#pragma warning restore IDE1006 // Naming Styles
}
