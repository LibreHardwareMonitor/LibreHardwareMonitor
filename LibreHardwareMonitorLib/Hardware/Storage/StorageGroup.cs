// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Linq;
using DiskInfoToolkit;
using StorageDIT = DiskInfoToolkit.Storage;

namespace LibreHardwareMonitor.Hardware.Storage;

internal class StorageGroup : IGroup, IHardwareChanged
{
    private readonly List<StorageDevice> _hardware = new();

    private readonly ISettings _settings;

    public event HardwareEventHandler HardwareAdded;
    public event HardwareEventHandler HardwareRemoved;

    public StorageGroup(ISettings settings)
    {
        if (Software.OperatingSystem.IsUnix)
            return;

        _settings = settings;

        AddHardware(settings);
    }

    public IReadOnlyList<IHardware> Hardware => _hardware;

    private void AddHardware(ISettings settings)
    {
        StorageDIT.DevicesChanged -= OnStoragesChanged;

        //Get all disks
        var disks = StorageDIT.GetDisks();

        //Transform storage device to hardware
        _hardware.AddRange(disks.Select(s => new StorageDevice(s, settings)));

        StorageDIT.DevicesChanged += OnStoragesChanged;
    }

    private void OnStoragesChanged(object sender, StorageDevicesChangedEventArgs e)
    {
        foreach (var added in e.Added)
        {
            var storageDevice = new StorageDevice(added, _settings);

            _hardware.Add(storageDevice);
            HardwareAdded?.Invoke(storageDevice);
        }

        foreach (var removed in e.Removed)
        {
            var storageDevice = _hardware.Find(sd => sd.Storage == removed);
            if (storageDevice != null)
            {
                _hardware.Remove(storageDevice);
                HardwareRemoved?.Invoke(storageDevice);
            }
        }
    }

    public void Close() { }

    public string GetReport() => null;
}
