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
  public class NVMeSamsung : INVMeDrive {

    //samsung spcific nvme access
    //https://github.com/hiyohiyo/CrystalDiskInfo
    //https://github.com/hiyohiyo/CrystalDiskInfo/blob/master/AtaSmart.cpp

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
      Interop.SPTWith512Buffer sptwb = Interop.CreateStruct<Interop.SPTWith512Buffer>();

      sptwb.Spt.Length = (ushort)Marshal.SizeOf<Interop.SCSI_PASS_THROUGH>();
      sptwb.Spt.PathId = 0;
      sptwb.Spt.TargetId = 0;
      sptwb.Spt.Lun = 0;
      sptwb.Spt.SenseInfoLength = 24;
      sptwb.Spt.DataTransferLength = Interop.IDENTIFY_BUFFER_SIZE;
      sptwb.Spt.TimeOutValue = 2;
      sptwb.Spt.DataBufferOffset = Marshal.OffsetOf(typeof(Interop.SPTWith512Buffer), "DataBuf");
      sptwb.Spt.SenseInfoOffset = (uint)Marshal.OffsetOf(typeof(Interop.SPTWith512Buffer), "SenseBuf");
      sptwb.Spt.CdbLength = 16;
      sptwb.Spt.Cdb[0] = 0xB5; // SECURITY PROTOCOL IN
      sptwb.Spt.Cdb[1] = 0xFE; // Samsung Protocol
      sptwb.Spt.Cdb[3] = 5;    // Identify
      sptwb.Spt.Cdb[8] = 0;   // Transfer Length
      sptwb.Spt.Cdb[9] = 0x40;
      sptwb.Spt.DataIn = (byte)Interop.SCSI_IOCTL_DATA_OUT;
      sptwb.DataBuf[0] = 1;

      length = (int)Marshal.SizeOf<Interop.SPTWith512Buffer>();
      buffer = Marshal.AllocHGlobal(length);
      Marshal.StructureToPtr<Interop.SPTWith512Buffer>(sptwb, buffer, false);
      validTransfer = Interop.DeviceIoControl(handle, Interop.Command.IOCTL_SCSI_PASS_THROUGH, buffer, length, buffer, length, out bytesReturned, IntPtr.Zero);
      Marshal.FreeHGlobal(buffer);

      if (validTransfer) {
        //read data from samsung SSD
        sptwb = Interop.CreateStruct<Interop.SPTWith512Buffer>();
        sptwb.Spt.Length = (ushort)Marshal.SizeOf<Interop.SCSI_PASS_THROUGH>();
        sptwb.Spt.PathId = 0;
        sptwb.Spt.TargetId = 0;
        sptwb.Spt.Lun = 0;
        sptwb.Spt.SenseInfoLength = 24;
        sptwb.Spt.DataTransferLength = Interop.IDENTIFY_BUFFER_SIZE;
        sptwb.Spt.TimeOutValue = 2;
        sptwb.Spt.DataBufferOffset = Marshal.OffsetOf(typeof(Interop.SPTWith512Buffer), "DataBuf");
        sptwb.Spt.SenseInfoOffset = (uint)Marshal.OffsetOf(typeof(Interop.SPTWith512Buffer), "SenseBuf");
        sptwb.Spt.CdbLength = 16;
        sptwb.Spt.Cdb[0] = 0xA2; // SECURITY PROTOCOL IN
        sptwb.Spt.Cdb[1] = 0xFE; // Samsung Protocol
        sptwb.Spt.Cdb[3] = 5;    // Identify
        sptwb.Spt.Cdb[8] = 2; // Transfer Length
        sptwb.Spt.Cdb[9] = 0;
        sptwb.Spt.DataIn = Interop.SCSI_IOCTL_DATA_IN;

        length = (int)Marshal.SizeOf<Interop.SPTWith512Buffer>();
        buffer = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr<Interop.SPTWith512Buffer>(sptwb, buffer, false);

        validTransfer = Interop.DeviceIoControl(handle, Interop.Command.IOCTL_SCSI_PASS_THROUGH, buffer, length, buffer, length, out bytesReturned, IntPtr.Zero);
        if (validTransfer) {
          var item = Marshal.PtrToStructure<Interop.SPTWith512Buffer>(buffer);
          Marshal.FreeHGlobal(buffer);
        }
        else {
          Marshal.FreeHGlobal(buffer);
          handle.Close();
          handle = null;
        }
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
      Interop.SPTWith512Buffer sptwb = Interop.CreateStruct<Interop.SPTWith512Buffer>();

      sptwb.Spt.Length = (ushort)Marshal.SizeOf<Interop.SCSI_PASS_THROUGH>();
      sptwb.Spt.PathId = 0;
      sptwb.Spt.TargetId = 0;
      sptwb.Spt.Lun = 0;
      sptwb.Spt.SenseInfoLength = 24;
      sptwb.Spt.DataTransferLength = Interop.IDENTIFY_BUFFER_SIZE;
      sptwb.Spt.TimeOutValue = 2;
      sptwb.Spt.DataBufferOffset = Marshal.OffsetOf(typeof(Interop.SPTWith512Buffer), "DataBuf");
      sptwb.Spt.SenseInfoOffset = (uint)Marshal.OffsetOf(typeof(Interop.SPTWith512Buffer), "SenseBuf");
      sptwb.Spt.CdbLength = 16;
      sptwb.Spt.Cdb[0] = 0xB5;    // SECURITY PROTOCOL IN
      sptwb.Spt.Cdb[1] = 0xFE;    // Samsung Protocol
      sptwb.Spt.Cdb[3] = 5;       // Identify
      sptwb.Spt.Cdb[8] = 0;       // Transfer Length
      sptwb.Spt.Cdb[9] = 0x40;    // Transfer Length
      sptwb.Spt.DataIn = (byte)Interop.SCSI_IOCTL_DATA_OUT;
      sptwb.DataBuf[0] = 1;

      length = (int)Marshal.SizeOf<Interop.SPTWith512Buffer>();
      buffer = Marshal.AllocHGlobal(length);
      Marshal.StructureToPtr<Interop.SPTWith512Buffer>(sptwb, buffer, false);
      validTransfer = Interop.DeviceIoControl(hDevice, Interop.Command.IOCTL_SCSI_PASS_THROUGH, buffer, length, buffer, length, out bytesReturned, IntPtr.Zero);
      Marshal.FreeHGlobal(buffer);

      if (validTransfer) {
        //read data from samsung SSD
        sptwb = Interop.CreateStruct<Interop.SPTWith512Buffer>();
        sptwb.Spt.Length = (ushort)Marshal.SizeOf<Interop.SCSI_PASS_THROUGH>();
        sptwb.Spt.PathId = 0;
        sptwb.Spt.TargetId = 0;
        sptwb.Spt.Lun = 0;
        sptwb.Spt.SenseInfoLength = 24;
        sptwb.Spt.DataTransferLength = Interop.IDENTIFY_BUFFER_SIZE;
        sptwb.Spt.TimeOutValue = 2;
        sptwb.Spt.DataBufferOffset = Marshal.OffsetOf<Interop.SPTWith512Buffer>("DataBuf");
        sptwb.Spt.SenseInfoOffset = (uint)Marshal.OffsetOf<Interop.SPTWith512Buffer>("SenseBuf");
        sptwb.Spt.CdbLength = 16;
        sptwb.Spt.Cdb[0] = 0xA2;  // SECURITY PROTOCOL IN
        sptwb.Spt.Cdb[1] = 0xFE;  // Samsung Protocol
        sptwb.Spt.Cdb[3] = 5;     // Identify
        sptwb.Spt.Cdb[8] = 2;     // Transfer Length (high)
        sptwb.Spt.Cdb[9] = 0;     // Transfer Length (low)
        sptwb.Spt.DataIn = Interop.SCSI_IOCTL_DATA_IN;

        length = (int)Marshal.SizeOf<Interop.SPTWith512Buffer>();
        buffer = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr<Interop.SPTWith512Buffer>(sptwb, buffer, false);

        validTransfer = Interop.DeviceIoControl(hDevice, Interop.Command.IOCTL_SCSI_PASS_THROUGH, buffer, length, buffer, length, out bytesReturned, IntPtr.Zero);
        if (validTransfer) {
          var offset = Marshal.OffsetOf<Interop.SPTWith512Buffer>("DataBuf");
          IntPtr newPtr = IntPtr.Add(buffer, offset.ToInt32());
          int finalSize = Marshal.SizeOf<Interop.NVMeIdentifyControllerData>();
          var ptr = Marshal.AllocHGlobal(Marshal.SizeOf<Interop.NVMeIdentifyControllerData>());
          Interop.RtlZeroMemory(ptr, finalSize);
          Interop.CopyMemory(ptr, newPtr, (uint)sptwb.DataBuf.Length);
          Marshal.FreeHGlobal(buffer);
          var item = Marshal.PtrToStructure<Interop.NVMeIdentifyControllerData>(ptr);
          data = item;
          Marshal.FreeHGlobal(ptr);
          result = true;
        }
        else {
          Marshal.FreeHGlobal(buffer);
        }
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
      Interop.SPTWith512Buffer sptwb = Interop.CreateStruct<Interop.SPTWith512Buffer>();

      sptwb.Spt.Length = (ushort)Marshal.SizeOf<Interop.SCSI_PASS_THROUGH>();
      sptwb.Spt.PathId = 0;
      sptwb.Spt.TargetId = 0;
      sptwb.Spt.Lun = 0;
      sptwb.Spt.SenseInfoLength = 24;
      sptwb.Spt.DataTransferLength = Interop.IDENTIFY_BUFFER_SIZE;
      sptwb.Spt.TimeOutValue = 2;
      sptwb.Spt.DataBufferOffset = Marshal.OffsetOf<Interop.SPTWith512Buffer>("DataBuf");
      sptwb.Spt.SenseInfoOffset = (uint)Marshal.OffsetOf<Interop.SPTWith512Buffer>("SenseBuf");
      sptwb.Spt.CdbLength = 16;
      sptwb.Spt.Cdb[0] = 0xB5;    // SECURITY PROTOCOL IN
      sptwb.Spt.Cdb[1] = 0xFE;    // Samsung Protocol
      sptwb.Spt.Cdb[3] = 6;       // Log Data
      sptwb.Spt.Cdb[8] = 0;       // Transfer Length
      sptwb.Spt.Cdb[9] = 0x40;    // Transfer Length
      sptwb.Spt.DataIn = (byte)Interop.SCSI_IOCTL_DATA_OUT;
      sptwb.DataBuf[0] = 2;
      sptwb.DataBuf[4] = 0xff;
      sptwb.DataBuf[5] = 0xff;
      sptwb.DataBuf[6] = 0xff;
      sptwb.DataBuf[7] = 0xff;

      length = (int)Marshal.SizeOf<Interop.SPTWith512Buffer>();
      buffer = Marshal.AllocHGlobal(length);
      Marshal.StructureToPtr<Interop.SPTWith512Buffer>(sptwb, buffer, false);
      validTransfer = Interop.DeviceIoControl(hDevice, Interop.Command.IOCTL_SCSI_PASS_THROUGH, buffer, length, buffer, length, out bytesReturned, IntPtr.Zero);
      Marshal.FreeHGlobal(buffer);

      if (validTransfer) {
        //read data from samsung SSD
        sptwb = Interop.CreateStruct<Interop.SPTWith512Buffer>();
        sptwb.Spt.Length = (ushort)Marshal.SizeOf<Interop.SCSI_PASS_THROUGH>();
        sptwb.Spt.PathId = 0;
        sptwb.Spt.TargetId = 0;
        sptwb.Spt.Lun = 0;
        sptwb.Spt.SenseInfoLength = 24;
        sptwb.Spt.DataTransferLength = Interop.IDENTIFY_BUFFER_SIZE;
        sptwb.Spt.TimeOutValue = 2;
        sptwb.Spt.DataBufferOffset = Marshal.OffsetOf<Interop.SPTWith512Buffer>("DataBuf");
        sptwb.Spt.SenseInfoOffset = (uint)Marshal.OffsetOf<Interop.SPTWith512Buffer>("SenseBuf");
        sptwb.Spt.CdbLength = 16;
        sptwb.Spt.Cdb[0] = 0xA2;  // SECURITY PROTOCOL IN
        sptwb.Spt.Cdb[1] = 0xFE;  // Samsung Protocol
        sptwb.Spt.Cdb[3] = 6;     // Log Data
        sptwb.Spt.Cdb[8] = 2;     // Transfer Length (high)
        sptwb.Spt.Cdb[9] = 0;     // Transfer Length (low)
        sptwb.Spt.DataIn = Interop.SCSI_IOCTL_DATA_IN;

        length = (int)Marshal.SizeOf<Interop.SPTWith512Buffer>();
        buffer = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr<Interop.SPTWith512Buffer>(sptwb, buffer, false);

        validTransfer = Interop.DeviceIoControl(hDevice, Interop.Command.IOCTL_SCSI_PASS_THROUGH, buffer, length, buffer, length, out bytesReturned, IntPtr.Zero);
        if (validTransfer) {
          var offset = Marshal.OffsetOf<Interop.SPTWith512Buffer>("DataBuf");
          IntPtr newPtr = IntPtr.Add(buffer, offset.ToInt32());
          int finalSize = Marshal.SizeOf<Interop.NVMeIdentifyControllerData>();
          var ptr = Marshal.AllocHGlobal(Marshal.SizeOf<Interop.NVMeIdentifyControllerData>());
          Interop.RtlZeroMemory(ptr, finalSize);
          Interop.CopyMemory(ptr, newPtr, (uint)sptwb.DataBuf.Length);
          Marshal.FreeHGlobal(buffer);
          var item = Marshal.PtrToStructure<Interop.NVMeHealthInfoLog>(ptr);
          data = item;
          Marshal.FreeHGlobal(ptr);
          result = true;
        }
        else {
          Marshal.FreeHGlobal(buffer);
        }
      }

      return result;
    }

  }
}