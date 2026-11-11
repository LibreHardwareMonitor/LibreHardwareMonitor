// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware;

/// <summary>
/// Well-known values of a <see cref="SensorType.Health"/> sensor. The scale is shared by all
/// providers so that consumers can interpret health without knowing the hardware.
/// </summary>
public static class SensorHealth
{
    public const float Healthy = 0;
    public const float Warning = 1;
    public const float Critical = 2;

    /// <summary>
    /// Converts a <see cref="SensorType.Health"/> value to its display text.
    /// </summary>
    /// <param name="value">Sensor value, or <see langword="null"/> if unavailable.</param>
    public static string ToDisplayString(float? value)
    {
        if (!value.HasValue)
            return "Unknown";

        switch ((int)value.Value)
        {
            case (int)Healthy:
                return "Healthy";
            case (int)Warning:
                return "Warning";
            case (int)Critical:
                return "Critical";
            default:
                return "Unknown";
        }
    }
}
