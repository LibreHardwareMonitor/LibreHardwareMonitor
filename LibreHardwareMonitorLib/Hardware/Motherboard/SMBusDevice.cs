// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Threading;

namespace LibreHardwareMonitor.Hardware;

internal class SmBusDevice
{
    public SmBusDevice(byte addr, ushort smb_addr)
    {
        CHIP_ADDRESS = addr;
        SMB_ADDRESS = smb_addr;
        SMBHSTSTS = (ushort)(0 + SMB_ADDRESS);
        SMBHSTCNT = (ushort)(2 + SMB_ADDRESS);
        SMBHSTCMD = (ushort)(3 + SMB_ADDRESS);
        SMBHSTADD = (ushort)(4 + SMB_ADDRESS);
        SMBHSTDAT0 = (ushort)(5 + SMB_ADDRESS);
        SMBHSTDAT1 = (ushort)(6 + SMB_ADDRESS);
        SMBAUXCTL = (ushort)(13 + SMB_ADDRESS);
    }

    private SmBusDevice()
    { }

    public byte ChipAddr { get => CHIP_ADDRESS; }

    public ushort SmBusAddr { get => SMB_ADDRESS; }

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

    private int Transaction(byte xact) // I801: 0x0C WORD_DATA, 0x08 BYTE_DATA, 0x00 QUICK
    {
        int result = CheckPre();
        if (result < 0)
            return result;

        Ring0.WriteSmbus(SMBHSTCNT, (byte)(Ring0.ReadSmbus(SMBHSTCNT) & ~SMBHSTCNT_INTREN));
        Ring0.WriteSmbus(SMBHSTCNT, (byte)(xact | SMBHSTCNT_START));

        int status = WaitIntr();

        return CheckPost(status);
    }

    public ushort ReadWord(byte register)
    {
        if (SMB_ADDRESS == 0 || CHIP_ADDRESS == 0)
            return 0xFFFF;

        Ring0.WriteSmbus(SMBHSTADD, (byte)(((CHIP_ADDRESS & 0x7f) << 1) | (SMB_READ & 0x01)));
        Ring0.WriteSmbus(SMBHSTCMD, register);

        Ring0.WriteSmbus(SMBAUXCTL, (byte)(Ring0.ReadSmbus(SMBAUXCTL) & (~SMBAUXCTL_CRC)));

        Transaction(0x0C);

        Ring0.WriteSmbus(SMBAUXCTL, (byte)(Ring0.ReadSmbus(SMBAUXCTL) & ~(SMBAUXCTL_CRC | SMBAUXCTL_E32B)));

        return (ushort)(Ring0.ReadSmbus(SMBHSTDAT0) | (Ring0.ReadSmbus(SMBHSTDAT1) << 8));
    }

    public byte ReadByte(byte register)
    {
        if (SMB_ADDRESS == 0 || CHIP_ADDRESS == 0)
            return 0xFF;

        Ring0.WriteSmbus(SMBHSTADD, (byte)(((CHIP_ADDRESS & 0x7f) << 1) | (SMB_READ & 0x01)));
        Ring0.WriteSmbus(SMBHSTCMD, register);

        Ring0.WriteSmbus(SMBAUXCTL, (byte)(Ring0.ReadSmbus(SMBAUXCTL) & (~SMBAUXCTL_CRC)));

        Transaction(0x08);

        Ring0.WriteSmbus(SMBAUXCTL, (byte)(Ring0.ReadSmbus(SMBAUXCTL) & ~(SMBAUXCTL_CRC | SMBAUXCTL_E32B)));

        return Ring0.ReadSmbus(SMBHSTDAT0);
    }

    public void WriteWord(byte register, ushort value)
    {
        if (SMB_ADDRESS == 0 || CHIP_ADDRESS == 0)
            return;

        Ring0.WriteSmbus(SMBHSTADD, (byte)(((CHIP_ADDRESS & 0x7f) << 1) | (SMB_WRITE & 0x01)));
        Ring0.WriteSmbus(SMBHSTCMD, register);
        Ring0.WriteSmbus(SMBHSTDAT0, (byte)(value & 0x00ff));
        Ring0.WriteSmbus(SMBHSTDAT1, (byte)((value & 0xff00) >> 8));

        Ring0.WriteSmbus(SMBAUXCTL, (byte)(Ring0.ReadSmbus(SMBAUXCTL) & (~SMBAUXCTL_CRC)));

        Transaction(0x0C);

        Ring0.WriteSmbus(SMBAUXCTL, (byte)(Ring0.ReadSmbus(SMBAUXCTL) & ~(SMBAUXCTL_CRC | SMBAUXCTL_E32B)));
    }

    public void WriteByte(byte register, byte value)
    {
        if (SMB_ADDRESS == 0 || CHIP_ADDRESS == 0)
            return;

        Ring0.WriteSmbus(SMBHSTADD, (byte)(((CHIP_ADDRESS & 0x7f) << 1) | (SMB_WRITE & 0x01)));
        Ring0.WriteSmbus(SMBHSTCMD, register);
        Ring0.WriteSmbus(SMBHSTDAT0, value);

        Ring0.WriteSmbus(SMBAUXCTL, (byte)(Ring0.ReadSmbus(SMBAUXCTL) & (~SMBAUXCTL_CRC)));

        Transaction(0x08);

        Ring0.WriteSmbus(SMBAUXCTL, (byte)(Ring0.ReadSmbus(SMBAUXCTL) & ~(SMBAUXCTL_CRC | SMBAUXCTL_E32B)));
    }

    private bool checkDevice(byte addr, ushort smb_addr)
    {
        SMBHSTSTS = (ushort)(0 + smb_addr);
        SMBHSTCNT = (ushort)(2 + smb_addr);
        SMBHSTCMD = (ushort)(3 + smb_addr);
        SMBHSTADD = (ushort)(4 + smb_addr);
        SMBHSTDAT0 = (ushort)(5 + smb_addr);
        SMBHSTDAT1 = (ushort)(6 + smb_addr);
        SMBAUXCTL = (ushort)(13 + smb_addr);

        Ring0.WriteSmbus(SMBHSTADD, (byte)(((addr & 0x7f) << 1) | SMB_WRITE));
        Ring0.WriteSmbus(SMBAUXCTL, (byte)(Ring0.ReadSmbus(SMBAUXCTL) & (~SMBAUXCTL_CRC)));

        int res = Transaction(0x00);

        Ring0.WriteSmbus(SMBAUXCTL, (byte)(Ring0.ReadSmbus(SMBAUXCTL) & ~(SMBAUXCTL_CRC | SMBAUXCTL_E32B)));

        return res >= 0;
    }

    // returns device address
    public static byte DetectDevice(byte start_addr, ushort smb_addr)
    {
        if (smb_addr == 0)
            return 0;

        if (Mutexes.WaitSmBus(100))
        {
            int index = 0;
            SmBusDevice tempDev = new SmBusDevice();
            for (byte addr = start_addr; addr < 0x7E; addr++)
            {
                if (tempDev.checkDevice(addr, smb_addr))
                {
                    Mutexes.ReleaseSmBus();
                    return addr;
                }

                Thread.Sleep(10);
                index++;
            }
            Mutexes.ReleaseSmBus();
        }
        return 0;
    }

    private readonly ushort SMB_ADDRESS = 0;
    private readonly byte CHIP_ADDRESS = 0;

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
}
