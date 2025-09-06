// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware.Memory;

internal sealed class TotalMemory : Hardware
{
    public TotalMemory(ISettings settings)
        : base("Total Memory", new Identifier("ram"), settings)
    {
        PhysicalMemoryUsed = new Sensor("Memory Used", 0, SensorType.Data, this, settings);
        ActivateSensor(PhysicalMemoryUsed);

        PhysicalMemoryAvailable = new Sensor("Memory Available", 1, SensorType.Data, this, settings);
        ActivateSensor(PhysicalMemoryAvailable);

        PhysicalMemoryLoad = new Sensor("Memory", 0, SensorType.Load, this, settings);
        ActivateSensor(PhysicalMemoryLoad);
    }

    public override HardwareType HardwareType => HardwareType.Memory;

    internal Sensor PhysicalMemoryAvailable { get; }

    internal Sensor PhysicalMemoryLoad { get; }

    internal Sensor PhysicalMemoryUsed { get; }

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
