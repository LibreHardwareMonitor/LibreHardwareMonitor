// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Linq;
using DiskInfoToolkit;
using DiskInfoToolkit.Events;

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
        StorageManager.StoragesChanged -= OnStoragesChanged;

        //Reload storage devices
        StorageManager.ReloadStorages();

        //Transform storage device to hardware
        _hardware.AddRange(StorageManager.Storages.Select(s => new StorageDevice(s, GetID(s), settings)));

        StorageManager.StoragesChanged += OnStoragesChanged;
    }

    private void OnStoragesChanged(StoragesChangedEventArgs e)
    {
        StorageDevice storageDevice = null;

        switch (e.StorageChangeIdentifier)
        {
            case StorageChangeIdentifier.Added:
                storageDevice = new StorageDevice(e.Storage, GetID(e.Storage), _settings);

                _hardware.Add(storageDevice);
                HardwareAdded?.Invoke(storageDevice);
                break;
            case StorageChangeIdentifier.Removed:
                storageDevice = _hardware.Find(sd => sd.Storage == e.Storage);

                if (storageDevice != null)
                {
                    _hardware.Remove(storageDevice);
                    HardwareRemoved?.Invoke(storageDevice);
                }
                break;
        }
    }

    private string GetID(DiskInfoToolkit.Storage storage)
    {
        if (storage.IsNVMe)
            return "nvme";
        else if (storage.IsSSD)
            return "ssd";
        else
            return "hdd";
    }

    public void Close() { }

    public string GetReport() => null;
}
