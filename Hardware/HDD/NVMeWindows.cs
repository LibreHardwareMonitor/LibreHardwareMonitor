// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using OpenHardwareMonitor.Interop;

namespace OpenHardwareMonitor.Hardware.HDD {
  internal class NVMeWindows : INVMeDrive {

    //windows generic driver nvme access

    public SafeHandle Identify(StorageInfo _storageInfo) {
      return NVMeWindows._Identify(_storageInfo);
    }

    public static SafeHandle _Identify(StorageInfo _storageInfo) {

      var handle = Kernel32.OpenDevice(_storageInfo.DeviceId);
      if (handle == null || handle.IsInvalid)
        return null;

      int length;
      bool validTransfer = false;
      uint bytesReturned = 0;
      IntPtr buffer;

      Kernel32.StorageQueryWithBuffer nptwb = Kernel32.CreateStruct<Kernel32.StorageQueryWithBuffer>();
      nptwb.ProtocolSpecific.ProtocolType = Kernel32.TStroageProtocolType.ProtocolTypeNvme;
      nptwb.ProtocolSpecific.DataType = (uint)Kernel32.StorageProtocolNVMeDataType.NVMeDataTypeIdentify;
      nptwb.ProtocolSpecific.ProtocolDataOffset = (uint)Marshal.SizeOf<Kernel32.StorageProtocolSpecificData>();
      nptwb.ProtocolSpecific.ProtocolDataLength = (uint)nptwb.Buffer.Length;
      nptwb.Query.PropertyId = Kernel32.StoragePropertyId.StorageAdapterProtocolSpecificProperty;
      nptwb.Query.QueryType = Kernel32.StorageQueryType.PropertyStandardQuery;

      length = (int)Marshal.SizeOf<Kernel32.StorageQueryWithBuffer>();
      buffer = Marshal.AllocHGlobal(length);
      Marshal.StructureToPtr<Kernel32.StorageQueryWithBuffer>(nptwb, buffer, false);
      validTransfer = Kernel32.DeviceIoControl(handle, Kernel32.Command.IOCTL_STORAGE_QUERY_PROPERTY, buffer, length, buffer, length, out bytesReturned, IntPtr.Zero);
      if (validTransfer) {
        Marshal.FreeHGlobal(buffer);
      }
      else {
        Marshal.FreeHGlobal(buffer);
        handle.Close();
        handle = null;
      }
      return handle;
    }

    public bool IdentifyController(SafeHandle hDevice, out Kernel32.NVMeIdentifyControllerData data) {
      data = Kernel32.CreateStruct<Kernel32.NVMeIdentifyControllerData>();
      if (hDevice == null || hDevice.IsInvalid)
        return false;

      bool result = false;
      int length;
      bool validTransfer = false;
      uint bytesReturned = 0;
      IntPtr buffer;

      Kernel32.StorageQueryWithBuffer nptwb = Kernel32.CreateStruct<Kernel32.StorageQueryWithBuffer>();
      nptwb.ProtocolSpecific.ProtocolType = Kernel32.TStroageProtocolType.ProtocolTypeNvme;
      nptwb.ProtocolSpecific.DataType = (uint)Kernel32.StorageProtocolNVMeDataType.NVMeDataTypeIdentify;
      nptwb.ProtocolSpecific.ProtocolDataOffset = (uint)Marshal.SizeOf<Kernel32.StorageProtocolSpecificData>();
      nptwb.ProtocolSpecific.ProtocolDataLength = (uint)nptwb.Buffer.Length;
      nptwb.Query.PropertyId = Kernel32.StoragePropertyId.StorageAdapterProtocolSpecificProperty;
      nptwb.Query.QueryType = Kernel32.StorageQueryType.PropertyStandardQuery;

      length = (int)Marshal.SizeOf<Kernel32.StorageQueryWithBuffer>();
      buffer = Marshal.AllocHGlobal(length);
      Marshal.StructureToPtr<Kernel32.StorageQueryWithBuffer>(nptwb, buffer, false);
      validTransfer = Kernel32.DeviceIoControl(hDevice, Kernel32.Command.IOCTL_STORAGE_QUERY_PROPERTY, buffer, length, buffer, length, out bytesReturned, IntPtr.Zero);
      if (validTransfer) {
        //map NVMeIdentifyControllerData to nptwb.Buffer
        var offset = Marshal.OffsetOf<Kernel32.StorageQueryWithBuffer>(nameof(Kernel32.StorageQueryWithBuffer.Buffer));
        IntPtr newPtr = IntPtr.Add(buffer, offset.ToInt32());
        var item = Marshal.PtrToStructure<Kernel32.NVMeIdentifyControllerData>(newPtr);
        data = item;
        Marshal.FreeHGlobal(buffer);
        result = true;
      }
      else {
        Marshal.FreeHGlobal(buffer);
      }
      return result;
    }

    public bool HealthInfoLog(SafeHandle hDevice, out Kernel32.NVMeHealthInfoLog data) {
      data = Kernel32.CreateStruct<Kernel32.NVMeHealthInfoLog>();
      if (hDevice == null || hDevice.IsInvalid)
        return false;

      bool result = false;
      int length;
      bool validTransfer = false;
      uint bytesReturned = 0;
      IntPtr buffer;

      Kernel32.StorageQueryWithBuffer nptwb = Kernel32.CreateStruct<Kernel32.StorageQueryWithBuffer>();
      nptwb.ProtocolSpecific.ProtocolType = Kernel32.TStroageProtocolType.ProtocolTypeNvme;
      nptwb.ProtocolSpecific.DataType = (uint)Kernel32.StorageProtocolNVMeDataType.NVMeDataTypeLogPage;
      nptwb.ProtocolSpecific.ProtocolDataRequestValue = (uint)Kernel32.NVME_LOG_PAGES.NVME_LOG_PAGE_HEALTH_INFO;
      nptwb.ProtocolSpecific.ProtocolDataOffset = (uint)Marshal.SizeOf<Kernel32.StorageProtocolSpecificData>();
      nptwb.ProtocolSpecific.ProtocolDataLength = (uint)nptwb.Buffer.Length;
      nptwb.Query.PropertyId = Kernel32.StoragePropertyId.StorageAdapterProtocolSpecificProperty;
      nptwb.Query.QueryType = Kernel32.StorageQueryType.PropertyStandardQuery;

      length = (int)Marshal.SizeOf<Kernel32.StorageQueryWithBuffer>();
      buffer = Marshal.AllocHGlobal(length);
      Marshal.StructureToPtr<Kernel32.StorageQueryWithBuffer>(nptwb, buffer, false);
      validTransfer = Kernel32.DeviceIoControl(hDevice, Kernel32.Command.IOCTL_STORAGE_QUERY_PROPERTY, buffer, length, buffer, length, out bytesReturned, IntPtr.Zero);
      if (validTransfer) {
        //map NVMeHealthInfoLog to nptwb.Buffer
        var offset = Marshal.OffsetOf<Kernel32.StorageQueryWithBuffer>(nameof(Kernel32.StorageQueryWithBuffer.Buffer));
        IntPtr newPtr = IntPtr.Add(buffer, offset.ToInt32());
        var item = Marshal.PtrToStructure<Kernel32.NVMeHealthInfoLog>(newPtr);
        data = item;
        Marshal.FreeHGlobal(buffer);
        result = true;
      }
      else {
        Marshal.FreeHGlobal(buffer);
      }
      return result;
    }

  }
}