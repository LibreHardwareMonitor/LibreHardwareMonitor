// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LibreHardwareMonitor.Hardware.Cpu;

/// <summary>
/// 
/// </summary>
/// <seealso cref="Hardware" />
public class GenericCpu : Hardware
{
    #region Fields

    private readonly CpuVendor _vendor;
    private readonly bool _isInvariantTimeStampCounter;
    private CpuLoad _cpuLoad;
    private double _estimatedTimeStampCounterFrequency;
    private double _estimatedTimeStampCounterFrequencyError;
    private Sensor[] _threadLoads;
    
    private Sensor _totalLoad;
    private Sensor _maxLoad;
    private long _lastTime;
    private ulong _lastTimeStampCount;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the CPUID.
    /// </summary>
    public CpuId[][] CpuId { get; }

    /// <summary>
    /// </summary>
    /// <inheritdoc />
    public override HardwareType HardwareType => HardwareType.Cpu;

    /// <summary>
    /// Gets a value indicating whether this instance has model specific registers.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance has model specific registers; otherwise, <c>false</c>.
    /// </value>
    public bool HasModelSpecificRegisters { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this instance has time stamp counter.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance has time stamp counter; otherwise, <c>false</c>.
    /// </value>
    public bool HasTimeStampCounter { get; private set; }

    /// <summary>
    /// Gets the CPU index.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the time stamp counter frequency.
    /// </summary>
    /// <value>
    /// The time stamp counter frequency.
    /// </value>
    public double TimeStampCounterFrequency { get; private set; }

    /// <summary>
    /// Gets the cpu0.
    /// </summary>
    /// <value>
    /// The cpu0.
    /// </value>
    protected CpuId Cpu0 { get; }

    /// <summary>
    /// Gets the core count.
    /// </summary>
    /// <value>
    /// The core count.
    /// </value>
    protected int CoreCount { get; }

    /// <summary>
    /// Gets the family.
    /// </summary>
    /// <value>
    /// The family.
    /// </value>
    protected uint Family { get; private set; }

    /// <summary>
    /// Gets the model.
    /// </summary>
    /// <value>
    /// The model.
    /// </value>
    protected uint Model { get; private set; }

    /// <summary>
    /// Gets the type of the package.
    /// </summary>
    /// <value>
    /// The type of the package.
    /// </value>
    protected uint PackageType { get; private set; }

    /// <summary>
    /// Gets the stepping.
    /// </summary>
    /// <value>
    /// The stepping.
    /// </value>
    protected uint Stepping { get; private set; }

    /// <summary>
    /// Gets the thread count.
    /// </summary>
    /// <value>
    /// The thread count.
    /// </value>
    protected int ThreadCount { get; }

    /// <summary>
    /// Gets the settings.
    /// </summary>
    /// <value>
    /// The settings.
    /// </value>
    protected internal ISettings Settings { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericCpu"/> class.
    /// </summary>
    /// <param name="processorIndex">Index of the processor.</param>
    /// <param name="cpuId">The cpu identifier.</param>
    /// <param name="settings">The settings.</param>
    public GenericCpu(int processorIndex, CpuId[][] cpuId, ISettings settings)
        : base(cpuId[0][0].Name, CreateIdentifier(cpuId[0][0].Vendor, processorIndex), settings)
    {
        CpuId = cpuId;
        CoreCount = cpuId.Length;
        ThreadCount = cpuId.Sum(x => x.Length);

        Cpu0 = cpuId[0][0];
        _vendor = Cpu0.Vendor;
        Family = Cpu0.Family;
        Model = Cpu0.Model;
        Stepping = Cpu0.Stepping;
        PackageType = Cpu0.PkgType;

        Index = processorIndex;
        Settings = settings;

        // CPU 0 Data
        uint[,] cpu0Data = Cpu0.Data;
        uint[,] cpu0ExtData = Cpu0.ExtData;

        // Check if processor has MSRs.
        HasModelSpecificRegisters = cpu0Data.GetLength(0) > 1 && (cpu0Data[1, 3] & 0x20) != 0;

        // Check if processor has a TSC.
        HasTimeStampCounter = cpu0Data.GetLength(0) > 1 && (cpu0Data[1, 3] & 0x10) != 0;

        // Check if processor supports an invariant TSC.
        _isInvariantTimeStampCounter = cpu0ExtData.GetLength(0) > 7 && (cpu0ExtData[7, 3] & 0x100) != 0;

    }

    #endregion

    #region Methods
    
    /// <summary>
    /// Prints the data to a report.
    /// </summary>
    /// <returns></returns>
    /// <inheritdoc />
    public override string GetReport()
    {
        StringBuilder r = new();
        switch (_vendor)
        {
            case CpuVendor.AMD:
                r.AppendLine("AMD CPU");
                break;
            case CpuVendor.Intel:
                r.AppendLine("Intel CPU");
                break;
            default:
                r.AppendLine("Generic CPU");
                break;
        }

        r.AppendLine();
        r.AppendFormat("Name: {0}{1}", _name, Environment.NewLine);
        r.AppendFormat("Number of Cores: {0}{1}", CoreCount, Environment.NewLine);
        r.AppendFormat("Threads per Core: {0}{1}", CpuId[0].Length, Environment.NewLine);
        r.AppendLine(string.Format(CultureInfo.InvariantCulture, "Timer Frequency: {0} MHz", Stopwatch.Frequency * 1e-6));
        r.AppendLine("Time Stamp Counter: " + (HasTimeStampCounter ? _isInvariantTimeStampCounter ? "Invariant" : "Not Invariant" : "None"));
        r.AppendLine(string.Format(CultureInfo.InvariantCulture, "Estimated Time Stamp Counter Frequency: {0} MHz", Math.Round(_estimatedTimeStampCounterFrequency * 100) * 0.01));
        r.AppendLine(string.Format(CultureInfo.InvariantCulture,
                                   "Estimated Time Stamp Counter Frequency Error: {0} Mhz",
                                   Math.Round(_estimatedTimeStampCounterFrequency * _estimatedTimeStampCounterFrequencyError * 1e5) * 1e-5));

        r.AppendLine(string.Format(CultureInfo.InvariantCulture, "Time Stamp Counter Frequency: {0} MHz", Math.Round(TimeStampCounterFrequency * 100) * 0.01));
        r.AppendLine();

        uint[] msrArray = GetMsrs();
        if (msrArray is not { Length: > 0 }) return r.ToString();
        for (int i = 0; i < CpuId.Length; i++)
        {
            r.AppendLine("MSR Core #" + (i + 1));
            r.AppendLine();
            r.AppendLine(" MSR       EDX       EAX");
            foreach (uint msr in msrArray)
            {
                GetMsrReportData(r, msr, CpuId[i][0].Affinity);
            }
            r.AppendLine();
        }
        return r.ToString();
    }

    /// <summary>
    /// Updates all sensors.
    /// </summary>
    /// <inheritdoc />
    public override void Update()
    {
        if (HasTimeStampCounter && _isInvariantTimeStampCounter)
        {
            // make sure always the same thread is used
            GroupAffinity previousAffinity = ThreadAffinity.Set(Cpu0.Affinity);

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
                if (_lastTime != 0 && delta is > 0.5 and < 2)
                {
                    // update the TSC frequency with the new value
                    TimeStampCounterFrequency = (timeStampCount - _lastTimeStampCount) / (1e6 * delta);
                }

                _lastTimeStampCount = timeStampCount;
                _lastTime = time;
            }
        }

        // Update CPU load
        if (!_cpuLoad.IsAvailable) return;
        _cpuLoad.Update();

        // Assess loads
        float maxLoad = 0;
        if (_threadLoads != null)
        {
            for (int i = 0; i < _threadLoads.Length; i++)
            {
                if (_threadLoads[i] == null) continue;
                _threadLoads[i].Value = _cpuLoad.GetThreadLoad(i);
                maxLoad = Math.Max(maxLoad, _threadLoads[i].Value ?? 0);
            }
        }

        if (_totalLoad != null)
        {
            _totalLoad.Value = _cpuLoad.GetTotalLoad();
        }

        if (_maxLoad != null)
        {
            _maxLoad.Value = maxLoad;
        }
    }

    /// <summary>
    /// Gets the MSRS.
    /// </summary>
    /// <returns></returns>
    protected virtual uint[] GetMsrs() => null;

    /// <summary>
    /// Cores the string.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns></returns>
    protected virtual string SetCoreName(int index)
        => CoreCount == 1 ? "CPU Core" : "CPU Core #" + (index + 1);

    /// <summary>
    /// Initializes all base objects.
    /// </summary>
    protected void Initialize()
    {
        // Set CPU Loads
        SetLoadSensors();

        // TimeStamp Counter
        SetTimeStampCounter();
    }

    /// <summary>
    /// Creates the identifier.
    /// </summary>
    /// <param name="vendor">The vendor.</param>
    /// <param name="processorIndex">Index of the processor.</param>
    /// <returns></returns>
    private static Identifier CreateIdentifier(CpuVendor vendor, int processorIndex)
    {
        string s = vendor switch
        {
            CpuVendor.AMD => "amdcpu",
            CpuVendor.Intel => "intelcpu",
            _ => "genericcpu"
        };

        return new Identifier(s, processorIndex.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Sets the loads.
    /// </summary>
    private void SetLoadSensors()
    {
        // Get CPU Load
        _cpuLoad = new CpuLoad(CpuId);
        if (!_cpuLoad.IsAvailable) return;

        // Threads
        _threadLoads = new Sensor[ThreadCount];
        for (int coreIndex = 0; coreIndex < CpuId.Length; coreIndex++)
        {
            for (int threadIndex = 0; threadIndex < CpuId[coreIndex].Length; threadIndex++)
            {
                int thread = CpuId[coreIndex][threadIndex].Thread;
                if (thread >= _threadLoads.Length) continue;

                // Some cores may have 2 threads while others have only one (e.g. P-cores vs E-cores on Intel 12th gen).
                string sensorName = SetCoreName(coreIndex) + (CpuId[coreIndex].Length > 1 ? $", Thread #{threadIndex}" : string.Empty);
                _threadLoads[thread] = new Sensor(sensorName, thread + 2, SensorType.Load, this, Settings);
                ActivateSensor(_threadLoads[thread]);
            }
        }

        // Total
        _totalLoad = CoreCount > 1 ? new Sensor("CPU Total", 0, SensorType.Load, this, Settings) : null;
        if (_totalLoad is not null)
        {
            ActivateSensor(_totalLoad);
        }

        // Max
        _maxLoad = CoreCount > 1 ? new Sensor("CPU Core Max", 1, SensorType.Load, this, Settings) : null;
        if (_maxLoad is not null)
        {
            ActivateSensor(_maxLoad);
        }
    }

    /// <summary>
    /// Sets the time stamp counter.
    /// </summary>
    private void SetTimeStampCounter()
    {
        if (HasTimeStampCounter)
        {
            GroupAffinity previousAffinity = ThreadAffinity.Set(Cpu0.Affinity);
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
    /// Estimates the time stamp counter frequency.
    /// </summary>
    /// <param name="frequency">The frequency.</param>
    /// <param name="error">The error.</param>
    private static void EstimateTimeStampCounterFrequency(out double frequency, out double error)
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

            if (error < 1e-4) break;
        }
    }

    /// <summary>
    /// Estimates the time stamp counter frequency.
    /// </summary>
    /// <param name="timeWindow">The time window.</param>
    /// <param name="frequency">The frequency.</param>
    /// <param name="error">The error.</param>
    private static void EstimateTimeStampCounterFrequency(double timeWindow, out double frequency, out double error)
    {
        long ticks = (long)(timeWindow * Stopwatch.Frequency);
        long timeBegin = Stopwatch.GetTimestamp() + (long)Math.Ceiling(0.001 * ticks);
        long timeEnd = timeBegin + ticks;

        // Wait
        while (Stopwatch.GetTimestamp() < timeBegin) { }
        while (Stopwatch.GetTimestamp() < timeEnd) { }

        // Frequency
        double delta = timeEnd - timeBegin;
        ulong countBegin = OpCode.Rdtsc();
        long afterBegin = Stopwatch.GetTimestamp();
        ulong countEnd = OpCode.Rdtsc();
        long afterEnd = Stopwatch.GetTimestamp();
        frequency = 1e-6 * ((double)(countEnd - countBegin) * Stopwatch.Frequency) / delta;

        // Errors
        double beginError = (afterBegin - timeBegin) / delta;
        double endError = (afterEnd - timeEnd) / delta;
        error = beginError + endError;
    }

    /// <summary>
    /// Appends the MSR data.
    /// </summary>
    /// <param name="r">The r.</param>
    /// <param name="msr">The MSR.</param>
    /// <param name="affinity">The affinity.</param>
    /// <returns></returns>
    private static void GetMsrReportData(StringBuilder r, uint msr, GroupAffinity affinity)
    {
        if (!Ring0.ReadMsr(msr, out uint eax, out uint edx, affinity)) return;

        r.Append(" ");
        r.Append(msr.ToString("X8", CultureInfo.InvariantCulture));
        r.Append("  ");
        r.Append(edx.ToString("X8", CultureInfo.InvariantCulture));
        r.Append("  ");
        r.Append(eax.ToString("X8", CultureInfo.InvariantCulture));
        r.AppendLine();
    }

    #endregion
}
