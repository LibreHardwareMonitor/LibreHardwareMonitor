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
  internal class NVMeIntel : INVMeDrive {

    //intel nvme access

    public SafeHandle Identify(StorageInfo _storageInfo) {
      return NVMeWindows._Identify(_storageInfo);
    }

    public static SafeHandle _Identify(StorageInfo _storageInfo) {

      var handle = Kernel32.OpenDevice(_storageInfo.Scsi);
      if (handle == null || handle.IsInvalid)
        return null;

      int length;
      bool validTransfer = false;
      uint bytesReturned = 0;
      IntPtr buffer;

      Kernel32.NVMePassThrough passThrough = Kernel32.CreateStruct<Kernel32.NVMePassThrough>();
      passThrough.srb.HeaderLenght = (uint)Marshal.SizeOf<Kernel32.SrbIoControl>();
      passThrough.srb.Signature = Encoding.ASCII.GetBytes(Kernel32.IntelNVMeMiniPortSignature1);
      passThrough.srb.Timeout = 10;
      passThrough.srb.ControlCode = Kernel32.NVMePassThroughSrbIoCode;
      passThrough.srb.ReturnCode = 0;
      passThrough.srb.Length = (uint)Marshal.SizeOf<Kernel32.NVMePassThrough>() -  (uint)Marshal.SizeOf<Kernel32.SrbIoControl>();
      passThrough.NVMeCmd = new uint[16];
      passThrough.NVMeCmd[0] = 6; //identify
      passThrough.NVMeCmd[10] = 1; //return to host
      passThrough.Direction = Kernel32.NVMePassThroughDirection.In;
      passThrough.Queue = Kernel32.NVMePassThroughQueue.AdminQ;
      passThrough.DataBufferLen = (uint)passThrough.DataBuf.Length;
      passThrough.MetaDataLen = 0;
      passThrough.ReturnBufferLen = (uint)Marshal.SizeOf<Kernel32.NVMePassThrough>();

      length = (int)Marshal.SizeOf<Kernel32.NVMePassThrough>();
      buffer = Marshal.AllocHGlobal(length);
      Marshal.StructureToPtr<Kernel32.NVMePassThrough>(passThrough, buffer, false);

      validTransfer = Kernel32.DeviceIoControl(handle, Kernel32.Command.IOCTL_SCSI_MINIPORT, buffer, length, buffer, length, out bytesReturned, IntPtr.Zero);
      Marshal.FreeHGlobal(buffer);

      if (validTransfer) {

      } else {
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

      Kernel32.NVMePassThrough passThrough = Kernel32.CreateStruct<Kernel32.NVMePassThrough>();
      passThrough.srb.HeaderLenght = (uint)Marshal.SizeOf<Kernel32.SrbIoControl>();
      passThrough.srb.Signature = Encoding.ASCII.GetBytes(Kernel32.IntelNVMeMiniPortSignature1);
      passThrough.srb.Timeout = 10;
      passThrough.srb.ControlCode = Kernel32.NVMePassThroughSrbIoCode;
      passThrough.srb.ReturnCode = 0;
      passThrough.srb.Length = (uint)Marshal.SizeOf<Kernel32.NVMePassThrough>() - (uint)Marshal.SizeOf<Kernel32.SrbIoControl>();
      passThrough.NVMeCmd = new uint[16];
      passThrough.NVMeCmd[0] = 6; //identify
      passThrough.NVMeCmd[10] = 1; //return to host
      passThrough.Direction = Kernel32.NVMePassThroughDirection.In;
      passThrough.Queue = Kernel32.NVMePassThroughQueue.AdminQ;
      passThrough.DataBufferLen = (uint)passThrough.DataBuf.Length;
      passThrough.MetaDataLen = 0;
      passThrough.ReturnBufferLen = (uint)Marshal.SizeOf<Kernel32.NVMePassThrough>();

      length = (int)Marshal.SizeOf<Kernel32.NVMePassThrough>();
      buffer = Marshal.AllocHGlobal(length);
      Marshal.StructureToPtr<Kernel32.NVMePassThrough>(passThrough, buffer, false);

      validTransfer = Kernel32.DeviceIoControl(hDevice, Kernel32.Command.IOCTL_SCSI_MINIPORT, buffer, length, buffer, length, out bytesReturned, IntPtr.Zero);
      if (validTransfer) {
        var offset = Marshal.OffsetOf<Kernel32.NVMePassThrough>(nameof(Kernel32.NVMePassThrough.DataBuf));
        IntPtr newPtr = IntPtr.Add(buffer, offset.ToInt32());
        int finalSize = Marshal.SizeOf<Kernel32.NVMeIdentifyControllerData>();
        var ptr = Marshal.AllocHGlobal(Marshal.SizeOf<Kernel32.NVMeIdentifyControllerData>());
        Kernel32.RtlZeroMemory(ptr, finalSize);
        int len = Math.Min(finalSize, passThrough.DataBuf.Length);
        Kernel32.CopyMemory(ptr, newPtr, (uint)len);
        Marshal.FreeHGlobal(buffer);

        var item = Marshal.PtrToStructure<Kernel32.NVMeIdentifyControllerData>(ptr);
        data = item;
        Marshal.FreeHGlobal(ptr);
        result = true;

      } else {
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

      Kernel32.NVMePassThrough passThrough = Kernel32.CreateStruct<Kernel32.NVMePassThrough>();
      passThrough.srb.HeaderLenght = (uint)Marshal.SizeOf<Kernel32.SrbIoControl>();
      passThrough.srb.Signature = Encoding.ASCII.GetBytes(Kernel32.IntelNVMeMiniPortSignature1);
      passThrough.srb.Timeout = 10;
      passThrough.srb.ControlCode = Kernel32.NVMePassThroughSrbIoCode;
      passThrough.srb.ReturnCode = 0;
      passThrough.srb.Length = (uint)Marshal.SizeOf<Kernel32.NVMePassThrough>() - (uint)Marshal.SizeOf<Kernel32.SrbIoControl>();
      passThrough.NVMeCmd[0] = (uint)Kernel32.StorageProtocolNVMeDataType.NVMeDataTypeLogPage;  // GetLogPage
      passThrough.NVMeCmd[1] = 0xFFFFFFFF;  // address
      passThrough.NVMeCmd[10] = 0x007f0002; // uint cdw10 = 0x000000002 | (((size / 4) - 1) << 16);
      passThrough.Direction = Kernel32.NVMePassThroughDirection.In;
      passThrough.Queue = Kernel32.NVMePassThroughQueue.AdminQ;
      passThrough.DataBufferLen = (uint)passThrough.DataBuf.Length;
      passThrough.MetaDataLen = 0;
      passThrough.ReturnBufferLen = (uint)Marshal.SizeOf<Kernel32.NVMePassThrough>();

      length = (int)Marshal.SizeOf<Kernel32.NVMePassThrough>();
      buffer = Marshal.AllocHGlobal(length);
      Marshal.StructureToPtr<Kernel32.NVMePassThrough>(passThrough, buffer, false);

      validTransfer = Kernel32.DeviceIoControl(hDevice, Kernel32.Command.IOCTL_SCSI_MINIPORT, buffer, length, buffer, length, out bytesReturned, IntPtr.Zero);
      if (validTransfer) {
        var offset = Marshal.OffsetOf<Kernel32.NVMePassThrough>(nameof(Kernel32.NVMePassThrough.DataBuf));
        IntPtr newPtr = IntPtr.Add(buffer, offset.ToInt32());
        var item = Marshal.PtrToStructure<Kernel32.NVMeHealthInfoLog>(newPtr);
        data = item;
        Marshal.FreeHGlobal(buffer);
        result = true;
      } else {
        Marshal.FreeHGlobal(buffer);
      }

      return result;
    }

  }
}