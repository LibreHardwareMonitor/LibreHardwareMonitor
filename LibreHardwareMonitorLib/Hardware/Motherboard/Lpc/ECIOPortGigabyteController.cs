// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Threading;

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc;

internal class ECIOPortGigabyteController : IGigabyteController
{
    private const ushort CONTROLLER_VERSION_OFFSET = 0x00;
    private const ushort CONTROLLER_ENABLE_OFFSET = 0x47;
    private const ushort CONTROLLER_FUNCTION_OFFSET = 0x900;
    private const ushort ECIO_REGISTER_PORT = 0x3F4;
    private const ushort ECIO_VALUE_PORT = 0x3F0;

    private IT879xECIOPort _port;
    private bool? _initialState;

    private ECIOPortGigabyteController(IT879xECIOPort port)
    {
        _port = port;
    }

    public static ECIOPortGigabyteController TryCreate()
    {
        IT879xECIOPort port = new IT879xECIOPort(ECIO_REGISTER_PORT, ECIO_VALUE_PORT);

        // Check compatibility by querying its version.
        byte majorVersion = port.Read(CONTROLLER_FUNCTION_OFFSET + CONTROLLER_VERSION_OFFSET);
        if (majorVersion != 1)
            return null;

        return new ECIOPortGigabyteController(port);
    }

    public bool Enable(bool enabled)
    {
        ushort offset = CONTROLLER_FUNCTION_OFFSET + CONTROLLER_ENABLE_OFFSET;

        bool current = Convert.ToBoolean(_port.Read(offset));

        _initialState ??= current;

        if (current && !enabled)
            _port.Write(offset, 0);
        else if (!current && enabled)
            _port.Write(offset, 1);

        // Allow the system to catch up.
        Thread.Sleep(250);

        return true;
    }

    public void Restore()
    {
        if (_initialState.HasValue)
            Enable(_initialState.Value);
    }
}
