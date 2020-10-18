// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace LibreHardwareMonitor.Hardware.CPU
{
    public class GenericCpu : Hardware
    {
        protected readonly int _coreCount;
        protected readonly CpuId[][] _cpuId;
        protected readonly uint _family;
        protected readonly uint _model;
        protected readonly int _processorIndex;
        protected readonly uint _stepping;

        private readonly Sensor[] _coreLoads;
        private readonly CpuLoad _cpuLoad;
        private readonly double _estimatedTimeStampCounterFrequency;
        private readonly double _estimatedTimeStampCounterFrequencyError;
        private readonly bool _isInvariantTimeStampCounter;
        private readonly Sensor _totalLoad;
        private readonly Vendor _vendor;
        private long _lastTime;
        private ulong _lastTimeStampCount;

        [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
        public GenericCpu(int processorIndex, CpuId[][] cpuId, ISettings settings) : base(cpuId[0][0].Name, CreateIdentifier(cpuId[0][0].Vendor, processorIndex), settings)
        {
            _cpuId = cpuId;
            _vendor = cpuId[0][0].Vendor;
            _family = cpuId[0][0].Family;
            _model = cpuId[0][0].Model;
            _stepping = cpuId[0][0].Stepping;

            _processorIndex = processorIndex;
            _coreCount = cpuId.Length;

            // check if processor has MSRs
            if (cpuId[0][0].Data.GetLength(0) > 1 && (cpuId[0][0].Data[1, 3] & 0x20) != 0)
                HasModelSpecificRegisters = true;
            else
                HasModelSpecificRegisters = false;

            // check if processor has a TSC
            if (cpuId[0][0].Data.GetLength(0) > 1 && (cpuId[0][0].Data[1, 3] & 0x10) != 0)
                HasTimeStampCounter = true;
            else
                HasTimeStampCounter = false;

            // check if processor supports an invariant TSC
            if (cpuId[0][0].ExtData.GetLength(0) > 7 && (cpuId[0][0].ExtData[7, 3] & 0x100) != 0)
                _isInvariantTimeStampCounter = true;
            else
                _isInvariantTimeStampCounter = false;

            _totalLoad = _coreCount > 1 ? new Sensor("CPU Total", 0, SensorType.Load, this, settings) : null;

            _coreLoads = new Sensor[_coreCount];
            for (int i = 0; i < _coreLoads.Length; i++)
                _coreLoads[i] = new Sensor(CoreString(i), i + 1, SensorType.Load, this, settings);

            _cpuLoad = new CpuLoad(cpuId);
            if (_cpuLoad.IsAvailable)
            {
                foreach (Sensor sensor in _coreLoads)
                    ActivateSensor(sensor);

                if (_totalLoad != null)
                    ActivateSensor(_totalLoad);
            }

            if (HasTimeStampCounter)
            {
                GroupAffinity previousAffinity = ThreadAffinity.Set(cpuId[0][0].Affinity);
                EstimateTimeStampCounterFrequency(out _estimatedTimeStampCounterFrequency, out _estimatedTimeStampCounterFrequencyError);
                ThreadAffinity.Set(previousAffinity);
            }
            else
            {
                _estimatedTimeStampCounterFrequency = 0;
            }

            TimeStampCounterFrequency = _estimatedTimeStampCounterFrequency;
        }

        /// <summary>
        /// Gets the CPUID.
        /// </summary>
        public CpuId[][] CpuId => _cpuId;

        public override HardwareType HardwareType => HardwareType.Cpu;

        public bool HasModelSpecificRegisters { get; }

        public bool HasTimeStampCounter { get; }

        public double TimeStampCounterFrequency { get; private set; }

        protected string CoreString(int i)
        {
            if (_coreCount == 1)
                return "CPU Core";


            return "CPU Core #" + (i + 1);
        }

        private static Identifier CreateIdentifier(Vendor vendor, int processorIndex)
        {
            string s;
            switch (vendor)
            {
                case Vendor.AMD:
                    s = "amdcpu";
                    break;
                case Vendor.Intel:
                    s = "intelcpu";
                    break;
                default:
                    s = "genericcpu";
                    break;
            }

            return new Identifier(s, processorIndex.ToString(CultureInfo.InvariantCulture));
        }

        private void EstimateTimeStampCounterFrequency(out double frequency, out double error)
        {
            // preload the function
            EstimateTimeStampCounterFrequency(0, out double f, out double e);
            EstimateTimeStampCounterFrequency(0, out f, out e);

            // estimate the frequency
            error = double.MaxValue;
            frequency = 0;
            for (int i = 0; i < 5; i++)
            {
                EstimateTimeStampCounterFrequency(0.025, out f, out e);
                if (e < error)
                {
                    error = e;
                    frequency = f;
                }

                if (error < 1e-4)
                    break;
            }
        }

        private static void EstimateTimeStampCounterFrequency(double timeWindow, out double frequency, out double error)
        {
            long ticks = (long)(timeWindow * Stopwatch.Frequency);

            long timeBegin = Stopwatch.GetTimestamp() + (long)Math.Ceiling(0.001 * ticks);
            long timeEnd = timeBegin + ticks;

            while (Stopwatch.GetTimestamp() < timeBegin)
            { }

            ulong countBegin = OpCode.Rdtsc();
            long afterBegin = Stopwatch.GetTimestamp();

            while (Stopwatch.GetTimestamp() < timeEnd)
            { }

            ulong countEnd = OpCode.Rdtsc();
            long afterEnd = Stopwatch.GetTimestamp();

            double delta = timeEnd - timeBegin;
            frequency = 1e-6 * ((double)(countEnd - countBegin) * Stopwatch.Frequency) / delta;

            double beginError = (afterBegin - timeBegin) / delta;
            double endError = (afterEnd - timeEnd) / delta;
            error = beginError + endError;
        }

        private static void AppendMsrData(StringBuilder r, uint msr, GroupAffinity affinity)
        {
            if (Ring0.ReadMsr(msr, out uint eax, out uint edx, affinity))
            {
                r.Append(" ");
                r.Append(msr.ToString("X8", CultureInfo.InvariantCulture));
                r.Append("  ");
                r.Append(edx.ToString("X8", CultureInfo.InvariantCulture));
                r.Append("  ");
                r.Append(eax.ToString("X8", CultureInfo.InvariantCulture));
                r.AppendLine();
            }
        }

        protected virtual uint[] GetMsrs()
        {
            return null;
        }

        public override string GetReport()
        {
            StringBuilder r = new StringBuilder();

            switch (_vendor)
            {
                case Vendor.AMD:
                    r.AppendLine("AMD CPU");
                    break;
                case Vendor.Intel:
                    r.AppendLine("Intel CPU");
                    break;
                default:
                    r.AppendLine("Generic CPU");
                    break;
            }

            r.AppendLine();
            r.AppendFormat("Name: {0}{1}", _name, Environment.NewLine);
            r.AppendFormat("Number of Cores: {0}{1}", _coreCount, Environment.NewLine);
            r.AppendFormat("Threads per Core: {0}{1}", _cpuId[0].Length, Environment.NewLine);
            r.AppendLine(string.Format(CultureInfo.InvariantCulture, "Timer Frequency: {0} MHz", Stopwatch.Frequency * 1e-6));
            r.AppendLine("Time Stamp Counter: " + (HasTimeStampCounter ? _isInvariantTimeStampCounter ? "Invariant" : "Not Invariant" : "None"));
            r.AppendLine(string.Format(CultureInfo.InvariantCulture, "Estimated Time Stamp Counter Frequency: {0} MHz", Math.Round(_estimatedTimeStampCounterFrequency * 100) * 0.01));
            r.AppendLine(string.Format(CultureInfo.InvariantCulture,
                                       "Estimated Time Stamp Counter Frequency Error: {0} Mhz",
                                       Math.Round(_estimatedTimeStampCounterFrequency * _estimatedTimeStampCounterFrequencyError * 1e5) * 1e-5));

            r.AppendLine(string.Format(CultureInfo.InvariantCulture, "Time Stamp Counter Frequency: {0} MHz", Math.Round(TimeStampCounterFrequency * 100) * 0.01));
            r.AppendLine();

            uint[] msrArray = GetMsrs();
            if (msrArray != null && msrArray.Length > 0)
            {
                for (int i = 0; i < _cpuId.Length; i++)
                {
                    r.AppendLine("MSR Core #" + (i + 1));
                    r.AppendLine();
                    r.AppendLine(" MSR       EDX       EAX");
                    foreach (uint msr in msrArray)
                        AppendMsrData(r, msr, _cpuId[i][0].Affinity);

                    r.AppendLine();
                }
            }

            return r.ToString();
        }

        public override void Update()
        {
            if (HasTimeStampCounter && _isInvariantTimeStampCounter)
            {
                // make sure always the same thread is used
                GroupAffinity previousAffinity = ThreadAffinity.Set(_cpuId[0][0].Affinity);

                // read time before and after getting the TSC to estimate the error
                long firstTime = Stopwatch.GetTimestamp();
                ulong timeStampCount = OpCode.Rdtsc();
                long time = Stopwatch.GetTimestamp();

                // restore the thread affinity mask
                ThreadAffinity.Set(previousAffinity);

                double delta = (double)(time - _lastTime) / Stopwatch.Frequency;
                double error = (double)(time - firstTime) / Stopwatch.Frequency;

                // only use data if they are measured accurate enough (max 0.1ms delay)
                if (error < 0.0001)
                {
                    // ignore the first reading because there are no initial values
                    // ignore readings with too large or too small time window
                    if (_lastTime != 0 && delta > 0.5 && delta < 2)
                    {
                        // update the TSC frequency with the new value
                        TimeStampCounterFrequency = (timeStampCount - _lastTimeStampCount) / (1e6 * delta);
                    }

                    _lastTimeStampCount = timeStampCount;
                    _lastTime = time;
                }
            }

            if (_cpuLoad.IsAvailable)
            {
                _cpuLoad.Update();
                for (int i = 0; i < _coreLoads.Length; i++)
                    _coreLoads[i].Value = _cpuLoad.GetCoreLoad(i);

                if (_totalLoad != null)
                    _totalLoad.Value = _cpuLoad.GetTotalLoad();
            }
        }
    }
}
