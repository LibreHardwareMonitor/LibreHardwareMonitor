// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BlackSharp.Core.Converters;
using BlackSharp.Core.Converters.Enums;
using DiskInfoToolkit;
using DiskInfoToolkit.Smart;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.System.Ioctl;
using StorageDeviceDIT = DiskInfoToolkit.StorageDevice;
using StorageDIT = DiskInfoToolkit.Storage;

namespace LibreHardwareMonitor.Hardware.Storage;

public sealed class StorageDevice : Hardware, ISmart
{
    private readonly PerformanceValue _perfRead = new();
    private readonly PerformanceValue _perfTotal = new();
    private readonly PerformanceValue _perfWrite = new();
    private readonly StorageDeviceDIT _storage;

    private bool _initialized;

    private long _lastReadCount;
    private long _lastTime;
    private long _lastWriteCount;

    private DateTime _lastUpdate = DateTime.MinValue;

    private readonly List<StorageDeviceSensor> _sensors = new();
    private readonly List<SmartAttribute> _attributes = new();

    private Sensor _sensorDiskReadActivity;
    private Sensor _sensorDiskReadRate;
    private Sensor _sensorDiskTotalActivity;
    private Sensor _sensorDiskWriteActivity;
    private Sensor _sensorDiskWriteRate;
    private Sensor _usageSensor;
    private Sensor _freeSpaceSensor;

    public StorageDevice(StorageDeviceDIT storage, ISettings settings)
        : base(storage.ProductName, GetIdentifier(storage), settings)
    {
        _storage = storage;

        CreateAttributes();

        CreateSensors();
    }

    public override HardwareType HardwareType => HardwareType.Storage;

    public StorageDeviceDIT Storage => _storage;

    public IReadOnlyList<SmartAttribute> Attributes => _attributes;

    /// <summary>
    /// Forces disk to wake up, if disk is asleep.
    /// </summary>
    /// <remarks>See <see cref="StorageDIT.TryWakeUp"/> for more information.</remarks>
    public bool ForceWakeup { get; set; }

    public static TimeSpan ThrottleInterval { get; set; }

    public override void Update()
    {
        if (DateTime.UtcNow - _lastUpdate < ThrottleInterval)
        {
            return;
        }

        bool isDevicePoweredOn = _storage.IsDevicePowerOn.GetValueOrDefault(true);

        // Try waking up storage device if it is asleep and ForceWakeup is enabled
        if (ForceWakeup && !isDevicePoweredOn)
        {
            StorageDIT.TryWakeUp(_storage);
            isDevicePoweredOn = _storage.IsDevicePowerOn.GetValueOrDefault(true);
        }

        bool hasChanges = false;

        //No updates for sleeping devices if we should not wake it up
        if (isDevicePoweredOn)
        {
            hasChanges = StorageDIT.Refresh(_storage);
        }

        if (!hasChanges)
        {
            if (!_initialized)
            {
                _initialized = true;
            }
            else
            {
                // If storage device has no changes, still update performance sensors to avoid stale throughput data
                UpdatePerformanceSensors();
                _lastUpdate = DateTime.UtcNow;
                return;
            }
        }

        _lastUpdate = DateTime.UtcNow;

        ToggleSpaceSensors();
        UpdatePerformanceSensors();
        UpdateSpaceSensors();

        // Update attributes
        foreach (var attribute in _storage.SmartAttributes)
        {
            // Try to find attribute
            var found = _attributes.Find(sa => sa.Id == attribute.ID);

            // Found attribute, update it
            if (found != null)
            {
                found.Attribute = attribute;
            }
        }

        // Update general sensors
        _sensors.ForEach(s => s.Update(_storage));
    }

    public override string GetReport()
    {
        var r = new StringBuilder();
        r.AppendLine("Storage");
        r.AppendLine();
        r.AppendLine($"Drive Name: {_storage.ProductName}");

        if (_storage.HealthStatus.HasValue)
        {
            r.AppendLine($"Health Status: {_storage.HealthStatus}");
        }

        if (_storage.HealthStatusReason != null)
        {
            r.AppendLine($"Health Status Reason:");
            r.AppendLine(_storage.HealthStatusReason);
        }

        r.AppendLine($"Revision: {_storage.ProductRevision}");
        r.AppendLine($"Bus type: {_storage.BusType}");

        r.AppendLine($"Controller information:");
        r.AppendLine($"  Service: {_storage.Controller.Service}"); // Important
        r.AppendLine($"  Vendor: 0x{_storage.Controller.VendorID:X4} ('{_storage.Controller.VendorName}')");
        r.AppendLine($"  Device: 0x{_storage.Controller.DeviceID:X4} ('{_storage.Controller.DeviceName}')");
        r.AppendLine($"  Family: {_storage.Controller.Family}");

        r.AppendLine($"Smart Attributes (Profile: '{_storage.SmartAttributeProfile}'):");
        r.AppendLine("  ID, Description, Value, Threshold");

        foreach (var attribute in _attributes)
        {
            r.AppendLine($"  {attribute.Id,3}, {attribute.Name,60}, {attribute.Value,18}, {attribute.Threshold,3}");
        }

        r.AppendLine();

        if (!_storage.IsDynamicDisk)
        {
            r.AppendLine("Partitions:");

            foreach (var partition in _storage.Partitions)
            {
                r.AppendLine($"  Partition #{partition.PartitionNumber}");

                if (partition.DriveLetter != null)
                {
                    r.AppendLine($"  Drive Letter: {partition.DriveLetter}");
                }

                if (partition.AvailableFreeSpaceBytes != null)
                {
                    r.AppendLine($"  Available Free Space: {partition.AvailableFreeSpaceBytes}");
                }

                r.AppendLine($"  Contains other OS: {partition.IsOtherOperatingSystemPartition}");
            }

            r.AppendLine();

            if (_storage.TotalPartitionFreeSpaceBytes != null)
            {
                r.AppendLine($"Total Free Size: {_storage.TotalPartitionFreeSpaceBytes}");
            }
        }

        r.AppendLine($"Total Size: {_storage.DiskSizeBytes}");
        _storage.ProbeTrace.ForEach(line => r.AppendLine($"Probe Trace: {line}"));

        return r.ToString();
    }

    public override void Traverse(IVisitor visitor)
    {
        foreach (ISensor sensor in Sensors)
            sensor.Accept(visitor);
    }

    private static string GetID(StorageDeviceDIT disk)
    {
        switch (disk.TransportKind)
        {
            case StorageTransportKind.Ata:
                return "ata";
            case StorageTransportKind.Scsi:
                return "scsi";
            case StorageTransportKind.Nvme:
                return "nvme";
            case StorageTransportKind.Usb:
                return "usb";
            case StorageTransportKind.Sd:
                return "sd";
            case StorageTransportKind.Mmc:
                return "mmc";
            case StorageTransportKind.Raid:
                return "raid";
            case StorageTransportKind.Sas:
                return "sas";
            case StorageTransportKind.Ahci:
                return "ahci";
            case StorageTransportKind.Virtual:
                return "virtual";
            case StorageTransportKind.Unknown:
            default:
                return "disk";
        }
    }

    private static Identifier GetIdentifier(StorageDeviceDIT storage)
    {
        string id;

        if (storage.StorageDeviceNumber.HasValue)
        {
            id = storage.StorageDeviceNumber.Value.ToString();
        }
        else if (storage.Scsi.PathID.HasValue && storage.Scsi.TargetID.HasValue)
        {
            id = $"{storage.Scsi.PathID}::{storage.Scsi.TargetID}";
        }
        else
        {
            id = "id_error";
        }

        return new Identifier(GetID(storage), id);
    }

    private void CreateAttributes()
    {
        _attributes.Clear();

        var attributes = _storage.SmartAttributes.Select(a => new SmartAttribute(a, null, 0, null));

        _attributes.AddRange(attributes.Where(a => a != null));
    }

    private void CreateSensors()
    {
        if (_storage.Temperature.HasValue)
        {
            AddSensor("Temperature", 0, false, SensorType.Temperature, s => s.Temperature.GetValueOrDefault());
        }

        // NVMe specific temperature sensors, if available
        TryAddTemperatureSensor(_storage, 1, false, 1, SmartTextKeys.GetAttributeNameKey(SmartTextKeys.TemperatureSensor1));
        TryAddTemperatureSensor(_storage, 2, false, 2, SmartTextKeys.GetAttributeNameKey(SmartTextKeys.TemperatureSensor2));
        TryAddTemperatureSensor(_storage, 3, false, 3, SmartTextKeys.GetAttributeNameKey(SmartTextKeys.TemperatureSensor3));
        TryAddTemperatureSensor(_storage, 4, false, 4, SmartTextKeys.GetAttributeNameKey(SmartTextKeys.TemperatureSensor4));
        TryAddTemperatureSensor(_storage, 5, false, 5, SmartTextKeys.GetAttributeNameKey(SmartTextKeys.TemperatureSensor5));
        TryAddTemperatureSensor(_storage, 6, false, 6, SmartTextKeys.GetAttributeNameKey(SmartTextKeys.TemperatureSensor6));
        TryAddTemperatureSensor(_storage, 7, false, 7, SmartTextKeys.GetAttributeNameKey(SmartTextKeys.TemperatureSensor7));
        TryAddTemperatureSensor(_storage, 8, false, 8, SmartTextKeys.GetAttributeNameKey(SmartTextKeys.TemperatureSensor8));

        if (_storage.TemperatureWarning.HasValue)
        {
            AddSensor("Warning Temperature", 10, false, SensorType.Temperature, s => s.TemperatureWarning.GetValueOrDefault());
        }

        if (_storage.TemperatureCritical.HasValue)
        {
            AddSensor("Critical Temperature", 11, false, SensorType.Temperature, s => s.TemperatureCritical.GetValueOrDefault());
        }

        if (_storage.Health.HasValue)
        {
            AddSensor("Life", 20, false, SensorType.Level, s => s.Health.GetValueOrDefault());
        }

        if (_storage.HostReads.HasValue)
        {
            AddSensor("Data Read", 21, false, SensorType.Data, s => s.HostReads.GetValueOrDefault());
        }

        if (_storage.HostWrites.HasValue)
        {
            AddSensor("Data Written", 22, false, SensorType.Data, s => s.HostWrites.GetValueOrDefault());
        }

        if (_storage.PowerOnCount.HasValue)
        {
            AddSensor("Power On Count", 23, false, SensorType.Factor, s => s.PowerOnCount.GetValueOrDefault());
        }

        if (_storage.PowerOnHours.HasValue)
        {
            AddSensor("Power On Hours", 24, false, SensorType.Factor, s => s.PowerOnHours.GetValueOrDefault());
        }

        _usageSensor = new Sensor("Used Space", 30, SensorType.Load, this, _settings);
        _freeSpaceSensor = new Sensor("Free Space", 31, SensorType.Data, this, _settings);
        ToggleSpaceSensors();

        var totalSpaceSensor = new Sensor("Total Space", 32, SensorType.Data, this, _settings)
        {
            Value = (float)DataUnitConverter.ToGigaByte(_storage.DiskSizeBytes.GetValueOrDefault(), DataUnit.Byte)
        };
        ActivateSensor(totalSpaceSensor);

        _sensorDiskReadActivity = new Sensor("Read Activity", 51, SensorType.Load, this, _settings);
        ActivateSensor(_sensorDiskReadActivity);

        _sensorDiskWriteActivity = new Sensor("Write Activity", 52, SensorType.Load, this, _settings);
        ActivateSensor(_sensorDiskWriteActivity);

        _sensorDiskTotalActivity = new Sensor("Total Activity", 53, SensorType.Load, this, _settings);
        ActivateSensor(_sensorDiskTotalActivity);

        _sensorDiskReadRate = new Sensor("Read Rate", 54, SensorType.Throughput, this, _settings);
        ActivateSensor(_sensorDiskReadRate);

        _sensorDiskWriteRate = new Sensor("Write Rate", 55, SensorType.Throughput, this, _settings);
        ActivateSensor(_sensorDiskWriteRate);

        AddSmartAttributeSensors();
    }

    private void TryAddTemperatureSensor(StorageDeviceDIT storage, int index, bool defaultHidden, int thermalSensorIndex, string smartTextKey)
    {
        var attr = storage.SmartAttributes.FirstOrDefault(a => a.TextKey == smartTextKey);

        if (attr != null && attr.RawValue > 0)
        {
            AddSensor($"Temperature #{thermalSensorIndex}", index, defaultHidden, SensorType.Temperature, s =>
            {
                var a = s.SmartAttributes.FirstOrDefault(a => a.TextKey == smartTextKey);
                if (a != null)
                {
                    return TemperatureConverter.KelvinToCelsius(a.RawValue);
                }

                return 0;
            });
        }
    }

    private void AddSensor(string name, int index, bool defaultHidden, SensorType sensorType, GetStorageDeviceSensorValue getValue)
    {
        var sensor = new StorageDeviceSensor(name, index, defaultHidden, sensorType, this, _settings, getValue)
        {
            Value = 0
        };

        ActivateSensor(sensor);
        _sensors.Add(sensor);
    }

    private unsafe void UpdatePerformanceSensors()
    {
        DISK_PERFORMANCE diskPerformance = new();

        using var handle = PInvoke.CreateFile(_storage.DevicePath,
                                              (uint)FileAccess.ReadWrite,
                                              FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE,
                                              null,
                                              FILE_CREATION_DISPOSITION.OPEN_EXISTING,
                                              FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL,
                                              null);

        uint bytesReturned;
        if (!PInvoke.DeviceIoControl((HANDLE)handle.DangerousGetHandle(), PInvoke.IOCTL_DISK_PERFORMANCE, null, 0, &diskPerformance, (uint)sizeof(DISK_PERFORMANCE), &bytesReturned, null))
        {
            return;
        }

        _perfRead.Update(diskPerformance.ReadTime, diskPerformance.QueryTime);
        _sensorDiskReadActivity.Value = (float)_perfRead.Result;

        _perfWrite.Update(diskPerformance.WriteTime, diskPerformance.QueryTime);
        _sensorDiskWriteActivity.Value = (float)_perfWrite.Result;

        _perfTotal.Update(diskPerformance.IdleTime, diskPerformance.QueryTime);
        _sensorDiskTotalActivity.Value = (float)(100 - _perfTotal.Result);

        long readCount = diskPerformance.BytesRead;
        long readDiff = readCount - _lastReadCount;
        _lastReadCount = readCount;

        long writeCount = diskPerformance.BytesWritten;
        long writeDiff = writeCount - _lastWriteCount;
        _lastWriteCount = writeCount;

        long currentTime = Stopwatch.GetTimestamp();
        if (_lastTime != 0)
        {
            double timeDeltaSeconds = (double)(currentTime - _lastTime) / Stopwatch.Frequency;

            if (timeDeltaSeconds > 0)
            {
                double writeSpeed = writeDiff / timeDeltaSeconds;
                _sensorDiskWriteRate.Value = (float)writeSpeed;

                double readSpeed = readDiff / timeDeltaSeconds;
                _sensorDiskReadRate.Value = (float)readSpeed;
            }
        }

        _lastTime = currentTime;
    }

    private void ToggleSpaceSensors()
    {
        if (!_storage.IsDynamicDisk && !_storage.Partitions.Any(p => p.IsOtherOperatingSystemPartition))
        {
            ActivateSensor(_usageSensor);
            ActivateSensor(_freeSpaceSensor);
        }
        else
        {
            DeactivateSensor(_usageSensor);
            DeactivateSensor(_freeSpaceSensor);
        }
    }

    private void UpdateSpaceSensors()
    {
        if (_storage.DiskSizeBytes > 0)
        {
            // Set sensor value
            _usageSensor.Value = 100.0f - (100.0f * _storage.TotalPartitionFreeSpaceBytes / _storage.DiskSizeBytes);
            _freeSpaceSensor.Value = (float)DataUnitConverter.ToGigaByte(_storage.TotalPartitionFreeSpaceBytes.GetValueOrDefault(), DataUnit.Byte);
        }
        else
        {
            _usageSensor.Value = null;
            _freeSpaceSensor.Value = null;
        }
    }

    private void AddSmartAttributeSensors()
    {
        // Unique attributes by sensor- type and channel
        var attributes = Attributes.Where(sa => sa.SensorType.HasValue)
                                   .GroupBy(sa => new { sa.SensorType.Value, sa.SensorChannel })
                                   .Select(sa => sa.First());

        foreach (var attr in attributes)
        {
            AddSensor(attr.SensorName,
                      attr.SensorChannel,
                      attr.IsHiddenByDefault,
                      attr.SensorType.Value,
                      s => attr.Value);
        }
    }

    /// <summary>
    /// Helper to calculate the disk performance with base timestamps
    /// https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-perfrawdata
    /// </summary>
    private class PerformanceValue
    {
        public double Result { get; private set; }

        private long Time { get; set; }

        private long Value { get; set; }

        public void Update(long val, long valBase)
        {
            long diffValue = val - Value;
            long diffTime = valBase - Time;

            Value = val;
            Time = valBase;
            Result = 100.0 / diffTime * diffValue;

            // sometimes it is possible that diff_value > diff_timebase
            // limit result to 100%, this is because timing issues during read from pcie controller an latency between IO operation
            if (Result > 100)
                Result = 100;
        }
    }
}
