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
    private readonly byte[] _initialFanPwmControl = new byte[2];
    private byte _initialFanPwmMode;
    private readonly bool[] _restoreDefaultFanPwmControlRequired = new bool[2];
    private readonly byte _chip_address;

    public F753XX(ChipSmbus chip, byte addr, ushort smb_addr)
    {
        Chip = (Chip)chip;
        _chip_address = addr;
        SMB_ADDRESS = smb_addr;
        SMBHSTSTS = (ushort)(0 + SMB_ADDRESS);
        SMBHSTCNT = (ushort)(2 + SMB_ADDRESS);
        SMBHSTCMD = (ushort)(3 + SMB_ADDRESS);
        SMBHSTADD = (ushort)(4 + SMB_ADDRESS);
        SMBHSTDAT0 = (ushort)(5 + SMB_ADDRESS);
        SMBHSTDAT1 = (ushort)(6 + SMB_ADDRESS);
        SMBAUXCTL = (ushort)(13 + SMB_ADDRESS);

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
            if (Chip == (Chip)ChipSmbus.F75387)
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

            if (Chip == (Chip)ChipSmbus.F75387)
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
        r.AppendLine(_chip_address.ToString("X4", CultureInfo.InvariantCulture));
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
            value += ReadByte(TEMPERATURE_LSB_REG[i]) / 256.0f;

            if (value is < 140 and > 0) // real range is 0 to 140.875
                Temperatures[i] = value;
            else
                Temperatures[i] = null;
        }

        for (int i = 0; i < Fans.Length; i++)
        {
            int value = ReadByte(FAN_TACHOMETER_MSB_REG[i]) << 8;
            value |= ReadByte(FAN_TACHOMETER_LSB_REG[i]);

            if (value > 0)
                Fans[i] = value < 0x0FFE ? 1.5e6f / value : 0;
            else
                Fans[i] = null;
        }

        for (int i = 0; i < Controls.Length; i++)
        {
            int value = ReadByte(FAN_PWM_EXP_MSB_REG[i]) << 8;
            value |= ReadByte(FAN_PWM_EXP_LSB_REG[i]);

            Controls[i] = value * 100.0f / 0xFF;
        }

        Ring0.ReleaseSmBusMutex();
    }

    private void SaveDefaultFanPwmControl(int index)
    {
        if (!_restoreDefaultFanPwmControlRequired[index])
        {
            _initialFanPwmMode = ReadByte(FAN_MODE_REG);

            if (Chip == (Chip)ChipSmbus.F75387)
                _initialFanPwmControl[index] = ReadByte(FAN_PWM_EXP_LSB_REG[index]);
            else
                _initialFanPwmControl[index] = ReadByte(FAN_PWM_DUTY_REG[index]);

            _restoreDefaultFanPwmControlRequired[index] = true;
        }
    }

    private void RestoreDefaultFanPwmControl(int index)
    {
        if (_restoreDefaultFanPwmControlRequired[index])
        {
            _initialFanPwmMode = ReadByte(FAN_MODE_REG);

            if (Chip == (Chip)ChipSmbus.F75387)
                WriteByte(FAN_PWM_EXP_LSB_REG[index], _initialFanPwmControl[index]);
            else
                WriteByte(FAN_PWM_DUTY_REG[index], _initialFanPwmControl[index]);

            _restoreDefaultFanPwmControlRequired[index] = false;
        }
    }

    private int CheckPre()
    {
        ushort status = Ring0.ReadSmbus(SMBHSTSTS);
        if ((status & SMBHSTSTS_HOST_BUSY) > 0)
        {
            return -1;
        }

        status &= STATUS_FLAGS;
        if (status > 0)
        {
            Ring0.WriteSmbus(SMBHSTSTS, (byte)status);
            status = (ushort)(Ring0.ReadSmbus(SMBHSTSTS) & STATUS_FLAGS);
            if (status > 0)
            {
                return -1;
            }
        }
        return 0;
    }

    private int WaitIntr()
    {
        const int maxCount = 1000;
        int timeout = 0;
        ushort status;
        bool val;
        bool val2;

        do
        {
            status = Ring0.ReadSmbus(SMBHSTSTS);
            val = (status & SMBHSTSTS_HOST_BUSY) > 0;
            val2 = (status & (STATUS_ERROR_FLAGS | SMBHSTSTS_INTR)) > 0;

        } while ((val || !val2) && timeout++ < maxCount);

        if (timeout > maxCount)
        {
            return -1;
        }
        return status & (STATUS_ERROR_FLAGS | SMBHSTSTS_INTR);
    }

    private int CheckPost(int status)
    {
        if (status < 0)
        {
            Ring0.WriteSmbus(SMBHSTCNT, (byte)(Ring0.ReadSmbus(SMBHSTCNT) | SMBHSTCNT_KILL));
            Thread.Sleep(1);
            Ring0.WriteSmbus(SMBHSTCNT, (byte)(Ring0.ReadSmbus(SMBHSTCNT) & (~SMBHSTCNT_KILL)));

            Ring0.WriteSmbus(SMBHSTSTS, (byte)STATUS_FLAGS);
            return -1;
        }

        Ring0.WriteSmbus(SMBHSTSTS, (byte)status);

        if ((status & SMBHSTSTS_FAILED) > 0 || (status & SMBHSTSTS_DEV_ERR) > 0 || (status & SMBHSTSTS_BUS_ERR) > 0)
        {
            return -1;
        }

        return 0;
    }

    private int Transaction(byte xact) // 0x0C WORD_DATA, 0x08 BYTE_DATA
    {
        int result = CheckPre();
        if (result < 0)
            return result;

        Ring0.WriteSmbus(SMBHSTCNT, (byte)(Ring0.ReadSmbus(SMBHSTCNT) & ~SMBHSTCNT_INTREN));
        Ring0.WriteSmbus(SMBHSTCNT, (byte)(xact | SMBHSTCNT_START));

        int status = WaitIntr();

        return CheckPost(status);
    }

    private byte ReadByte(byte register)
    {
        if (SMB_ADDRESS == 0)
            return 0;

        Ring0.WriteSmbus(SMBHSTADD, (byte)(((_chip_address & 0x7f) << 1) | (SMB_READ & 0x01)));
        Ring0.WriteSmbus(SMBHSTCMD, register);

        Ring0.WriteSmbus(SMBAUXCTL, (byte)(Ring0.ReadSmbus(SMBAUXCTL) & (~SMBAUXCTL_CRC)));

        Transaction(0x08);

        Ring0.WriteSmbus(SMBAUXCTL, (byte)(Ring0.ReadSmbus(SMBAUXCTL) & ~(SMBAUXCTL_CRC | SMBAUXCTL_E32B)));

        return Ring0.ReadSmbus(SMBHSTDAT0);
    }

    private void WriteByte(byte register, byte value)
    {
        if (SMB_ADDRESS == 0)
            return;

        Ring0.WriteSmbus(SMBHSTADD, (byte)(((_chip_address & 0x7f) << 1) | (SMB_WRITE & 0x01)));
        Ring0.WriteSmbus(SMBHSTCMD, register);
        Ring0.WriteSmbus(SMBHSTDAT0, value);

        Ring0.WriteSmbus(SMBAUXCTL, (byte)(Ring0.ReadSmbus(SMBAUXCTL) & (~SMBAUXCTL_CRC)));

        Transaction(0x08);

        Ring0.WriteSmbus(SMBAUXCTL, (byte)(Ring0.ReadSmbus(SMBAUXCTL) & ~(SMBAUXCTL_CRC | SMBAUXCTL_E32B)));
    }

    // ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles

    private readonly byte[] TEMPERATURE_MSB_REG = { 0x14, 0x15, 0x1C }; // read MSB first!  [T1, T2, Local]
    private readonly byte[] TEMPERATURE_LSB_REG = { 0x1A, 0x1B, 0x1D };

    private const byte VOLTAGE_BASE_REG = 0x10;                         // [VCC, V1, V2, V3]


    private const byte FAN_MODE_REG = 0x60;
    private readonly byte[] FAN_PWM_DUTY_REG = { 0x76, 0x86 };          // set pwm (use PWM_EXP for F75387)
    private readonly byte[] FAN_PWM_EXP_MSB_REG = { 0x74, 0x84 };       // expected speed
    private readonly byte[] FAN_PWM_EXP_LSB_REG = { 0x75, 0x85 };

    private readonly byte[] FAN_TACHOMETER_MSB_REG = { 0x16, 0x18 };    // read MSB first!
    private readonly byte[] FAN_TACHOMETER_LSB_REG = { 0x17, 0x19 };

    //SMBus:
    private readonly ushort SMB_ADDRESS = 0;

    private ushort SMBHSTSTS = 0;
    private ushort SMBHSTCNT = 0;
    private ushort SMBHSTCMD = 0;
    private ushort SMBHSTADD = 0;
    private ushort SMBHSTDAT0 = 0;
    private ushort SMBHSTDAT1 = 0;
    private ushort SMBAUXCTL = 0;

    private const byte SMB_READ = 0x01;
    private const byte SMB_WRITE = 0x00;

    private static ushort SMBAUXCTL_CRC = (1 << 0);
    private static ushort SMBAUXCTL_E32B = (1 << 1);

    private static ushort SMBHSTCNT_INTREN = (1 << 0);
    private static ushort SMBHSTCNT_KILL = (1 << 1);
    //private static ushort SMBHSTCNT_LAST_BYTE = (1 << 5);
    private static ushort SMBHSTCNT_START = (1 << 6);
    //private static ushort SMBHSTCNT_PEC_EN = (1 << 7);

    private static ushort SMBHSTSTS_BYTE_DONE = (1 << 7);
    //private static ushort SMBHSTSTS_INUSE_STS = (1 << 6);
    //private static ushort SMBHSTSTS_SMBALERT_STS = (1 << 5);
    private static ushort SMBHSTSTS_FAILED = (1 << 4);
    private static ushort SMBHSTSTS_BUS_ERR = (1 << 3);
    private static ushort SMBHSTSTS_DEV_ERR = (1 << 2);
    private static ushort SMBHSTSTS_INTR = (1 << 1);
    private static ushort SMBHSTSTS_HOST_BUSY = (1 << 0);

    private static ushort STATUS_ERROR_FLAGS = (ushort)(SMBHSTSTS_FAILED | SMBHSTSTS_BUS_ERR | SMBHSTSTS_DEV_ERR);
    private static ushort STATUS_FLAGS = (ushort)(SMBHSTSTS_BYTE_DONE | SMBHSTSTS_INTR | STATUS_ERROR_FLAGS);

    // ReSharper restore InconsistentNaming
#pragma warning restore IDE1006 // Naming Styles
}
