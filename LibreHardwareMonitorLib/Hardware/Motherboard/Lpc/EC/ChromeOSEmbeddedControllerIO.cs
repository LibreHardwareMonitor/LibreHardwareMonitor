// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Diagnostics;
using LibreHardwareMonitor.PawnIo;

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc.EC;

public class ChromeOSEmbeddedControllerIO : IEmbeddedControllerIO
{
    private bool _disposed;

    private readonly LpcCrOSEc _pawnModule;

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
            data[i] = this.ReadMemmap((byte)registers[i], 1)[0];
        }
    }

    public byte[] ReadMemmap(byte offset, byte bytes)
    {
        return _pawnModule.ReadMemmap(offset, bytes);
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
