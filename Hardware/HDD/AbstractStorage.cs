/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2009-2015 Michael Möller <mmoeller@openhardwaremonitor.org>
	Copyright (C) 2010 Paul Werelds
  Copyright (C) 2011 Roland Reinl <roland-reinl@gmx.de>
	
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace OpenHardwareMonitor.Hardware.HDD {
  public abstract class AbstractStorage : Hardware {

    private const int UPDATE_DIVIDER = 30; // update only every 30s

    protected string firmwareRevision;
    
    protected readonly int index;
    private int count;
    
    private DriveInfo[] driveInfos;
    private Sensor usageSensor;

    protected AbstractStorage(string name, string firmwareRevision, 
      string id, int index, ISettings settings) 
      : base(name, new Identifier(id,
        index.ToString(CultureInfo.InvariantCulture)), settings)
    {
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
        } catch (ArgumentException) {
        } catch (IOException) {
        } catch (UnauthorizedAccessException) {
        }
      }
      driveInfos = driveInfoList.ToArray();
    }

    public static AbstractStorage CreateInstance(int driveNumber, ISettings settings) {
      StorageInfo info = WindowsStorage.GetStorageInfo(driveNumber);
      if (info == null || info.Removable)
        return null;
      if (info.BusType == StorageBusType.BusTypeAta || info.BusType == StorageBusType.BusTypeSata)
        return ATAStorage.CreateInstance(info, settings);
      if (info.BusType == StorageBusType.BusTypeNvme)
        return NVMeGeneric.CreateInstance(info, settings);
      return StorageGeneric.CreateInstance(info, settings);
    }
    
    protected virtual void CreateSensors() {
      if (driveInfos.Length > 0) {
        usageSensor = 
          new Sensor("Used Space", 0, SensorType.Load, this, settings);
        ActivateSensor(usageSensor);
      }
    }

    public override HardwareType HardwareType {
      get { return HardwareType.HDD; }
    }

    public abstract void UpdateSensors();
    
    public override void Update() {
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
            } catch (IOException) { } catch (UnauthorizedAccessException) { }
          }
          if (totalSize > 0) {
            usageSensor.Value = 100.0f - (100.0f * totalFreeSpace) / totalSize;
          } else {
            usageSensor.Value = null;
          }
        }
      }

      count++; 
      count %= UPDATE_DIVIDER; 
    }

    public abstract void GetReport(StringBuilder r);
    
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
        } catch (IOException) { } catch (UnauthorizedAccessException) { }
      }

      return r.ToString();
    }

    public override void Traverse(IVisitor visitor) {
      foreach (ISensor sensor in Sensors)
        sensor.Accept(visitor);
    }
  }
}
