// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Threading;

namespace LibreHardwareMonitor.Hardware.Motherboard 
{
    internal sealed class IntelSmbus
    {
        public static byte SmbDetect(byte addr)
        {
            Ring0.WriteSmbus(SMBHSTADD, ((addr & 0x7f) << 1) | SMB_WRITE);            
            Ring0.WriteSmbus(SMBAUXCTL, Ring0.ReadSmbus(SMBAUXCTL) & (~SMBAUXCTL_CRC));

            int res = Transaction(0x00);

            Ring0.WriteSmbus(SMBAUXCTL, Ring0.ReadSmbus(SMBAUXCTL) & ~(SMBAUXCTL_CRC | SMBAUXCTL_E32B));

            if (res >= 0)
                return addr;

            return 0x00;
        }

        public static ushort GetWord(byte addr, byte command)
        {
            Ring0.WriteSmbus(SMBHSTADD, ((addr & 0x7f) << 1) | SMB_READ);
            Ring0.WriteSmbus(SMBHSTCMD, command);

            Ring0.WriteSmbus(SMBAUXCTL, Ring0.ReadSmbus(SMBAUXCTL) & (~SMBAUXCTL_CRC));

            Transaction(0x0C);

            Ring0.WriteSmbus(SMBAUXCTL, Ring0.ReadSmbus(SMBAUXCTL) & ~(SMBAUXCTL_CRC | SMBAUXCTL_E32B));

            ushort wordData = (ushort)(Ring0.ReadSmbus(SMBHSTDAT0) + (Ring0.ReadSmbus(SMBHSTDAT1) << 8));
            return wordData;
        }
        
        public static int GetBlock(byte addr, byte command, List<byte> block)
        {
            Ring0.WriteSmbus(SMBHSTADD, ((addr & 0x7f) << 1) | SMB_READ);
            Ring0.WriteSmbus(SMBHSTCMD, command);

            Ring0.WriteSmbus(SMBAUXCTL, Ring0.ReadSmbus(SMBAUXCTL) | SMBAUXCTL_E32B);
            Ring0.WriteSmbus(SMBHSTCNT, Ring0.ReadSmbus(SMBHSTCNT));
            int status = Transaction(0x14);
            if (status < 0)
                return status;

            byte length = (byte)Ring0.ReadSmbus(SMBHSTDAT0);
            if (length < 1 || length > 32)
                return -1;

            for (int i = 0; i < length; i++)
            {
                block.Add((byte)Ring0.ReadSmbus(SMBBLKDAT));
            }

            return 0;
        }


        private static int Transaction(byte xact)
        {
            int result = CheckPre();
            if (result < 0)
                return result;

            Ring0.WriteSmbus(SMBHSTCNT, Ring0.ReadSmbus(SMBHSTCNT) & ~SMBHSTCNT_INTREN);
            Ring0.WriteSmbus(SMBHSTCNT, xact | SMBHSTCNT_START);

            int status = WaitIntr();
            return CheckPost(status);
        }

        private static int CheckPre()
        {
            ushort status = Ring0.ReadSmbus(SMBHSTSTS);
            if ((status & SMBHSTSTS_HOST_BUSY) > 0)
            {
                return -1;
            }

            status &= STATUS_FLAGS;
            if (status > 0)
            {
                Ring0.WriteSmbus(SMBHSTSTS, status);
            }

            Ring0.WriteSmbus(SMBAUXSTS, Ring0.ReadSmbus(SMBAUXSTS) & SMBAUXSTS_CRCE);

            return 0;
        }

        private static int WaitIntr()
        {
            int maxCount = 400;
            int timeout = 0;
            ushort status;
            bool val = false;
            bool val2 = false;

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

        private static int CheckPost(int status)
        {
            int result = 0;
            if (status < 0)
            {
                Ring0.WriteSmbus(SMBHSTCNT, Ring0.ReadSmbus(SMBHSTCNT) | SMBHSTCNT_KILL);
                Thread.Sleep(1);
                Ring0.WriteSmbus(SMBHSTCNT, Ring0.ReadSmbus(SMBHSTCNT) & (~SMBHSTCNT_KILL));

                Ring0.WriteSmbus(SMBHSTSTS, STATUS_FLAGS);
                return -1;
            }

            if ((status & SMBHSTSTS_FAILED) > 0 || (status & SMBHSTSTS_DEV_ERR) > 0 || (status & SMBHSTSTS_BUS_ERR) > 0)
            {
                result = -1;
            }

            Ring0.WriteSmbus(SMBHSTSTS, status);

            return result;
        }
        private const byte SMB_READ = 0x01;
        private const byte SMB_WRITE = 0x00;

        private static ushort SMB_ADDRESS = 0;

        public static void SetSMBAddress(ushort address)
        {
            SMB_ADDRESS = address;
            SMBHSTSTS = (ushort)(0 + SMB_ADDRESS);
            SMBHSTCNT = (ushort)(2 + SMB_ADDRESS);
            SMBHSTCMD = (ushort)(3 + SMB_ADDRESS);
            SMBHSTADD = (ushort)(4 + SMB_ADDRESS);
            SMBHSTDAT0 = (ushort)(5 + SMB_ADDRESS);
            SMBHSTDAT1 = (ushort)(6 + SMB_ADDRESS);
            SMBBLKDAT = (ushort)(7 + SMB_ADDRESS);
            //SMBPEC = (ushort)(8 + SMB_ADDRESS);
            SMBAUXSTS = (ushort)(12 + SMB_ADDRESS);
            SMBAUXCTL = (ushort)(13 + SMB_ADDRESS);
            //SMBSLVSTS = (ushort)(16 + SMB_ADDRESS);
            //SMBSLVCMD = (ushort)(17 + SMB_ADDRESS);
            //SMBNTFDADD = (ushort)(20 + SMB_ADDRESS);
        }

        private static ushort SMBHSTSTS = 0;
        private static ushort SMBHSTCNT = 0;
        private static ushort SMBHSTCMD = 0;
        private static ushort SMBHSTADD = 0;
        private static ushort SMBHSTDAT0 = 0;
        private static ushort SMBHSTDAT1 = 0;
        private static ushort SMBBLKDAT = 0;
        //private static ushort SMBPEC = 0;
        private static ushort SMBAUXSTS = 0;
        private static ushort SMBAUXCTL = 0;
        //private static ushort SMBSLVSTS = 0;
        //private static ushort SMBSLVCMD = 0;
        //private static ushort SMBNTFDADD = 0;
        private static ushort SMBAUXSTS_CRCE = (1 << 0);
        private static ushort SMBAUXCTL_CRC = (1 << 0);
        private static ushort SMBAUXCTL_E32B = (1 << 1);

        private static ushort SMBHSTCNT_INTREN = (1 << 0);
        private static ushort SMBHSTCNT_KILL = (1 << 1);
        //private static ushort SMBHSTCNT_LAST_BYTE = (1 << 5);
        private static ushort SMBHSTCNT_START = (1 << 6);
        //private static ushort SMBHSTCNT_PEC_EN = (1 << 7);

        private static ushort SMBHSTSTS_BYTE_DONE = (1 << 7);
        private static ushort SMBHSTSTS_INUSE_STS = (1 << 6);
        //private static ushort SMBHSTSTS_SMBALERT_STS = (1 << 5);
        private static ushort SMBHSTSTS_FAILED = (1 << 4);
        private static ushort SMBHSTSTS_BUS_ERR = (1 << 3);
        private static ushort SMBHSTSTS_DEV_ERR = (1 << 2);
        private static ushort SMBHSTSTS_INTR = (1 << 1);
        private static ushort SMBHSTSTS_HOST_BUSY = (1 << 0);

        //private static ushort SMBSLVSTS_HST_NTFY_STS = (1 << 0);

        //private static ushort SMBSLVCMD_HST_NTFY_INTREN = (1 << 0);

        private static ushort STATUS_ERROR_FLAGS = (ushort)(SMBHSTSTS_FAILED | SMBHSTSTS_BUS_ERR | SMBHSTSTS_DEV_ERR);
        private static ushort STATUS_FLAGS = (ushort)(SMBHSTSTS_BYTE_DONE | SMBHSTSTS_INTR | STATUS_ERROR_FLAGS);
    }
}
