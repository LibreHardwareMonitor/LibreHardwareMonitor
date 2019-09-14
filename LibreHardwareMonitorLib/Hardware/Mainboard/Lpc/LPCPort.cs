// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

namespace LibreHardwareMonitor.Hardware.Lpc
{
    internal class LPCPort
    {
        private readonly ushort _registerPort;
        private readonly ushort _valuePort;

        private const byte DEVCIE_SELECT_REGISTER = 0x07;
        private const byte CONFIGURATION_CONTROL_REGISTER = 0x02;

        public LPCPort(ushort registerPort, ushort valuePort)
        {
            _registerPort = registerPort;
            _valuePort = valuePort;
        }

        public ushort RegisterPort
        {
            get
            {
                return _registerPort;
            }
        }

        public ushort ValuePort
        {
            get
            {
                return _valuePort;
            }
        }

        public byte ReadByte(byte register)
        {
            Ring0.WriteIoPort(_registerPort, register);
            return Ring0.ReadIoPort(_valuePort);
        }

        public void WriteByte(byte register, byte value)
        {
            Ring0.WriteIoPort(_registerPort, register);
            Ring0.WriteIoPort(_valuePort, value);
        }

        public ushort ReadWord(byte register)
        {
            return (ushort)((ReadByte(register) << 8) |
              ReadByte((byte)(register + 1)));
        }

        public void Select(byte logicalDeviceNumber)
        {
            Ring0.WriteIoPort(_registerPort, DEVCIE_SELECT_REGISTER);
            Ring0.WriteIoPort(_valuePort, logicalDeviceNumber);
        }

        public void WinbondNuvotonFintekEnter()
        {
            Ring0.WriteIoPort(_registerPort, 0x87);
            Ring0.WriteIoPort(_registerPort, 0x87);
        }

        public void WinbondNuvotonFintekExit()
        {
            Ring0.WriteIoPort(_registerPort, 0xAA);
        }

        private const byte NUVOTON_HARDWARE_MONITOR_IO_SPACE_LOCK = 0x28;

        public void NuvotonDisableIOSpaceLock()
        {
            byte options = ReadByte(NUVOTON_HARDWARE_MONITOR_IO_SPACE_LOCK);
            // if the i/o space lock is enabled
            if ((options & 0x10) > 0)
            {

                // disable the i/o space lock
                WriteByte(NUVOTON_HARDWARE_MONITOR_IO_SPACE_LOCK, (byte)(options & ~0x10));
            }
        }

        public void IT87Enter()
        {
            Ring0.WriteIoPort(_registerPort, 0x87);
            Ring0.WriteIoPort(_registerPort, 0x01);
            Ring0.WriteIoPort(_registerPort, 0x55);
            Ring0.WriteIoPort(_registerPort, 0x55);
        }

        public void IT87Exit()
        {
            Ring0.WriteIoPort(_registerPort, CONFIGURATION_CONTROL_REGISTER);
            Ring0.WriteIoPort(_valuePort, 0x02);
        }

        public void SMSCEnter()
        {
            Ring0.WriteIoPort(_registerPort, 0x55);
        }

        public void SMSCExit()
        {
            Ring0.WriteIoPort(_registerPort, 0xAA);
        }
    }

}
