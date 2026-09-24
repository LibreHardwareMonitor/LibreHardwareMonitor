// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management;

namespace LibreHardwareMonitor.Hardware.Storage.StorageSpaces;

/// <summary>
/// A point-in-time read of the Storage Spaces WMI classes, shared by the pool hardware and by
/// <see cref="StorageDevice" /> so that both read one snapshot instead of querying independently.
/// </summary>
internal sealed class StorageSpacesSnapshot
{
    private const string Scope = @"root\Microsoft\Windows\Storage";

    private StorageSpacesSnapshot(List<StorageSpacesPoolInfo> pools, bool repairInProgress, float? repairPercentComplete)
    {
        Pools = pools;
        RepairInProgress = repairInProgress;
        RepairPercentComplete = repairPercentComplete;
    }

    public IReadOnlyList<StorageSpacesPoolInfo> Pools { get; }

    /// <summary>
    /// Storage jobs are not attributed to a specific pool, so repair state is snapshot-wide.
    /// </summary>
    public bool RepairInProgress { get; }

    public float? RepairPercentComplete { get; }

    /// <summary>
    /// Reads the current state, or returns <see langword="null" /> if Storage Spaces is unavailable.
    /// </summary>
    public static StorageSpacesSnapshot Create()
    {
        if (Software.OperatingSystem.IsUnix)
            return null;

        try
        {
            var pools = new List<StorageSpacesPoolInfo>();

            using (var searcher = new ManagementObjectSearcher(Scope, "SELECT * FROM MSFT_StoragePool WHERE IsPrimordial = FALSE"))
            using (ManagementObjectCollection collection = searcher.Get())
            {
                foreach (ManagementBaseObject item in collection)
                {
                    using var pool = (ManagementObject)item;
                    pools.Add(ReadPool(pool));
                }
            }

            if (pools.Count == 0)
                return null;

            ReadJobs(out bool repairInProgress, out float? percentComplete);

            return new StorageSpacesSnapshot(pools, repairInProgress, percentComplete);
        }
        catch (ManagementException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static StorageSpacesPoolInfo ReadPool(ManagementObject pool)
    {
        var info = new StorageSpacesPoolInfo
        {
            Name = GetString(pool, "FriendlyName"),
            UniqueId = GetString(pool, "UniqueId"),
            HealthStatus = GetInt32(pool, "HealthStatus"),
            OperationalStatus = GetFirstInt32(pool, "OperationalStatus"),
            Size = GetUInt64(pool, "Size"),
            AllocatedSize = GetUInt64(pool, "AllocatedSize"),
            IsReadOnly = GetBoolean(pool, "IsReadOnly"),
            RepairPolicy = GetInt32(pool, "RepairPolicy"),
            DefaultResiliency = GetString(pool, "ResiliencySettingNameDefault")
        };

        foreach (ManagementObject virtualDisk in GetRelated(pool, "MSFT_VirtualDisk"))
        {
            using (virtualDisk)
                info.VirtualDisks.Add(ReadVirtualDisk(virtualDisk));
        }

        // Pooled disks are owned by spaceport.sys and are absent from MSFT_Disk and Win32_DiskDrive,
        // so this association is the only way to see them. StoragePoolUniqueId is empty on every
        // disk and must not be used to filter.
        foreach (ManagementObject physicalDisk in GetRelated(pool, "MSFT_PhysicalDisk"))
        {
            using (physicalDisk)
                info.PhysicalDisks.Add(ReadPhysicalDisk(physicalDisk));
        }

        return info;
    }

    private static StorageSpacesVirtualDiskInfo ReadVirtualDisk(ManagementObject virtualDisk)
    {
        var info = new StorageSpacesVirtualDiskInfo
        {
            Name = GetString(virtualDisk, "FriendlyName"),
            HealthStatus = GetInt32(virtualDisk, "HealthStatus"),
            OperationalStatus = GetFirstInt32(virtualDisk, "OperationalStatus"),
            Size = GetUInt64(virtualDisk, "Size"),
            AllocatedSize = GetUInt64(virtualDisk, "AllocatedSize"),
            FootprintOnPool = GetUInt64(virtualDisk, "FootprintOnPool"),
            Resiliency = GetString(virtualDisk, "ResiliencySettingName"),
            NumberOfDataCopies = GetInt32(virtualDisk, "NumberOfDataCopies"),
            NumberOfColumns = GetInt32(virtualDisk, "NumberOfColumns")
        };

        // MSFT_Disk.Path is a \\?\storage#disk#... string, so the device number is the only way to
        // tie the virtual disk back to the disk LHM already enumerates.
        foreach (ManagementObject disk in GetRelated(virtualDisk, "MSFT_Disk"))
        {
            using (disk)
                info.DeviceNumber ??= GetInt32(disk, "Number");
        }

        return info;
    }

    private static StorageSpacesPhysicalDiskInfo ReadPhysicalDisk(ManagementObject physicalDisk)
    {
        var info = new StorageSpacesPhysicalDiskInfo
        {
            ObjectId = GetString(physicalDisk, "ObjectId"),
            DeviceId = GetString(physicalDisk, "DeviceId"),
            Name = GetString(physicalDisk, "FriendlyName"),
            Model = GetString(physicalDisk, "Model"),
            SerialNumber = GetString(physicalDisk, "SerialNumber"),
            MediaType = GetInt32(physicalDisk, "MediaType"),
            HealthStatus = GetInt32(physicalDisk, "HealthStatus"),
            OperationalStatus = GetFirstInt32(physicalDisk, "OperationalStatus"),
            Size = GetUInt64(physicalDisk, "Size"),
            AllocatedSize = GetUInt64(physicalDisk, "AllocatedSize")
        };

        ReadReliabilityCounter(physicalDisk, info);
        return info;
    }

    /// <summary>
    /// MSFT_StorageReliabilityCounter has no instances when queried directly, so it has to be
    /// reached through the association. It also requires elevation, unlike every other class here.
    /// </summary>
    private static void ReadReliabilityCounter(ManagementObject physicalDisk, StorageSpacesPhysicalDiskInfo info)
    {
        foreach (ManagementObject counter in GetRelated(physicalDisk, "MSFT_StorageReliabilityCounter"))
        {
            using (counter)
            {
                // Temperature reads 0 on drives that do not report it; TemperatureMax is 0 even on
                // drives that do, and ManufactureDate and the WriteErrors* counters are always null.
                info.Temperature = GetInt32(counter, "Temperature");
                info.Wear = GetInt32(counter, "Wear");
                info.PowerOnHours = GetUInt64(counter, "PowerOnHours");
                info.StartStopCycleCount = GetUInt64(counter, "StartStopCycleCount");
                info.LoadUnloadCycleCount = GetUInt64(counter, "LoadUnloadCycleCount");
                info.ReadErrorsTotal = GetUInt64(counter, "ReadErrorsTotal");
                info.ReadErrorsCorrected = GetUInt64(counter, "ReadErrorsCorrected");
                info.ReadErrorsUncorrected = GetUInt64(counter, "ReadErrorsUncorrected");
                info.ReadLatencyMax = GetUInt64(counter, "ReadLatencyMax");
                info.WriteLatencyMax = GetUInt64(counter, "WriteLatencyMax");
                info.FlushLatencyMax = GetUInt64(counter, "FlushLatencyMax");
            }
        }
    }

    private static void ReadJobs(out bool repairInProgress, out float? percentComplete)
    {
        repairInProgress = false;
        percentComplete = null;

        try
        {
            using var searcher = new ManagementObjectSearcher(Scope, "SELECT * FROM MSFT_StorageJob");
            using ManagementObjectCollection collection = searcher.Get();

            foreach (ManagementBaseObject item in collection)
            {
                using var job = (ManagementObject)item;

                // Finished jobs linger in this class. States above ShuttingDown are done.
                int? state = GetInt32(job, "JobState");
                if (state > 6)
                    continue;

                repairInProgress = true;

                int? complete = GetInt32(job, "PercentComplete");
                if (complete.HasValue && (!percentComplete.HasValue || complete.Value < percentComplete.Value))
                    percentComplete = complete.Value;
            }
        }
        catch (ManagementException)
        {
            // Jobs are optional; a failure here must not lose the rest of the snapshot.
        }
    }

    private static IEnumerable<ManagementObject> GetRelated(ManagementObject source, string resultClass)
    {
        ManagementObjectCollection related;

        try
        {
            related = source.GetRelated(resultClass);
        }
        catch (ManagementException)
        {
            // Reliability counters throw here without elevation.
            yield break;
        }

        using (related)
        {
            foreach (ManagementBaseObject item in related)
                yield return (ManagementObject)item;
        }
    }

    private static object GetValue(ManagementBaseObject source, string name)
    {
        try
        {
            return source[name];
        }
        catch (ManagementException)
        {
            return null;
        }
    }

    private static string GetString(ManagementBaseObject source, string name)
    {
        return GetValue(source, name) as string;
    }

    private static bool? GetBoolean(ManagementBaseObject source, string name)
    {
        return GetValue(source, name) is bool value ? value : null;
    }

    /// <summary>
    /// Values arrive boxed as UInt8, UInt16 or UInt32 depending on the class, so they cannot be
    /// unboxed directly.
    /// </summary>
    private static int? GetInt32(ManagementBaseObject source, string name)
    {
        object value = GetValue(source, name);

        try
        {
            return value == null ? null : Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }
        catch (Exception e) when (e is InvalidCastException or FormatException or OverflowException)
        {
            return null;
        }
    }

    private static ulong? GetUInt64(ManagementBaseObject source, string name)
    {
        object value = GetValue(source, name);

        try
        {
            return value == null ? null : Convert.ToUInt64(value, CultureInfo.InvariantCulture);
        }
        catch (Exception e) when (e is InvalidCastException or FormatException or OverflowException)
        {
            return null;
        }
    }

    private static int? GetFirstInt32(ManagementBaseObject source, string name)
    {
        if (GetValue(source, name) is not Array array || array.Length == 0)
            return null;

        try
        {
            return Convert.ToInt32(array.GetValue(0), CultureInfo.InvariantCulture);
        }
        catch (Exception e) when (e is InvalidCastException or FormatException or OverflowException)
        {
            return null;
        }
    }
}

internal sealed class StorageSpacesPoolInfo
{
    public string Name { get; set; }

    public string UniqueId { get; set; }

    public int? HealthStatus { get; set; }

    public int? OperationalStatus { get; set; }

    public ulong? Size { get; set; }

    public ulong? AllocatedSize { get; set; }

    public bool? IsReadOnly { get; set; }

    public int? RepairPolicy { get; set; }

    public string DefaultResiliency { get; set; }

    public List<StorageSpacesVirtualDiskInfo> VirtualDisks { get; } = new();

    public List<StorageSpacesPhysicalDiskInfo> PhysicalDisks { get; } = new();
}

internal sealed class StorageSpacesVirtualDiskInfo
{
    public string Name { get; set; }

    public int? HealthStatus { get; set; }

    public int? OperationalStatus { get; set; }

    public ulong? Size { get; set; }

    public ulong? AllocatedSize { get; set; }

    public ulong? FootprintOnPool { get; set; }

    public string Resiliency { get; set; }

    public int? NumberOfDataCopies { get; set; }

    public int? NumberOfColumns { get; set; }

    public int? DeviceNumber { get; set; }
}

internal sealed class StorageSpacesPhysicalDiskInfo
{
    /// <summary>
    /// The only identifier that survives a disk going missing. DeviceId is emptied and UniqueId is
    /// replaced with an internal GUID once the disk stops responding.
    /// </summary>
    public string ObjectId { get; set; }

    public string DeviceId { get; set; }

    public string Name { get; set; }

    public string Model { get; set; }

    public string SerialNumber { get; set; }

    public int? MediaType { get; set; }

    public int? HealthStatus { get; set; }

    public int? OperationalStatus { get; set; }

    public ulong? Size { get; set; }

    public ulong? AllocatedSize { get; set; }

    public int? Temperature { get; set; }

    public int? Wear { get; set; }

    public ulong? PowerOnHours { get; set; }

    public ulong? StartStopCycleCount { get; set; }

    public ulong? LoadUnloadCycleCount { get; set; }

    public ulong? ReadErrorsTotal { get; set; }

    public ulong? ReadErrorsCorrected { get; set; }

    public ulong? ReadErrorsUncorrected { get; set; }

    public ulong? ReadLatencyMax { get; set; }

    public ulong? WriteLatencyMax { get; set; }

    public ulong? FlushLatencyMax { get; set; }

    /// <summary>
    /// Wear is only meaningful on SSDs; it is a constant 0 on spinning disks.
    /// </summary>
    public bool IsSolidState => MediaType == 4;
}
