// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using LibreHardwareMonitor.Interop;
using Windows.Win32;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.System.Ioctl;
using static LibreHardwareMonitor.Interop.AtaSmart;

namespace LibreHardwareMonitor.Hardware.Storage;

internal class WindowsSmart : ISmart
{
    private readonly int _driveNumber;
    private readonly SafeHandle _handle;

    public WindowsSmart(int driveNumber)
    {
        _driveNumber = driveNumber;
        _handle = PInvoke.CreateFile(@"\\.\PhysicalDrive" + driveNumber, (uint)FileAccess.ReadWrite, FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE, null, FILE_CREATION_DISPOSITION.OPEN_EXISTING, FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL, null);
    }

    public bool IsValid => !_handle.IsInvalid && !_handle.IsClosed;

    public void Dispose()
    {
        Close();
    }

    public void Close()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public unsafe bool EnableSmart()
    {
        if (_handle.IsClosed)
            throw new ObjectDisposedException(nameof(WindowsSmart));

        var parameter = new SENDCMDINPARAMS
        {
            bDriveNumber = (byte)_driveNumber,
            irDriveRegs =
            {
                bFeaturesReg = 0xD8,
                bCylLowReg = (byte)PInvoke.SMART_CYL_LOW,
                bCylHighReg = (byte)PInvoke.SMART_CYL_HI,
                bCommandReg = (byte) PInvoke.SMART_CMD
            }
        };

        var result = new SENDCMDOUTPARAMS();

        return PInvoke.DeviceIoControl(_handle,
                                        PInvoke.SMART_SEND_DRIVE_COMMAND,
                                        &parameter,
                                        (uint)sizeof(SENDCMDINPARAMS),
                                        &result,
                                        (uint)sizeof(SENDCMDOUTPARAMS),
                                        null,
                                        null);
    }

    public unsafe SMART_ATTRIBUTE[] ReadSmartData()
    {
        if (_handle.IsClosed)
            throw new ObjectDisposedException(nameof(WindowsSmart));

        var parameter = new SENDCMDINPARAMS
        {
            bDriveNumber = (byte)_driveNumber,
            irDriveRegs =
            {
                bFeaturesReg = (byte)PInvoke.READ_ATTRIBUTES,
                bCylLowReg = (byte)PInvoke.SMART_CYL_LOW,
                bCylHighReg = (byte)PInvoke.SMART_CYL_HI,
                bCommandReg = (byte) PInvoke.SMART_CMD
            }
        };

        int cb = sizeof(SENDCMDOUTPARAMS) + 512; // 512 bytes buffer.
        IntPtr buffer = Marshal.AllocHGlobal(cb);

        bool isValid = PInvoke.DeviceIoControl(_handle,
                                               PInvoke.SMART_RCV_DRIVE_DATA,
                                               &parameter,
                                               (uint)sizeof(SENDCMDINPARAMS),
                                               (void*)buffer,
                                               (uint)cb,
                                               null,
                                               null);

        if (isValid)
        {
            var attributes = new SMART_ATTRIBUTE[30]; // A maximum of 30 are returned.

            var sendCmdOutParams = (SENDCMDOUTPARAMS*)buffer;
            fixed (byte* pBuffer = &sendCmdOutParams->bBuffer[0])
            {
                var pAttribute = (SMART_ATTRIBUTE*)(pBuffer + 2); // + 2 padding.
                
                for (int i = 0; i < attributes.Length; i++)
                    attributes[i] = pAttribute[i];
            }

            Marshal.FreeHGlobal(buffer);
            return attributes;
        }

        Marshal.FreeHGlobal(buffer);

        return [];
    }

    public unsafe SMART_THRESHOLD[] ReadSmartThresholds()
    {
        if (_handle.IsClosed)
            throw new ObjectDisposedException(nameof(WindowsSmart));

        var parameter = new SENDCMDINPARAMS
        {
            bDriveNumber = (byte)_driveNumber,
            irDriveRegs =
            {
                bFeaturesReg = (byte)PInvoke.READ_THRESHOLDS,
                bCylLowReg = (byte)PInvoke.SMART_CYL_LOW,
                bCylHighReg = (byte)PInvoke.SMART_CYL_HI,
                bCommandReg = (byte) PInvoke.SMART_CMD
            }
        };

        int cb = sizeof(SENDCMDOUTPARAMS) + 512; // 2 bytes padding + 512 bytes buffer.
        IntPtr buffer = Marshal.AllocHGlobal(cb);
        bool isValid = PInvoke.DeviceIoControl(_handle,
                                              PInvoke.SMART_RCV_DRIVE_DATA,
                                              &parameter,
                                              (uint)sizeof(SENDCMDINPARAMS),
                                              (void*)buffer,
                                              (uint)cb,
                                              null,
                                              null);

        if (isValid)
        {
            var thresholds = new SMART_THRESHOLD[30]; // A maximum of 30 are returned.

            var sendCmdOutParams = (SENDCMDOUTPARAMS*)buffer;
            fixed (byte* pBuffer = &sendCmdOutParams->bBuffer[0])
            {
                var pThreshold = (SMART_THRESHOLD*)(pBuffer + 2); // + 2 padding.

                for (int i = 0; i < thresholds.Length; i++)
                    thresholds[i] = pThreshold[i];
            }

            Marshal.FreeHGlobal(buffer);
            return thresholds;
        }

        Marshal.FreeHGlobal(buffer);
        return null;
    }

    public unsafe bool ReadNameAndFirmwareRevision(out string name, out string firmwareRevision)
    {
        if (_handle.IsClosed)
            throw new ObjectDisposedException(nameof(WindowsSmart));

        var parameter = new SENDCMDINPARAMS
        {
            bDriveNumber = (byte)_driveNumber,
            irDriveRegs =
            {
                bCommandReg = 0xEC
            }
        };

        int cb = sizeof(SENDCMDOUTPARAMS) + sizeof(IDENTIFY_DEVICE_DATA);
        IntPtr buffer = Marshal.AllocHGlobal(cb);

        bool valid = PInvoke.DeviceIoControl(_handle,
                                              PInvoke.SMART_RCV_DRIVE_DATA,
                                              &parameter,
                                              (uint)sizeof(SENDCMDINPARAMS),
                                              (void*)buffer,
                                              (uint)cb,
                                              null,
                                              null);

        if (!valid)
        {
            Marshal.FreeHGlobal(buffer);
            name = null;
            firmwareRevision = null;
            return false;
        }

        IDENTIFY_DEVICE_DATA identity = *(IDENTIFY_DEVICE_DATA*)((byte*)buffer + (int)Marshal.OffsetOf<SENDCMDOUTPARAMS>(nameof(SENDCMDOUTPARAMS.bBuffer)));

        byte* p = identity.ModelNumber;
        for (int i = 0; i < 40; i += 2)
        {
            (p[i], p[i + 1]) = (p[i + 1], p[i]);
        }

        p = identity.FirmwareRevision;
        for (int i = 0; i < 8; i += 2)
        {
            (p[i], p[i + 1]) = (p[i + 1], p[i]);
        }

        name = Encoding.ASCII.GetString(identity.ModelNumber, 40).Trim();
        firmwareRevision = Encoding.ASCII.GetString(identity.FirmwareRevision, 8);

        Marshal.FreeHGlobal(buffer);
        return true;
    }

    protected void Dispose(bool disposing)
    {
        if (disposing && !_handle.IsClosed)
        {
            _handle.Close();
        }
    }
}
