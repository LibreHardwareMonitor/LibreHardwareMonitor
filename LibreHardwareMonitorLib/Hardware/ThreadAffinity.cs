// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Runtime.InteropServices;

namespace LibreHardwareMonitor.Hardware
{
    internal static class ThreadAffinity
    {
        public static ulong Set(ulong mask)
        {
            if (mask == 0)
                return 0;


            if (Software.OperatingSystem.IsLinux)
            {
                ulong result = 0;
                if (Interop.LibC.sched_getaffinity(0, (IntPtr)Marshal.SizeOf(result), ref result) != 0)
                    return 0;

                return Interop.LibC.sched_setaffinity(0, (IntPtr)Marshal.SizeOf(mask), ref mask) != 0 ? (ulong) 0 : result;
            }

            UIntPtr uIntPtrMask;
            try
            {
                uIntPtrMask = (UIntPtr)mask;
            }
            catch (OverflowException)
            {
                throw new ArgumentOutOfRangeException(nameof(mask));
            }
            return (ulong)Interop.Kernel32.SetThreadAffinityMask(Interop.Kernel32.GetCurrentThread(), uIntPtrMask);
        }
    }
}

