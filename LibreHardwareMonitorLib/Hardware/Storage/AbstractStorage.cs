// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using LibreHardwareMonitor.Interop;

namespace LibreHardwareMonitor.Hardware.Storage;

public abstract class AbstractStorage : Hardware
{
    private readonly PerformanceValue _perfTotal = new();
    private readonly PerformanceValue _perfWrite = new();
    private readonly StorageInfo _storageInfo;
    private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(60);

    private ulong _lastReadCount;
    private long _lastTime;
    private DateTime _lastUpdate = DateTime.MinValue;
    private ulong _lastWriteCount;
    private Sensor _sensorDiskReadRate;
    private Sensor _sensorDiskTotalActivity;
    private Sensor _sensorDiskWriteActivity;
    private Sensor _sensorDiskWriteRate;
    private Sensor _usageSensor;

    internal AbstractStorage(StorageInfo storageInfo, string name, string firmwareRevision, string id, int index, ISettings settings)
        : base(name, new Identifier(id, index.ToString(CultureInfo.InvariantCulture)), settings)
    {
        _storageInfo = storageInfo;
        FirmwareRevision = firmwareRevision;
        Index = index;

        string[] logicalDrives = WindowsStorage.GetLogicalDrives(index);
        var driveInfoList = new List<DriveInfo>(logicalDrives.Length);

        foreach (string logicalDrive in logicalDrives)
        {
            try
            {
                var di = new DriveInfo(logicalDrive);
                if (di.TotalSize > 0)
                    driveInfoList.Add(new DriveInfo(logicalDrive));
            }
            catch (ArgumentException)
            { }
            catch (IOException)
            { }
            catch (UnauthorizedAccessException)
            { }
        }

        DriveInfos = driveInfoList.ToArray();
    }

    public DriveInfo[] DriveInfos { get; }

    public string FirmwareRevision { get; }

    public override HardwareType HardwareType => HardwareType.Storage;

    public int Index { get; }

    /// <inheritdoc />
    public override void Close()
    {
        _storageInfo.Handle?.Close();
        base.Close();
    }

    public static AbstractStorage CreateInstance(string deviceId, uint driveNumber, ulong diskSize, int scsiPort, ISettings settings)
    {
        StorageInfo info = WindowsStorage.GetStorageInfo(deviceId, driveNumber);
        if (info == null || info.Removable || info.BusType is Kernel32.STORAGE_BUS_TYPE.BusTypeVirtual or Kernel32.STORAGE_BUS_TYPE.BusTypeFileBackedVirtual)
            return null;

        info.DiskSize = diskSize;
        info.DeviceId = deviceId;
        info.Handle = Kernel32.OpenDevice(deviceId);
        info.Scsi = $@"\\.\SCSI{scsiPort}:";
        
        //fallback, when it is not possible to read out with the nvme implementation,
        //try it with the sata smart implementation
        if (info.BusType == Kernel32.STORAGE_BUS_TYPE.BusTypeNvme)
        {
            AbstractStorage x = NVMeGeneric.CreateInstance(info, settings);
            if (x != null)
                return x;
        }

        return info.BusType is Kernel32.STORAGE_BUS_TYPE.BusTypeAta or Kernel32.STORAGE_BUS_TYPE.BusTypeSata or Kernel32.STORAGE_BUS_TYPE.BusTypeNvme
            ? AtaStorage.CreateInstance(info, settings)
            : StorageGeneric.CreateInstance(info, settings);
    }

    protected virtual void CreateSensors()
    {
        if (DriveInfos.Length > 0)
        {
            _usageSensor = new Sensor("Used Space", 0, SensorType.Load, this, _settings);
            ActivateSensor(_usageSensor);
        }

        _sensorDiskWriteActivity = new Sensor("Write Activity", 32, SensorType.Load, this, _settings);
        ActivateSensor(_sensorDiskWriteActivity);

        _sensorDiskTotalActivity = new Sensor("Total Activity", 33, SensorType.Load, this, _settings);
        ActivateSensor(_sensorDiskTotalActivity);

        _sensorDiskReadRate = new Sensor("Read Rate", 34, SensorType.Throughput, this, _settings);
        ActivateSensor(_sensorDiskReadRate);

        _sensorDiskWriteRate = new Sensor("Write Rate", 35, SensorType.Throughput, this, _settings);
        ActivateSensor(_sensorDiskWriteRate);
    }

    protected abstract void UpdateSensors();

    public override void Update()
    {
        // Update statistics.
        if (_storageInfo != null)
        {
            try
            {
                UpdatePerformanceSensors();
            }
            catch
            {
                // Ignored.
            }
        }

        // Read out at update interval.
        TimeSpan tDiff = DateTime.UtcNow - _lastUpdate;
        if (tDiff > _updateInterval)
        {
            _lastUpdate = DateTime.UtcNow;

            UpdateSensors();

            if (_usageSensor != null)
            {
                long totalSize = 0;
                long totalFreeSpace = 0;

                for (int i = 0; i < DriveInfos.Length; i++)
                {
                    if (!DriveInfos[i].IsReady)
                        continue;

                    try
                    {
                        totalSize += DriveInfos[i].TotalSize;
                        totalFreeSpace += DriveInfos[i].TotalFreeSpace;
                    }
                    catch (IOException)
                    { }
                    catch (UnauthorizedAccessException)
                    { }
                }

                if (totalSize > 0)
                    _usageSensor.Value = 100.0f - (100.0f * totalFreeSpace / totalSize);
                else
                    _usageSensor.Value = null;
            }
        }
    }

    private void UpdatePerformanceSensors()
    {
        if (!Kernel32.DeviceIoControl(_storageInfo.Handle,
                                      Kernel32.IOCTL.IOCTL_DISK_PERFORMANCE,
                                      IntPtr.Zero,
                                      0,
                                      out Kernel32.DISK_PERFORMANCE diskPerformance,
                                      Marshal.SizeOf<Kernel32.DISK_PERFORMANCE>(),
                                      out _,
                                      IntPtr.Zero))
        {
            return;
        }

        _perfWrite.Update(diskPerformance.WriteTime, diskPerformance.QueryTime);
        _sensorDiskWriteActivity.Value = (float)_perfWrite.Result;

        _perfTotal.Update(diskPerformance.IdleTime, diskPerformance.QueryTime);
        _sensorDiskTotalActivity.Value = (float)(100 - _perfTotal.Result);

        ulong readCount = diskPerformance.BytesRead;
        ulong readDiff = readCount - _lastReadCount;
        _lastReadCount = readCount;

        ulong writeCount = diskPerformance.BytesWritten;
        ulong writeDiff = writeCount - _lastWriteCount;
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

    protected abstract void GetReport(StringBuilder r);

    public override string GetReport()
    {
        var r = new StringBuilder();
        r.AppendLine("Storage");
        r.AppendLine();
        r.AppendLine("Drive Name: " + _name);
        r.AppendLine("Firmware Version: " + FirmwareRevision);
        r.AppendLine();
        GetReport(r);

        foreach (DriveInfo di in DriveInfos)
        {
            if (!di.IsReady)
                continue;

            try
            {
                r.AppendLine("Logical Drive Name: " + di.Name);
                r.AppendLine("Format: " + di.DriveFormat);
                r.AppendLine("Total Size: " + di.TotalSize);
                r.AppendLine("Total Free Space: " + di.TotalFreeSpace);
                r.AppendLine();
            }
            catch (IOException)
            { }
            catch (UnauthorizedAccessException)
            { }
        }

        return r.ToString();
    }

    public override void Traverse(IVisitor visitor)
    {
        foreach (ISensor sensor in Sensors)
            sensor.Accept(visitor);
    }

    /// <summary>
    /// Helper to calculate the disk performance with base timestamps
    /// https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-perfrawdata
    /// </summary>
    private class PerformanceValue
    {
        public double Result { get; private set; }

        private ulong Time { get; set; }

        private ulong Value { get; set; }

        public void Update(ulong val, ulong valBase)
        {
            ulong diffValue = val - Value;
            ulong diffTime = valBase - Time;

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
