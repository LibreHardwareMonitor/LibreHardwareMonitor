// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Collections.Generic;
using System.Management;

namespace OpenHardwareMonitor.Hardware.HDD {
  internal class HarddriveGroup : IGroup {
    private readonly List<AbstractStorage> hardware = new List<AbstractStorage>();

    public HarddriveGroup(ISettings settings) {
      if (Software.OperatingSystem.IsLinux) return;


      //https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-diskdrive
      var mosDisks = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
      ManagementObjectCollection queryCollection = mosDisks.Get(); // get the results

      foreach (var disk in queryCollection) {
        var deviceId = (string) disk.Properties["DeviceId"].Value; // is \\.\PhysicalDrive0..n
        var idx = Convert.ToUInt32(disk.Properties["Index"].Value);
        var diskSize = Convert.ToUInt64(disk.Properties["Size"].Value);
        var scsi = Convert.ToInt32(disk.Properties["SCSIPort"].Value);

        if (deviceId != null) {
          var instance = AbstractStorage.CreateInstance(deviceId, idx, diskSize, scsi, settings);
          if (instance != null) {
            hardware.Add(instance);
          }
        }
      }

      queryCollection.Dispose();
      mosDisks.Dispose();
    }

    public IEnumerable<IHardware> Hardware => hardware;

    public string GetReport() {
      return null;
    }

    public void Close() {
      foreach (AbstractStorage storage in hardware)
        storage.Close();
    }
  }
}