/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2017 Alexander Thulcke <alexth4ef9@gmail.com>
  
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace OpenHardwareMonitor.Hardware.HDD {
  internal sealed class NVMeGeneric : AbstractStorage {
    private delegate float GetSensorValue(NVMeHealthInfo health);
    
    private class NVMeSensor : Sensor {
      private readonly GetSensorValue getValue;
      
      public NVMeSensor(string name, int index, bool defaultHidden, 
        SensorType sensorType, Hardware hardware, ISettings settings,
        GetSensorValue getValue)
        : base(name, index, defaultHidden, sensorType, hardware, null, settings)
      {        
        this.getValue = getValue;
      }

      public void Update(NVMeHealthInfo health) {
        Value = getValue(health);
      }
    }

    private const int MAX_DRIVES = 32;
    
    private static int nextDrive;
    private readonly WindowsNVMeSmart smart;
    private readonly NVMeInfo info;
    private List<NVMeSensor> sensors = new List<NVMeSensor>();
        
    private NVMeGeneric(NVMeInfo info, int index, ISettings settings)
      : base(info.Model, info.Revision, "nvme", index, settings)
    {
      this.smart = new WindowsNVMeSmart(info.Index);
      this.info = info;
      CreateSensors();
    }
     
    // \\.\ScsiY: is different from \\.\PhysicalDriveX
    private static NVMeInfo GetDeviceInfo() {
      while (nextDrive < MAX_DRIVES) {
        using(WindowsNVMeSmart smart = new WindowsNVMeSmart(nextDrive)) {
          nextDrive++;
          if (!smart.IsValid)
            continue;
          NVMeInfo info = smart.GetInfo();
          if (info != null)
            return info;
        }
      }
      return null;
    }
    
    public static AbstractStorage CreateInstance(StorageInfo storageInfo, ISettings settings) {
      NVMeInfo nvmeInfo = GetDeviceInfo();
      if (nvmeInfo == null)
        return null;
      return new NVMeGeneric(nvmeInfo, storageInfo.Index, settings);
    }
            
    protected override void CreateSensors() {
      AddSensor("Temperature", 0, false, SensorType.Temperature, (health) => health.Temperature);
      AddSensor("Available Spare", 0, false, SensorType.Level, (health) => health.AvailableSpare);
      AddSensor("Available Spare Threshold", 1, false, SensorType.Level, (health) => health.AvailableSpareThreshold);
      AddSensor("Percentage Used", 2, false, SensorType.Level, (health) => health.PercentageUsed);
      AddSensor("Data Read", 1, false, SensorType.Data, (health) => UnitsToData(health.DataUnitRead));
      AddSensor("Data Written", 2, false, SensorType.Data, (health) => UnitsToData(health.DataUnitWritten));
      NVMeHealthInfo log = smart.GetHealthInfo();
      for (int i=0; i<log.TemperatureSensors.Length; i++) {
        if (log.TemperatureSensors[i] > short.MinValue) { 
          int idx = i;
          AddSensor("Temperature", i + 1, true, SensorType.Temperature, (health) => health.TemperatureSensors[idx]);
        }
      }

      base.CreateSensors();
    }
    
    private void AddSensor(string name, int index, bool defaultHidden, 
      SensorType sensorType, GetSensorValue getValue)
    {
      NVMeSensor sensor = new NVMeSensor(name, index, defaultHidden, sensorType, this, settings, getValue);
      ActivateSensor(sensor);
      sensors.Add(sensor);
    }

    static readonly ulong Units = 512;
    static readonly ulong Scale = 1000000;
    
    private static float UnitsToData(ulong u) {
      // one unit is 512 * 1000 bytes, return in GB (not GiB)
      return ((Units * u) / Scale);
    }
    
    public override void UpdateSensors() {
      NVMeHealthInfo health = smart.GetHealthInfo();
      foreach(NVMeSensor sensor in sensors)
        sensor.Update(health);
    }
    
    public override void GetReport(StringBuilder r) {
      r.AppendLine("PCI Vendor ID: 0x" + info.VID.ToString("x04"));
      if (info.VID != info.SSVID)
        r.AppendLine("PCI Subsystem Vendor ID: 0x" + info.VID.ToString("x04"));
      r.AppendLine("IEEE OUI Identifier: 0x" + info.IEEE[2].ToString("x02") + info.IEEE[1].ToString("x02") + info.IEEE[0].ToString("x02"));
      r.AppendLine("Total NVM Capacity: " + info.TotalCapacity);
      r.AppendLine("Unallocated NVM Capacity: " + info.UnallocatedCapacity);
      r.AppendLine("Controller ID: " + info.ControllerId);
      r.AppendLine("Number of Namespaces: " + info.NumberNamespaces);
      if (info.Namespace1 != null) {
        r.AppendLine("Namespace 1 Size: " + info.Namespace1.Size);
        r.AppendLine("Namespace 1 Capacity: " + info.Namespace1.Capacity);
        r.AppendLine("Namespace 1 Utilization: " + info.Namespace1.Utilization);
        r.AppendLine("Namespace 1 LBA Data Size: " + info.Namespace1.LBADataSize);
      }
      
      NVMeHealthInfo health = smart.GetHealthInfo();
      if (health.CriticalWarning == NVMeCriticalWarning.None)
        r.AppendLine("Critical Warning: -");
      else {
        if ((health.CriticalWarning & NVMeCriticalWarning.AvailableSpaceLow) != 0)
          r.AppendLine("Critical Warning: the available spare space has fallen below the threshold.");
        if ((health.CriticalWarning & NVMeCriticalWarning.TemperatureThreshold) != 0)
          r.AppendLine("Critical Warning: a temperature is above an over temperature threshold or below an under temperature threshold.");
        if ((health.CriticalWarning & NVMeCriticalWarning.ReliabilityDegraded) != 0)
          r.AppendLine("Critical Warning: the device reliability has been degraded due to significant media related errors or any internal error that degrades device reliability.");
        if ((health.CriticalWarning & NVMeCriticalWarning.ReadOnly) != 0)
          r.AppendLine("Critical Warning: the media has been placed in read only mode.");
        if ((health.CriticalWarning & NVMeCriticalWarning.VolatileMemoryBackupDeviceFailed) != 0)
          r.AppendLine("Critical Warning: the volatile memory backup device has failed.");
      }
      r.AppendLine("Temperature: " + health.Temperature +" Celsius");
      r.AppendLine("Available Spare: " + health.AvailableSpare +"%");
      r.AppendLine("Available Spare Threshold: " + health.AvailableSpareThreshold +"%");
      r.AppendLine("Percentage Used: " + health.PercentageUsed + "%");
      r.AppendLine("Data Units Read: " + health.DataUnitRead);
      r.AppendLine("Data Units Written: " + health.DataUnitWritten);
      r.AppendLine("Host Read Commands: " + health.HostReadCommands);
      r.AppendLine("Host Write Commands: " + health.HostWriteCommands);
      r.AppendLine("Controller Busy Time: " + health.ControllerBusyTime);
      r.AppendLine("Power Cycles: " + health.PowerCycle);
      r.AppendLine("Power On Hours: " + health.PowerOnHours);
      r.AppendLine("Unsafe Shutdowns: " + health.UnsafeShutdowns);
      r.AppendLine("Media Errors: " + health.MediaErrors);
      r.AppendLine("Number of Error Information Log Entries: " + health.ErrorInfoLogEntryCount);
      r.AppendLine("Warning Composite Temperature Time: " + health.WarningCompositeTemperatureTime);
      r.AppendLine("Critical Composite Temperature Time: " + health.CriticalCompositeTemperatureTime);
      for (int i=0; i<health.TemperatureSensors.Length; i++) {
        if (health.TemperatureSensors[i] > short.MinValue)
          r.AppendLine("Temperature Sensor " + (i + 1) + ": " + health.TemperatureSensors[i] + " Celsius");
      }                                               
    }

    public override void Close() {
      smart.Close();
      
      base.Close();
    }    
  }
}
