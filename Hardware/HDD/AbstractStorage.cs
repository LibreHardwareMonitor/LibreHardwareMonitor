// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) 2009-2015 Michael Möller <mmoeller@openhardwaremonitor.org>
// Copyright (C) 2010 Paul Werelds
// Copyright (C) 2011 Roland Reinl <roland-reinl@gmx.de>
// Copyright (C) 2017 Alexander Thulcke <alexth4ef9@gmail.com>
// Copyright (C) 2016-2019 Sebastian Grams <https://github.com/sebastian-dev>
// Copyright (C) 2016-2019 Aqua Computer <https://github.com/aquacomputer, info@aqua-computer.de>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Management;
using System.Linq;

namespace OpenHardwareMonitor.Hardware.HDD {
  public abstract class AbstractStorage : Hardware {

    private const int UPDATE_DIVIDER = 30; // update only every 30s

    public string firmwareRevision { get; set; }
    public int index { get; set; }

    private int count { get; set; }
    private DriveInfo[] driveInfos { get; set; } = null;
    private StorageInfo storageInfo { get; } = null;
    private PerformanceValue perfTotal { get; } = new PerformanceValue();
    private PerformanceValue perfWrite { get; } = new PerformanceValue();
    private double lastTime { get; set; } = 0;
    private ulong readRateCounterlast { get; set; } = 0;
    private ulong writeRateCounterlast { get; set; } = 0;

    private Sensor usageSensor { get; set; } = null;
    private Sensor sensorDiskWriteActivity { get; set; } = null;
    private Sensor sensorDiskTotalActivity { get; set; } = null;
    private Sensor sensorDiskReadRate { get; set; } = null;
    private Sensor sensorDiskWriteRate { get; set; } = null;

    protected AbstractStorage(StorageInfo info, string name, string firmwareRevision, string id, int index, ISettings settings)
      : base(name, new Identifier(id, index.ToString(CultureInfo.InvariantCulture)), settings) {

      this.storageInfo = info;
      this.firmwareRevision = firmwareRevision;
      this.index = index;
      this.count = 0;

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

      if (info == null || info.Removable || info.BusType == Interop.StorageBusType.BusTypeVirtual || info.BusType == Interop.StorageBusType.BusTypeFileBackedVirtual)
        return null;

      if (info.BusType == Interop.StorageBusType.BusTypeAta || info.BusType == Interop.StorageBusType.BusTypeSata)
        return ATAStorage.CreateInstance(info, settings);

      if (info.BusType == Interop.StorageBusType.BusTypeNvme)
        return NVMeGeneric.CreateInstance(info, settings);

      return StorageGeneric.CreateInstance(info, settings);
    }

    protected virtual void CreateSensors() {
      if (driveInfos.Length > 0) {
        usageSensor = new Sensor("Used Space", 0, SensorType.Load, this, settings);
        ActivateSensor(usageSensor);
      }

      sensorDiskWriteActivity = new Sensor("Write Activity", 32, SensorType.Load, this, settings);
      sensorDiskTotalActivity = new Sensor("Total Activity", 33, SensorType.Load, this, settings);
      ActivateSensor(sensorDiskWriteActivity);
      ActivateSensor(sensorDiskTotalActivity);

      sensorDiskReadRate = new Sensor("Read Rate", 34, SensorType.Throughput, this, settings);
      sensorDiskWriteRate = new Sensor("Write Rate", 35, SensorType.Throughput, this, settings);
      ActivateSensor(sensorDiskReadRate);
      ActivateSensor(sensorDiskWriteRate);
    }

    public override HardwareType HardwareType {
      get { return HardwareType.HDD; }
    }

    protected abstract void UpdateSensors();


    public class PerformanceValue {
      public ulong perfValue { get; set; } = 0;
      public ulong perfValueBase { get; set; } = 0;
      public double result { get; set; } = 0;

      public void Update(ulong v, ulong vBase) {
        ulong diff_value = v - perfValue;
        ulong diff_timebase = vBase - perfValueBase;

        perfValue = v;
        perfValueBase = vBase;

        result = (100.0 / diff_timebase) * diff_value;

        //somtimes it is possible that diff_value > diff_timebase
        //limit result to 100%, this is because timing issues during read from pcie controller an latency between IO operation
        if (result > 100)
          result = 100;
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
      sensorDiskWriteActivity.Value = (float)(perfWrite.result);

      value = (ulong)data.Properties["PercentIdleTime"].Value;
      value_base = (ulong)data.Properties["PercentIdleTime_Base"].Value;
      perfTotal.Update(value, value_base);
      sensorDiskTotalActivity.Value = (float)(100.0 - perfTotal.result);

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
      lastTime = current_time;
      if (timedeltaSeconds > 0.2 && timedeltaSeconds < 5.0) {
        double writeSpeed = (double)writeRate;
        writeSpeed *= (1 / timedeltaSeconds);
        sensorDiskWriteRate.Value = (float)writeSpeed;

        double readSpeed = (double)readRate;
        readSpeed *= (1 / timedeltaSeconds);
        sensorDiskReadRate.Value = (float)readSpeed;
      }

      perfData.Dispose();
    }

    public override void Update() {

      //update statistics from WMI every time
      if (storageInfo != null) {
        try {
          UpdateStatisticsFromWmi(storageInfo.Index);
        }
        catch { }
      }

      //read out other data only 1/UPDATE_DIVIDER
      if (count == 0) {
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
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
          }
          if (totalSize > 0) {
            usageSensor.Value = 100.0f - (100.0f * totalFreeSpace) / totalSize;
          }
          else {
            usageSensor.Value = null;
          }
        }
      }
      count++;
      count %= UPDATE_DIVIDER;
    }

    protected abstract void GetReport(StringBuilder r);

    public override string GetReport() {
      StringBuilder r = new StringBuilder();
      r.AppendLine(this.GetType().Name);
      r.AppendLine();
      r.AppendLine("Drive name: " + name);
      r.AppendLine("Firmware version: " + firmwareRevision);
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