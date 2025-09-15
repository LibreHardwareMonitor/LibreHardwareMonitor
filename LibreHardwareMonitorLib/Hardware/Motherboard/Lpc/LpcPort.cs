// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc;

internal class LpcPort
{
    private PawnIo.LpcIo _pawnModule;

    public LpcPort(ushort registerPort, ushort valuePort)
    {
        RegisterPort = registerPort;
        ValuePort = valuePort;
        _pawnModule = new PawnIo.LpcIo();
        _pawnModule.SelectSlot(registerPort == 0x2e ? 0 : 1);
    }

    public ushort RegisterPort { get; }

    public ushort ValuePort { get; }

    public byte ReadIoPort(ushort port)
    {
        return _pawnModule.ReadPort(port);
    }

    public void WriteIoPort(ushort port, byte value)
    {
        _pawnModule.WritePort(port, value);
    }

    public byte ReadByte(byte register)
    {
        return _pawnModule.ReadByte(register);
    }

    public void WriteByte(byte register, byte value)
    {
        _pawnModule.WriteByte(register, value);
    }

    public ushort ReadWord(byte register)
    {
        return _pawnModule.ReadWord(register);
    }

    public bool TryReadWord(byte register, out ushort value)
    {
        value = ReadWord(register);
        return value != 0xFFFF;
    }

    public void FindBars()
    {
        _pawnModule.FindBars();
    }

    public void Select(byte logicalDeviceNumber)
    {
        WriteByte(DEVICE_SELECT_REGISTER, logicalDeviceNumber);
    }

    public void WinbondNuvotonFintekEnter()
    {
        _pawnModule.WritePort(RegisterPort, 0x87);
        _pawnModule.WritePort(RegisterPort, 0x87);
    }

    public void WinbondNuvotonFintekExit()
    {
        _pawnModule.WritePort(RegisterPort, 0xAA);
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
        _pawnModule.WritePort(RegisterPort, 0x87);
        _pawnModule.WritePort(RegisterPort, 0x01);
        _pawnModule.WritePort(RegisterPort, 0x55);
        _pawnModule.WritePort(RegisterPort, RegisterPort == 0x4E ? (byte)0xAA : (byte)0x55);
    }

    public void IT87Exit()
    {
        // Do not exit config mode for secondary super IO.
        if (RegisterPort != 0x4E)
        {
            _pawnModule.WritePort(RegisterPort, CONFIGURATION_CONTROL_REGISTER);
            _pawnModule.WritePort(ValuePort, 0x02);
        }
    }

    public void SmscEnter()
    {
        _pawnModule.WritePort(RegisterPort, 0x55);
    }

    public void SmscExit()
    {
        _pawnModule.WritePort(RegisterPort, 0xAA);
    }

    public void Close() => _pawnModule.Close();

    // ReSharper disable InconsistentNaming
    private const byte CONFIGURATION_CONTROL_REGISTER = 0x02;
    private const byte DEVICE_SELECT_REGISTER = 0x07;
    private const byte NUVOTON_HARDWARE_MONITOR_IO_SPACE_LOCK = 0x28;
    // ReSharper restore InconsistentNaming
}
