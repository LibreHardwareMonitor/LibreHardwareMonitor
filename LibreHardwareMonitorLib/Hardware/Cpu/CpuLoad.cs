// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Runtime.InteropServices;

namespace LibreHardwareMonitor.Hardware.CPU
{
    internal class CpuLoad
    {
        private readonly float[] _coreLoads;
        private readonly CpuId[][] _cpuid;
        private long[] _idleTimes;
        private float _totalLoad;
        private long[] _totalTimes;

        public CpuLoad(CpuId[][] cpuid)
        {
            _cpuid = cpuid;
            _coreLoads = new float[cpuid.Length];
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
            Interop.NtDll.SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION[] information = new Interop.NtDll.SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION[64];
            int size = Marshal.SizeOf(typeof(Interop.NtDll.SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION));

            idle = null;
            total = null;

            if (Interop.NtDll.NtQuerySystemInformation(Interop.NtDll.SYSTEM_INFORMATION_CLASS.SystemProcessorPerformanceInformation,
                                                       information,
                                                       information.Length * size,
                                                       out IntPtr returnLength) != 0)
            {
                return false;
            }

            idle = new long[(int)returnLength / size];
            total = new long[(int)returnLength / size];

            for (int i = 0; i < idle.Length; i++)
            {
                idle[i] = information[i].IdleTime;
                total[i] = information[i].KernelTime + information[i].UserTime;
            }

            return true;
        }

        public float GetTotalLoad()
        {
            return _totalLoad;
        }

        public float GetCoreLoad(int core)
        {
            return _coreLoads[core];
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
            for (int i = 0; i < _cpuid.Length; i++)
            {
                float value = 0;
                for (int j = 0; j < _cpuid[i].Length; j++)
                {
                    long index = _cpuid[i][j].Thread;
                    if (index < newIdleTimes.Length && index < _totalTimes.Length)
                    {
                        float idle = (newIdleTimes[index] - _idleTimes[index]) / (float)(newTotalTimes[index] - _totalTimes[index]);
                        value += idle;
                        total += idle;
                        count++;
                    }
                }

                value = 1.0f - value / _cpuid[i].Length;
                value = value < 0 ? 0 : value;
                _coreLoads[i] = value * 100;
            }

            if (count > 0)
            {
                total = 1.0f - total / count;
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
}
