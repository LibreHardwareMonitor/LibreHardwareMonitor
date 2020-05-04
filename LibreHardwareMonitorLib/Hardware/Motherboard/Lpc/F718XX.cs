// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Globalization;
using System.Text;

// ReSharper disable once InconsistentNaming

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc
{
    internal class F718XX : ISuperIO
    {
        private readonly ushort _address;
        private readonly byte[] _initialFanPwmControl = new byte[4];
        private readonly bool[] _restoreDefaultFanPwmControlRequired = new bool[4];

        public F718XX(Chip chip, ushort address)
        {
            _address = address;
            Chip = chip;

            Voltages = new float?[chip == Chip.F71858 ? 3 : 9];
            Temperatures = new float?[chip == Chip.F71808E ? 2 : 3];
            Fans = new float?[chip == Chip.F71882 || chip == Chip.F71858 ? 4 : 3];
            Controls = new float?[chip == Chip.F71878AD ? 3 : 0];
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


            if (!Ring0.WaitIsaBusMutex(10))
                return;


            if (value.HasValue)
            {
                SaveDefaultFanPwmControl(index);

                WriteByte(FAN_PWM_REG[index], value.Value);
            }
            else
            {
                RestoreDefaultFanPwmControl(index);
            }

            Ring0.ReleaseIsaBusMutex();
        }

        public string GetReport()
        {
            StringBuilder r = new StringBuilder();

            r.AppendLine("LPC " + GetType().Name);
            r.AppendLine();
            r.Append("Base Address: 0x");
            r.AppendLine(_address.ToString("X4", CultureInfo.InvariantCulture));
            r.AppendLine();

            if (!Ring0.WaitIsaBusMutex(100))
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

            Ring0.ReleaseIsaBusMutex();
            return r.ToString();
        }

        public void Update()
        {
            if (!Ring0.WaitIsaBusMutex(10))
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
                        int high = ReadByte((byte)(TEMPERATURE_BASE_REG + 2 * i));
                        int low = ReadByte((byte)(TEMPERATURE_BASE_REG + 2 * i + 1));
                        if (high != 0xbb && high != 0xcc)
                        {
                            int bits = 0;
                            switch (tableMode)
                            {
                                case 0:
                                    bits = 0;
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
                        sbyte value = (sbyte)ReadByte((byte)(TEMPERATURE_BASE_REG + 2 * (i + 1)));
                        if (value < sbyte.MaxValue && value > 0)
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

            for (int i = 0; i < Controls.Length; i++)
            {
                Controls[i] = ReadByte((byte)(PWM_VALUES_OFFSET + i)) * 100.0f / 0xFF;
            }

            Ring0.ReleaseIsaBusMutex();
        }

        private void SaveDefaultFanPwmControl(int index)
        {
            if (!_restoreDefaultFanPwmControlRequired[index])
            {
                _initialFanPwmControl[index] = ReadByte(FAN_PWM_REG[index]);
                _restoreDefaultFanPwmControlRequired[index] = true;
            }
        }

        private void RestoreDefaultFanPwmControl(int index)
        {
            if (_restoreDefaultFanPwmControlRequired[index])
            {
                WriteByte(FAN_PWM_REG[index], _initialFanPwmControl[index]);
                _restoreDefaultFanPwmControlRequired[index] = false;
            }
        }

        private byte ReadByte(byte register)
        {
            Ring0.WriteIoPort((ushort)(_address + ADDRESS_REGISTER_OFFSET), register);
            return Ring0.ReadIoPort((ushort)(_address + DATA_REGISTER_OFFSET));
        }

        private void WriteByte(byte register, byte value)
        {
            Ring0.WriteIoPort((ushort)(_address + ADDRESS_REGISTER_OFFSET), register);
            Ring0.WriteIoPort((ushort)(_address + DATA_REGISTER_OFFSET), value);
        }

        // ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles

        private const byte ADDRESS_REGISTER_OFFSET = 0x05;
        private const byte DATA_REGISTER_OFFSET = 0x06;
        private const byte PWM_VALUES_OFFSET = 0x2D;
        private const byte TEMPERATURE_BASE_REG = 0x70;
        private const byte TEMPERATURE_CONFIG_REG = 0x69;

        private const byte VOLTAGE_BASE_REG = 0x20;
        private readonly byte[] FAN_PWM_REG = { 0xA3, 0xB3, 0xC3, 0xD3 };
        private readonly byte[] FAN_TACHOMETER_REG = { 0xA0, 0xB0, 0xC0, 0xD0 };

        // ReSharper restore InconsistentNaming
#pragma warning restore IDE1006 // Naming Styles
    }
}
