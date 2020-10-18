// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using LibreHardwareMonitor.Interop;

namespace LibreHardwareMonitor.Hardware
{
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
        /// Returns true if the <paramref name="affinity"/> is valid.
        /// </summary>
        /// <param name="affinity">The affinity.</param>
        /// <returns><c>true</c> if the specified affinity is valid; otherwise, <c>false</c>.</returns>
        public static bool IsValid(GroupAffinity affinity)
        {
            if (Software.OperatingSystem.IsUnix)
            {
                if (affinity.Group > 0)
                    return false;
            }

            try
            {
                GroupAffinity previousAffinity = Set(affinity);
                if (previousAffinity == GroupAffinity.Undefined)
                    return false;


                Set(previousAffinity);
                return true;
            }
            catch
            {
                return false;
            }
        }

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
                if (LibC.sched_setaffinity(0, (IntPtr)8, ref mask) != 0)
                    return GroupAffinity.Undefined;


                return new GroupAffinity(0, result);
            }

            UIntPtr uIntPtrMask;
            try
            {
                uIntPtrMask = (UIntPtr)affinity.Mask;
            }
            catch (OverflowException)
            {
                throw new ArgumentOutOfRangeException(nameof(affinity));
            }

            var groupAffinity = new Kernel32.GROUP_AFFINITY { Group = affinity.Group, Mask = uIntPtrMask };

            IntPtr currentThread = Kernel32.GetCurrentThread();

            try
            {
                if (Kernel32.SetThreadGroupAffinity(currentThread,
                                                    ref groupAffinity,
                                                    out Kernel32.GROUP_AFFINITY previousGroupAffinity))
                {
                    return new GroupAffinity(previousGroupAffinity.Group, (ulong)previousGroupAffinity.Mask);
                }

                return GroupAffinity.Undefined;
            }
            catch (EntryPointNotFoundException)
            {
                if (affinity.Group > 0)
                    throw new ArgumentOutOfRangeException(nameof(affinity));


                ulong previous = (ulong)Kernel32.SetThreadAffinityMask(currentThread, uIntPtrMask);

                return new GroupAffinity(0, previous);
            }
        }
    }
}
