// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.Hardware.Storage;

namespace LibreHardwareMonitor.UI;

public class UpdateVisitor : IVisitor
{
    StorageUpdater _storageUpdater = new StorageUpdater(5, 60);

    public void VisitComputer(IComputer computer)
    {
        computer.Traverse(this);
    }

    public void VisitHardware(IHardware hardware)
    {
        if (hardware is AbstractStorage stor)
            _storageUpdater.TryUpdate(stor);
        else
            hardware.Update();
        foreach (IHardware subHardware in hardware.SubHardware)
            subHardware.Accept(this);
    }

    public void VisitSensor(ISensor sensor) { }

    public void VisitParameter(IParameter parameter) { }
}

class StorageUpdater
{
    // identifier -> (interval_sec, last_update)
    private readonly Dictionary<Identifier, (TimeSpan, DateTime)> _intervals = new();
    private readonly uint _nvmeIntervalSec;
    private readonly uint _otherIntervalSec;

    public StorageUpdater(uint nvmeIntervalSec, uint otherIntervalSec)
    {
        _nvmeIntervalSec = nvmeIntervalSec;
        _otherIntervalSec = otherIntervalSec;
    }
    public void TryUpdate(AbstractStorage storage)
    {
        var (interval, lastUpdate) = GetSettings(storage);
        TimeSpan diff = DateTime.UtcNow - lastUpdate;
        if (diff > interval)
        {
            _intervals[storage.Identifier] = (interval, DateTime.UtcNow);
            storage.Update();
        }
        else
        { // for real-time perf update without set interval
            try
            {
                storage.UpdatePerformanceSensors();
            }
            catch
            {
                // Ignored.
            }
        }
    }

    private (TimeSpan, DateTime) GetSettings(AbstractStorage storage)
    {
        var id = storage.Identifier;
        if (!_intervals.ContainsKey(id))
        {
            switch (storage)
            {
                case NVMeGeneric nvme:
                    _intervals.Add(id, (TimeSpan.FromSeconds(_nvmeIntervalSec), DateTime.MinValue));
                    break;
                default:
                    _intervals.Add(id, (TimeSpan.FromSeconds(_otherIntervalSec), DateTime.MinValue));
                    break;
            }
        }
        return _intervals[id];
    }
}
