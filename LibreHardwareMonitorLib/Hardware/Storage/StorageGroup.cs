// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Linq;
using DiskInfoToolkit.Monitoring;
using LibreHardwareMonitor.Hardware.Storage.StorageSpaces;
using StorageDIT = DiskInfoToolkit.Storage;

namespace LibreHardwareMonitor.Hardware.Storage;

internal class StorageGroup : IGroup, IHardwareChanged
{
    //Every disk, including those shown under a Storage Spaces pool
    private readonly List<StorageDevice> _disks = new();

    //The top level: disks that are not in a pool, and the pools
    private readonly List<IHardware> _hardware = new();

    private readonly List<StorageSpacesPool> _pools = new();

    private readonly ISettings _settings;

    //Tree changes come from both the disk monitor and the pool refresh, each on its own thread
    private readonly object _lock = new();

    private bool _closed;

    public event HardwareEventHandler HardwareAdded;
    public event HardwareEventHandler HardwareRemoved;

    public StorageGroup(ISettings settings)
    {
        _settings = settings;

        AddHardware(settings);
    }

    public IReadOnlyList<IHardware> Hardware => _hardware;

    private void AddHardware(ISettings settings)
    {
        StorageDIT.DevicesChanged -= OnStoragesChanged;
        StorageSpacesData.MembersChanged -= OnPoolMembersChanged;

        //Get all disks
        var disks = StorageDIT.GetDisks();

        //Read the Storage Spaces pools first, so that a space's disk and pool members can show their state
        StorageSpacesData.Initialize();

        //Transform storage device to hardware
        _disks.AddRange(disks.Select(s => new StorageDevice(s, settings)));

        //Pool members are shown under their pool instead of at the top level
        CreatePools();

        _hardware.AddRange(_disks.Where(d => d.Parent == null));
        _hardware.AddRange(_pools);

        StorageDIT.DevicesChanged += OnStoragesChanged;
        StorageSpacesData.MembersChanged += OnPoolMembersChanged;
    }

    private void OnStoragesChanged(object sender, StorageDevicesChangedEventArgs e)
    {
        //A rescan also reports disks whose details changed, such as their temperature, which leaves the tree as it is
        if (e.Added.Count == 0 && e.Removed.Count == 0)
        {
            return;
        }

        lock (_lock)
        {
            if (_closed)
            {
                return;
            }

            foreach (var removed in e.Removed)
            {
                var storageDevice = _disks.Find(sd => sd.Storage == removed);
                if (storageDevice == null)
                {
                    continue;
                }

                //A pool member whose disk has gone stays under its pool, so that it shows which disk is missing
                if (storageDevice.StorageSpacesObjectId != null && storageDevice.Parent is StorageSpacesPool)
                {
                    storageDevice.SetMissing();
                    continue;
                }

                _disks.Remove(storageDevice);

                if (_hardware.Remove(storageDevice))
                {
                    HardwareRemoved?.Invoke(storageDevice);
                }
            }

            StorageSpacesData.Initialize();

            foreach (var added in e.Added)
            {
                _disks.Add(new StorageDevice(added, _settings));
            }

            UpdateTree();
        }
    }

    private void OnPoolMembersChanged()
    {
        //WMI can see a disk join or leave a pool after the disk itself was added or removed
        lock (_lock)
        {
            if (!_closed)
            {
                UpdateTree();
            }
        }
    }

    private void UpdateTree()
    {
        //Storage Spaces sensors are created with the disk, so a disk that became a space or pool member, or stopped being one, is created again
        for (int i = 0; i < _disks.Count; i++)
        {
            var storageDevice = _disks[i];
            if (storageDevice.IsMissing || !HasNewStorageSpacesRole(storageDevice))
            {
                continue;
            }

            if (_hardware.Remove(storageDevice))
            {
                HardwareRemoved?.Invoke(storageDevice);
            }

            _disks[i] = new StorageDevice(storageDevice.Storage, _settings);
        }

        //A missing disk is dropped once it is back, which can be with a new disk number, once another disk has its identifier, or once it has left its pool
        _disks.RemoveAll(missing => missing.IsMissing &&
                                    (_disks.Any(sd => !sd.IsMissing && (sd.StorageSpacesObjectId == missing.StorageSpacesObjectId || sd.Storage == missing.Storage || sd.Identifier == missing.Identifier)) ||
                                     StorageSpacesData.FindPhysicalDisk(missing.StorageSpacesObjectId) == null));

        //A pool is only built again when it gains or loses a disk, as that removes it and its disks from the tree and adds them again
        bool rebuildPools = !HasSamePools();
        if (rebuildPools)
        {
            foreach (var pool in _pools)
            {
                _hardware.Remove(pool);
                HardwareRemoved?.Invoke(pool);
                pool.Close();
            }

            _pools.Clear();

            CreatePools();
        }

        //Move disks between the top level and their pool
        foreach (var storageDevice in _disks)
        {
            bool isTopLevel = storageDevice.Parent == null;
            bool isShown = _hardware.Contains(storageDevice);

            if (isTopLevel && !isShown)
            {
                _hardware.Add(storageDevice);
                HardwareAdded?.Invoke(storageDevice);
            }
            else if (!isTopLevel && isShown)
            {
                _hardware.Remove(storageDevice);
                HardwareRemoved?.Invoke(storageDevice);
            }
        }

        if (rebuildPools)
        {
            foreach (var pool in _pools)
            {
                _hardware.Add(pool);
                HardwareAdded?.Invoke(pool);
            }
        }
    }

    private bool HasSamePools()
    {
        var pools = StorageSpacesData.Pools;
        if (pools.Count != _pools.Count)
        {
            return false;
        }

        foreach (var pool in pools)
        {
            var shown = _pools.Find(sp => sp.PoolId == pool.Id);
            if (shown == null)
            {
                return false;
            }

            var members = GetMembers(pool).ToList();
            if (members.Count != shown.SubHardware.Length || members.Any(sd => !shown.SubHardware.Contains(sd)))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasNewStorageSpacesRole(StorageDevice storageDevice)
    {
        string current = storageDevice.StorageSpacesObjectId;
        string now = StorageSpacesDiskSensors.GetObjectId(storageDevice.Storage);

        if (now != null)
        {
            return now != current;
        }

        //WMI can report a member missing before the disk is removed, so a member keeps its role for as long as its pool lists it
        return current != null && StorageSpacesData.FindPhysicalDisk(current) == null && StorageSpacesData.FindSpace(current) == null;
    }

    private void CreatePools()
    {
        foreach (var pool in StorageSpacesData.Pools)
        {
            _pools.Add(new StorageSpacesPool(pool, GetMembers(pool), _settings));
        }
    }

    private IEnumerable<StorageDevice> GetMembers(StorageSpacesPoolInfo pool)
    {
        //Matched by the pool member each disk shows, which stays the same while the disk is missing
        return _disks.Where(sd => sd.StorageSpacesObjectId != null && pool.PhysicalDisks.Any(pd => pd.ObjectId == sd.StorageSpacesObjectId));
    }

    public void Close()
    {
        StorageDIT.DevicesChanged -= OnStoragesChanged;
        StorageSpacesData.MembersChanged -= OnPoolMembersChanged;

        lock (_lock)
        {
            _closed = true;

            foreach (var pool in _pools)
            {
                pool.Close();
            }

            foreach (var hardware in _disks)
            {
                hardware.Close();
            }

            StorageSpacesData.Close();
        }
    }

    public string GetReport() => null;
}
