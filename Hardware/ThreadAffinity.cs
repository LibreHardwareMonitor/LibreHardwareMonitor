/*

  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.

  Copyright (C) 2010-2014 Michael Möller <mmoeller@openhardwaremonitor.org>

*/

using System;
using System.Runtime.InteropServices;

namespace OpenHardwareMonitor.Hardware {

  internal static class ThreadAffinity {

    public static ulong Set(ulong mask) {
      if (mask == 0)
        return 0;

      if (Software.OperatingSystem.IsLinux)
      { // Unix
        ulong result = 0;
        if (NativeMethods.sched_getaffinity(0, (IntPtr)Marshal.SizeOf(result),
          ref result) != 0)
          return 0;
        if (NativeMethods.sched_setaffinity(0, (IntPtr)Marshal.SizeOf(mask),
          ref mask) != 0)
          return 0;
        return result;
      } // Windows
        UIntPtr uIntPtrMask;
        try {
            uIntPtrMask = (UIntPtr)mask;
        } catch (OverflowException) {
            throw new ArgumentOutOfRangeException("mask");
        }
        return (ulong)NativeMethods.SetThreadAffinityMask(
            NativeMethods.GetCurrentThread(), uIntPtrMask);
    }

    private static class NativeMethods {
      private const string KERNEL = "kernel32.dll";

      [DllImport(KERNEL, CallingConvention = CallingConvention.Winapi)]
      public static extern UIntPtr
        SetThreadAffinityMask(IntPtr handle, UIntPtr mask);

      [DllImport(KERNEL, CallingConvention = CallingConvention.Winapi)]
      public static extern IntPtr GetCurrentThread();

      private const string LIBC = "libc";

      [DllImport(LIBC)]
      public static extern int sched_getaffinity(int pid, IntPtr maskSize,
        ref ulong mask);

      [DllImport(LIBC)]
      public static extern int sched_setaffinity(int pid, IntPtr maskSize,
        ref ulong mask);
    }
  }
}

