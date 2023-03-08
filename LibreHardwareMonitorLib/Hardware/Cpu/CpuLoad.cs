// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace LibreHardwareMonitor.Hardware.CPU;

internal class CpuLoad
{
    private long[] _idleTimes;
    private readonly float[] _threadLoads;
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
        idle = null;
        total = null;

        //Query processor idle information
        Interop.NtDll.SYSTEM_PROCESSOR_IDLE_INFORMATION[] idleInformation = new Interop.NtDll.SYSTEM_PROCESSOR_IDLE_INFORMATION[64];
        int idleSize = Marshal.SizeOf(typeof(Interop.NtDll.SYSTEM_PROCESSOR_IDLE_INFORMATION));
        if (Interop.NtDll.NtQuerySystemInformation(Interop.NtDll.SYSTEM_INFORMATION_CLASS.SystemProcessorIdleInformation, idleInformation, idleInformation.Length * idleSize, out int idleReturn) != 0)
        {
            return false;
        }

        //Query processor performance information
        Interop.NtDll.SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION[] perfInformation = new Interop.NtDll.SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION[64];
        int perfSize = Marshal.SizeOf(typeof(Interop.NtDll.SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION));
        if (Interop.NtDll.NtQuerySystemInformation(Interop.NtDll.SYSTEM_INFORMATION_CLASS.SystemProcessorPerformanceInformation, perfInformation, perfInformation.Length * perfSize, out int perfReturn) != 0)
        {
            return false;
        }

        idle = new long[idleReturn / idleSize];
        for (int i = 0; i < idle.Length; i++)
        {
            idle[i] = idleInformation[i].IdleTime;
        }

        total = new long[perfReturn / perfSize];
        for (int i = 0; i < total.Length; i++)
        {
            total[i] = perfInformation[i].KernelTime + perfInformation[i].UserTime;
        }

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
        if (_idleTimes == null)
            return;

        if (!GetTimes(out long[] newIdleTimes, out long[] newTotalTimes))
            return;

        for (int i = 0; i < Math.Min(newTotalTimes.Length, _totalTimes.Length); i++)
        {
            if (newTotalTimes[i] - _totalTimes[i] < 100000)
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
