// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware;

/// <summary>
/// This structure describes a group-specific affinity.
/// </summary>
public readonly struct GroupAffinity
{
    public static GroupAffinity Undefined = new(ushort.MaxValue, 0);

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupAffinity" /> struct.
    /// </summary>
    /// <param name="group">The group.</param>
    /// <param name="mask">The mask.</param>
    public GroupAffinity(ushort group, ulong mask)
    {
        Group = group;
        Mask = mask;
    }

    /// <summary>
    /// Gets a single group affinity.
    /// </summary>
    /// <param name="group">The group.</param>
    /// <param name="index">The index.</param>
    /// <returns><see cref="GroupAffinity" />.</returns>
    public static GroupAffinity Single(ushort group, int index)
    {
        return new GroupAffinity(group, 1UL << index);
    }

    /// <summary>
    /// Gets the group.
    /// </summary>
    public ushort Group { get; }

    /// <summary>
    /// Gets the mask.
    /// </summary>
    public ulong Mask { get; }

    /// <summary>
    /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
    /// </summary>
    /// <param name="o">The <see cref="System.Object" /> to compare with this instance.</param>
    /// <returns><c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
    public override bool Equals(object o)
    {
        if (o == null || GetType() != o.GetType())
            return false;

        GroupAffinity a = (GroupAffinity)o;
        return (Group == a.Group) && (Mask == a.Mask);
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
    public override int GetHashCode()
    {
        return Group.GetHashCode() ^ Mask.GetHashCode();
    }

    /// <summary>
    /// Implements the == operator.
    /// </summary>
    /// <param name="a1">The a1.</param>
    /// <param name="a2">The a2.</param>
    /// <returns>The result of the operator.</returns>
    public static bool operator ==(GroupAffinity a1, GroupAffinity a2)
    {
        return (a1.Group == a2.Group) && (a1.Mask == a2.Mask);
    }

    /// <summary>
    /// Implements the != operator.
    /// </summary>
    /// <param name="a1">The a1.</param>
    /// <param name="a2">The a2.</param>
    /// <returns>The result of the operator.</returns>
    public static bool operator !=(GroupAffinity a1, GroupAffinity a2)
    {
        return (a1.Group != a2.Group) || (a1.Mask != a2.Mask);
    }
}