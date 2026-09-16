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
                // /proc/meminfo reports kB, convert to bytes for the sensor.
                float totalMemoryBytes = GetMemInfoValue(memoryInfo.First(entry => entry.StartsWith("MemTotal:"))) * 1024.0f;
                float freeMemoryBytes = GetMemInfoValue(memoryInfo.First(entry => entry.StartsWith("MemFree:"))) * 1024.0f;
                float cachedMemoryBytes = GetMemInfoValue(memoryInfo.First(entry => entry.StartsWith("Cached:"))) * 1024.0f;

                float usedMemoryBytes = totalMemoryBytes - freeMemoryBytes - cachedMemoryBytes;

                memory.PhysicalMemoryUsed.Value = usedMemoryBytes;
                memory.PhysicalMemoryAvailable.Value = totalMemoryBytes;
                memory.PhysicalMemoryLoad.Value = 100.0f * (usedMemoryBytes / totalMemoryBytes);
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
                // /proc/meminfo reports kB, convert to bytes for the sensor.
                float totalSwapMemoryBytes = GetMemInfoValue(memoryInfo.First(entry => entry.StartsWith("SwapTotal"))) * 1024.0f;
                float freeSwapMemoryBytes = GetMemInfoValue(memoryInfo.First(entry => entry.StartsWith("SwapFree"))) * 1024.0f;
                float usedSwapMemoryBytes = totalSwapMemoryBytes - freeSwapMemoryBytes;

                memory.VirtualMemoryUsed.Value = usedSwapMemoryBytes;
                memory.VirtualMemoryAvailable.Value = totalSwapMemoryBytes;
                memory.VirtualMemoryLoad.Value = 100.0f * (usedSwapMemoryBytes / totalSwapMemoryBytes);
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
