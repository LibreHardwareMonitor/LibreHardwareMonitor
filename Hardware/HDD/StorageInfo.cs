// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Collections.Generic;
using System.IO;
using OpenHardwareMonitor.Interop;

namespace OpenHardwareMonitor.Hardware.HDD {

  internal abstract class StorageInfo {
    public string DeviceId { get; set; }
    public string Scsi { get; set; }
    public int Index { get; protected set; }
    public string Vendor { get; protected set; }
    public string Product { get; protected set; }
    public string Revision { get; protected set; }
    public string Serial { get; protected set; }
    public Kernel32.StorageBusType BusType { get; protected set; }
    public bool Removable { get; protected set; }
    public ulong DiskSize { get; set; }
    public string Name {
      get { return (Vendor + " " + Product).Trim(); }
    }
    public byte[] RawData { get; protected set; }
  }
}