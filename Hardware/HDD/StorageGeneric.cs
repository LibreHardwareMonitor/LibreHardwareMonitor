// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) 2017 Alexander Thulcke <alexth4ef9@gmail.com>

using System;
using System.Text;

namespace OpenHardwareMonitor.Hardware.HDD {
  internal sealed class StorageGeneric : AbstractStorage {
    private StorageGeneric(StorageInfo _storageInfo, string name, string firmwareRevision, int index, ISettings settings)
      : base(_storageInfo, name, firmwareRevision, "hdd", index, settings)
    {
      CreateSensors();
    }

    public static AbstractStorage CreateInstance(StorageInfo info, ISettings settings) {
      string name = string.IsNullOrEmpty(info.Name) ? "Generic Hard Disk" : info.Name;
      string firmwareRevision = string.IsNullOrEmpty(info.Revision) ? "Unknown" : info.Revision;
      return new StorageGeneric(info, name, firmwareRevision, info.Index, settings);
    }

    protected override void UpdateSensors() {
    }

    protected override void GetReport(StringBuilder r) {
    }
  }
}