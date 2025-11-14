// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using BlackSharp.Core.Converters;
using DiskInfoToolkit.Interop.Enums;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.System.Ioctl;

namespace LibreHardwareMonitor.Hardware.Storage;

public sealed class StorageDevice : Hardware, ISmart
{
    private readonly PerformanceValue _perfRead = new();
    private readonly PerformanceValue _perfTotal = new();
    private readonly PerformanceValue _perfWrite = new();

    private readonly DiskInfoToolkit.Storage _storage;

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

    public StorageDevice(DiskInfoToolkit.Storage storage, string id, ISettings settings)
        : base(storage.Model, new Identifier(id, storage.DriveNumber.ToString(CultureInfo.InvariantCulture)), settings)
    {
        _storage = storage;

        CreateAttributes();

        CreateSensors();
    }

    public override HardwareType HardwareType => HardwareType.Storage;

    internal DiskInfoToolkit.Storage Storage => _storage;

    public IReadOnlyList<SmartAttribute> Attributes => _attributes;

    public static TimeSpan ThrottleInterval { get; set; }

    public override void Update()
    {
        if (DateTime.UtcNow - _lastUpdate < ThrottleInterval)
        {
            return;
        }

        _lastUpdate = DateTime.UtcNow;

        //Toggle usage sensor
        ToggleUsageSensor();

        //Update performance sensors
        UpdatePerformanceSensors();

        //Update storage object
        _storage.Update();

        //Update usage sensors
        UpdateUsageSensor();

        //Update attributes
        foreach (var attribute in _storage.Smart.SmartAttributes)
        {
            //Try to find attribute
            var found = _attributes.Find(sa => sa.Id == attribute.Info.ID);

            //Found attribute, update it
            if (found != null)
            {
                found.Attribute = attribute;
            }
        }

        //Update general sensors
        _sensors.ForEach(s => s.Update(_storage));
    }

    public override string GetReport()
    {
        var r = new StringBuilder();
        r.AppendLine("Storage");
        r.AppendLine();
        r.AppendLine($"Drive Name: {_storage.Model}");
        r.AppendLine($"Firmware Version and Revision: {_storage.Firmware}; {_storage.FirmwareRev}");
        r.AppendLine();
        r.AppendLine("Smart Attributes:");
        r.AppendLine("ID, Description, Value, Threshold");

        foreach (var attribute in _attributes)
        {
            r.AppendLine($"{attribute.Id,3}, {attribute.Name,60}, {attribute.Value,18}, {attribute.Threshold,3}");
        }

        r.AppendLine();

        if (!_storage.IsDynamicDisk)
        {
            r.AppendLine("Partitions:");

            foreach (var partition in _storage.Partitions)
            {
                r.AppendLine($"Partition #{partition.PartitionNumber}");

                if (partition.DriveLetter != null)
                {
                    r.AppendLine($"Drive Letter: {partition.DriveLetter}");
                }

                if (partition.AvailableFreeSpace != null)
                {
                    r.AppendLine($"Available Free Space: {partition.AvailableFreeSpace}");
                }
            }

            r.AppendLine();

            if (_storage.TotalFreeSize != null)
            {
                r.AppendLine($"Total Free Size: {_storage.TotalFreeSize}");
            }
        }

        r.AppendLine($"Total Size: {_storage.TotalSize}");

        return r.ToString();
    }

    public override void Traverse(IVisitor visitor)
    {
        foreach (ISensor sensor in Sensors)
            sensor.Accept(visitor);
    }

    private void CreateAttributes()
    {
        _attributes.Clear();

        var attributes = SmartAttributeTranslator.GetAttributesFor(_storage);

        _attributes.AddRange(attributes.Where(a => a != null));
    }

    private void CreateSensors()
    {
        _usageSensor = new Sensor("Used Space", 0, SensorType.Load, this, _settings);
        ToggleUsageSensor();

        int index = 0;

        AddSensor("Temperature", index++, false, SensorType.Temperature, s => s.Smart.Temperature.GetValueOrDefault());

        if (_storage.Smart.Life.HasValue)
        {
            AddSensor("Life", index++, false, SensorType.Level, s => s.Smart.Life.GetValueOrDefault());
        }

        if (_storage.Smart.HostReads.HasValue)
        {
            AddSensor("Data read", index++, false, SensorType.Data, s => s.Smart.HostReads.GetValueOrDefault());
        }

        if (_storage.Smart.HostWrites.HasValue)
        {
            AddSensor("Data written", index++, false, SensorType.Data, s => s.Smart.HostWrites.GetValueOrDefault());
        }

        AddSensor("Power on count", index++, false, SensorType.Factor, s => s.Smart.PowerOnCount);
        AddSensor("Power on hours", index++, false, SensorType.Factor, s => Math.Max(s.Smart.MeasuredPowerOnHours, s.Smart.DetectedPowerOnHours));

        if (_storage.IsNVMe)
        {
            AddSensor("Temperature warning", index++, false, SensorType.Factor, s => s.Smart.TemperatureWarning.GetValueOrDefault());
            AddSensor("Temperature critical", index++, false, SensorType.Factor, s => s.Smart.TemperatureCritical.GetValueOrDefault());

            TryAddTemperatureSensor(index++, false, 1, SmartAttributeType.TemperatureSensor1);
            TryAddTemperatureSensor(index++, false, 2, SmartAttributeType.TemperatureSensor2);
            TryAddTemperatureSensor(index++, false, 3, SmartAttributeType.TemperatureSensor3);
            TryAddTemperatureSensor(index++, false, 4, SmartAttributeType.TemperatureSensor4);
            TryAddTemperatureSensor(index++, false, 5, SmartAttributeType.TemperatureSensor5);
            TryAddTemperatureSensor(index++, false, 6, SmartAttributeType.TemperatureSensor6);
            TryAddTemperatureSensor(index++, false, 7, SmartAttributeType.TemperatureSensor7);
            TryAddTemperatureSensor(index++, false, 8, SmartAttributeType.TemperatureSensor8);
        }

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

    private void TryAddTemperatureSensor(int index, bool defaultHidden, int thermalSensorIndex, SmartAttributeType type)
    {
        var attr = GetSmartAttribute(type);

        //Does the sensor have a value ?
        if (attr != null && attr.Attribute.RawValueULong > 0)
        {
            AddSensor($"Temperature {thermalSensorIndex}", index, defaultHidden, SensorType.Temperature, s =>
            {
                var a = GetSmartAttribute(type);
                if (a != null)
                {
                    return TemperatureConverter.KelvinToCelsius(a.Attribute.RawValueULong);
                }

                return 0;
            });
        }
    }

    private DiskInfoToolkit.SmartAttribute GetSmartAttribute(SmartAttributeType type)
    {
        return _storage.Smart.SmartAttributes.FirstOrDefault(sa => sa.Info.Type == type);
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

        using var handle = PInvoke.CreateFile(_storage.PhysicalPath,
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
            double timeDeltaSeconds = TimeSpan.FromTicks(currentTime - _lastTime).TotalSeconds;

            double writeSpeed = writeDiff * (1 / timeDeltaSeconds);
            _sensorDiskWriteRate.Value = (float)writeSpeed;

            double readSpeed = readDiff * (1 / timeDeltaSeconds);
            _sensorDiskReadRate.Value = (float)readSpeed;
        }

        _lastTime = currentTime;
    }

    private void ToggleUsageSensor()
    {
        if (!_storage.IsDynamicDisk
         && !_storage.Partitions.Any(p => p.IsOtherOperatingSystemPartition))
        {
            ActivateSensor(_usageSensor);
        }
        else
        {
            DeactivateSensor(_usageSensor);
        }
    }

    private void UpdateUsageSensor()
    {
        if (_storage.TotalSize > 0)
        {
            //Set sensor value
            _usageSensor.Value = 100.0f - (100.0f * _storage.TotalFreeSize / _storage.TotalSize);
        }
        else
        {
            _usageSensor.Value = null;
        }
    }

    private void AddSmartAttributeSensors()
    {
        //Unique attributes by sensor- type and channel
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

            //sometimes it is possible that diff_value > diff_timebase
            //limit result to 100%, this is because timing issues during read from pcie controller an latency between IO operation
            if (Result > 100)
                Result = 100;
        }
    }
}
