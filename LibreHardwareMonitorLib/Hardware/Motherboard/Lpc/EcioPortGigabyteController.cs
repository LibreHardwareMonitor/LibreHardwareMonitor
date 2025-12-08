// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

//#define ECIO_GIGABYTE_CONTROLLER_DEBUG

using System;
using System.Diagnostics;
using System.Threading;

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc;

internal class EcioPortGigabyteController : IGigabyteController
{
    private const ushort ControllerVersionOffset = 0x00;
    private const ushort ControllerEnableRegister = 0x47;
    private const ushort ControllerFanControlArea = 0x900;

    private readonly IT879xEcioPort _port;

    private bool? _initialState;
    private bool? _current;

    private EcioPortGigabyteController(IT879xEcioPort port)
    {
        _port = port;
    }

    public static EcioPortGigabyteController TryCreate(LpcPort lpcPort)
    {
        IT879xEcioPort port = new(lpcPort);

        // Check compatibility by querying its version.
        if (!port.Read(ControllerFanControlArea + ControllerVersionOffset, out byte majorVersion) || majorVersion != 1)
        {
            return null;
        }

        return new EcioPortGigabyteController(port);
    }

    public bool Enable(bool enabled)
    {
        ushort offset = ControllerFanControlArea + ControllerEnableRegister;

        if (!_current.HasValue)
        {
            if (!_port.Read(offset, out byte bCurrent))
            {
                Log($"ENABLE: Could not read at offset {offset}");
                return false;
            }

            Log($"ENABLE: read value {bCurrent} at offset {offset}");

            _current = Convert.ToBoolean(bCurrent);
            _initialState ??= _current;
        }

        if (_current != enabled)
        {
            if (!_port.Write(offset, Convert.ToByte(enabled)))
            {
                Log($"ENABLE: could not write value {Convert.ToByte(enabled)} at offset {offset}");
                return false;
            }

            Log($"ENABLE: write value {Convert.ToByte(enabled)} at offset {offset}");

            // Allow the system to catch up.
            Thread.Sleep(500);

            _current = enabled;
        }

        return true;
    }

    public void Restore()
    {
        if (_initialState.HasValue)
        {
            Enable(_initialState.Value);
        }
    }

    public void Dispose()
    {
        Restore();
    }

    /// <summary>
    /// Writes a debug message to the output window and appends it to the debug log file when
    /// ECIO_GIGABYTE_CONTROLLER_DEBUG is defined.
    /// </summary>
    /// <remarks>This method only performs logging when the ECIO_GIGABYTE_CONTROLLER_DEBUG compilation symbol
    /// is defined. The log file is named "EcioPortGigabyteController_DebugLog.txt" and is appended to with each call.
    /// Use this method for diagnostic purposes during development or troubleshooting.</remarks>
    /// <param name="message">The message to log. This text is written to both the debug output and the log file.</param>
    [Conditional("DEBUG_LOG"), Conditional("ECIO_GIGABYTE_CONTROLLER_DEBUG")]
    private static void Log(string message)
    {
        Debug.WriteLine(message);
    }
}
