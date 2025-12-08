// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System.Diagnostics;
using System.Text;
using LibreHardwareMonitor.PawnIo;

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc.EC;

public class ChromeOSEmbeddedControllerIO : IEmbeddedControllerIO
{
    private const short EC_CMD_TEMP_SENSOR_GET_INFO = 0x0070;

    private readonly LpcCrOSEc _pawnModule;
    private bool _disposed;

    public ChromeOSEmbeddedControllerIO()
    {
        _pawnModule = new LpcCrOSEc();

        if (!Mutexes.WaitEc(10))
        {
            throw new BusMutexLockingFailedException();
        }
    }

    public void Read(ushort[] registers, byte[] data)
    {
        Trace.Assert(registers.Length <= data.Length,
                     "data buffer length has to be greater or equal to the registers array length");

        for (int i = 0; i < registers.Length; i++)
        {
            data[i] = ReadMemmap((byte)registers[i], 1)[0];
        }
    }

    public byte[] ReadMemmap(byte offset, byte bytes)
    {
        return _pawnModule.ReadMemmap(offset, bytes);
    }

    public string TempSensorGetName(byte index)
    {
        try
        {
            byte[] resp = _pawnModule.EcCommand(0, EC_CMD_TEMP_SENSOR_GET_INFO, 1, 33, [index]);
            //byte sensorType = resp[33];
            return Encoding.ASCII.GetString(resp, 0, 32).TrimEnd('\0');
        }
        catch (System.Exception)
        {
            return "Temp " + index;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            Mutexes.ReleaseEc();
        }
    }

    public class BusMutexLockingFailedException : EmbeddedController.IOException
    {
        public BusMutexLockingFailedException()
            : base("could not lock ISA bus mutex")
        { }
    }
}
