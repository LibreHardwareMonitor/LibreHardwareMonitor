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
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace OpenHardwareMonitor.Hardware.HDD {

  public abstract class StorageInfo {
    public string DeviceId { get; set; }
    public string Scsi { get; set; }
    public int Index { get; protected set; }
    public string Vendor { get; protected set; }
    public string Product { get; protected set; }
    public string Revision { get; protected set; }
    public string Serial { get; protected set; }
    public Interop.StorageBusType BusType { get; protected set; }
    public bool Removable { get; protected set; }
    public ulong DiskSize { get; set; }
    public string Name {
      get { return (Vendor + " " + Product).Trim(); }
    }
    public byte[] RawData { get; protected set; }
  }
}