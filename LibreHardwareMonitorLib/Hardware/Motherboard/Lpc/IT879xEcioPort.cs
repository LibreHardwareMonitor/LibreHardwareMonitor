// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Diagnostics;

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc;

internal class IT879xEcioPort
{
    private const ushort EcioRegisterPort = 0x3F4;
    private const ushort EcioValuePort = 0x3F0;

    public IT879xEcioPort(LpcPort lpcPort)
    {
        _lpcPort = lpcPort;
    }

    public bool Read(ushort offset, out byte value)
    {
        if (!Init(0xB0, offset))
        {
            value = 0;
            return false;
        }

        return ReadFromValue(out value);
    }

    public bool Write(ushort offset, byte value)
    {
        if (!Init(0xB1, offset))
        {
            return false;
        }

        return WriteToValue(value);
    }

    private bool Init(byte command, ushort offset)
    {
        if (!WriteToRegister(command))
        {
            return false;
        }

        if (!WriteToValue((byte)((offset >> 8) & 0xFF)))
        {
            return false;
        }

        if (!WriteToValue((byte)(offset & 0xFF)))
        {
            return false;
        }

        return true;
    }

    private bool WriteToRegister(byte value)
    {
        if (!WaitIBE())
        {
            return false;
        }

        _lpcPort.WriteIoPort(EcioRegisterPort, value);
        return true;
    }

    private bool WriteToValue(byte value)
    {
        if (!WaitIBE())
        {
            return false;
        }

        _lpcPort.WriteIoPort(EcioValuePort, value);
        return true;
    }

    private bool ReadFromValue(out byte value)
    {
        if (!WaitOBF())
        {
            value = 0;
            return false;
        }

        value = _lpcPort.ReadIoPort(EcioValuePort);
        return true;
    }

    private bool WaitIBE()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        while ((_lpcPort.ReadIoPort(EcioRegisterPort) & 2) != 0)
        {
            if (stopwatch.ElapsedMilliseconds > WAIT_TIMEOUT)
            {
                return false;
            }
        }

        return true;
    }

    private bool WaitOBF()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while ((_lpcPort.ReadIoPort(EcioRegisterPort) & 1) == 0)
        {
            if (stopwatch.ElapsedMilliseconds > WAIT_TIMEOUT)
            {
                return false;
            }
        }

        return true;
    }

    private const long WAIT_TIMEOUT = 1000L;
    private readonly LpcPort _lpcPort;
}
