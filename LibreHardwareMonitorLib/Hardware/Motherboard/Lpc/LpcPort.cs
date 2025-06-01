// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using LibreHardwareMonitor.WinRing0;

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc;

internal class LpcPort
{
    public LpcPort(ushort registerPort, ushort valuePort)
    {
        RegisterPort = registerPort;
        ValuePort = valuePort;
    }

    public ushort RegisterPort { get; }

    public ushort ValuePort { get; }

    public byte ReadByte(byte register)
    {
        DriverAccess.WriteIoPort(RegisterPort, register);
        return DriverAccess.ReadIoPort(ValuePort);
    }

    public void WriteByte(byte register, byte value)
    {
        DriverAccess.WriteIoPort(RegisterPort, register);
        DriverAccess.WriteIoPort(ValuePort, value);
    }

    public ushort ReadWord(byte register)
    {
        return (ushort)((ReadByte(register) << 8) | ReadByte((byte)(register + 1)));
    }

    public bool TryReadWord(byte register, out ushort value)
    {
        value = ReadWord(register);
        return value != 0xFFFF;
    }

    public void Select(byte logicalDeviceNumber)
    {
        DriverAccess.WriteIoPort(RegisterPort, DEVICE_SELECT_REGISTER);
        DriverAccess.WriteIoPort(ValuePort, logicalDeviceNumber);
    }

    public void WinbondNuvotonFintekEnter()
    {
        DriverAccess.WriteIoPort(RegisterPort, 0x87);
        DriverAccess.WriteIoPort(RegisterPort, 0x87);
    }

    public void WinbondNuvotonFintekExit()
    {
        DriverAccess.WriteIoPort(RegisterPort, 0xAA);
    }

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
        DriverAccess.WriteIoPort(RegisterPort, 0x87);
        DriverAccess.WriteIoPort(RegisterPort, 0x01);
        DriverAccess.WriteIoPort(RegisterPort, 0x55);
        DriverAccess.WriteIoPort(RegisterPort, RegisterPort == 0x4E ? (byte)0xAA : (byte)0x55);
    }

    public void IT87Exit()
    {
        // Do not exit config mode for secondary super IO.
        if (RegisterPort != 0x4E)
        {
            DriverAccess.WriteIoPort(RegisterPort, CONFIGURATION_CONTROL_REGISTER);
            DriverAccess.WriteIoPort(ValuePort, 0x02);
        }
    }

    public void SmscEnter()
    {
        DriverAccess.WriteIoPort(RegisterPort, 0x55);
    }

    public void SmscExit()
    {
        DriverAccess.WriteIoPort(RegisterPort, 0xAA);
    }

    // ReSharper disable InconsistentNaming
    private const byte CONFIGURATION_CONTROL_REGISTER = 0x02;
    private const byte DEVICE_SELECT_REGISTER = 0x07;
    private const byte NUVOTON_HARDWARE_MONITOR_IO_SPACE_LOCK = 0x28;
    // ReSharper restore InconsistentNaming
}
