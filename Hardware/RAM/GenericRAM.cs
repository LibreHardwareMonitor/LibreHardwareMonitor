// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) 2011-2012 Michael Möller <mmoeller@openhardwaremonitor.org>
// Copyright (C) 2016-2019 Sebastian Grams <https://github.com/sebastian-dev>
// Copyright (C) 2016-2019 Aqua Computer <https://github.com/aquacomputer, info@aqua-computer.de>

using System.Runtime.InteropServices;

namespace OpenHardwareMonitor.Hardware.RAM {
  internal class GenericRAM : Hardware {

    private Sensor physicalMemoryUsed { get; set; } = null;
    private Sensor physicalMemoryAvailable { get; set; } = null;
    private Sensor physicalMemoryLoad { get; set; } = null;
    private Sensor virtualMemoryUsed { get; set; } = null;
    private Sensor virtualMemoryAvailable { get; set; } = null;
    private Sensor virtualMemoryLoad { get; set; } = null;

    public GenericRAM(string name, ISettings settings)
      : base(name, new Identifier("ram"), settings) {

      physicalMemoryUsed = new Sensor("Memory Used", 0, SensorType.Data, this, settings);
      ActivateSensor(physicalMemoryUsed);

      physicalMemoryAvailable = new Sensor("Memory Available", 1, SensorType.Data, this, settings);
      ActivateSensor(physicalMemoryAvailable);

      physicalMemoryLoad = new Sensor("Memory", 0, SensorType.Load, this, settings);
      ActivateSensor(physicalMemoryLoad);

      virtualMemoryUsed = new Sensor("Virtual Memory Used", 2, SensorType.Data, this, settings);
      ActivateSensor(virtualMemoryUsed);

      virtualMemoryAvailable = new Sensor("Virtual Memory Available", 3, SensorType.Data, this, settings);
      ActivateSensor(virtualMemoryAvailable);

      virtualMemoryLoad = new Sensor("Virtual Memory", 1, SensorType.Load, this, settings);
      ActivateSensor(virtualMemoryLoad);
    }

    public override HardwareType HardwareType {
      get {
        return HardwareType.RAM;
      }
    }

    public override void Update() {
      Interop.MemoryStatusEx status = new Interop.MemoryStatusEx();
      status.Length = (uint)Marshal.SizeOf<Interop.MemoryStatusEx>();

      if (!Interop.GlobalMemoryStatusEx(ref status))
        return;

      physicalMemoryUsed.Value = (float)(status.TotalPhysicalMemory - status.AvailablePhysicalMemory) / (1024 * 1024 * 1024);
      physicalMemoryAvailable.Value = (float)status.AvailablePhysicalMemory / (1024 * 1024 * 1024);
      physicalMemoryLoad.Value = 100.0f - (100.0f * status.AvailablePhysicalMemory) / status.TotalPhysicalMemory;

      virtualMemoryUsed.Value = (float)(status.TotalPageFile - status.AvailPageFile) / (1024 * 1024 * 1024);
      virtualMemoryAvailable.Value = (float)status.AvailPageFile / (1024 * 1024 * 1024);
      virtualMemoryLoad.Value = 100.0f - (100.0f * status.AvailPageFile) / status.TotalPageFile;
    }
  }
}