// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Management;
using System.Linq;
using OpenHardwareMonitor.Interop;

namespace OpenHardwareMonitor.Hardware.HDD {
  internal abstract class AbstractStorage : Hardware {
    private DateTime lastUpdate = DateTime.MinValue;
    private TimeSpan updateInterval = TimeSpan.FromSeconds(60);

    public string FirmwareRevision { get; set; }
    public int Index { get; set; }
    public DriveInfo[] driveInfos { get; private set; } = null;

    private StorageInfo storageInfo = null;
    private PerformanceValue perfTotal = new PerformanceValue();
    private PerformanceValue perfWrite = new PerformanceValue();
    private double lastTime = 0;
    private ulong readRateCounterlast = 0;
    private ulong writeRateCounterlast = 0;
    private Sensor usageSensor = null;
    private Sensor sensorDiskWriteActivity = null;
    private Sensor sensorDiskTotalActivity = null;
    private Sensor sensorDiskReadRate = null;
    private Sensor sensorDiskWriteRate = null;

    protected AbstractStorage(StorageInfo info, string name, string firmwareRevision, string id, int index, ISettings settings)
      : base(name, new Identifier(id, index.ToString(CultureInfo.InvariantCulture)), settings) {

      this.storageInfo = info;
      this.FirmwareRevision = firmwareRevision;
      this.Index = index;

      string[] logicalDrives = WindowsStorage.GetLogicalDrives(index);
      List<DriveInfo> driveInfoList = new List<DriveInfo>(logicalDrives.Length);
      foreach (string logicalDrive in logicalDrives) {
        try {
          DriveInfo di = new DriveInfo(logicalDrive);
          if (di.TotalSize > 0)
            driveInfoList.Add(new DriveInfo(logicalDrive));
        }
        catch (ArgumentException) {
        }
        catch (IOException) {
        }
        catch (UnauthorizedAccessException) {
        }
      }
      driveInfos = driveInfoList.ToArray();
    }

    public static AbstractStorage CreateInstance(string deviceId, uint driveNumber, ulong diskSize, int scsiPort, ISettings settings) {
      StorageInfo info = WindowsStorage.GetStorageInfo(deviceId, driveNumber);
      info.DiskSize = diskSize;
      info.DeviceId = deviceId;
      info.Scsi = string.Format(@"\\.\SCSI{0}:", scsiPort);

      if (info == null || info.Removable || info.BusType == Kernel32.StorageBusType.BusTypeVirtual || info.BusType == Kernel32.StorageBusType.BusTypeFileBackedVirtual)
        return null;

      //fallback, when it is not possible to read out with the nvme implementation,
      //try it with the sata smart implementation
      if (info.BusType == Kernel32.StorageBusType.BusTypeNvme) {
        var x = NVMeGeneric.CreateInstance(info, settings);
        if (x != null)
          return x;
      }

      if (info.BusType == Kernel32.StorageBusType.BusTypeAta ||
        info.BusType == Kernel32.StorageBusType.BusTypeSata ||
        info.BusType == Kernel32.StorageBusType.BusTypeNvme) {
        return ATAStorage.CreateInstance(info, settings);
      }
      return StorageGeneric.CreateInstance(info, settings);
    }

    protected virtual void CreateSensors() {
      if (driveInfos.Length > 0) {
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

    public override HardwareType HardwareType {
      get { return HardwareType.HDD; }
    }

    protected abstract void UpdateSensors();

    /// <summary>
    /// Helper to calculate the disk performance with base timestamps
    /// https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-perfrawdata
    /// </summary>
    internal class PerformanceValue {
      public ulong PerfValue { get; set; } = 0;
      public ulong PerfValueBase { get; set; } = 0;
      public double Result { get; set; } = 0;

      public void Update(ulong v, ulong vBase) {
        ulong diff_value = v - PerfValue;
        ulong diff_timebase = vBase - PerfValueBase;

        PerfValue = v;
        PerfValueBase = vBase;
        Result = (100.0 / diff_timebase) * diff_value;

        //sometimes it is possible that diff_value > diff_timebase
        //limit result to 100%, this is because timing issues during read from pcie controller an latency between IO operation
        if (Result > 100)
          Result = 100;
      }
    }

    private void UpdateStatisticsFromWmi(int driveIndex) {
      string query = string.Format("SELECT * FROM Win32_PerfRawData_PerfDisk_PhysicalDisk Where Name LIKE \"{0}%\"", driveIndex);
      ManagementObjectSearcher perfData = new ManagementObjectSearcher(query);
      var data = (from ManagementObject x in perfData.Get()
                  select x).FirstOrDefault();

      if (data == null) {
        perfData.Dispose();
        return;
      }

      ulong value;
      ulong value_base;

      value = (ulong)data.Properties["PercentDiskWriteTime"].Value;
      value_base = (ulong)data.Properties["PercentDiskWriteTime_Base"].Value;
      perfWrite.Update(value, value_base);
      sensorDiskWriteActivity.Value = (float)(perfWrite.Result);

      value = (ulong)data.Properties["PercentIdleTime"].Value;
      value_base = (ulong)data.Properties["PercentIdleTime_Base"].Value;
      perfTotal.Update(value, value_base);
      sensorDiskTotalActivity.Value = (float)(100.0 - perfTotal.Result);

      ulong readRateCounter = (ulong)data.Properties["DiskReadBytesPerSec"].Value;
      ulong readRate = readRateCounter - readRateCounterlast;
      readRateCounterlast = readRateCounter;

      ulong writeRateCounter = (ulong)data.Properties["DiskWriteBytesPerSec"].Value;
      ulong writeRate = writeRateCounter - writeRateCounterlast;
      writeRateCounterlast = writeRateCounter;

      ulong Timestamp_PerfTime = (ulong)data.Properties["Timestamp_PerfTime"].Value;
      ulong Frequency_Perftime = value_base = (ulong)data.Properties["Frequency_Perftime"].Value;
      double current_time = (double)Timestamp_PerfTime / Frequency_Perftime;

      double timedeltaSeconds = current_time - lastTime;
      if (lastTime == 0 || timedeltaSeconds > 0.2) {
        double writeSpeed = (double)writeRate;
        writeSpeed *= (1 / timedeltaSeconds);
        sensorDiskWriteRate.Value = (float)writeSpeed;

        double readSpeed = (double)readRate;
        readSpeed *= (1 / timedeltaSeconds);
        sensorDiskReadRate.Value = (float)readSpeed;
      }

      if (lastTime == 0 || timedeltaSeconds > 0.2) {
        lastTime = current_time;
      }

      perfData.Dispose();
    }

    public override void Update() {

      //update statistics from WMI on every update
      if (storageInfo != null) {
        try {
          UpdateStatisticsFromWmi(storageInfo.Index);
        }
        catch { }
      }

      //read out with updateInterval
      var tDiff = DateTime.UtcNow - lastUpdate;
      if(tDiff > updateInterval) {
        lastUpdate = DateTime.UtcNow;

        UpdateSensors();

        if (usageSensor != null) {
          long totalSize = 0;
          long totalFreeSpace = 0;

          for (int i = 0; i < driveInfos.Length; i++) {
            if (!driveInfos[i].IsReady)
              continue;
            try {
              totalSize += driveInfos[i].TotalSize;
              totalFreeSpace += driveInfos[i].TotalFreeSpace;
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
      StringBuilder r = new StringBuilder();
      r.AppendLine("STORAGE");
      r.AppendLine();
      r.AppendLine("Drive name: " + name);
      r.AppendLine("Firmware version: " + FirmwareRevision);
      r.AppendLine();
      GetReport(r);

      foreach (DriveInfo di in driveInfos) {
        if (!di.IsReady)
          continue;
        try {
          r.AppendLine("Logical drive name: " + di.Name);
          r.AppendLine("Format: " + di.DriveFormat);
          r.AppendLine("Total size: " + di.TotalSize);
          r.AppendLine("Total free space: " + di.TotalFreeSpace);
          r.AppendLine();
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
      }
      return r.ToString();
    }

    public override void Traverse(IVisitor visitor) {
      foreach (ISensor sensor in Sensors)
        sensor.Accept(visitor);
    }
  }
}