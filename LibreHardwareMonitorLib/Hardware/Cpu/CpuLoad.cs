// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace LibreHardwareMonitor.Hardware.Cpu;

internal class CpuLoad
{
    private readonly float[] _threadLoads;

    private long[] _idleTimes;
    private float _totalLoad;
    private long[] _totalTimes;

    public CpuLoad(CpuId[][] cpuid)
    {
        _threadLoads = new float[cpuid.Sum(x => x.Length)];
        _totalLoad = 0;

        try
        {
            GetTimes(out _idleTimes, out _totalTimes);
        }
        catch (Exception)
        {
            _idleTimes = null;
            _totalTimes = null;
        }

        if (_idleTimes != null)
            IsAvailable = true;
    }

    public bool IsAvailable { get; }

    private static bool GetTimes(out long[] idle, out long[] total)
    {
        return !Software.OperatingSystem.IsUnix ? GetWindowsTimes(out idle, out total) : GetUnixTimes(out idle, out total);
    }

    private static bool GetWindowsTimes(out long[] idle, out long[] total)
    {
        idle = null;
        total = null;

        //Query processor idle information
        Interop.NtDll.SYSTEM_PROCESSOR_IDLE_INFORMATION[] idleInformation = new Interop.NtDll.SYSTEM_PROCESSOR_IDLE_INFORMATION[64];
        int idleSize = Marshal.SizeOf(typeof(Interop.NtDll.SYSTEM_PROCESSOR_IDLE_INFORMATION));
        if (Interop.NtDll.NtQuerySystemInformation(Interop.NtDll.SYSTEM_INFORMATION_CLASS.SystemProcessorIdleInformation, idleInformation, idleInformation.Length * idleSize, out int idleReturn) != 0)
            return false;

        //Query processor performance information
        Interop.NtDll.SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION[] perfInformation = new Interop.NtDll.SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION[64];
        int perfSize = Marshal.SizeOf(typeof(Interop.NtDll.SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION));
        if (Interop.NtDll.NtQuerySystemInformation(Interop.NtDll.SYSTEM_INFORMATION_CLASS.SystemProcessorPerformanceInformation,
                                                   perfInformation,
                                                   perfInformation.Length * perfSize,
                                                   out int perfReturn) != 0)
        {
            return false;
        }

        idle = new long[idleReturn / idleSize];
        for (int i = 0; i < idle.Length; i++)
            idle[i] = idleInformation[i].IdleTime;

        total = new long[perfReturn / perfSize];
        for (int i = 0; i < total.Length; i++)
            total[i] = perfInformation[i].KernelTime + perfInformation[i].UserTime;

        return true;
    }

    private static bool GetUnixTimes(out long[] idle, out long[] total)
    {
        idle = null;
        total = null;

        List<long> idleList = new();
        List<long> totalList = new();

        if (!File.Exists("/proc/stat"))
            return false;

        string[] cpuInfos = File.ReadAllLines("/proc/stat");

        // currently parse the OverAll CPU info
        // cpu   1583083 737    452845   36226266 723316   63685 31896     0       0       0
        // cpu0  397468  189    109728   9040007  191429   16939 14954     0       0       0
        // 0=cpu 1=user  2=nice 3=system 4=idle   5=iowait 6=irq 7=softirq 8=steal 9=guest 10=guest_nice
        foreach (string cpuInfo in cpuInfos.Where(s => s.StartsWith("cpu") && s.Length > 3 && s[3] != ' '))
        {
            string[] overall = cpuInfo.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            try
            {
                // Parse idle information.
                idleList.Add(long.Parse(overall[4]));
            }
            catch
            {
                // Ignored.
            }

            // Parse total information = user + nice + system + idle + iowait + irq + softirq + steal + guest + guest_nice.
            long currentTotal = 0;
            foreach (string item in overall.Skip(1))
            {
                try
                {
                    currentTotal += long.Parse(item);
                }
                catch
                {
                    // Ignored.
                }
            }

            totalList.Add(currentTotal);
        }

        idle = idleList.ToArray();
        total = totalList.ToArray();
        return true;
    }

    public float GetTotalLoad()
    {
        return _totalLoad;
    }

    public float GetThreadLoad(int thread)
    {
        return _threadLoads[thread];
    }

    public void Update()
    {
        if (_idleTimes == null || !GetTimes(out long[] newIdleTimes, out long[] newTotalTimes))
            return;

        int minDiff = Software.OperatingSystem.IsUnix ? 100 : 100000;
        for (int i = 0; i < Math.Min(newTotalTimes.Length, _totalTimes.Length); i++)
        {
            if (newTotalTimes[i] - _totalTimes[i] < minDiff)
                return;
        }

        if (newIdleTimes == null)
            return;

        float total = 0;
        int count = 0;
        for (int i = 0; i < _threadLoads.Length && i < _idleTimes.Length && i < newIdleTimes.Length; i++)
        {
            float idle = (newIdleTimes[i] - _idleTimes[i]) / (float)(newTotalTimes[i] - _totalTimes[i]);
            _threadLoads[i] = 100f * (1.0f - Math.Min(idle, 1.0f));
            total += idle;
            count++;
        }

        if (count > 0)
        {
            total = 1.0f - (total / count);
            total = total < 0 ? 0 : total;
        }
        else
        {
            total = 0;
        }

        _totalLoad = total * 100;
        _totalTimes = newTotalTimes;
        _idleTimes = newIdleTimes;
    }
}
