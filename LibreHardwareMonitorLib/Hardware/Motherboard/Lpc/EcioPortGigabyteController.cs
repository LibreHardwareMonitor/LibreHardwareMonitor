// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
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

    private static void DebugLog(string message)
    {
        const string fileName = "EcioPortGigabyteController_DebugLog.txt";
        string header = $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ";
        System.IO.File.AppendAllText(fileName, header + message + System.Environment.NewLine);
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
                DebugLog($"ENABLE: Could not read at offset {offset}");
                return false;
            }

            DebugLog($"ENABLE: read value {bCurrent} at offset {offset}");

            _current = Convert.ToBoolean(bCurrent);
            _initialState ??= _current;
        }
        
        if (_current != enabled)
        {
            if (!_port.Write(offset, Convert.ToByte(enabled)))
            {
                DebugLog($"ENABLE: could not write value {Convert.ToByte(enabled)} at offset {offset}");
                return false;
            }

            DebugLog($"ENABLE: write value {Convert.ToByte(enabled)} at offset {offset}");

            // Allow the system to catch up.
            Thread.Sleep(500);
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
}
