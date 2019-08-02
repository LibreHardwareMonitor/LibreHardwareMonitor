// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using OpenHardwareMonitor.Interop;

namespace OpenHardwareMonitor.Hardware.HDD {

  internal static class WindowsStorage {

    private class StorageInfoImpl : StorageInfo {
      public StorageInfoImpl(int index, IntPtr descriptorPtr) {
        Kernel32.StorageDeviceDescriptor descriptor = Marshal.PtrToStructure<Kernel32.StorageDeviceDescriptor>(descriptorPtr);
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

      using (SafeHandle handle = Kernel32.OpenDevice(deviceId)) {
        if (handle == null || handle.IsInvalid)
          return null;

        Kernel32.StoragePropertyQuery query = new Kernel32.StoragePropertyQuery();
        Kernel32.StorageDescriptorHeader header;
        uint bytesReturned = 0;

        query.PropertyId = Kernel32.StoragePropertyId.StorageDeviceProperty;
        query.QueryType = Kernel32.StorageQueryType.PropertyStandardQuery;

        if (!Kernel32.DeviceIoControl(handle, Kernel32.StorageCommand.QueryProperty, ref query, Marshal.SizeOf(query), out header, Marshal.SizeOf<Kernel32.StorageDescriptorHeader>(), out bytesReturned, IntPtr.Zero))
          return null;

        IntPtr descriptorPtr = Marshal.AllocHGlobal((int)header.Size);
        try {
          if (!Kernel32.DeviceIoControl(handle, Kernel32.StorageCommand.QueryProperty, ref query, Marshal.SizeOf(query), descriptorPtr, header.Size, out bytesReturned, IntPtr.Zero))
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
        using (ManagementObjectSearcher s =
          new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_DiskPartition " + "WHERE DiskIndex = " + driveIndex)) {

          using (ManagementObjectCollection dpc = s.Get()) {
            foreach (ManagementObject dp in dpc) {
              using (ManagementObjectCollection ldc = dp.GetRelated("Win32_LogicalDisk")) {
                foreach (ManagementBaseObject ld in ldc) {
                  list.Add(((string)ld["Name"]).TrimEnd(':'));
                }
              }
            }
          }
        }
      } catch { }
      return list.ToArray();
    }
  }
}