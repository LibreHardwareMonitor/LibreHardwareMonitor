// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Runtime.InteropServices;
using LibreHardwareMonitor.Interop;

namespace LibreHardwareMonitor.Hardware.Memory;

internal sealed class GenericWindowsMemory : Hardware
{
    private readonly Sensor _physicalMemoryAvailable;
    private readonly Sensor _physicalMemoryLoad;
    private readonly Sensor _physicalMemoryUsed;
    private readonly Sensor _virtualMemoryAvailable;
    private readonly Sensor _virtualMemoryLoad;
    private readonly Sensor _virtualMemoryUsed;

    public GenericWindowsMemory(string name, ISettings settings) : base(name, new Identifier("ram"), settings)
    {
        _physicalMemoryUsed = new Sensor("Memory Used", 0, SensorType.Data, this, settings);
        ActivateSensor(_physicalMemoryUsed);

        _physicalMemoryAvailable = new Sensor("Memory Available", 1, SensorType.Data, this, settings);
        ActivateSensor(_physicalMemoryAvailable);

        _physicalMemoryLoad = new Sensor("Memory", 0, SensorType.Load, this, settings);
        ActivateSensor(_physicalMemoryLoad);

        _virtualMemoryUsed = new Sensor("Virtual Memory Used", 2, SensorType.Data, this, settings);
        ActivateSensor(_virtualMemoryUsed);

        _virtualMemoryAvailable = new Sensor("Virtual Memory Available", 3, SensorType.Data, this, settings);
        ActivateSensor(_virtualMemoryAvailable);

        _virtualMemoryLoad = new Sensor("Virtual Memory", 1, SensorType.Load, this, settings);
        ActivateSensor(_virtualMemoryLoad);
    }

    public override HardwareType HardwareType
    {
        get { return HardwareType.Memory; }
    }

    public override void Update()
    {
        Kernel32.MEMORYSTATUSEX status = new() { dwLength = (uint)Marshal.SizeOf<Kernel32.MEMORYSTATUSEX>() };

        if (!Kernel32.GlobalMemoryStatusEx(ref status))
            return;

        _physicalMemoryUsed.Value = (float)(status.ullTotalPhys - status.ullAvailPhys) / (1024 * 1024 * 1024);
        _physicalMemoryAvailable.Value = (float)status.ullAvailPhys / (1024 * 1024 * 1024);
        _physicalMemoryLoad.Value = 100.0f - ((100.0f * status.ullAvailPhys) / status.ullTotalPhys);

        _virtualMemoryUsed.Value = (float)(status.ullTotalPageFile - status.ullAvailPageFile) / (1024 * 1024 * 1024);
        _virtualMemoryAvailable.Value = (float)status.ullAvailPageFile / (1024 * 1024 * 1024);
        _virtualMemoryLoad.Value = 100.0f - ((100.0f * status.ullAvailPageFile) / status.ullTotalPageFile);
    }
}