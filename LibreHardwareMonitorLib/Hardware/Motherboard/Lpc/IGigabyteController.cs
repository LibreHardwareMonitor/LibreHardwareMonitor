// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc;

internal interface IGigabyteController: IDisposable
{
    bool Enable(bool enabled);

    void Restore();

    /// <summary>
    /// Sets an IT879x fan control through the EC, leaving the controller enabled. Null or 0 hands it back to the BIOS curve.
    /// </summary>
    bool SetControl(int index, byte? value);

    /// <summary>
    /// Reads consecutive bytes of EC memory, starting at <paramref name="offset" />.
    /// </summary>
    bool Read(ushort offset, byte[] buffer);

    bool Write(ushort offset, byte value);
}
