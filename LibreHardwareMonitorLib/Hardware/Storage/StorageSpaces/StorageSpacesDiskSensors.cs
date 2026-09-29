// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Text;
using BlackSharp.Core.Converters;
using BlackSharp.Core.Converters.Enums;
using StorageDeviceDIT = DiskInfoToolkit.Devices.StorageDevice;

namespace LibreHardwareMonitor.Hardware.Storage.StorageSpaces;

/// <summary>
/// What Storage Spaces knows about a disk: the state of the space for a space's own disk, or the
/// disk's state in its pool for a pool member.
/// </summary>
internal abstract class StorageSpacesDiskSensors
{
    protected StorageSpacesDiskSensors(string objectId)
    {
        ObjectId = objectId;
    }

    /// <summary>
    /// Gets the WMI object identifier of the space or pool member.
    /// </summary>
    public string ObjectId { get; }

    public IReadOnlyList<Sensor> Sensors { get; protected set; }

    /// <summary>
    /// Creates the sensors for a disk that is a storage space or a pool member, or returns
    /// <see langword="null" /> for any other disk.
    /// </summary>
    public static StorageSpacesDiskSensors Create(Hardware hardware, StorageDeviceDIT storage, ISettings settings)
    {
        StorageSpacesDiskSensors sensors = null;

        StorageSpaceInfo space = StorageSpacesData.FindSpace(storage);
        if (space?.ObjectId != null)
        {
            sensors = new SpaceSensors(space.ObjectId, hardware, settings);
        }
        else
        {
            StorageSpacesPhysicalDiskInfo physicalDisk = StorageSpacesData.FindPhysicalDisk(storage);
            if (physicalDisk?.ObjectId != null)
                sensors = new MemberSensors(physicalDisk.ObjectId, hardware, settings);
        }

        sensors?.Update();
        return sensors;
    }

    /// <summary>
    /// Gets the WMI object identifier of the space or pool member a disk is now, or
    /// <see langword="null" /> for any other disk. Differs from <see cref="ObjectId" /> once a disk
    /// joins or leaves a pool.
    /// </summary>
    public static string GetObjectId(StorageDeviceDIT storage)
    {
        return StorageSpacesData.FindSpace(storage)?.ObjectId ?? StorageSpacesData.FindPhysicalDisk(storage)?.ObjectId;
    }

    /// <summary>
    /// Gets the name for a disk that is a storage space, or <see langword="null" /> for any other disk.
    /// </summary>
    public static string GetName(StorageDeviceDIT storage)
    {
        // Several spaces all report the same product name, so name the disk after its space.
        StorageSpaceInfo space = StorageSpacesData.FindSpace(storage);
        return !string.IsNullOrEmpty(space?.Name) ? $"Storage Space - {space.Name}" : null;
    }

    public abstract void Update();

    public abstract void AppendReport(StringBuilder r);

    protected void Clear()
    {
        foreach (Sensor sensor in Sensors)
            sensor.Value = null;
    }

    protected static float? ToGigaByte(ulong? bytes)
    {
        return bytes.HasValue ? (float)DataUnitConverter.ToGigaByte(bytes.Value, DataUnit.Byte) : null;
    }

    /// <summary>
    /// The state of a storage space, shown on the space's own disk. Size, used and free space
    /// already come from the disk itself.
    /// </summary>
    private sealed class SpaceSensors : StorageSpacesDiskSensors
    {
        private readonly Sensor _allocatedSpaceSensor;
        private readonly Sensor _footprintSensor;
        private readonly Sensor _healthSensor;
        private readonly Sensor _repairProgressSensor;

        public SpaceSensors(string objectId, Hardware hardware, ISettings settings)
            : base(objectId)
        {
            _healthSensor = new Sensor("Health", 0, SensorType.Health, hardware, settings);
            _allocatedSpaceSensor = new Sensor("Allocated Space", 40, SensorType.Data, hardware, settings);
            _footprintSensor = new Sensor("Footprint On Pool", 41, SensorType.Data, hardware, settings);

            // Created up front, as sensors activated during an update never reach the tree. Reads
            // empty when idle.
            _repairProgressSensor = new Sensor("Repair Progress", 40, SensorType.Level, hardware, settings);

            Sensors = new[] { _healthSensor, _allocatedSpaceSensor, _footprintSensor, _repairProgressSensor };
        }

        public override void Update()
        {
            StorageSpacesData.Update();

            StorageSpaceInfo space = StorageSpacesData.FindSpace(ObjectId);
            if (space == null)
            {
                Clear();
                return;
            }

            _healthSensor.Value = StorageSpacesData.ToSensorValue(space.HealthStatus);
            _allocatedSpaceSensor.Value = ToGigaByte(space.AllocatedSize);
            _footprintSensor.Value = ToGigaByte(space.FootprintOnPool);

            // Reads 0 while a repair waits to start. A repair can take several passes, each with
            // its own total, so this can start again from a lower value.
            _repairProgressSensor.Value = space.RepairProgress;
        }

        public override void AppendReport(StringBuilder r)
        {
            StorageSpaceInfo space = StorageSpacesData.FindSpace(ObjectId);
            if (space == null)
                return;

            r.AppendLine("Storage Space:");
            r.AppendLine($"  Name: {space.Name}");
            r.AppendLine($"  Health Status: {StorageSpacesData.GetHealthStatusText(space.HealthStatus)}");
            r.AppendLine($"  Operational Status: {StorageSpacesData.GetOperationalStatusText(space.OperationalStatus)}");
            r.AppendLine($"  Resiliency: {space.Resiliency}");
            r.AppendLine($"  Allocated Size: {space.AllocatedSize}");
            r.AppendLine($"  Footprint On Pool: {space.FootprintOnPool}");
            r.AppendLine();
        }
    }

    /// <summary>
    /// The state of a pool member in its pool. Everything else comes from the disk itself.
    /// </summary>
    private sealed class MemberSensors : StorageSpacesDiskSensors
    {
        private readonly Sensor _allocatedSpaceSensor;
        private readonly Sensor _healthSensor;

        public MemberSensors(string objectId, Hardware hardware, ISettings settings)
            : base(objectId)
        {
            _healthSensor = new Sensor("Health", 0, SensorType.Health, hardware, settings);
            _allocatedSpaceSensor = new Sensor("Allocated Space", 42, SensorType.Data, hardware, settings);

            Sensors = new[] { _healthSensor, _allocatedSpaceSensor };
        }

        public override void Update()
        {
            StorageSpacesData.Update();

            StorageSpacesPhysicalDiskInfo physicalDisk = StorageSpacesData.FindPhysicalDisk(ObjectId);
            if (physicalDisk == null)
            {
                Clear();
                return;
            }

            _healthSensor.Value = StorageSpacesData.ToSensorValue(physicalDisk.HealthStatus);
            _allocatedSpaceSensor.Value = ToGigaByte(physicalDisk.AllocatedSize);
        }

        public override void AppendReport(StringBuilder r)
        {
            StorageSpacesPhysicalDiskInfo physicalDisk = StorageSpacesData.FindPhysicalDisk(ObjectId);
            if (physicalDisk == null)
                return;

            r.AppendLine("Storage Spaces Pool Member:");
            r.AppendLine($"  Health Status: {StorageSpacesData.GetHealthStatusText(physicalDisk.HealthStatus)}");
            r.AppendLine($"  Operational Status: {StorageSpacesData.GetOperationalStatusText(physicalDisk.OperationalStatus)}");
            r.AppendLine($"  Allocated Size: {physicalDisk.AllocatedSize}");
            r.AppendLine();
        }
    }
}
