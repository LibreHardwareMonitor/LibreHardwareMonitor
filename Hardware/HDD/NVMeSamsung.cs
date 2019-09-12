// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Runtime.InteropServices;
using OpenHardwareMonitor.Interop;

namespace OpenHardwareMonitor.Hardware.HDD {
  internal class NVMeSamsung : INVMeDrive {
    //samsung nvme access
    //https://github.com/hiyohiyo/CrystalDiskInfo
    //https://github.com/hiyohiyo/CrystalDiskInfo/blob/master/AtaSmart.cpp

    public SafeHandle Identify(StorageInfo storageInfo) {
      return NVMeWindows.IdentifyDevice(storageInfo);
    }

    public bool IdentifyController(SafeHandle hDevice, out Kernel32.NVME_IDENTIFY_CONTROLLER_DATA data) {
      data = Kernel32.CreateStruct<Kernel32.NVME_IDENTIFY_CONTROLLER_DATA>();
      if (hDevice == null || hDevice.IsInvalid)
        return false;


      bool result = false;
      IntPtr buffer;
      Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS sptwb = Kernel32.CreateStruct<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>();

      sptwb.Spt.Length = (ushort) Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH>();
      sptwb.Spt.PathId = 0;
      sptwb.Spt.TargetId = 0;
      sptwb.Spt.Lun = 0;
      sptwb.Spt.SenseInfoLength = 24;
      sptwb.Spt.DataTransferLength = Kernel32.SCSI_PASS_THROUGH_BUFFER_SIZE;
      sptwb.Spt.TimeOutValue = 2;
      sptwb.Spt.DataBufferOffset = Marshal.OffsetOf(typeof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS), nameof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS.DataBuf));
      sptwb.Spt.SenseInfoOffset = (uint) Marshal.OffsetOf(typeof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS), nameof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS.SenseBuf));
      sptwb.Spt.CdbLength = 16;
      sptwb.Spt.Cdb[0] = 0xB5; // SECURITY PROTOCOL IN
      sptwb.Spt.Cdb[1] = 0xFE; // Samsung Protocol
      sptwb.Spt.Cdb[3] = 5; // Identify
      sptwb.Spt.Cdb[8] = 0; // Transfer Length
      sptwb.Spt.Cdb[9] = 0x40; // Transfer Length
      sptwb.Spt.DataIn = (byte) Kernel32.SCSI_IOCTL_DATA.SCSI_IOCTL_DATA_OUT;
      sptwb.DataBuf[0] = 1;

      var length = Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>();
      buffer = Marshal.AllocHGlobal(length);
      Marshal.StructureToPtr(sptwb, buffer, false);
      var validTransfer = Kernel32.DeviceIoControl(hDevice, Kernel32.IOCTL.IOCTL_SCSI_PASS_THROUGH, buffer, length, buffer, length, out _, IntPtr.Zero);
      Marshal.FreeHGlobal(buffer);

      if (validTransfer) {
        //read data from samsung SSD
        sptwb = Kernel32.CreateStruct<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>();
        sptwb.Spt.Length = (ushort) Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH>();
        sptwb.Spt.PathId = 0;
        sptwb.Spt.TargetId = 0;
        sptwb.Spt.Lun = 0;
        sptwb.Spt.SenseInfoLength = 24;
        sptwb.Spt.DataTransferLength = Kernel32.SCSI_PASS_THROUGH_BUFFER_SIZE;
        sptwb.Spt.TimeOutValue = 2;
        sptwb.Spt.DataBufferOffset = Marshal.OffsetOf<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>(nameof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS.DataBuf));
        sptwb.Spt.SenseInfoOffset = (uint) Marshal.OffsetOf<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>(nameof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS.SenseBuf));
        sptwb.Spt.CdbLength = 16;
        sptwb.Spt.Cdb[0] = 0xA2; // SECURITY PROTOCOL IN
        sptwb.Spt.Cdb[1] = 0xFE; // Samsung Protocol
        sptwb.Spt.Cdb[3] = 5; // Identify
        sptwb.Spt.Cdb[8] = 2; // Transfer Length (high)
        sptwb.Spt.Cdb[9] = 0; // Transfer Length (low)
        sptwb.Spt.DataIn = (byte) Kernel32.SCSI_IOCTL_DATA.SCSI_IOCTL_DATA_IN;

        length = Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>();
        buffer = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr(sptwb, buffer, false);

        validTransfer = Kernel32.DeviceIoControl(hDevice, Kernel32.IOCTL.IOCTL_SCSI_PASS_THROUGH, buffer, length, buffer, length, out _, IntPtr.Zero);
        if (validTransfer) {
          var offset = Marshal.OffsetOf<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>(nameof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS.DataBuf));
          IntPtr newPtr = IntPtr.Add(buffer, offset.ToInt32());
          var item = Marshal.PtrToStructure<Kernel32.NVME_IDENTIFY_CONTROLLER_DATA>(newPtr);
          data = item;
          Marshal.FreeHGlobal(buffer);
          result = true;
        } else {
          Marshal.FreeHGlobal(buffer);
        }
      }

      return result;
    }

    public bool HealthInfoLog(SafeHandle hDevice, out Kernel32.NVME_HEALTH_INFO_LOG data) {
      data = Kernel32.CreateStruct<Kernel32.NVME_HEALTH_INFO_LOG>();
      if (hDevice == null || hDevice.IsInvalid)
        return false;


      bool result = false;
      IntPtr buffer;
      Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS sptwb = Kernel32.CreateStruct<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>();

      sptwb.Spt.Length = (ushort) Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH>();
      sptwb.Spt.PathId = 0;
      sptwb.Spt.TargetId = 0;
      sptwb.Spt.Lun = 0;
      sptwb.Spt.SenseInfoLength = 24;
      sptwb.Spt.DataTransferLength = Kernel32.SCSI_PASS_THROUGH_BUFFER_SIZE;
      sptwb.Spt.TimeOutValue = 2;
      sptwb.Spt.DataBufferOffset = Marshal.OffsetOf<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>(nameof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS.DataBuf));
      sptwb.Spt.SenseInfoOffset = (uint) Marshal.OffsetOf<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>(nameof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS.SenseBuf));
      sptwb.Spt.CdbLength = 16;
      sptwb.Spt.Cdb[0] = 0xB5; // SECURITY PROTOCOL IN
      sptwb.Spt.Cdb[1] = 0xFE; // Samsung Protocol
      sptwb.Spt.Cdb[3] = 6; // Log Data
      sptwb.Spt.Cdb[8] = 0; // Transfer Length
      sptwb.Spt.Cdb[9] = 0x40; // Transfer Length
      sptwb.Spt.DataIn = (byte) Kernel32.SCSI_IOCTL_DATA.SCSI_IOCTL_DATA_OUT;
      sptwb.DataBuf[0] = 2;
      sptwb.DataBuf[4] = 0xff;
      sptwb.DataBuf[5] = 0xff;
      sptwb.DataBuf[6] = 0xff;
      sptwb.DataBuf[7] = 0xff;

      var length = Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>();
      buffer = Marshal.AllocHGlobal(length);
      Marshal.StructureToPtr(sptwb, buffer, false);
      var validTransfer = Kernel32.DeviceIoControl(hDevice, Kernel32.IOCTL.IOCTL_SCSI_PASS_THROUGH, buffer, length, buffer, length, out _, IntPtr.Zero);
      Marshal.FreeHGlobal(buffer);

      if (validTransfer) {
        //read data from samsung SSD
        sptwb = Kernel32.CreateStruct<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>();
        sptwb.Spt.Length = (ushort) Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH>();
        sptwb.Spt.PathId = 0;
        sptwb.Spt.TargetId = 0;
        sptwb.Spt.Lun = 0;
        sptwb.Spt.SenseInfoLength = 24;
        sptwb.Spt.DataTransferLength = Kernel32.SCSI_PASS_THROUGH_BUFFER_SIZE;
        sptwb.Spt.TimeOutValue = 2;
        sptwb.Spt.DataBufferOffset = Marshal.OffsetOf<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>(nameof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS.DataBuf));
        sptwb.Spt.SenseInfoOffset = (uint) Marshal.OffsetOf<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>(nameof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS.SenseBuf));
        sptwb.Spt.CdbLength = 16;
        sptwb.Spt.Cdb[0] = 0xA2; // SECURITY PROTOCOL IN
        sptwb.Spt.Cdb[1] = 0xFE; // Samsung Protocol
        sptwb.Spt.Cdb[3] = 6; // Log Data
        sptwb.Spt.Cdb[8] = 2; // Transfer Length (high)
        sptwb.Spt.Cdb[9] = 0; // Transfer Length (low)
        sptwb.Spt.DataIn = (byte) Kernel32.SCSI_IOCTL_DATA.SCSI_IOCTL_DATA_IN;

        length = Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>();
        buffer = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr(sptwb, buffer, false);

        validTransfer = Kernel32.DeviceIoControl(hDevice, Kernel32.IOCTL.IOCTL_SCSI_PASS_THROUGH, buffer, length, buffer, length, out _, IntPtr.Zero);
        if (validTransfer) {
          var offset = Marshal.OffsetOf<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>(nameof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS.DataBuf));
          IntPtr newPtr = IntPtr.Add(buffer, offset.ToInt32());
          var item = Marshal.PtrToStructure<Kernel32.NVME_HEALTH_INFO_LOG>(newPtr);
          data = item;
          Marshal.FreeHGlobal(buffer);
          result = true;
        } else {
          Marshal.FreeHGlobal(buffer);
        }
      }

      return result;
    }

    public static SafeHandle IdentifyDevice(StorageInfo storageInfo) {
      var handle = Kernel32.OpenDevice(storageInfo.DeviceId);
      if (handle == null || handle.IsInvalid)
        return null;


      IntPtr buffer;
      Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS sptwb = Kernel32.CreateStruct<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>();

      sptwb.Spt.Length = (ushort) Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH>();
      sptwb.Spt.PathId = 0;
      sptwb.Spt.TargetId = 0;
      sptwb.Spt.Lun = 0;
      sptwb.Spt.SenseInfoLength = 24;
      sptwb.Spt.DataTransferLength = Kernel32.SCSI_PASS_THROUGH_BUFFER_SIZE;
      sptwb.Spt.TimeOutValue = 2;
      sptwb.Spt.DataBufferOffset = Marshal.OffsetOf(typeof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS), nameof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS.DataBuf));
      sptwb.Spt.SenseInfoOffset = (uint) Marshal.OffsetOf(typeof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS), nameof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS.SenseBuf));
      sptwb.Spt.CdbLength = 16;
      sptwb.Spt.Cdb[0] = 0xB5; // SECURITY PROTOCOL IN
      sptwb.Spt.Cdb[1] = 0xFE; // Samsung Protocol
      sptwb.Spt.Cdb[3] = 5; // Identify
      sptwb.Spt.Cdb[8] = 0; // Transfer Length
      sptwb.Spt.Cdb[9] = 0x40;
      sptwb.Spt.DataIn = (byte) Kernel32.SCSI_IOCTL_DATA.SCSI_IOCTL_DATA_OUT;
      sptwb.DataBuf[0] = 1;

      var length = Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>();
      buffer = Marshal.AllocHGlobal(length);
      Marshal.StructureToPtr(sptwb, buffer, false);
      var validTransfer = Kernel32.DeviceIoControl(handle, Kernel32.IOCTL.IOCTL_SCSI_PASS_THROUGH, buffer, length, buffer, length, out _, IntPtr.Zero);
      Marshal.FreeHGlobal(buffer);

      if (validTransfer) {
        //read data from samsung SSD
        sptwb = Kernel32.CreateStruct<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>();
        sptwb.Spt.Length = (ushort) Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH>();
        sptwb.Spt.PathId = 0;
        sptwb.Spt.TargetId = 0;
        sptwb.Spt.Lun = 0;
        sptwb.Spt.SenseInfoLength = 24;
        sptwb.Spt.DataTransferLength = Kernel32.SCSI_PASS_THROUGH_BUFFER_SIZE;
        sptwb.Spt.TimeOutValue = 2;
        sptwb.Spt.DataBufferOffset = Marshal.OffsetOf(typeof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS), nameof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS.DataBuf));
        sptwb.Spt.SenseInfoOffset = (uint) Marshal.OffsetOf(typeof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS), nameof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS.SenseBuf));
        sptwb.Spt.CdbLength = 16;
        sptwb.Spt.Cdb[0] = 0xA2; // SECURITY PROTOCOL IN
        sptwb.Spt.Cdb[1] = 0xFE; // Samsung Protocol
        sptwb.Spt.Cdb[3] = 5; // Identify
        sptwb.Spt.Cdb[8] = 2; // Transfer Length
        sptwb.Spt.Cdb[9] = 0;
        sptwb.Spt.DataIn = (byte) Kernel32.SCSI_IOCTL_DATA.SCSI_IOCTL_DATA_IN;

        length = Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>();
        buffer = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr(sptwb, buffer, false);

        validTransfer = Kernel32.DeviceIoControl(handle, Kernel32.IOCTL.IOCTL_SCSI_PASS_THROUGH, buffer, length, buffer, length, out _, IntPtr.Zero);
        if (validTransfer) {
          Marshal.PtrToStructure<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>(buffer);
          Marshal.FreeHGlobal(buffer);
        } else {
          Marshal.FreeHGlobal(buffer);
          handle.Close();
          handle = null;
        }
      }

      return handle;
    }
  }
}