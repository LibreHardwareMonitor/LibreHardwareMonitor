// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using WmiLight;

namespace LibreHardwareMonitor.Hardware.Storage;

#pragma warning disable CA1416 // Validate platform compatibility

internal class StorageGroup : IGroup
{
    private readonly List<AbstractStorage> _hardware = new();

    public StorageGroup(ISettings settings)
    {
        if (Software.OperatingSystem.IsUnix)
            return;

        using var cimv2   = new WmiConnection(@"\\.\root\CIMV2");
        using var storage = new WmiConnection(@"\\.\root\Microsoft\Windows\Storage");

        Dictionary<uint, List<(uint, ulong)>> storageSpaceDiskToPhysicalDiskMap = GetStorageSpaceDiskToPhysicalDiskMap(storage);
        AddHardware(settings, cimv2, storageSpaceDiskToPhysicalDiskMap);
    }

    public IReadOnlyList<IHardware> Hardware => _hardware;

    /// <summary>
    /// Adds the hardware.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="cimv2">Connection to WMI.</param>
    /// <param name="storageSpaceDiskToPhysicalDiskMap">The storage space disk to physical disk map.</param>
    private void AddHardware(
        ISettings settings,
        WmiConnection cimv2,
        Dictionary<uint, List<(uint deviceId, ulong size)>> storageSpaceDiskToPhysicalDiskMap)
    {
        try
        {
            // https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-diskdrive
            foreach (var diskDrive in cimv2.CreateQuery(
                         @"SELECT DeviceID, Index, Size, SCSIPort FROM Win32_DiskDrive"))
            {
                var deviceId = diskDrive.GetPropertyValue<string>("DeviceID"); // \\.\PHYSICALDRIVEn
                if (string.IsNullOrEmpty(deviceId))
                    continue;

                uint index = SafeGetUInt32(diskDrive, "Index");
                ulong size = SafeGetUInt64(diskDrive, "Size");
                int scsi = SafeGetInt32(diskDrive, "SCSIPort");

                var instance = AbstractStorage.CreateInstance(deviceId, index, size, scsi, settings);
                if (instance != null)
                    _hardware.Add(instance);

                if (storageSpaceDiskToPhysicalDiskMap != null &&
                    storageSpaceDiskToPhysicalDiskMap.TryGetValue(index, out var pdList))
                {
                    foreach (var (pdIndex, pdSize) in pdList)
                    {
                        var phys = AbstractStorage.CreateInstance(
                            $@"\\.\PHYSICALDRIVE{pdIndex}", pdIndex, pdSize, scsi, settings);
                        if (phys != null)
                            _hardware.Add(phys);
                    }
                }
            }
        }
        catch(Exception ex)
        {
            // Ignored.
        }
    }

    /// <summary>
    /// Maps each StorageSpace to the PhysicalDisks it is composed of.
    /// </summary>
    private static Dictionary<uint, List<(uint deviceId, ulong size)>> GetStorageSpaceDiskToPhysicalDiskMap(
        WmiConnection storage)
    {
        var map = new Dictionary<uint, List<(uint, ulong)>>();

        if (!Software.OperatingSystem.IsWindows8OrGreater)
            return map;

        try
        {
            // https://learn.microsoft.com/en-us/previous-versions/windows/desktop/stormgmt/msft-disk
            // Lists all the disks visible to your system, the output is the same as Win32_DiskDrive.
            // If you're using a storage Space, the "hidden" disks which compose your storage space will not be listed.
            foreach (var disk in storage.CreateQuery(@"SELECT Number, ObjectId FROM MSFT_Disk"))
            {
                try
                {
                    var list = MapDiskToPhysicalDisk(disk, storage);
                    if (list.Count > 0)
                        map[SafeGetUInt32(disk, "Number")] = list;
                }
                catch
                {
                    // Ignored.
                }
            }
        }
        catch(Exception ex)
        {
            // Ignored.
        }

        return map;
    }

    /// <summary>
    /// Maps a disk to a physical disk.
    /// </summary>
    /// <param name="disk">The disk.</param>
    /// <param name="scope">The scope.</param>
    private static List<(uint deviceId, ulong size)> MapDiskToPhysicalDisk(WmiObject disk, WmiConnection storage)
    {
        var result = new List<(uint, ulong)>();

        var diskObjectId = disk.GetPropertyValue<string>("ObjectId");
        if (string.IsNullOrEmpty(diskObjectId))
            return result;

        // https://learn.microsoft.com/en-us/previous-versions/windows/desktop/stormgmt/msft-virtualdisk
        // Maps the current Disk to its corresponding VirtualDisk. If the current Disk is not a storage space, it does not have a corresponding VirtualDisk.
        // Each Disk maps to one or zero VirtualDisk.
        foreach (var virtualDisk in storage.CreateQuery(
                     FollowAssociationQuery("MSFT_Disk", diskObjectId, "MSFT_VirtualDiskToDisk", "MSFT_VirtualDisk")))
        {
            var vObjectId = virtualDisk.GetPropertyValue<string>("ObjectId");
            if (string.IsNullOrEmpty(vObjectId))
                continue;

            // https://learn.microsoft.com/en-us/previous-versions/windows/desktop/stormgmt/msft-physicaldisk
            // Maps the current VirtualDisk to the PhysicalDisk it is composed of.
            // Each VirtualDisk maps to one or more PhysicalDisk.
            foreach (var physicalDisk in storage.CreateQuery(FollowAssociationQuery("MSFT_VirtualDisk", vObjectId, "MSFT_VirtualDiskToPhysicalDisk", "MSFT_PhysicalDisk")))
            {
                var size = SafeGetUInt64(physicalDisk, "Size");
                var devStr = physicalDisk.GetPropertyValue<string>("DeviceId");
                if (UInt32.TryParse(devStr, out var devId))
                    result.Add((devId, size));
            }
        }

        return result;
    }

    private static string EscapeForWql(string s) =>
        (s ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static uint SafeGetUInt32(WmiObject o, string name)
    {
        try { return o.GetPropertyValue<uint>(name); } catch { return 0u; }
    }

    private static ulong SafeGetUInt64(WmiObject o, string name)
    {
        try { return o.GetPropertyValue<ulong>(name); } catch { return 0UL; }
    }

    private static int SafeGetInt32(WmiObject o, string name)
    {
        try { return unchecked((int)o.GetPropertyValue<uint>(name)); } catch { return 0; }
    }

    private static string FollowAssociationQuery(string source, string objectId, string associationClass, string resultClass)
    {
        return @$"ASSOCIATORS OF {{{source}.ObjectId=""{EscapeForWql(objectId)}""}} WHERE AssocClass = {associationClass} ResultClass = {resultClass}";
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
