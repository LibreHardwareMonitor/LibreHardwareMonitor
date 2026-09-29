// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using Windows.Win32;
using Windows.Win32.System.SystemInformation;

namespace LibreHardwareMonitor.Hardware.Memory;

internal static unsafe class MemoryWindows
{
    public static void Update(TotalMemory memory)
    {
        MEMORYSTATUSEX status = new() { dwLength = (uint)sizeof(MEMORYSTATUSEX) };

        if (!PInvoke.GlobalMemoryStatusEx(ref status))
            return;

        memory.PhysicalMemoryUsed.Value = (float)(status.ullTotalPhys - status.ullAvailPhys);
        memory.PhysicalMemoryAvailable.Value = (float)status.ullAvailPhys;
        memory.PhysicalMemoryLoad.Value = 100.0f - ((100.0f * status.ullAvailPhys) / status.ullTotalPhys);
    }

    public static void Update(VirtualMemory memory)
    {
        MEMORYSTATUSEX status = new() { dwLength = (uint)sizeof(MEMORYSTATUSEX) };

        if (!PInvoke.GlobalMemoryStatusEx(ref status))
            return;

        memory.VirtualMemoryUsed.Value = (float)(status.ullTotalPageFile - status.ullAvailPageFile);
        memory.VirtualMemoryAvailable.Value = (float)status.ullAvailPageFile;
        memory.VirtualMemoryLoad.Value = 100.0f - ((100.0f * status.ullAvailPageFile) / status.ullTotalPageFile);
    }
}
