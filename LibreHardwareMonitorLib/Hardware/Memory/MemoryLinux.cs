// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.IO;
using System.Linq;

namespace LibreHardwareMonitor.Hardware.Memory
{
    internal static class MemoryLinux
    {
        public static void Update(DimmTotalMemory memory)
        {
            try
            {
                string[] memoryInfo = File.ReadAllLines("/proc/meminfo");

                {
                    float totalMemory_GB  = GetMemInfoValue(memoryInfo.First(entry => entry.StartsWith("MemTotal:"))) / 1024.0f / 1024.0f;
                    float freeMemory_GB   = GetMemInfoValue(memoryInfo.First(entry => entry.StartsWith("MemFree:" ))) / 1024.0f / 1024.0f;
                    float cachedMemory_GB = GetMemInfoValue(memoryInfo.First(entry => entry.StartsWith("Cached:"  ))) / 1024.0f / 1024.0f;

                    float usedMemory_GB = totalMemory_GB - freeMemory_GB - cachedMemory_GB;

                    memory.PhysicalMemoryUsed.Value      = usedMemory_GB;
                    memory.PhysicalMemoryAvailable.Value = totalMemory_GB;
                    memory.PhysicalMemoryLoad.Value      = 100.0f * (usedMemory_GB / totalMemory_GB);
                }
            }
            catch
            {
                memory.PhysicalMemoryUsed.Value      = null;
                memory.PhysicalMemoryAvailable.Value = null;
                memory.PhysicalMemoryLoad.Value      = null;
            }
        }

        public static void Update(DimmVirtualMemory memory)
        {
            try
            {
                string[] memoryInfo = File.ReadAllLines("/proc/meminfo");

                {
                    float totalSwapMemory_GB = GetMemInfoValue(memoryInfo.First(entry => entry.StartsWith("SwapTotal"))) / 1024.0f / 1024.0f;
                    float freeSwapMemory_GB  = GetMemInfoValue(memoryInfo.First(entry => entry.StartsWith("SwapFree" ))) / 1024.0f / 1024.0f;
                    float usedSwapMemory_GB  = totalSwapMemory_GB - freeSwapMemory_GB;

                    memory.VirtualMemoryUsed.Value      = usedSwapMemory_GB;
                    memory.VirtualMemoryAvailable.Value = totalSwapMemory_GB;
                    memory.VirtualMemoryLoad.Value      = 100.0f * (usedSwapMemory_GB / totalSwapMemory_GB);
                }
            }
            catch
            {
                memory.VirtualMemoryUsed.Value      = null;
                memory.VirtualMemoryAvailable.Value = null;
                memory.VirtualMemoryLoad.Value      = null;
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
}
