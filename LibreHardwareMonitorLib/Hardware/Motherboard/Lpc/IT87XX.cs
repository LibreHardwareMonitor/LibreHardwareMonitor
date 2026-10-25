// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Globalization;
using System.Linq;
using System.Text;

// ReSharper disable once InconsistentNaming

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc;

internal class IT87XX : ISuperIO
{
    private const int MaxFanHeaders = 6;
    private readonly ushort _address;
    private readonly ushort _addressReg;
    private readonly int _bankCount;
    private readonly ushort _dataReg;
    private readonly bool[] _fansDisabled = Array.Empty<bool>();
    private readonly ushort _gpioAddress;
    private readonly int _gpioCount;
    private readonly bool _has16BitFanCounter;
    private readonly bool _hasExtReg;
    private readonly bool _hasAlt6thFanReg;
    private readonly bool[] _initialFanOutputModeEnabled = new bool[3]; // Initial Fan Controller Main Control Register value. 
    private readonly byte[] _initialFanPwmControl = new byte[MaxFanHeaders]; // This will also store the 2nd control register value.
    private readonly byte[] _initialFanPwmControlExt = new byte[MaxFanHeaders];
    private readonly bool[] _restoreDefaultFanPwmControlRequired = new bool[MaxFanHeaders];
    private readonly byte _version;
    private readonly float _voltageGain;
    private IGigabyteController _gigabyteController;
    private readonly LpcPort _port;
    private readonly LibreHardwareMonitor.PawnIo.LpcAcpiEc _lpcAcpiEc;
    private readonly byte[][] _initialEcCurve = new byte[6][];
    private readonly byte[] _initialSioTempLimit = new byte[3];
    private readonly byte[] _initialSioPwm = new byte[3];
    private readonly byte[] _initialSioManualPwm = new byte[3];
    private readonly bool _requiresBankSelect;  // Fix #780 Set to true for those chips that need a SelectBank(0) to fix dodgy temps and fan speeds

    private bool SupportsMultipleBanks => _bankCount > 1;

    public IT87XX(LpcPort port, Chip chip, ushort address, ushort gpioAddress, byte version, Motherboard motherboard, IGigabyteController gigabyteController)
    {
        _port = port;
        _address = address;
        _version = version;
        _addressReg = (ushort)(address + ADDRESS_REGISTER_OFFSET);
        _dataReg = (ushort)(address + DATA_REGISTER_OFFSET);
        _gpioAddress = gpioAddress;
        _gigabyteController = gigabyteController;
        _requiresBankSelect = false;

        Chip = chip;

        // Check vendor id
        byte vendorId = ReadByte(VENDOR_ID_REGISTER, out bool valid);
        if (!valid)
            return;

        bool hasMatchingVendorId = false;
        foreach (byte iteVendorId in ITE_VENDOR_IDS)
        {
            if (iteVendorId == vendorId)
            {
                hasMatchingVendorId = true;
                break;
            }
        }

        if (!hasMatchingVendorId)
            return;

        // Bit 0x10 of the configuration register should always be 1
        byte configuration = ReadByte(CONFIGURATION_REGISTER, out valid);
        if (!valid || ((configuration & 0x10) == 0 && chip != Chip.IT8655E && chip != Chip.IT8665E))
            return;

        FAN_PWM_CTRL_REG = chip switch
        {
            Chip.IT8665E or Chip.IT8625E => new byte[] { 0x15, 0x16, 0x17, 0x1e, 0x1f, 0x92 },
            Chip.IT8792E => new byte[] { 0x15, 0x16, 0x17 },
            _ => new byte[] { 0x15, 0x16, 0x17, 0x7f, 0xa7, 0xaf }
        };

        _bankCount = chip switch
        {
            Chip.IT8689E => 4,
            _ => 1
        };

        _hasExtReg = chip is Chip.IT8721F or
            Chip.IT8728F or
            Chip.IT8665E or
            Chip.IT8686E or
            Chip.IT8688E or
            Chip.IT8689E or
            Chip.IT87952E or
            Chip.IT8628E or
            Chip.IT8625E or
            Chip.IT8620E or
            Chip.IT8613E or
            Chip.IT8792E or
            Chip.IT8655E or
            Chip.IT8631E or
            Chip.IT8638E or
            Chip.IT8696E or
            Chip.IT8772E;

        _hasAlt6thFanReg = chip is Chip.IT8665E or Chip.IT8625E;

        switch (chip)
        {
            case Chip.IT8613E:
                Voltages = new float?[10];
                Temperatures = new float?[4];
                Fans = motherboard.Model == Model.MS_iCraft_Z890_Arctic ? new float?[6] : new float?[5];
                Controls = motherboard.Model == Model.MS_iCraft_Z890_Arctic ? new float?[6] : new float?[4];
                break;

            case Chip.IT8625E:
                Voltages = new float?[7];
                Temperatures = new float?[3];
                Fans = new float?[6];
                Controls = new float?[6];
                break;
            case Chip.IT8628E:
                Voltages = new float?[10];
                Temperatures = new float?[6];
                Fans = new float?[6];
                Controls = new float?[6];
                break;

            case Chip.IT8631E:
                Voltages = new float?[9];
                Temperatures = new float?[2];
                Fans = new float?[2];
                Controls = new float?[2];
                break;

            case Chip.IT8638E:
                Voltages = new float?[9];
                Temperatures = new float?[2];
                Fans = new float?[2];
                Controls = new float?[2];
                break;

            case Chip.IT8665E:
                Voltages = new float?[9];
                Temperatures = new float?[6];
                Fans = new float?[6];
                Controls = new float?[6];
                _requiresBankSelect = true;
                break;

            case Chip.IT8686E:
                Voltages = new float?[10];
                Temperatures = new float?[7];
                Fans = new float?[6];
                Controls = new float?[5];
                break;

            case Chip.IT8688E:
                Voltages = new float?[11];
                Temperatures = new float?[6];
                Fans = new float?[6];
                Controls = new float?[5];
                break;

            case Chip.IT8689E:
                Voltages = new float?[10];
                Temperatures = new float?[6];
                Fans = new float?[6];
                Controls = new float?[6];
                break;

            case Chip.IT8696E:
                Voltages = new float?[10];
                Temperatures = new float?[6];
                Fans = new float?[6];
                Controls = new float?[6];
                break;

            case Chip.IT87952E:
                Voltages = new float?[9];
                Temperatures = new float?[3];
                Fans = new float?[3];
                Controls = new float?[3];
                break;

            case Chip.IT8655E:
                Voltages = new float?[9];
                Temperatures = new float?[6];
                Fans = new float?[3];
                Controls = new float?[3];
                _requiresBankSelect = true;
                break;

            case Chip.IT8792E:
                Voltages = new float?[9];
                Temperatures = new float?[3];
                Fans = new float?[3];
                Controls = new float?[3];
                break;

            case Chip.IT8705F:
                Voltages = new float?[9];
                Temperatures = new float?[3];
                Fans = new float?[3];
                Controls = new float?[3];
                break;

            case Chip.IT8620E:
                Voltages = new float?[9];
                Temperatures = new float?[3];
                Fans = new float?[5];
                Controls = new float?[5];
                break;

            default:
                Voltages = new float?[9];
                Temperatures = new float?[3];
                Fans = new float?[5];
                Controls = new float?[3];
                break;
        }

        _fansDisabled = new bool[Fans.Length];

        // Voltage gain varies by model.
        // Conflicting reports on IT8792E: either 0.0109 in linux drivers or 0.011 comparing with Gigabyte board & SIV SW.
        _voltageGain = chip switch
        {
            Chip.IT8613E or Chip.IT8620E or Chip.IT8628E or Chip.IT8631E or Chip.IT8638E or Chip.IT8721F or Chip.IT8728F or Chip.IT8771E or Chip.IT8772E or Chip.IT8686E or Chip.IT8688E or Chip.IT8689E or Chip.IT8696E => 0.012f,
            Chip.IT8625E or Chip.IT8792E or Chip.IT87952E => 0.011f,
            Chip.IT8655E or Chip.IT8665E => 0.0109f,
            _ => 0.016f
        };

        // Older IT8705F and IT8721F revisions do not have 16-bit fan counters.
        _has16BitFanCounter = (chip != Chip.IT8705F || version >= 3) && (chip != Chip.IT8712F || version >= 8);

        // Disable any fans that aren't set with 16-bit fan counters
        if (_has16BitFanCounter)
        {
            if (chip is Chip.IT8613E or Chip.IT8625E)
            {
                byte cur12 = ReadByte(12, out valid);
                if ((cur12 & 0x3F) != 0x3F)
                {
                    WriteByte(12, (byte)(cur12 | 0x3F));
                }
            }

            int modes = ReadByte(FAN_TACHOMETER_16BIT_REGISTER, out valid);

            if (!valid)
                return;

            if (Fans.Length >= 5)
            {
                _fansDisabled[3] = (modes & (1 << 4)) == 0 && chip != Chip.IT8613E && chip != Chip.IT8625E;
                _fansDisabled[4] = (modes & (1 << 5)) == 0 && chip != Chip.IT8613E && chip != Chip.IT8625E;
            }

            if (Fans.Length >= 6)
            {
                if (chip == Chip.IT8665E)
                {
                    modes = ReadByte(FAN_TACHOMETER_16BIT_REGISTER_ALT, out valid);
                    if (valid)
                        _fansDisabled[5] = (modes & (1 << 3)) == 0;
                }
                else
                    _fansDisabled[5] = (modes & (1 << 2)) == 0 && chip != Chip.IT8613E && chip != Chip.IT8625E;
            }
        }

        // Set the number of GPIO sets
        _gpioCount = chip switch
        {
            Chip.IT8712F or Chip.IT8716F or Chip.IT8718F or Chip.IT8726F => 5,
            Chip.IT8720F or Chip.IT8721F or Chip.IT8772E => 8,
            _ => 0
        };

        if (motherboard.Model == Model.MS_iCraft_Z890_Arctic && chip == Chip.IT8613E)
        {
            try
            {
                _lpcAcpiEc = new LibreHardwareMonitor.PawnIo.LpcAcpiEc();
                _initialEcCurve[3] = new byte[5] { 0x64, 0x8C, 0xA8, 0xBC, 0xC8 };
                _initialEcCurve[4] = new byte[5] { 0x78, 0x96, 0xA8, 0xBC, 0xC8 };
                _initialEcCurve[5] = new byte[5] { 0x64, 0xA6, 0xA6, 0xA6, 0xA6 };

                byte cur60 = ReadEc(0x60);
                if ((cur60 & 7) != 7)
                {
                    WriteEc(0x60, 0x07);
                }

                byte[] baseRegs = new byte[6] { 0, 0, 0, 0x1B, 0x4B, 0x2B };
                for (int idx = 3; idx <= 5; idx++)
                {
                    byte val0 = ReadEc(baseRegs[idx]);
                    byte val1 = ReadEc((byte)(baseRegs[idx] + 1));
                    byte val4 = ReadEc((byte)(baseRegs[idx] + 4));
                    if (val0 == val1 && val1 == val4)
                    {
                        for (byte j = 0; j < 5; j++)
                        {
                            WriteEc((byte)(baseRegs[idx] + j), _initialEcCurve[idx][j]);
                        }
                    }
                }

                for (int s = 0; s < 3; s++)
                {
                    _initialSioTempLimit[s] = ReadByte((byte)(97 + s * 8), out _);
                    _initialSioPwm[s] = ReadByte((byte)(99 + s * 8), out _);
                    _initialSioManualPwm[s] = ReadByte((byte)(103 + s * 8), out _);
                }
            }
            catch
            {
            }
        }
    }

    public Chip Chip { get; }

    public float?[] Controls { get; } = Array.Empty<float?>();

    public float?[] Fans { get; } = Array.Empty<float?>();

    public float?[] Temperatures { get; } = Array.Empty<float?>();

    public float?[] Voltages { get; } = Array.Empty<float?>();

    public byte? ReadGpio(int index)
    {
        if (index >= _gpioCount)
            return null;

        return _port.ReadIoPort((ushort)(_gpioAddress + index));
    }

    public void WriteGpio(int index, byte value)
    {
        if (index >= _gpioCount)
            return;

        _port.WriteIoPort((ushort)(_gpioAddress + index), value);
    }

    public void SetControl(int index, byte? value)
    {
        if (index < 0 || index >= Controls.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (!Mutexes.WaitIsaBus(10))
            return;

        if (Chip == Chip.IT8613E && index >= 3 && _lpcAcpiEc != null)
        {
            byte baseDutyReg = index == 3 ? (byte)0x1B : (index == 4 ? (byte)0x4B : (byte)0x2B);
            byte ecBit = index == 3 ? (byte)0x01 : (index == 4 ? (byte)0x04 : (byte)0x02);
            if (value.HasValue)
            {
                byte cur60 = ReadEc(0x60);
                WriteEc(0x60, (byte)(cur60 & ~ecBit));
                byte ecDuty = (byte)Math.Max(0, Math.Min(200, (int)Math.Round((float)value.Value * 200f / 255f)));
                for (byte i = 0; i < 5; i++)
                {
                    WriteEc((byte)(baseDutyReg + i), ecDuty);
                }
            }
            else
            {
                if (_initialEcCurve[index] != null)
                {
                    for (byte i = 0; i < 5; i++)
                    {
                        WriteEc((byte)(baseDutyReg + i), _initialEcCurve[index][i]);
                    }
                }
                byte cur60 = ReadEc(0x60);
                WriteEc(0x60, (byte)(cur60 | ecBit));
            }

            Mutexes.ReleaseIsaBus();
            return;
        }

        if (Chip == Chip.IT8613E && index < 3)
        {
            byte tempLimitReg = (byte)(97 + index * 8);
            byte pwmDutyReg = (byte)(99 + index * 8);
            byte manualDutyReg = (byte)(103 + index * 8);
            if (value.HasValue)
            {
                SaveDefaultFanPwmControl(index);
                if (!_initialFanOutputModeEnabled[index])
                {
                    WriteByte(FAN_MAIN_CTRL_REG, (byte)(ReadByte(FAN_MAIN_CTRL_REG, out _) | (1 << index)));
                }

                byte duty7Bit = (byte)(value.Value >> 1);
                WriteByte(FAN_PWM_CTRL_REG[index], duty7Bit);
                WriteByte(tempLimitReg, 16);
                WriteByte(pwmDutyReg, value.Value);
                WriteByte(manualDutyReg, value.Value);
            }
            else
            {
                RestoreDefaultFanPwmControl(index);
                if (_initialSioTempLimit[index] > 0)
                {
                    WriteByte(tempLimitReg, _initialSioTempLimit[index]);
                    WriteByte(pwmDutyReg, _initialSioPwm[index]);
                    WriteByte(manualDutyReg, _initialSioManualPwm[index]);
                }
            }

            Mutexes.ReleaseIsaBus();
            return;
        }

        if (value.HasValue)
        {
            SaveDefaultFanPwmControl(index);

            // Disable the controller when setting values to prevent it from overriding them
            if (_gigabyteController != null)
                _gigabyteController.Enable(false);

            if (index < 3 && !_initialFanOutputModeEnabled[index])
                WriteByte(FAN_MAIN_CTRL_REG, (byte)(ReadByte(FAN_MAIN_CTRL_REG, out _) | (1 << index)));

            if (_hasExtReg)
            {
                if (Chip == Chip.IT8689E)
                {
                    WriteByte(FAN_PWM_CTRL_REG[index], 0x7F);
                }
                else
                {
                    WriteByte(FAN_PWM_CTRL_REG[index], (byte)(_initialFanPwmControl[index] & 0x7F));
                }
                WriteByte(FAN_PWM_CTRL_EXT_REG[index], value.Value);
            }
            else
            {
                WriteByte(FAN_PWM_CTRL_REG[index], (byte)(value.Value >> 1));
            }
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
        r.Append("Chip ID: 0x");
        r.AppendLine(Chip.ToString("X"));
        r.Append("Chip Version: 0x");
        r.AppendLine(_version.ToString("X", CultureInfo.InvariantCulture));
        r.Append("Base Address: 0x");
        r.AppendLine(_address.ToString("X4", CultureInfo.InvariantCulture));
        r.Append("GPIO Address: 0x");
        r.AppendLine(_gpioAddress.ToString("X4", CultureInfo.InvariantCulture));
        r.AppendLine();

        if (!Mutexes.WaitIsaBus(100))
            return r.ToString();

        if (_requiresBankSelect)
            SelectBank(0);

        // dump memory of all banks if supported by chip
        for (byte b = 0; b < _bankCount; b++)
        {
            if (SupportsMultipleBanks && b > 0)
            {
                SelectBank(b);
            }
            r.AppendLine($"Environment Controller Registers Bank {b}");
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
                    r.Append(valid ? value.ToString("X2", CultureInfo.InvariantCulture) : "??");
                }

                r.AppendLine();
            }

            r.AppendLine();
        }

        if (SupportsMultipleBanks)
        {
            SelectBank(0);
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
        Mutexes.ReleaseIsaBus();
        return r.ToString();
    }

    /// <summary>
    /// Selects another bank. Memory from 0x10-0xAF swaps to data from new bank.
    /// Beware to select the default bank 0 after changing.
    /// Bank selection is reset after power cycle.
    /// </summary>
    /// <param name="bankIndex">New bank index. Can be a value of 0-3.</param>
    private void SelectBank(byte bankIndex)
    {
        if (bankIndex >= _bankCount)
            return; // current chip does not support that many banks

        // hard cap SelectBank to 2 bit values. If we ever have chips with more bank bits rewrite this method.
        bankIndex &= 0x3;

        byte value = ReadByte(BANK_REGISTER, out bool valid);
        if (valid)
        {
            value &= 0x9F;
            value |= (byte)(bankIndex << 5);
            WriteByte(BANK_REGISTER, value);
        }
    }

    public void Update()
    {
        if (!Mutexes.WaitIsaBus(10))
            return;

        // Is this needed on every update?  Yes, until a way to detect resume from sleep/hibernation is added, as that invalidates the bank select.
        if (_requiresBankSelect)
            SelectBank(0);

        for (int i = 0; i < Voltages.Length; i++)
        {
            float value = _voltageGain * ReadByte(IT87_REG_VIN[i], out bool valid);

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

            if (value is < sbyte.MaxValue and > 0)
                Temperatures[i] = value;
            else
                Temperatures[i] = null;
        }

        if (_has16BitFanCounter)
        {
            for (int i = 0; i < Fans.Length; i++)
            {
                if (Chip == Chip.IT8613E && i >= 3 && _lpcAcpiEc != null)
                {
                    byte ecMsbReg = i == 3 ? (byte)0x10 : (i == 4 ? (byte)0x40 : (byte)0x20);
                    byte ecLsbReg = i == 3 ? (byte)0x11 : (i == 4 ? (byte)0x41 : (byte)0x21);
                    int ecRpm = (ReadEc(ecMsbReg) << 8) | ReadEc(ecLsbReg);
                    Fans[i] = ecRpm is > 0 and < 20000 ? (float)ecRpm : 0f;
                    continue;
                }

                if (_fansDisabled[i])
                    continue;

                int value = ReadByte(_hasAlt6thFanReg ? FAN_TACHOMETER_REG_ALT[i] : FAN_TACHOMETER_REG[i], out bool valid);
                if (!valid)
                    continue;

                value |= ReadByte(_hasAlt6thFanReg ? FAN_TACHOMETER_EXT_REG_ALT[i] : FAN_TACHOMETER_EXT_REG[i], out valid) << 8;
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
            if (Chip == Chip.IT8613E && i >= 3 && _lpcAcpiEc != null)
            {
                byte ecDutyReg = i == 3 ? (byte)0x12 : (i == 4 ? (byte)0x42 : (byte)0x22);
                byte rawDuty = ReadEc(ecDutyReg);
                Controls[i] = (float)Math.Round((float)rawDuty * 100f / 200f);
                continue;
            }

            byte value = ReadByte(FAN_PWM_CTRL_REG[i], out bool valid);
            if (!valid)
                continue;

            if ((value & 0x80) > 0)
            {
                // Automatic operation (value can't be read).
                Controls[i] = null;
            }
            else
            {
                // Software operation.
                if (_hasExtReg)
                {
                    value = ReadByte(FAN_PWM_CTRL_EXT_REG[i], out valid);
                    if (valid)
                        Controls[i] = (float)Math.Round(value * 100.0f / 0xFF);
                }
                else
                {
                    Controls[i] = (float)Math.Round((value & 0x7F) * 100.0f / 0x7F);
                }
            }
        }

        Mutexes.ReleaseIsaBus();
    }

    public void Close()
    {
        if (Chip == Chip.IT8613E)
        {
            for (int idx = 0; idx < Controls.Length; idx++)
            {
                try
                {
                    SetControl(idx, null);
                }
                catch
                {
                }
            }
        }

        _lpcAcpiEc?.Close();
        _gigabyteController?.Dispose();
        _port.Close();
    }

    private byte ReadEc(byte reg)
    {
        if (_lpcAcpiEc == null)
            return 0;

        try
        {
            _lpcAcpiEc.WritePort(0x66, 0x80);
            int timeout = 5000;
            while (--timeout > 0 && (_lpcAcpiEc.ReadPort(0x66) & 2) != 0) { }
            _lpcAcpiEc.WritePort(0x62, reg);
            timeout = 5000;
            while (--timeout > 0 && (_lpcAcpiEc.ReadPort(0x66) & 1) == 0) { }
            return _lpcAcpiEc.ReadPort(0x62);
        }
        catch
        {
            return 0;
        }
    }

    private void WriteEc(byte reg, byte val)
    {
        if (_lpcAcpiEc == null)
            return;

        try
        {
            int timeout = 5000;
            while (--timeout > 0 && (_lpcAcpiEc.ReadPort(0x66) & 2) != 0) { }
            _lpcAcpiEc.WritePort(0x66, 0x81);
            timeout = 5000;
            while (--timeout > 0 && (_lpcAcpiEc.ReadPort(0x66) & 2) != 0) { }
            _lpcAcpiEc.WritePort(0x62, reg);
            timeout = 5000;
            while (--timeout > 0 && (_lpcAcpiEc.ReadPort(0x66) & 2) != 0) { }
            _lpcAcpiEc.WritePort(0x62, val);
        }
        catch
        {
        }
    }

    private byte ReadByte(byte register, out bool valid)
    {
        _port.WriteIoPort(_addressReg, register);
        byte value = _port.ReadIoPort(_dataReg);
        valid = register == _port.ReadIoPort(_addressReg) || Chip == Chip.IT8688E;
        // IT8688E doesn't return the value we wrote to
        // addressReg when we read it back.

        return value;
    }

    private void WriteByte(byte register, byte value)
    {
        _port.WriteIoPort(_addressReg, register);
        _port.WriteIoPort(_dataReg, value);
        _port.ReadIoPort(_addressReg);
    }

    private void SaveDefaultFanPwmControl(int index)
    {
        if (!_restoreDefaultFanPwmControlRequired[index])
        {
            _initialFanPwmControl[index] = ReadByte(FAN_PWM_CTRL_REG[index], out bool _);

            if (index < 3)
                _initialFanOutputModeEnabled[index] = (ReadByte(FAN_MAIN_CTRL_REG, out bool _) & (1 << index)) != 0; // Save default control bit value.

            if (_hasExtReg)
                _initialFanPwmControlExt[index] = ReadByte(FAN_PWM_CTRL_EXT_REG[index], out _);
        }

        _restoreDefaultFanPwmControlRequired[index] = true;
    }

    private void RestoreDefaultFanPwmControl(int index)
    {
        if (_restoreDefaultFanPwmControlRequired[index])
        {
            WriteByte(FAN_PWM_CTRL_REG[index], _initialFanPwmControl[index]);

            if (index < 3)
            {
                byte value = ReadByte(FAN_MAIN_CTRL_REG, out _);

                bool isEnabled = (value & (1 << index)) != 0;
                if (isEnabled != _initialFanOutputModeEnabled[index])
                    WriteByte(FAN_MAIN_CTRL_REG, (byte)(value ^ (1 << index)));
            }

            if (_hasExtReg)
                WriteByte(FAN_PWM_CTRL_EXT_REG[index], _initialFanPwmControlExt[index]);

            _restoreDefaultFanPwmControlRequired[index] = false;

            // restore the GB controller when all fans become restored
            if (_gigabyteController != null && _restoreDefaultFanPwmControlRequired.All(e => e == false))
                _gigabyteController.Restore();
        }
    }

    // ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles

    private const byte ADDRESS_REGISTER_OFFSET = 0x05;

    private const byte CONFIGURATION_REGISTER = 0x00;
    private const byte DATA_REGISTER_OFFSET = 0x06;
    private const byte BANK_REGISTER = 0x06; // bit 5-6 define selected bank
    private const byte FAN_TACHOMETER_16BIT_REGISTER = 0x0C;
    private const byte FAN_TACHOMETER_16BIT_REGISTER_ALT = 0x0B;
    private const byte FAN_TACHOMETER_DIVISOR_REGISTER = 0x0B;

    private readonly byte[] ITE_VENDOR_IDS = { 0x90, 0x7F };

    private const byte TEMPERATURE_BASE_REG = 0x29;
    private const byte VENDOR_ID_REGISTER = 0x58;

    // https://github.com/torvalds/linux/blob/master/drivers/hwmon/it87.c
    private readonly byte[] IT87_REG_VIN = { 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x2f, 0x2c, 0x2d, 0x2e };

    private readonly byte[] FAN_PWM_CTRL_REG;
    private readonly byte[] FAN_PWM_CTRL_EXT_REG = { 0x63, 0x6b, 0x73, 0x7b, 0xa3, 0xab };
    private readonly byte[] FAN_TACHOMETER_EXT_REG = { 0x18, 0x19, 0x1a, 0x81, 0x83, 0x4d };
    private readonly byte[] FAN_TACHOMETER_REG = { 0x0d, 0x0e, 0x0f, 0x80, 0x82, 0x4c };
    private readonly byte[] FAN_TACHOMETER_EXT_REG_ALT = { 0x18, 0x19, 0x1a, 0x81, 0x83, 0x94 };
    private readonly byte[] FAN_TACHOMETER_REG_ALT = { 0x0d, 0x0e, 0x0f, 0x80, 0x82, 0x93 };

    // Address of the Fan Controller Main Control Register.
    // No need for the 2nd control register (bit 7 of 0x15 0x16 0x17),
    // as PWM value will set it to manual mode when new value is set.
    private const byte FAN_MAIN_CTRL_REG = 0x13;

#pragma warning restore IDE1006 // Naming Styles
    // ReSharper restore InconsistentNaming
}
