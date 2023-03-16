// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

// ReSharper disable InconsistentNaming

namespace LibreHardwareMonitor.Interop;

public class Kernel32
{
    public const int ERROR_SERVICE_ALREADY_RUNNING = unchecked((int)0x80070420);

    public const int ERROR_SERVICE_EXISTS = unchecked((int)0x80070431);

    internal const uint BATTERY_UNKNOWN_TIME = 0xFFFFFFFF;
    internal const string IntelNVMeMiniPortSignature1 = "NvmeMini";
    internal const string IntelNVMeMiniPortSignature2 = "IntelNvm";

    internal const uint LPTR = 0x0000 | 0x0040;

    internal const int MAX_DRIVE_ATTRIBUTES = 512;
    internal const uint NVME_PASS_THROUGH_SRB_IO_CODE = 0xe0002000;
    internal const byte SMART_LBA_HI = 0xC2;
    internal const byte SMART_LBA_HI_EXCEEDED = 0x2C;
    internal const byte SMART_LBA_MID = 0x4F;
    internal const byte SMART_LBA_MID_EXCEEDED = 0xF4;

    private const string DllName = "kernel32.dll";

    [Flags]
    public enum NVME_CRITICAL_WARNING
    {
        None = 0x00,

        /// <summary>
        /// If set to 1, then the available spare space has fallen below the threshold.
        /// </summary>
        AvailableSpaceLow = 0x01,

        /// <summary>
        /// If set to 1, then a temperature is above an over temperature threshold or below an under temperature threshold.
        /// </summary>
        TemperatureThreshold = 0x02,

        /// <summary>
        /// If set to 1, then the device reliability has been degraded due to significant media related errors or any internal error that degrades device reliability.
        /// </summary>
        ReliabilityDegraded = 0x04,

        /// <summary>
        /// If set to 1, then the media has been placed in read only mode
        /// </summary>
        ReadOnly = 0x08,

        /// <summary>
        /// If set to 1, then the volatile memory backup device has failed. This field is only valid if the controller has a volatile memory backup solution.
        /// </summary>
        VolatileMemoryBackupDeviceFailed = 0x10
    }

    /// <summary>
    /// Create a instance from a struct with zero initialized memory arrays
    /// no need to init every inner array with the correct sizes
    /// </summary>
    /// <typeparam name="T">type of struct that is needed</typeparam>
    /// <returns></returns>
    internal static T CreateStruct<T>()
    {
        int size = Marshal.SizeOf<T>();
        IntPtr ptr = Marshal.AllocHGlobal(size);
        RtlZeroMemory(ptr, size);
        T result = Marshal.PtrToStructure<T>(ptr);
        Marshal.FreeHGlobal(ptr);
        return result;
    }

    internal static SafeFileHandle OpenDevice(string devicePath)
    {
        SafeFileHandle hDevice = CreateFile(devicePath, FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
        if (hDevice.IsInvalid || hDevice.IsClosed)
            hDevice = null;

        return hDevice;
    }

    [DllImport(DllName, CharSet = CharSet.Auto, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

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
        out SENDCMDOUTPARAMS lpOutBuffer,
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
        ref STORAGE_PROPERTY_QUERY lpInBuffer,
        int nInBufferSize,
        out STORAGE_DEVICE_DESCRIPTOR_HEADER lpOutBuffer,
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
        ref STORAGE_PROPERTY_QUERY lpInBuffer,
        int nInBufferSize,
        IntPtr lpOutBuffer,
        uint nOutBufferSize,
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
        SafeHandle hDevice,
        IOCTL dwIoControlCode,
        IntPtr lpInBuffer,
        int nInBufferSize,
        out DISK_PERFORMANCE lpOutBuffer,
        int nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeviceIoControl
    (
        SafeFileHandle hDevice,
        IOCTL dwIoControlCode,
        ref BATTERY_QUERY_INFORMATION lpInBuffer,
        int nInBufferSize,
        ref BATTERY_INFORMATION lpOutBuffer,
        int nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeviceIoControl
    (
        SafeFileHandle hDevice,
        IOCTL dwIoControlCode,
        ref uint lpInBuffer,
        int nInBufferSize,
        ref uint lpOutBuffer,
        int nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeviceIoControl
    (
        SafeFileHandle hDevice,
        IOCTL dwIoControlCode,
        ref BATTERY_WAIT_STATUS lpInBuffer,
        int nInBufferSize,
        ref BATTERY_STATUS lpOutBuffer,
        int nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeviceIoControl
    (
        SafeFileHandle hDevice,
        IOCTL dwIoControlCode,
        ref BATTERY_QUERY_INFORMATION lpInBuffer,
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
        IOCTL dwIoControlCode,
        ref BATTERY_QUERY_INFORMATION lpInBuffer,
        int nInBufferSize,
        ref uint lpOutBuffer,
        int nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern IntPtr LocalAlloc(uint uFlags, ulong uBytes);

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern IntPtr LocalFree(IntPtr hMem);

    [DllImport(DllName, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern void RtlZeroMemory(IntPtr Destination, int Length);

    [DllImport(DllName, SetLastError = false)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern void RtlCopyMemory(IntPtr Destination, IntPtr Source, uint Length);

    [DllImport(DllName, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport(DllName, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern IntPtr GetProcAddress(IntPtr module, string methodName);

    [DllImport(DllName)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool FreeLibrary(IntPtr module);

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern IntPtr GetCurrentThread();

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern ushort GetActiveProcessorGroupCount();

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern bool SetThreadGroupAffinity(IntPtr thread, ref GROUP_AFFINITY groupAffinity, out GROUP_AFFINITY previousGroupAffinity);

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern IntPtr VirtualAlloc(IntPtr lpAddress, UIntPtr dwSize, MEM flAllocationType, PAGE flProtect);

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern bool VirtualFree(IntPtr lpAddress, UIntPtr dwSize, MEM dwFreeType);

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern bool DeviceIoControl
    (
        SafeFileHandle device,
        IOControlCode ioControlCode,
        [MarshalAs(UnmanagedType.AsAny)] [In] object inBuffer,
        uint inBufferSize,
        [MarshalAs(UnmanagedType.AsAny)] [Out] object outBuffer,
        uint nOutBufferSize,
        out uint bytesReturned,
        IntPtr overlapped);

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern IntPtr CreateFile
    (
        string lpFileName,
        uint dwDesiredAccess,
        FileShare dwShareMode,
        IntPtr lpSecurityAttributes,
        FileMode dwCreationDisposition,
        FileAttributes dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern int EnumSystemFirmwareTables(Provider firmwareTableProviderSignature, IntPtr firmwareTableBuffer, int bufferSize);

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern int GetSystemFirmwareTable(Provider firmwareTableProviderSignature, int firmwareTableID, IntPtr firmwareTableBuffer, int bufferSize);

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct GROUP_AFFINITY
    {
        public UIntPtr Mask;

        [MarshalAs(UnmanagedType.U2)]
        public ushort Group;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.U2)]
        public ushort[] Reserved;
    }

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
    internal enum BatteryCapabilities : uint
    {
        BATTERY_CAPACITY_RELATIVE = 0x40000000,
        BATTERY_IS_SHORT_TERM = 0x20000000,
        BATTERY_SET_CHARGE_SUPPORTED = 0x00000001,
        BATTERY_SET_DISCHARGE_SUPPORTED = 0x00000002,
        BATTERY_SYSTEM_BATTERY = 0x80000000
    }

    internal enum BATTERY_QUERY_INFORMATION_LEVEL
    {
        BatteryInformation,
        BatteryGranularityInformation,
        BatteryTemperature,
        BatteryEstimatedTime,
        BatteryDeviceName,
        BatteryManufactureDate,
        BatteryManufactureName,
        BatteryUniqueID,
        BatterySerialNumber
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BATTERY_QUERY_INFORMATION
    {
        public uint BatteryTag;
        public BATTERY_QUERY_INFORMATION_LEVEL InformationLevel;
        public uint AtRate;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BATTERY_INFORMATION
    {
        public BatteryCapabilities Capabilities;
        public byte Technology;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] Reserved;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public char[] Chemistry;

        public uint DesignedCapacity;
        public uint FullChargedCapacity;
        public uint DefaultAlert1;
        public uint DefaultAlert2;
        public uint CriticalBias;
        public uint CycleCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BATTERY_WAIT_STATUS
    {
        public uint BatteryTag;
        public uint Timeout;
        public uint PowerState;
        public uint LowCapacity;
        public uint HighCapacity;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BATTERY_STATUS
    {
        public uint PowerState;
        public uint Capacity;
        public uint Voltage;
        public int Rate;
    }

    [Flags]
    internal enum NVME_DIRECTION : uint
    {
        NVME_FROM_HOST_TO_DEV = 1,
        NVME_FROM_DEV_TO_HOST = 2,
        NVME_BI_DIRECTION = NVME_FROM_DEV_TO_HOST | NVME_FROM_HOST_TO_DEV
    }

    internal enum NVME_LOG_PAGES
    {
        NVME_LOG_PAGE_ERROR_INFO = 0x01,
        NVME_LOG_PAGE_HEALTH_INFO = 0x02,
        NVME_LOG_PAGE_FIRMWARE_SLOT_INFO = 0x03,
        NVME_LOG_PAGE_CHANGED_NAMESPACE_LIST = 0x04,
        NVME_LOG_PAGE_COMMAND_EFFECTS = 0x05,
        NVME_LOG_PAGE_DEVICE_SELF_TEST = 0x06,
        NVME_LOG_PAGE_TELEMETRY_HOST_INITIATED = 0x07,
        NVME_LOG_PAGE_TELEMETRY_CTLR_INITIATED = 0x08,
        NVME_LOG_PAGE_RESERVATION_NOTIFICATION = 0x80,
        NVME_LOG_PAGE_SANITIZE_STATUS = 0x81
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

    internal enum STORAGE_BUS_TYPE
    {
        BusTypeUnknown = 0x00,
        BusTypeScsi,
        BusTypeAtapi,
        BusTypeAta,
        BusType1394,
        BusTypeSsa,
        BusTypeFibre,
        BusTypeUsb,
        BusTypeRAID,
        BusTypeiScsi,
        BusTypeSas,
        BusTypeSata,
        BusTypeSd,
        BusTypeMmc,
        BusTypeVirtual,
        BusTypeFileBackedVirtual,
        BusTypeSpaces,
        BusTypeNvme,
        BusTypeSCM,
        BusTypeMax,
        BusTypeMaxReserved = 0x7F
    }

    internal enum STORAGE_PROPERTY_ID
    {
        StorageDeviceProperty = 0,
        StorageAdapterProperty,
        StorageDeviceIdProperty,
        StorageDeviceUniqueIdProperty,
        StorageDeviceWriteCacheProperty,
        StorageMiniportProperty,
        StorageAccessAlignmentProperty,
        StorageDeviceSeekPenaltyProperty,
        StorageDeviceTrimProperty,
        StorageDeviceWriteAggregationProperty,
        StorageDeviceDeviceTelemetryProperty,
        StorageDeviceLBProvisioningProperty,
        StorageDevicePowerProperty,
        StorageDeviceCopyOffloadProperty,
        StorageDeviceResiliencyProperty,
        StorageDeviceMediumProductType,
        StorageAdapterRpmbProperty,
        StorageDeviceIoCapabilityProperty = 48,
        StorageAdapterProtocolSpecificProperty,
        StorageDeviceProtocolSpecificProperty,
        StorageAdapterTemperatureProperty,
        StorageDeviceTemperatureProperty,
        StorageAdapterPhysicalTopologyProperty,
        StorageDevicePhysicalTopologyProperty,
        StorageDeviceAttributesProperty,
        StorageDeviceManagementStatus,
        StorageAdapterSerialNumberProperty,
        StorageDeviceLocationProperty
    }

    internal enum STORAGE_PROTOCOL_NVME_DATA_TYPE
    {
        NVMeDataTypeUnknown = 0,
        NVMeDataTypeIdentify,
        NVMeDataTypeLogPage,
        NVMeDataTypeFeature
    }

    internal enum STORAGE_PROTOCOL_NVME_PROTOCOL_DATA_REQUEST_VALUE
    {
        NVMeIdentifyCnsSpecificNamespace = 0,
        NVMeIdentifyCnsController = 1,
        NVMeIdentifyCnsActiveNamespaces = 2
    }

    internal enum STORAGE_PROTOCOL_TYPE
    {
        ProtocolTypeUnknown = 0x00,
        ProtocolTypeScsi,
        ProtocolTypeAta,
        ProtocolTypeNvme,
        ProtocolTypeSd,
        ProtocolTypeProprietary = 0x7E,
        ProtocolTypeMaxReserved = 0x7F
    }

    internal enum STORAGE_QUERY_TYPE
    {
        PropertyStandardQuery = 0,
        PropertyExistsQuery,
        PropertyMaskQuery,
        PropertyQueryMaxDefined
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
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
    internal struct SENDCMDINPARAMS
    {
        public uint cBufferSize;
        public IDEREGS irDriveRegs;
        public byte bDriveNumber;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] bReserved;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public uint[] dwReserved;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] bBuffer;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct IDEREGS
    {
        public SMART_FEATURES bFeaturesReg;
        public byte bSectorCountReg;
        public byte bSectorNumberReg;
        public byte bCylLowReg;
        public byte bCylHighReg;
        public byte bDriveHeadReg;
        public ATA_COMMAND bCommandReg;
        public byte bReserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct DRIVERSTATUS
    {
        public byte bDriverError;
        public byte bIDEError;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] Reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SENDCMDOUTPARAMS
    {
        public uint cBufferSize;
        public DRIVERSTATUS DriverStatus;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] bBuffer;
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
    internal struct IDENTIFY_DATA
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
        public IDENTIFY_DATA Identify;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct STORAGE_PROPERTY_QUERY
    {
        public STORAGE_PROPERTY_ID PropertyId;
        public STORAGE_QUERY_TYPE QueryType;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] AdditionalParameters;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct STORAGE_DEVICE_DESCRIPTOR_HEADER
    {
        public uint Version;
        public uint Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct STORAGE_DEVICE_DESCRIPTOR
    {
        public uint Version;
        public uint Size;
        public byte DeviceType;
        public byte DeviceTypeModifier;

        [MarshalAs(UnmanagedType.U1)]
        public bool RemovableMedia;

        [MarshalAs(UnmanagedType.U1)]
        public bool CommandQueueing;

        public uint VendorIdOffset;
        public uint ProductIdOffset;
        public uint ProductRevisionOffset;
        public uint SerialNumberOffset;
        public STORAGE_BUS_TYPE BusType;
        public uint RawPropertiesLength;
    }
    
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DISK_PERFORMANCE
    {
        /// <nodoc />
        public ulong BytesRead;

        /// <nodoc />
        public ulong BytesWritten;

        /// <nodoc />
        public ulong ReadTime;

        /// <nodoc />
        public ulong WriteTime;

        /// <nodoc />
        public ulong IdleTime;

        /// <nodoc />
        public uint ReadCount;

        /// <nodoc />
        public uint WriteCount;

        /// <nodoc />
        public uint QueueDepth;

        /// <nodoc />
        public uint SplitCount;

        /// <nodoc />
        public ulong QueryTime;

        /// <nodoc />
        public int StorageDeviceNumber;

        /// <nodoc />
        public long StorageManagerName0;

        /// <nodoc />
        public long StorageManagerName1;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SRB_IO_CONTROL
    {
        public uint HeaderLenght;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Signature;

        public uint Timeout;
        public uint ControlCode;
        public uint ReturnCode;
        public uint Length;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NVME_PASS_THROUGH_IOCTL
    {
        public SRB_IO_CONTROL srb;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public uint[] VendorSpecific;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public uint[] NVMeCmd;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public uint[] CplEntry;

        public NVME_DIRECTION Direction;
        public uint QueueId;
        public uint DataBufferLen;
        public uint MetaDataLen;
        public uint ReturnBufferLen;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4096)]
        public byte[] DataBuffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SCSI_PASS_THROUGH
    {
        [MarshalAs(UnmanagedType.U2)]
        public ushort Length;

        public byte ScsiStatus;
        public byte PathId;
        public byte TargetId;
        public byte Lun;
        public byte CdbLength;
        public byte SenseInfoLength;
        public byte DataIn;
        public uint DataTransferLength;
        public uint TimeOutValue;
        public IntPtr DataBufferOffset;
        public uint SenseInfoOffset;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] Cdb;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SCSI_PASS_THROUGH_WITH_BUFFERS
    {
        public SCSI_PASS_THROUGH Spt;

        public uint Filler;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] SenseBuf;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4096)]
        public byte[] DataBuf;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NVME_POWER_STATE_DESC
    {
        /// <summary>
        /// bit 0:15 Maximum  Power (MP) in centiwatts
        /// </summary>
        public ushort MP;

        /// <summary>
        /// bit 16:23
        /// </summary>
        public byte Reserved0;

        /// <summary>
        /// bit 24 Max Power Scale (MPS), bit 25 Non-Operational State (NOPS)
        /// </summary>
        public byte MPS_NOPS;

        /// <summary>
        /// bit 32:63 Entry Latency (ENLAT) in microseconds
        /// </summary>
        public uint ENLAT;

        /// <summary>
        /// bit 64:95 Exit Latency (EXLAT) in microseconds
        /// </summary>
        public uint EXLAT;

        /// <summary>
        /// bit 96:100 Relative Read Throughput (RRT)
        /// </summary>
        public byte RRT;

        /// <summary>
        /// bit 104:108 Relative Read Latency (RRL)
        /// </summary>
        public byte RRL;

        /// <summary>
        /// bit 112:116 Relative Write Throughput (RWT)
        /// </summary>
        public byte RWT;

        /// <summary>
        /// bit 120:124 Relative Write Latency (RWL)
        /// </summary>
        public byte RWL;

        /// <summary>
        /// bit 128:143 Idle Power (IDLP)
        /// </summary>
        public ushort IDLP;

        /// <summary>
        /// bit 150:151 Idle Power Scale (IPS)
        /// </summary>
        public byte IPS;

        /// <summary>
        /// bit 152:159
        /// </summary>
        public byte Reserved7;

        /// <summary>
        /// bit 160:175 Active Power (ACTP)
        /// </summary>
        public ushort ACTP;

        /// <summary>
        /// bit 176:178 Active Power Workload (APW), bit 182:183  Active Power Scale (APS)
        /// </summary>
        public byte APW_APS;

        /// <summary>
        /// bit 184:255.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public byte[] Reserved9;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NVME_IDENTIFY_CONTROLLER_DATA
    {
        /// <summary>
        /// byte 0:1 M - PCI Vendor ID (VID)
        /// </summary>
        public ushort VID;

        /// <summary>
        /// byte 2:3 M - PCI Subsystem Vendor ID (SSVID)
        /// </summary>
        public ushort SSVID;

        /// <summary>
        /// byte 4: 23 M - Serial Number (SN)
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] SN;

        /// <summary>
        /// byte 24:63 M - Model Number (MN)
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public byte[] MN;

        /// <summary>
        /// byte 64:71 M - Firmware Revision (FR)
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] FR;

        /// <summary>
        /// byte 72 M - Recommended Arbitration Burst (RAB)
        /// </summary>
        public byte RAB;

        /// <summary>
        /// byte 73:75 M - IEEE OUI Identifier (IEEE). Controller Vendor code.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] IEEE;

        /// <summary>
        /// byte 76 O - Controller Multi-Path I/O and Namespace Sharing Capabilities (CMIC)
        /// </summary>
        public byte CMIC;

        /// <summary>
        /// byte 77 M - Maximum Data Transfer Size (MDTS)
        /// </summary>
        public byte MDTS;

        /// <summary>
        /// byte 78:79 M - Controller ID (CNTLID)
        /// </summary>
        public ushort CNTLID;

        /// <summary>
        /// byte 80:83 M - Version (VER)
        /// </summary>
        public uint VER;

        /// <summary>
        /// byte 84:87 M - RTD3 Resume Latency (RTD3R)
        /// </summary>
        public uint RTD3R;

        /// <summary>
        /// byte 88:91 M - RTD3 Entry Latency (RTD3E)
        /// </summary>
        public uint RTD3E;

        /// <summary>
        /// byte 92:95 M - Optional Asynchronous Events Supported (OAES)
        /// </summary>
        public uint OAES;

        /// <summary>
        /// byte 96:239.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 144)]
        public byte[] Reserved0;

        /// <summary>
        /// byte 240:255.  Refer to the NVMe Management Interface Specification for definition.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] ReservedForManagement;

        /// <summary>
        /// byte 256:257 M - Optional Admin Command Support (OACS)
        /// </summary>
        public ushort OACS;

        /// <summary>
        /// byte 258 M - Abort Command Limit (ACL)
        /// </summary>
        public byte ACL;

        /// <summary>
        /// byte 259 M - Asynchronous Event Request Limit (AERL)
        /// </summary>
        public byte AERL;

        /// <summary>
        /// byte 260 M - Firmware Updates (FRMW)
        /// </summary>
        public byte FRMW;

        /// <summary>
        /// byte 261 M - Log Page Attributes (LPA)
        /// </summary>
        public byte LPA;

        /// <summary>
        /// byte 262 M - Error Log Page Entries (ELPE)
        /// </summary>
        public byte ELPE;

        /// <summary>
        /// byte 263 M - Number of Power States Support (NPSS)
        /// </summary>
        public byte NPSS;

        /// <summary>
        /// byte 264 M - Admin Vendor Specific Command Configuration (AVSCC)
        /// </summary>
        public byte AVSCC;

        /// <summary>
        /// byte 265 O - Autonomous Power State Transition Attributes (APSTA)
        /// </summary>
        public byte APSTA;

        /// <summary>
        /// byte 266:267 M - Warning Composite Temperature Threshold (WCTEMP)
        /// </summary>
        public ushort WCTEMP;

        /// <summary>
        /// byte 268:269 M - Critical Composite Temperature Threshold (CCTEMP)
        /// </summary>
        public ushort CCTEMP;

        /// <summary>
        /// byte 270:271 O - Maximum Time for Firmware Activation (MTFA)
        /// </summary>
        public ushort MTFA;

        /// <summary>
        /// byte 272:275 O - Host Memory Buffer Preferred Size (HMPRE)
        /// </summary>
        public uint HMPRE;

        /// <summary>
        /// byte 276:279 O - Host Memory Buffer Minimum Size (HMMIN)
        /// </summary>
        public uint HMMIN;

        /// <summary>
        /// byte 280:295 O - Total NVM Capacity (TNVMCAP)
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] TNVMCAP;

        /// <summary>
        /// byte 296:311 O - Unallocated NVM Capacity (UNVMCAP)
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] UNVMCAP;

        /// <summary>
        /// byte 312:315 O - Replay Protected Memory Block Support (RPMBS)
        /// </summary>
        public uint RPMBS;

        /// <summary>
        /// byte 316:511
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 196)]
        public byte[] Reserved1;

        /// <summary>
        /// byte 512 M - Submission Queue Entry Size (SQES)
        /// </summary>
        public byte SQES;

        /// <summary>
        /// byte 513 M - Completion Queue Entry Size (CQES)
        /// </summary>
        public byte CQES;

        /// <summary>
        /// byte 514:515
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] Reserved2;

        /// <summary>
        /// byte 516:519 M - Number of Namespaces (NN)
        /// </summary>
        public uint NN;

        /// <summary>
        /// byte 520:521 M - Optional NVM Command Support (ONCS)
        /// </summary>
        public ushort ONCS;

        /// <summary>
        /// byte 522:523 M - Fused Operation Support (FUSES)
        /// </summary>
        public ushort FUSES;

        /// <summary>
        /// byte 524 M - Format NVM Attributes (FNA)
        /// </summary>
        public byte FNA;

        /// <summary>
        /// byte 525 M - Volatile Write Cache (VWC)
        /// </summary>
        public byte VWC;

        /// <summary>
        /// byte 526:527 M - Atomic Write Unit Normal (AWUN)
        /// </summary>
        public ushort AWUN;

        /// <summary>
        /// byte 528:529 M - Atomic Write Unit Power Fail (AWUPF)
        /// </summary>
        public ushort AWUPF;

        /// <summary>
        /// byte 530 M - NVM Vendor Specific Command Configuration (NVSCC)
        /// </summary>
        public byte NVSCC;

        /// <summary>
        /// byte 531
        /// </summary>
        public byte Reserved3;

        /// <summary>
        /// byte 532:533 O - Atomic Compare and Write Unit (ACWU)
        /// </summary>
        public ushort ACWU;

        /// <summary>
        /// byte 534:535
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] Reserved4;

        /// <summary>
        /// byte 536:539 O - SGL Support (SGLS)
        /// </summary>
        public uint SGLS;

        /// <summary>
        /// byte 540:703
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 164)]
        public byte[] Reserved5;

        /// <summary>
        /// byte 704:2047
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1344)]
        public byte[] Reserved6;

        /// <summary>
        /// byte 2048:3071 Power State Descriptors
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public NVME_POWER_STATE_DESC[] PDS;

        /// <summary>
        /// byte 3072:4095 Vendor Specific
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
        public byte[] VS;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NVME_HEALTH_INFO_LOG
    {
        /// <summary>
        /// This field indicates critical warnings for the state of the  controller.
        /// Each bit corresponds to a critical warning type; multiple bits may be set.
        /// </summary>
        public byte CriticalWarning;

        /// <summary>
        /// Composite Temperature:  Contains the temperature of the overall device (controller and NVM included) in units of Kelvin.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] CompositeTemp;

        /// <summary>
        /// Available Spare:  Contains a normalized percentage (0 to 100%) of the remaining spare capacity available
        /// </summary>
        public byte AvailableSpare;

        /// <summary>
        /// Available Spare Threshold:  When the Available Spare falls below the threshold indicated in this field,
        /// an asynchronous event completion may occur. The value is indicated as a normalized percentage (0 to 100%).
        /// </summary>
        public byte AvailableSpareThreshold;

        /// <summary>
        /// Percentage Used:  Contains a vendor specific estimate of the percentage of NVM subsystem life used based on
        /// the actual usage and the manufacturer’s prediction of NVM life. A value of 100 indicates that the estimated endurance of
        /// the NVM in the NVM subsystem has been consumed, but may not indicate an NVM subsystem failure. The value is allowed to exceed 100.
        /// </summary>
        public byte PercentageUsed;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 26)]
        public byte[] Reserved1;

        /// <summary>
        /// Data Units Read:  Contains the number of 512 byte data units the host has read from the controller;
        /// this value does not include metadata. This value is reported in thousands
        /// (i.e., a value of 1 corresponds to 1000 units of 512 bytes read) and is rounded up.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] DataUnitRead;

        /// <summary>
        /// Data Units Written:  Contains the number of 512 byte data units the host has written to the controller;
        /// this value does not include metadata. This value is reported in thousands
        /// (i.e., a value of 1 corresponds to 1000 units of 512 bytes written) and is rounded up.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] DataUnitWritten;

        /// <summary>
        /// Host Read Commands:  Contains the number of read commands completed by the controller.
        /// For the NVM command set, this is the number of Compare and Read commands.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] HostReadCommands;

        /// <summary>
        /// Host Write Commands:  Contains the number of write commands completed by the controller.
        /// For the NVM command set, this is the number of Write commands.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] HostWriteCommands;

        /// <summary>
        /// Controller Busy Time:  Contains the amount of time the controller is busy with I/O commands.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] ControllerBusyTime;

        /// <summary>
        /// Power Cycles:  Contains the number of power cycles.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] PowerCycles;

        /// <summary>
        /// Power On Hours:  Contains the number of power-on hours.
        /// This does not include time that the controller was powered and in a low power state condition.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] PowerOnHours;

        /// <summary>
        /// Unsafe Shutdowns:  Contains the number of unsafe shutdowns.
        /// This count is incremented when a shutdown notification is not received prior to loss of power.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] UnsafeShutdowns;

        /// <summary>
        /// Media Errors:  Contains the number of occurrences where the controller detected an unrecoverable data integrity error.
        /// Errors such as uncorrectable ECC, CRC checksum failure, or LBA tag mismatch are included in this field.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] MediaAndDataIntegrityErrors;

        /// <summary>
        /// Number of Error Information Log Entries:  Contains the number of Error Information log entries over the life of the controller
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] NumberErrorInformationLogEntries;

        /// <summary>
        /// Warning Composite Temperature Time:  Contains the amount of time in minutes that the controller is operational and the Composite Temperature is greater than or equal to the Warning Composite
        /// Temperature Threshold.
        /// </summary>
        public uint WarningCompositeTemperatureTime;

        /// <summary>
        /// Critical Composite Temperature Time:  Contains the amount of time in minutes that the controller is operational and the Composite Temperature is greater than the Critical Composite Temperature
        /// Threshold.
        /// </summary>
        public uint CriticalCompositeTemperatureTime;

        /// <summary>
        /// Contains the current temperature reported by temperature sensor 1-8.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public ushort[] TemperatureSensor;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 296)]
        internal byte[] Reserved2;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct STORAGE_PROTOCOL_SPECIFIC_DATA
    {
        public STORAGE_PROTOCOL_TYPE ProtocolType;
        public uint DataType;
        public uint ProtocolDataRequestValue;
        public uint ProtocolDataRequestSubValue;
        public uint ProtocolDataOffset;
        public uint ProtocolDataLength;
        public uint FixedProtocolReturnData;
        public uint ProtocolDataRequestSubValue2;
        public uint ProtocolDataRequestSubValue3;
        public uint Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct STORAGE_QUERY_BUFFER
    {
        public STORAGE_PROPERTY_ID PropertyId;
        public STORAGE_QUERY_TYPE QueryType;
        public STORAGE_PROTOCOL_SPECIFIC_DATA ProtocolSpecific;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4096)]
        internal byte[] Buffer;
    }

    [Flags]
    internal enum MEM : uint
    {
        MEM_COMMIT = 0x1000,
        MEM_RESERVE = 0x2000,
        MEM_DECOMMIT = 0x4000,
        MEM_RELEASE = 0x8000,
        MEM_RESET = 0x80000,
        MEM_LARGE_PAGES = 0x20000000,
        MEM_PHYSICAL = 0x400000,
        MEM_TOP_DOWN = 0x100000,
        MEM_WRITE_WATCH = 0x200000
    }

    [Flags]
    internal enum PAGE : uint
    {
        PAGE_EXECUTE = 0x10,
        PAGE_EXECUTE_READ = 0x20,
        PAGE_EXECUTE_READWRITE = 0x40,
        PAGE_EXECUTE_WRITECOPY = 0x80,
        PAGE_NOACCESS = 0x01,
        PAGE_READONLY = 0x02,
        PAGE_READWRITE = 0x04,
        PAGE_WRITECOPY = 0x08,
        PAGE_GUARD = 0x100,
        PAGE_NOCACHE = 0x200,
        PAGE_WRITECOMBINE = 0x400
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct IOControlCode
    {
        /// <summary>
        /// Gets the resulting IO control code.
        /// </summary>
        public uint Code { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IOControlCode" /> struct.
        /// </summary>
        /// <param name="deviceType">Type of the device.</param>
        /// <param name="function">The function.</param>
        /// <param name="access">The access.</param>
        public IOControlCode(uint deviceType, uint function, Access access) : this(deviceType, function, Method.Buffered, access)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="IOControlCode" /> struct.
        /// </summary>
        /// <param name="deviceType">Type of the device.</param>
        /// <param name="function">The function.</param>
        /// <param name="method">The method.</param>
        /// <param name="access">The access.</param>
        public IOControlCode(uint deviceType, uint function, Method method, Access access)
        {
            Code = (deviceType << 16) | ((uint)access << 14) | (function << 2) | (uint)method;
        }

        public enum Method : uint
        {
            Buffered = 0,
            InDirect = 1,
            OutDirect = 2,
            Neither = 3
        }

        public enum Access : uint
        {
            Any = 0,
            Read = 1,
            Write = 2
        }
    }

    internal enum Provider
    {
        ACPI = (byte)'A' << 24 | (byte)'C' << 16 | (byte)'P' << 8 | (byte)'I',
        FIRM = (byte)'F' << 24 | (byte)'I' << 16 | (byte)'R' << 8 | (byte)'M',
        RSMB = (byte)'R' << 24 | (byte)'S' << 16 | (byte)'M' << 8 | (byte)'B'
    }
}
