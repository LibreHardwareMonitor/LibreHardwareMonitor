// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) 2011-2012 Michael Möller <mmoeller@openhardwaremonitor.org>
// Copyright (C) 2017 Alexander Thulcke <alexth4ef9@gmail.com>
// Copyright (C) 2016-2019 Sebastian Grams <https://github.com/sebastian-dev>
// Copyright (C) 2016-2019 Aqua Computer <https://github.com/aquacomputer, info@aqua-computer.de>

using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace OpenHardwareMonitor.Hardware.HDD {
  public class Interop {

    public const string KERNEL = "kernel32.dll";
    public const byte SMART_LBA_MID = 0x4F;
    public const byte SMART_LBA_HI = 0xC2;
    public const int MAX_DRIVE_ATTRIBUTES = 512;
    public const string NVMeMiniPortSignature = "NvmeMini";
    public const uint NVMePassThroughSrbIoCode = 0xe0002000;

    public const int IDENTIFY_BUFFER_SIZE = 512;
    public const int SCSI_IOCTL_DATA_OUT = 0;
    public const int SCSI_IOCTL_DATA_IN = 1;
    public const int SCSI_IOCTL_DATA_UNSPECIFIED = 2;

    #region DriveAttributeValue
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DriveAttributeValue {
      public byte Identifier;
      public short StatusFlags;
      public byte AttrValue;
      public byte WorstValue;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
      public byte[] RawValue;
      public byte Reserved;
    }
    #endregion

    #region DriveThresholdValue
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DriveThresholdValue {
      public byte Identifier;
      public byte Threshold;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
      public byte[] Unknown;
    }
    #endregion

    #region DriveCommand
    public enum DriveCommand : uint {
      GetVersion = 0x00074080,
      SendDriveCommand = 0x0007c084,
      ReceiveDriveData = 0x0007c088
    }
    #endregion

    #region RegisterCommand
    public enum RegisterCommand : byte {
      /// <summary>
      /// SMART data requested.
      /// </summary>
      SmartCmd = 0xB0,

      /// <summary>
      /// Identify data is requested.
      /// </summary>
      IdCmd = 0xEC,
    }
    #endregion

    #region RegisterFeature
    public enum RegisterFeature : byte {
      /// <summary>
      /// Read SMART data.
      /// </summary>
      SmartReadData = 0xD0,

      /// <summary>
      /// Read SMART thresholds.
      /// </summary>
      SmartReadThresholds = 0xD1, /* obsolete */

      /// <summary>
      /// Autosave SMART data.
      /// </summary>
      SmartAutosave = 0xD2,

      /// <summary>
      /// Save SMART attributes.
      /// </summary>
      SmartSaveAttr = 0xD3,

      /// <summary>
      /// Set SMART to offline immediately.
      /// </summary>
      SmartImmediateOffline = 0xD4,

      /// <summary>
      /// Read SMART log.
      /// </summary>
      SmartReadLog = 0xD5,

      /// <summary>
      /// Write SMART log.
      /// </summary>
      SmartWriteLog = 0xD6,

      /// <summary>
      /// Write SMART thresholds.
      /// </summary>
      SmartWriteThresholds = 0xD7, /* obsolete */

      /// <summary>
      /// Enable SMART.
      /// </summary>
      SmartEnableOperations = 0xD8,

      /// <summary>
      /// Disable SMART.
      /// </summary>
      SmartDisableOperations = 0xD9,

      /// <summary>
      /// Get SMART status.
      /// </summary>
      SmartStatus = 0xDA,

      /// <summary>
      /// Set SMART to offline automatically.
      /// </summary>
      SmartAutoOffline = 0xDB, /* obsolete */
    }
    #endregion

    #region CommandBlockRegisters
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CommandBlockRegisters {
      public RegisterFeature Features;
      public byte SectorCount;
      public byte LBALow;
      public byte LBAMid;
      public byte LBAHigh;
      public byte Device;
      public RegisterCommand Command;
      public byte Reserved;
    }
    #endregion

    #region DriveCommandParameter
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DriveCommandParameter {
      public uint BufferSize;
      public CommandBlockRegisters Registers;
      public byte DriveNumber;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
      public byte[] Reserved;
    }
    #endregion

    #region DriverStatus
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DriverStatus {
      public byte DriverError;
      public byte IDEError;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
      public byte[] Reserved;
    }
    #endregion

    #region DriveCommandResult
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DriveCommandResult {
      public uint BufferSize;
      public DriverStatus DriverStatus;
    }
    #endregion

    #region DriveSmartReadDataResult
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DriveSmartReadDataResult {
      public uint BufferSize;
      public DriverStatus DriverStatus;
      public byte Version;
      public byte Reserved;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_DRIVE_ATTRIBUTES)]
      public Interop.DriveAttributeValue[] Attributes;
    }
    #endregion

    #region DriveSmartReadThresholdsResult
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DriveSmartReadThresholdsResult {
      public uint BufferSize;
      public DriverStatus DriverStatus;
      public byte Version;
      public byte Reserved;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_DRIVE_ATTRIBUTES)]
      public Interop.DriveThresholdValue[] Thresholds;
    }
    #endregion

    #region Identify
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Identify {
      public ushort GeneralConfiguration;
      public ushort NumberOfCylinders;
      public ushort Reserved;
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
      public ushort MoreVendorUnique;
      public ushort DoubleWordIo;
      public ushort Capabilities;
      public ushort MoreReserved;
      public ushort PioCycleTimingMode;
      public ushort DmaCycleTimingMode;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 406)]
      public byte[] More;
    }
    #endregion

    #region DriveIdentifyResult
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DriveIdentifyResult {
      public uint BufferSize;
      public DriverStatus DriverStatus;
      public Identify Identify;
    }
    #endregion

    #region SCSI_ADDRESS
    [StructLayout(LayoutKind.Sequential)]
    public struct SCSI_ADDRESS {
      public uint Length;
      public byte PortNumber;
      public byte PathId;
      public byte TargetId;
      public byte Lun;
    }
    #endregion

    #region StorageBusType
    public enum StorageBusType {
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
      BusTypeMaxReserved = 0x7F,
    }
    #endregion

    #region StorageCommand
    public enum StorageCommand : uint {
      QueryProperty = 0x002d1400,
    }
    #endregion

    #region StorageQueryType
    public enum StorageQueryType {
      PropertyStandardQuery = 0,
      PropertyExistsQuery,
      PropertyMaskQuery,
      PropertyQueryMaxDefined,
    }
    #endregion

    #region StoragePropertyId
    public enum StoragePropertyId {
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
      StorageDeviceLocationProperty,
    }
    #endregion

    #region StoragePropertyQuery
    [StructLayout(LayoutKind.Sequential)]
    public struct StoragePropertyQuery {
      public StoragePropertyId PropertyId;
      public StorageQueryType QueryType;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x1)]
      public byte[] AdditionalParameters;
    }
    #endregion

    #region StoragePropertyQuery1
    [StructLayout(LayoutKind.Sequential)]
    public struct StoragePropertyQuery1 {
      public StoragePropertyId PropertyId;
      public StorageQueryType QueryType;
    }
    #endregion

    #region StorageDescriptorHeader
    [StructLayout(LayoutKind.Sequential)]
    public struct StorageDescriptorHeader {
      public uint Version;
      public uint Size;
    }
    #endregion

    #region StorageDeviceDescriptor
    [StructLayout(LayoutKind.Sequential)]
    public struct StorageDeviceDescriptor {
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
      public StorageBusType BusType;
      public uint RawPropertiesLength;
    }
    #endregion

    #region NVMeCriticalWarning
    [Flags]
    public enum NVMeCriticalWarning {
      None = 0x00,
      AvailableSpaceLow = 0x01, // If set to 1, then the available spare space has fallen below the threshold.
      TemperatureThreshold = 0x02, // If set to 1, then a temperature is above an over temperature threshold or below an under temperature threshold.
      ReliabilityDegraded = 0x04, // If set to 1, then the device reliability has been degraded due to significant media related errors or any internal error that degrades device reliability.
      ReadOnly = 0x08, // If set to 1, then the media has been placed in read only mode
      VolatileMemoryBackupDeviceFailed = 0x10, // If set to 1, then the volatile memory backup device has failed. This field is only valid if the controller has a volatile memory backup solution.
    }
    #endregion

    #region Command
    public enum Command : uint {
      IOCTL_SCSI_PASS_THROUGH = 0x04d004,
      IOCTL_SCSI_MINIPORT = 0x04d008,
      IOCTL_SCSI_PASS_THROUGH_DIRECT = 0x04d014,
      IOCTL_SCSI_GET_ADDRESS = 0x41018,
      IOCTL_STORAGE_QUERY_PROPERTY = 0x2D1400,
    }
    #endregion

    #region NVMePassThroughOpcode
    public enum NVMePassThroughOpcode : uint {
      AdminGetLogPage = 0x02,
      AdminIdentify = 0x06,
    }
    #endregion

    #region NVMePassThroughDirection
    [Flags]
    public enum NVMePassThroughDirection : uint {
      None = 0,
      Out = 1,
      In = 2,
      InOut = In | Out,
    }
    #endregion

    #region NVMePassThroughQueue
    public enum NVMePassThroughQueue : uint {
      AdminQ = 0,
    }
    #endregion

    #region SrbIoControl
    [StructLayout(LayoutKind.Sequential)]
    public struct SrbIoControl {
      public uint HeaderLenght;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
      public byte[] Signature;
      public uint Timeout;
      public uint ControlCode;
      public uint ReturnCode;
      public uint Length;
    }
    #endregion

    #region NVMePassThrough
    [StructLayout(LayoutKind.Sequential)]
    public struct NVMePassThrough {
      public SrbIoControl srb;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
      public uint[] VendorSpecific;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public uint[] NVMeCmd;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
      public uint[] CplEntry;
      public NVMePassThroughDirection Direction;
      public NVMePassThroughQueue Queue;
      public uint DataBufferLen;
      public uint MetaDataLen;
      public uint ReturnBufferLen;
    }
    #endregion

    #region SCSI_PASS_THROUGH
    /// <summary>
    /// https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/content/ntddscsi/ns-ntddscsi-_scsi_pass_through
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SCSI_PASS_THROUGH {
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
    #endregion

    #region SPTWith512Buffer
    [StructLayout(LayoutKind.Sequential)]
    public struct SPTWith512Buffer {
      public SCSI_PASS_THROUGH Spt;

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
      public byte[] SenseBuf;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
      public byte[] DataBuf;
    }
    #endregion

    #region NVMePowerStateDesc
    [StructLayout(LayoutKind.Sequential)]
    public struct NVMePowerStateDesc {
      public ushort mp; // bit 0:15 Maximum  Power (MP) in centiwatts
      public byte Reserved0; // bit 16:23
      public byte mps_nops; // bit 24 Max Power Scale (MPS), bit 25 Non-Operational State (NOPS)
      public uint enlat; // bit 32:63 Entry Latency (ENLAT) in microseconds
      public uint exlat; // bit 64:95 Exit Latency (EXLAT) in microseconds
      public byte rrt; // bit 96:100 Relative Read Throughput (RRT)
      public byte rrl; // bit 104:108 Relative Read Latency (RRL)
      public byte rwt; // bit 112:116 Relative Write Throughput (RWT)
      public byte rwl; // bit 120:124 Relative Write Latency (RWL)
      public ushort idlp; // bit 128:143 Idle Power (IDLP)
      public byte ips; // bit 150:151 Idle Power Scale (IPS)
      public byte Reserved7; // bit 152:159
      public ushort actp; // bit 160:175 Active Power (ACTP)
      public byte apw_aps; // bit 176:178 Active Power Workload (APW), bit 182:183  Active Power Scale (APS)
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
      public byte[] Reserved9; // bit 184:255.
    }
    #endregion

    #region NVMeIdentifyControllerData
    [StructLayout(LayoutKind.Sequential)]
    public struct NVMeIdentifyControllerData {
      public ushort vid; // byte 0:1 M - PCI Vendor ID (VID)
      public ushort ssvid; // byte 2:3 M - PCI Subsystem Vendor ID (SSVID)
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
      public byte[] sn; // byte 4: 23 M - Serial Number (SN)
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
      public byte[] mn; // byte 24:63 M - Model Number (MN)
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
      public byte[] fr; // byte 64:71 M - Firmware Revision (FR)
      public byte rab; // byte 72 M - Recommended Arbitration Burst (RAB)
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
      public byte[] ieee; // byte 73:75 M - IEEE OUI Identifier (IEEE). Controller Vendor code.
      public byte cmic; // byte 76 O - Controller Multi-Path I/O and Namespace Sharing Capabilities (CMIC)
      public byte mdts; // byte 77 M - Maximum Data Transfer Size (MDTS)
      public ushort cntlid; // byte 78:79 M - Controller ID (CNTLID)
      public uint ver; // byte 80:83 M - Version (VER)
      public uint rtd3r; // byte 84:87 M - RTD3 Resume Latency (RTD3R)
      public uint rtd3e; // byte 88:91 M - RTD3 Entry Latency (RTD3E)
      public uint oaes; // byte 92:95 M - Optional Asynchronous Events Supported (OAES)
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 144)]
      public byte[] Reserved0; // byte 96:239.
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[] ReservedForManagement; // byte 240:255.  Refer to the NVMe Management Interface Specification for definition.
      public ushort oacs; // byte 256:257 M - Optional Admin Command Support (OACS)
      public byte acl; // byte 258 M - Abort Command Limit (ACL)
      public byte aerl; // byte 259 M - Asynchronous Event Request Limit (AERL)
      public byte frmw; // byte 260 M - Firmware Updates (FRMW)
      public byte lpa; // byte 261 M - Log Page Attributes (LPA)
      public byte elpe; // byte 262 M - Error Log Page Entries (ELPE)
      public byte npss; // byte 263 M - Number of Power States Support (NPSS)
      public byte avscc; // byte 264 M - Admin Vendor Specific Command Configuration (AVSCC)
      public byte apsta; // byte 265 O - Autonomous Power State Transition Attributes (APSTA)
      public ushort wctemp; // byte 266:267 M - Warning Composite Temperature Threshold (WCTEMP)
      public ushort cctemp; // byte 268:269 M - Critical Composite Temperature Threshold (CCTEMP)
      public ushort mtfa; // byte 270:271 O - Maximum Time for Firmware Activation (MTFA)
      public uint hmpre; // byte 272:275 O - Host Memory Buffer Preferred Size (HMPRE)
      public uint hmmin; // byte 276:279 O - Host Memory Buffer Minimum Size (HMMIN)
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[] tnvmcap; // byte 280:295 O - Total NVM Capacity (TNVMCAP)
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[] unvmcap; // byte 296:311 O - Unallocated NVM Capacity (UNVMCAP)
      public uint rpmbs; // byte 312:315 O - Replay Protected Memory Block Support (RPMBS)
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 196)]
      public byte[] Reserved1; // byte 316:511
      public byte sqes; // byte 512 M - Submission Queue Entry Size (SQES)
      public byte cqes; // byte 513 M - Completion Queue Entry Size (CQES)
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
      public byte[] Reserved2; // byte 514:515
      public uint nn; // byte 516:519 M - Number of Namespaces (NN)
      public ushort oncs; // byte 520:521 M - Optional NVM Command Support (ONCS)
      public ushort fuses; // byte 522:523 M - Fused Operation Support (FUSES)
      public byte fna; // byte 524 M - Format NVM Attributes (FNA)
      public byte vwc; // byte 525 M - Volatile Write Cache (VWC)
      public ushort awun; // byte 526:527 M - Atomic Write Unit Normal (AWUN)
      public ushort awupf; // byte 528:529 M - Atomic Write Unit Power Fail (AWUPF)
      public byte nvscc; // byte 530 M - NVM Vendor Specific Command Configuration (NVSCC)
      public byte Reserved3; // byte 531
      public ushort acwu; // byte 532:533 O - Atomic Compare & Write Unit (ACWU)
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
      public byte[] Reserved4; // byte 534:535
      public uint sgls; // byte 536:539 O - SGL Support (SGLS)
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 164)]
      public byte[] Reserved5; // byte 540:703
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1344)]
      public byte[] Reserved6; // byte 704:2047
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
      public NVMePowerStateDesc[] pds; // byte 2048:3071 Power State Descriptors
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
      public byte[] vs; // byte 3072:4095 Vendor Specific
    }
    #endregion

    #region NVMeLBAFormat
    [StructLayout(LayoutKind.Sequential)]
    public struct NVMeLBAFormat {
      public ushort ms; // bit 0:15 Metadata Size (MS)
      public byte lbads; // bit 16:23 LBA  Data  Size (LBADS)
      public byte rp; // bit 24:25 Relative Performance (RP)
    }
    #endregion

    #region NVMeHealthInfoLog
    [StructLayout(LayoutKind.Sequential)]
    public struct NVMeHealthInfoLog {
      public byte CriticalWarning; // This field indicates critical warnings for the state of the  controller. Each bit corresponds to a critical warning type; multiple bits may be set.
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
      public byte[] CompositeTemperature; // Composite Temperature:  Contains the temperature of the overall device (controller and NVM included) in units of Kelvin.
      public byte AvailableSpare; // Available Spare:  Contains a normalized percentage (0 to 100%) of the remaining spare capacity available
      public byte AvailableSpareThreshold; // Available Spare Threshold:  When the Available Spare falls below the threshold indicated in this field, an asynchronous event completion may occur. The value is indicated as a normalized percentage (0 to 100%).
      public byte PercentageUsed; // Percentage Used:  Contains a vendor specific estimate of the percentage of NVM subsystem life used based on the actual usage and the manufacturer’s prediction of NVM life. A value of 100 indicates that the estimated endurance of the NVM in the NVM subsystem has been consumed, but may not indicate an NVM subsystem failure. The value is allowed to exceed 100.
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 26)]
      public byte[] Reserved0;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[] DataUnitRead; // Data Units Read:  Contains the number of 512 byte data units the host has read from the controller; this value does not include metadata. This value is reported in thousands (i.e., a value of 1 corresponds to 1000 units of 512 bytes read) and is rounded up.
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[] DataUnitWritten; // Data Units Written:  Contains the number of 512 byte data units the host has written to the controller; this value does not include metadata. This value is reported in thousands (i.e., a value of 1 corresponds to 1000 units of 512 bytes written) and is rounded up.
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[] HostReadCommands; // Host Read Commands:  Contains the number of read commands completed by the controller. For the NVM command set, this is the number of Compare and Read commands.
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[] HostWriteCommands; // Host Write Commands:  Contains the number of write commands completed by the controller. For the NVM command set, this is the number of Write commands.
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[] ControllerBusyTime; // Controller Busy Time:  Contains the amount of time the controller is busy with I/O commands.
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[] PowerCycle; // Power Cycles:  Contains the number of power cycles.
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[] PowerOnHours; // Power On Hours:  Contains the number of power-on hours. This does not include time that the controller was powered and in a low power state condition.
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[] UnsafeShutdowns; // Unsafe Shutdowns:  Contains the number of unsafe shutdowns. This count is incremented when a shutdown notification is not received prior to loss of power.
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[] MediaErrors; // Media Errors:  Contains the number of occurrences where the controller detected an unrecovered data integrity error. Errors such as uncorrectable ECC, CRC checksum failure, or LBA tag mismatch are included in this field.
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[] ErrorInfoLogEntryCount; // Number of Error Information Log Entries:  Contains the number of Error Information log entries over the life of the controller
      public uint WarningCompositeTemperatureTime; // Warning Composite Temperature Time:  Contains the amount of time in minutes that the controller is operational and the Composite Temperature is greater than or equal to the Warning Composite Temperature Threshold.
      public uint CriticalCompositeTemperatureTime; // Critical Composite Temperature Time:  Contains the amount of time in minutes that the controller is operational and the Composite Temperature is greater than the Critical Composite Temperature Threshold.
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
      public ushort[] TemperatureSensors; // Contains the current temperature reported by temperature sensor 1-8.
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 296)]
      public byte[] Reserved1;
    }
    #endregion

    #region StorageProtocolNVMeDataType
    public enum StorageProtocolNVMeDataType {
      NVMeDataTypeUnknown = 0,
      NVMeDataTypeIdentify,
      NVMeDataTypeLogPage,
      NVMeDataTypeFeature,
    }
    #endregion

    #region TStroageProtocolType
    public enum TStroageProtocolType {
      ProtocolTypeUnknown = 0x00,
      ProtocolTypeScsi,
      ProtocolTypeAta,
      ProtocolTypeNvme,
      ProtocolTypeSd,
      ProtocolTypeProprietary = 0x7E,
      ProtocolTypeMaxReserved = 0x7F
    }
    #endregion

    #region NVME_LOG_PAGES
    public enum NVME_LOG_PAGES {
      NVME_LOG_PAGE_ERROR_INFO = 0x01,
      NVME_LOG_PAGE_HEALTH_INFO = 0x02,
      NVME_LOG_PAGE_FIRMWARE_SLOT_INFO = 0x03,
      NVME_LOG_PAGE_CHANGED_NAMESPACE_LIST = 0x04,
      NVME_LOG_PAGE_COMMAND_EFFECTS = 0x05,
      NVME_LOG_PAGE_DEVICE_SELF_TEST = 0x06,
      NVME_LOG_PAGE_TELEMETRY_HOST_INITIATED = 0x07,
      NVME_LOG_PAGE_TELEMETRY_CTLR_INITIATED = 0x08,
      NVME_LOG_PAGE_RESERVATION_NOTIFICATION = 0x80,
      NVME_LOG_PAGE_SANITIZE_STATUS = 0x81,
    }
    #endregion

    #region StorageProtocolSpecificData
    [StructLayout(LayoutKind.Sequential)]
    public struct StorageProtocolSpecificData {
      public TStroageProtocolType ProtocolType;
      public uint DataType;
      public uint ProtocolDataRequestValue;
      public uint ProtocolDataRequestSubValue;
      public uint ProtocolDataOffset;
      public uint ProtocolDataLength;
      public uint FixedProtocolReturnData;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
      public uint[] Reserved;
    }
    #endregion

    #region StorageQueryWithBuffer
    [StructLayout(LayoutKind.Sequential)]
    public struct StorageQueryWithBuffer {
      public StoragePropertyQuery1 Query;
      public StorageProtocolSpecificData ProtocolSpecific;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4096)]
      public byte[] Buffer;
    }
    #endregion


    #region PhysicalPathToSCSIPath
    public static string PhysicalPathToSCSIPath(SafeHandle pysicalDeviceHandle) {
      string result = string.Empty;
      uint bytesReturned = 0;
      int length = (int)Marshal.SizeOf(typeof(SCSI_ADDRESS));
      IntPtr pBuffer = Marshal.AllocHGlobal(length);
      bool valid = DeviceIoControl(pysicalDeviceHandle, Command.IOCTL_SCSI_GET_ADDRESS, IntPtr.Zero, 0, pBuffer, length, out bytesReturned, IntPtr.Zero);
      if (valid) {
        var item = Marshal.PtrToStructure<SCSI_ADDRESS>(pBuffer);
        result = string.Format(@"\\.\SCSI{0}:", item.PortNumber);
      }
      Marshal.FreeHGlobal(pBuffer);
      return result;
    }
    #endregion

    #region CreateStruct
    /// <summary>
    /// CreateStruct
    /// create a instance from a struct with zero initilized memory arrays
    /// no need to init every inner array with the correct sizes
    /// </summary>
    /// <typeparam name="T">type of struct that is needed</typeparam>
    /// <returns></returns>
    public static T CreateStruct<T>() {
      int size = Marshal.SizeOf<T>();
      var ptr = Marshal.AllocHGlobal(size);
      RtlZeroMemory(ptr, size);
      T result = Marshal.PtrToStructure<T>(ptr);
      Marshal.FreeHGlobal(ptr);
      return result;
    }
    #endregion

    #region OpenDevice
    public static SafeHandle OpenDevice(string deivcePath) {
      SafeHandle hDevice = null;
      hDevice = CreateFile(deivcePath, FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
      if (hDevice.IsInvalid || hDevice.IsClosed)
        hDevice = null;
      return hDevice;
    }
    #endregion

    #region WIN32

    [DllImport(KERNEL, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
    public static extern SafeFileHandle CreateFile(
      [MarshalAs(UnmanagedType.LPTStr)] string filename,
      [MarshalAs(UnmanagedType.U4)] FileAccess access,
      [MarshalAs(UnmanagedType.U4)] FileShare share,
      IntPtr securityAttributes,
      [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
      [MarshalAs(UnmanagedType.U4)]FileAttributes flagsAndAttributes,
      IntPtr templateFile);

    [DllImport(KERNEL, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAsAttribute(UnmanagedType.Bool)]
    public static extern bool DeviceIoControl(
      SafeHandle handle,
      DriveCommand command, ref DriveCommandParameter parameter,
      int parameterSize, out DriveSmartReadDataResult result, int resultSize,
      out uint bytesReturned, IntPtr overlapped);

    [DllImport(KERNEL, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAsAttribute(UnmanagedType.Bool)]
    public static extern bool DeviceIoControl(
      SafeHandle handle,
      DriveCommand command, ref DriveCommandParameter parameter,
      int parameterSize, out DriveSmartReadThresholdsResult result,
      int resultSize, out uint bytesReturned, IntPtr overlapped);

    [DllImport(KERNEL, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAsAttribute(UnmanagedType.Bool)]
    public static extern bool DeviceIoControl(
      SafeHandle handle,
      DriveCommand command, ref DriveCommandParameter parameter,
      int parameterSize, out DriveCommandResult result, int resultSize,
      out uint bytesReturned, IntPtr overlapped);

    [DllImport(KERNEL, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAsAttribute(UnmanagedType.Bool)]
    public static extern bool DeviceIoControl(
      SafeHandle handle,
      DriveCommand command, ref DriveCommandParameter parameter,
      int parameterSize, out DriveIdentifyResult result, int resultSize,
      out uint bytesReturned, IntPtr overlapped);


    [DllImport(KERNEL, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeviceIoControl(
        SafeHandle handle, StorageCommand command,
        ref StoragePropertyQuery query, int querySize,
        out StorageDescriptorHeader descriptorHeader,
        int descriptorHeaderSize,
        out uint bytesReturned, IntPtr overlapped);

    [DllImport(KERNEL, CallingConvention = CallingConvention.Winapi,
      CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeviceIoControl(
        SafeHandle handle, StorageCommand command,
        ref StoragePropertyQuery query, int querySize,
        IntPtr descriptor, uint descriptorSize,
        out uint bytesReturned, IntPtr overlapped);

    [DllImport(KERNEL, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAsAttribute(UnmanagedType.Bool)]
    public static extern bool DeviceIoControl(SafeHandle handle,
      Command command, IntPtr dataIn, int dataInSize,
      IntPtr dataOut, int dataOutSize, out uint bytesReturned,
      IntPtr overlapped);

    [DllImport(KERNEL, SetLastError = true)]
    public static extern void RtlZeroMemory(IntPtr dst, int length);

    [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
    public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

    [DllImport(KERNEL)]
    public static extern uint GetLastError();

    #endregion
  }
}