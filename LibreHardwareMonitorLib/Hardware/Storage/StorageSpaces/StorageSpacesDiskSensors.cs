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
/// What Storage Spaces knows about a disk: the disk's state in its pool for a pool member.
/// </summary>
internal abstract class StorageSpacesDiskSensors
{
    protected StorageSpacesDiskSensors(string objectId)
    {
        ObjectId = objectId;
    }

    /// <summary>
    /// Gets the WMI object identifier of the pool member.
    /// </summary>
    public string ObjectId { get; }

    public IReadOnlyList<Sensor> Sensors { get; protected set; }

    /// <summary>
    /// Creates the sensors for a disk that is a pool member, or returns <see langword="null" /> for
    /// any other disk.
    /// </summary>
    public static StorageSpacesDiskSensors Create(Hardware hardware, StorageDeviceDIT storage, ISettings settings)
    {
        StorageSpacesDiskSensors sensors = null;

        StorageSpacesPhysicalDiskInfo physicalDisk = StorageSpacesData.FindPhysicalDisk(storage);
        if (physicalDisk?.ObjectId != null)
            sensors = new MemberSensors(physicalDisk.ObjectId, hardware, settings);

        sensors?.Update();
        return sensors;
    }

    /// <summary>
    /// Gets the WMI object identifier of the pool member a disk is now, or <see langword="null" /> for
    /// any other disk. Differs from <see cref="ObjectId" /> once a disk joins or leaves a pool.
    /// </summary>
    public static string GetObjectId(StorageDeviceDIT storage)
    {
        return StorageSpacesData.FindPhysicalDisk(storage)?.ObjectId;
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
