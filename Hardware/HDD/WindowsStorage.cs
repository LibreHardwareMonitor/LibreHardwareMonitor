// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) 2009-2015 Michael Möller <mmoeller@openhardwaremonitor.org>
// Copyright (C) 2010 Paul Werelds
// Copyright (C) 2011 Roland Reinl <roland-reinl@gmx.de>
// Copyright (C) 2017 Alexander Thulcke <alexth4ef9@gmail.com>
// Copyright (C) 2016-2019 Sebastian Grams <https://github.com/sebastian-dev>
// Copyright (C) 2016-2019 Aqua Computer <https://github.com/aquacomputer, info@aqua-computer.de>

using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;

namespace OpenHardwareMonitor.Hardware.HDD {

  public static class WindowsStorage {

    private class StorageInfoImpl : StorageInfo {
      public StorageInfoImpl(int index, IntPtr descriptorPtr) {
        Interop.StorageDeviceDescriptor descriptor = Marshal.PtrToStructure<Interop.StorageDeviceDescriptor>(descriptorPtr);
        Index = index;
        Vendor = GetString(descriptorPtr, descriptor.VendorIdOffset);
        Product = GetString(descriptorPtr, descriptor.ProductIdOffset);
        Revision = GetString(descriptorPtr, descriptor.ProductRevisionOffset);
        Serial = GetString(descriptorPtr, descriptor.SerialNumberOffset);
        BusType = descriptor.BusType;
        Removable = descriptor.RemovableMedia;
        RawData = new byte[descriptor.Size];
        Marshal.Copy(descriptorPtr, RawData, 0, RawData.Length);
      }

      private static string GetString(IntPtr descriptorPtr, uint offset) {
        return (offset > 0) ? Marshal.PtrToStringAnsi(new IntPtr(descriptorPtr.ToInt64() + offset)).Trim() : string.Empty;
      }
    }

    public static StorageInfo GetStorageInfo(string deviceId, uint driveIndex) {

      using (SafeHandle handle = Interop.OpenDevice(deviceId)) {
        if (handle == null || handle.IsInvalid)
          return null;

        Interop.StoragePropertyQuery query = new Interop.StoragePropertyQuery();
        Interop.StorageDescriptorHeader header;
        uint bytesReturned = 0;

        query.PropertyId = Interop.StoragePropertyId.StorageDeviceProperty;
        query.QueryType = Interop.StorageQueryType.PropertyStandardQuery;

        if (!Interop.DeviceIoControl(handle, Interop.StorageCommand.QueryProperty, ref query, Marshal.SizeOf(query), out header, Marshal.SizeOf<Interop.StorageDescriptorHeader>(), out bytesReturned, IntPtr.Zero))
          return null;

        IntPtr descriptorPtr = Marshal.AllocHGlobal((int)header.Size);
        try {
          if (!Interop.DeviceIoControl(handle, Interop.StorageCommand.QueryProperty, ref query, Marshal.SizeOf(query), descriptorPtr, header.Size, out bytesReturned, IntPtr.Zero))
            return null;

          return new StorageInfoImpl((int)driveIndex, descriptorPtr);
        }
        finally {
          Marshal.FreeHGlobal(descriptorPtr);
        }
      }
    }

    public static string[] GetLogicalDrives(int driveIndex) {
      List<string> list = new List<string>();
      try {
        using (ManagementObjectSearcher s = new ManagementObjectSearcher(
            "root\\CIMV2",
            "SELECT * FROM Win32_DiskPartition " +
            "WHERE DiskIndex = " + driveIndex))
        using (ManagementObjectCollection dpc = s.Get())
          foreach (ManagementObject dp in dpc)
            using (ManagementObjectCollection ldc =
              dp.GetRelated("Win32_LogicalDisk"))
              foreach (ManagementBaseObject ld in ldc)
                list.Add(((string)ld["Name"]).TrimEnd(':'));
      }
      catch { }
      return list.ToArray();
    }


  }
}