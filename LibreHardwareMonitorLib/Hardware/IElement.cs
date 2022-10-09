// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware;

/// <summary>
/// Abstract parent with logic for the abstract class that stores data.
/// </summary>
public interface IElement
{
    /// <summary>
    /// Accepts the observer for this instance.
    /// </summary>
    /// <param name="visitor">Computer observer making the calls.</param>
    void Accept(IVisitor visitor);

    /// <summary>
    /// Call the <see cref="Accept"/> method for all child instances <c>(called only from visitors).</c>
    /// </summary>
    /// <param name="visitor">Computer observer making the calls.</param>
    void Traverse(IVisitor visitor);
}