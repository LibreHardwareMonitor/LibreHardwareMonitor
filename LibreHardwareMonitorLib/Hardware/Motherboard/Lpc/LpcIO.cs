﻿// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc
{
    internal class LpcIO
    {
        private readonly StringBuilder _report = new StringBuilder();
        private readonly List<ISuperIO> _superIOs = new List<ISuperIO>();

        public LpcIO()
        {
            if (!Ring0.IsOpen)
                return;

            if (!Ring0.WaitIsaBusMutex(100))
                return;


            Detect();

            Ring0.ReleaseIsaBusMutex();
        }

        public ISuperIO[] SuperIO => _superIOs.ToArray();

        private void ReportUnknownChip(LpcPort port, string type, int chip)
        {
            _report.Append("Chip ID: Unknown ");
            _report.Append(type);
            _report.Append(" with ID 0x");
            _report.Append(chip.ToString("X", CultureInfo.InvariantCulture));
            _report.Append(" at 0x");
            _report.Append(port.RegisterPort.ToString("X", CultureInfo.InvariantCulture));
            _report.Append("/0x");
            _report.AppendLine(port.ValuePort.ToString("X", CultureInfo.InvariantCulture));
            _report.AppendLine();
        }

        private bool DetectSmsc(LpcPort port)
        {
            port.SmscEnter();

            ushort chipId = port.ReadWord(CHIP_ID_REGISTER);
            Chip chip;
            switch (chipId)
            {
                default:
                    chip = Chip.Unknown;
                    break;
            }

            if (chip == Chip.Unknown)
            {
                if (chipId != 0 && chipId != 0xffff)
                {
                    port.SmscExit();
                    ReportUnknownChip(port, "SMSC", chipId);
                }
            }
            else
            {
                port.SmscExit();
                return true;
            }

            return false;
        }

        private void Detect()
        {
            for (int i = 0; i < REGISTER_PORTS.Length; i++)
            {
                var port = new LpcPort(REGISTER_PORTS[i], VALUE_PORTS[i]);

                if (DetectWinbondFintek(port)) continue;

                if (DetectIT87(port)) continue;

                if (DetectSmsc(port)) continue;
            }
        }

        public string GetReport()
        {
            if (_report.Length > 0)
            {
                return "LpcIO" + Environment.NewLine + Environment.NewLine + _report;
            }

            return null;
        }

        private bool DetectWinbondFintek(LpcPort port)
        {
            port.WinbondNuvotonFintekEnter();

            byte logicalDeviceNumber = 0;
            byte id = port.ReadByte(CHIP_ID_REGISTER);
            byte revision = port.ReadByte(CHIP_REVISION_REGISTER);
            Chip chip = Chip.Unknown;

            switch (id)
            {
                case 0x05:
                {
                    switch (revision)
                    {
                        case 0x07:
                        {
                            chip = Chip.F71858;
                            logicalDeviceNumber = F71858_HARDWARE_MONITOR_LDN;
                            break;
                        }
                        case 0x41:
                        {
                            chip = Chip.F71882;
                            logicalDeviceNumber = FINTEK_HARDWARE_MONITOR_LDN;
                            break;
                        }
                    }

                    break;
                }
                case 0x06:
                {
                    switch (revision)
                    {
                        case 0x01:
                        {
                            chip = Chip.F71862;
                            logicalDeviceNumber = FINTEK_HARDWARE_MONITOR_LDN;
                            break;
                        }
                    }

                    break;
                }
                case 0x07:
                {
                    switch (revision)
                    {
                        case 0x23:
                        {
                            chip = Chip.F71889F;
                            logicalDeviceNumber = FINTEK_HARDWARE_MONITOR_LDN;
                            break;
                        }
                    }

                    break;
                }
                case 0x08:
                {
                    switch (revision)
                    {
                        case 0x14:
                        {
                            chip = Chip.F71869;
                            logicalDeviceNumber = FINTEK_HARDWARE_MONITOR_LDN;
                            break;
                        }
                    }

                    break;
                }
                case 0x09:
                {
                    switch (revision)
                    {
                        case 0x01:
                        {
                            chip = Chip.F71808E;
                            logicalDeviceNumber = FINTEK_HARDWARE_MONITOR_LDN;
                            break;
                        }
                        case 0x09:
                        {
                            chip = Chip.F71889ED;
                            logicalDeviceNumber = FINTEK_HARDWARE_MONITOR_LDN;
                            break;
                        }
                    }

                    break;
                }
                case 0x10:
                {
                    switch (revision)
                    {
                        case 0x05:
                        {
                            chip = Chip.F71889AD;
                            logicalDeviceNumber = FINTEK_HARDWARE_MONITOR_LDN;
                            break;
                        }
                        case 0x07:
                        {
                            chip = Chip.F71869A;
                            logicalDeviceNumber = FINTEK_HARDWARE_MONITOR_LDN;
                            break;
                        }
                    }

                    break;
                }
                case 0x11:
                {
                    switch (revision)
                    {
                        case 0x06:
                        {
                            chip = Chip.F71878AD;
                            logicalDeviceNumber = FINTEK_HARDWARE_MONITOR_LDN;
                            break;
                        }
                        case 0x18:
                        {
                            chip = Chip.F71811;
                            logicalDeviceNumber = FINTEK_HARDWARE_MONITOR_LDN;
                            break;
                        }
                    }

                    break;
                }
                case 0x52:
                {
                    switch (revision)
                    {
                        case 0x17:
                        case 0x3A:
                        case 0x41:
                        {
                            chip = Chip.W83627HF;
                            logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
                            break;
                        }
                    }

                    break;
                }
                case 0x82:
                {
                    switch (revision & 0xF0)
                    {
                        case 0x80:
                        {
                            chip = Chip.W83627THF;
                            logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
                            break;
                        }
                    }

                    break;
                }
                case 0x85:
                {
                    switch (revision)
                    {
                        case 0x41:
                        {
                            chip = Chip.W83687THF;
                            logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
                            break;
                        }
                    }

                    break;
                }
                case 0x88:
                {
                    switch (revision & 0xF0)
                    {
                        case 0x50:
                        case 0x60:
                        {
                            chip = Chip.W83627EHF;
                            logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
                            break;
                        }
                    }

                    break;
                }
                case 0xA0:
                {
                    switch (revision & 0xF0)
                    {
                        case 0x20:
                        {
                            chip = Chip.W83627DHG;
                            logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
                            break;
                        }
                    }

                    break;
                }
                case 0xA5:
                {
                    switch (revision & 0xF0)
                    {
                        case 0x10:
                        {
                            chip = Chip.W83667HG;
                            logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
                            break;
                        }
                    }

                    break;
                }
                case 0xB0:
                {
                    switch (revision & 0xF0)
                    {
                        case 0x70:
                        {
                            chip = Chip.W83627DHGP;
                            logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
                            break;
                        }
                    }

                    break;
                }
                case 0xB3:
                {
                    switch (revision & 0xF0)
                    {
                        case 0x50:
                        {
                            chip = Chip.W83667HGB;
                            logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
                            break;
                        }
                    }

                    break;
                }
                case 0xB4:
                {
                    switch (revision & 0xF0)
                    {
                        case 0x70:
                        {
                            chip = Chip.NCT6771F;
                            logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
                            break;
                        }
                    }

                    break;
                }
                case 0xC3:
                {
                    switch (revision & 0xF0)
                    {
                        case 0x30:
                        {
                            chip = Chip.NCT6776F;
                            logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
                            break;
                        }
                    }

                    break;
                }
                case 0xC4:
                {
                    switch (revision & 0xF0)
                    {
                        case 0x50:
                        {
                            chip = Chip.NCT610XD;
                            logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
                            break;
                        }
                    }

                    break;
                }
                case 0xC5:
                {
                    switch (revision & 0xF0)
                    {
                        case 0x60:
                        {
                            chip = Chip.NCT6779D;
                            logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
                            break;
                        }
                    }

                    break;
                }
                case 0xC8:
                {
                    switch (revision)
                    {
                        case 0x03:
                        {
                            chip = Chip.NCT6791D;
                            logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
                            break;
                        }
                    }

                    break;
                }
                case 0xC9:
                {
                    switch (revision)
                    {
                        case 0x11:
                        {
                            chip = Chip.NCT6792D;
                            logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
                            break;
                        }
                        case 0x13:
                        {
                            chip = Chip.NCT6792DA;
                            logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
                            break;
                        }
                    }

                    break;
                }
                case 0xD1:
                {
                    switch (revision)
                    {
                        case 0x21:
                        {
                            chip = Chip.NCT6793D;
                            logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
                            break;
                        }
                    }

                    break;
                }
                case 0xD3:
                {
                    switch (revision)
                    {
                        case 0x52:
                        {
                            chip = Chip.NCT6795D;
                            logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
                            break;
                        }
                    }

                    break;
                }
                case 0xD4:
                {
                    switch (revision)
                    {
                        case 0x23:
                        {
                            chip = Chip.NCT6796D;
                            logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
                            break;
                        }
                        case 0x2A:
                        {
                            chip = Chip.NCT6796DR;
                            logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
                            break;
                        }
                        case 0x51:
                        {
                            chip = Chip.NCT6797D;
                            logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
                            break;
                        }
                        case 0x2B:
                        {
                            chip = Chip.NCT6798D;
                            logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
                            break;
                        }
                    }

                    break;
                }
                case 0xD5:
                {
                    switch (revision)
                    {
                        case 0x92:
                        {
                            chip = Chip.NCT6687D;
                            logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
                            break;
                        }
                    }

                    break;
                }
            }

            if (chip == Chip.Unknown)
            {
                if (id != 0 && id != 0xff)
                {
                    port.WinbondNuvotonFintekExit();
                    ReportUnknownChip(port, "Winbond / Nuvoton / Fintek", (id << 8) | revision);
                }
            }
            else
            {
                port.Select(logicalDeviceNumber);
                ushort address = port.ReadWord(BASE_ADDRESS_REGISTER);
                Thread.Sleep(1);
                ushort verify = port.ReadWord(BASE_ADDRESS_REGISTER);

                ushort vendorId = port.ReadWord(FINTEK_VENDOR_ID_REGISTER);

                // disable the hardware monitor i/o space lock on NCT679XD chips
                if (address == verify && (chip == Chip.NCT6791D ||
                                          chip == Chip.NCT6792D ||
                                          chip == Chip.NCT6792DA ||
                                          chip == Chip.NCT6793D ||
                                          chip == Chip.NCT6795D ||
                                          chip == Chip.NCT6796D || 
                                          chip == Chip.NCT6796DR || 
                                          chip == Chip.NCT6798D || 
                                          chip == Chip.NCT6797D))
                {
                    port.NuvotonDisableIOSpaceLock();
                }

                port.WinbondNuvotonFintekExit();

                if (address != verify)
                {
                    _report.Append("Chip ID: 0x");
                    _report.AppendLine(chip.ToString("X"));
                    _report.Append("Chip revision: 0x");
                    _report.AppendLine(revision.ToString("X", CultureInfo.InvariantCulture));
                    _report.AppendLine("Error: Address verification failed");
                    _report.AppendLine();

                    return false;
                }

                // some Fintek chips have address register offset 0x05 added already
                if ((address & 0x07) == 0x05)
                    address &= 0xFFF8;

                if (address < 0x100 || (address & 0xF007) != 0)
                {
                    _report.Append("Chip ID: 0x");
                    _report.AppendLine(chip.ToString("X"));
                    _report.Append("Chip revision: 0x");
                    _report.AppendLine(revision.ToString("X", CultureInfo.InvariantCulture));
                    _report.Append("Error: Invalid address 0x");
                    _report.AppendLine(address.ToString("X", CultureInfo.InvariantCulture));
                    _report.AppendLine();

                    return false;
                }

                switch (chip)
                {
                    case Chip.W83627DHG:
                    case Chip.W83627DHGP:
                    case Chip.W83627EHF:
                    case Chip.W83627HF:
                    case Chip.W83627THF:
                    case Chip.W83667HG:
                    case Chip.W83667HGB:
                    case Chip.W83687THF:
                    {
                        _superIOs.Add(new W836XX(chip, revision, address));
                        break;
                    }
                    case Chip.NCT610XD:
                    case Chip.NCT6771F:
                    case Chip.NCT6776F:
                    case Chip.NCT6779D:
                    case Chip.NCT6791D:
                    case Chip.NCT6792D:
                    case Chip.NCT6792DA:
                    case Chip.NCT6793D:
                    case Chip.NCT6795D:
                    case Chip.NCT6796D:
                    case Chip.NCT6796DR:
                    case Chip.NCT6797D:
                    case Chip.NCT6798D:
                    case Chip.NCT6687D:
                    {
                        _superIOs.Add(new Nct677X(chip, revision, address, port));
                        break;
                    }
                    case Chip.F71858:
                    case Chip.F71862:
                    case Chip.F71869:
                    case Chip.F71878AD:
                    case Chip.F71869A:
                    case Chip.F71882:
                    case Chip.F71889AD:
                    case Chip.F71889ED:
                    case Chip.F71889F:
                    case Chip.F71808E:
                    {
                        if (vendorId != FINTEK_VENDOR_ID)
                        {
                            _report.Append("Chip ID: 0x");
                            _report.AppendLine(chip.ToString("X"));
                            _report.Append("Chip revision: 0x");
                            _report.AppendLine(revision.ToString("X", CultureInfo.InvariantCulture));
                            _report.Append("Error: Invalid vendor ID 0x");
                            _report.AppendLine(vendorId.ToString("X", CultureInfo.InvariantCulture));
                            _report.AppendLine();

                            return false;
                        }

                        _superIOs.Add(new F718XX(chip, address));
                        break;
                    }
                }

                return true;
            }

            return false;
        }

        private bool DetectIT87(LpcPort port)
        {
            // IT87XX can enter only on port 0x2E
            // IT8792 using 0x4E
            if (port.RegisterPort != 0x2E && port.RegisterPort != 0x4E)
                return false;


            port.IT87Enter();

            ushort chipId = port.ReadWord(CHIP_ID_REGISTER);
            Chip chip;
            switch (chipId)
            {
                case 0x8620:
                    chip = Chip.IT8620E;
                    break;
                case 0x8628:
                    chip = Chip.IT8628E;
                    break;
                case 0x8665:
                    chip = Chip.IT8665E;
                    break;
                case 0x8655:
                    chip = Chip.IT8655E;
                    break;
                case 0x8686:
                    chip = Chip.IT8686E;
                    break;
                case 0x8688:
                    chip = Chip.IT8688E;
                    break;
                case 0x8705:
                    chip = Chip.IT8705F;
                    break;
                case 0x8712:
                    chip = Chip.IT8712F;
                    break;
                case 0x8716:
                    chip = Chip.IT8716F;
                    break;
                case 0x8718:
                    chip = Chip.IT8718F;
                    break;
                case 0x8720:
                    chip = Chip.IT8720F;
                    break;
                case 0x8721:
                    chip = Chip.IT8721F;
                    break;
                case 0x8726:
                    chip = Chip.IT8726F;
                    break;
                case 0x8728:
                    chip = Chip.IT8728F;
                    break;
                case 0x8771:
                    chip = Chip.IT8771E;
                    break;
                case 0x8772:
                    chip = Chip.IT8772E;
                    break;
                case 0x8733:
                    chip = Chip.IT879XE;
                    break;
                default:
                    chip = Chip.Unknown;
                    break;
            }

            if (chip == Chip.Unknown)
            {
                if (chipId != 0 && chipId != 0xffff)
                {
                    port.IT87Exit();

                    ReportUnknownChip(port, "ITE", chipId);
                }
            }
            else
            {
                port.Select(IT87_ENVIRONMENT_CONTROLLER_LDN);               

                ushort address = port.ReadWord(BASE_ADDRESS_REGISTER);
                Thread.Sleep(1);
                ushort verify = port.ReadWord(BASE_ADDRESS_REGISTER);

                byte version = (byte)(port.ReadByte(IT87_CHIP_VERSION_REGISTER) & 0x0F);

                ushort gpioAddress;
                ushort gpioVerify;

                if (chip == Chip.IT8705F)
                {
                    port.Select(IT8705_GPIO_LDN);
                    gpioAddress = port.ReadWord(BASE_ADDRESS_REGISTER);
                    Thread.Sleep(1);
                    gpioVerify = port.ReadWord(BASE_ADDRESS_REGISTER);
                }
                else
                {
                    port.Select(IT87XX_GPIO_LDN);
                    gpioAddress = port.ReadWord(BASE_ADDRESS_REGISTER + 2);
                    Thread.Sleep(1);
                    gpioVerify = port.ReadWord(BASE_ADDRESS_REGISTER + 2);
                }

                port.IT87Exit();

                if (address != verify || address < 0x100 || (address & 0xF007) != 0)
                {
                    _report.Append("Chip ID: 0x");
                    _report.AppendLine(chip.ToString("X"));
                    _report.Append("Error: Invalid address 0x");
                    _report.AppendLine(address.ToString("X", CultureInfo.InvariantCulture));
                    _report.AppendLine();

                    return false;
                }

                if (gpioAddress != gpioVerify || gpioAddress < 0x100 || (gpioAddress & 0xF007) != 0)
                {
                    _report.Append("Chip ID: 0x");
                    _report.AppendLine(chip.ToString("X"));
                    _report.Append("Error: Invalid GPIO address 0x");
                    _report.AppendLine(gpioAddress.ToString("X", CultureInfo.InvariantCulture));
                    _report.AppendLine();

                    return false;
                }

                _superIOs.Add(new IT87XX(chip, address, gpioAddress, version));
                return true;
            }

            return false;
        }

        // ReSharper disable InconsistentNaming
        private const byte BASE_ADDRESS_REGISTER = 0x60;
        private const byte CHIP_ID_REGISTER = 0x20;
        private const byte CHIP_REVISION_REGISTER = 0x21;

        private const byte F71858_HARDWARE_MONITOR_LDN = 0x02;
        private const byte FINTEK_HARDWARE_MONITOR_LDN = 0x04;
        private const byte IT87_ENVIRONMENT_CONTROLLER_LDN = 0x04;
        private const byte IT8705_GPIO_LDN = 0x05;
        private const byte IT87XX_GPIO_LDN = 0x07;
        private const byte WINBOND_NUVOTON_HARDWARE_MONITOR_LDN = 0x0B;

        private const ushort FINTEK_VENDOR_ID = 0x1934;

        private const byte FINTEK_VENDOR_ID_REGISTER = 0x23;
        private const byte IT87_CHIP_VERSION_REGISTER = 0x22;

        private readonly ushort[] REGISTER_PORTS = { 0x2E, 0x4E };
        private readonly ushort[] VALUE_PORTS = { 0x2F, 0x4F };
        // ReSharper restore InconsistentNaming
    }
}
