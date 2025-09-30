// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Runtime.InteropServices;
using Windows.Win32.Storage.IscsiDisc;
using Windows.Win32.Storage.Nvme;
using Windows.Win32.System.Ioctl;
using LibreHardwareMonitor.Interop;
using Microsoft.Win32.SafeHandles;

namespace LibreHardwareMonitor.Hardware.Storage;

internal class NVMeIntelRst : INVMeDrive
{
    //intel RST (raid) nvme access

    public SafeHandle Identify(StorageInfo storageInfo)
    {
        return NVMeWindows.IdentifyDevice(storageInfo);
    }

    public unsafe bool IdentifyController(SafeHandle hDevice, out NVME_IDENTIFY_CONTROLLER_DATA data)
    {
        data = new NVME_IDENTIFY_CONTROLLER_DATA();
        if (hDevice?.IsInvalid != false)
            return false;

        bool result = false;

        Kernel32.NVME_PASS_THROUGH_IOCTL passThrough = new();
        passThrough.SrbIoCtrl.HeaderLength = (uint)sizeof(SRB_IO_CONTROL);

        ReadOnlySpan<byte> signature = "IntelNvm"u8;
        for (int i = 0; i < signature.Length; i++)
            passThrough.SrbIoCtrl.Signature[i] = signature[i];

        passThrough.SrbIoCtrl.Timeout = 10;
        passThrough.SrbIoCtrl.ControlCode = Kernel32.NVME_PASS_THROUGH_SRB_IO_CODE;
        passThrough.SrbIoCtrl.ReturnCode = 0;
        passThrough.SrbIoCtrl.Length = (uint)Marshal.SizeOf<Kernel32.NVME_PASS_THROUGH_IOCTL>() - (uint)Marshal.SizeOf<SRB_IO_CONTROL>();
        passThrough.NVMeCmd[0] = 6; //identify
        passThrough.NVMeCmd[10] = 1; //return to host
        passThrough.Direction = Kernel32.NVME_DIRECTION.NVME_FROM_DEV_TO_HOST;
        passThrough.QueueId = 0;
        passThrough.DataBufferLen = Kernel32.IOCTL_BUFFER_SIZE;
        passThrough.MetaDataLen = 0;
        passThrough.ReturnBufferLen = (uint)Marshal.SizeOf<Kernel32.NVME_PASS_THROUGH_IOCTL>();

        int length = Marshal.SizeOf<Kernel32.NVME_PASS_THROUGH_IOCTL>();
        IntPtr buffer = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr(passThrough, buffer, false);

        bool validTransfer = Kernel32.DeviceIoControl(hDevice, Kernel32.IOCTL.IOCTL_SCSI_MINIPORT, buffer, length, buffer, length, out _, IntPtr.Zero);
        if (validTransfer)
        {
            IntPtr offset = Marshal.OffsetOf<Kernel32.NVME_PASS_THROUGH_IOCTL>(nameof(Kernel32.NVME_PASS_THROUGH_IOCTL.DataBuffer));
            IntPtr newPtr = IntPtr.Add(buffer, offset.ToInt32());
            int finalSize = Marshal.SizeOf<NVME_IDENTIFY_CONTROLLER_DATA>();
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<NVME_IDENTIFY_CONTROLLER_DATA>());
            Kernel32.RtlZeroMemory(ptr, finalSize);
            int len = Math.Min(finalSize, Kernel32.IOCTL_BUFFER_SIZE);
            Kernel32.RtlCopyMemory(ptr, newPtr, (uint)len);
            Marshal.FreeHGlobal(buffer);

            data = Marshal.PtrToStructure<NVME_IDENTIFY_CONTROLLER_DATA>(ptr);
            Marshal.FreeHGlobal(ptr);
            result = true;
        }
        else
        {
            Marshal.FreeHGlobal(buffer);
        }

        return result;
    }

    public unsafe bool HealthInfoLog(SafeHandle hDevice, out NVME_HEALTH_INFO_LOG data)
    {
        data = new NVME_HEALTH_INFO_LOG();
        if (hDevice?.IsInvalid != false)
            return false;

        bool result = false;

        Kernel32.NVME_PASS_THROUGH_IOCTL passThrough = new();
        passThrough.SrbIoCtrl.HeaderLength = (uint)sizeof(SRB_IO_CONTROL);

        ReadOnlySpan<byte> signature = "IntelNvm"u8;
        for (int i = 0; i < signature.Length; i++)
            passThrough.SrbIoCtrl.Signature[i] = signature[i];

        passThrough.SrbIoCtrl.Timeout = 10;
        passThrough.SrbIoCtrl.ControlCode = Kernel32.NVME_PASS_THROUGH_SRB_IO_CODE;
        passThrough.SrbIoCtrl.ReturnCode = 0;
        passThrough.SrbIoCtrl.Length = (uint)Marshal.SizeOf<Kernel32.NVME_PASS_THROUGH_IOCTL>() - (uint)Marshal.SizeOf<SRB_IO_CONTROL>();
        passThrough.NVMeCmd[0] = (uint)STORAGE_PROTOCOL_NVME_DATA_TYPE.NVMeDataTypeLogPage; // GetLogPage
        passThrough.NVMeCmd[1] = 0xFFFFFFFF; // address
        passThrough.NVMeCmd[10] = 0x007f0002; // uint cdw10 = 0x000000002 | (((size / 4) - 1) << 16);
        passThrough.Direction = Kernel32.NVME_DIRECTION.NVME_FROM_DEV_TO_HOST;
        passThrough.QueueId = 0;
        passThrough.DataBufferLen = Kernel32.IOCTL_BUFFER_SIZE;
        passThrough.MetaDataLen = 0;
        passThrough.ReturnBufferLen = (uint)Marshal.SizeOf<Kernel32.NVME_PASS_THROUGH_IOCTL>();

        int length = Marshal.SizeOf<Kernel32.NVME_PASS_THROUGH_IOCTL>();
        IntPtr buffer = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr(passThrough, buffer, false);

        bool validTransfer = Kernel32.DeviceIoControl(hDevice, Kernel32.IOCTL.IOCTL_SCSI_MINIPORT, buffer, length, buffer, length, out _, IntPtr.Zero);
        if (validTransfer)
        {
            IntPtr offset = Marshal.OffsetOf<Kernel32.NVME_PASS_THROUGH_IOCTL>(nameof(Kernel32.NVME_PASS_THROUGH_IOCTL.DataBuffer));
            IntPtr newPtr = IntPtr.Add(buffer, offset.ToInt32());
            data = Marshal.PtrToStructure<NVME_HEALTH_INFO_LOG>(newPtr);
            Marshal.FreeHGlobal(buffer);
            result = true;
        }
        else
        {
            Marshal.FreeHGlobal(buffer);
        }

        return result;
    }

    public static unsafe SafeHandle IdentifyDevice(StorageInfo storageInfo)
    {
        SafeFileHandle handle = Kernel32.OpenDevice(storageInfo.Scsi);
        if (handle?.IsInvalid != false)
            return null;

        Kernel32.NVME_PASS_THROUGH_IOCTL passThrough = new();
        passThrough.SrbIoCtrl.HeaderLength = (uint)sizeof(SRB_IO_CONTROL);

        ReadOnlySpan<byte> signature = "IntelNvm"u8;
        for (int i = 0; i < signature.Length; i++)
            passThrough.SrbIoCtrl.Signature[i] = signature[i];

        passThrough.SrbIoCtrl.Timeout = 10;
        passThrough.SrbIoCtrl.ControlCode = Kernel32.NVME_PASS_THROUGH_SRB_IO_CODE;
        passThrough.SrbIoCtrl.ReturnCode = 0;
        passThrough.SrbIoCtrl.Length = (uint)Marshal.SizeOf<Kernel32.NVME_PASS_THROUGH_IOCTL>() - (uint)Marshal.SizeOf<SRB_IO_CONTROL>();
        passThrough.NVMeCmd[0] = 6; //identify
        passThrough.NVMeCmd[10] = 1; //return to host
        passThrough.Direction = Kernel32.NVME_DIRECTION.NVME_FROM_DEV_TO_HOST;
        passThrough.QueueId = 0;
        passThrough.DataBufferLen = Kernel32.IOCTL_BUFFER_SIZE;
        passThrough.MetaDataLen = 0;
        passThrough.ReturnBufferLen = (uint)Marshal.SizeOf<Kernel32.NVME_PASS_THROUGH_IOCTL>();

        int length = Marshal.SizeOf<Kernel32.NVME_PASS_THROUGH_IOCTL>();
        IntPtr buffer = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr(passThrough, buffer, false);

        bool validTransfer = Kernel32.DeviceIoControl(handle, Kernel32.IOCTL.IOCTL_SCSI_MINIPORT, buffer, length, buffer, length, out _, IntPtr.Zero);
        Marshal.FreeHGlobal(buffer);

        if (validTransfer)
        { }
        else
        {
            handle.Close();
            handle = null;
        }

        return handle;
    }
}
