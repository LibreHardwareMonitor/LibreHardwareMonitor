// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware.Memory;

internal sealed class GenericMemory : Hardware
{
    public GenericMemory(ISettings settings)
        : base("Memory", new Identifier("ram"), settings)
    {
        // Physical Memory Sensors (indices 0-1 for Data, 0 for Load)
        PhysicalMemoryUsed = new Sensor("Memory Used", 0, SensorType.Data, this, settings);
        ActivateSensor(PhysicalMemoryUsed);

        PhysicalMemoryAvailable = new Sensor("Memory Available", 1, SensorType.Data, this, settings);
        ActivateSensor(PhysicalMemoryAvailable);

        PhysicalMemoryLoad = new Sensor("Memory", 0, SensorType.Load, this, settings);
        ActivateSensor(PhysicalMemoryLoad);

        // Virtual Memory Sensors (indices 2-3 for Data, 1 for Load)
        VirtualMemoryUsed = new Sensor("Virtual Memory Used", 2, SensorType.Data, this, settings);
        ActivateSensor(VirtualMemoryUsed);

        VirtualMemoryAvailable = new Sensor("Virtual Memory Available", 3, SensorType.Data, this, settings);
        ActivateSensor(VirtualMemoryAvailable);

        VirtualMemoryLoad = new Sensor("Virtual Memory", 1, SensorType.Load, this, settings);
        ActivateSensor(VirtualMemoryLoad);
    }

    public override HardwareType HardwareType => HardwareType.Memory;

    internal Sensor PhysicalMemoryAvailable { get; }
    internal Sensor PhysicalMemoryLoad { get; }
    internal Sensor PhysicalMemoryUsed { get; }

    internal Sensor VirtualMemoryAvailable { get; }
    internal Sensor VirtualMemoryLoad { get; }
    internal Sensor VirtualMemoryUsed { get; }

    public override void Update()
    {
        if (Software.OperatingSystem.IsUnix)
        {
            MemoryLinux.Update(this);
        }
        else
        {
            MemoryWindows.Update(this);
        }
    }
}
