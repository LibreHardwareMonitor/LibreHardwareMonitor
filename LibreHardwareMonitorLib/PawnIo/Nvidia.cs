// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;

namespace LibreHardwareMonitor.PawnIo;

internal sealed class Nvidia
{
    public const int ThermalChannelCount = 6;

    private const uint ThermalChannelValid = 1u << 30;
    private const uint ThermalChannelValueMask = 0xFFFF;

    private readonly PawnIo _pawnIo = PawnIo.LoadModuleFromResource(typeof(Nvidia).Assembly, $"{nameof(LibreHardwareMonitor)}.Resources.PawnIo.Nvidia.bin");

    public bool IsLoaded => _pawnIo.IsLoaded;

    public bool TryReadThermalChannels(uint bus, uint device, uint function, float?[] temperatures)
    {
        if (temperatures == null)
        {
            throw new ArgumentNullException(nameof(temperatures));
        }

        if (temperatures.Length != ThermalChannelCount)
        {
            throw new ArgumentException($"Exactly {ThermalChannelCount} output values are required.", nameof(temperatures));
        }

        Array.Clear(temperatures, 0, temperatures.Length);

        if (!_pawnIo.IsLoaded)
        {
            return false;
        }

        try
        {
            var rawTemperatures = _pawnIo.Execute("ioctl_read_thermal_registers", [bus, device, function], ThermalChannelCount);
            bool hasValidTemperature = false;

            for (int i = 0; i < ThermalChannelCount; i++)
            {
                uint rawTemperature = (uint)rawTemperatures[i];

                if ((rawTemperature & ThermalChannelValid) == 0)
                {
                    continue;
                }

                temperatures[i] = (rawTemperature & ThermalChannelValueMask) / 256.0f;
                hasValidTemperature = true;
            }

            return hasValidTemperature;
        }
        catch
        {
            return false;
        }
    }

    public void Close() => _pawnIo.Close();
}
