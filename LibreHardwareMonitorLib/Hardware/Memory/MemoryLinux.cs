// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.IO;
using System.Linq;

namespace LibreHardwareMonitor.Hardware.Memory;

internal static class MemoryLinux
{
    public static void Update(TotalMemory memory)
    {
        try
        {
            string[] memoryInfo = File.ReadAllLines("/proc/meminfo");

            {
                float totalMemoryGb = GetMemInfoValue(memoryInfo.First(entry => entry.StartsWith("MemTotal:"))) / 1024.0f / 1024.0f;
                float freeMemoryGb = GetMemInfoValue(memoryInfo.First(entry => entry.StartsWith("MemFree:"))) / 1024.0f / 1024.0f;
                float cachedMemoryGb = GetMemInfoValue(memoryInfo.First(entry => entry.StartsWith("Cached:"))) / 1024.0f / 1024.0f;

                float usedMemoryGb = totalMemoryGb - freeMemoryGb - cachedMemoryGb;

                memory.PhysicalMemoryUsed.Value = usedMemoryGb;
                memory.PhysicalMemoryAvailable.Value = totalMemoryGb;
                memory.PhysicalMemoryLoad.Value = 100.0f * (usedMemoryGb / totalMemoryGb);
            }
        }
        catch
        {
            memory.PhysicalMemoryUsed.Value = null;
            memory.PhysicalMemoryAvailable.Value = null;
            memory.PhysicalMemoryLoad.Value = null;
        }
    }

    public static void Update(VirtualMemory memory)
    {
        try
        {
            string[] memoryInfo = File.ReadAllLines("/proc/meminfo");

            {
                float totalSwapMemoryGb = GetMemInfoValue(memoryInfo.First(entry => entry.StartsWith("SwapTotal"))) / 1024.0f / 1024.0f;
                float freeSwapMemoryGb = GetMemInfoValue(memoryInfo.First(entry => entry.StartsWith("SwapFree"))) / 1024.0f / 1024.0f;
                float usedSwapMemoryGb = totalSwapMemoryGb - freeSwapMemoryGb;

                memory.VirtualMemoryUsed.Value = usedSwapMemoryGb;
                memory.VirtualMemoryAvailable.Value = totalSwapMemoryGb;
                memory.VirtualMemoryLoad.Value = 100.0f * (usedSwapMemoryGb / totalSwapMemoryGb);
            }
        }
        catch
        {
            memory.VirtualMemoryUsed.Value = null;
            memory.VirtualMemoryAvailable.Value = null;
            memory.VirtualMemoryLoad.Value = null;
        }
    }

    private static long GetMemInfoValue(string line)
    {
        // Example: "MemTotal:       32849676 kB"

        string valueWithUnit = line.Split(':').Skip(1).First().Trim();
        string valueAsString = valueWithUnit.Split(' ').First();

        return long.Parse(valueAsString);
    }
}
