/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2017 Alexander Thulcke <alexth4ef9@gmail.com>
  
*/

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace OpenHardwareMonitor.Hardware.HDD {

  internal abstract class NVMeNamespaceInfo {
    public ulong Size { get; protected set; }
    public ulong Capacity { get; protected set; }
    public ulong Utilization { get; protected set; }
    public uint LBADataSize { get; protected set; }
    public byte[] RawData { get; protected set; }
  }
  
  internal abstract class NVMeInfo {
    public int Index { get; protected set; }
    public ushort VID { get; protected set; }
    public ushort SSVID { get; protected set; }
    public string Serial { get; protected set; }
    public string Model { get; protected set; }
    public string Revision { get; protected set; }
    public byte[] IEEE { get; protected set; }
    public ulong TotalCapacity { get; protected set; }
    public ulong UnallocatedCapacity { get; protected set; }
    public ushort ControllerId { get; protected set; }
    public uint NumberNamespaces { get; protected set; }
    public NVMeNamespaceInfo Namespace1 { get; protected set; }
    public byte[] RawData { get; protected set; }
  }
  
  [Flags]
  internal enum NVMeCriticalWarning {
    None = 0x00,
    AvailableSpaceLow = 0x01, // If set to 1, then the available spare space has fallen below the threshold.
    TemperatureThreshold = 0x02, // If set to 1, then a temperature is above an over temperature threshold or below an under temperature threshold.
    ReliabilityDegraded = 0x04, // If set to 1, then the device reliability has been degraded due to significant media related errors or any internal error that degrades device reliability.
    ReadOnly = 0x08, // If set to 1, then the media has been placed in read only mode
    VolatileMemoryBackupDeviceFailed = 0x10, // If set to 1, then the volatile memory backup device has failed. This field is only valid if the controller has a volatile memory backup solution.
  }

  internal abstract class NVMeHealthInfo {
    public NVMeCriticalWarning CriticalWarning { get; protected set; }
    public short Temperature { get; protected set; }
    public byte AvailableSpare { get; protected set; }
    public byte AvailableSpareThreshold { get; protected set; }
    public byte PercentageUsed { get; protected set; }
    public ulong DataUnitRead { get; protected set; }
    public ulong DataUnitWritten { get; protected set; }
    public ulong HostReadCommands { get; protected set; }
    public ulong HostWriteCommands { get; protected set; }
    public ulong ControllerBusyTime { get; protected set; }
    public ulong PowerCycle { get; protected set; }
    public ulong PowerOnHours { get; protected set; }
    public ulong UnsafeShutdowns { get; protected set; }
    public ulong MediaErrors { get; protected set; }
    public ulong ErrorInfoLogEntryCount { get; protected set; }
    public uint WarningCompositeTemperatureTime { get; protected set; }
    public uint CriticalCompositeTemperatureTime { get; protected set; }
    public short[] TemperatureSensors { get; protected set; }
    public byte[] RawData { get; protected set; }
  }
  
  internal class WindowsNVMeSmart : IDisposable {
    private enum Command : uint {
      IoctlScsiMiniPort = 0x04d008,
    }
     
    private const string NVMeMiniPortSignature = "NvmeMini";
    private const uint NVMePassThroughSrbIoCode = 0xe0002000;

    private enum NVMePassThroughOpcode : uint {
      AdminGetLogPage = 0x02,
      AdminIdentify = 0x06,
    }
    
    [Flags]
    private enum NVMePassThroughDirection : uint {
      None = 0,
      Out = 1,
      In = 2,
      InOut = In | Out,
    }
    
    private enum NVMePassThroughQueue : uint {
      AdminQ = 0,
    }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct SrbIoControl {
      public uint HeaderLenght;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
      public byte[] Signature;
      public uint Timeout;
      public uint ControlCode;
      public uint ReturnCode;
      public uint Length;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct NVMePassThrough {
      #region SRB_IO_CONTROL
      public uint HeaderLenght;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
      public byte[] Signature;
      public uint Timeout;
      public uint ControlCode;
      public uint ReturnCode;
      public uint Length;
      #endregion
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
      //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
      //public byte[] DataBuffer;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct NVMePowerStateDesc {
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
    
    [StructLayout(LayoutKind.Sequential)]
    private struct NVMeIdentifyControllerData {
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
        
    [StructLayout(LayoutKind.Sequential)]
    private struct NVMeLBAFormat {
      public ushort ms; // bit 0:15 Metadata Size (MS)
      public byte lbads; // bit 16:23 LBA  Data  Size (LBADS)
      public byte rp; // bit 24:25 Relative Performance (RP)
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NVMeIdentifyNamespaceData {
      public ulong nsze; // byte 0:7 M - Namespace Size (NSZE)
      public ulong ncap; // byte 8:15 M - Namespace Capacity (NCAP)
      public ulong nuse; // byte 16:23 M - Namespace Utilization (NUSE)
      public byte nsfeat; // byte 24 M - Namespace Features (NSFEAT)
      public byte nlbaf; // byte 25 M - Number of LBA Formats (NLBAF)
      public byte flbas; // byte 26 M - Formatted LBA Size (FLBAS)
      public byte mc; // byte 27 M - Metadata Capabilities (MC)
      public byte dpc; // byte 28 M - End-to-end Data Protection Capabilities (DPC)
      public byte dps; // byte 29 M - End-to-end Data Protection Type Settings (DPS)
      public byte nmic; // byte 30 O - Namespace Multi-path I/O and Namespace Sharing Capabilities (NMIC)
      public byte rescap; // byte 31 O - Reservation Capabilities (RESCAP)
      public byte fpi; // byte 32 O - Format Progress Indicator (FPI)
      public byte Reserved0; // byte 33
      public ushort nawun; // byte 34:35 O - Namespace Atomic Write Unit Normal (NAWUN)
      public ushort nawupf;// byte 36:37 O - Namespace Atomic Write Unit Power Fail (NAWUPF)
      public ushort nacwu; // byte 38:39 O - Namespace Atomic Compare & Write Unit (NACWU)
      public ushort nabsn; // byte 40:41 O - Namespace Atomic Boundary Size Normal (NABSN)
      public ushort nabo; // byte 42:43 O - Namespace Atomic Boundary Offset (NABO)
      public ushort nabspf; // byte 44:45 O - Namespace Atomic Boundary Size Power Fail (NABSPF)
      public ushort Reserved1; // byte 46:47
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[] nvmcap; // byte 48:63 O - NVM Capacity (NVMCAP)
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
      public byte[] Reserved2; // byte 64:103
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public byte[] nguid; // byte 104:119 O - Namespace Globally Unique Identifier (NGUID)
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
      public byte[] eui64; // byte 120:127 M - IEEE Extended Unique Identifier (EUI64)
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
      public NVMeLBAFormat[] lbaf; // byte 128:131 M - LBA Format 0 Support (LBAF0), byte 132:191 O - LBA Format 1-15 Support (LBAF1-LBAF15)
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 192)]
      public byte[] Reserved3;     // byte 192:383
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3712)]
      public byte[] vs; // byte 384:4095 O - Vendor Specific (VS): This range of bytes is allocated for vendor specific usage.
    }
    
    private class NVMeNamespaceInfoImpl : NVMeNamespaceInfo {
      public NVMeNamespaceInfoImpl(NVMeIdentifyNamespaceData data, byte[] rawData) {
        byte shift = data.lbaf[data.flbas & 0x0f].lbads;
        Size = ShiftValue(data.nsze, shift);
        Capacity = ShiftValue(data.ncap, shift);
        Utilization = ShiftValue(data.nuse, shift);
        LBADataSize = 1u << shift;
        RawData = rawData;
      }
    }
    
    private class NVMeInfoImpl : NVMeInfo {
      public NVMeInfoImpl(int index, NVMeIdentifyControllerData data, byte[] rawData) {
        Index = index;
        VID = data.vid;
        SSVID = data.ssvid;
        Serial = GetString(data.sn);
        Model = GetString(data.mn);
        Revision = GetString(data.fr);
        IEEE = data.ieee;
        TotalCapacity = BitConverter.ToUInt64(data.tnvmcap, 0); // 128bit little endian
        UnallocatedCapacity = BitConverter.ToUInt64(data.unvmcap, 0);
        ControllerId = data.cntlid;
        NumberNamespaces = data.nn;
        RawData = rawData;
      }
    
      public NVMeInfoImpl(int index, NVMeIdentifyControllerData data, byte[] rawData,
        NVMeIdentifyNamespaceData namespaceData, byte[] namespaceRawData)
        : this(index, data, rawData)
      {
        Namespace1 = new NVMeNamespaceInfoImpl(namespaceData, namespaceRawData);
      }
    }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct NVMeHealthInfoLog {
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
    };
      
    private class NVMeHealthInfoImpl : NVMeHealthInfo {
      public NVMeHealthInfoImpl(NVMeHealthInfoLog log, byte[] rawData) {
        CriticalWarning = (NVMeCriticalWarning)log.CriticalWarning;
        Temperature = KelvinToCelsius(log.CompositeTemperature);
        AvailableSpare = log.AvailableSpare;
        AvailableSpareThreshold = log.AvailableSpareThreshold;
        PercentageUsed = log.PercentageUsed;
        DataUnitRead = BitConverter.ToUInt64(log.DataUnitRead, 0);
        DataUnitWritten = BitConverter.ToUInt64(log.DataUnitWritten, 0);
        HostReadCommands = BitConverter.ToUInt64(log.HostReadCommands, 0);
        HostWriteCommands = BitConverter.ToUInt64(log.HostWriteCommands, 0);
        ControllerBusyTime = BitConverter.ToUInt64(log.ControllerBusyTime, 0);
        PowerCycle = BitConverter.ToUInt64(log.PowerCycle, 0);
        PowerOnHours = BitConverter.ToUInt64(log.PowerOnHours, 0);
        UnsafeShutdowns = BitConverter.ToUInt64(log.UnsafeShutdowns, 0);
        MediaErrors = BitConverter.ToUInt64(log.MediaErrors, 0);
        ErrorInfoLogEntryCount = BitConverter.ToUInt64(log.ErrorInfoLogEntryCount, 0);
        WarningCompositeTemperatureTime = log.WarningCompositeTemperatureTime;
        CriticalCompositeTemperatureTime = log.CriticalCompositeTemperatureTime;
        TemperatureSensors = new short[log.TemperatureSensors.Length];
        for(int i=0; i<TemperatureSensors.Length; i++)
          TemperatureSensors[i] = KelvinToCelsius(log.TemperatureSensors[i]);
        RawData = rawData;
      }
    }
    
    private static string GetString(byte[] s) {
      return Encoding.ASCII.GetString(s).Trim('\t', '\n', '\r', ' ', '\0');
    }
    
    // should use BigInteger from .NET 4.0, 128-bit integers
    private static ulong ShiftValue(ulong v, byte s) {
      ulong low = v << s;
      //ulong high = v >> (64 - s);
      return low;
    }
    
    private static short KelvinToCelsius(ushort k) {
      return (short)((k > 0) ? (int)k - 273 : short.MinValue);
    }

    private static short KelvinToCelsius(byte[] k) {
      return KelvinToCelsius(BitConverter.ToUInt16(k, 0));
    }
             
    private readonly SafeHandle handle;
    private int driveNumber;
    
#if DEBUG
    static WindowsNVMeSmart() {
      Debug.Assert(Marshal.SizeOf(typeof(NVMeIdentifyControllerData)) == 4096);
      Debug.Assert(Marshal.SizeOf(typeof(NVMeLBAFormat)) == 4);
      Debug.Assert(Marshal.SizeOf(typeof(NVMeIdentifyNamespaceData)) == 4096);
      Debug.Assert(Marshal.SizeOf(typeof(NVMeHealthInfoLog)) == 512);
    }
#endif
    
    public WindowsNVMeSmart(int driveNumber) {
      this.driveNumber = driveNumber;
      handle = NativeMethods.CreateFile(string.Format(@"\\.\Scsi{0}:", driveNumber), FileAccess.ReadWrite,
        FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
    }

    public bool IsValid {
      get { return !handle.IsInvalid; }
    }
    
    public void Close() {
      Dispose(true);
      GC.SuppressFinalize(this);
    }
    
    public NVMeInfo GetInfo() {
      if (handle.IsClosed)
        throw new ObjectDisposedException("WindowsNVMeSmart");
      try {
        byte[] rawData;
        NVMeIdentifyControllerData data = ReadPassThrough<NVMeIdentifyControllerData>(NVMePassThroughOpcode.AdminIdentify, 0x00000000, 0x000000001, out rawData);
        if (data.nn == 1) {
          byte[] rawDataNamespace;
          NVMeIdentifyNamespaceData nspace = ReadPassThrough<NVMeIdentifyNamespaceData>(NVMePassThroughOpcode.AdminIdentify, 0x000000001, 0x00000000, out rawDataNamespace);
          return new NVMeInfoImpl(driveNumber, data, rawData, nspace, rawDataNamespace);
        }
        return new NVMeInfoImpl(driveNumber, data, rawData);
      } catch(Win32Exception) {
      }
      return null;
    }
    
    public NVMeHealthInfo GetHealthInfo() {
      if (handle.IsClosed)
        throw new ObjectDisposedException("WindowsNVMeSmart");
      try {
        uint size = (uint)Marshal.SizeOf(typeof(NVMeHealthInfoLog));
        uint cdw10 = 0x000000002 | (((size / 4) - 1) << 16);
        byte[] rawData;
        NVMeHealthInfoLog data = ReadPassThrough<NVMeHealthInfoLog>(NVMePassThroughOpcode.AdminGetLogPage, 0xffffffff, cdw10, out rawData);
        return new NVMeHealthInfoImpl(data, rawData);
      } catch(Win32Exception) {
      }
      return null;
    }
    
    private T ReadPassThrough<T>(NVMePassThroughOpcode opcode, uint nsid, uint cdw10, out byte[] rawData) {
      NVMePassThroughDirection direction = (NVMePassThroughDirection)((uint)opcode & 0x00000003);
      if ((direction & NVMePassThroughDirection.Out) != 0)
          throw new ArgumentOutOfRangeException("opcode");
            
      int size = Marshal.SizeOf(typeof(NVMePassThrough)) + Marshal.SizeOf(typeof(T));
      NVMePassThrough passThrough = new NVMePassThrough();
      passThrough.HeaderLenght = (uint)Marshal.SizeOf(typeof(SrbIoControl));
      passThrough.Signature = Encoding.ASCII.GetBytes(NVMeMiniPortSignature);
      passThrough.Timeout = 60;
      passThrough.ControlCode = NVMePassThroughSrbIoCode;
      passThrough.ReturnCode = 0;
      passThrough.Length = (uint)(size - Marshal.SizeOf(typeof(SrbIoControl)));
      
      passThrough.NVMeCmd = new uint[16];
      passThrough.NVMeCmd[0] = (uint)opcode;
      passThrough.NVMeCmd[1] = nsid;
      passThrough.NVMeCmd[10] = cdw10;
      passThrough.Direction = direction;
      passThrough.Queue = NVMePassThroughQueue.AdminQ;
      passThrough.DataBufferLen = 0;
      passThrough.MetaDataLen = 0;
      passThrough.ReturnBufferLen = (uint)size;
      
      IntPtr buffer = Marshal.AllocHGlobal(size);
      try {
        Marshal.StructureToPtr(passThrough, buffer, false);
        
        uint bytesReturned = 0;
        if (!NativeMethods.DeviceIoControl(handle, Command.IoctlScsiMiniPort,
            buffer, size, buffer, size, out bytesReturned, IntPtr.Zero))
          throw new Win32Exception();
        rawData = new byte[Marshal.SizeOf(typeof(T))];
        IntPtr dataBuffer = new IntPtr(buffer.ToInt64() + Marshal.SizeOf(typeof(NVMePassThrough)));
        Marshal.Copy(dataBuffer, rawData, 0, rawData.Length);
        return (T)Marshal.PtrToStructure(dataBuffer, typeof(T));
      }
      finally {
        Marshal.FreeHGlobal(buffer);
      }
    }
          
    #region IDisposable implementation
    public void Dispose() {
      Close();
    }
    #endregion
    
    protected void Dispose(bool disposing) {
      if (disposing) {
        if (!handle.IsClosed)
          handle.Close();
      }
    }
    
    private static class NativeMethods {
      private const string KERNEL = "kernel32.dll";

      [DllImport(KERNEL, CallingConvention = CallingConvention.Winapi,
        CharSet = CharSet.Auto, SetLastError = true)]
      public static extern SafeFileHandle CreateFile(
       [MarshalAs(UnmanagedType.LPTStr)] string filename,
       [MarshalAs(UnmanagedType.U4)] FileAccess access,
       [MarshalAs(UnmanagedType.U4)] FileShare share,
       IntPtr securityAttributes,
       [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
       [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
       IntPtr templateFile);

      [DllImport(KERNEL, CallingConvention = CallingConvention.Winapi,
        CharSet = CharSet.Auto, SetLastError = true)]
      [return: MarshalAsAttribute(UnmanagedType.Bool)]
      public static extern bool DeviceIoControl(SafeHandle handle,
        Command command, IntPtr dataIn, int dataInSize,
        IntPtr dataOut, int dataOutSize, out uint bytesReturned,
        IntPtr overlapped);
    }
  }
}
