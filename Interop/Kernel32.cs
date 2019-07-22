// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenHardwareMonitor.Interop {
  internal class Kernel32 {
    private const string KERNEL = "kernel32.dll";

    [StructLayout(LayoutKind.Sequential)]
    public struct MemoryStatusEx {
      public uint Length;
      public uint MemoryLoad;
      public ulong TotalPhysicalMemory;
      public ulong AvailablePhysicalMemory;
      public ulong TotalPageFile;
      public ulong AvailPageFile;
      public ulong TotalVirtual;
      public ulong AvailVirtual;
      public ulong AvailExtendedVirtual;
    }

    [DllImport(KERNEL, CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);
  }
}