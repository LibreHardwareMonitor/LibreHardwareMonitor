// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware;

/// <summary>
/// Abstract object that stores settings passed to <see cref="IComputer"/>, <see cref="IHardware"/> and <see cref="ISensor"/>.
/// </summary>
public interface ISettings
{
    /// <summary>
    /// Returns information whether the given collection of settings contains a value assigned to the given key.
    /// </summary>
    /// <param name="name">Key to which the setting value is assigned.</param>
    bool Contains(string name);

    /// <summary>
    /// Assigns a setting option to a given key.
    /// </summary>
    /// <param name="name">Key to which the setting value is assigned.</param>
    /// <param name="value">Text setting value.</param>
    void SetValue(string name, string value);

    /// <summary>
    /// Gets a setting option assigned to the given key.
    /// </summary>
    /// <param name="name">Key to which the setting value is assigned.</param>
    /// <param name="value">Default value.</param>
    string GetValue(string name, string value);

    /// <summary>
    /// Removes a setting with the specified key from the settings collection.
    /// </summary>
    /// <param name="name">Key to which the setting value is assigned.</param>
    void Remove(string name);
}