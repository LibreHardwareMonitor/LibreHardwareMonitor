// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Interop;
using Microsoft.Win32.SafeHandles;

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

    public bool IdentifyController(SafeHandle hDevice, out Kernel32.NVME_IDENTIFY_CONTROLLER_DATA data)
    {
        data = Kernel32.CreateStruct<Kernel32.NVME_IDENTIFY_CONTROLLER_DATA>();
        if (hDevice?.IsInvalid != false)
            return false;

        bool result = false;
        Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS buffers = Kernel32.CreateStruct<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>();

        buffers.Spt.Length = (ushort)Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH>();
        buffers.Spt.PathId = 0;
        buffers.Spt.TargetId = 0;
        buffers.Spt.Lun = 0;
        buffers.Spt.SenseInfoLength = 24;
        buffers.Spt.DataTransferLength = (uint)buffers.DataBuf.Length;
        buffers.Spt.TimeOutValue = 2;
        buffers.Spt.DataBufferOffset = Marshal.OffsetOf(typeof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS), nameof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS.DataBuf));
        buffers.Spt.SenseInfoOffset = (uint)Marshal.OffsetOf(typeof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS), nameof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS.SenseBuf));
        buffers.Spt.CdbLength = 16;
        buffers.Spt.Cdb[0] = 0xB5; // SECURITY PROTOCOL IN
        buffers.Spt.Cdb[1] = 0xFE; // Samsung Protocol
        buffers.Spt.Cdb[3] = 5; // Identify
        buffers.Spt.Cdb[8] = 0; // Transfer Length
        buffers.Spt.Cdb[9] = 0x40; // Transfer Length
        buffers.Spt.DataIn = (byte)Kernel32.SCSI_IOCTL_DATA.SCSI_IOCTL_DATA_OUT;
        buffers.DataBuf[0] = 1;

        int length = Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>();
        IntPtr buffer = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr(buffers, buffer, false);
        bool validTransfer = Kernel32.DeviceIoControl(hDevice, Kernel32.IOCTL.IOCTL_SCSI_PASS_THROUGH, buffer, length, buffer, length, out _, IntPtr.Zero);
        Marshal.FreeHGlobal(buffer);

        if (validTransfer)
        {
            //read data from samsung SSD
            buffers = Kernel32.CreateStruct<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>();
            buffers.Spt.Length = (ushort)Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH>();
            buffers.Spt.PathId = 0;
            buffers.Spt.TargetId = 0;
            buffers.Spt.Lun = 0;
            buffers.Spt.SenseInfoLength = 24;
            buffers.Spt.DataTransferLength = (uint)buffers.DataBuf.Length;
            buffers.Spt.TimeOutValue = 2;
            buffers.Spt.DataBufferOffset = Marshal.OffsetOf<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>(nameof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS.DataBuf));
            buffers.Spt.SenseInfoOffset = (uint)Marshal.OffsetOf<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>(nameof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS.SenseBuf));
            buffers.Spt.CdbLength = 16;
            buffers.Spt.Cdb[0] = 0xA2; // SECURITY PROTOCOL IN
            buffers.Spt.Cdb[1] = 0xFE; // Samsung Protocol
            buffers.Spt.Cdb[3] = 5; // Identify
            buffers.Spt.Cdb[8] = 2; // Transfer Length (high)
            buffers.Spt.Cdb[9] = 0; // Transfer Length (low)
            buffers.Spt.DataIn = (byte)Kernel32.SCSI_IOCTL_DATA.SCSI_IOCTL_DATA_IN;

            length = Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>();
            buffer = Marshal.AllocHGlobal(length);
            Marshal.StructureToPtr(buffers, buffer, false);

            validTransfer = Kernel32.DeviceIoControl(hDevice, Kernel32.IOCTL.IOCTL_SCSI_PASS_THROUGH, buffer, length, buffer, length, out _, IntPtr.Zero);
            if (validTransfer && buffers.DataBuf.Any(x => x != 0))
            {
                IntPtr offset = Marshal.OffsetOf<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>(nameof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS.DataBuf));
                IntPtr newPtr = IntPtr.Add(buffer, offset.ToInt32());
                data = Marshal.PtrToStructure<Kernel32.NVME_IDENTIFY_CONTROLLER_DATA>(newPtr);
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

    public bool HealthInfoLog(SafeHandle hDevice, out Kernel32.NVME_HEALTH_INFO_LOG data)
    {
        data = Kernel32.CreateStruct<Kernel32.NVME_HEALTH_INFO_LOG>();
        if (hDevice?.IsInvalid != false)
            return false;

        bool result = false;
        Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS buffers = Kernel32.CreateStruct<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>();

        buffers.Spt.Length = (ushort)Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH>();
        buffers.Spt.PathId = 0;
        buffers.Spt.TargetId = 0;
        buffers.Spt.Lun = 0;
        buffers.Spt.SenseInfoLength = 24;
        buffers.Spt.DataTransferLength = (uint)buffers.DataBuf.Length;
        buffers.Spt.TimeOutValue = 2;
        buffers.Spt.DataBufferOffset = Marshal.OffsetOf<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>(nameof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS.DataBuf));
        buffers.Spt.SenseInfoOffset = (uint)Marshal.OffsetOf<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>(nameof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS.SenseBuf));
        buffers.Spt.CdbLength = 16;
        buffers.Spt.Cdb[0] = 0xB5; // SECURITY PROTOCOL IN
        buffers.Spt.Cdb[1] = 0xFE; // Samsung Protocol
        buffers.Spt.Cdb[3] = 6; // Log Data
        buffers.Spt.Cdb[8] = 0; // Transfer Length
        buffers.Spt.Cdb[9] = 0x40; // Transfer Length
        buffers.Spt.DataIn = (byte)Kernel32.SCSI_IOCTL_DATA.SCSI_IOCTL_DATA_OUT;
        buffers.DataBuf[0] = 2;
        buffers.DataBuf[4] = 0xff;
        buffers.DataBuf[5] = 0xff;
        buffers.DataBuf[6] = 0xff;
        buffers.DataBuf[7] = 0xff;

        int length = Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>();
        IntPtr buffer = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr(buffers, buffer, false);
        bool validTransfer = Kernel32.DeviceIoControl(hDevice, Kernel32.IOCTL.IOCTL_SCSI_PASS_THROUGH, buffer, length, buffer, length, out _, IntPtr.Zero);
        Marshal.FreeHGlobal(buffer);

        if (validTransfer)
        {
            //read data from samsung SSD
            buffers = Kernel32.CreateStruct<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>();
            buffers.Spt.Length = (ushort)Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH>();
            buffers.Spt.PathId = 0;
            buffers.Spt.TargetId = 0;
            buffers.Spt.Lun = 0;
            buffers.Spt.SenseInfoLength = 24;
            buffers.Spt.DataTransferLength = (uint)buffers.DataBuf.Length;
            buffers.Spt.TimeOutValue = 2;
            buffers.Spt.DataBufferOffset = Marshal.OffsetOf<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>(nameof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS.DataBuf));
            buffers.Spt.SenseInfoOffset = (uint)Marshal.OffsetOf<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>(nameof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS.SenseBuf));
            buffers.Spt.CdbLength = 16;
            buffers.Spt.Cdb[0] = 0xA2; // SECURITY PROTOCOL IN
            buffers.Spt.Cdb[1] = 0xFE; // Samsung Protocol
            buffers.Spt.Cdb[3] = 6; // Log Data
            buffers.Spt.Cdb[8] = 2; // Transfer Length (high)
            buffers.Spt.Cdb[9] = 0; // Transfer Length (low)
            buffers.Spt.DataIn = (byte)Kernel32.SCSI_IOCTL_DATA.SCSI_IOCTL_DATA_IN;

            length = Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>();
            buffer = Marshal.AllocHGlobal(length);
            Marshal.StructureToPtr(buffers, buffer, false);

            validTransfer = Kernel32.DeviceIoControl(hDevice, Kernel32.IOCTL.IOCTL_SCSI_PASS_THROUGH, buffer, length, buffer, length, out _, IntPtr.Zero);
            if (validTransfer)
            {
                IntPtr offset = Marshal.OffsetOf<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>(nameof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS.DataBuf));
                IntPtr newPtr = IntPtr.Add(buffer, offset.ToInt32());
                data = Marshal.PtrToStructure<Kernel32.NVME_HEALTH_INFO_LOG>(newPtr);
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

    public static SafeHandle IdentifyDevice(StorageInfo storageInfo)
    {
        SafeFileHandle handle = Kernel32.OpenDevice(storageInfo.DeviceId);
        if (handle?.IsInvalid != false)
            return null;

        Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS buffers = Kernel32.CreateStruct<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>();

        buffers.Spt.Length = (ushort)Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH>();
        buffers.Spt.PathId = 0;
        buffers.Spt.TargetId = 0;
        buffers.Spt.Lun = 0;
        buffers.Spt.SenseInfoLength = 24;
        buffers.Spt.DataTransferLength = (uint)buffers.DataBuf.Length;
        buffers.Spt.TimeOutValue = 2;
        buffers.Spt.DataBufferOffset = Marshal.OffsetOf(typeof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS), nameof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS.DataBuf));
        buffers.Spt.SenseInfoOffset = (uint)Marshal.OffsetOf(typeof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS), nameof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS.SenseBuf));
        buffers.Spt.CdbLength = 16;
        buffers.Spt.Cdb[0] = 0xB5; // SECURITY PROTOCOL IN
        buffers.Spt.Cdb[1] = 0xFE; // Samsung Protocol
        buffers.Spt.Cdb[3] = 5; // Identify
        buffers.Spt.Cdb[8] = 0; // Transfer Length
        buffers.Spt.Cdb[9] = 0x40;
        buffers.Spt.DataIn = (byte)Kernel32.SCSI_IOCTL_DATA.SCSI_IOCTL_DATA_OUT;
        buffers.DataBuf[0] = 1;

        int length = Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>();
        IntPtr buffer = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr(buffers, buffer, false);
        bool validTransfer = Kernel32.DeviceIoControl(handle, Kernel32.IOCTL.IOCTL_SCSI_PASS_THROUGH, buffer, length, buffer, length, out _, IntPtr.Zero);
        Marshal.FreeHGlobal(buffer);

        if (validTransfer)
        {
            //read data from samsung SSD
            buffers = Kernel32.CreateStruct<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>();
            buffers.Spt.Length = (ushort)Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH>();
            buffers.Spt.PathId = 0;
            buffers.Spt.TargetId = 0;
            buffers.Spt.Lun = 0;
            buffers.Spt.SenseInfoLength = 24;
            buffers.Spt.DataTransferLength = (uint)buffers.DataBuf.Length;
            buffers.Spt.TimeOutValue = 2;
            buffers.Spt.DataBufferOffset = Marshal.OffsetOf(typeof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS), nameof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS.DataBuf));
            buffers.Spt.SenseInfoOffset = (uint)Marshal.OffsetOf(typeof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS), nameof(Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS.SenseBuf));
            buffers.Spt.CdbLength = 16;
            buffers.Spt.Cdb[0] = 0xA2; // SECURITY PROTOCOL IN
            buffers.Spt.Cdb[1] = 0xFE; // Samsung Protocol
            buffers.Spt.Cdb[3] = 5; // Identify
            buffers.Spt.Cdb[8] = 2; // Transfer Length
            buffers.Spt.Cdb[9] = 0;
            buffers.Spt.DataIn = (byte)Kernel32.SCSI_IOCTL_DATA.SCSI_IOCTL_DATA_IN;

            length = Marshal.SizeOf<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>();
            buffer = Marshal.AllocHGlobal(length);
            Marshal.StructureToPtr(buffers, buffer, false);

            validTransfer = Kernel32.DeviceIoControl(handle, Kernel32.IOCTL.IOCTL_SCSI_PASS_THROUGH, buffer, length, buffer, length, out _, IntPtr.Zero);
            if (validTransfer)
            {
                Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS result = Marshal.PtrToStructure<Kernel32.SCSI_PASS_THROUGH_WITH_BUFFERS>(buffer);

                if (result.DataBuf.All(x => x == 0))
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
}
