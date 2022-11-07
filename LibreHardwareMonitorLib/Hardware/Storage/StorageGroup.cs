// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Management;
using System.Text.RegularExpressions;

namespace LibreHardwareMonitor.Hardware.Storage;

internal class StorageGroup : IGroup
{
    private readonly List<AbstractStorage> _hardware = new();

    public StorageGroup(ISettings settings)
    {
        if (Software.OperatingSystem.IsUnix)
            return;

        var indexToPhysicalDiskMap = DiskToPhysicalDisk();

        //https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-diskdrive
        using var diskDriveSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive") { Options = { Timeout = TimeSpan.FromSeconds(10) } };
        foreach (ManagementBaseObject diskDrive in diskDriveSearcher.Get())
        {
            string deviceId = (string)diskDrive.Properties["DeviceId"].Value; // is \\.\PhysicalDrive0..n
            uint idx = Convert.ToUInt32(diskDrive.Properties["Index"].Value);
            ulong diskSize = Convert.ToUInt64(diskDrive.Properties["Size"].Value);
            int scsi = Convert.ToInt32(diskDrive.Properties["SCSIPort"].Value);

            if (deviceId != null)
            {
                var instance = AbstractStorage.CreateInstance(deviceId, idx, diskSize, scsi, settings);
                if (instance != null)
                {
                    _hardware.Add(instance);
                }

                if (indexToPhysicalDiskMap.ContainsKey(idx))
                {
                    var physicalDisks = indexToPhysicalDiskMap[idx];
                    foreach ((string, ulong) physicalDisk in physicalDisks)
                    {
                        var physicialDiskIdx = Convert.ToUInt32(physicalDisk.Item1);
                        var physicalDiskInstance = AbstractStorage.CreateInstance($"\\\\.\\PHYSICALDRIVE{physicalDisk.Item1}", physicialDiskIdx, physicalDisk.Item2, scsi, settings);
                        if (physicalDiskInstance != null)
                        {
                            _hardware.Add(physicalDiskInstance);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Returns a map of diskIndex -> physical disk properties.
    /// If you are using Windows Storage Spaces, one or more physical disks are put together to form a single disk (for better performance and/or resilience).
    /// We can use the Storage Management API to find the device ids of these "hidden" physical disks.
    ///
    /// There's a bit of gymnastics, but basically this method is just doing a join between the tables: MSFT_Disk, MSFT_VirtualDiskToDisk, MSFT_VirtualDiskToPhysicalDisk, MSFT_PhysicalDisk
    /// </summary>
    /// <returns></returns>
    private static Dictionary<UInt32, List<(string, ulong)>> DiskToPhysicalDisk()
    {
        try
        {
            var physicalDiskMap = new Dictionary<string, (string, ulong)>();
            ManagementScope managementScope = new ManagementScope("\\\\.\\ROOT\\Microsoft\\Windows\\Storage");
            //https://learn.microsoft.com/en-us/previous-versions/windows/desktop/stormgmt/msft-physicaldisk
            using var physicalDiskSearcher = new ManagementObjectSearcher(managementScope, new ObjectQuery("SELECT * FROM MSFT_PhysicalDisk"));
            foreach (ManagementBaseObject physicalDisk in physicalDiskSearcher.Get())
            {
                var objectId = (string)physicalDisk["ObjectId"];
                var deviceId = (string)physicalDisk["DeviceId"];
                var size = (ulong)physicalDisk["Size"];
                if (objectId != null && deviceId != null)
                {
                    physicalDiskMap.Add(objectId.ToLower(), (deviceId, size));
                }
            }

            var virtualDiskMap = new Dictionary<string, List<(string, ulong)>>();
            //https://learn.microsoft.com/en-us/previous-versions/windows/desktop/stormgmt/msft-virtualdisktophysicaldisk
            using var virtualToPhysicalDiskSearcher = new ManagementObjectSearcher(managementScope, new ObjectQuery("SELECT * FROM MSFT_VirtualDiskToPhysicalDisk"));
            foreach (ManagementBaseObject virtualToPhysicalDisk in virtualToPhysicalDiskSearcher.Get())
            {
                var physicalDiskObjectId = ExtractReferenceObjectID((string)virtualToPhysicalDisk["PhysicalDisk"]);
                var virtualDiskObjectId = ExtractReferenceObjectID((string)virtualToPhysicalDisk["VirtualDisk"]);
                if (physicalDiskObjectId != null && virtualDiskObjectId != null && physicalDiskMap.ContainsKey(physicalDiskObjectId))
                {
                    if (!virtualDiskMap.ContainsKey(virtualDiskObjectId))
                    {
                        virtualDiskMap[virtualDiskObjectId] = new List<(string, ulong)>();
                    }
                    virtualDiskMap[virtualDiskObjectId].Add(physicalDiskMap[physicalDiskObjectId]);
                }
            }

            var diskMap = new Dictionary<string, List<(string, ulong)>>();
            //https://learn.microsoft.com/en-us/previous-versions/windows/desktop/stormgmt/msft-virtualdisktodisk
            using var diskToVirtualDiskSearcher = new ManagementObjectSearcher(managementScope, new ObjectQuery("SELECT * FROM MSFT_VirtualDiskToDisk"));
            foreach (ManagementBaseObject diskToVirtualDisk in diskToVirtualDiskSearcher.Get())
            {
                var virtualDiskObjectId = ExtractReferenceObjectID((string)diskToVirtualDisk["VirtualDisk"]);
                var diskObjectId = ExtractReferenceObjectID((string)diskToVirtualDisk["Disk"]);
                if (virtualDiskObjectId != null && diskObjectId != null && virtualDiskMap.ContainsKey(virtualDiskObjectId))
                {
                    if (!diskMap.ContainsKey(diskObjectId))
                    {
                        diskMap[diskObjectId] = new List<(string, ulong)>();
                    }
                    diskMap[diskObjectId].AddRange(virtualDiskMap[virtualDiskObjectId]);
                }
            }

            var indexToPhysicalDiskMap = new Dictionary<UInt32, List<(string, ulong)>>();
            using var diskSearcher = new ManagementObjectSearcher(managementScope, new ObjectQuery("SELECT * FROM MSFT_Disk"));
            foreach (ManagementBaseObject disk in diskSearcher.Get())
            {
                var objectId = (string)disk["ObjectId"];
                var index = (UInt32)disk["Number"];
                if (objectId != null && diskMap.ContainsKey(objectId.ToLower()))
                {
                    if (!indexToPhysicalDiskMap.ContainsKey(index))
                    {
                        indexToPhysicalDiskMap[index] = new List<(string, ulong)>();
                    }
                    indexToPhysicalDiskMap[index].AddRange(diskMap[objectId.ToLower()]);
                }
            }

            return indexToPhysicalDiskMap;
        }
        catch (Exception)
        {
            return new Dictionary<UInt32, List<(string, ulong)>>();
        }
    }

    private static Regex referenceObjectIDRegex = new Regex(".*ObjectId=\"(?<id>.+)\"$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Returns the object id inside a reference string
    /// </summary>
    /// <param name="referenceString"></param>
    /// <returns></returns>
    private static String ExtractReferenceObjectID(string referenceString)
    {
        MatchCollection matches = referenceObjectIDRegex.Matches(referenceString);
        foreach (Match match in matches)
        {
            GroupCollection groups = match.Groups;
            return Regex.Unescape(groups["id"].Value).ToLower();
        }

        return null;
    }

    public IReadOnlyList<IHardware> Hardware => _hardware;

    public string GetReport()
    {
        return null;
    }

    public void Close()
    {
        foreach (AbstractStorage storage in _hardware)
            storage.Close();
    }
}
