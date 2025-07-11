// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware.Memory;

internal sealed class VirtualMemory : Hardware
{
    public VirtualMemory(ISettings settings)
        : base("Virtual Memory", new Identifier("ram"), settings)
    {
        VirtualMemoryUsed = new Sensor("Memory Used", 2, SensorType.Data, this, settings);
        ActivateSensor(VirtualMemoryUsed);

        VirtualMemoryAvailable = new Sensor("Memory Available", 3, SensorType.Data, this, settings);
        ActivateSensor(VirtualMemoryAvailable);

        VirtualMemoryLoad = new Sensor("Memory", 1, SensorType.Load, this, settings);
        ActivateSensor(VirtualMemoryLoad);
    }

    public override HardwareType HardwareType => HardwareType.Memory;

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
