// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Win32.Storage.IscsiDisc;
using Windows.Win32.System.Ioctl;
using Microsoft.Win32.SafeHandles;

// ReSharper disable InconsistentNaming

namespace LibreHardwareMonitor.Interop;

public class Kernel32
{
    public const int ERROR_SERVICE_ALREADY_RUNNING = unchecked((int)0x80070420);
    public const int ERROR_SERVICE_EXISTS = unchecked((int)0x80070431);

    public const int IOCTL_BUFFER_SIZE = 4096;
    public const int NVME_IOCTL_CMD_DW_SIZE = 16;
    public const int NVME_IOCTL_COMPLETE_DW_SIZE = 4;
    public const int NVME_IOCTL_VENDOR_SPECIFIC_DW_SIZE = 6;
    public const int SCSI_IOCTL_SENSE_SIZE = 24;

    internal const uint BATTERY_UNKNOWN_CAPACITY = 0xFFFFFFFF;
    internal const int BATTERY_UNKNOWN_RATE = unchecked((int)0x80000000);
    internal const uint BATTERY_UNKNOWN_TIME = 0xFFFFFFFF;
    internal const uint BATTERY_UNKNOWN_VOLTAGE = 0xFFFFFFFF;

    internal const uint LPTR = 0x0000 | 0x0040;

    internal const int MAX_DRIVE_ATTRIBUTES = 512;
    internal const uint NVME_PASS_THROUGH_SRB_IO_CODE = 0xe0002000;
    internal const byte SMART_LBA_HI = 0xC2;
    internal const byte SMART_LBA_HI_EXCEEDED = 0x2C;
    internal const byte SMART_LBA_MID = 0x4F;
    internal const byte SMART_LBA_MID_EXCEEDED = 0xF4;

    private const string DllName = "kernel32.dll";

    internal static SafeFileHandle OpenDevice(string devicePath)
    {
        SafeFileHandle hDevice = CreateFile(devicePath, FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
        if (hDevice.IsInvalid || hDevice.IsClosed)
            hDevice = null;

        return hDevice;
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern SafeFileHandle CreateFile
    (
        [MarshalAs(UnmanagedType.LPTStr)] string lpFileName,
        [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess,
        [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
        IntPtr lpSecurityAttributes,
        [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
        [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeviceIoControl
    (
        SafeHandle hDevice,
        DFP dwIoControlCode,
        ref SENDCMDINPARAMS lpInBuffer,
        int nInBufferSize,
        out ATTRIBUTECMDOUTPARAMS lpOutBuffer,
        int nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeviceIoControl
    (
        SafeHandle hDevice,
        DFP dwIoControlCode,
        ref SENDCMDINPARAMS lpInBuffer,
        int nInBufferSize,
        out THRESHOLDCMDOUTPARAMS lpOutBuffer,
        int nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeviceIoControl
    (
        SafeHandle hDevice,
        DFP dwIoControlCode,
        ref SENDCMDINPARAMS lpInBuffer,
        int nInBufferSize,
        out IDENTIFYCMDOUTPARAMS lpOutBuffer,
        int nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeviceIoControl
    (
        SafeHandle hDevice,
        DFP dwIoControlCode,
        ref SENDCMDINPARAMS lpInBuffer,
        int nInBufferSize,
        out STATUSCMDOUTPARAMS lpOutBuffer,
        int nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeviceIoControl
    (
        SafeHandle hDevice,
        IOCTL dwIoControlCode,
        IntPtr lpInBuffer,
        int nInBufferSize,
        IntPtr lpOutBuffer,
        int nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeviceIoControl
    (
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        [In] byte[] lpInBuffer,
        uint nInBufferSize,
        [Out] byte[] lpOutBuffer,
        uint nOutBufferSize,
        ref uint lpBytesReturned,
        [In] [Optional] IntPtr lpOverlapped);

    [DllImport(DllName, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern void RtlZeroMemory(IntPtr Destination, int Length);

    [DllImport(DllName, SetLastError = false)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern void RtlCopyMemory(IntPtr Destination, IntPtr Source, uint Length);

    internal enum DFP : uint
    {
        DFP_GET_VERSION = 0x00074080,
        DFP_SEND_DRIVE_COMMAND = 0x0007c084,
        DFP_RECEIVE_DRIVE_DATA = 0x0007c088
    }

    internal enum IOCTL : uint
    {
        IOCTL_SCSI_PASS_THROUGH = 0x04d004,
        IOCTL_SCSI_MINIPORT = 0x04d008,
        IOCTL_SCSI_PASS_THROUGH_DIRECT = 0x04d014,
        IOCTL_SCSI_GET_ADDRESS = 0x41018,
        IOCTL_DISK_PERFORMANCE = 0x70020,
        IOCTL_STORAGE_QUERY_PROPERTY = 0x2D1400,
        IOCTL_BATTERY_QUERY_TAG = 0x294040,
        IOCTL_BATTERY_QUERY_INFORMATION = 0x294044,
        IOCTL_BATTERY_QUERY_STATUS = 0x29404C
    }

    [Flags]
    internal enum NVME_DIRECTION : uint
    {
        NVME_FROM_HOST_TO_DEV = 1,
        NVME_FROM_DEV_TO_HOST = 2,
        NVME_BI_DIRECTION = NVME_FROM_DEV_TO_HOST | NVME_FROM_HOST_TO_DEV
    }

    internal enum ATA_COMMAND : byte
    {
        /// <summary>
        /// SMART data requested.
        /// </summary>
        ATA_SMART = 0xB0,

        /// <summary>
        /// Identify data is requested.
        /// </summary>
        ATA_IDENTIFY_DEVICE = 0xEC
    }

    internal enum SCSI_IOCTL_DATA
    {
        SCSI_IOCTL_DATA_OUT = 0,
        SCSI_IOCTL_DATA_IN = 1,
        SCSI_IOCTL_DATA_UNSPECIFIED = 2
    }

    internal enum SMART_FEATURES : byte
    {
        /// <summary>
        /// Read SMART data.
        /// </summary>
        SMART_READ_DATA = 0xD0,

        /// <summary>
        /// Read SMART thresholds.
        /// obsolete
        /// </summary>
        READ_THRESHOLDS = 0xD1,

        /// <summary>
        /// Autosave SMART data.
        /// </summary>
        ENABLE_DISABLE_AUTOSAVE = 0xD2,

        /// <summary>
        /// Save SMART attributes.
        /// </summary>
        SAVE_ATTRIBUTE_VALUES = 0xD3,

        /// <summary>
        /// Set SMART to offline immediately.
        /// </summary>
        EXECUTE_OFFLINE_DIAGS = 0xD4,

        /// <summary>
        /// Read SMART log.
        /// </summary>
        SMART_READ_LOG = 0xD5,

        /// <summary>
        /// Write SMART log.
        /// </summary>
        SMART_WRITE_LOG = 0xD6,

        /// <summary>
        /// Write SMART thresholds.
        /// obsolete
        /// </summary>
        WRITE_THRESHOLDS = 0xD7,

        /// <summary>
        /// Enable SMART.
        /// </summary>
        ENABLE_SMART = 0xD8,

        /// <summary>
        /// Disable SMART.
        /// </summary>
        DISABLE_SMART = 0xD9,

        /// <summary>
        /// Get SMART status.
        /// </summary>
        RETURN_SMART_STATUS = 0xDA,

        /// <summary>
        /// Set SMART to offline automatically.
        /// </summary>
        ENABLE_DISABLE_AUTO_OFFLINE = 0xDB /* obsolete */
    }

    internal enum STORAGE_PROTOCOL_NVME_PROTOCOL_DATA_REQUEST_VALUE
    {
        NVMeIdentifyCnsSpecificNamespace = 0,
        NVMeIdentifyCnsController = 1,
        NVMeIdentifyCnsActiveNamespaces = 2
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SMART_ATTRIBUTE
    {
        public byte Id;
        public short Flags;
        public byte CurrentValue;
        public byte WorstValue;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] RawValue;

        public byte Reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SMART_THRESHOLD
    {
        public byte Id;
        public byte Threshold;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] Reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct ATTRIBUTECMDOUTPARAMS
    {
        public uint cBufferSize;
        public DRIVERSTATUS DriverStatus;
        public byte Version;
        public byte Reserved;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_DRIVE_ATTRIBUTES)]
        public SMART_ATTRIBUTE[] Attributes;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct THRESHOLDCMDOUTPARAMS
    {
        public uint cBufferSize;
        public DRIVERSTATUS DriverStatus;
        public byte Version;
        public byte Reserved;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_DRIVE_ATTRIBUTES)]
        public SMART_THRESHOLD[] Thresholds;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct STATUSCMDOUTPARAMS
    {
        public uint cBufferSize;
        public DRIVERSTATUS DriverStatus;
        public IDEREGS irDriveRegs;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct IDENTIFY_DEVICE_DATA
    {
        public ushort GeneralConfiguration;
        public ushort NumberOfCylinders;
        public ushort Reserved1;
        public ushort NumberOfHeads;
        public ushort UnformattedBytesPerTrack;
        public ushort UnformattedBytesPerSector;
        public ushort SectorsPerTrack;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public ushort[] VendorUnique;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] SerialNumber;

        public ushort BufferType;
        public ushort BufferSectorSize;
        public ushort NumberOfEccBytes;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] FirmwareRevision;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public byte[] ModelNumber;

        public byte MaximumBlockTransfer;
        public byte VendorUnique2;
        public ushort DoubleWordIo;
        public ushort Capabilities;
        public ushort Reserved2;
        public byte VendorUnique3;
        public byte PioCycleTimingMode;
        public byte VendorUnique4;
        public byte DmaCycleTimingMode;
        public ushort TranslationFieldsValid;
        public ushort NumberOfCurrentCylinders;
        public ushort NumberOfCurrentHeads;
        public ushort CurrentSectorsPerTrack;
        public uint CurrentSectorCapacity;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 197)]
        public ushort[] Reserved3;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct IDENTIFYCMDOUTPARAMS
    {
        public uint cBufferSize;
        public DRIVERSTATUS DriverStatus;
        public IDENTIFY_DEVICE_DATA Identify;
    }

    internal unsafe struct NVME_PASS_THROUGH_IOCTL
    {
        public SRB_IO_CONTROL SrbIoCtrl;
        public fixed uint VendorSpecific[NVME_IOCTL_VENDOR_SPECIFIC_DW_SIZE];
        public fixed uint NVMeCmd[NVME_IOCTL_CMD_DW_SIZE];
        public fixed uint CplEntry[NVME_IOCTL_COMPLETE_DW_SIZE];
        public NVME_DIRECTION Direction;
        public uint QueueId;
        public uint DataBufferLen;
        public uint MetaDataLen;
        public uint ReturnBufferLen;
        public fixed byte DataBuffer[IOCTL_BUFFER_SIZE];
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct SCSI_PASS_THROUGH_WITH_BUFFERS
    {
        public SCSI_PASS_THROUGH Spt;
        public fixed byte SenseBuf[SCSI_IOCTL_SENSE_SIZE];
        public fixed byte DataBuf[IOCTL_BUFFER_SIZE];
    }
}
