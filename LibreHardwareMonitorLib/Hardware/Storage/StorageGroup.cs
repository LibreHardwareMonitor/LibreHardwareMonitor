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

        var virtualDiskMap = DiskToPhysicalDisk();

        //https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-diskdrive
        using var diskDriveSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive") { Options = { Timeout = TimeSpan.FromSeconds(10) } };
        foreach (ManagementBaseObject diskDrive in diskDriveSearcher.Get())
        {
            string deviceId = (string)diskDrive.Properties["DeviceId"].Value; // is \\.\PhysicalDrive0..n
            uint idx = Convert.ToUInt32(diskDrive.Properties["Index"].Value);
            ulong diskSize = Convert.ToUInt64(diskDrive.Properties["Size"].Value);
            int scsi = Convert.ToInt32(diskDrive.Properties["SCSIPort"].Value);
            string serialNumber = ExtractSerialNumber((string)diskDrive.Properties["SerialNumber"].Value);

            if (deviceId != null)
            {
                var instance = AbstractStorage.CreateInstance(deviceId, idx, diskSize, scsi, settings);
                if (instance != null)
                {
                    _hardware.Add(instance);
                }

                if (serialNumber != null && virtualDiskMap.ContainsKey(serialNumber))
                {
                    var physicalDisks = virtualDiskMap[serialNumber];
                    foreach (PhysicalDisk physicalDisk in physicalDisks)
                    {
                        var physicialDiskIdx = Convert.ToUInt32(physicalDisk.deviceId);
                        var physicalDiskInstance = AbstractStorage.CreateInstance($"\\\\.\\PHYSICALDRIVE{physicalDisk.deviceId}", physicialDiskIdx, physicalDisk.size, scsi, settings);
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
    /// Returns a map of serialNumber -> physical disk properties.
    /// If you are using Windows Storage Spaces, one or more physical disks are put together to form a single disk (for better performance and/or resilience).
    /// We can use the Storage Management API to find the device ids of these "hidden" physical disks. 
    /// </summary>
    /// <returns></returns>
    private Dictionary<string, List<PhysicalDisk>> DiskToPhysicalDisk()
    {
        var physicalDiskMap = new Dictionary<string, PhysicalDisk>();
        ManagementScope managementScope = new ManagementScope("\\\\.\\ROOT\\Microsoft\\Windows\\Storage");
        //https://learn.microsoft.com/en-us/previous-versions/windows/desktop/stormgmt/msft-physicaldisk
        using var physicalDiskSearcher = new ManagementObjectSearcher(managementScope, new ObjectQuery("SELECT * FROM MSFT_PhysicalDisk"));
        foreach (ManagementBaseObject physicalDisk in physicalDiskSearcher.Get())
        {
            var sn = ExtractSerialNumber((string)physicalDisk["ObjectId"]);
            var deviceId = (string)physicalDisk["DeviceId"];
            var size = (ulong)physicalDisk["Size"];
            if (sn != null)
            {
                physicalDiskMap.Add(sn, new PhysicalDisk(deviceId, size));
            }
        }

        var virtualDiskMap = new Dictionary<string, List<PhysicalDisk>>();
        //https://learn.microsoft.com/en-us/previous-versions/windows/desktop/stormgmt/msft-virtualdisktophysicaldisk
        using var virtualToPhysicalDiskSearcher = new ManagementObjectSearcher(managementScope, new ObjectQuery("SELECT * FROM MSFT_VirtualDiskToPhysicalDisk"));
        foreach (ManagementBaseObject virtualToPhysicalDisk in virtualToPhysicalDiskSearcher.Get())
        {
            var virtualDiskSn = ExtractSerialNumber((string)virtualToPhysicalDisk["VirtualDisk"]);
            var physicalDiskSn = ExtractSerialNumber((string)virtualToPhysicalDisk["PhysicalDisk"]);
            if (virtualDiskSn != null && physicalDiskSn != null && physicalDiskMap.ContainsKey(physicalDiskSn))
            {
                if (!virtualDiskMap.ContainsKey(virtualDiskSn))
                {
                    virtualDiskMap[virtualDiskSn] = new List<PhysicalDisk>();
                }
                virtualDiskMap[virtualDiskSn].Add(physicalDiskMap[physicalDiskSn]);
            }
        }

        return virtualDiskMap;
    }

    private class PhysicalDisk
    {
        public readonly string deviceId;
        public readonly ulong size;

        public PhysicalDisk(string deviceId, ulong size)
        {
            this.deviceId = deviceId;
            this.size = size;
        }
    }

    private static Regex serialNumberRegex = new Regex(@".*\{(?<id>[^}]+)\}[^}]*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Returns the serial number from a disk string id.
    /// </summary>
    /// <param name="objectId"></param>
    /// <returns></returns>
    private static String ExtractSerialNumber(string objectId)
    {
        MatchCollection matches = serialNumberRegex.Matches(objectId);
        foreach (Match match in matches)
        {
            GroupCollection groups = match.Groups;
            return groups["id"].Value.ToLower();
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
