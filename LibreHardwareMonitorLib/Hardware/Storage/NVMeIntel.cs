// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Runtime.InteropServices;
using System.Text;
using LibreHardwareMonitor.Interop;
using Microsoft.Win32.SafeHandles;

namespace LibreHardwareMonitor.Hardware.Storage;

internal class NVMeIntel : INVMeDrive
{
    //intel nvme access

    public SafeHandle Identify(StorageInfo storageInfo)
    {
        return NVMeWindows.IdentifyDevice(storageInfo);
    }

    public bool IdentifyController(SafeHandle hDevice, out Kernel32.NVME_IDENTIFY_CONTROLLER_DATA data)
    {
        data = Kernel32.CreateStruct<Kernel32.NVME_IDENTIFY_CONTROLLER_DATA>();
        if (hDevice?.IsInvalid != false)
            return false;

        bool result = false;

        Kernel32.NVME_PASS_THROUGH_IOCTL passThrough = Kernel32.CreateStruct<Kernel32.NVME_PASS_THROUGH_IOCTL>();
        passThrough.srb.HeaderLenght = (uint)Marshal.SizeOf<Kernel32.SRB_IO_CONTROL>();
        passThrough.srb.Signature = Encoding.ASCII.GetBytes(Kernel32.IntelNVMeMiniPortSignature1);
        passThrough.srb.Timeout = 10;
        passThrough.srb.ControlCode = Kernel32.NVME_PASS_THROUGH_SRB_IO_CODE;
        passThrough.srb.ReturnCode = 0;
        passThrough.srb.Length = (uint)Marshal.SizeOf<Kernel32.NVME_PASS_THROUGH_IOCTL>() - (uint)Marshal.SizeOf<Kernel32.SRB_IO_CONTROL>();
        passThrough.NVMeCmd = new uint[16];
        passThrough.NVMeCmd[0] = 6; //identify
        passThrough.NVMeCmd[10] = 1; //return to host
        passThrough.Direction = Kernel32.NVME_DIRECTION.NVME_FROM_DEV_TO_HOST;
        passThrough.QueueId = 0;
        passThrough.DataBufferLen = (uint)passThrough.DataBuffer.Length;
        passThrough.MetaDataLen = 0;
        passThrough.ReturnBufferLen = (uint)Marshal.SizeOf<Kernel32.NVME_PASS_THROUGH_IOCTL>();

        int length = Marshal.SizeOf<Kernel32.NVME_PASS_THROUGH_IOCTL>();
        IntPtr buffer = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr(passThrough, buffer, false);

        bool validTransfer = Kernel32.DeviceIoControl(hDevice, Kernel32.IOCTL.IOCTL_SCSI_MINIPORT, buffer, length, buffer, length, out _, IntPtr.Zero);
        if (validTransfer)
        {
            IntPtr offset = Marshal.OffsetOf<Kernel32.NVME_PASS_THROUGH_IOCTL>(nameof(Kernel32.NVME_PASS_THROUGH_IOCTL.DataBuffer));
            var newPtr = IntPtr.Add(buffer, offset.ToInt32());
            int finalSize = Marshal.SizeOf<Kernel32.NVME_IDENTIFY_CONTROLLER_DATA>();
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<Kernel32.NVME_IDENTIFY_CONTROLLER_DATA>());
            Kernel32.RtlZeroMemory(ptr, finalSize);
            int len = Math.Min(finalSize, passThrough.DataBuffer.Length);
            Kernel32.RtlCopyMemory(ptr, newPtr, (uint)len);
            Marshal.FreeHGlobal(buffer);

            data = Marshal.PtrToStructure<Kernel32.NVME_IDENTIFY_CONTROLLER_DATA>(ptr);
            Marshal.FreeHGlobal(ptr);
            result = true;
        }
        else
        {
            Marshal.FreeHGlobal(buffer);
        }

        return result;
    }

    public bool HealthInfoLog(SafeHandle hDevice, out Kernel32.NVME_HEALTH_INFO_LOG data)
    {
        data = Kernel32.CreateStruct<Kernel32.NVME_HEALTH_INFO_LOG>();
        if (hDevice?.IsInvalid != false)
            return false;

        bool result = false;

        Kernel32.NVME_PASS_THROUGH_IOCTL passThrough = Kernel32.CreateStruct<Kernel32.NVME_PASS_THROUGH_IOCTL>();
        passThrough.srb.HeaderLenght = (uint)Marshal.SizeOf<Kernel32.SRB_IO_CONTROL>();
        passThrough.srb.Signature = Encoding.ASCII.GetBytes(Kernel32.IntelNVMeMiniPortSignature1);
        passThrough.srb.Timeout = 10;
        passThrough.srb.ControlCode = Kernel32.NVME_PASS_THROUGH_SRB_IO_CODE;
        passThrough.srb.ReturnCode = 0;
        passThrough.srb.Length = (uint)Marshal.SizeOf<Kernel32.NVME_PASS_THROUGH_IOCTL>() - (uint)Marshal.SizeOf<Kernel32.SRB_IO_CONTROL>();
        passThrough.NVMeCmd[0] = (uint)Kernel32.STORAGE_PROTOCOL_NVME_DATA_TYPE.NVMeDataTypeLogPage; // GetLogPage
        passThrough.NVMeCmd[1] = 0xFFFFFFFF; // address
        passThrough.NVMeCmd[10] = 0x007f0002; // uint cdw10 = 0x000000002 | (((size / 4) - 1) << 16);
        passThrough.Direction = Kernel32.NVME_DIRECTION.NVME_FROM_DEV_TO_HOST;
        passThrough.QueueId = 0;
        passThrough.DataBufferLen = (uint)passThrough.DataBuffer.Length;
        passThrough.MetaDataLen = 0;
        passThrough.ReturnBufferLen = (uint)Marshal.SizeOf<Kernel32.NVME_PASS_THROUGH_IOCTL>();

        int length = Marshal.SizeOf<Kernel32.NVME_PASS_THROUGH_IOCTL>();
        IntPtr buffer = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr(passThrough, buffer, false);

        bool validTransfer = Kernel32.DeviceIoControl(hDevice, Kernel32.IOCTL.IOCTL_SCSI_MINIPORT, buffer, length, buffer, length, out _, IntPtr.Zero);
        if (validTransfer)
        {
            IntPtr offset = Marshal.OffsetOf<Kernel32.NVME_PASS_THROUGH_IOCTL>(nameof(Kernel32.NVME_PASS_THROUGH_IOCTL.DataBuffer));
            var newPtr = IntPtr.Add(buffer, offset.ToInt32());
            data = Marshal.PtrToStructure<Kernel32.NVME_HEALTH_INFO_LOG>(newPtr);
            Marshal.FreeHGlobal(buffer);
            result = true;
        }
        else
        {
            Marshal.FreeHGlobal(buffer);
        }
        return result;
    }

    public static SafeHandle IdentifyDevice(StorageInfo storageInfo)
    {
        SafeFileHandle handle = Kernel32.OpenDevice(storageInfo.Scsi);
        if (handle?.IsInvalid != false)
            return null;

        Kernel32.NVME_PASS_THROUGH_IOCTL passThrough = Kernel32.CreateStruct<Kernel32.NVME_PASS_THROUGH_IOCTL>();
        passThrough.srb.HeaderLenght = (uint)Marshal.SizeOf<Kernel32.SRB_IO_CONTROL>();
        passThrough.srb.Signature = Encoding.ASCII.GetBytes(Kernel32.IntelNVMeMiniPortSignature1);
        passThrough.srb.Timeout = 10;
        passThrough.srb.ControlCode = Kernel32.NVME_PASS_THROUGH_SRB_IO_CODE;
        passThrough.srb.ReturnCode = 0;
        passThrough.srb.Length = (uint)Marshal.SizeOf<Kernel32.NVME_PASS_THROUGH_IOCTL>() - (uint)Marshal.SizeOf<Kernel32.SRB_IO_CONTROL>();
        passThrough.NVMeCmd = new uint[16];
        passThrough.NVMeCmd[0] = 6; //identify
        passThrough.NVMeCmd[10] = 1; //return to host
        passThrough.Direction = Kernel32.NVME_DIRECTION.NVME_FROM_DEV_TO_HOST;
        passThrough.QueueId = 0;
        passThrough.DataBufferLen = (uint)passThrough.DataBuffer.Length;
        passThrough.MetaDataLen = 0;
        passThrough.ReturnBufferLen = (uint)Marshal.SizeOf<Kernel32.NVME_PASS_THROUGH_IOCTL>();

        int length = Marshal.SizeOf<Kernel32.NVME_PASS_THROUGH_IOCTL>();
        IntPtr buffer = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr(passThrough, buffer, false);

        bool validTransfer = Kernel32.DeviceIoControl(handle, Kernel32.IOCTL.IOCTL_SCSI_MINIPORT, buffer, length, buffer, length, out _, IntPtr.Zero);
        Marshal.FreeHGlobal(buffer);

        if (validTransfer) { }
        else
        {
            handle.Close();
            handle = null;
        }
        return handle;
    }
}
