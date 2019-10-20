// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Globalization;
using System.Text;

// ReSharper disable once InconsistentNaming

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc
{
    internal class IT87XX : ISuperIO
    {
        private readonly ushort _address;
        private readonly ushort _addressReg;
        private readonly ushort _dataReg;
        private readonly bool[] _fansDisabled = new bool[0];

        private readonly ushort _gpioAddress;
        private readonly int _gpioCount;
        private readonly bool _has16BitFanCounter;
        private readonly bool _hasNewerAutoPwm;

        private readonly byte[] _initialFanPwmControl = new byte[3];
        private readonly byte[] _initialFanPwmControlMode = new byte[3];

        private readonly bool[] _restoreDefaultFanPwmControlRequired = new bool[3];
        private readonly byte _version;

        private readonly float _voltageGain;

        public IT87XX(Chip chip, ushort address, ushort gpioAddress, byte version)
        {
            _address = address;
            Chip = chip;
            _version = version;
            _addressReg = (ushort)(address + ADDRESS_REGISTER_OFFSET);
            _dataReg = (ushort)(address + DATA_REGISTER_OFFSET);
            _gpioAddress = gpioAddress;

            // Check vendor id
            byte vendorId = ReadByte(VENDOR_ID_REGISTER, out bool valid);
            if (!valid || vendorId != ITE_VENDOR_ID)
                return;


            // Bit 0x10 of the configuration register should always be 1
            byte config = ReadByte(CONFIGURATION_REGISTER, out valid);
            if ((config & 0x10) == 0 && chip != Chip.IT8665E)
                return;

            if (!valid)
                return;


            // IT8686E has more sensors
            if (chip == Chip.IT8686E)
            {
                Voltages = new float?[10];
                Temperatures = new float?[5];
                Fans = new float?[5];
                _fansDisabled = new bool[5];
                Controls = new float?[3];
            }
            else if (chip == Chip.IT8665E)
            {
                Voltages = new float?[10];
                Temperatures = new float?[6];
                Fans = new float?[6];
                _fansDisabled = new bool[6];
                Controls = new float?[3];
            }
            else if (chip == Chip.IT8688E)
            {
                Voltages = new float?[11];
                Temperatures = new float?[6];
                Fans = new float?[5];
                _fansDisabled = new bool[5];
                Controls = new float?[3];
            }
            else if (chip == Chip.IT8792E)
            {
                Voltages = new float?[8];
                Temperatures = new float?[3];
                Fans = new float?[3];
                _fansDisabled = new bool[3];
                Controls = new float?[3];
            }
            else
            {
                Voltages = new float?[9];
                Temperatures = new float?[3];
                Fans = new float?[chip == Chip.IT8705F ? 3 : 5];
                _fansDisabled = new bool[chip == Chip.IT8705F ? 3 : 5];
                Controls = new float?[3];
            }

            // IT8620E, IT8628E, IT8721F, IT8728F, IT8772E and IT8686E use a 12mV resolution
            // ADC, all others 16mV
            if (chip == Chip.IT8620E ||
                chip == Chip.IT8628E ||
                chip == Chip.IT8721F ||
                chip == Chip.IT8728F ||
                chip == Chip.IT8771E ||
                chip == Chip.IT8772E ||
                chip == Chip.IT8686E ||
                chip == Chip.IT8688E ||
                chip == Chip.IT8792E
                )
            {
                _voltageGain = 0.012f;
            }
            else if (chip == Chip.IT8665E)
            {
                _voltageGain = 0.0109f;
            }
            else
            {
                _voltageGain = 0.016f;
            }

            // older IT8705F and IT8721F revisions do not have 16-bit fan counters
            if ((chip == Chip.IT8705F && version < 3) ||
                (chip == Chip.IT8712F && version < 8))
            {
                _has16BitFanCounter = false;
            }
            else
            {
                _has16BitFanCounter = true;
            }

            if (chip == Chip.IT8620E || chip == Chip.IT8792E)
            {
                _hasNewerAutoPwm = true;
            }

            // Disable any fans that aren't set with 16-bit fan counters
            if (_has16BitFanCounter)
            {
                int modes = ReadByte(FAN_TACHOMETER_16BIT_REGISTER, out valid);

                if (!valid)
                    return;


                if (Fans.Length >= 5)
                {
                    _fansDisabled[3] = (modes & (1 << 4)) == 0;
                    _fansDisabled[4] = (modes & (1 << 5)) == 0;
                }

                if (Fans.Length >= 6)
                {
                    _fansDisabled[5] = (modes & (1 << 2)) == 0;
                }
            }

            // Set the number of GPIO sets
            switch (chip)
            {
                case Chip.IT8712F:
                case Chip.IT8716F:
                case Chip.IT8718F:
                case Chip.IT8726F:
                    _gpioCount = 5;
                    break;
                case Chip.IT8720F:
                case Chip.IT8721F:
                    _gpioCount = 8;
                    break;
                case Chip.IT8620E:
                case Chip.IT8628E:
                case Chip.IT8688E:
                case Chip.IT8705F:
                case Chip.IT8728F:
                case Chip.IT8771E:
                case Chip.IT8772E:
                    _gpioCount = 0;
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
            if (index >= _gpioCount)
                return null;


            return Ring0.ReadIoPort((ushort)(_gpioAddress + index));
        }

        public void WriteGpio(int index, byte value)
        {
            if (index >= _gpioCount)
                return;


            Ring0.WriteIoPort((ushort)(_gpioAddress + index), value);
        }

        public void SetControl(int index, byte? value)
        {
            if (index < 0 || index >= Controls.Length)
                throw new ArgumentOutOfRangeException(nameof(index));


            if (!Ring0.WaitIsaBusMutex(10))
                return;


            if (value.HasValue)
            {
                SaveDefaultFanPwmControl(index);

                if (_hasNewerAutoPwm)
                {
                    byte ctrlValue = ReadByte(FAN_PWM_CTRL_REG[index], out bool valid);

                    if (valid)
                    {
                        bool isOnAutoControl = (ctrlValue & (1 << 7)) > 0;
                        if (isOnAutoControl)
                        {
                            // Set to manual speed control
                            ctrlValue &= byte.MaxValue ^ (1 << 7);
                            WriteByte(FAN_PWM_CTRL_REG[index], ctrlValue);
                        }
                    }

                    // set speed
                    WriteByte(FAN_PWM_DUTY_REG[index], value.Value);
                }
                else
                {
                    // set output value
                    WriteByte(FAN_PWM_CTRL_REG[index], (byte)(value.Value >> 1));
                }
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
            r.Append("Chip ID: 0x");
            r.AppendLine(Chip.ToString("X"));
            r.Append("Chip Version: 0x");
            r.AppendLine(_version.ToString("X", CultureInfo.InvariantCulture));
            r.Append("Base Address: 0x");
            r.AppendLine(_address.ToString("X4", CultureInfo.InvariantCulture));
            r.Append("GPIO Address: 0x");
            r.AppendLine(_gpioAddress.ToString("X4", CultureInfo.InvariantCulture));
            r.AppendLine();

            if (!Ring0.WaitIsaBusMutex(100))
                return r.ToString();


            r.AppendLine("Environment Controller Registers");
            r.AppendLine();
            r.AppendLine("      00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F");
            r.AppendLine();
            for (int i = 0; i <= 0xA; i++)
            {
                r.Append(" ");
                r.Append((i << 4).ToString("X2", CultureInfo.InvariantCulture));
                r.Append("  ");
                for (int j = 0; j <= 0xF; j++)
                {
                    r.Append(" ");
                    byte value = ReadByte((byte)((i << 4) | j), out bool valid);
                    r.Append(
                             valid ? value.ToString("X2", CultureInfo.InvariantCulture) : "??");
                }

                r.AppendLine();
            }

            r.AppendLine();

            r.AppendLine("GPIO Registers");
            r.AppendLine();
            for (int i = 0; i < _gpioCount; i++)
            {
                r.Append(" ");
                r.Append(ReadGpio(i)?.ToString("X2", CultureInfo.InvariantCulture));
            }

            r.AppendLine();
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
                float value = _voltageGain * ReadByte((byte)(VOLTAGE_BASE_REG + i), out bool valid);

                if (!valid)
                    continue;


                if (value > 0)
                    Voltages[i] = value;
                else
                    Voltages[i] = null;
            }

            for (int i = 0; i < Temperatures.Length; i++)
            {
                sbyte value = (sbyte)ReadByte((byte)(TEMPERATURE_BASE_REG + i), out bool valid);
                if (!valid)
                    continue;


                if (value < sbyte.MaxValue && value > 0)
                    Temperatures[i] = value;
                else
                    Temperatures[i] = null;
            }

            if (_has16BitFanCounter)
            {
                for (int i = 0; i < Fans.Length; i++)
                {
                    if (_fansDisabled[i])
                        continue;


                    int value = ReadByte(FAN_TACHOMETER_REG[i], out bool valid);
                    if (!valid)
                        continue;


                    value |= ReadByte(FAN_TACHOMETER_EXT_REG[i], out valid) << 8;
                    if (!valid)
                        continue;


                    if (value > 0x3f)
                        Fans[i] = value < 0xffff ? 1.35e6f / (value * 2) : 0;
                    else
                        Fans[i] = null;
                }
            }
            else
            {
                for (int i = 0; i < Fans.Length; i++)
                {
                    int value = ReadByte(FAN_TACHOMETER_REG[i], out bool valid);
                    if (!valid)
                        continue;


                    int divisor = 2;
                    if (i < 2)
                    {
                        int divisors = ReadByte(FAN_TACHOMETER_DIVISOR_REGISTER, out valid);
                        if (!valid)
                            continue;


                        divisor = 1 << ((divisors >> (3 * i)) & 0x7);
                    }

                    if (value > 0)
                        Fans[i] = value < 0xff ? 1.35e6f / (value * divisor) : 0;
                    else
                        Fans[i] = null;
                }
            }

            for (int i = 0; i < Controls.Length; i++)
            {
                if (_hasNewerAutoPwm)
                {
                    byte value = ReadByte(FAN_PWM_DUTY_REG[i], out bool valid);
                    if (!valid)
                        continue;


                    byte ctrlValue = ReadByte(FAN_PWM_CTRL_REG[i], out valid);
                    if (!valid)
                        continue;


                    if ((ctrlValue & 0x80) > 0)
                        Controls[i] = null; // automatic operation (value can't be read)
                    else
                        Controls[i] = (float)Math.Round(value * 100.0f / 0xFF);
                }
                else
                {
                    byte value = ReadByte(FAN_PWM_CTRL_REG[i], out bool valid);
                    if (!valid)
                        continue;


                    if ((value & 0x80) > 0)
                    {
                        // automatic operation (value can't be read)
                        Controls[i] = null;
                    }
                    else
                    {
                        // software operation
                        Controls[i] = (float)Math.Round((value & 0x7F) * 100.0f / 0x7F);
                    }
                }
            }

            Ring0.ReleaseIsaBusMutex();
        }

        private byte ReadByte(byte register, out bool valid)
        {
            Ring0.WriteIoPort(_addressReg, register);
            byte value = Ring0.ReadIoPort(_dataReg);
            valid = register == Ring0.ReadIoPort(_addressReg);
            // IT8688E doesn't return the value we wrote to
            // addressReg when we read it back.
            if (Chip == Chip.IT8688E)
                valid = true;

            return value;
        }

        private void WriteByte(byte register, byte value)
        {
            Ring0.WriteIoPort(_addressReg, register);
            Ring0.WriteIoPort(_dataReg, value);
            Ring0.ReadIoPort(_addressReg);
        }

        private void SaveDefaultFanPwmControl(int index)
        {
            if (_hasNewerAutoPwm)
            {
                if (!_restoreDefaultFanPwmControlRequired[index])
                {
                    _initialFanPwmControlMode[index] = ReadByte(FAN_PWM_CTRL_REG[index], out bool _);
                    _initialFanPwmControl[index] = ReadByte(FAN_PWM_DUTY_REG[index], out bool _);
                }
            }
            else
            {
                if (!_restoreDefaultFanPwmControlRequired[index])
                    _initialFanPwmControl[index] = ReadByte(FAN_PWM_CTRL_REG[index], out bool _);
            }

            _restoreDefaultFanPwmControlRequired[index] = true;
        }

        private void RestoreDefaultFanPwmControl(int index)
        {
            if (_hasNewerAutoPwm)
            {
                if (_restoreDefaultFanPwmControlRequired[index])
                {
                    WriteByte(FAN_PWM_CTRL_REG[index], _initialFanPwmControlMode[index]);
                    WriteByte(FAN_PWM_DUTY_REG[index], _initialFanPwmControl[index]);
                    _restoreDefaultFanPwmControlRequired[index] = false;
                }
            }
            else
            {
                if (_restoreDefaultFanPwmControlRequired[index])
                {
                    WriteByte(FAN_PWM_CTRL_REG[index], _initialFanPwmControl[index]);
                    _restoreDefaultFanPwmControlRequired[index] = false;
                }
            }
        }

        // ReSharper disable InconsistentNaming
        private const byte ADDRESS_REGISTER_OFFSET = 0x05;

        private const byte CONFIGURATION_REGISTER = 0x00;
        private const byte DATA_REGISTER_OFFSET = 0x06;
        private const byte FAN_TACHOMETER_16BIT_REGISTER = 0x0C;
        private const byte FAN_TACHOMETER_DIVISOR_REGISTER = 0x0B;

        private const byte ITE_VENDOR_ID = 0x90;
        private const byte TEMPERATURE_BASE_REG = 0x29;
        private const byte VENDOR_ID_REGISTER = 0x58;
        private const byte VOLTAGE_BASE_REG = 0x20;

        private readonly byte[] FAN_PWM_CTRL_REG = { 0x15, 0x16, 0x17 };
        private readonly byte[] FAN_PWM_DUTY_REG = { 0x63, 0x6b, 0x73 };
        private readonly byte[] FAN_TACHOMETER_EXT_REG = { 0x18, 0x19, 0x1a, 0x81, 0x83, 0x4c };

        private readonly byte[] FAN_TACHOMETER_REG = { 0x0d, 0x0e, 0x0f, 0x80, 0x82, 0x4c };
        // ReSharper restore InconsistentNaming
    }
}
