// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.IO;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Interop;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.Storage.IscsiDisc;
using Windows.Win32.Storage.Nvme;

namespace LibreHardwareMonitor.Hardware.Storage;

internal class NVMeSamsung : INVMeDrive
{
    //samsung nvme access
    //https://github.com/hiyohiyo/CrystalDiskInfo
    //https://github.com/hiyohiyo/CrystalDiskInfo/blob/master/AtaSmart.cpp

    public SafeHandle Identify(StorageInfo storageInfo)
    {
        return NVMeWindows.IdentifyDevice(storageInfo);
    }

    public unsafe bool IdentifyController(SafeHandle hDevice, out NVME_IDENTIFY_CONTROLLER_DATA data)
    {
        data = new NVME_IDENTIFY_CONTROLLER_DATA();
        if (hDevice?.IsInvalid != false)
            return false;

        AtaSmart.SCSI_PASS_THROUGH buffers = new();

        buffers.Spt.Length = (ushort)sizeof(SCSI_PASS_THROUGH);
        buffers.Spt.PathId = 0;
        buffers.Spt.TargetId = 0;
        buffers.Spt.Lun = 0;
        buffers.Spt.SenseInfoLength = 24;
        buffers.Spt.DataTransferLength = AtaSmart.IOCTL_BUFFER_SIZE;
        buffers.Spt.TimeOutValue = 2;
        buffers.Spt.DataBufferOffset = new UIntPtr((ulong)Marshal.OffsetOf<AtaSmart.SCSI_PASS_THROUGH>(nameof(AtaSmart.SCSI_PASS_THROUGH.DataBuf)));
        buffers.Spt.SenseInfoOffset = (uint)Marshal.OffsetOf(typeof(AtaSmart.SCSI_PASS_THROUGH), nameof(AtaSmart.SCSI_PASS_THROUGH.SenseBuf));

        buffers.Spt.CdbLength = 16;
        buffers.Spt.Cdb[0] = 0xB5; // SECURITY PROTOCOL IN
        buffers.Spt.Cdb[1] = 0xFE; // Samsung Protocol
        buffers.Spt.Cdb[3] = 5; // Identify
        buffers.Spt.Cdb[8] = 0; // Transfer Length
        buffers.Spt.Cdb[9] = 0x40; // Transfer Length
        buffers.Spt.DataIn = (byte)PInvoke.SCSI_IOCTL_DATA_OUT;
        buffers.DataBuf[0] = 1;

        int length = (int)(Marshal.OffsetOf(typeof(AtaSmart.SCSI_PASS_THROUGH), nameof(AtaSmart.SCSI_PASS_THROUGH.DataBuf)).ToInt32() + buffers.Spt.DataTransferLength);
        IntPtr buffer = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr(buffers, buffer, false);
        bool validTransfer = PInvoke.DeviceIoControl(hDevice, PInvoke.IOCTL_SCSI_PASS_THROUGH, (void*)buffer, (uint)length, (void*)buffer, (uint)length, null, null);
        Marshal.FreeHGlobal(buffer);

        bool result = false;

        if (validTransfer)
        {
            //read data from samsung SSD
            buffers = new AtaSmart.SCSI_PASS_THROUGH();
            buffers.Spt.Length = (ushort)sizeof(SCSI_PASS_THROUGH);
            buffers.Spt.PathId = 0;
            buffers.Spt.TargetId = 0;
            buffers.Spt.Lun = 0;
            buffers.Spt.SenseInfoLength = 24;
            buffers.Spt.DataTransferLength = AtaSmart.IOCTL_BUFFER_SIZE;
            buffers.Spt.TimeOutValue = 2;
            buffers.Spt.DataBufferOffset = new UIntPtr((ulong)Marshal.OffsetOf<AtaSmart.SCSI_PASS_THROUGH>(nameof(AtaSmart.SCSI_PASS_THROUGH.DataBuf)));
            buffers.Spt.SenseInfoOffset = (uint)Marshal.OffsetOf<AtaSmart.SCSI_PASS_THROUGH>(nameof(AtaSmart.SCSI_PASS_THROUGH.SenseBuf));

            buffers.Spt.CdbLength = 16;
            buffers.Spt.Cdb[0] = 0xA2; // SECURITY PROTOCOL IN
            buffers.Spt.Cdb[1] = 0xFE; // Samsung Protocol
            buffers.Spt.Cdb[3] = 5; // Identify
            buffers.Spt.Cdb[8] = 1; // Transfer Length (high)
            buffers.Spt.Cdb[9] = 0; // Transfer Length (low)
            buffers.Spt.DataIn = (byte)PInvoke.SCSI_IOCTL_DATA_IN;

            buffer = Marshal.AllocHGlobal(length);
            Marshal.StructureToPtr(buffers, buffer, false);

            validTransfer = PInvoke.DeviceIoControl(hDevice, PInvoke.IOCTL_SCSI_PASS_THROUGH, (void*)buffer, (uint)length, (void*)buffer, (uint)length, null, null);

            buffers = Marshal.PtrToStructure<AtaSmart.SCSI_PASS_THROUGH>(buffer);

            if (validTransfer && !IsAllZero(buffers.DataBuf, AtaSmart.IOCTL_BUFFER_SIZE))
            {
                IntPtr offset = Marshal.OffsetOf<AtaSmart.SCSI_PASS_THROUGH>(nameof(AtaSmart.SCSI_PASS_THROUGH.DataBuf));
                IntPtr newPtr = IntPtr.Add(buffer, offset.ToInt32());
                data = Marshal.PtrToStructure<NVME_IDENTIFY_CONTROLLER_DATA>(newPtr);
                result = true;

                Marshal.FreeHGlobal(buffer);
            }
        }

        return result;
    }

    public unsafe bool HealthInfoLog(SafeHandle hDevice, out NVME_HEALTH_INFO_LOG data)
    {
        data = new NVME_HEALTH_INFO_LOG();
        if (hDevice?.IsInvalid != false)
            return false;

        bool result = false;
        AtaSmart.SCSI_PASS_THROUGH buffers = new();

        buffers.Spt.Length = (ushort)sizeof(SCSI_PASS_THROUGH);
        buffers.Spt.PathId = 0;
        buffers.Spt.TargetId = 0;
        buffers.Spt.Lun = 0;
        buffers.Spt.SenseInfoLength = 24;
        buffers.Spt.DataTransferLength = AtaSmart.IOCTL_BUFFER_SIZE;
        buffers.Spt.TimeOutValue = 2;
        buffers.Spt.DataBufferOffset = new UIntPtr((ulong)Marshal.OffsetOf<AtaSmart.SCSI_PASS_THROUGH>(nameof(AtaSmart.SCSI_PASS_THROUGH.DataBuf)));
        buffers.Spt.SenseInfoOffset = (uint)Marshal.OffsetOf<AtaSmart.SCSI_PASS_THROUGH>(nameof(AtaSmart.SCSI_PASS_THROUGH.SenseBuf));
        buffers.Spt.CdbLength = 16;
        buffers.Spt.Cdb[0] = 0xB5; // SECURITY PROTOCOL IN
        buffers.Spt.Cdb[1] = 0xFE; // Samsung Protocol
        buffers.Spt.Cdb[3] = 6; // Log Data
        buffers.Spt.Cdb[8] = 0; // Transfer Length
        buffers.Spt.Cdb[9] = 0x40; // Transfer Length
        buffers.Spt.DataIn = (byte)PInvoke.SCSI_IOCTL_DATA_OUT;
        buffers.DataBuf[0] = 2;
        buffers.DataBuf[4] = 0xff;
        buffers.DataBuf[5] = 0xff;
        buffers.DataBuf[6] = 0xff;
        buffers.DataBuf[7] = 0xff;

        int length = Marshal.SizeOf<AtaSmart.SCSI_PASS_THROUGH>();
        IntPtr buffer = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr(buffers, buffer, false);
        bool validTransfer = PInvoke.DeviceIoControl(hDevice, PInvoke.IOCTL_SCSI_PASS_THROUGH, (void*)buffer, (uint)length, (void*)buffer, (uint)length, null, null);
        Marshal.FreeHGlobal(buffer);

        if (validTransfer)
        {
            //read data from samsung SSD
            buffers = new AtaSmart.SCSI_PASS_THROUGH();
            buffers.Spt.Length = (ushort)sizeof(SCSI_PASS_THROUGH);
            buffers.Spt.PathId = 0;
            buffers.Spt.TargetId = 0;
            buffers.Spt.Lun = 0;
            buffers.Spt.SenseInfoLength = 24;
            buffers.Spt.DataTransferLength = AtaSmart.IOCTL_BUFFER_SIZE;
            buffers.Spt.TimeOutValue = 2;
            buffers.Spt.DataBufferOffset = new UIntPtr((ulong)Marshal.OffsetOf<AtaSmart.SCSI_PASS_THROUGH>(nameof(AtaSmart.SCSI_PASS_THROUGH.DataBuf)));
            buffers.Spt.SenseInfoOffset = (uint)Marshal.OffsetOf<AtaSmart.SCSI_PASS_THROUGH>(nameof(AtaSmart.SCSI_PASS_THROUGH.SenseBuf));
            buffers.Spt.CdbLength = 16;
            buffers.Spt.Cdb[0] = 0xA2; // SECURITY PROTOCOL IN
            buffers.Spt.Cdb[1] = 0xFE; // Samsung Protocol
            buffers.Spt.Cdb[3] = 6; // Log Data
            buffers.Spt.Cdb[8] = 2; // Transfer Length (high)
            buffers.Spt.Cdb[9] = 0; // Transfer Length (low)
            buffers.Spt.DataIn = (byte)PInvoke.SCSI_IOCTL_DATA_IN;

            length = Marshal.SizeOf<AtaSmart.SCSI_PASS_THROUGH>();
            buffer = Marshal.AllocHGlobal(length);
            Marshal.StructureToPtr(buffers, buffer, false);

            validTransfer = PInvoke.DeviceIoControl(hDevice, PInvoke.IOCTL_SCSI_PASS_THROUGH, (void*)buffer, (uint)length, (void*)buffer, (uint)length, null, null);
            if (validTransfer)
            {
                IntPtr offset = Marshal.OffsetOf<AtaSmart.SCSI_PASS_THROUGH>(nameof(AtaSmart.SCSI_PASS_THROUGH.DataBuf));
                IntPtr newPtr = IntPtr.Add(buffer, offset.ToInt32());
                data = Marshal.PtrToStructure<NVME_HEALTH_INFO_LOG>(newPtr);
                Marshal.FreeHGlobal(buffer);
                result = true;
            }
            else
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        return result;
    }

    public static unsafe SafeHandle IdentifyDevice(StorageInfo storageInfo)
    {
        SafeFileHandle handle = PInvoke.CreateFile(storageInfo.DeviceId,
                                                   (uint)FileAccess.ReadWrite,
                                                   FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE,
                                                   null,
                                                   FILE_CREATION_DISPOSITION.OPEN_EXISTING,
                                                   FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL,
                                                   null);
        if (handle?.IsInvalid != false)
            return null;

        AtaSmart.SCSI_PASS_THROUGH buffers = new();

        buffers.Spt.Length = (ushort)sizeof(SCSI_PASS_THROUGH);
        buffers.Spt.PathId = 0;
        buffers.Spt.TargetId = 0;
        buffers.Spt.Lun = 0;
        buffers.Spt.SenseInfoLength = 24;
        buffers.Spt.DataTransferLength = AtaSmart.IOCTL_BUFFER_SIZE;
        buffers.Spt.TimeOutValue = 2;
        buffers.Spt.DataBufferOffset = new UIntPtr((ulong)Marshal.OffsetOf<AtaSmart.SCSI_PASS_THROUGH>(nameof(AtaSmart.SCSI_PASS_THROUGH.DataBuf)));
        buffers.Spt.SenseInfoOffset = (uint)Marshal.OffsetOf(typeof(AtaSmart.SCSI_PASS_THROUGH), nameof(AtaSmart.SCSI_PASS_THROUGH.SenseBuf));
        buffers.Spt.CdbLength = 16;
        buffers.Spt.Cdb[0] = 0xB5; // SECURITY PROTOCOL IN
        buffers.Spt.Cdb[1] = 0xFE; // Samsung Protocol
        buffers.Spt.Cdb[3] = 5; // Identify
        buffers.Spt.Cdb[8] = 0; // Transfer Length
        buffers.Spt.Cdb[9] = 0x40;
        buffers.Spt.DataIn = (byte)PInvoke.SCSI_IOCTL_DATA_OUT;
        buffers.DataBuf[0] = 1;

        int length = Marshal.SizeOf<AtaSmart.SCSI_PASS_THROUGH>();
        IntPtr buffer = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr(buffers, buffer, false);
        bool validTransfer = PInvoke.DeviceIoControl(handle, PInvoke.IOCTL_SCSI_PASS_THROUGH, (void*)buffer, (uint)length, (void*)buffer, (uint)length, null, null);
        Marshal.FreeHGlobal(buffer);

        if (validTransfer)
        {
            //read data from samsung SSD
            buffers = new AtaSmart.SCSI_PASS_THROUGH();
            buffers.Spt.Length = (ushort)sizeof(SCSI_PASS_THROUGH);
            buffers.Spt.PathId = 0;
            buffers.Spt.TargetId = 0;
            buffers.Spt.Lun = 0;
            buffers.Spt.SenseInfoLength = 24;
            buffers.Spt.DataTransferLength = AtaSmart.IOCTL_BUFFER_SIZE;
            buffers.Spt.TimeOutValue = 2;
            buffers.Spt.DataBufferOffset = new UIntPtr((ulong)Marshal.OffsetOf<AtaSmart.SCSI_PASS_THROUGH>(nameof(AtaSmart.SCSI_PASS_THROUGH.DataBuf)));
            buffers.Spt.SenseInfoOffset = (uint)Marshal.OffsetOf(typeof(AtaSmart.SCSI_PASS_THROUGH), nameof(AtaSmart.SCSI_PASS_THROUGH.SenseBuf));
            buffers.Spt.CdbLength = 16;
            buffers.Spt.Cdb[0] = 0xA2; // SECURITY PROTOCOL IN
            buffers.Spt.Cdb[1] = 0xFE; // Samsung Protocol
            buffers.Spt.Cdb[3] = 5; // Identify
            buffers.Spt.Cdb[8] = 2; // Transfer Length
            buffers.Spt.Cdb[9] = 0;
            buffers.Spt.DataIn = (byte)PInvoke.SCSI_IOCTL_DATA_IN;

            length = Marshal.SizeOf<AtaSmart.SCSI_PASS_THROUGH>();
            buffer = Marshal.AllocHGlobal(length);
            Marshal.StructureToPtr(buffers, buffer, false);

            validTransfer = PInvoke.DeviceIoControl(handle, PInvoke.IOCTL_SCSI_PASS_THROUGH, (void*)buffer, (uint)length, (void*)buffer, (uint)length, null, null);
            if (validTransfer)
            {
                AtaSmart.SCSI_PASS_THROUGH result = Marshal.PtrToStructure<AtaSmart.SCSI_PASS_THROUGH>(buffer);

                if (IsAllZero(result.DataBuf, AtaSmart.IOCTL_BUFFER_SIZE))
                {
                    handle.Close();
                    handle = null;
                }
            }
            else
            {
                handle.Close();
                handle = null;
            }

            Marshal.FreeHGlobal(buffer);
        }

        return handle;
    }

    private static unsafe bool IsAllZero(byte* bytes, int length)
    {
        for (int i = 0; i < length; i++)
        {
            if (bytes[i] != 0)
                return false;
        }
        return true;
    }
}
