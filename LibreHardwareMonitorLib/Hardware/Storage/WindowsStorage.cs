// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.System.Ioctl;
using Microsoft.Win32.SafeHandles;
using WmiLight;

namespace LibreHardwareMonitor.Hardware.Storage;

internal static class WindowsStorage
{
    public static unsafe Storage.StorageInfo GetStorageInfo(string deviceId, uint driveIndex)
    {
        using SafeFileHandle handle = PInvoke.CreateFile(deviceId,
                                                         (uint)FileAccess.ReadWrite,
                                                         FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE,
                                                         null,
                                                         FILE_CREATION_DISPOSITION.OPEN_EXISTING,
                                                         FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL,
                                                         null);

        if (handle?.IsInvalid != false)
            return null;

        var query = new STORAGE_PROPERTY_QUERY { PropertyId = STORAGE_PROPERTY_ID.StorageDeviceProperty, QueryType = STORAGE_QUERY_TYPE.PropertyStandardQuery };
        STORAGE_DESCRIPTOR_HEADER result = new();

        if (!PInvoke.DeviceIoControl(handle, PInvoke.IOCTL_STORAGE_QUERY_PROPERTY, &query, (uint)sizeof(STORAGE_PROPERTY_QUERY), &result, (uint)sizeof(STORAGE_DESCRIPTOR_HEADER), null, null))
        {
            return null;
        }

        IntPtr descriptorPtr = Marshal.AllocHGlobal((int)result.Size);

        try
        {
            return PInvoke.DeviceIoControl(handle, PInvoke.IOCTL_STORAGE_QUERY_PROPERTY, &query, (uint)sizeof(STORAGE_PROPERTY_QUERY), (void*)descriptorPtr, result.Size, null, null)
                ? new StorageInfo((int)driveIndex, descriptorPtr)
                : null;
        }
        finally
        {
            Marshal.FreeHGlobal(descriptorPtr);
        }
    }

    public static string[] GetLogicalDrives(int driveIndex)
    {
        var list = new List<string>();

        using var connection = new WmiConnection(@"\\.\root\CIMV2");

        try
        {
            foreach (var partition in connection.CreateQuery(
                         $"SELECT DeviceID FROM Win32_DiskPartition WHERE DiskIndex = {driveIndex}"))
            {
                var deviceId = partition.GetPropertyValue<string>("DeviceID");
                if (string.IsNullOrEmpty(deviceId))
                    continue;

                foreach (var logicalDisk in connection.CreateQuery(
                             $@"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID=""{EscapeForWql(deviceId)}""}}
                                WHERE AssocClass=Win32_LogicalDiskToPartition
                                      ResultClass=Win32_LogicalDisk"))
                {
                    var name = logicalDisk.GetPropertyValue<string>("Name");
                    if (!string.IsNullOrEmpty(name))
                        list.Add(name.TrimEnd(':'));
                }
            }
        }
        catch
        {
            // Ignored.
        }

        return list.ToArray();
    }

    private static string EscapeForWql(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private class StorageInfo : Storage.StorageInfo
    {
        public unsafe StorageInfo(int index, IntPtr descriptorPtr)
        {
            STORAGE_DEVICE_DESCRIPTOR descriptor = *(STORAGE_DEVICE_DESCRIPTOR*)descriptorPtr;
            Index = index;
            Vendor = GetString(descriptorPtr, descriptor.VendorIdOffset, descriptor.Size);
            Product = GetString(descriptorPtr, descriptor.ProductIdOffset, descriptor.Size);
            Revision = GetString(descriptorPtr, descriptor.ProductRevisionOffset, descriptor.Size);
            Serial = GetString(descriptorPtr, descriptor.SerialNumberOffset, descriptor.Size);
            BusType = descriptor.BusType;
            Removable = descriptor.RemovableMedia;
            RawData = new byte[descriptor.Size];
            Marshal.Copy(descriptorPtr, RawData, 0, RawData.Length);
        }

        private static string GetString(IntPtr descriptorPtr, uint offset, uint size)
        {
            return offset > 0 && offset < size ? Marshal.PtrToStringAnsi(IntPtr.Add(descriptorPtr, (int)offset))?.Trim() : string.Empty;
        }
    }
}
