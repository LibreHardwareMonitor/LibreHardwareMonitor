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

  public abstract class NVMeHealthInfo {
    public Interop.NVMeCriticalWarning CriticalWarning { get; protected set; }
    public short Temperature { get; protected set; }
    public byte AvailableSpare { get; protected set; }
    public byte AvailableSpareThreshold { get; protected set; }
    public byte PercentageUsed { get; protected set; }
    public ulong DataUnitRead { get; protected set; }
    public ulong DataUnitWritten { get; protected set; }
    public ulong HostReadCommands { get; protected set; }
    public ulong HostWriteCommands { get; protected set; }
    public ulong ControllerBusyTime { get; protected set; }
    public ulong PowerCycle { get; protected set; }
    public ulong PowerOnHours { get; protected set; }
    public ulong UnsafeShutdowns { get; protected set; }
    public ulong MediaErrors { get; protected set; }
    public ulong ErrorInfoLogEntryCount { get; protected set; }
    public uint WarningCompositeTemperatureTime { get; protected set; }
    public uint CriticalCompositeTemperatureTime { get; protected set; }
    public short[] TemperatureSensors { get; protected set; }
  }
}