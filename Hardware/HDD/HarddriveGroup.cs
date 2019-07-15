// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) 2009-2011 Michael Möller <mmoeller@openhardwaremonitor.org>
// Copyright (C) 2010 Paul Werelds
// Copyright (C) 2011 Roland Reinl <roland-reinl@gmx.de>
// Copyright (C) 2016-2019 Sebastian Grams <https://github.com/sebastian-dev>
// Copyright (C) 2016-2019 Aqua Computer <https://github.com/aquacomputer, info@aqua-computer.de>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Management;

namespace OpenHardwareMonitor.Hardware.HDD {
  internal class HarddriveGroup : IGroup {
    private readonly List<AbstractStorage> hardware = new List<AbstractStorage>();

    public HarddriveGroup(ISettings settings) {

      if (Software.OperatingSystem.IsLinux) return;

      //https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-diskdrive
      ManagementObjectSearcher mosDisks = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
      ManagementObjectCollection queryCollection = mosDisks.Get(); // get the results

      foreach (var disk in queryCollection) {
        var deviceId = (string)disk.Properties["DeviceId"].Value; // is \\.\PhysicalDrive0..n
        var idx = Convert.ToUInt32(disk.Properties["Index"].Value);
        var diskSize = Convert.ToUInt64(disk.Properties["Size"].Value);
        var scsi = Convert.ToInt32(disk.Properties["SCSIPort"].Value);

        if (deviceId != null) {
          AbstractStorage instance = AbstractStorage.CreateInstance(deviceId, idx, diskSize, scsi, settings);
          if (instance != null) {
            this.hardware.Add(instance);
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