// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Management;

namespace LibreHardwareMonitor.Hardware.Storage;

#pragma warning disable CA1416 // Validate platform compatibility

internal class StorageGroup : IGroup
{
    private readonly List<AbstractStorage> _hardware = new();

    public StorageGroup(ISettings settings)
    {
        if (Software.OperatingSystem.IsUnix)
            return;

        Dictionary<uint, List<(uint, ulong)>> storageSpaceDiskToPhysicalDiskMap = GetStorageSpaceDiskToPhysicalDiskMap();
        AddHardware(settings, storageSpaceDiskToPhysicalDiskMap);
    }

    public IReadOnlyList<IHardware> Hardware => _hardware;

    /// <summary>
    /// Adds the hardware.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="storageSpaceDiskToPhysicalDiskMap">The storage space disk to physical disk map.</param>
    private void AddHardware(ISettings settings, Dictionary<uint, List<(uint, ulong)>> storageSpaceDiskToPhysicalDiskMap)
    {
        try
        {
            // https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-diskdrive
            using var diskDriveSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive") { Options = { Timeout = TimeSpan.FromSeconds(10) } };

            foreach (ManagementBaseObject diskDrive in diskDriveSearcher.Get())
            {
                string deviceId = (string)diskDrive.Properties["DeviceId"].Value; // is \\.\PhysicalDrive0..n
                uint index = Convert.ToUInt32(diskDrive.Properties["Index"].Value);
                ulong diskSize = Convert.ToUInt64(diskDrive.Properties["Size"].Value);
                int scsi = Convert.ToInt32(diskDrive.Properties["SCSIPort"].Value);

                if (deviceId != null)
                {
                    var instance = AbstractStorage.CreateInstance(deviceId, index, diskSize, scsi, settings);
                    if (instance != null)
                        _hardware.Add(instance);

                    if (storageSpaceDiskToPhysicalDiskMap.ContainsKey(index))
                    {
                        foreach ((uint, ulong) physicalDisk in storageSpaceDiskToPhysicalDiskMap[index])
                        {
                            var physicalDiskInstance = AbstractStorage.CreateInstance(@$"\\.\PHYSICALDRIVE{physicalDisk.Item1}", physicalDisk.Item1, physicalDisk.Item2, scsi, settings);
                            if (physicalDiskInstance != null)
                                _hardware.Add(physicalDiskInstance);
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignored.
        }
    }

    /// <summary>
    /// Maps each StorageSpace to the PhysicalDisks it is composed of.
    /// </summary>
    private static Dictionary<uint, List<(uint, ulong)>> GetStorageSpaceDiskToPhysicalDiskMap()
    {
        var diskToPhysicalDisk = new Dictionary<uint, List<(uint, ulong)>>();

        if (!Software.OperatingSystem.IsWindows8OrGreater)
            return diskToPhysicalDisk;

        try
        {
            ManagementScope scope = new(@"\root\Microsoft\Windows\Storage");

            // https://learn.microsoft.com/en-us/previous-versions/windows/desktop/stormgmt/msft-disk
            // Lists all the disks visible to your system, the output is the same as Win32_DiskDrive.
            // If you're using a storage Space, the "hidden" disks which compose your storage space will not be listed.
            using var diskSearcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM MSFT_Disk"));

            foreach (ManagementBaseObject disk in diskSearcher.Get())
            {
                try
                {
                    List<(uint, ulong)> map = MapDiskToPhysicalDisk(disk, scope);
                    if (map.Count > 0)
                        diskToPhysicalDisk[(uint)disk["Number"]] = map;
                }
                catch
                {
                    // Ignored.
                }
            }
        }
        catch
        {
            // Ignored.
        }

        return diskToPhysicalDisk;
    }

    /// <summary>
    /// Maps a disk to a physical disk.
    /// </summary>
    /// <param name="disk">The disk.</param>
    /// <param name="scope">The scope.</param>
    private static List<(uint, ulong)> MapDiskToPhysicalDisk(ManagementBaseObject disk, ManagementScope scope)
    {
        var map = new List<(uint, ulong)>();

        // https://learn.microsoft.com/en-us/previous-versions/windows/desktop/stormgmt/msft-virtualdisk
        // Maps the current Disk to its corresponding VirtualDisk. If the current Disk is not a storage space, it does not have a corresponding VirtualDisk.
        // Each Disk maps to one or zero VirtualDisk.
        using var toVirtualDisk = new ManagementObjectSearcher(scope, new ObjectQuery(FollowAssociationQuery("MSFT_Disk", (string)disk["ObjectId"], "MSFT_VirtualDiskToDisk")));

        foreach (ManagementBaseObject virtualDisk in toVirtualDisk.Get())
        {
            // https://learn.microsoft.com/en-us/previous-versions/windows/desktop/stormgmt/msft-physicaldisk
            // Maps the current VirtualDisk to the PhysicalDisk it is composed of.
            // Each VirtualDisk maps to one or more PhysicalDisk.

            using var toPhysicalDisk = new ManagementObjectSearcher(scope,
                                                                    new ObjectQuery(FollowAssociationQuery("MSFT_VirtualDisk",
                                                                                                           (string)virtualDisk["ObjectId"],
                                                                                                           "MSFT_VirtualDiskToPhysicalDisk")));

            foreach (ManagementBaseObject physicalDisk in toPhysicalDisk.Get())
            {
                ulong physicalDiskSize = (ulong)physicalDisk["Size"];

                if (uint.TryParse((string)physicalDisk["DeviceId"], out uint physicalDiskId))
                    map.Add((physicalDiskId, physicalDiskSize));
            }
        }

        return map;
    }

    private static string FollowAssociationQuery(string source, string objectId, string associationClass)
    {
        return @$"ASSOCIATORS OF {{{source}.ObjectId=""{objectId.Replace(@"\", @"\\").Replace(@"""", @"\""")}""}} WHERE AssocClass = {associationClass}";
    }

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
