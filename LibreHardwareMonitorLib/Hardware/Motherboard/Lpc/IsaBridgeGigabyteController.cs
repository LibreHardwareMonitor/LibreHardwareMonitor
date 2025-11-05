// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using LibreHardwareMonitor.Hardware.Cpu;
using LibreHardwareMonitor.PawnIo;

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc;

/// <summary>
/// This is a controller present on some Gigabyte motherboards for both Intel and AMD, that is in custom firmware
/// loaded onto the 2nd ITE EC.
/// It can be accessed by using memory mapped IO, mapping its internal RAM onto main RAM via the ISA Bridge.
/// This class can disable it so that the regular IT87XX code can drive the fans.
/// </summary>
internal class IsaBridgeGigabyteController : IGigabyteController
{
    private readonly IsaBridgeEc _isaBridgeEc = new IsaBridgeEc();
    private MMIOState? _originalState;
    private bool _enabled;

    private IsaBridgeGigabyteController(IsaBridgeEc isaBridgeEc, MMIOState originalState)
    {
        _isaBridgeEc = isaBridgeEc;
        _originalState = originalState;
    }

    public static bool TryCreate(out IsaBridgeGigabyteController isaBridgeGigabyteController)
    {
        IsaBridgeEc _isaBridgeEc = new IsaBridgeEc();
        if (_isaBridgeEc.GetOriginalState(out MMIOState state))
        {
            isaBridgeGigabyteController = new IsaBridgeGigabyteController(_isaBridgeEc, state);
            return true;
        }

        _isaBridgeEc.Close();
        isaBridgeGigabyteController = null;
        return false;
    }

    /// <summary>
    /// Enable/Disable Fan Control
    /// </summary>
    /// <param name="enabled"></param>
    /// <returns>true on success</returns>
    public bool Enable(bool enabled)
    {
        if (_enabled != enabled)
        {
            return false;
        }

        if (enabled)
        {
            if (_isaBridgeEc.TryGetCurrentState(out MMIOState currentState) && currentState != MMIOState.MMIO_Enabled4E)
            {
                if ( _isaBridgeEc.TrySetState(MMIOState.MMIO_Enabled4E))
                {
                    _enabled = true;
                    return true;
                }
            }
        }
        else
        {
            if ( _isaBridgeEc.TrySetState(MMIOState.MMIO_Disabled))
            {
                _enabled = false;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Restore settings back to initial values
    /// </summary>
    public void Restore()
    {
        if (_originalState.HasValue)
            _isaBridgeEc.TrySetState(_originalState.Value);
    }

    public void Dispose() => _isaBridgeEc.Close();
}
