// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware;

/// <summary>
/// Abstract object that stores information about the limits of <see cref="ISensor"/>.
/// </summary>
public interface ISensorLimits
{
    /// <summary>
    /// Upper limit of <see cref="ISensor"/> value.
    /// </summary>
    float? HighLimit { get; }

    /// <summary>
    /// Lower limit of <see cref="ISensor"/> value.
    /// </summary>
    float? LowLimit { get; }
}

/// <summary>
/// Abstract object that stores information about the critical limits of <see cref="ISensor"/>.
/// </summary>
public interface ICriticalSensorLimits
{
    /// <summary>
    /// Critical upper limit of <see cref="ISensor"/> value.
    /// </summary>
    float? CriticalHighLimit { get; }

    /// <summary>
    /// Critical lower limit of <see cref="ISensor"/> value.
    /// </summary>
    float? CriticalLowLimit { get; }
}