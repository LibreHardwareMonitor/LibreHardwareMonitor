// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.Storage.IscsiDisc;
using Windows.Win32.Storage.Nvme;
using Windows.Win32.System.Ioctl;
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

    public unsafe bool IdentifyController(SafeHandle hDevice, out NVME_IDENTIFY_CONTROLLER_DATA data)
    {
        data = new NVME_IDENTIFY_CONTROLLER_DATA();
        if (hDevice?.IsInvalid != false)
            return false;

        bool result = false;

        AtaSmart.NVME_PASS_THROUGH_IOCTL passThrough = new();
        passThrough.SrbIoCtrl.HeaderLength = (uint)sizeof(SRB_IO_CONTROL);

        ReadOnlySpan<byte> signature = "NvmeMini"u8;
        for (int i = 0; i < signature.Length; i++)
            passThrough.SrbIoCtrl.Signature[i] = signature[i];

        passThrough.SrbIoCtrl.Timeout = 10;
        passThrough.SrbIoCtrl.ControlCode = AtaSmart.NVME_PASS_THROUGH_SRB_IO_CODE;
        passThrough.SrbIoCtrl.ReturnCode = 0;
        passThrough.SrbIoCtrl.Length = (uint)sizeof(AtaSmart.NVME_PASS_THROUGH_IOCTL) - (uint)sizeof(SRB_IO_CONTROL);
        passThrough.NVMeCmd[0] = 6; //identify
        passThrough.NVMeCmd[10] = 1; //return to host
        passThrough.Direction = AtaSmart.NVME_DATA_IN;
        passThrough.QueueId = 0;
        passThrough.DataBufferLen = AtaSmart.IOCTL_BUFFER_SIZE;
        passThrough.MetaDataLen = 0;
        passThrough.ReturnBufferLen = (uint)sizeof(AtaSmart.NVME_PASS_THROUGH_IOCTL);

        int length = sizeof(AtaSmart.NVME_PASS_THROUGH_IOCTL);
        IntPtr buffer = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr(passThrough, buffer, false);

        bool validTransfer = PInvoke.DeviceIoControl(hDevice, PInvoke.IOCTL_SCSI_MINIPORT, (void*)buffer, (uint)length, (void*)buffer, (uint)length, null, null);
        if (validTransfer)
        {
            IntPtr offset = Marshal.OffsetOf<AtaSmart.NVME_PASS_THROUGH_IOCTL>(nameof(AtaSmart.NVME_PASS_THROUGH_IOCTL.DataBuffer));
            var newPtr = IntPtr.Add(buffer, offset.ToInt32());
            data = *(NVME_IDENTIFY_CONTROLLER_DATA*)newPtr;
            Marshal.FreeHGlobal(buffer);
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

        AtaSmart.NVME_PASS_THROUGH_IOCTL passThrough = new();
        passThrough.SrbIoCtrl.HeaderLength = (uint)sizeof(SRB_IO_CONTROL);

        ReadOnlySpan<byte> signature = "NvmeMini"u8;
        for (int i = 0; i < signature.Length; i++)
            passThrough.SrbIoCtrl.Signature[i] = signature[i];

        passThrough.SrbIoCtrl.Timeout = 10;
        passThrough.SrbIoCtrl.ControlCode = AtaSmart.NVME_PASS_THROUGH_SRB_IO_CODE;
        passThrough.SrbIoCtrl.ReturnCode = 0;
        passThrough.SrbIoCtrl.Length = (uint)sizeof(AtaSmart.NVME_PASS_THROUGH_IOCTL) - (uint)sizeof(SRB_IO_CONTROL);
        passThrough.NVMeCmd[0] = (uint)STORAGE_PROTOCOL_NVME_DATA_TYPE.NVMeDataTypeLogPage; // GetLogPage
        passThrough.NVMeCmd[1] = 0xFFFFFFFF; // address
        passThrough.NVMeCmd[10] = 0x007f0002; // uint cdw10 = 0x000000002 | (((size / 4) - 1) << 16);
        passThrough.Direction = AtaSmart.NVME_DATA_IN;
        passThrough.QueueId = 0;
        passThrough.DataBufferLen = AtaSmart.IOCTL_BUFFER_SIZE;
        passThrough.MetaDataLen = 0;
        passThrough.ReturnBufferLen = (uint)sizeof(AtaSmart.NVME_PASS_THROUGH_IOCTL);

        int length = sizeof(AtaSmart.NVME_PASS_THROUGH_IOCTL);
        IntPtr buffer = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr(passThrough, buffer, false);

        bool validTransfer = PInvoke.DeviceIoControl(hDevice, PInvoke.IOCTL_SCSI_MINIPORT, (void*)buffer, (uint)length, (void*)buffer, (uint)length, null, null);
        if (validTransfer)
        {
            IntPtr offset = Marshal.OffsetOf<AtaSmart.NVME_PASS_THROUGH_IOCTL>(nameof(AtaSmart.NVME_PASS_THROUGH_IOCTL.DataBuffer));
            var newPtr = IntPtr.Add(buffer, offset.ToInt32());
            data = *(NVME_HEALTH_INFO_LOG*)newPtr;
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
        SafeFileHandle handle = PInvoke.CreateFile(storageInfo.Scsi,
                                                   (uint)FileAccess.ReadWrite,
                                                   FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE,
                                                   null,
                                                   FILE_CREATION_DISPOSITION.OPEN_EXISTING,
                                                   FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL,
                                                   null);
        if (handle?.IsInvalid != false)
            return null;

        AtaSmart.NVME_PASS_THROUGH_IOCTL passThrough = new();
        passThrough.SrbIoCtrl.HeaderLength = (uint)sizeof(SRB_IO_CONTROL);

        ReadOnlySpan<byte> signature = "NvmeMini"u8;
        for (int i = 0; i < signature.Length; i++)
            passThrough.SrbIoCtrl.Signature[i] = signature[i];

        passThrough.SrbIoCtrl.Timeout = 10;
        passThrough.SrbIoCtrl.ControlCode = AtaSmart.NVME_PASS_THROUGH_SRB_IO_CODE;
        passThrough.SrbIoCtrl.ReturnCode = 0;
        passThrough.SrbIoCtrl.Length = (uint)sizeof(AtaSmart.NVME_PASS_THROUGH_IOCTL) - (uint)sizeof(SRB_IO_CONTROL);
        passThrough.NVMeCmd[0] = 6; //identify
        passThrough.NVMeCmd[10] = 1; //return to host
        passThrough.Direction = AtaSmart.NVME_DATA_IN;
        passThrough.QueueId = 0;
        passThrough.DataBufferLen = AtaSmart.IOCTL_BUFFER_SIZE;
        passThrough.MetaDataLen = 0;
        passThrough.ReturnBufferLen = (uint)sizeof(AtaSmart.NVME_PASS_THROUGH_IOCTL);

        int length = sizeof(AtaSmart.NVME_PASS_THROUGH_IOCTL);
        IntPtr buffer = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr(passThrough, buffer, false);

        bool validTransfer = PInvoke.DeviceIoControl(handle, PInvoke.IOCTL_SCSI_MINIPORT, (void*)buffer, (uint)length, (void*)buffer, (uint)length, null, null);
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
