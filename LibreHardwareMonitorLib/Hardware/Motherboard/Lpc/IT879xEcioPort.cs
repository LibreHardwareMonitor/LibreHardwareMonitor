// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Diagnostics;

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc;

internal class IT879xEcioPort
{
    public IT879xEcioPort(ushort registerPort, ushort valuePort)
    {
        RegisterPort = registerPort;
        ValuePort = valuePort;
    }

    public ushort RegisterPort { get; }

    public ushort ValuePort { get; }

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

        Ring0.WriteIoPort(RegisterPort, value);
        return true;
    }

    private bool WriteToValue(byte value)
    {
        if (!WaitIBE())
        {
            return false;
        }

        Ring0.WriteIoPort(ValuePort, value);
        return true;
    }

    private bool ReadFromValue(out byte value)
    {
        if (!WaitOBF())
        {
            value = 0;
            return false;
        }

        value = Ring0.ReadIoPort(ValuePort);
        return true;
    }

    private bool WaitIBE()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            while ((Ring0.ReadIoPort(RegisterPort) & 2) != 0)
            {
                if (stopwatch.ElapsedMilliseconds > WAIT_TIMEOUT)
                {
                    return false;
                }
            }
            return true;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    private bool WaitOBF()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            while ((Ring0.ReadIoPort(RegisterPort) & 1) == 0)
            {
                if (stopwatch.ElapsedMilliseconds > WAIT_TIMEOUT)
                {
                    return false;
                }
            }
            return true;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    private const long WAIT_TIMEOUT = 1000L;
}
