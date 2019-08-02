// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System.Runtime.InteropServices;
using OpenHardwareMonitor.Interop;

namespace OpenHardwareMonitor.Hardware.RAM {
  internal class GenericRAM : Hardware {

    private Sensor physicalMemoryUsed;
    private Sensor physicalMemoryAvailable;
    private Sensor physicalMemoryLoad;
    private Sensor virtualMemoryUsed;
    private Sensor virtualMemoryAvailable;
    private Sensor virtualMemoryLoad;

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
      Kernel32.MemoryStatusEx status = new Kernel32.MemoryStatusEx();
      status.Length = (uint)Marshal.SizeOf<Kernel32.MemoryStatusEx>();

      if (!Kernel32.GlobalMemoryStatusEx(ref status))
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