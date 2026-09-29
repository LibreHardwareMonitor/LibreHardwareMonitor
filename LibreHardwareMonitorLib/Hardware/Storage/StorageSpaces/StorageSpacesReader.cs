// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management;

namespace LibreHardwareMonitor.Hardware.Storage.StorageSpaces;

/// <summary>
/// Reads Storage Spaces pools from the documented Storage Management API classes in
/// <c>root\Microsoft\Windows\Storage</c>. All of them can be read without elevation.
/// </summary>
internal static class StorageSpacesReader
{
    private const string Scope = @"root\Microsoft\Windows\Storage";

    /// <summary>
    /// The last job state that is still active. Finished jobs stay listed for a while.
    /// </summary>
    private const int JobStateShuttingDown = 6;

    public static List<StorageSpacesPoolInfo> Read()
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

        if (pools.Count > 0)
            ReadRepairProgress(pools);

        return pools;
    }

    private static StorageSpacesPoolInfo ReadPool(ManagementObject pool)
    {
        var info = new StorageSpacesPoolInfo
        {
            ObjectId = GetString(pool, "ObjectId"),
            Id = Guid.TryParse(GetString(pool, "UniqueId"), out Guid id) ? id : Guid.Empty,
            Name = GetString(pool, "FriendlyName"),
            HealthStatus = GetInt32(pool, "HealthStatus"),
            OperationalStatus = GetInt32Array(pool, "OperationalStatus"),
            Size = GetUInt64(pool, "Size"),
            AllocatedSize = GetUInt64(pool, "AllocatedSize")
        };

        foreach (ManagementObject virtualDisk in GetRelated(pool, "MSFT_VirtualDisk"))
        {
            using (virtualDisk)
                info.Spaces.Add(ReadSpace(virtualDisk));
        }

        // Pool members are not listed as MSFT_Disk, and StoragePoolUniqueId is empty on every
        // physical disk, so the association is the only way to find them. A member that has gone
        // missing stays listed.
        foreach (ManagementObject physicalDisk in GetRelated(pool, "MSFT_PhysicalDisk"))
        {
            using (physicalDisk)
                info.PhysicalDisks.Add(ReadPhysicalDisk(physicalDisk));
        }

        return info;
    }

    private static StorageSpaceInfo ReadSpace(ManagementObject virtualDisk)
    {
        var info = new StorageSpaceInfo
        {
            ObjectId = GetString(virtualDisk, "ObjectId"),
            Name = GetString(virtualDisk, "FriendlyName"),
            HealthStatus = GetInt32(virtualDisk, "HealthStatus"),
            OperationalStatus = GetInt32Array(virtualDisk, "OperationalStatus"),
            Size = GetUInt64(virtualDisk, "Size"),
            AllocatedSize = GetUInt64(virtualDisk, "AllocatedSize"),
            FootprintOnPool = GetUInt64(virtualDisk, "FootprintOnPool"),
            Resiliency = GetString(virtualDisk, "ResiliencySettingName")
        };

        // The disk number ties the space to the disk it is shown as.
        foreach (ManagementObject disk in GetRelated(virtualDisk, "MSFT_Disk"))
        {
            using (disk)
                info.DiskNumber ??= GetUInt32(disk, "Number");
        }

        return info;
    }

    private static StorageSpacesPhysicalDiskInfo ReadPhysicalDisk(ManagementObject physicalDisk)
    {
        // DeviceId is the disk number, and is empty while the disk is missing.
        string deviceId = GetString(physicalDisk, "DeviceId");

        return new StorageSpacesPhysicalDiskInfo
        {
            ObjectId = GetString(physicalDisk, "ObjectId"),
            DiskNumber = uint.TryParse(deviceId, NumberStyles.None, CultureInfo.InvariantCulture, out uint number) ? number : null,
            Name = GetString(physicalDisk, "FriendlyName"),
            SerialNumber = GetString(physicalDisk, "SerialNumber")?.Trim(),
            HealthStatus = GetInt32(physicalDisk, "HealthStatus"),
            OperationalStatus = GetInt32Array(physicalDisk, "OperationalStatus"),
            Size = GetUInt64(physicalDisk, "Size"),
            AllocatedSize = GetUInt64(physicalDisk, "AllocatedSize")
        };
    }

    /// <summary>
    /// Attributes each active job to the pools and spaces it affects. A repair runs as one job per
    /// space, so a pool combines the jobs of its spaces.
    /// </summary>
    private static void ReadRepairProgress(List<StorageSpacesPoolInfo> pools)
    {
        var progress = new Dictionary<string, JobProgress>(StringComparer.OrdinalIgnoreCase);

        using (var searcher = new ManagementObjectSearcher(Scope, "SELECT * FROM MSFT_StorageJob"))
        using (ManagementObjectCollection collection = searcher.Get())
        {
            foreach (ManagementBaseObject item in collection)
            {
                using var job = (ManagementObject)item;

                if (GetInt32(job, "JobState") is not <= JobStateShuttingDown)
                    continue;

                var jobProgress = new JobProgress(GetUInt64(job, "BytesProcessed"), GetUInt64(job, "BytesTotal"), GetInt32(job, "PercentComplete"));

                foreach (string resultClass in new[] { "MSFT_StoragePool", "MSFT_VirtualDisk" })
                {
                    foreach (ManagementObject affected in GetRelated(job, resultClass))
                    {
                        using (affected)
                        {
                            string objectId = GetString(affected, "ObjectId");
                            if (objectId != null)
                                progress[objectId] = progress.TryGetValue(objectId, out JobProgress existing) ? existing.Add(jobProgress) : jobProgress;
                        }
                    }
                }
            }
        }

        foreach (StorageSpacesPoolInfo pool in pools)
        {
            if (pool.ObjectId != null && progress.TryGetValue(pool.ObjectId, out JobProgress poolProgress))
                pool.RepairProgress = poolProgress.Percent;

            foreach (StorageSpaceInfo space in pool.Spaces)
            {
                if (space.ObjectId != null && progress.TryGetValue(space.ObjectId, out JobProgress spaceProgress))
                    space.RepairProgress = spaceProgress.Percent;
            }
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
            yield break;
        }

        using (related)
        {
            IEnumerator<ManagementBaseObject> enumerator;

            try
            {
                enumerator = related.Cast<ManagementBaseObject>().GetEnumerator();
            }
            catch (ManagementException)
            {
                yield break;
            }

            using (enumerator)
            {
                while (true)
                {
                    try
                    {
                        if (!enumerator.MoveNext())
                            yield break;
                    }
                    catch (ManagementException)
                    {
                        // A short-lived job can be gone before its associations are read.
                        yield break;
                    }

                    yield return (ManagementObject)enumerator.Current;
                }
            }
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

    private static uint? GetUInt32(ManagementBaseObject source, string name)
    {
        object value = GetValue(source, name);

        try
        {
            return value == null ? null : Convert.ToUInt32(value, CultureInfo.InvariantCulture);
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

    private static IReadOnlyList<int> GetInt32Array(ManagementBaseObject source, string name)
    {
        var values = new List<int>();

        if (GetValue(source, name) is Array array)
        {
            foreach (object value in array)
            {
                try
                {
                    values.Add(Convert.ToInt32(value, CultureInfo.InvariantCulture));
                }
                catch (Exception e) when (e is InvalidCastException or FormatException or OverflowException)
                {
                    // Skip a value that does not fit rather than lose the rest.
                }
            }
        }

        return values;
    }

    private readonly struct JobProgress
    {
        private readonly ulong _processed;
        private readonly ulong _total;
        private readonly int? _percent;

        public JobProgress(ulong? processed, ulong? total, int? percent)
        {
            _processed = processed ?? 0;
            _total = total ?? 0;
            _percent = percent;
        }

        private JobProgress(ulong processed, ulong total, int? percent)
        {
            _processed = processed;
            _total = total;
            _percent = percent;
        }

        public float? Percent => _total > 0 ? 100.0f * _processed / _total : _percent;

        public JobProgress Add(JobProgress other)
        {
            // Without byte counts, report the job that is furthest behind.
            int? percent = _percent.HasValue && other._percent.HasValue ? Math.Min(_percent.Value, other._percent.Value) : _percent ?? other._percent;
            return new JobProgress(_processed + other._processed, _total + other._total, percent);
        }
    }
}

internal sealed class StorageSpacesPoolInfo
{
    public string ObjectId { get; set; }

    public Guid Id { get; set; }

    public string Name { get; set; }

    public int? HealthStatus { get; set; }

    public IReadOnlyList<int> OperationalStatus { get; set; }

    public ulong? Size { get; set; }

    public ulong? AllocatedSize { get; set; }

    public ulong? FreeSize => Size >= AllocatedSize ? Size - AllocatedSize : null;

    /// <summary>
    /// Gets the progress of the repairs running on the pool, or <see langword="null" /> when there are none.
    /// </summary>
    public float? RepairProgress { get; set; }

    public List<StorageSpaceInfo> Spaces { get; } = new();

    public List<StorageSpacesPhysicalDiskInfo> PhysicalDisks { get; } = new();
}

internal sealed class StorageSpaceInfo
{
    public string ObjectId { get; set; }

    public string Name { get; set; }

    public int? HealthStatus { get; set; }

    public IReadOnlyList<int> OperationalStatus { get; set; }

    public ulong? Size { get; set; }

    public ulong? AllocatedSize { get; set; }

    public ulong? FootprintOnPool { get; set; }

    public string Resiliency { get; set; }

    /// <summary>
    /// Gets the number of the disk the space is shown as, or <see langword="null" /> when it is detached.
    /// </summary>
    public uint? DiskNumber { get; set; }

    /// <summary>
    /// Gets the progress of the repair running on the space, or <see langword="null" /> when there is none.
    /// </summary>
    public float? RepairProgress { get; set; }
}

internal sealed class StorageSpacesPhysicalDiskInfo
{
    private const int LostCommunication = 13;

    /// <summary>
    /// Gets the identifier of the disk. Unlike the disk number and unique ID, it stays the same while
    /// the disk is missing.
    /// </summary>
    public string ObjectId { get; set; }

    public uint? DiskNumber { get; set; }

    public string Name { get; set; }

    public string SerialNumber { get; set; }

    public int? HealthStatus { get; set; }

    public IReadOnlyList<int> OperationalStatus { get; set; }

    public ulong? Size { get; set; }

    public ulong? AllocatedSize { get; set; }

    public bool IsMissing => !DiskNumber.HasValue || OperationalStatus.Contains(LostCommunication);
}
