// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Text;
using BlackSharp.Core.Converters;
using BlackSharp.Core.Converters.Enums;

namespace LibreHardwareMonitor.Hardware.Storage.StorageSpaces;

/// <summary>
/// A Storage Spaces pool, with its member disks as <see cref="SubHardware" />. The virtual disks
/// built on the pool are not repeated here; they are already enumerated as ordinary disks.
/// </summary>
public sealed class StorageSpacesPool : Hardware
{
    private static readonly IReadOnlyList<StorageSpacesPhysicalDiskInfo> _noDisks = new List<StorageSpacesPhysicalDiskInfo>();

    private readonly StorageSpacesPhysicalDisk[] _physicalDisks;
    private readonly string _uniqueId;

    private Sensor _allocatedSpaceSensor;
    private Sensor _freeSpaceSensor;
    private Sensor _healthSensor;
    private Sensor _repairProgressSensor;
    private Sensor _totalSpaceSensor;
    private Sensor _usedSpaceSensor;

    internal StorageSpacesPool(StorageSpacesPoolInfo info, int index, ISettings settings)
        : base(info.Name ?? "Storage Pool", new Identifier("storagespaces", index.ToString()), settings)
    {
        _uniqueId = info.UniqueId;

        CreateSensors();
        UpdateSensors(info);

        var disks = new List<StorageSpacesPhysicalDisk>();
        foreach (StorageSpacesPhysicalDiskInfo disk in info.PhysicalDisks)
            disks.Add(new StorageSpacesPhysicalDisk(disk, this, settings));

        _physicalDisks = disks.ToArray();
    }

    public override HardwareType HardwareType => HardwareType.StorageSpaces;

    public override IHardware[] SubHardware => _physicalDisks;

    public override IDictionary<string, string> Properties
    {
        get
        {
            var properties = new SortedDictionary<string, string>();
            StorageSpacesPoolInfo info = Find();

            if (info != null)
            {
                properties.Add("Health Status", SensorHealth.ToDisplayString(info.HealthStatus));

                if (info.IsReadOnly.HasValue)
                    properties.Add("Read Only", info.IsReadOnly.Value.ToString());

                if (!string.IsNullOrEmpty(info.DefaultResiliency))
                    properties.Add("Default Resiliency", info.DefaultResiliency);

                foreach (StorageSpacesVirtualDiskInfo virtualDisk in info.VirtualDisks)
                {
                    properties.Add($"Virtual Disk '{virtualDisk.Name}'",
                                   $"{virtualDisk.Resiliency}, {virtualDisk.NumberOfDataCopies} copies, {virtualDisk.NumberOfColumns} columns");
                }
            }

            return properties;
        }
    }

    public override void Update()
    {
        StorageSpacesData.Update();

        StorageSpacesPoolInfo info = Find();
        if (info != null)
            UpdateSensors(info);
    }

    public override void Traverse(IVisitor visitor)
    {
        base.Traverse(visitor);

        foreach (IHardware disk in _physicalDisks)
            disk.Accept(visitor);
    }

    public override void Close()
    {
        foreach (StorageSpacesPhysicalDisk disk in _physicalDisks)
            disk.Close();

        base.Close();
    }

    internal IReadOnlyList<StorageSpacesPhysicalDiskInfo> GetPhysicalDisks()
    {
        return Find()?.PhysicalDisks ?? _noDisks;
    }

    public override string GetReport()
    {
        StorageSpacesPoolInfo info = Find();
        if (info == null)
            return null;

        var r = new StringBuilder();
        r.AppendLine("Storage Spaces Pool");
        r.AppendLine();
        r.AppendLine($"Name: {info.Name}");
        r.AppendLine($"Unique Id: {info.UniqueId}");
        r.AppendLine($"Health Status: {SensorHealth.ToDisplayString(info.HealthStatus)} ({info.HealthStatus})");
        r.AppendLine($"Operational Status: {info.OperationalStatus}");
        r.AppendLine($"Size: {info.Size}");
        r.AppendLine($"Allocated Size: {info.AllocatedSize}");
        r.AppendLine($"Read Only: {info.IsReadOnly}");
        r.AppendLine($"Repair Policy: {info.RepairPolicy}");
        r.AppendLine($"Default Resiliency: {info.DefaultResiliency}");
        r.AppendLine();

        foreach (StorageSpacesVirtualDiskInfo virtualDisk in info.VirtualDisks)
        {
            r.AppendLine($"Virtual Disk: {virtualDisk.Name}");
            r.AppendLine($"  Health Status: {SensorHealth.ToDisplayString(virtualDisk.HealthStatus)}");
            r.AppendLine($"  Resiliency: {virtualDisk.Resiliency}");
            r.AppendLine($"  Data Copies: {virtualDisk.NumberOfDataCopies}");
            r.AppendLine($"  Columns: {virtualDisk.NumberOfColumns}");
            r.AppendLine($"  Size: {virtualDisk.Size}");
            r.AppendLine($"  Footprint On Pool: {virtualDisk.FootprintOnPool}");
            r.AppendLine($"  Device Number: {virtualDisk.DeviceNumber}");
        }

        r.AppendLine();
        return r.ToString();
    }

    private StorageSpacesPoolInfo Find()
    {
        StorageSpacesSnapshot snapshot = StorageSpacesData.Snapshot;
        if (snapshot == null)
            return null;

        foreach (StorageSpacesPoolInfo info in snapshot.Pools)
        {
            if (info.UniqueId == _uniqueId)
                return info;
        }

        return null;
    }

    private void CreateSensors()
    {
        _healthSensor = Add(new Sensor("Health", 0, SensorType.Health, this, _settings));
        _usedSpaceSensor = Add(new Sensor("Used Space", 10, SensorType.Load, this, _settings));
        _totalSpaceSensor = Add(new Sensor("Total Space", 11, SensorType.Data, this, _settings));
        _allocatedSpaceSensor = Add(new Sensor("Allocated Space", 12, SensorType.Data, this, _settings));
        _freeSpaceSensor = Add(new Sensor("Free Space", 13, SensorType.Data, this, _settings));

        // Created up front like the rest: activating a sensor during an update happens on the
        // background update thread, where it never reaches the tree. Reads empty when idle.
        _repairProgressSensor = Add(new Sensor("Repair Progress", 20, SensorType.Level, this, _settings));
    }

    private Sensor Add(Sensor sensor)
    {
        ActivateSensor(sensor);
        return sensor;
    }

    private void UpdateSensors(StorageSpacesPoolInfo info)
    {
        _healthSensor.Value = info.HealthStatus;
        _totalSpaceSensor.Value = ToGigaByte(info.Size);
        _allocatedSpaceSensor.Value = ToGigaByte(info.AllocatedSize);

        if (info.Size.HasValue && info.AllocatedSize.HasValue)
        {
            _freeSpaceSensor.Value = ToGigaByte(info.Size.Value - info.AllocatedSize.Value);
            _usedSpaceSensor.Value = info.Size.Value > 0 ? 100.0f * info.AllocatedSize.Value / info.Size.Value : null;
        }
        else
        {
            _freeSpaceSensor.Value = null;
            _usedSpaceSensor.Value = null;
        }

        StorageSpacesSnapshot snapshot = StorageSpacesData.Snapshot;
        _repairProgressSensor.Value = snapshot is { RepairInProgress: true } ? snapshot.RepairPercentComplete : null;
    }

    private static float? ToGigaByte(ulong? bytes)
    {
        return bytes.HasValue ? (float)DataUnitConverter.ToGigaByte(bytes.Value, DataUnit.Byte) : null;
    }
}
