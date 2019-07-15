// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) 2016-2019 Sebastian Grams <https://github.com/sebastian-dev>
// Copyright (C) 2016-2019 Aqua Computer <https://github.com/aquacomputer, info@aqua-computer.de>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace OpenHardwareMonitor.Hardware.HDD {
  public class NVMeWindows : INVMeDrive {

    public SafeHandle Identify(StorageInfo _storageInfo) {
      return NVMeWindows._Identify(_storageInfo);
    }

    public static SafeHandle _Identify(StorageInfo _storageInfo) {

      var handle = Interop.OpenDevice(_storageInfo.DeviceId);
      if (handle == null || handle.IsInvalid)
        return null;

      int length;
      bool validTransfer = false;
      uint bytesReturned = 0;
      IntPtr buffer;

      Interop.StorageQueryWithBuffer nptwb = Interop.CreateStruct<Interop.StorageQueryWithBuffer>();
      nptwb.ProtocolSpecific.ProtocolType = Interop.TStroageProtocolType.ProtocolTypeNvme;
      nptwb.ProtocolSpecific.DataType = (uint)Interop.StorageProtocolNVMeDataType.NVMeDataTypeIdentify;
      nptwb.ProtocolSpecific.ProtocolDataOffset = (uint)Marshal.SizeOf<Interop.StorageProtocolSpecificData>();
      nptwb.ProtocolSpecific.ProtocolDataLength = (uint)nptwb.Buffer.Length;
      nptwb.Query.PropertyId = Interop.StoragePropertyId.StorageAdapterProtocolSpecificProperty;
      nptwb.Query.QueryType = Interop.StorageQueryType.PropertyStandardQuery;

      length = (int)Marshal.SizeOf<Interop.StorageQueryWithBuffer>();
      buffer = Marshal.AllocHGlobal(length);
      Marshal.StructureToPtr<Interop.StorageQueryWithBuffer>(nptwb, buffer, false);
      validTransfer = Interop.DeviceIoControl(handle, Interop.Command.IOCTL_STORAGE_QUERY_PROPERTY, buffer, length, buffer, length, out bytesReturned, IntPtr.Zero);
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

    public bool IdentifyController(SafeHandle hDevice, out Interop.NVMeIdentifyControllerData data) {
      data = Interop.CreateStruct<Interop.NVMeIdentifyControllerData>();
      if (hDevice == null || hDevice.IsInvalid)
        return false;

      bool result = false;
      int length;
      bool validTransfer = false;
      uint bytesReturned = 0;
      IntPtr buffer;

      Interop.StorageQueryWithBuffer nptwb = Interop.CreateStruct<Interop.StorageQueryWithBuffer>();
      nptwb.ProtocolSpecific.ProtocolType = Interop.TStroageProtocolType.ProtocolTypeNvme;
      nptwb.ProtocolSpecific.DataType = (uint)Interop.StorageProtocolNVMeDataType.NVMeDataTypeIdentify;
      nptwb.ProtocolSpecific.ProtocolDataOffset = (uint)Marshal.SizeOf<Interop.StorageProtocolSpecificData>();
      nptwb.ProtocolSpecific.ProtocolDataLength = (uint)nptwb.Buffer.Length;
      nptwb.Query.PropertyId = Interop.StoragePropertyId.StorageAdapterProtocolSpecificProperty;
      nptwb.Query.QueryType = Interop.StorageQueryType.PropertyStandardQuery;

      length = (int)Marshal.SizeOf<Interop.StorageQueryWithBuffer>();
      buffer = Marshal.AllocHGlobal(length);
      Marshal.StructureToPtr<Interop.StorageQueryWithBuffer>(nptwb, buffer, false);
      validTransfer = Interop.DeviceIoControl(hDevice, Interop.Command.IOCTL_STORAGE_QUERY_PROPERTY, buffer, length, buffer, length, out bytesReturned, IntPtr.Zero);
      if (validTransfer) {
        //map NVMeIdentifyControllerData to nptwb.Buffer
        var offset = Marshal.OffsetOf<Interop.StorageQueryWithBuffer>("Buffer");
        IntPtr newPtr = IntPtr.Add(buffer, offset.ToInt32());
        var item = Marshal.PtrToStructure<Interop.NVMeIdentifyControllerData>(newPtr);
        data = item;
        Marshal.FreeHGlobal(buffer);
        result = true;
      }
      else {
        Marshal.FreeHGlobal(buffer);
      }
      return result;
    }

    public bool HealthInfoLog(SafeHandle hDevice, out Interop.NVMeHealthInfoLog data) {
      data = Interop.CreateStruct<Interop.NVMeHealthInfoLog>();
      if (hDevice == null || hDevice.IsInvalid)
        return false;

      bool result = false;
      int length;
      bool validTransfer = false;
      uint bytesReturned = 0;
      IntPtr buffer;

      Interop.StorageQueryWithBuffer nptwb = Interop.CreateStruct<Interop.StorageQueryWithBuffer>();
      nptwb.ProtocolSpecific.ProtocolType = Interop.TStroageProtocolType.ProtocolTypeNvme;
      nptwb.ProtocolSpecific.DataType = (uint)Interop.StorageProtocolNVMeDataType.NVMeDataTypeLogPage;
      nptwb.ProtocolSpecific.ProtocolDataRequestValue = (uint)Interop.NVME_LOG_PAGES.NVME_LOG_PAGE_HEALTH_INFO;
      nptwb.ProtocolSpecific.ProtocolDataOffset = (uint)Marshal.SizeOf<Interop.StorageProtocolSpecificData>();
      nptwb.ProtocolSpecific.ProtocolDataLength = (uint)nptwb.Buffer.Length;
      nptwb.Query.PropertyId = Interop.StoragePropertyId.StorageAdapterProtocolSpecificProperty;
      nptwb.Query.QueryType = Interop.StorageQueryType.PropertyStandardQuery;

      length = (int)Marshal.SizeOf<Interop.StorageQueryWithBuffer>();
      buffer = Marshal.AllocHGlobal(length);
      Marshal.StructureToPtr<Interop.StorageQueryWithBuffer>(nptwb, buffer, false);
      validTransfer = Interop.DeviceIoControl(hDevice, Interop.Command.IOCTL_STORAGE_QUERY_PROPERTY, buffer, length, buffer, length, out bytesReturned, IntPtr.Zero);
      if (validTransfer) {
        //map NVMeHealthInfoLog to nptwb.Buffer
        var offset = Marshal.OffsetOf<Interop.StorageQueryWithBuffer>("Buffer");
        IntPtr newPtr = IntPtr.Add(buffer, offset.ToInt32());
        var item = Marshal.PtrToStructure<Interop.NVMeHealthInfoLog>(newPtr);
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