// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Diagnostics;
using System.Threading;

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc;

internal class IT879xECIOPort
{
    public IT879xECIOPort(ushort registerPort, ushort valuePort)
    {
        RegisterPort = registerPort;
        ValuePort = valuePort;
    }

    public ushort RegisterPort { get; }

    public ushort ValuePort { get; }

    public byte Read(ushort offset)
    {
        WriteToRegister(0xB0);
        WriteToValue((byte) ((offset >> 8) & 0xFF));
        WriteToValue((byte) (offset & 0xFF));
        return ReadFromValue();
    }

    public void Write(ushort offset, byte value)
    {
        WriteToRegister(0xB1);
        WriteToValue((byte)((offset >> 8) & 0xFF));
        WriteToValue((byte)(offset & 0xFF));
        WriteToValue(value);
    }

    private void WriteToRegister(byte value)
    {
        if (!WaitIBE())
            return;
        Ring0.WriteIoPort(RegisterPort, value);
        WaitIBE();
    }

    private void WriteToValue(byte value)
    {
        if (!WaitIBE())
            return;
        Ring0.WriteIoPort(ValuePort, value);
        WaitIBE();
    }

    private byte ReadFromValue()
    {
        if (!WaitOBF())
            return 0;
        return Ring0.ReadIoPort(ValuePort);
    }

    private bool WaitIBE()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            while ((Ring0.ReadIoPort(RegisterPort) & 2) != 0)
            {
                if (stopwatch.ElapsedMilliseconds > WAIT_TIMEOUT)
                    return false;

                Thread.Sleep(1);
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
                    return false;

                Thread.Sleep(1);
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
