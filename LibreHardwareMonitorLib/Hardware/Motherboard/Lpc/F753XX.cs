// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Globalization;
using System.Text;
using System.Threading;

// ReSharper disable once InconsistentNaming

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc;

internal class F753XX : ISuperIO
{
    private readonly byte[] _initialFanExpMsb = new byte[2]; // only for F75387 to prevent unexpected things on unknown HW
    private readonly byte[] _initialFanPwmControl = new byte[2];
    private byte _initialFanPwmMode;
    private readonly bool[] _restoreDefaultFanPwmControlRequired = new bool[2];
    SmBusDevice Dev;

    public F753XX(Chip chip, byte addr, ushort smb_addr)
    {
        Chip = chip;
        Dev = new SmBusDevice(addr, smb_addr);
        
        Voltages = new float?[4];
        Temperatures = new float?[chip == Chip.F75387 ? 3 : 2];
        Fans = new float?[2];
        Controls = new float?[2];
    }

    public F753XX(Chip chip, SmBusDevice dev)
    {
        Chip = chip;
        Dev = dev;

        Voltages = new float?[4];
        Temperatures = new float?[3];
        Fans = new float?[2];
        Controls = new float?[2];
    }

    public Chip Chip { get; } // ChipSmbus

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

        if (!Ring0.WaitSmBusMutex(100))
            return;

        if (value.HasValue)
        {
            SaveDefaultFanPwmControl(index);

            // force change fan mode to manual
            byte fanmode = ReadByte(FAN_MODE_REG);
            if (Chip == Chip.F75387)
            {
                for (byte nr = 0; nr < Controls.Length; nr++)
                {
                    fanmode &= (byte)~(1 << (2 + (nr * 4)));
                    fanmode &= (byte)~(1 << (nr * 4));
                    fanmode |= (byte)(1 << (nr * 4));
                    fanmode |= (byte)(1 << (2 + (nr * 4)));
                }
            }
            else
            {
                for (byte nr = 0; nr < Controls.Length; nr++)
                {
                    fanmode &= (byte)~(3 << (4 + (nr * 2)));
                    fanmode |= (byte)(3 << (4 + (nr * 2)));
                }
            }
            WriteByte(FAN_MODE_REG, fanmode);

            if (Chip == Chip.F75387)
                WriteByte(FAN_PWM_EXP_LSB_REG[index], value.Value);
            else
                WriteByte(FAN_PWM_DUTY_REG[index], value.Value);
        }
        else
        {
            RestoreDefaultFanPwmControl(index);
        }

        Ring0.ReleaseSmBusMutex();
    }

    public string GetReport()
    {
        StringBuilder r = new();

        r.AppendLine("SMBus " + GetType().Name);
        r.AppendLine();
        r.Append("Base Address: 0x");
        r.AppendLine(Dev.ChipAddr.ToString("X4", CultureInfo.InvariantCulture));
        r.AppendLine();

        if (!Ring0.WaitSmBusMutex(100))
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

        Ring0.ReleaseSmBusMutex();
        return r.ToString();
    }

    public void Update()
    {
        if (!Ring0.WaitSmBusMutex(100))
            return;

        for (int i = 0; i < Voltages.Length; i++)
        {
            int value = ReadByte((byte)(VOLTAGE_BASE_REG + i));
            Voltages[i] = 0.008f * value;
        }

        for (int i = 0; i < Temperatures.Length; i++)
        {
            float value = ReadByte(TEMPERATURE_MSB_REG[i]);
            if (Chip == Chip.F75387)
                value += ReadByte(TEMPERATURE_LSB_REG[i]) / 256.0f;

            if (value is < 140 and > 0) // real range is 0 to 140.875
                Temperatures[i] = value;
            else
                Temperatures[i] = null;
        }

        for (int i = 0; i < Fans.Length; i++)
        {
            int value = ReadMsbLsb(FAN_TACHOMETER_MSB_REG[i], FAN_TACHOMETER_LSB_REG[i]);

            if (value > 0)
                Fans[i] = value < 0x0FFE ? 1.5e6f / value : 0;
            else
                Fans[i] = null;
        }

        for (int i = 0; i < Controls.Length; i++)
        {
            int value = ReadMsbLsb(FAN_PWM_EXP_MSB_REG[i], FAN_PWM_EXP_LSB_REG[i]);

            Controls[i] = value * 100.0f / 0xFF;
        }

        Ring0.ReleaseSmBusMutex();
    }

    private void SaveDefaultFanPwmControl(int index)
    {
        if (!_restoreDefaultFanPwmControlRequired[index])
        {
            _initialFanPwmMode = ReadByte(FAN_MODE_REG);

            if (Chip == Chip.F75387)
            {
                _initialFanExpMsb[index] = ReadByte(FAN_PWM_EXP_MSB_REG[index]);
                _initialFanPwmControl[index] = ReadByte(FAN_PWM_EXP_LSB_REG[index]);
            }
            else
            {
                _initialFanPwmControl[index] = ReadByte(FAN_PWM_DUTY_REG[index]);
            }

            _restoreDefaultFanPwmControlRequired[index] = true;
        }
    }

    private void RestoreDefaultFanPwmControl(int index)
    {
        if (_restoreDefaultFanPwmControlRequired[index])
        {
            WriteByte(FAN_MODE_REG, _initialFanPwmMode);

            if (Chip == Chip.F75387)
            {
                WriteByte(FAN_PWM_EXP_MSB_REG[index], _initialFanExpMsb[index]);
                WriteByte(FAN_PWM_EXP_LSB_REG[index], _initialFanPwmControl[index]);
            }
            else
            {
                WriteByte(FAN_PWM_DUTY_REG[index], _initialFanPwmControl[index]);
            }

            _restoreDefaultFanPwmControlRequired[index] = false;
        }
    }

    private ushort ReadMsbLsb(byte reg_msb, byte reg_lsb)
    {
        return (ushort)((Dev.ReadByte(reg_msb) << 8) | Dev.ReadByte(reg_lsb));
    }

    private byte ReadByte(byte register)
    {
        return Dev.ReadByte(register);
    }

    private void WriteMsbLsb(byte reg_msb, byte reg_lsb, ushort value)
    {
        Dev.WriteByte(reg_msb, (byte)((value >> 8) & 0xff));
        Dev.WriteByte(reg_lsb, (byte)(value & 0xff));
    }

    private void WriteByte(byte register, byte value)
    {
        Dev.WriteByte(register, value);
    }

    // ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles

    private readonly byte[] TEMPERATURE_MSB_REG = { 0x14, 0x15, 0x1C }; // read MSB first!  [T1, T2, Local]
    private readonly byte[] TEMPERATURE_LSB_REG = { 0x1A, 0x1B, 0x1D }; // only for F75387

    private const byte VOLTAGE_BASE_REG = 0x10;                         // [VCC, V1, V2, V3]

    private const byte FAN_MODE_REG = 0x60;
    private readonly byte[] FAN_PWM_DUTY_REG = { 0x76, 0x86 };          // set pwm (use PWM_EXP for F75387)
    private readonly byte[] FAN_PWM_EXP_MSB_REG = { 0x74, 0x84 };       // expected speed
    private readonly byte[] FAN_PWM_EXP_LSB_REG = { 0x75, 0x85 };

    private readonly byte[] FAN_TACHOMETER_MSB_REG = { 0x16, 0x18 };    // read MSB first!
    private readonly byte[] FAN_TACHOMETER_LSB_REG = { 0x17, 0x19 };

    // ReSharper restore InconsistentNaming
#pragma warning restore IDE1006 // Naming Styles
}
