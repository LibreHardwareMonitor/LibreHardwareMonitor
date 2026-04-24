// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace LibreHardwareMonitor.Hardware.Cpu;

/// <summary>
/// Qualcomm Snapdragon CPU implementation for ARM-based Windows devices.
/// ARM processors do not support x86 CPUID/MSR/TSC, so hardware information is
/// sourced from Windows APIs: registry (CPU name), kernel topology (core count),
/// CallNtPowerInformation (clock speeds), and SetupAPI/DeviceIoControl ACPI thermal zones (temperatures).
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
    private readonly string[] _thermalZoneDevicePaths;
    private readonly int _processorIndex;

    // For CPU load tracking
    private long[] _idleTimes;
    private long[] _totalTimes;

    public QualcommCpu(int processorIndex, ISettings settings)
        : base("Qualcomm Snapdragon", new Identifier("qualcommcpu", processorIndex.ToString(CultureInfo.InvariantCulture)), settings)
    {
        _processorIndex = processorIndex;

        // ARM processors don't expose a CPUID brand string via the CPUID instruction.
        // Windows reads the name from ACPI/firmware at boot and writes it to the registry,
        // making this the standard way to obtain the CPU name on ARM Windows.
        string cpuName = ReadCpuNameFromRegistry();

        // Environment.ProcessorCount returns logical processors (threads), which equals
        // physical cores on Qualcomm's current designs but could differ. Use the kernel
        // topology API to get the true physical core count for sensor naming/sizing.
        _threadCount = Environment.ProcessorCount;
        _coreCount = GetPhysicalCoreCount(_threadCount);

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

        // ACPI thermal zone sensors — discovered via SetupAPI device interface enumeration.
        // DeviceIoControl(IOCTL_THERMAL_QUERY_INFORMATION) reads each zone directly without
        // going through WMI's COM infrastructure.
        var thermalSensors = new List<Sensor>();
        var devicePaths = new List<string>();

        try
        {
            int idx = 0;
            foreach (string devicePath in EnumerateThermalZoneDevicePaths())
            {
                // Only register zones that already have a valid reading at startup.
                // Zones that return no data or out-of-range values are ACPI placeholders.
                if (!TryReadThermalZoneTemperature(devicePath, out float tempCelsius))
                    continue;

                if (tempCelsius is <= -40 or >= 150)
                    continue;

                string displayName = FormatThermalZoneDeviceName(devicePath, idx);

                var sensor = new Sensor(displayName, idx, SensorType.Temperature, this, settings);
                thermalSensors.Add(sensor);
                devicePaths.Add(devicePath);
                ActivateSensor(sensor);
                idx++;
            }
        }
        catch
        {
            // Thermal zone enumeration may fail if not supported on this platform
        }

        _thermalZones = thermalSensors.ToArray();
        _thermalZoneDevicePaths = devicePaths.ToArray();
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
        for (int i = 0; i < _thermalZoneDevicePaths.Length; i++)
        {
            if (TryReadThermalZoneTemperature(_thermalZoneDevicePaths[i], out float tempCelsius))
                _thermalZones[i].Value = tempCelsius is > -40 and < 150 ? tempCelsius : (float?)null;
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

        for (int i = 0; i < _thermalZoneDevicePaths.Length; i++)
        {
            r.AppendFormat("  Thermal Zone {0}: {1}{2}", i, _thermalZoneDevicePaths[i], Environment.NewLine);
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

    private static string FormatThermalZoneDeviceName(string devicePath, int index)
    {
        // Device paths look like: \\?\ACPI#ThermalZone#THM0_0#{4afa3d52-...}
        // Extract the last #-segment before the GUID suffix for a readable name.
        try
        {
            int guidStart = devicePath.LastIndexOf('{');
            string withoutGuid = guidStart > 0 ? devicePath.Substring(0, guidStart - 1) : devicePath;
            int lastHash = withoutGuid.LastIndexOf('#');
            if (lastHash >= 0)
            {
                string segment = withoutGuid.Substring(lastHash + 1);
                // Remove trailing numeric instance suffix (_0, _1 etc.)
                int underscoreIdx = segment.LastIndexOf('_');
                if (underscoreIdx > 0 && int.TryParse(segment.Substring(underscoreIdx + 1), out _))
                    segment = segment.Substring(0, underscoreIdx);
                if (!string.IsNullOrWhiteSpace(segment))
                    return $"Thermal Zone ({segment})";
            }
        }
        catch { }

        return $"Thermal Zone {index}";
    }

    // Enumerates \\?\... device paths for all ACPI thermal zones via SetupAPI.
    private static List<string> EnumerateThermalZoneDevicePaths()
    {
        var paths = new List<string>();
        Guid thermalGuid = ThermalZoneInterfaceGuid;
        const uint DiGcfPresent = 0x00000002;
        const uint DiGcfDeviceInterface = 0x00000010;

        IntPtr devInfoSet = SetupDiGetClassDevs(ref thermalGuid, IntPtr.Zero, IntPtr.Zero, DiGcfPresent | DiGcfDeviceInterface);
        if (devInfoSet.ToInt64() == -1)
            return paths;

        try
        {
            SP_DEVICE_INTERFACE_DATA ifaceData = default;
            ifaceData.CbSize = (uint)Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA>();

            for (uint i = 0; SetupDiEnumDeviceInterfaces(devInfoSet, IntPtr.Zero, ref thermalGuid, i, ref ifaceData); i++)
            {
                // First call with zero buffer to obtain the required size.
                SetupDiGetDeviceInterfaceDetail(devInfoSet, ref ifaceData, IntPtr.Zero, 0, out uint requiredSize, IntPtr.Zero);
                if (requiredSize == 0)
                    continue;

                IntPtr detailBuf = Marshal.AllocHGlobal((int)requiredSize);
                try
                {
                    // SP_DEVICE_INTERFACE_DETAIL_DATA cbSize: 8 on 64-bit, 6 on 32-bit.
                    Marshal.WriteInt32(detailBuf, IntPtr.Size == 8 ? 8 : 6);

                    if (SetupDiGetDeviceInterfaceDetail(devInfoSet, ref ifaceData, detailBuf, requiredSize, out _, IntPtr.Zero))
                    {
                        // DevicePath is a Unicode string starting at offset +4 (after cbSize DWORD).
                        string path = Marshal.PtrToStringUni(IntPtr.Add(detailBuf, 4));
                        if (!string.IsNullOrEmpty(path))
                            paths.Add(path);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(detailBuf);
                }
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(devInfoSet);
        }

        return paths;
    }

    // Opens a thermal zone device and reads its current temperature via IOCTL_THERMAL_QUERY_INFORMATION.
    private static bool TryReadThermalZoneTemperature(string devicePath, out float tempCelsius)
    {
        tempCelsius = 0;
        const uint GenericRead = 0x80000000;
        const uint FileShareReadWrite = 0x00000003;
        const uint OpenExisting = 3;

        IntPtr handle = CreateFile(devicePath, GenericRead, FileShareReadWrite, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
        if (handle.ToInt64() == -1)
            return false;

        try
        {
            THERMAL_WAIT_READ waitRead = new THERMAL_WAIT_READ
            {
                Timeout = 0,           // return immediately, no threshold wait
                LowTemperature = 0,
                HighTemperature = uint.MaxValue
            };

            if (!ThermalDeviceIoControl(handle, IoctlThermalQueryInformation,
                ref waitRead, (uint)Marshal.SizeOf<THERMAL_WAIT_READ>(),
                out THERMAL_INFORMATION info, (uint)Marshal.SizeOf<THERMAL_INFORMATION>(),
                out _, IntPtr.Zero))
                return false;

            // CurrentTemperature is in tenths of Kelvin
            tempCelsius = (info.CurrentTemperature / 10.0f) - 273.15f;
            return true;
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    #region CPU Info Helpers

    // ARM processors don't expose a CPUID brand string. Windows sources the name from
    // ACPI/firmware at boot and writes it to HKLM\HARDWARE\DESCRIPTION\System\CentralProcessor\0\ProcessorNameString —
    // the ARM equivalent of x86 CPUID leaves 0x80000002-0x80000004.
    private static string ReadCpuNameFromRegistry()
    {
        try
        {
            using RegistryKey key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            if (key?.GetValue("ProcessorNameString") is string name && !string.IsNullOrWhiteSpace(name))
                return name.Trim();
        }
        catch
        {
            // Fall through to default
        }

        return "Qualcomm Snapdragon";
    }

    // Environment.ProcessorCount returns logical threads, not physical cores. Use the
    // kernel topology API so sensor count is correct on big.LITTLE Snapdragon designs.
    private static int GetPhysicalCoreCount(int fallback)
    {
        // GetLogicalProcessorInformationEx with RelationProcessorCore returns one
        // SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX entry per physical core.
        uint bufferSize = 0;
        GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore, IntPtr.Zero, ref bufferSize);

        if (bufferSize == 0)
            return fallback;

        IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
        try
        {
            if (!GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore, buffer, ref bufferSize))
                return fallback;

            int coreCount = 0;
            int offset = 0;
            while (offset < (int)bufferSize)
            {
                // SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX starts with Relationship (DWORD) + Size (DWORD)
                int size = Marshal.ReadInt32(buffer, offset + 4);
                if (size <= 0)
                    break;

                coreCount++;
                offset += size;
            }

            return coreCount > 0 ? coreCount : fallback;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetLogicalProcessorInformationEx(
        LOGICAL_PROCESSOR_RELATIONSHIP relationshipType,
        IntPtr buffer,
        ref uint returnedLength);

    private enum LOGICAL_PROCESSOR_RELATIONSHIP
    {
        RelationProcessorCore = 0,
    }

    #endregion

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

    // Thermal zone GUID from poclass.h: {4AFA3D52-74A7-11d0-be5e-00A0C9062910}
    private static readonly Guid ThermalZoneInterfaceGuid = new Guid(0x4AFA3D52, 0x74A7, 0x11d0, 0xbe, 0x5e, 0x00, 0xA0, 0xC9, 0x06, 0x29, 0x10);

    // CTL_CODE(FILE_DEVICE_BATTERY=0x29, 0x12, METHOD_BUFFERED, FILE_READ_ACCESS) = 0x294048
    private const uint IoctlThermalQueryInformation = 0x294048;

    [StructLayout(LayoutKind.Sequential)]
    private struct THERMAL_WAIT_READ
    {
        public uint Timeout;         // milliseconds; 0 = return immediately
        public uint LowTemperature;  // tenths of Kelvin lower threshold
        public uint HighTemperature; // tenths of Kelvin upper threshold
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct THERMAL_INFORMATION
    {
        public uint ThermalStandardInformation;
        public ulong SamplingPeriod;
        public ulong CurrentTemperature; // tenths of Kelvin
        public ulong MyFaultThreshold;
        public ulong ThrottleThreshold;
        public ulong LimitInterface;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SP_DEVICE_INTERFACE_DATA
    {
        public uint CbSize;
        public Guid InterfaceClassGuid;
        public uint Flags;
        public IntPtr Reserved;
    }

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, IntPtr enumerator, IntPtr hwndParent, uint flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiEnumDeviceInterfaces(IntPtr deviceInfoSet, IntPtr deviceInfoData, ref Guid interfaceClassGuid, uint memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr deviceInfoSet, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, IntPtr deviceInterfaceDetailData, uint deviceInterfaceDetailDataSize, out uint requiredSize, IntPtr deviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "DeviceIoControl")]
    private static extern bool ThermalDeviceIoControl(IntPtr hDevice, uint dwIoControlCode, ref THERMAL_WAIT_READ lpInBuffer, uint nInBufferSize, out THERMAL_INFORMATION lpOutBuffer, uint nOutBufferSize, out uint lpBytesReturned, IntPtr lpOverlapped);

    #endregion
}
