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
    internal class W836XX : ISuperIO
    {
        private readonly ushort _address;
        private readonly bool[] _peciTemperature = new bool[0];
        private readonly byte _revision;
        private readonly byte[] _voltageBank = new byte[0];
        private readonly float _voltageGain = 0.008f;
        private readonly byte[] _voltageRegister = new byte[0];

        public W836XX(Chip chip, byte revision, ushort address)
        {
            _address = address;
            _revision = revision;
            Chip = chip;

            if (!IsWinbondVendor())
                return;


            Temperatures = new float?[3];
            _peciTemperature = new bool[3];
            switch (chip)
            {
                case Chip.W83667HG:
                case Chip.W83667HGB:
                    // note temperature sensor registers that read PECI
                    byte flag = ReadByte(0, TEMPERATURE_SOURCE_SELECT_REG);
                    _peciTemperature[0] = (flag & 0x04) != 0;
                    _peciTemperature[1] = (flag & 0x40) != 0;
                    _peciTemperature[2] = false;
                    break;
                case Chip.W83627DHG:
                case Chip.W83627DHGP:
                    // note temperature sensor registers that read PECI
                    byte sel = ReadByte(0, TEMPERATURE_SOURCE_SELECT_REG);
                    _peciTemperature[0] = (sel & 0x07) != 0;
                    _peciTemperature[1] = (sel & 0x70) != 0;
                    _peciTemperature[2] = false;
                    break;
                default:
                    // no PECI support
                    _peciTemperature[0] = false;
                    _peciTemperature[1] = false;
                    _peciTemperature[2] = false;
                    break;
            }

            switch (chip)
            {
                case Chip.W83627EHF:
                    Voltages = new float?[10];
                    _voltageRegister = new byte[] { 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x50, 0x51, 0x52 };
                    _voltageBank = new byte[] { 0, 0, 0, 0, 0, 0, 0, 5, 5, 5 };
                    _voltageGain = 0.008f;
                    Fans = new float?[5];
                    break;
                case Chip.W83627DHG:
                case Chip.W83627DHGP:
                case Chip.W83667HG:
                case Chip.W83667HGB:
                    Voltages = new float?[9];
                    _voltageRegister = new byte[] { 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x50, 0x51 };
                    _voltageBank = new byte[] { 0, 0, 0, 0, 0, 0, 0, 5, 5 };
                    _voltageGain = 0.008f;
                    Fans = new float?[5];
                    break;
                case Chip.W83627HF:
                case Chip.W83627THF:
                case Chip.W83687THF:
                    Voltages = new float?[7];
                    _voltageRegister = new byte[] { 0x20, 0x21, 0x22, 0x23, 0x24, 0x50, 0x51 };
                    _voltageBank = new byte[] { 0, 0, 0, 0, 0, 5, 5 };
                    _voltageGain = 0.016f;
                    Fans = new float?[3];
                    break;
            }
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
        { }

        public void Update()
        {
            if (!Ring0.WaitIsaBusMutex(10))
                return;


            for (int i = 0; i < Voltages.Length; i++)
            {
                if (_voltageRegister[i] != VOLTAGE_VBAT_REG)
                {
                    // two special VCore measurement modes for W83627THF
                    float fValue;
                    if ((Chip == Chip.W83627HF || Chip == Chip.W83627THF || Chip == Chip.W83687THF) && i == 0)
                    {
                        byte vrmConfiguration = ReadByte(0, 0x18);
                        int value = ReadByte(_voltageBank[i], _voltageRegister[i]);
                        if ((vrmConfiguration & 0x01) == 0)
                            fValue = 0.016f * value; // VRM8 formula
                        else
                            fValue = 0.00488f * value + 0.69f; // VRM9 formula
                    }
                    else
                    {
                        int value = ReadByte(_voltageBank[i], _voltageRegister[i]);
                        fValue = _voltageGain * value;
                    }

                    if (fValue > 0)
                        Voltages[i] = fValue;
                    else
                        Voltages[i] = null;
                }
                else
                {
                    // Battery voltage
                    bool valid = (ReadByte(0, 0x5D) & 0x01) > 0;
                    if (valid)
                    {
                        Voltages[i] = _voltageGain * ReadByte(5, VOLTAGE_VBAT_REG);
                    }
                    else
                    {
                        Voltages[i] = null;
                    }
                }
            }

            for (int i = 0; i < Temperatures.Length; i++)
            {
                int value = (sbyte)ReadByte(TEMPERATURE_BANK[i], TEMPERATURE_REG[i]) << 1;
                if (TEMPERATURE_BANK[i] > 0)
                    value |= ReadByte(TEMPERATURE_BANK[i], (byte)(TEMPERATURE_REG[i] + 1)) >> 7;

                float temperature = value / 2.0f;
                if (temperature <= 125 && temperature >= -55 && !_peciTemperature[i])
                {
                    Temperatures[i] = temperature;
                }
                else
                {
                    Temperatures[i] = null;
                }
            }

            ulong bits = 0;
            foreach (byte t in FAN_BIT_REG)
                bits = (bits << 8) | ReadByte(0, t);

            ulong newBits = bits;
            for (int i = 0; i < Fans.Length; i++)
            {
                int count = ReadByte(FAN_TACHO_BANK[i], FAN_TACHO_REG[i]);

                // assemble fan divisor
                int divisorBits = (int)(
                    (((bits >> FAN_DIV_BIT2[i]) & 1) << 2) |
                    (((bits >> FAN_DIV_BIT1[i]) & 1) << 1) |
                    ((bits >> FAN_DIV_BIT0[i]) & 1));

                int divisor = 1 << divisorBits;

                float value = count < 0xff ? 1.35e6f / (count * divisor) : 0;
                Fans[i] = value;

                // update fan divisor
                if (count > 192 && divisorBits < 7)
                    divisorBits++;

                if (count < 96 && divisorBits > 0)
                    divisorBits--;

                newBits = SetBit(newBits, FAN_DIV_BIT2[i], (divisorBits >> 2) & 1);
                newBits = SetBit(newBits, FAN_DIV_BIT1[i], (divisorBits >> 1) & 1);
                newBits = SetBit(newBits, FAN_DIV_BIT0[i], divisorBits & 1);
            }

            // write new fan divisors
            for (int i = FAN_BIT_REG.Length - 1; i >= 0; i--)
            {
                byte oldByte = (byte)(bits & 0xFF);
                byte newByte = (byte)(newBits & 0xFF);
                bits >>= 8;
                newBits >>= 8;
                if (oldByte != newByte)
                    WriteByte(0, FAN_BIT_REG[i], newByte);
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
            r.AppendLine(_address.ToString("X4", CultureInfo.InvariantCulture));
            r.AppendLine();

            if (!Ring0.WaitIsaBusMutex(100))
                return r.ToString();


            r.AppendLine("Hardware Monitor Registers");
            r.AppendLine();
            r.AppendLine("      00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F");
            r.AppendLine();
            for (int i = 0; i <= 0x7; i++)
            {
                r.Append(" ");
                r.Append((i << 4).ToString("X2", CultureInfo.InvariantCulture));
                r.Append("  ");
                for (int j = 0; j <= 0xF; j++)
                {
                    r.Append(" ");
                    r.Append(ReadByte(0, (byte)((i << 4) | j)).ToString("X2", CultureInfo.InvariantCulture));
                }

                r.AppendLine();
            }

            for (int k = 1; k <= 15; k++)
            {
                r.AppendLine("Bank " + k);
                for (int i = 0x5; i < 0x6; i++)
                {
                    r.Append(" ");
                    r.Append((i << 4).ToString("X2", CultureInfo.InvariantCulture));
                    r.Append("  ");
                    for (int j = 0; j <= 0xF; j++)
                    {
                        r.Append(" ");
                        r.Append(ReadByte((byte)k, (byte)((i << 4) | j)).ToString("X2", CultureInfo.InvariantCulture));
                    }

                    r.AppendLine();
                }
            }

            r.AppendLine();
            Ring0.ReleaseIsaBusMutex();
            return r.ToString();
        }

        private byte ReadByte(byte bank, byte register)
        {
            Ring0.WriteIoPort((ushort)(_address + ADDRESS_REGISTER_OFFSET), BANK_SELECT_REGISTER);
            Ring0.WriteIoPort((ushort)(_address + DATA_REGISTER_OFFSET), bank);
            Ring0.WriteIoPort((ushort)(_address + ADDRESS_REGISTER_OFFSET), register);
            return Ring0.ReadIoPort((ushort)(_address + DATA_REGISTER_OFFSET));
        }

        private void WriteByte(byte bank, byte register, byte value)
        {
            Ring0.WriteIoPort((ushort)(_address + ADDRESS_REGISTER_OFFSET), BANK_SELECT_REGISTER);
            Ring0.WriteIoPort((ushort)(_address + DATA_REGISTER_OFFSET), bank);
            Ring0.WriteIoPort((ushort)(_address + ADDRESS_REGISTER_OFFSET), register);
            Ring0.WriteIoPort((ushort)(_address + DATA_REGISTER_OFFSET), value);
        }

        private bool IsWinbondVendor()
        {
            ushort vendorId = (ushort)((ReadByte(HIGH_BYTE, VENDOR_ID_REGISTER) << 8) | ReadByte(0, VENDOR_ID_REGISTER));
            return vendorId == WINBOND_VENDOR_ID;
        }

        private static ulong SetBit(ulong target, int bit, int value)
        {
            if ((value & 1) != value)
                throw new ArgumentException("Value must be one bit only.");

            if (bit < 0 || bit > 63)
                throw new ArgumentException("Bit out of range.");


            ulong mask = (ulong)1 << bit;
            return value > 0 ? target | mask : target & ~mask;
        }
        // ReSharper disable InconsistentNaming

        private const byte ADDRESS_REGISTER_OFFSET = 0x05;
        private const byte BANK_SELECT_REGISTER = 0x4E;
        private const byte DATA_REGISTER_OFFSET = 0x06;
        private const byte HIGH_BYTE = 0x80;
        private const byte TEMPERATURE_SOURCE_SELECT_REG = 0x49;
        private const byte VENDOR_ID_REGISTER = 0x4F;
        private const byte VOLTAGE_VBAT_REG = 0x51;

        private const ushort WINBOND_VENDOR_ID = 0x5CA3;

        private readonly byte[] FAN_BIT_REG = { 0x47, 0x4B, 0x4C, 0x59, 0x5D };
        private readonly byte[] FAN_DIV_BIT0 = { 36, 38, 30, 8, 10 };
        private readonly byte[] FAN_DIV_BIT1 = { 37, 39, 31, 9, 11 };
        private readonly byte[] FAN_DIV_BIT2 = { 5, 6, 7, 23, 15 };
        private readonly byte[] FAN_TACHO_BANK = { 0, 0, 0, 0, 5 };
        private readonly byte[] FAN_TACHO_REG = { 0x28, 0x29, 0x2A, 0x3F, 0x53 };
        private readonly byte[] TEMPERATURE_BANK = { 1, 2, 0 };
        private readonly byte[] TEMPERATURE_REG = { 0x50, 0x50, 0x27 };

        // ReSharper restore InconsistentNaming
    }
}
