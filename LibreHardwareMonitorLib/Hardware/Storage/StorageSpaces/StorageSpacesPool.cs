// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlackSharp.Core.Converters;
using BlackSharp.Core.Converters.Enums;

namespace LibreHardwareMonitor.Hardware.Storage.StorageSpaces;

/// <summary>
/// A Storage Spaces pool, with its member disks as <see cref="SubHardware" />. The members are the
/// same <see cref="StorageDevice" /> nodes that would otherwise be shown at the top level. The
/// spaces built on the pool are not repeated here; each is already enumerated as its own disk.
/// </summary>
public sealed class StorageSpacesPool : Hardware
{
    private readonly Guid _id;
    private readonly StorageDevice[] _members;

    private Sensor _allocatedSpaceSensor;
    private Sensor _freeSpaceSensor;
    private Sensor _healthSensor;
    private Sensor _missingDisksSensor;
    private Sensor _repairProgressSensor;
    private Sensor _totalSpaceSensor;
    private Sensor _usedSpaceSensor;

    internal StorageSpacesPool(StorageSpacesPoolInfo pool, IEnumerable<StorageDevice> members, ISettings settings)
        : base(GetName(pool), new Identifier("storagespaces", pool.Id.ToString()), settings)
    {
        _id = pool.Id;
        _members = members.ToArray();

        foreach (StorageDevice member in _members)
            member.SetParent(this);

        CreateSensors();
        UpdateSensors(pool);
    }

    public override HardwareType HardwareType => HardwareType.StorageSpaces;

    public override IHardware[] SubHardware => _members;

    public override IDictionary<string, string> Properties
    {
        get
        {
            var properties = new SortedDictionary<string, string>();
            StorageSpacesPoolInfo pool = StorageSpacesData.FindPool(_id);

            if (pool != null)
            {
                properties.Add("Health Status", StorageSpacesData.GetHealthStatusText(pool.HealthStatus));
                properties.Add("Operational Status", StorageSpacesData.GetOperationalStatusText(pool.OperationalStatus));

                foreach (StorageSpacesPhysicalDiskInfo physicalDisk in pool.PhysicalDisks.Where(physicalDisk => physicalDisk.IsMissing))
                    properties[$"Missing Disk '{GetName(physicalDisk)}'"] = StorageSpacesData.GetOperationalStatusText(physicalDisk.OperationalStatus);

                foreach (StorageSpaceInfo space in pool.Spaces)
                    properties[$"Space '{space.Name}'"] = $"{StorageSpacesData.GetHealthStatusText(space.HealthStatus)}, {StorageSpacesData.GetOperationalStatusText(space.OperationalStatus)}";
            }

            return properties;
        }
    }

    public override void Update()
    {
        StorageSpacesData.Update();

        StorageSpacesPoolInfo pool = StorageSpacesData.FindPool(_id);
        if (pool != null)
            UpdateSensors(pool);
    }

    public override void Traverse(IVisitor visitor)
    {
        base.Traverse(visitor);

        foreach (IHardware member in _members)
            member.Accept(visitor);
    }

    public override void Close()
    {
        // The members are closed by the storage group, which owns them.
        foreach (StorageDevice member in _members)
            member.SetParent(null);

        base.Close();
    }

    public override string GetReport()
    {
        StorageSpacesPoolInfo pool = StorageSpacesData.FindPool(_id);
        if (pool == null)
            return null;

        var r = new StringBuilder();
        r.AppendLine("Storage Spaces Pool");
        r.AppendLine();
        r.AppendLine($"Name: {pool.Name}");
        r.AppendLine($"ID: {pool.Id}");
        r.AppendLine($"Health Status: {StorageSpacesData.GetHealthStatusText(pool.HealthStatus)}");
        r.AppendLine($"Operational Status: {StorageSpacesData.GetOperationalStatusText(pool.OperationalStatus)}");
        r.AppendLine($"Size: {pool.Size}");
        r.AppendLine($"Allocated Size: {pool.AllocatedSize}");
        r.AppendLine($"Repair Progress: {pool.RepairProgress?.ToString("0.0") ?? "-"}");
        r.AppendLine();

        foreach (StorageSpacesPhysicalDiskInfo physicalDisk in pool.PhysicalDisks)
        {
            StorageDevice member = _members.FirstOrDefault(m => m.StorageSpacesObjectId == physicalDisk.ObjectId);

            r.AppendLine($"Physical Disk: {GetName(physicalDisk)}");
            r.AppendLine($"  Shown As: {(member != null ? $"{member.Name} ({member.Identifier}){(member.IsMissing ? ", missing" : "")}" : "-")}");
            r.AppendLine($"  Disk Number: {physicalDisk.DiskNumber?.ToString() ?? "-"}");
            r.AppendLine($"  Health Status: {StorageSpacesData.GetHealthStatusText(physicalDisk.HealthStatus)}");
            r.AppendLine($"  Operational Status: {StorageSpacesData.GetOperationalStatusText(physicalDisk.OperationalStatus)}");
            r.AppendLine($"  Size: {physicalDisk.Size}");
            r.AppendLine($"  Allocated Size: {physicalDisk.AllocatedSize}");
        }

        r.AppendLine();

        foreach (StorageSpaceInfo space in pool.Spaces)
        {
            r.AppendLine($"Space: {space.Name}");
            r.AppendLine($"  Disk Number: {space.DiskNumber?.ToString() ?? "-"}");
            r.AppendLine($"  Health Status: {StorageSpacesData.GetHealthStatusText(space.HealthStatus)}");
            r.AppendLine($"  Operational Status: {StorageSpacesData.GetOperationalStatusText(space.OperationalStatus)}");
            r.AppendLine($"  Resiliency: {space.Resiliency}");
            r.AppendLine($"  Size: {space.Size}");
            r.AppendLine($"  Allocated Size: {space.AllocatedSize}");
            r.AppendLine($"  Footprint On Pool: {space.FootprintOnPool}");
            r.AppendLine($"  Repair Progress: {space.RepairProgress?.ToString("0.0") ?? "-"}");
        }

        r.AppendLine();
        return r.ToString();
    }

    private static string GetName(StorageSpacesPoolInfo pool)
    {
        // Prefixed so a pool stands out from the disks around it, whatever it is called.
        return !string.IsNullOrEmpty(pool.Name) ? $"Storage Pool - {pool.Name}" : "Storage Pool";
    }

    private static string GetName(StorageSpacesPhysicalDiskInfo physicalDisk)
    {
        // Disks of the same model share a name, so add the serial number to tell them apart.
        return !string.IsNullOrEmpty(physicalDisk.SerialNumber) ? $"{physicalDisk.Name} ({physicalDisk.SerialNumber})" : physicalDisk.Name;
    }

    private void CreateSensors()
    {
        _healthSensor = Add(new Sensor("Health", 0, SensorType.Health, this, _settings));
        _missingDisksSensor = Add(new Sensor("Missing Disks", 0, SensorType.Factor, this, _settings));
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

    private void UpdateSensors(StorageSpacesPoolInfo pool)
    {
        _healthSensor.Value = StorageSpacesData.ToSensorValue(pool.HealthStatus);
        _missingDisksSensor.Value = pool.PhysicalDisks.Count(physicalDisk => physicalDisk.IsMissing);
        _totalSpaceSensor.Value = ToGigaByte(pool.Size);
        _allocatedSpaceSensor.Value = ToGigaByte(pool.AllocatedSize);
        _freeSpaceSensor.Value = ToGigaByte(pool.FreeSize);
        _usedSpaceSensor.Value = pool.Size > 0 && pool.AllocatedSize.HasValue ? 100.0f * pool.AllocatedSize / pool.Size : null;

        // Reads 0 while a repair waits to start. A repair can take several passes, each with its
        // own total, so this can start again from a lower value.
        _repairProgressSensor.Value = pool.RepairProgress;
    }

    private static float? ToGigaByte(ulong? bytes)
    {
        return bytes.HasValue ? (float)DataUnitConverter.ToGigaByte(bytes.Value, DataUnit.Byte) : null;
    }
}
