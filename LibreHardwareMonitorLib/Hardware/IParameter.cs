// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware;

/// <summary>
/// Abstract object that represents additional parameters included in <see cref="ISensor"/>.
/// </summary>
public interface IParameter : IElement
{
    /// <summary>
    /// Gets a parameter default value defined by library.
    /// </summary>
    float DefaultValue { get; }

    /// <summary>
    /// Gets a parameter description defined by library.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets a unique parameter ID that represents its location.
    /// </summary>
    Identifier Identifier { get; }

    /// <summary>
    /// Gets or sets information whether the given <see cref="IParameter"/> is the default for <see cref="ISensor"/>.
    /// </summary>
    bool IsDefault { get; set; }

    /// <summary>
    /// Gets a parameter name defined by library.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the sensor that is the data container for the given parameter.
    /// </summary>
    ISensor Sensor { get; }

    /// <summary>
    /// Gets or sets the current value.
    /// </summary>
    float Value { get; set; }
}