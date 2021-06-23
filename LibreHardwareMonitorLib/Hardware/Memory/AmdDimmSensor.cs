// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LibreHardwareMonitor.Hardware.Memory
{
    internal sealed class AmdDimmSensor : DimmSensor
    {
        public AmdDimmSensor(string name, int index, Hardware hardware, ISettings settings, byte address) : base(name, index, hardware, settings, address)
        {

        }

        public override void UpdateSensor()
        {
            try
            {
                ushort data = GetWord(_address, 0x05);
                var temp = BitConverter.GetBytes(data);
                Array.Reverse(temp);

                double value = 0.0f;
                byte upper = (byte)(temp[1] & 0x1F);
                byte lower = temp[0];

                if ((upper & 0x10) == 0x10)     // negative
                {
                    upper = (byte)(upper & 0x0F);
                    value = (double)(256 - ((double)upper * 16 + (double)lower / 16));
                }
                else
                {
                    upper = (byte)(upper & 0x0F);
                    value = (double)((double)upper * 16 + (double)lower / 16);
                    Value = (float)value;
                }
            }
            catch { }
        }

        public static byte SmbDetect(byte addr)
        {
            Ring0.WriteSmbus(SMB_HSTADD, (addr << 1) | SMB_WRITE);
            Ring0.WriteSmbus(SMB_HSTCNT, 0);
            if (Transaction() == true)
            {
                ushort configuration = GetWord(addr, 0x01);
                BitArray bitArray = new BitArray(BitConverter.GetBytes(configuration));
                if (bitArray[8])
                    return 0x00;


                ushort manufacturerID = GetWord(addr, 0x06);
                ushort deviceID = GetWord(addr, 0x07);

                if (manufacturerID > 0 && deviceID > 0)
                    return addr;
            }
            return 0x00;
        }

        private static ushort GetWord(byte addr, byte command)
        {
            Ring0.WriteSmbus(SMB_HSTADD, (addr << 1) | SMB_READ);
            Ring0.WriteSmbus(SMB_HSTCMD, command);

            Ring0.WriteSmbus(SMB_HSTCNT, 0x0C);

            Transaction();

            ushort temp = (ushort)(Ring0.ReadSmbus(SMB_HSTDAT0) + (Ring0.ReadSmbus(SMB_HSTDAT1) << 8));
            return temp;
        }

        private static bool Transaction()
        {
            byte temp = (byte)Ring0.ReadSmbus(SMB_HSTSTS);

            if (temp != 0x00)
            {
                Ring0.WriteSmbus(SMB_HSTSTS, temp);

                temp = (byte)Ring0.ReadSmbus(SMB_HSTSTS);

                if (temp != 0x00)
                {
                    return false;
                }
            }

            temp = (byte)Ring0.ReadSmbus(SMB_HSTCNT);
            Ring0.WriteSmbus(SMB_HSTCNT, (byte)(temp | 0x040));

            temp = 0;
            int timeout = 0;
            int MAX_TIMEOUT = 5000;
            while ((++timeout < MAX_TIMEOUT) && temp <= 1)
            {
                temp = (byte)Ring0.ReadSmbus(SMB_HSTSTS);
            }

            if (timeout == MAX_TIMEOUT || (temp & 0x10) > 0 || (temp & 0x08) > 0 || (temp & 0x04) > 0)
            {
                return false;
            }

            temp = (byte)Ring0.ReadSmbus(SMB_HSTSTS);
            if (temp != 0x00)
            {
                Ring0.WriteSmbus(SMB_HSTSTS, temp);
            }

            return true;
        }

        private const byte SMB_READ = 0x01;
        private const byte SMB_WRITE = 0x00;

        private const ushort SMB_ADDRESS = 0x0B00;

        private const ushort SMB_HSTSTS = (0 + SMB_ADDRESS);
        //private const ushort SMB_HSLVSTS = (1 + SMB_ADDRESS);
        private const ushort SMB_HSTCNT = (2 + SMB_ADDRESS);
        private const ushort SMB_HSTCMD = (3 + SMB_ADDRESS);
        private const ushort SMB_HSTADD = (4 + SMB_ADDRESS);
        private const ushort SMB_HSTDAT0 = (5 + SMB_ADDRESS);
        private const ushort SMB_HSTDAT1 = (6 + SMB_ADDRESS);
        //private const ushort SMB_BLKDAT = (7 + SMB_ADDRESS);
        //private const ushort SMB_SLVCNT = (8 + SMB_ADDRESS);
        //private const ushort SMB_SHDWCMD = (9 + SMB_ADDRESS);
        //private const ushort SMB_SLVEVT = (0xA + SMB_ADDRESS);
        //private const ushort SMB_SLVDAT = (0xC + SMB_ADDRESS);
    }
}
