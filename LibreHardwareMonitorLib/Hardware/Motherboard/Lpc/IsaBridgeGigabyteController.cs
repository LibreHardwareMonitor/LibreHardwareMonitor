// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
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
    private readonly IsaBridgeEc _isaBridgeEc;
    private readonly MMIOMapping _mmio;
    private MMIOState? _originalState;
    private bool _enabled;

    private const int ControllerEnableRegister = 0x47;
    private const uint ControllerFanControlArea = 0x900;

    private IsaBridgeGigabyteController(IsaBridgeEc isaBridgeEc, MMIOMapping mmio, MMIOState originalState)
    {
        _isaBridgeEc = isaBridgeEc;
        _mmio = mmio;
        _originalState = originalState;
    }

    public static bool TryCreate(out IsaBridgeGigabyteController isaBridgeGigabyteController)
    {
        isaBridgeGigabyteController = null;
        IsaBridgeEc _isaBridgeEc = new IsaBridgeEc();

        // find
        if (!_isaBridgeEc.FindSuperIoMMIO(out MMIOMapping mmio))
        {
            _isaBridgeEc.Close();
            return false;
        }

        // get original state
        if (!_isaBridgeEc.GetOriginalState(out MMIOState state))
        {
            _isaBridgeEc.Close();
            return false;
        }

        // map
        if (!_isaBridgeEc.Map())
        {
            _isaBridgeEc.Close();
            return false;
        }

        // try set state to disabled then enabled4E mode
        if (!_isaBridgeEc.TrySetState(MMIOState.MMIO_Disabled) || !_isaBridgeEc.TrySetState(mmio.Index == 0 ? MMIOState.MMIO_Enabled2E : MMIOState.MMIO_Enabled4E))
        {
            _isaBridgeEc.Close();
            return false;
        }


        isaBridgeGigabyteController = new IsaBridgeGigabyteController(_isaBridgeEc, mmio, state);
        return true;
    }

    /// <summary>
    /// Enable/Disable Fan Control
    /// </summary>
    /// <param name="enabled"></param>
    /// <returns>true on success</returns>
    public bool Enable(bool enabled)
    {
        if (_enabled == enabled)
        {
            return false;
        }

        if (!_isaBridgeEc.ReadMmio(
            superIoIndex: _mmio.Index,
            offset: ControllerFanControlArea + ControllerEnableRegister,
            size: 1,
            value: out byte value))
        {
            return false;
        }

        bool isEnabled = Convert.ToBoolean(value);

        if (isEnabled == enabled)
        {
            return false;
        }

        value = Convert.ToByte(enabled);

        if (!_isaBridgeEc.WriteMmio(
            superIoIndex: _mmio.Index,
            offset: ControllerFanControlArea + ControllerEnableRegister,
            size: 1,
            value: value))
        {
            return false;
        }


        return true;
    }

    /// <summary>
    /// Restore settings back to initial values
    /// </summary>
    public void Restore()
    {
        Enable(false);

        if (_originalState.HasValue)
            _isaBridgeEc.TrySetState(_originalState.Value);
    }

    public void Dispose()
    {
        Restore();
        _isaBridgeEc.Close();
    }
}
