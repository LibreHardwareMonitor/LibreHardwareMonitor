/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2009-2015 Michael Möller <mmoeller@openhardwaremonitor.org>
  Copyright (C) 2010 Paul Werelds
  Copyright (C) 2011 Roland Reinl <roland-reinl@gmx.de>
  Copyright (C) 2017 Alexander Thulcke <alexth4ef9@gmail.com>
  
*/
 
using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace OpenHardwareMonitor.Hardware.HDD {
  public enum StorageBusType {
    BusTypeUnknown = 0x00,
    BusTypeScsi,
    BusTypeAtapi,
    BusTypeAta,
    BusType1394,
    BusTypeSsa,
    BusTypeFibre,
    BusTypeUsb,
    BusTypeRAID,
    BusTypeiScsi,
    BusTypeSas,
    BusTypeSata,
    BusTypeSd,
    BusTypeMmc,
    BusTypeVirtual,
    BusTypeFileBackedVirtual,
    BusTypeSpaces,
    BusTypeNvme,
    BusTypeSCM,
    BusTypeMax,
    BusTypeMaxReserved = 0x7F,
  }
  
  public abstract class StorageInfo {
    public int Index { get; protected set; }
    public string Vendor { get; protected set; }
    public string Product { get; protected set; }
    public string Revision { get; protected set; }
    public string Serial { get; protected set; }
    public StorageBusType BusType { get; protected set; }
    public bool Removable { get; protected set; }
    public string Name { 
      get { return (Vendor + " " + Product).Trim(); }
    }
    public byte[] RawData { get; protected set; }
  }
  
  internal static class WindowsStorage {    
    private enum StorageCommand : uint {
      QueryProperty = 0x002d1400,
    }
        
    private enum StorageQueryType {
      PropertyStandardQuery = 0,
      PropertyExistsQuery,
      PropertyMaskQuery,
      PropertyQueryMaxDefined,      
    }
    
    private enum StoragePropertyId {
      StorageDeviceProperty = 0,
      StorageAdapterProperty,
      StorageDeviceIdProperty,
      StorageDeviceUniqueIdProperty,
      StorageDeviceWriteCacheProperty,
      StorageMiniportProperty,
      StorageAccessAlignmentProperty,
      StorageDeviceSeekPenaltyProperty,
      StorageDeviceTrimProperty,
      StorageDeviceWriteAggregationProperty,
      StorageDeviceDeviceTelemetryProperty,
      StorageDeviceLBProvisioningProperty,
      StorageDevicePowerProperty,
      StorageDeviceCopyOffloadProperty,
      StorageDeviceResiliencyProperty,
      StorageDeviceMediumProductType,
      StorageAdapterRpmbProperty,
      StorageDeviceIoCapabilityProperty = 48,
      StorageAdapterProtocolSpecificProperty,
      StorageDeviceProtocolSpecificProperty,
      StorageAdapterTemperatureProperty,
      StorageDeviceTemperatureProperty,
      StorageAdapterPhysicalTopologyProperty,
      StorageDevicePhysicalTopologyProperty,
      StorageDeviceAttributesProperty,
      StorageDeviceManagementStatus,
      StorageAdapterSerialNumberProperty,
      StorageDeviceLocationProperty,
    }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct StoragePropertyQuery {
      public StoragePropertyId PropertyId;
      public StorageQueryType QueryType;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x1)]
      public byte[] AdditionalParameters;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct StorageDescriptorHeader {
      public uint Version;
      public uint Size;
    }
        
    [StructLayout(LayoutKind.Sequential)]
    private struct StorageDeviceDescriptor {
      public uint Version;
      public uint Size;
      public byte DeviceType;
      public byte DeviceTypeModifier;
      [MarshalAs(UnmanagedType.U1)]
      public bool RemovableMedia;
      [MarshalAs(UnmanagedType.U1)]
      public bool CommandQueueing;
      public uint VendorIdOffset;
      public uint ProductIdOffset;
      public uint ProductRevisionOffset;
      public uint SerialNumberOffset;
      public StorageBusType BusType;
      public uint RawPropertiesLength;
      //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x1)]
      //public byte[] RawDeviceProperties;
    } 
        
    private class StorageInfoImpl : StorageInfo {    
      public StorageInfoImpl(int index, IntPtr descriptorPtr) {
        StorageDeviceDescriptor descriptor = (StorageDeviceDescriptor)Marshal.PtrToStructure(descriptorPtr, typeof(StorageDeviceDescriptor));
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
        
    public static StorageInfo GetStorageInfo(int driveNumber) {      
      using(SafeHandle handle = NativeMethods.CreateFile(@"\\.\PhysicalDrive" + driveNumber,
        0, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero))
      {      
        if (handle.IsInvalid)
          return null;
        
        StoragePropertyQuery query = new StoragePropertyQuery();
        StorageDescriptorHeader header;
        uint bytesReturned = 0;
  
        query.PropertyId = StoragePropertyId.StorageDeviceProperty;
        query.QueryType = StorageQueryType.PropertyStandardQuery;
        
        if (!NativeMethods.DeviceIoControl(handle, StorageCommand.QueryProperty, ref query, 
            Marshal.SizeOf(query), out header, Marshal.SizeOf(typeof(StorageDescriptorHeader)),
            out bytesReturned, IntPtr.Zero))
          return null;
          
        IntPtr descriptorPtr = Marshal.AllocHGlobal((int)header.Size);
        try {        
          if (!NativeMethods.DeviceIoControl(handle, StorageCommand.QueryProperty, ref query, 
              Marshal.SizeOf(query), descriptorPtr, header.Size, 
              out bytesReturned, IntPtr.Zero))
            return null;
          
          return new StorageInfoImpl(driveNumber, descriptorPtr);
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
      } catch { }
      return list.ToArray();
    }
    
    private static class NativeMethods {
      private const string KERNEL = "kernel32.dll";

      [DllImport(KERNEL, CallingConvention = CallingConvention.Winapi, 
        CharSet = CharSet.Auto, SetLastError = true)]
      public static extern SafeFileHandle CreateFile(
       [MarshalAs(UnmanagedType.LPTStr)] string filename,
       [MarshalAs(UnmanagedType.U4)] FileAccess access,
       [MarshalAs(UnmanagedType.U4)] FileShare share,
       IntPtr securityAttributes, 
       [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
       [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
       IntPtr templateFile);

      [DllImport(KERNEL, CallingConvention = CallingConvention.Winapi, 
        CharSet = CharSet.Auto, SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      public static extern bool DeviceIoControl(
          SafeHandle handle, StorageCommand command,
          ref StoragePropertyQuery query, int querySize,
          out StorageDescriptorHeader descriptorHeader,
          int descriptorHeaderSize,
          out uint bytesReturned, IntPtr overlapped);      

      [DllImport(KERNEL, CallingConvention = CallingConvention.Winapi, 
        CharSet = CharSet.Auto, SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      public static extern bool DeviceIoControl(
          SafeHandle handle, StorageCommand command,
          ref StoragePropertyQuery query, int querySize,
          IntPtr descriptor, uint descriptorSize,
          out uint bytesReturned, IntPtr overlapped);      
    }    
  }
}
