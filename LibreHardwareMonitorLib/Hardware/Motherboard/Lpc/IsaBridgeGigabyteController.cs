// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Threading;
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
    private bool? _enabled;
    private bool? _restoreEnabled;
    private const int ControllerEnableRegister = 0x47;
    private const uint ControllerFanControlArea = 0x900;

    private IsaBridgeGigabyteController(IsaBridgeEc isaBridgeEc, MMIOMapping mmio)
    {
        _isaBridgeEc = isaBridgeEc;
        _mmio = mmio;
    }

    public static bool TryCreate(out IsaBridgeGigabyteController isaBridgeGigabyteController)
    {
        isaBridgeGigabyteController = null;
        IsaBridgeEc _isaBridgeEc = new IsaBridgeEc();

        // find
        if (!_isaBridgeEc.FindSuperIoMMIO(out _, out MMIOMapping secondMmio))
        {
            _isaBridgeEc.Close();
            return false;
        }

        if (!_isaBridgeEc.GetOriginalState(out MMIOState originalState))
        {
            _isaBridgeEc.Close();
            return false;
        }

        if (!EnterMmio(_isaBridgeEc, originalState))
        {
            _isaBridgeEc.Close();
            return false;
        }

        // if we get 0xFF, we can't use the IsaBridgeGigabyteController
        if (!_isaBridgeEc.ReadMmio(
            superIoIndex: secondMmio.Index,
            offset: ControllerFanControlArea + ControllerEnableRegister,
            size: 1,
            value: out byte readvaluebyte) ||
            readvaluebyte == 0xFF)
        {
            _isaBridgeEc.Close();
            return false;
        }

        if (!ExitMmio(_isaBridgeEc))
        {
            _isaBridgeEc.Close();
            return false;
        }

        isaBridgeGigabyteController = new IsaBridgeGigabyteController(_isaBridgeEc, secondMmio);

        return true;
    }

    /// <summary>
    /// Enable/Disable Fan Control
    /// </summary>
    /// <param name="enabled"></param>
    /// <returns>true on success</returns>
    public bool Enable(bool enabled)
    {
        bool isEntered = false;

        // use try finally + isEntered to get a safe
        // EnterMmio and ExitMmio pattern
        try
        {
            // get initial state if missing
            if (_enabled is null)
            {
                isEntered = EnterMmio(_isaBridgeEc);
                if (!isEntered)
                {
                    return false;
                }

                if (!_isaBridgeEc.ReadMmio(
                      superIoIndex: _mmio.Index,
                      offset: ControllerFanControlArea + ControllerEnableRegister,
                      size: 1,
                      value: out byte readvaluebyte))
                {
                    return false;
                }

                bool readValue = Convert.ToBoolean(readvaluebyte);
                _restoreEnabled ??= readValue;
                _enabled = Convert.ToBoolean(readvaluebyte);
            }

            // if already enabled, return
            if (_enabled == enabled)
            {
                return true;
            }

            if (!isEntered)
            {
                // we didn't enter in the initial state block, enter now
                isEntered = EnterMmio(_isaBridgeEc);
                if (!isEntered)
                {
                    return false;
                }
            }

            // write the value
            byte writeValue = Convert.ToByte(enabled);
            if (!_isaBridgeEc.WriteMmio(
                superIoIndex: _mmio.Index,
                offset: ControllerFanControlArea + ControllerEnableRegister,
                size: 1,
                value: writeValue))
            {
                return false;
            }

            Thread.Sleep(500);

            _enabled = enabled;

            return true;
        }
        finally
        {
            // safe exit from any return above
            if (isEntered)
            {
                ExitMmio(_isaBridgeEc);
            }
        }
    }

    /// <summary>
    /// Restore settings back to initial values
    /// </summary>
    public void Restore()
    {
        if (_restoreEnabled is null)
        {
            return;
        }

        Enable(_restoreEnabled.Value);
    }

    public void Dispose()
    {
        Restore();
        _isaBridgeEc.Close();
    }

    private static bool EnterMmio(IsaBridgeEc isaBridgeEc, MMIOState? currentState = null)
    {
        if (!isaBridgeEc.Map())
        {
            return false;
        }

        if (currentState is null || (currentState != MMIOState.MMIO_Enabled4E && currentState != MMIOState.MMIO_EnabledBoth))
        {
            if (!isaBridgeEc.TrySetState(MMIOState.MMIO_Enabled4E))
            {
                isaBridgeEc.Unmap();
                return false;
            }
        }

        return true;
    }

    private static bool ExitMmio(IsaBridgeEc isaBridgeEc)
    {
        if (!isaBridgeEc.TrySetState(MMIOState.MMIO_Original))
        {
            return false;
        }

        if (!isaBridgeEc.Unmap())
        {
            return false;
        }

        return true;
    }
}
