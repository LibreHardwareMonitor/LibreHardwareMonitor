// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Text;
using BlackSharp.Core.Converters;
using BlackSharp.Core.Converters.Enums;

namespace LibreHardwareMonitor.Hardware.Storage.StorageSpaces;

/// <summary>
/// A physical disk that is a member of a <see cref="StorageSpacesPool" />. These disks are claimed
/// by spaceport.sys and are not visible to the normal storage enumeration.
/// </summary>
public sealed class StorageSpacesPhysicalDisk : Hardware
{
    private readonly string _objectId;
    private readonly StorageSpacesPool _pool;

    private Sensor _allocatedSpaceSensor;
    private Sensor _flushLatencySensor;
    private Sensor _healthSensor;
    private Sensor _loadUnloadCycleSensor;
    private Sensor _powerOnHoursSensor;
    private Sensor _readErrorsCorrectedSensor;
    private Sensor _readErrorsTotalSensor;
    private Sensor _readErrorsUncorrectedSensor;
    private Sensor _readLatencySensor;
    private Sensor _startStopCycleSensor;
    private Sensor _temperatureSensor;
    private Sensor _totalSpaceSensor;
    private Sensor _usedSpaceSensor;
    private Sensor _wearSensor;
    private Sensor _writeLatencySensor;

    internal StorageSpacesPhysicalDisk(StorageSpacesPhysicalDiskInfo info, StorageSpacesPool pool, ISettings settings)
        : base(GetName(info), new Identifier(pool.Identifier, "disk", GetId(info)), settings)
    {
        _objectId = info.ObjectId;
        _pool = pool;

        CreateSensors(info);
        Update(info);
    }

    public override HardwareType HardwareType => HardwareType.Storage;

    public override IHardware Parent => _pool;

    public override void Update()
    {
        StorageSpacesPhysicalDiskInfo info = Find();
        if (info != null)
            Update(info);
    }

    public override string GetReport()
    {
        StorageSpacesPhysicalDiskInfo info = Find();
        if (info == null)
            return null;

        var r = new StringBuilder();
        r.AppendLine("Storage Spaces Physical Disk");
        r.AppendLine();
        r.AppendLine($"Name: {info.Name}");
        r.AppendLine($"Model: {info.Model}");
        r.AppendLine($"Device Id: {info.DeviceId}");
        r.AppendLine($"Media Type: {info.MediaType}");
        r.AppendLine($"Health Status: {SensorHealth.ToDisplayString(info.HealthStatus)} ({info.HealthStatus})");
        r.AppendLine($"Operational Status: {info.OperationalStatus}");
        r.AppendLine($"Size: {info.Size}");
        r.AppendLine($"Allocated Size: {info.AllocatedSize}");
        r.AppendLine();

        return r.ToString();
    }

    private static string GetName(StorageSpacesPhysicalDiskInfo info)
    {
        string model = !string.IsNullOrEmpty(info.Model) ? info.Model : info.Name;

        // A disk that is already missing has no DeviceId to disambiguate it with.
        if (string.IsNullOrEmpty(info.DeviceId))
            return !string.IsNullOrEmpty(model) ? model : "Disk";

        // Pools are commonly built from identical drives, so the model alone is ambiguous.
        if (string.IsNullOrEmpty(model))
            return $"Disk {info.DeviceId}";

        return $"{model} (Disk {info.DeviceId})";
    }

    private static string GetId(StorageSpacesPhysicalDiskInfo info)
    {
        if (!string.IsNullOrEmpty(info.DeviceId))
            return info.DeviceId;

        // Missing disks have no DeviceId, so fall back to something that stays unique.
        int guid = info.ObjectId?.LastIndexOf("PD:", StringComparison.Ordinal) ?? -1;
        return guid >= 0 ? info.ObjectId.Substring(guid + 3).Trim('"', '{', '}') : "0";
    }

    private StorageSpacesPhysicalDiskInfo Find()
    {
        // Matched on ObjectId: a disk that stops responding loses its DeviceId.
        foreach (StorageSpacesPhysicalDiskInfo info in _pool.GetPhysicalDisks())
        {
            if (info.ObjectId == _objectId)
                return info;
        }

        return null;
    }

    private void CreateSensors(StorageSpacesPhysicalDiskInfo info)
    {
        _healthSensor = Add(new Sensor("Health", 0, SensorType.Health, this, _settings));

        // A disk that does not report temperature returns a flat 0 rather than nothing.
        if (info.Temperature is > 0)
            _temperatureSensor = Add(new Sensor("Temperature", 1, SensorType.Temperature, this, _settings));

        _usedSpaceSensor = Add(new Sensor("Used Space", 10, SensorType.Load, this, _settings));
        _totalSpaceSensor = Add(new Sensor("Total Space", 11, SensorType.Data, this, _settings));
        _allocatedSpaceSensor = Add(new Sensor("Allocated Space", 12, SensorType.Data, this, _settings));

        if (info.PowerOnHours.HasValue)
            _powerOnHoursSensor = AddHidden("Power On Hours", 20, SensorType.Factor);

        if (info.StartStopCycleCount.HasValue)
            _startStopCycleSensor = AddHidden("Start/Stop Cycle Count", 21, SensorType.Factor);

        if (info.LoadUnloadCycleCount.HasValue)
            _loadUnloadCycleSensor = AddHidden("Load/Unload Cycle Count", 22, SensorType.Factor);

        if (info.ReadErrorsTotal.HasValue)
            _readErrorsTotalSensor = AddHidden("Read Errors", 23, SensorType.Factor);

        if (info.ReadErrorsCorrected.HasValue)
            _readErrorsCorrectedSensor = AddHidden("Read Errors Corrected", 24, SensorType.Factor);

        if (info.ReadErrorsUncorrected.HasValue)
            _readErrorsUncorrectedSensor = AddHidden("Read Errors Uncorrected", 25, SensorType.Factor);

        // Wear is a constant 0 on spinning disks, so only offer it where it means something.
        if (info.IsSolidState && info.Wear.HasValue)
            _wearSensor = AddHidden("Wear", 26, SensorType.Level);

        if (info.ReadLatencyMax.HasValue)
            _readLatencySensor = AddHidden("Read Latency Max", 30, SensorType.TimeSpan);

        if (info.WriteLatencyMax.HasValue)
            _writeLatencySensor = AddHidden("Write Latency Max", 31, SensorType.TimeSpan);

        if (info.FlushLatencyMax.HasValue)
            _flushLatencySensor = AddHidden("Flush Latency Max", 32, SensorType.TimeSpan);
    }

    private Sensor Add(Sensor sensor)
    {
        ActivateSensor(sensor);
        return sensor;
    }

    private Sensor AddHidden(string name, int index, SensorType sensorType)
    {
        return Add(new Sensor(name, index, true, sensorType, this, null, _settings));
    }

    private void Update(StorageSpacesPhysicalDiskInfo info)
    {
        _healthSensor.Value = info.HealthStatus;

        if (_temperatureSensor != null)
            _temperatureSensor.Value = info.Temperature;

        _totalSpaceSensor.Value = ToGigaByte(info.Size);
        _allocatedSpaceSensor.Value = ToGigaByte(info.AllocatedSize);

        if (info.Size is > 0 && info.AllocatedSize.HasValue)
            _usedSpaceSensor.Value = 100.0f * info.AllocatedSize.Value / info.Size.Value;
        else
            _usedSpaceSensor.Value = null;

        SetValue(_powerOnHoursSensor, info.PowerOnHours);
        SetValue(_startStopCycleSensor, info.StartStopCycleCount);
        SetValue(_loadUnloadCycleSensor, info.LoadUnloadCycleCount);
        SetValue(_readErrorsTotalSensor, info.ReadErrorsTotal);
        SetValue(_readErrorsCorrectedSensor, info.ReadErrorsCorrected);
        SetValue(_readErrorsUncorrectedSensor, info.ReadErrorsUncorrected);

        if (_wearSensor != null)
            _wearSensor.Value = info.Wear;

        SetLatency(_readLatencySensor, info.ReadLatencyMax);
        SetLatency(_writeLatencySensor, info.WriteLatencyMax);
        SetLatency(_flushLatencySensor, info.FlushLatencyMax);
    }

    private static void SetValue(Sensor sensor, ulong? value)
    {
        if (sensor != null)
            sensor.Value = value;
    }

    /// <summary>
    /// Latencies are reported in milliseconds, but <see cref="SensorType.TimeSpan" /> is seconds.
    /// </summary>
    private static void SetLatency(Sensor sensor, ulong? milliseconds)
    {
        if (sensor != null)
            sensor.Value = milliseconds.HasValue ? milliseconds.Value / 1000.0f : null;
    }

    private static float? ToGigaByte(ulong? bytes)
    {
        return bytes.HasValue ? (float)DataUnitConverter.ToGigaByte(bytes.Value, DataUnit.Byte) : null;
    }
}
