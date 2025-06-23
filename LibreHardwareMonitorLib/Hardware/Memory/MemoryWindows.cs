// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Runtime.InteropServices;
using LibreHardwareMonitor.Interop;

namespace LibreHardwareMonitor.Hardware.Memory;

internal static class MemoryWindows
{
    public static void Update(TotalMemory memory)
    {
        Kernel32.MEMORYSTATUSEX status = new() { dwLength = (uint)Marshal.SizeOf<Kernel32.MEMORYSTATUSEX>() };

        if (!Kernel32.GlobalMemoryStatusEx(ref status))
            return;

        memory.PhysicalMemoryUsed.Value      = (float)(status.ullTotalPhys - status.ullAvailPhys) / (1024 * 1024 * 1024);
        memory.PhysicalMemoryAvailable.Value = (float)status.ullAvailPhys / (1024 * 1024 * 1024);
        memory.PhysicalMemoryLoad.Value      = 100.0f - ((100.0f * status.ullAvailPhys) / status.ullTotalPhys);
    }

    public static void Update(VirtualMemory memory)
    {
        Kernel32.MEMORYSTATUSEX status = new() { dwLength = (uint)Marshal.SizeOf<Kernel32.MEMORYSTATUSEX>() };

        if (!Kernel32.GlobalMemoryStatusEx(ref status))
            return;

        memory.VirtualMemoryUsed.Value      = (float)(status.ullTotalPageFile - status.ullAvailPageFile) / (1024 * 1024 * 1024);
        memory.VirtualMemoryAvailable.Value = (float)status.ullAvailPageFile / (1024 * 1024 * 1024);
        memory.VirtualMemoryLoad.Value      = 100.0f - ((100.0f * status.ullAvailPageFile) / status.ullTotalPageFile);
    }
}
