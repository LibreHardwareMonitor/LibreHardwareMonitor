// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware;

/// <summary>
/// Composite class containing information about the selected <see cref="ISensor"/>.
/// </summary>
public struct ParameterDescription
{
    /// <summary>
    /// Creates a new instance and assigns values.
    /// </summary>
    /// <param name="name">Name of the selected component.</param>
    /// <param name="description">Description of the selected component.</param>
    /// <param name="defaultValue">Default value of the selected component.</param>
    public ParameterDescription(string name, string description, float defaultValue)
    {
        Name = name;
        Description = description;
        DefaultValue = defaultValue;
    }

    /// <summary>
    /// Gets a name of the parent <see cref="ISensor"/>.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets a description of the parent <see cref="ISensor"/>.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets a default value of the parent <see cref="ISensor"/>.
    /// </summary>
    public float DefaultValue { get; }
}