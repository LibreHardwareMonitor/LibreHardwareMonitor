// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

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
        Ring0.WriteIoPort(RegisterPort, register);
        return Ring0.ReadIoPort(ValuePort);
    }

    public void WriteByte(byte register, byte value)
    {
        Ring0.WriteIoPort(RegisterPort, register);
        Ring0.WriteIoPort(ValuePort, value);
    }

    public ushort ReadWord(byte register)
    {
        return (ushort)((ReadByte(register) << 8) | ReadByte((byte)(register + 1)));
    }

    public void Select(byte logicalDeviceNumber)
    {
        Ring0.WriteIoPort(RegisterPort, DEVICE_SELECT_REGISTER);
        Ring0.WriteIoPort(ValuePort, logicalDeviceNumber);
    }

    public void WinbondNuvotonFintekEnter()
    {
        Ring0.WriteIoPort(RegisterPort, 0x87);
        Ring0.WriteIoPort(RegisterPort, 0x87);
    }

    public void WinbondNuvotonFintekExit()
    {
        Ring0.WriteIoPort(RegisterPort, 0xAA);
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
        Ring0.WriteIoPort(RegisterPort, 0x87);
        Ring0.WriteIoPort(RegisterPort, 0x01);
        Ring0.WriteIoPort(RegisterPort, 0x55);
        Ring0.WriteIoPort(RegisterPort, RegisterPort == 0x4E ? (byte)0xAA : (byte)0x55);
    }

    public void IT87Exit()
    {
        // Do not exit config mode for secondary super IO.
        if (RegisterPort != 0x4E)
        {
            Ring0.WriteIoPort(RegisterPort, CONFIGURATION_CONTROL_REGISTER);
            Ring0.WriteIoPort(ValuePort, 0x02);
        }
    }

    public void SmscEnter()
    {
        Ring0.WriteIoPort(RegisterPort, 0x55);
    }

    public void SmscExit()
    {
        Ring0.WriteIoPort(RegisterPort, 0xAA);
    }

    // ReSharper disable InconsistentNaming
    private const byte CONFIGURATION_CONTROL_REGISTER = 0x02;
    private const byte DEVICE_SELECT_REGISTER = 0x07;
    private const byte NUVOTON_HARDWARE_MONITOR_IO_SPACE_LOCK = 0x28;
    // ReSharper restore InconsistentNaming
}
