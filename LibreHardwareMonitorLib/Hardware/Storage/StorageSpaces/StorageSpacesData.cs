// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DiskInfoToolkit.Devices;
using StorageDeviceDIT = DiskInfoToolkit.Devices.StorageDevice;

namespace LibreHardwareMonitor.Hardware.Storage.StorageSpaces;

/// <summary>
/// Owns the shared list of Storage Spaces pools and refreshes it off the update thread.
/// </summary>
/// <remarks>
/// Reading the pools from WMI takes hundreds of milliseconds, so it is not done on the update
/// thread. Updates only ever read the last completed list while a refresh runs in the background.
/// </remarks>
internal static class StorageSpacesData
{
    private static readonly object _lock = new();
    private static readonly IReadOnlyList<StorageSpacesPoolInfo> _noPools = new List<StorageSpacesPoolInfo>();

    private static readonly Dictionary<int, string> _statusNames = new()
    {
        { 0, "Unknown" }, { 1, "Other" }, { 2, "OK" }, { 3, "Degraded" }, { 4, "Stressed" },
        { 5, "Predictive Failure" }, { 6, "Error" }, { 7, "Non-Recoverable Error" }, { 8, "Starting" },
        { 9, "Stopping" }, { 10, "Stopped" }, { 11, "In Service" }, { 12, "No Contact" },
        { 13, "Lost Communication" }, { 14, "Aborted" }, { 15, "Dormant" },
        { 16, "Supporting Entity in Error" }, { 17, "Completed" }, { 18, "Power Mode" }, { 19, "Relocating" },

        // The vendor range differs per class, but the pool, virtual disk and physical disk values do not overlap.
        { 0xD000, "Read-only" }, { 0xD001, "Incomplete" }, { 0xD002, "Detached" }, { 0xD003, "Incomplete" },
        { 0xD004, "Failed Media" }, { 0xD005, "Split" }, { 0xD006, "Stale Metadata" }, { 0xD007, "IO Error" },
        { 0xD008, "Unrecognized Metadata" }, { 0xD015, "Removing From Pool" }, { 0xD016, "In Maintenance Mode" },
        { 0xD017, "Updating Firmware" }, { 0xD018, "Device Hardware Error" }, { 0xD019, "Not Usable" },
        { 0xD01A, "Transient Error" }, { 0xD01B, "Suboptimal" }, { 0xD01C, "Starting Maintenance Mode" },
        { 0xD01D, "Stopping Maintenance Mode" }, { 0xD024, "No Redundancy" }, { 0xD025, "Threshold Exceeded" },
        { 0xD026, "Abnormal Latency" }
    };

    private static IReadOnlyList<StorageSpacesPoolInfo> _pools = _noPools;
    private static Task _refresh;
    private static DateTime _lastRefresh = DateTime.MinValue;
    private static bool _closed;
    private static int _generation;

    /// <summary>
    /// Gets or sets how long to wait between refreshes. Health and capacity change slowly, so this
    /// is deliberately longer than the update interval.
    /// </summary>
    public static TimeSpan UpdateInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Occurs on a background thread when a refresh finds that a pool gained or lost a disk. WMI can
    /// see a disk arrive or leave a little after the disk itself is added or removed.
    /// </summary>
    public static event Action MembersChanged;

    /// <summary>
    /// Gets the most recently read pools.
    /// </summary>
    public static IReadOnlyList<StorageSpacesPoolInfo> Pools
    {
        get
        {
            lock (_lock)
                return _pools;
        }
    }

    /// <summary>
    /// Reads the pools synchronously. Used while building the hardware tree, which cannot be
    /// populated from pools that have not been read yet.
    /// </summary>
    public static IReadOnlyList<StorageSpacesPoolInfo> Initialize()
    {
        IReadOnlyList<StorageSpacesPoolInfo> pools = Read();

        lock (_lock)
        {
            _closed = false;
            _generation++;
            _pools = pools;
            _lastRefresh = DateTime.UtcNow;
        }

        return pools;
    }

    /// <summary>
    /// Starts a background refresh if one is due. Never blocks.
    /// </summary>
    public static void Update()
    {
        lock (_lock)
        {
            // Without pools there is nothing to refresh. A new pool is picked up by Initialize, as
            // creating a space adds a disk.
            if (_closed || _pools.Count == 0 || (_refresh != null && !_refresh.IsCompleted))
                return;

            if (DateTime.UtcNow - _lastRefresh < UpdateInterval)
                return;

            _refresh = Task.Run(Refresh);
        }
    }

    /// <summary>
    /// Finds a pool by its Storage Spaces identifier.
    /// </summary>
    public static StorageSpacesPoolInfo FindPool(Guid id)
    {
        return Pools.FirstOrDefault(pool => pool.Id == id);
    }

    /// <summary>
    /// Finds a storage space by its WMI object identifier.
    /// </summary>
    public static StorageSpaceInfo FindSpace(string objectId)
    {
        return Pools.SelectMany(pool => pool.Spaces).FirstOrDefault(space => space.ObjectId == objectId);
    }

    /// <summary>
    /// Finds the storage space shown as a disk, or returns <see langword="null" /> for any other disk.
    /// </summary>
    public static StorageSpaceInfo FindSpace(StorageDeviceDIT disk)
    {
        if (disk.BusType != StorageBusType.Spaces || !disk.StorageDeviceNumber.HasValue)
            return null;

        return Pools.SelectMany(pool => pool.Spaces).FirstOrDefault(space => space.DiskNumber == disk.StorageDeviceNumber);
    }

    /// <summary>
    /// Finds a pool member by its WMI object identifier.
    /// </summary>
    public static StorageSpacesPhysicalDiskInfo FindPhysicalDisk(string objectId)
    {
        return Pools.SelectMany(pool => pool.PhysicalDisks).FirstOrDefault(physicalDisk => physicalDisk.ObjectId == objectId);
    }

    /// <summary>
    /// Finds the pool member that is a disk, or returns <see langword="null" /> for any other disk.
    /// </summary>
    public static StorageSpacesPhysicalDiskInfo FindPhysicalDisk(StorageDeviceDIT disk)
    {
        return Pools.SelectMany(pool => pool.PhysicalDisks).FirstOrDefault(physicalDisk => IsSameDisk(physicalDisk, disk));
    }

    /// <summary>
    /// Checks whether a pool member is a disk. Matched by disk number, or by serial number when
    /// either has no number.
    /// </summary>
    private static bool IsSameDisk(StorageSpacesPhysicalDiskInfo physicalDisk, StorageDeviceDIT disk)
    {
        if (physicalDisk.IsMissing)
            return false;

        if (physicalDisk.DiskNumber.HasValue && disk.StorageDeviceNumber.HasValue)
            return physicalDisk.DiskNumber == disk.StorageDeviceNumber;

        return !string.IsNullOrEmpty(physicalDisk.SerialNumber) &&
               string.Equals(physicalDisk.SerialNumber, disk.SerialNumber?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Converts a Storage Spaces health status to a <see cref="SensorType.Health" /> value.
    /// </summary>
    public static float? ToSensorValue(int? healthStatus)
    {
        switch (healthStatus)
        {
            case 0:
                return SensorHealth.Healthy;
            case 1:
                return SensorHealth.Warning;
            case 2:
                return SensorHealth.Critical;
            default:
                return null;
        }
    }

    /// <summary>
    /// Gets the display text of a Storage Spaces health status.
    /// </summary>
    public static string GetHealthStatusText(int? healthStatus)
    {
        return healthStatus == 2 ? "Unhealthy" : SensorHealth.ToDisplayString(ToSensorValue(healthStatus));
    }

    /// <summary>
    /// Gets the display text of Storage Spaces operational statuses.
    /// </summary>
    public static string GetOperationalStatusText(IReadOnlyList<int> operationalStatus)
    {
        if (operationalStatus == null || operationalStatus.Count == 0)
            return "Unknown";

        return string.Join(", ", operationalStatus.Select(status => _statusNames.TryGetValue(status, out string name) ? name : $"0x{status:X}"));
    }

    public static void Close()
    {
        lock (_lock)
        {
            _closed = true;
            _pools = _noPools;
        }
    }

    private static void Refresh()
    {
        int generation;

        lock (_lock)
            generation = _generation;

        IReadOnlyList<StorageSpacesPoolInfo> pools = Read();
        bool membersChanged;

        lock (_lock)
        {
            // Discard a refresh that finished after the group was closed, or after the disks
            // changed and the pools were read again.
            if (_closed || generation != _generation)
                return;

            membersChanged = GetMembers(pools) != GetMembers(_pools);
            _pools = pools;
            _lastRefresh = DateTime.UtcNow;
        }

        if (membersChanged)
            MembersChanged?.Invoke();
    }

    /// <summary>
    /// Describes which disks and spaces make up the pools, and which disks are present.
    /// </summary>
    private static string GetMembers(IReadOnlyList<StorageSpacesPoolInfo> pools)
    {
        return string.Join("|", pools.Select(pool => pool.ObjectId + ":" +
                                                     string.Join(",", pool.PhysicalDisks.Select(physicalDisk => $"{physicalDisk.ObjectId}={(physicalDisk.IsMissing ? "-" : physicalDisk.DiskNumber.ToString())}").OrderBy(disk => disk, StringComparer.Ordinal)) + ":" +
                                                     string.Join(",", pool.Spaces.Select(space => $"{space.ObjectId}={space.DiskNumber}").OrderBy(space => space, StringComparer.Ordinal)))
                                          .OrderBy(pool => pool, StringComparer.Ordinal));
    }

    private static IReadOnlyList<StorageSpacesPoolInfo> Read()
    {
        try
        {
            return StorageSpacesReader.Read();
        }
        catch (Exception)
        {
            // A failed read must not take the rest of the storage tree down with it.
            return _noPools;
        }
    }
}
