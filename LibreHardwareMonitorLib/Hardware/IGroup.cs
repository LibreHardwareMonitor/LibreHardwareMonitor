// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Collections.Generic;

namespace LibreHardwareMonitor.Hardware;

/// <summary>
/// A group of devices from one category in one list.
/// </summary>
internal interface IGroup
{
    /// <summary>
    /// Gets a list that stores information about <see cref="IHardware"/> in a given group.
    /// </summary>
    IReadOnlyList<IHardware> Hardware { get; }

    /// <summary>
    /// Report containing most of the known information about all <see cref="IHardware"/> in this <see cref="IGroup"/>.
    /// </summary>
    /// <returns>A formatted text string with hardware information.</returns>
    string GetReport();

    /// <summary>
    /// Stop updating this group in the future.
    /// </summary>
    void Close();
}