// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;

namespace LibreHardwareMonitor.Hardware.Cpu;

/// <summary>
/// Qualcomm Snapdragon CPU implementation for ARM-based Windows devices.
/// Since x86 CPUID/MSR/TSC instructions are not available on ARM,
/// this class uses WMI (ACPI thermal zones) and Windows power APIs
/// to provide temperature, clock speed, and load sensors.
/// </summary>
internal class QualcommCpu : Hardware
{
    private readonly int _coreCount;
    private readonly int _threadCount;
    private readonly Sensor _totalLoad;
    private readonly Sensor _maxLoad;
    private readonly Sensor[] _coreClocks;
    private readonly Sensor[] _coreLoads;
    private readonly Sensor[] _thermalZones;
    private readonly string[] _thermalZoneInstanceNames;
    private readonly int _processorIndex;

    // For CPU load tracking
    private long[] _idleTimes;
    private long[] _totalTimes;

    public QualcommCpu(int processorIndex, ISettings settings)
        : base("Qualcomm Snapdragon", new Identifier("qualcommcpu", processorIndex.ToString(CultureInfo.InvariantCulture)), settings)
    {
        _processorIndex = processorIndex;

        // Detect CPU name and core/thread counts from WMI
        string cpuName = "Qualcomm Snapdragon";
        _coreCount = Environment.ProcessorCount;
        _threadCount = Environment.ProcessorCount;

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                string name = obj["Name"]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(name))
                    cpuName = name;

                if (obj["NumberOfCores"] is uint cores)
                    _coreCount = (int)cores;

                if (obj["NumberOfLogicalProcessors"] is uint threads)
                    _threadCount = (int)threads;

                break; // Use first processor
            }
        }
        catch
        {
            // Fall back to Environment.ProcessorCount
        }

        Name = cpuName;

        // Load sensors
        _totalLoad = new Sensor("CPU Total", 0, SensorType.Load, this, settings);
        ActivateSensor(_totalLoad);

        _maxLoad = _coreCount > 1 ? new Sensor("CPU Core Max", 1, SensorType.Load, this, settings) : null;
        if (_maxLoad != null)
            ActivateSensor(_maxLoad);

        _coreLoads = new Sensor[_threadCount];
        for (int i = 0; i < _threadCount; i++)
        {
            _coreLoads[i] = new Sensor(CoreString(i), i + 2, SensorType.Load, this, settings);
            ActivateSensor(_coreLoads[i]);
        }

        // Initialize load tracking
        try
        {
            GetProcessorTimes(out _idleTimes, out _totalTimes);
        }
        catch
        {
            _idleTimes = null;
            _totalTimes = null;
        }

        // Clock speed sensors — one per core
        _coreClocks = new Sensor[_coreCount];
        for (int i = 0; i < _coreCount; i++)
        {
            _coreClocks[i] = new Sensor(CoreString(i), i, SensorType.Clock, this, settings);
            ActivateSensor(_coreClocks[i]);
        }

        // ACPI thermal zone sensors — discovered via WMI
        var thermalSensors = new List<Sensor>();
        var instanceNames = new List<string>();

        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\WMI",
                "SELECT InstanceName, CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");

            int idx = 0;
            foreach (ManagementObject obj in searcher.Get())
            {
                string instanceName = obj["InstanceName"]?.ToString() ?? $"Thermal Zone {idx}";

                // Only register zones that already have a valid reading at startup.
                // Zones that return no data (null CurrentTemperature or out-of-range
                // Kelvin values) are ACPI placeholders that never provide useful data.
                if (!(obj["CurrentTemperature"] is uint tempRaw))
                    continue;

                float tempCelsius = (tempRaw / 10.0f) - 273.15f;
                if (tempCelsius is <= -40 or >= 150)
                    continue;

                string displayName = FormatThermalZoneName(instanceName, idx);

                var sensor = new Sensor(displayName, idx, SensorType.Temperature, this, settings);
                thermalSensors.Add(sensor);
                instanceNames.Add(instanceName);
                ActivateSensor(sensor);
                idx++;
            }
        }
        catch
        {
            // WMI thermal zone query may fail if not running as admin or if not supported
        }

        _thermalZones = thermalSensors.ToArray();
        _thermalZoneInstanceNames = instanceNames.ToArray();
    }

    public override HardwareType HardwareType => HardwareType.Cpu;

    public override void Update()
    {
        UpdateLoads();
        UpdateClocks();
        UpdateTemperatures();
    }

    private void UpdateLoads()
    {
        if (_idleTimes == null || !GetProcessorTimes(out long[] newIdleTimes, out long[] newTotalTimes))
            return;

        // Require minimum time diff to avoid division by zero
        for (int i = 0; i < Math.Min(newTotalTimes.Length, _totalTimes.Length); i++)
        {
            if (newTotalTimes[i] - _totalTimes[i] < 100000)
                return;
        }

        double totalLoadSum = 0;
        int count = 0;
        float maxLoad = 0;

        for (int i = 0; i < _coreLoads.Length && i < _idleTimes.Length && i < newIdleTimes.Length; i++)
        {
            double idle = (newIdleTimes[i] - _idleTimes[i]) / (double)(newTotalTimes[i] - _totalTimes[i]);
            idle = Math.Max(0, Math.Min(1, idle));

            float load = (float)Math.Round(100.0 * (1.0 - idle), 2);
            _coreLoads[i].Value = load;
            maxLoad = Math.Max(maxLoad, load);
            totalLoadSum += idle;
            count++;
        }

        if (count > 0)
        {
            double total = 1.0 - (totalLoadSum / count);
            total = Math.Max(0, Math.Min(1, total));
            _totalLoad.Value = (float)Math.Round(total * 100.0, 2);
        }

        if (_maxLoad != null)
            _maxLoad.Value = maxLoad;

        _totalTimes = newTotalTimes;
        _idleTimes = newIdleTimes;
    }

    private void UpdateClocks()
    {
        try
        {
            // Use CallNtPowerInformation to get per-processor MHz on ARM
            int infoSize = Marshal.SizeOf<PROCESSOR_POWER_INFORMATION>();
            int bufferSize = infoSize * _coreCount;
            IntPtr buffer = Marshal.AllocHGlobal(bufferSize);

            try
            {
                int status = CallNtPowerInformation(
                    POWER_INFORMATION_LEVEL.ProcessorInformation,
                    IntPtr.Zero, 0,
                    buffer, (uint)bufferSize);

                if (status == 0) // STATUS_SUCCESS
                {
                    for (int i = 0; i < _coreCount; i++)
                    {
                        var info = Marshal.PtrToStructure<PROCESSOR_POWER_INFORMATION>(
                            IntPtr.Add(buffer, i * infoSize));

                        _coreClocks[i].Value = info.CurrentMhz;
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        catch
        {
            // Clock reading failed — leave values as-is
        }
    }

    private void UpdateTemperatures()
    {
        if (_thermalZones.Length == 0)
            return;

        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\WMI",
                "SELECT InstanceName, CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");

            foreach (ManagementObject obj in searcher.Get())
            {
                string instanceName = obj["InstanceName"]?.ToString();
                if (instanceName == null)
                    continue;

                for (int i = 0; i < _thermalZoneInstanceNames.Length; i++)
                {
                    if (_thermalZoneInstanceNames[i] == instanceName)
                    {
                        // ACPI thermal zones return temperature in tenths of Kelvin
                        if (obj["CurrentTemperature"] is uint tempRaw)
                        {
                            float tempCelsius = (tempRaw / 10.0f) - 273.15f;

                            // Sanity check: ignore obviously wrong readings
                            if (tempCelsius is > -40 and < 150)
                            {
                                _thermalZones[i].Value = tempCelsius;
                            }
                            else
                            {
                                _thermalZones[i].Value = null;
                            }
                        }

                        break;
                    }
                }
            }
        }
        catch
        {
            // WMI query failed — leave temperature values as-is
        }
    }

    public override string GetReport()
    {
        StringBuilder r = new();

        r.AppendLine("Qualcomm CPU");
        r.AppendLine();
        r.AppendFormat("Name: {0}{1}", Name, Environment.NewLine);
        r.AppendFormat("Number of Cores: {0}{1}", _coreCount, Environment.NewLine);
        r.AppendFormat("Number of Threads: {0}{1}", _threadCount, Environment.NewLine);
        r.AppendFormat("Architecture: {0}{1}", RuntimeInformation.ProcessArchitecture, Environment.NewLine);
        r.AppendFormat("ACPI Thermal Zones: {0}{1}", _thermalZones.Length, Environment.NewLine);
        r.AppendLine();

        for (int i = 0; i < _thermalZoneInstanceNames.Length; i++)
        {
            r.AppendFormat("  Thermal Zone {0}: {1}{2}", i, _thermalZoneInstanceNames[i], Environment.NewLine);
        }

        r.AppendLine();
        return r.ToString();
    }

    private string CoreString(int i)
    {
        if (_coreCount == 1)
            return "CPU Core";

        return "CPU Core #" + (i + 1);
    }

    private static string FormatThermalZoneName(string instanceName, int index)
    {
        // ACPI instance names are typically like "ACPI\ThermalZone\THM0_0"
        // Try to extract a readable name
        if (instanceName.Contains("\\"))
        {
            string[] parts = instanceName.Split('\\');
            string lastPart = parts[parts.Length - 1];

            // Remove trailing _0, _1 etc.
            int underscoreIdx = lastPart.LastIndexOf('_');
            if (underscoreIdx > 0)
                lastPart = lastPart.Substring(0, underscoreIdx);

            if (!string.IsNullOrWhiteSpace(lastPart))
                return $"Thermal Zone ({lastPart})";
        }

        return $"Thermal Zone {index}";
    }

    #region Native Interop

    private static unsafe bool GetProcessorTimes(out long[] idle, out long[] total)
    {
        idle = null;
        total = null;

        int structSize = sizeof(SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION);
        int size = structSize * 256;
        uint returnSize = 0;
        IntPtr handle = Marshal.AllocHGlobal(size);

        try
        {
            while (true)
            {
                int status = NtQuerySystemInformation(8, handle, (uint)size, ref returnSize);
                if (status == unchecked((int)0xC0000004)) // STATUS_INFO_LENGTH_MISMATCH
                {
                    size = (int)returnSize;
                    handle = Marshal.ReAllocHGlobal(handle, new IntPtr(size));
                }
                else if (status == 0) // STATUS_SUCCESS
                {
                    int count = (int)(returnSize / structSize);
                    idle = new long[count];
                    total = new long[count];

                    for (int i = 0; i < count; i++)
                    {
                        var info = Marshal.PtrToStructure<SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION>(
                            IntPtr.Add(handle, i * structSize));

                        idle[i] = info.IdleTime;
                        total[i] = info.KernelTime + info.UserTime;
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(handle);
        }
    }

    [DllImport("ntdll.dll")]
    private static extern int NtQuerySystemInformation(int systemInformationClass, IntPtr systemInformation, uint systemInformationLength, ref uint returnLength);

    [DllImport("powrprof.dll")]
    private static extern int CallNtPowerInformation(
        POWER_INFORMATION_LEVEL informationLevel,
        IntPtr inputBuffer,
        uint inputBufferLength,
        IntPtr outputBuffer,
        uint outputBufferLength);

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION
    {
        public long IdleTime;
        public long KernelTime;
        public long UserTime;
        public long DpcTime;
        public long InterruptTime;
        public uint InterruptCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESSOR_POWER_INFORMATION
    {
        public uint Number;
        public uint MaxMhz;
        public uint CurrentMhz;
        public uint MhzLimit;
        public uint MaxIdleState;
        public uint CurrentIdleState;
    }

    private enum POWER_INFORMATION_LEVEL
    {
        ProcessorInformation = 11
    }

    #endregion
}
