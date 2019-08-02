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
  internal class NVMeSamsung : INVMeDrive {

    //samsung nvme access
    //https://github.com/hiyohiyo/CrystalDiskInfo
    //https://github.com/hiyohiyo/CrystalDiskInfo/blob/master/AtaSmart.cpp

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
      Kernel32.SPTWith512Buffer sptwb = Kernel32.CreateStruct<Kernel32.SPTWith512Buffer>();

      sptwb.Spt.Length = (ushort)Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH>();
      sptwb.Spt.PathId = 0;
      sptwb.Spt.TargetId = 0;
      sptwb.Spt.Lun = 0;
      sptwb.Spt.SenseInfoLength = 24;
      sptwb.Spt.DataTransferLength = Kernel32.IDENTIFY_BUFFER_SIZE;
      sptwb.Spt.TimeOutValue = 2;
      sptwb.Spt.DataBufferOffset = Marshal.OffsetOf(typeof(Kernel32.SPTWith512Buffer), nameof(Kernel32.SPTWith512Buffer.DataBuf));
      sptwb.Spt.SenseInfoOffset = (uint)Marshal.OffsetOf(typeof(Kernel32.SPTWith512Buffer), nameof(Kernel32.SPTWith512Buffer.SenseBuf));
      sptwb.Spt.CdbLength = 16;
      sptwb.Spt.Cdb[0] = 0xB5; // SECURITY PROTOCOL IN
      sptwb.Spt.Cdb[1] = 0xFE; // Samsung Protocol
      sptwb.Spt.Cdb[3] = 5;    // Identify
      sptwb.Spt.Cdb[8] = 0;   // Transfer Length
      sptwb.Spt.Cdb[9] = 0x40;
      sptwb.Spt.DataIn = (byte)Kernel32.SCSI_IOCTL_DATA_OUT;
      sptwb.DataBuf[0] = 1;

      length = (int)Marshal.SizeOf<Kernel32.SPTWith512Buffer>();
      buffer = Marshal.AllocHGlobal(length);
      Marshal.StructureToPtr<Kernel32.SPTWith512Buffer>(sptwb, buffer, false);
      validTransfer = Kernel32.DeviceIoControl(handle, Kernel32.Command.IOCTL_SCSI_PASS_THROUGH, buffer, length, buffer, length, out bytesReturned, IntPtr.Zero);
      Marshal.FreeHGlobal(buffer);

      if (validTransfer) {
        //read data from samsung SSD
        sptwb = Kernel32.CreateStruct<Kernel32.SPTWith512Buffer>();
        sptwb.Spt.Length = (ushort)Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH>();
        sptwb.Spt.PathId = 0;
        sptwb.Spt.TargetId = 0;
        sptwb.Spt.Lun = 0;
        sptwb.Spt.SenseInfoLength = 24;
        sptwb.Spt.DataTransferLength = Kernel32.IDENTIFY_BUFFER_SIZE;
        sptwb.Spt.TimeOutValue = 2;
        sptwb.Spt.DataBufferOffset = Marshal.OffsetOf(typeof(Kernel32.SPTWith512Buffer), nameof(Kernel32.SPTWith512Buffer.DataBuf));
        sptwb.Spt.SenseInfoOffset = (uint)Marshal.OffsetOf(typeof(Kernel32.SPTWith512Buffer), nameof(Kernel32.SPTWith512Buffer.SenseBuf));
        sptwb.Spt.CdbLength = 16;
        sptwb.Spt.Cdb[0] = 0xA2; // SECURITY PROTOCOL IN
        sptwb.Spt.Cdb[1] = 0xFE; // Samsung Protocol
        sptwb.Spt.Cdb[3] = 5;    // Identify
        sptwb.Spt.Cdb[8] = 2; // Transfer Length
        sptwb.Spt.Cdb[9] = 0;
        sptwb.Spt.DataIn = Kernel32.SCSI_IOCTL_DATA_IN;

        length = (int)Marshal.SizeOf<Kernel32.SPTWith512Buffer>();
        buffer = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr<Kernel32.SPTWith512Buffer>(sptwb, buffer, false);

        validTransfer = Kernel32.DeviceIoControl(handle, Kernel32.Command.IOCTL_SCSI_PASS_THROUGH, buffer, length, buffer, length, out bytesReturned, IntPtr.Zero);
        if (validTransfer) {
          var item = Marshal.PtrToStructure<Kernel32.SPTWith512Buffer>(buffer);
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

    public bool IdentifyController(SafeHandle hDevice, out Kernel32.NVMeIdentifyControllerData data) {
      data = Kernel32.CreateStruct<Kernel32.NVMeIdentifyControllerData>();
      if (hDevice == null || hDevice.IsInvalid)
        return false;

      bool result = false;
      int length;
      bool validTransfer = false;
      uint bytesReturned = 0;
      IntPtr buffer;
      Kernel32.SPTWith512Buffer sptwb = Kernel32.CreateStruct<Kernel32.SPTWith512Buffer>();

      sptwb.Spt.Length = (ushort)Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH>();
      sptwb.Spt.PathId = 0;
      sptwb.Spt.TargetId = 0;
      sptwb.Spt.Lun = 0;
      sptwb.Spt.SenseInfoLength = 24;
      sptwb.Spt.DataTransferLength = Kernel32.IDENTIFY_BUFFER_SIZE;
      sptwb.Spt.TimeOutValue = 2;
      sptwb.Spt.DataBufferOffset = Marshal.OffsetOf(typeof(Kernel32.SPTWith512Buffer), nameof(Kernel32.SPTWith512Buffer.DataBuf));
      sptwb.Spt.SenseInfoOffset = (uint)Marshal.OffsetOf(typeof(Kernel32.SPTWith512Buffer), nameof(Kernel32.SPTWith512Buffer.SenseBuf));
      sptwb.Spt.CdbLength = 16;
      sptwb.Spt.Cdb[0] = 0xB5;    // SECURITY PROTOCOL IN
      sptwb.Spt.Cdb[1] = 0xFE;    // Samsung Protocol
      sptwb.Spt.Cdb[3] = 5;       // Identify
      sptwb.Spt.Cdb[8] = 0;       // Transfer Length
      sptwb.Spt.Cdb[9] = 0x40;    // Transfer Length
      sptwb.Spt.DataIn = (byte)Kernel32.SCSI_IOCTL_DATA_OUT;
      sptwb.DataBuf[0] = 1;

      length = (int)Marshal.SizeOf<Kernel32.SPTWith512Buffer>();
      buffer = Marshal.AllocHGlobal(length);
      Marshal.StructureToPtr<Kernel32.SPTWith512Buffer>(sptwb, buffer, false);
      validTransfer = Kernel32.DeviceIoControl(hDevice, Kernel32.Command.IOCTL_SCSI_PASS_THROUGH, buffer, length, buffer, length, out bytesReturned, IntPtr.Zero);
      Marshal.FreeHGlobal(buffer);

      if (validTransfer) {
        //read data from samsung SSD
        sptwb = Kernel32.CreateStruct<Kernel32.SPTWith512Buffer>();
        sptwb.Spt.Length = (ushort)Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH>();
        sptwb.Spt.PathId = 0;
        sptwb.Spt.TargetId = 0;
        sptwb.Spt.Lun = 0;
        sptwb.Spt.SenseInfoLength = 24;
        sptwb.Spt.DataTransferLength = Kernel32.IDENTIFY_BUFFER_SIZE;
        sptwb.Spt.TimeOutValue = 2;
        sptwb.Spt.DataBufferOffset = Marshal.OffsetOf<Kernel32.SPTWith512Buffer>(nameof(Kernel32.SPTWith512Buffer.DataBuf));
        sptwb.Spt.SenseInfoOffset = (uint)Marshal.OffsetOf<Kernel32.SPTWith512Buffer>(nameof(Kernel32.SPTWith512Buffer.SenseBuf));
        sptwb.Spt.CdbLength = 16;
        sptwb.Spt.Cdb[0] = 0xA2;  // SECURITY PROTOCOL IN
        sptwb.Spt.Cdb[1] = 0xFE;  // Samsung Protocol
        sptwb.Spt.Cdb[3] = 5;     // Identify
        sptwb.Spt.Cdb[8] = 2;     // Transfer Length (high)
        sptwb.Spt.Cdb[9] = 0;     // Transfer Length (low)
        sptwb.Spt.DataIn = Kernel32.SCSI_IOCTL_DATA_IN;

        length = (int)Marshal.SizeOf<Kernel32.SPTWith512Buffer>();
        buffer = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr<Kernel32.SPTWith512Buffer>(sptwb, buffer, false);

        validTransfer = Kernel32.DeviceIoControl(hDevice, Kernel32.Command.IOCTL_SCSI_PASS_THROUGH, buffer, length, buffer, length, out bytesReturned, IntPtr.Zero);
        if (validTransfer) {
          var offset = Marshal.OffsetOf<Kernel32.SPTWith512Buffer>(nameof(Kernel32.SPTWith512Buffer.DataBuf));
          IntPtr newPtr = IntPtr.Add(buffer, offset.ToInt32());
          var item = Marshal.PtrToStructure<Kernel32.NVMeIdentifyControllerData>(newPtr);
          data = item;
          Marshal.FreeHGlobal(buffer);
          result = true;
        }
        else {
          Marshal.FreeHGlobal(buffer);
        }
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
      Kernel32.SPTWith512Buffer sptwb = Kernel32.CreateStruct<Kernel32.SPTWith512Buffer>();

      sptwb.Spt.Length = (ushort)Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH>();
      sptwb.Spt.PathId = 0;
      sptwb.Spt.TargetId = 0;
      sptwb.Spt.Lun = 0;
      sptwb.Spt.SenseInfoLength = 24;
      sptwb.Spt.DataTransferLength = Kernel32.IDENTIFY_BUFFER_SIZE;
      sptwb.Spt.TimeOutValue = 2;
      sptwb.Spt.DataBufferOffset = Marshal.OffsetOf<Kernel32.SPTWith512Buffer>(nameof(Kernel32.SPTWith512Buffer.DataBuf));
      sptwb.Spt.SenseInfoOffset = (uint)Marshal.OffsetOf<Kernel32.SPTWith512Buffer>(nameof(Kernel32.SPTWith512Buffer.SenseBuf));
      sptwb.Spt.CdbLength = 16;
      sptwb.Spt.Cdb[0] = 0xB5;    // SECURITY PROTOCOL IN
      sptwb.Spt.Cdb[1] = 0xFE;    // Samsung Protocol
      sptwb.Spt.Cdb[3] = 6;       // Log Data
      sptwb.Spt.Cdb[8] = 0;       // Transfer Length
      sptwb.Spt.Cdb[9] = 0x40;    // Transfer Length
      sptwb.Spt.DataIn = (byte)Kernel32.SCSI_IOCTL_DATA_OUT;
      sptwb.DataBuf[0] = 2;
      sptwb.DataBuf[4] = 0xff;
      sptwb.DataBuf[5] = 0xff;
      sptwb.DataBuf[6] = 0xff;
      sptwb.DataBuf[7] = 0xff;

      length = (int)Marshal.SizeOf<Kernel32.SPTWith512Buffer>();
      buffer = Marshal.AllocHGlobal(length);
      Marshal.StructureToPtr<Kernel32.SPTWith512Buffer>(sptwb, buffer, false);
      validTransfer = Kernel32.DeviceIoControl(hDevice, Kernel32.Command.IOCTL_SCSI_PASS_THROUGH, buffer, length, buffer, length, out bytesReturned, IntPtr.Zero);
      Marshal.FreeHGlobal(buffer);

      if (validTransfer) {
        //read data from samsung SSD
        sptwb = Kernel32.CreateStruct<Kernel32.SPTWith512Buffer>();
        sptwb.Spt.Length = (ushort)Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH>();
        sptwb.Spt.PathId = 0;
        sptwb.Spt.TargetId = 0;
        sptwb.Spt.Lun = 0;
        sptwb.Spt.SenseInfoLength = 24;
        sptwb.Spt.DataTransferLength = Kernel32.IDENTIFY_BUFFER_SIZE;
        sptwb.Spt.TimeOutValue = 2;
        sptwb.Spt.DataBufferOffset = Marshal.OffsetOf<Kernel32.SPTWith512Buffer>(nameof(Kernel32.SPTWith512Buffer.DataBuf));
        sptwb.Spt.SenseInfoOffset = (uint)Marshal.OffsetOf<Kernel32.SPTWith512Buffer>(nameof(Kernel32.SPTWith512Buffer.SenseBuf));
        sptwb.Spt.CdbLength = 16;
        sptwb.Spt.Cdb[0] = 0xA2;  // SECURITY PROTOCOL IN
        sptwb.Spt.Cdb[1] = 0xFE;  // Samsung Protocol
        sptwb.Spt.Cdb[3] = 6;     // Log Data
        sptwb.Spt.Cdb[8] = 2;     // Transfer Length (high)
        sptwb.Spt.Cdb[9] = 0;     // Transfer Length (low)
        sptwb.Spt.DataIn = Kernel32.SCSI_IOCTL_DATA_IN;

        length = (int)Marshal.SizeOf<Kernel32.SPTWith512Buffer>();
        buffer = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr<Kernel32.SPTWith512Buffer>(sptwb, buffer, false);

        validTransfer = Kernel32.DeviceIoControl(hDevice, Kernel32.Command.IOCTL_SCSI_PASS_THROUGH, buffer, length, buffer, length, out bytesReturned, IntPtr.Zero);
        if (validTransfer) {
          var offset = Marshal.OffsetOf<Kernel32.SPTWith512Buffer>(nameof(Kernel32.SPTWith512Buffer.DataBuf));
          IntPtr newPtr = IntPtr.Add(buffer, offset.ToInt32());
          var item = Marshal.PtrToStructure<Kernel32.NVMeHealthInfoLog>(newPtr);
          data = item;
          Marshal.FreeHGlobal(buffer);
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