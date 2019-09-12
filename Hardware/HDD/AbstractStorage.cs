// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using OpenHardwareMonitor.Interop;

namespace OpenHardwareMonitor.Hardware.HDD {
  public abstract class AbstractStorage : Hardware {
    private readonly PerformanceValue perfTotal = new PerformanceValue();
    private readonly PerformanceValue perfWrite = new PerformanceValue();
    private readonly StorageInfo storageInfo;
    private readonly TimeSpan updateInterval = TimeSpan.FromSeconds(60);
    private bool canUpdateWmi = true;

    private double lastTime;
    private DateTime lastUpdate = DateTime.MinValue;
    private ulong readRateCounterlast;
    private Sensor sensorDiskReadRate;
    private Sensor sensorDiskTotalActivity;
    private Sensor sensorDiskWriteActivity;
    private Sensor sensorDiskWriteRate;
    private Sensor usageSensor;
    private ulong writeRateCounterlast;

    internal AbstractStorage(StorageInfo storageInfo, string name, string firmwareRevision, string id, int index, ISettings settings)
      : base(name, new Identifier(id, index.ToString(CultureInfo.InvariantCulture)), settings) {
      this.storageInfo = storageInfo;
      FirmwareRevision = firmwareRevision;
      Index = index;

      string[] logicalDrives = WindowsStorage.GetLogicalDrives(index);
      var driveInfoList = new List<DriveInfo>(logicalDrives.Length);
      foreach (string logicalDrive in logicalDrives) {
        try {
          var di = new DriveInfo(logicalDrive);
          if (di.TotalSize > 0)
            driveInfoList.Add(new DriveInfo(logicalDrive));
        } catch (ArgumentException) { } catch (IOException) { } catch (UnauthorizedAccessException) { }
      }

      DriveInfos = driveInfoList.ToArray();
    }

    public DriveInfo[] DriveInfos { get; }

    public string FirmwareRevision { get; }

    public override HardwareType HardwareType => HardwareType.HDD;

    public int Index { get; }

    public static AbstractStorage CreateInstance(string deviceId, uint driveNumber, ulong diskSize, int scsiPort, ISettings settings) {
      StorageInfo info = WindowsStorage.GetStorageInfo(deviceId, driveNumber);
      info.DiskSize = diskSize;
      info.DeviceId = deviceId;
      info.Scsi = $@"\\.\SCSI{scsiPort}:";

      if (info.Removable || info.BusType == Kernel32.STORAGE_BUS_TYPE.BusTypeVirtual || info.BusType == Kernel32.STORAGE_BUS_TYPE.BusTypeFileBackedVirtual)
        return null;


      //fallback, when it is not possible to read out with the nvme implementation,
      //try it with the sata smart implementation
      if (info.BusType == Kernel32.STORAGE_BUS_TYPE.BusTypeNvme) {
        var x = NVMeGeneric.CreateInstance(info, settings);
        if (x != null)
          return x;
      }

      if (info.BusType == Kernel32.STORAGE_BUS_TYPE.BusTypeAta ||
          info.BusType == Kernel32.STORAGE_BUS_TYPE.BusTypeSata ||
          info.BusType == Kernel32.STORAGE_BUS_TYPE.BusTypeNvme) {
        return ATAStorage.CreateInstance(info, settings);
      }

      return StorageGeneric.CreateInstance(info, settings);
    }

    protected virtual void CreateSensors() {
      if (DriveInfos.Length > 0) {
        usageSensor = new Sensor("Used Space", 0, SensorType.Load, this, settings);
        ActivateSensor(usageSensor);
      }

      sensorDiskWriteActivity = new Sensor("Write Activity", 32, SensorType.Load, this, settings);
      ActivateSensor(sensorDiskWriteActivity);

      sensorDiskTotalActivity = new Sensor("Total Activity", 33, SensorType.Load, this, settings);
      ActivateSensor(sensorDiskTotalActivity);

      sensorDiskReadRate = new Sensor("Read Rate", 34, SensorType.Throughput, this, settings);
      ActivateSensor(sensorDiskReadRate);

      sensorDiskWriteRate = new Sensor("Write Rate", 35, SensorType.Throughput, this, settings);
      ActivateSensor(sensorDiskWriteRate);
    }

    protected abstract void UpdateSensors();

    private void UpdateStatisticsFromWmi(int driveIndex) {
      string query = $"SELECT * FROM Win32_PerfRawData_PerfDisk_PhysicalDisk Where Name LIKE \"{driveIndex}%\"";

      var perfData = new ManagementObjectSearcher(query);
      var data = perfData.Get().OfType<ManagementObject>().FirstOrDefault();

      if (data == null) {
        perfData.Dispose();
        return;
      }

      var value = (ulong) data.Properties["PercentDiskWriteTime"].Value;
      var valueBase = (ulong) data.Properties["PercentDiskWriteTime_Base"].Value;
      perfWrite.Update(value, valueBase);
      sensorDiskWriteActivity.Value = (float) (perfWrite.Result);

      value = (ulong) data.Properties["PercentIdleTime"].Value;
      valueBase = (ulong) data.Properties["PercentIdleTime_Base"].Value;
      perfTotal.Update(value, valueBase);
      sensorDiskTotalActivity.Value = (float) (100.0 - perfTotal.Result);

      ulong readRateCounter = (ulong) data.Properties["DiskReadBytesPerSec"].Value;
      ulong readRate = readRateCounter - readRateCounterlast;
      readRateCounterlast = readRateCounter;

      ulong writeRateCounter = (ulong) data.Properties["DiskWriteBytesPerSec"].Value;
      ulong writeRate = writeRateCounter - writeRateCounterlast;
      writeRateCounterlast = writeRateCounter;

      ulong timestampPerfTime = (ulong) data.Properties["Timestamp_PerfTime"].Value;
      ulong frequencyPerfTime = (ulong) data.Properties["Frequency_Perftime"].Value;
      double currentTime = (double) timestampPerfTime / frequencyPerfTime;

      double timeDeltaSeconds = currentTime - lastTime;
      if (lastTime == 0 || timeDeltaSeconds > 0.2) {
        double writeSpeed = writeRate;
        writeSpeed *= (1 / timeDeltaSeconds);
        sensorDiskWriteRate.Value = (float) writeSpeed;

        double readSpeed = readRate;
        readSpeed *= (1 / timeDeltaSeconds);
        sensorDiskReadRate.Value = (float) readSpeed;
      }

      if (lastTime == 0 || timeDeltaSeconds > 0.2) {
        lastTime = currentTime;
      }

      perfData.Dispose();
    }

    public override void Update() {
      //update statistics from WMI on every update
      if (storageInfo != null && canUpdateWmi) {
        try {
          UpdateStatisticsFromWmi(storageInfo.Index);
        } catch (ManagementException managementException) {
          // Invalid query.
          if (managementException.HResult == -2146233087) {
            DeactivateSensor(sensorDiskTotalActivity);
            DeactivateSensor(sensorDiskWriteActivity);
            DeactivateSensor(sensorDiskReadRate);
            DeactivateSensor(sensorDiskWriteRate);

            canUpdateWmi = false;
          }
        } catch { }
      }

      //read out with updateInterval
      var tDiff = DateTime.UtcNow - lastUpdate;
      if (tDiff > updateInterval) {
        lastUpdate = DateTime.UtcNow;

        UpdateSensors();

        if (usageSensor != null) {
          long totalSize = 0;
          long totalFreeSpace = 0;

          for (int i = 0; i < DriveInfos.Length; i++) {
            if (!DriveInfos[i].IsReady)
              continue;


            try {
              totalSize += DriveInfos[i].TotalSize;
              totalFreeSpace += DriveInfos[i].TotalFreeSpace;
            } catch (IOException) { } catch (UnauthorizedAccessException) { }
          }

          if (totalSize > 0) {
            usageSensor.Value = 100.0f - (100.0f * totalFreeSpace) / totalSize;
          } else {
            usageSensor.Value = null;
          }
        }
      }
    }

    protected abstract void GetReport(StringBuilder r);

    public override string GetReport() {
      var r = new StringBuilder();
      r.AppendLine("Storage");
      r.AppendLine();
      r.AppendLine("Drive Name: " + name);
      r.AppendLine("Firmware Version: " + FirmwareRevision);
      r.AppendLine();
      GetReport(r);

      foreach (DriveInfo di in DriveInfos) {
        if (!di.IsReady)
          continue;


        try {
          r.AppendLine("Logical Drive Name: " + di.Name);
          r.AppendLine("Format: " + di.DriveFormat);
          r.AppendLine("Total Size: " + di.TotalSize);
          r.AppendLine("Total Free Space: " + di.TotalFreeSpace);
          r.AppendLine();
        } catch (IOException) { } catch (UnauthorizedAccessException) { }
      }

      return r.ToString();
    }

    public override void Traverse(IVisitor visitor) {
      foreach (ISensor sensor in Sensors)
        sensor.Accept(visitor);
    }

    /// <summary>
    /// Helper to calculate the disk performance with base timestamps
    /// https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-perfrawdata
    /// </summary>
    private class PerformanceValue {
      public double Result { get; private set; }

      private ulong Time { get; set; }

      private ulong Value { get; set; }

      public void Update(ulong val, ulong valBase) {
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
}