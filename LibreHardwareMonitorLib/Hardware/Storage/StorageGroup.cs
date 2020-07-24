// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Management;

namespace LibreHardwareMonitor.Hardware.Storage
{
    internal class StorageGroup : IGroup
    {
        private readonly List<AbstractStorage> _hardware = new List<AbstractStorage>();

        public StorageGroup(ISettings settings)
        {
            if (Software.OperatingSystem.IsUnix)
                return;

            //https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-diskdrive
            var mosDisks = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
            ManagementObjectCollection queryCollection = mosDisks.Get(); // get the results

            foreach (ManagementBaseObject disk in queryCollection)
            {
                string deviceId = (string)disk.Properties["DeviceId"].Value; // is \\.\PhysicalDrive0..n
                uint idx = Convert.ToUInt32(disk.Properties["Index"].Value);
                ulong diskSize = Convert.ToUInt64(disk.Properties["Size"].Value);
                int scsi = Convert.ToInt32(disk.Properties["SCSIPort"].Value);

                if (deviceId != null)
                {
                    var instance = AbstractStorage.CreateInstance(deviceId, idx, diskSize, scsi, settings);
                    if (instance != null)
                    {
                        _hardware.Add(instance);
                    }
                }
            }

            queryCollection.Dispose();
            mosDisks.Dispose();
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
}
