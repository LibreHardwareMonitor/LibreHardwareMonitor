// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

// ReSharper disable InconsistentNaming

namespace OpenHardwareMonitor.Interop {
  public class Kernel32 {
    internal const string IntelNVMeMiniPortSignature1 = "NvmeMini";
    internal const string IntelNVMeMiniPortSignature2 = "IntelNvm";
    internal const int MAX_DRIVE_ATTRIBUTES = 512;
    internal const uint NVMePassThroughSrbIoCode = 0xe0002000;
    internal const int SCSI_PASS_THROUGH_BUFFER_SIZE = 512;
    internal const byte SMART_LBA_HI = 0xC2;
    internal const byte SMART_LBA_MID = 0x4F;

    private const string DllName = "kernel32.dll";

    internal enum DFP : uint {
      DFP_GET_VERSION = 0x00074080,
      DFP_SEND_DRIVE_COMMAND = 0x0007c084,
      DFP_RECEIVE_DRIVE_DATA = 0x0007c088
    }

    internal enum IOCTL : uint {
      IOCTL_SCSI_PASS_THROUGH = 0x04d004,
      IOCTL_SCSI_MINIPORT = 0x04d008,
      IOCTL_SCSI_PASS_THROUGH_DIRECT = 0x04d014,
      IOCTL_SCSI_GET_ADDRESS = 0x41018,
      IOCTL_STORAGE_QUERY_PROPERTY = 0x2D1400
    }

    [Flags]
    public enum NVME_CRITICAL_WARNING {
      None = 0x00,
      AvailableSpaceLow = 0x01, // If set to 1, then the available spare space has fallen below the threshold.
      TemperatureThreshold = 0x02, // If set to 1, then a temperature is above an over temperature threshold or below an under temperature threshold.
      ReliabilityDegraded = 0x04, // If set to 1, then the device reliability has been degraded due to significant media related errors or any internal error that degrades device reliability.
      ReadOnly = 0x08, // If set to 1, then the media has been placed in read only mode
      VolatileMemoryBackupDeviceFailed = 0x10 // If set to 1, then the volatile memory backup device has failed. This field is only valid if the controller has a volatile memory backup solution.
    }

    [Flags]
    internal enum NVME_DIRECTION : uint {
      NVME_FROM_HOST_TO_DEV = 1,
      NVME_FROM_DEV_TO_HOST = 2,
      NVME_BI_DIRECTION = NVME_FROM_DEV_TO_HOST | NVME_FROM_HOST_TO_DEV
    }

    internal enum NVME_LOG_PAGES {
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

    internal enum ATA_COMMAND : byte {
      /// <summary>
      /// SMART data requested.
      /// </summary>
      ATA_SMART = 0xB0,

      /// <summary>
      /// Identify data is requested.
      /// </summary>
      ATA_IDENTIFY_DEVICE = 0xEC
    }

    internal enum SCSI_IOCTL_DATA {
      SCSI_IOCTL_DATA_OUT = 0,
      SCSI_IOCTL_DATA_IN = 1,
      SCSI_IOCTL_DATA_UNSPECIFIED = 2
    }

    internal enum SMART_FEATURES : byte {
      /// <summary>
      /// Read SMART data.
      /// </summary>
      SMART_READ_DATA = 0xD0,

      /// <summary>
      /// Read SMART thresholds.
      /// </summary>
      READ_THRESHOLDS = 0xD1, /* obsolete */

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
      /// </summary>
      WRITE_THRESHOLDS = 0xD7, /* obsolete */

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

    internal enum STORAGE_BUS_TYPE {
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

    internal enum STORAGE_PROPERTY_ID {
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

    internal enum STORAGE_PROTOCOL_NVME_DATA_TYPE {
      NVMeDataTypeUnknown = 0,
      NVMeDataTypeIdentify,
      NVMeDataTypeLogPage,
      NVMeDataTypeFeature
    }

    internal enum STORAGE_PROTOCOL_TYPE {
      ProtocolTypeUnknown = 0x00,
      ProtocolTypeScsi,
      ProtocolTypeAta,
      ProtocolTypeNvme,
      ProtocolTypeSd,
      ProtocolTypeProprietary = 0x7E,
      ProtocolTypeMaxReserved = 0x7F
    }

    internal enum STORAGE_QUERY_TYPE {
      PropertyStandardQuery = 0,
      PropertyExistsQuery,
      PropertyMaskQuery,
      PropertyQueryMaxDefined
    }

    /// <summary>
    /// Create a instance from a struct with zero initialized memory arrays
    /// no need to init every inner array with the correct sizes
    /// </summary>
    /// <typeparam name="T">type of struct that is needed</typeparam>
    /// <returns></returns>
    internal static T CreateStruct<T>() {
      int size = Marshal.SizeOf<T>();
      var ptr = Marshal.AllocHGlobal(size);
      RtlZeroMemory(ptr, size);
      var result = Marshal.PtrToStructure<T>(ptr);
      Marshal.FreeHGlobal(ptr);
      return result;
    }

    internal static SafeHandle OpenDevice(string devicePath) {
      SafeHandle hDevice = CreateFile(devicePath, FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
      if (hDevice.IsInvalid || hDevice.IsClosed)
        hDevice = null;

      return hDevice;
    }

    [DllImport(DllName, CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
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

    [DllImport(DllName,
      CallingConvention = CallingConvention.Winapi,
      CharSet = CharSet.Auto,
      SetLastError = true)]
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

    [DllImport(DllName, SetLastError = true)]
    internal static extern void RtlZeroMemory(IntPtr Destination, int Length);

    [DllImport(DllName, EntryPoint = "CopyMemory", SetLastError = false)]
    internal static extern void CopyMemory(IntPtr Destination, IntPtr Source, uint Length);

    [DllImport(DllName, SetLastError = true)]
    internal static extern IntPtr LoadLibrary(string lpFileName);

    [StructLayout(LayoutKind.Sequential)]
    internal struct MEMORYSTATUSEX {
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
    public struct SMART_ATTRIBUTE {
      public byte Id;
      public short Flags;
      public byte CurrentValue;
      public byte WorstValue;

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
      public byte[] RawValue;

      public byte Reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SMART_THRESHOLD {
      public byte Id;
      public byte Threshold;

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
      public byte[] Reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SENDCMDINPARAMS {
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
    internal struct IDEREGS {
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
    internal struct DRIVERSTATUS {
      public byte bDriverError;
      public byte bIDEError;

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
      public byte[] Reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SENDCMDOUTPARAMS {
      public uint cBufferSize;
      public DRIVERSTATUS DriverStatus;

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
      public byte[] bBuffer;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct ATTRIBUTECMDOUTPARAMS {
      public uint cBufferSize;
      public DRIVERSTATUS DriverStatus;
      public byte Version;
      public byte Reserved;

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_DRIVE_ATTRIBUTES)]
      public SMART_ATTRIBUTE[] Attributes;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct THRESHOLDCMDOUTPARAMS {
      public uint cBufferSize;
      public DRIVERSTATUS DriverStatus;
      public byte Version;
      public byte Reserved;

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_DRIVE_ATTRIBUTES)]
      public SMART_THRESHOLD[] Thresholds;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct IDENTIFY_DATA {
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
    internal struct IDENTIFYCMDOUTPARAMS {
      public uint cBufferSize;
      public DRIVERSTATUS DriverStatus;
      public IDENTIFY_DATA Identify;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct STORAGE_PROPERTY_QUERY {
      public STORAGE_PROPERTY_ID PropertyId;
      public STORAGE_QUERY_TYPE QueryType;

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
      public byte[] AdditionalParameters;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct STORAGE_DEVICE_DESCRIPTOR_HEADER {
      public uint Version;
      public uint Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct STORAGE_DEVICE_DESCRIPTOR {
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

    [StructLayout(LayoutKind.Sequential)]
    internal struct SRB_IO_CONTROL {
      public uint HeaderLenght;

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
      public byte[] Signature;

      public uint Timeout;
      public uint ControlCode;
      public uint ReturnCode;
      public uint Length;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NVME_PASS_THROUGH_IOCTL {
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
    internal struct SCSI_PASS_THROUGH {
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
    internal struct SCSI_PASS_THROUGH_WITH_BUFFERS {
      public SCSI_PASS_THROUGH Spt;

      public uint Filler;

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
      public byte[] SenseBuf;

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = SCSI_PASS_THROUGH_BUFFER_SIZE)]
      public byte[] DataBuf;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NVME_POWER_STATE_DESC {
      public ushort MP; // bit 0:15 Maximum  Power (MP) in centiwatts
      public byte Reserved0; // bit 16:23
      public byte MPS_NOPS; // bit 24 Max Power Scale (MPS), bit 25 Non-Operational State (NOPS)
      public uint ENLAT; // bit 32:63 Entry Latency (ENLAT) in microseconds
      public uint EXLAT; // bit 64:95 Exit Latency (EXLAT) in microseconds
      public byte RRT; // bit 96:100 Relative Read Throughput (RRT)
      public byte RRL; // bit 104:108 Relative Read Latency (RRL)
      public byte RWT; // bit 112:116 Relative Write Throughput (RWT)
      public byte RWL; // bit 120:124 Relative Write Latency (RWL)
      public ushort IDLP; // bit 128:143 Idle Power (IDLP)
      public byte IPS; // bit 150:151 Idle Power Scale (IPS)
      public byte Reserved7; // bit 152:159
      public ushort ACTP; // bit 160:175 Active Power (ACTP)
      public byte APW_APS; // bit 176:178 Active Power Workload (APW), bit 182:183  Active Power Scale (APS)

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
      public byte[] Reserved9; // bit 184:255.
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NVME_IDENTIFY_CONTROLLER_DATA {
      public ushort VID; // byte 0:1 M - PCI Vendor ID (VID)
      public ushort SSVID; // byte 2:3 M - PCI Subsystem Vendor ID (SSVID)

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
      public byte[] SN; // byte 4: 23 M - Serial Number (SN)

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
      public byte[] MN; // byte 24:63 M - Model Number (MN)

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
      public byte[] FR; // byte 64:71 M - Firmware Revision (FR)

      public byte RAB; // byte 72 M - Recommended Arbitration Burst (RAB)

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
      public byte[] IEEE; // byte 73:75 M - IEEE OUI Identifier (IEEE). Controller Vendor code.

      public byte CMIC; // byte 76 O - Controller Multi-Path I/O and Namespace Sharing Capabilities (CMIC)
      public byte MDTS; // byte 77 M - Maximum Data Transfer Size (MDTS)
      public ushort CNTLID; // byte 78:79 M - Controller ID (CNTLID)
      public uint VER; // byte 80:83 M - Version (VER)
      public uint RTD3R; // byte 84:87 M - RTD3 Resume Latency (RTD3R)
      public uint RTD3E; // byte 88:91 M - RTD3 Entry Latency (RTD3E)
      public uint OAES; // byte 92:95 M - Optional Asynchronous Events Supported (OAES)

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 144)]
      public byte[] Reserved0; // byte 96:239.

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[] ReservedForManagement; // byte 240:255.  Refer to the NVMe Management Interface Specification for definition.

      public ushort OACS; // byte 256:257 M - Optional Admin Command Support (OACS)
      public byte ACL; // byte 258 M - Abort Command Limit (ACL)
      public byte AERL; // byte 259 M - Asynchronous Event Request Limit (AERL)
      public byte FRMW; // byte 260 M - Firmware Updates (FRMW)
      public byte LPA; // byte 261 M - Log Page Attributes (LPA)
      public byte ELPE; // byte 262 M - Error Log Page Entries (ELPE)
      public byte NPSS; // byte 263 M - Number of Power States Support (NPSS)
      public byte AVSCC; // byte 264 M - Admin Vendor Specific Command Configuration (AVSCC)
      public byte APSTA; // byte 265 O - Autonomous Power State Transition Attributes (APSTA)
      public ushort WCTEMP; // byte 266:267 M - Warning Composite Temperature Threshold (WCTEMP)
      public ushort CCTEMP; // byte 268:269 M - Critical Composite Temperature Threshold (CCTEMP)
      public ushort MTFA; // byte 270:271 O - Maximum Time for Firmware Activation (MTFA)
      public uint HMPRE; // byte 272:275 O - Host Memory Buffer Preferred Size (HMPRE)
      public uint HMMIN; // byte 276:279 O - Host Memory Buffer Minimum Size (HMMIN)

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[] TNVMCAP; // byte 280:295 O - Total NVM Capacity (TNVMCAP)

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[] UNVMCAP; // byte 296:311 O - Unallocated NVM Capacity (UNVMCAP)

      public uint RPMBS; // byte 312:315 O - Replay Protected Memory Block Support (RPMBS)

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 196)]
      public byte[] Reserved1; // byte 316:511

      public byte SQES; // byte 512 M - Submission Queue Entry Size (SQES)
      public byte CQES; // byte 513 M - Completion Queue Entry Size (CQES)

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
      public byte[] Reserved2; // byte 514:515

      public uint NN; // byte 516:519 M - Number of Namespaces (NN)
      public ushort ONCS; // byte 520:521 M - Optional NVM Command Support (ONCS)
      public ushort FUSES; // byte 522:523 M - Fused Operation Support (FUSES)
      public byte FNA; // byte 524 M - Format NVM Attributes (FNA)
      public byte VWC; // byte 525 M - Volatile Write Cache (VWC)
      public ushort AWUN; // byte 526:527 M - Atomic Write Unit Normal (AWUN)
      public ushort AWUPF; // byte 528:529 M - Atomic Write Unit Power Fail (AWUPF)
      public byte NVSCC; // byte 530 M - NVM Vendor Specific Command Configuration (NVSCC)
      public byte Reserved3; // byte 531
      public ushort ACWU; // byte 532:533 O - Atomic Compare & Write Unit (ACWU)

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
      public byte[] Reserved4; // byte 534:535

      public uint SGLS; // byte 536:539 O - SGL Support (SGLS)

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 164)]
      public byte[] Reserved5; // byte 540:703

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1344)]
      public byte[] Reserved6; // byte 704:2047

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
      public NVME_POWER_STATE_DESC[] PDS; // byte 2048:3071 Power State Descriptors

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
      public byte[] VS; // byte 3072:4095 Vendor Specific
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NVME_HEALTH_INFO_LOG {
      public byte CriticalWarning; // This field indicates critical warnings for the state of the  controller. Each bit corresponds to a critical warning type; multiple bits may be set.

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
      public byte[] CompositeTemp; // Composite Temperature:  Contains the temperature of the overall device (controller and NVM included) in units of Kelvin.

      public byte AvailableSpare; // Available Spare:  Contains a normalized percentage (0 to 100%) of the remaining spare capacity available

      public byte
        AvailableSpareThreshold; // Available Spare Threshold:  When the Available Spare falls below the threshold indicated in this field, an asynchronous event completion may occur. The value is indicated as a normalized percentage (0 to 100%).

      public byte
        PercentageUsed; // Percentage Used:  Contains a vendor specific estimate of the percentage of NVM subsystem life used based on the actual usage and the manufacturer’s prediction of NVM life. A value of 100 indicates that the estimated endurance of the NVM in the NVM subsystem has been consumed, but may not indicate an NVM subsystem failure. The value is allowed to exceed 100.

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 26)]
      public byte[] Reserved1;

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[]
        DataUnitRead; // Data Units Read:  Contains the number of 512 byte data units the host has read from the controller; this value does not include metadata. This value is reported in thousands (i.e., a value of 1 corresponds to 1000 units of 512 bytes read) and is rounded up.

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[]
        DataUnitWritten; // Data Units Written:  Contains the number of 512 byte data units the host has written to the controller; this value does not include metadata. This value is reported in thousands (i.e., a value of 1 corresponds to 1000 units of 512 bytes written) and is rounded up.

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[]
        HostReadCommands; // Host Read Commands:  Contains the number of read commands completed by the controller. For the NVM command set, this is the number of Compare and Read commands.

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[] HostWriteCommands; // Host Write Commands:  Contains the number of write commands completed by the controller. For the NVM command set, this is the number of Write commands.

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[] ControllerBusyTime; // Controller Busy Time:  Contains the amount of time the controller is busy with I/O commands.

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[] PowerCycles; // Power Cycles:  Contains the number of power cycles.

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[] PowerOnHours; // Power On Hours:  Contains the number of power-on hours. This does not include time that the controller was powered and in a low power state condition.

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[] UnsafeShutdowns; // Unsafe Shutdowns:  Contains the number of unsafe shutdowns. This count is incremented when a shutdown notification is not received prior to loss of power.

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[]
        MediaAndDataIntegrityErrors; // Media Errors:  Contains the number of occurrences where the controller detected an unrecoverable data integrity error. Errors such as uncorrectable ECC, CRC checksum failure, or LBA tag mismatch are included in this field.

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[] NumberErrorInformationLogEntries; // Number of Error Information Log Entries:  Contains the number of Error Information log entries over the life of the controller

      public uint
        WarningCompositeTemperatureTime; // Warning Composite Temperature Time:  Contains the amount of time in minutes that the controller is operational and the Composite Temperature is greater than or equal to the Warning Composite Temperature Threshold.

      public uint
        CriticalCompositeTemperatureTime; // Critical Composite Temperature Time:  Contains the amount of time in minutes that the controller is operational and the Composite Temperature is greater than the Critical Composite Temperature Threshold.

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
      public ushort[] TemperatureSensor; // Contains the current temperature reported by temperature sensor 1-8.

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 296)]
      internal byte[] Reserved2;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct STORAGE_PROTOCOL_SPECIFIC_DATA {
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
    internal struct STORAGE_QUERY_BUFFER {
      public STORAGE_PROPERTY_ID PropertyId;
      public STORAGE_QUERY_TYPE QueryType;
      public STORAGE_PROTOCOL_SPECIFIC_DATA ProtocolSpecific;

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4096)]
      internal byte[] Buffer;
    }
  }
}