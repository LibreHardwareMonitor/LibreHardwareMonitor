// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc.EC;

public class ChromeOSEmbeddedControllerIO : IEmbeddedControllerIO
{
    private bool _disposed;

    public ushort BasePort { get; }

    private readonly ushort[] EC_LPC_ADDR_MEMMAP = { 0x900, 0xE00 };
    private const byte EC_MEMMAP_SIZE = 255;
    private const byte EC_MEMMAP_ID = 0x20;

    public ChromeOSEmbeddedControllerIO()
    {
        if (!Mutexes.WaitEc(10))
        {
            throw new BusMutexLockingFailedException();
        }
        
        // Find the EC base port
        foreach (var port in EC_LPC_ADDR_MEMMAP)
        {
            if (Ring0.ReadIoPort((uint)(port + EC_MEMMAP_ID)) == 'E' &&
                Ring0.ReadIoPort((uint)(port + EC_MEMMAP_ID + 1)) == 'C')
            {
                BasePort = port;
                return;
            }
        }

        throw new NotImplementedException("Embedded controller not found");
    }

    public void Read(ushort[] registers, byte[] data)
    {
        if (registers.Length != data.Length)
        {
            throw new ArgumentException("Registers and data length mismatch");
        }

        if (registers.Length > EC_MEMMAP_SIZE)
        {
            throw new ArgumentException("Registers length exceeds 255");
        }

        for (int i = 0; i < registers.Length; i++)
        {
            data[i] = Ring0.ReadIoPort((uint)(BasePort + registers[i]));
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
