// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) 2017 Alexander Thulcke <alexth4ef9@gmail.com>
// Copyright (C) 2016-2019 Sebastian Grams <https://github.com/sebastian-dev>
// Copyright (C) 2016-2019 Aqua Computer <https://github.com/aquacomputer, info@aqua-computer.de>

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace OpenHardwareMonitor.Hardware.HDD {

  public abstract class NVMeInfo {
    public int Index { get; protected set; }
    public ushort VID { get; protected set; }
    public ushort SSVID { get; protected set; }
    public string Serial { get; protected set; }
    public string Model { get; protected set; }
    public string Revision { get; protected set; }
    public byte[] IEEE { get; protected set; }
    public ulong TotalCapacity { get; protected set; }
    public ulong UnallocatedCapacity { get; protected set; }
    public ushort ControllerId { get; protected set; }
    public uint NumberNamespaces { get; protected set; }
  }
}