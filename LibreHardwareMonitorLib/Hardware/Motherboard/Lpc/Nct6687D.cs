// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael M?ler <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Threading;

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc
{
    internal class Nct6687D : ISuperIO
    {
        private const uint PAGE_REGISTER_OFFSET = 0x04;
        private const uint INDEX_REGISTER_OFFSET = 0x05;
        private const uint DATA_REGISTER_OFFSET = 0x06;
        private const byte PAGE_SELECT_REGISTER = 0xFF;

        private readonly LpcPort _lpcPort;
        private readonly ushort _port;

        private readonly byte _revision;

        private readonly ushort _initRegister;

        private readonly ushort[] _temperatureRegister;
        private readonly ushort[] _voltageRegisters;

        private readonly bool[] _restoreDefaultFanControlRequired;
        private readonly byte[] _initialFanPwmCommand;

        private readonly ushort[] _fanRpmRegister;
        private readonly ushort[] _fanPwmOutRegisters;
        private readonly ushort[] _fanPwmCommandRegisters;

        private readonly ushort _fanControlManualModeRegister;
        private readonly ushort _fanControlRequestRegister;

        private readonly byte _fanControlRequestStartCommand;
        private readonly byte _fanControlRequestEndCommand;

        public Nct6687D(Chip chip, byte revision, ushort ecAddressPort, LpcPort lpcPort)
        {
            Chip = chip;
            _revision = revision;
            _port = ecAddressPort;
            _lpcPort = lpcPort;

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
            _temperatureRegister = new ushort[] { 0x100, 0x102, 0x104, 0x106, 0x108, 0x10A, 0x10C };

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
            _fanPwmOutRegisters = new ushort[] { 0x160, 0x161, 0x162, 0x163, 0x164, 0x165, 0x166, 0x167 };
            _fanPwmCommandRegisters = new ushort[] { 0xA28, 0xA29, 0xA2A, 0xA2B, 0xA2C, 0xA2D, 0xA2E, 0xA2F };
            _fanControlManualModeRegister = 0xA00;
            _fanControlRequestRegister = 0xA01;
            _fanControlRequestStartCommand = 0x80;
            _fanControlRequestEndCommand = 0x40;

            _restoreDefaultFanControlRequired = new bool[_fanPwmCommandRegisters.Length];
            _initialFanPwmCommand = new byte[_fanPwmCommandRegisters.Length];

            // initialize
            _initRegister = 0x180;
            byte data = ReadByte(_initRegister);
            if ((data & 0x80) == 0)
            {
                WriteByte(_initRegister, (byte)(data | 0x80));
            }

            // enable SIO voltage
            WriteByte(0x1BB, 0x61);
            WriteByte(0x1BC, 0x62);
            WriteByte(0x1BD, 0x63);
            WriteByte(0x1BE, 0x64);
            WriteByte(0x1BF, 0x65);
        }

        ~Nct6687D()
        {
            // Set bios mode
            WriteByte(_fanControlManualModeRegister, 0x00);
        }

        public Chip Chip { get; }

        public float?[] Controls { get; } = new float?[0];

        public float?[] Fans { get; } = new float?[0];

        public float?[] Temperatures { get; } = new float?[0];

        public float?[] Voltages { get; } = new float?[0];

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


            if (!Ring0.WaitIsaBusMutex(10))
                return;

            if (value.HasValue)
            {
                SaveDefaultFanControl(index);

                // Manual mode, bit(1 : set, 0 : unset)
                // bit 0 : CPU Fan
                // bit 1 : PUMP Fan
                // bit 2 : SYS Fan 1
                // bit 3 : SYS Fan 2
                // bit 4 : SYS Fan 3
                // bit 5 : SYS Fan 4
                // bit 6 : SYS Fan 5
                // bit 7 : SYS Fan 6
                byte mode = ReadByte(_fanControlManualModeRegister);
                byte bitMask = (byte)(0x01 << index);
                mode = (byte)(mode | bitMask);                
                WriteByte(_fanControlManualModeRegister, mode);

                WriteByte(_fanControlRequestRegister, _fanControlRequestStartCommand);

                // need sleep
                Thread.Sleep(100);

                WriteByte(_fanPwmCommandRegisters[index], value.Value);
                WriteByte(_fanControlRequestRegister, _fanControlRequestEndCommand);
            }
            else
            {
                RestoreDefaultFanControl(index);
            }

            Ring0.ReleaseIsaBusMutex();
        }

        public void Update()
        {
            if (!Ring0.WaitIsaBusMutex(10))
                return;

            for (int i = 0; i < Voltages.Length; i++)
            {
                float value = 0.001f * (16 * ReadByte(_voltageRegisters[i]) + (ReadByte((ushort)(_voltageRegisters[i] + 1)) >> 4));

                // 12V
                if (i == 0)
                {
                    Voltages[i] = value * 12.0f;
                }
                // 5V
                else if (i == 1)
                {
                    Voltages[i] = value * 5.0f;
                }
                else
                {
                    Voltages[i] = value;
                }
            }

            for (int i = 0; i < _temperatureRegister.Length; i++)
            {
                int value = (sbyte)ReadByte(_temperatureRegister[i]);
                int half = (ReadByte((ushort)(_temperatureRegister[i] + 1)) >> 7) & 0x1;
                float temperature = value + (0.5f * half);
                Temperatures[i] = temperature;
            }
            
            for (int i = 0; i < Fans.Length; i++)
            {
                int value = (ReadByte(_fanRpmRegister[i]) << 8) | ReadByte((ushort)(_fanRpmRegister[i] + 1));
                Fans[i] = value;
            }

            for (int i = 0; i < Controls.Length; i++)
            {
                int value = ReadByte(_fanPwmOutRegisters[i]);
                Controls[i] = (float)Math.Round(value / 2.55f);
            }

            Ring0.ReleaseIsaBusMutex();
        }

        public string GetReport()
        {
            StringBuilder r = new StringBuilder();

            r.AppendLine("LPC " + GetType().Name);
            r.AppendLine();
            r.Append("Chip Id: 0x");
            r.AppendLine(Chip.ToString("X"));
            r.Append("Chip Revision: 0x");
            r.AppendLine(_revision.ToString("X", CultureInfo.InvariantCulture));
            r.Append("Base Address: 0x");
            r.AppendLine(_port.ToString("X4", CultureInfo.InvariantCulture));
            r.AppendLine();

            if (!Ring0.WaitIsaBusMutex(100))
                return r.ToString();

            r.AppendLine("Hardware Monitor Registers");
            r.AppendLine();

            r.AppendLine("        00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F");
            r.AppendLine();
            
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
            r.AppendLine();

            Ring0.ReleaseIsaBusMutex();

            return r.ToString();
        }

        private byte ReadByte(ushort address)
        {
            byte page = (byte)(address >> 8);
            byte index = (byte)(address & 0xFF);
            Ring0.WriteIoPort(_port + PAGE_REGISTER_OFFSET, PAGE_SELECT_REGISTER);
            Ring0.WriteIoPort(_port + PAGE_REGISTER_OFFSET, page);
            Ring0.WriteIoPort(_port + INDEX_REGISTER_OFFSET, index);
            return Ring0.ReadIoPort(_port + DATA_REGISTER_OFFSET);
        }

        private void WriteByte(ushort address, byte value)
        {
            byte page = (byte)(address >> 8);
            byte index = (byte)(address & 0xFF);
            Ring0.WriteIoPort(_port + PAGE_REGISTER_OFFSET, PAGE_SELECT_REGISTER);
            Ring0.WriteIoPort(_port + PAGE_REGISTER_OFFSET, page);
            Ring0.WriteIoPort(_port + INDEX_REGISTER_OFFSET, index);
            Ring0.WriteIoPort(_port + DATA_REGISTER_OFFSET, value);
        }

        private void SaveDefaultFanControl(int index)
        {
            if (!_restoreDefaultFanControlRequired[index])
            {
                _initialFanPwmCommand[index] = ReadByte(_fanPwmCommandRegisters[index]);
                _restoreDefaultFanControlRequired[index] = true;
            }
        }

        private void RestoreDefaultFanControl(int index)
        {
            if (_restoreDefaultFanControlRequired[index])
            {
                byte mode = ReadByte(_fanControlManualModeRegister);
                byte bitMask = (byte)(0x01 << index);
                mode = (byte)(mode & ~bitMask);
                WriteByte(_fanControlManualModeRegister, mode);

                WriteByte(_fanControlRequestRegister, _fanControlRequestStartCommand);

                // need sleep
                Thread.Sleep(100);

                WriteByte(_fanPwmCommandRegisters[index], _initialFanPwmCommand[index]);
                WriteByte(_fanControlRequestRegister, _fanControlRequestEndCommand);

                _restoreDefaultFanControlRequired[index] = false;
            }
        }

    }
}
