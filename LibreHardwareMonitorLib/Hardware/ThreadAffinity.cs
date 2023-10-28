// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using LibreHardwareMonitor.Interop;

namespace LibreHardwareMonitor.Hardware;

internal static class ThreadAffinity
{
    /// <summary>
    /// Initializes static members of the <see cref="ThreadAffinity" /> class.
    /// </summary>
    static ThreadAffinity()
    {
        ProcessorGroupCount = Software.OperatingSystem.IsUnix ? 1 : Kernel32.GetActiveProcessorGroupCount();

        if (ProcessorGroupCount < 1)
            ProcessorGroupCount = 1;
    }

    /// <summary>
    /// Gets the processor group count.
    /// </summary>
    public static int ProcessorGroupCount { get; }

    /// <summary>
    /// Sets the processor group affinity for the current thread.
    /// </summary>
    /// <param name="affinity">The processor group affinity.</param>
    /// <returns>The previous processor group affinity.</returns>
    public static GroupAffinity Set(GroupAffinity affinity)
    {
        if (affinity == GroupAffinity.Undefined)
            return GroupAffinity.Undefined;

        if (Software.OperatingSystem.IsUnix)
        {
            if (affinity.Group > 0)
                throw new ArgumentOutOfRangeException(nameof(affinity));

            ulong result = 0;
            if (LibC.sched_getaffinity(0, (IntPtr)8, ref result) != 0)
                return GroupAffinity.Undefined;

            ulong mask = affinity.Mask;
            return LibC.sched_setaffinity(0, (IntPtr)8, ref mask) != 0
                ? GroupAffinity.Undefined
                : new GroupAffinity(0, result);
        }

        ulong maxValue = IntPtr.Size == 8 ? ulong.MaxValue : uint.MaxValue;
        if (affinity.Mask > maxValue)
            throw new ArgumentOutOfRangeException(nameof(affinity));

        var groupAffinity = new Kernel32.GROUP_AFFINITY { Group = affinity.Group, Mask = (UIntPtr)affinity.Mask };

        IntPtr currentThread = Kernel32.GetCurrentThread();

        return Kernel32.SetThreadGroupAffinity(currentThread,
                                               ref groupAffinity,
                                               out Kernel32.GROUP_AFFINITY previousGroupAffinity)
            ? new GroupAffinity(previousGroupAffinity.Group, (ulong)previousGroupAffinity.Mask)
            : GroupAffinity.Undefined;
    }
}
