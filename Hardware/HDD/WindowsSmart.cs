/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2009-2012 Michael Möller <mmoeller@openhardwaremonitor.org>
	Copyright (C) 2010 Paul Werelds
  Copyright (C) 2011 Roland Reinl <roland-reinl@gmx.de>
	
*/

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace OpenHardwareMonitor.Hardware.HDD {

  internal class WindowsSmart : ISmart {
    protected enum DriveCommand : uint {
      GetVersion = 0x00074080,
      SendDriveCommand = 0x0007c084,
      ReceiveDriveData = 0x0007c088
    }

    protected enum RegisterCommand : byte {
      /// <summary>
      /// SMART data requested.
      /// </summary>
      SmartCmd = 0xB0,

      /// <summary>
      /// Identify data is requested.
      /// </summary>
      IdCmd = 0xEC,
    }

    protected enum RegisterFeature : byte {
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

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    protected struct CommandBlockRegisters {
      public RegisterFeature Features;         
      public byte SectorCount;      
      public byte LBALow;       
      public byte LBAMid;           
      public byte LBAHigh;        
      public byte Device;
      public RegisterCommand Command;           
      public byte Reserved;                  
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    protected struct DriveCommandParameter {
      public uint BufferSize;           
      public CommandBlockRegisters Registers;           
      public byte DriveNumber;   
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
      public byte[] Reserved;                                
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    protected struct DriverStatus {
      public byte DriverError;   
      public byte IDEError;             
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
      public byte[] Reserved;               
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    protected struct DriveCommandResult {
      public uint BufferSize;
      public DriverStatus DriverStatus;
    } 

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    protected struct DriveSmartReadDataResult {
      public uint BufferSize;           
      public DriverStatus DriverStatus;
      public byte Version;
      public byte Reserved;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_DRIVE_ATTRIBUTES)]
      public DriveAttributeValue[] Attributes;                                                                                       
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    protected struct DriveSmartReadThresholdsResult {
      public uint BufferSize;
      public DriverStatus DriverStatus;
      public byte Version;
      public byte Reserved;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_DRIVE_ATTRIBUTES)]
      public DriveThresholdValue[] Thresholds;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    protected struct Identify {
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

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    protected struct DriveIdentifyResult {
      public uint BufferSize;
      public DriverStatus DriverStatus;
      public Identify Identify;
    }

    private const byte SMART_LBA_MID = 0x4F;
    private const byte SMART_LBA_HI = 0xC2;

    private const int MAX_DRIVE_ATTRIBUTES = 512;

    private readonly SafeHandle handle;
    private int driveNumber;
    
    public WindowsSmart(int driveNumber) {
      this.driveNumber = driveNumber;
      handle = NativeMethods.CreateFile(@"\\.\PhysicalDrive" + driveNumber, FileAccess.ReadWrite,
        FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
    }
    
    public bool IsValid {
      get { return !handle.IsInvalid; }
    }
    
    public void Close() {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    public bool EnableSmart() {
      if (handle.IsClosed)
        throw new ObjectDisposedException("WindowsATASmart");
      
      DriveCommandParameter parameter = new DriveCommandParameter();
      DriveCommandResult result;
      uint bytesReturned;

      parameter.DriveNumber = (byte)driveNumber;
      parameter.Registers.Features = RegisterFeature.SmartEnableOperations;
      parameter.Registers.LBAMid = SMART_LBA_MID;
      parameter.Registers.LBAHigh = SMART_LBA_HI;
      parameter.Registers.Command = RegisterCommand.SmartCmd;

      return NativeMethods.DeviceIoControl(handle, DriveCommand.SendDriveCommand, 
        ref parameter, Marshal.SizeOf(parameter), out result,
        Marshal.SizeOf(typeof(DriveCommandResult)), out bytesReturned,
        IntPtr.Zero);
    }

    public DriveAttributeValue[] ReadSmartData() {
      if (handle.IsClosed)
        throw new ObjectDisposedException("WindowsATASmart");
      
      DriveCommandParameter parameter = new DriveCommandParameter();
      DriveSmartReadDataResult result;
      uint bytesReturned;

      parameter.DriveNumber = (byte)driveNumber;
      parameter.Registers.Features = RegisterFeature.SmartReadData;
      parameter.Registers.LBAMid = SMART_LBA_MID;
      parameter.Registers.LBAHigh = SMART_LBA_HI;
      parameter.Registers.Command = RegisterCommand.SmartCmd;

      bool isValid = NativeMethods.DeviceIoControl(handle, 
        DriveCommand.ReceiveDriveData, ref parameter, Marshal.SizeOf(parameter), 
        out result, Marshal.SizeOf(typeof(DriveSmartReadDataResult)),
        out bytesReturned, IntPtr.Zero);

      return (isValid) ? result.Attributes : new DriveAttributeValue[0];
    }

    public DriveThresholdValue[] ReadSmartThresholds() {
      if (handle.IsClosed)
        throw new ObjectDisposedException("WindowsATASmart");

      DriveCommandParameter parameter = new DriveCommandParameter();
      DriveSmartReadThresholdsResult result;
      uint bytesReturned = 0;

      parameter.DriveNumber = (byte)driveNumber;
      parameter.Registers.Features = RegisterFeature.SmartReadThresholds;
      parameter.Registers.LBAMid = SMART_LBA_MID;
      parameter.Registers.LBAHigh = SMART_LBA_HI;
      parameter.Registers.Command = RegisterCommand.SmartCmd;

      bool isValid = NativeMethods.DeviceIoControl(handle,
        DriveCommand.ReceiveDriveData, ref parameter, Marshal.SizeOf(parameter),
        out result, Marshal.SizeOf(typeof(DriveSmartReadThresholdsResult)),
        out bytesReturned, IntPtr.Zero); 

      return (isValid) ? result.Thresholds : new DriveThresholdValue[0];
    }

    private string GetString(byte[] bytes) {   
      char[] chars = new char[bytes.Length];
      for (int i = 0; i < bytes.Length; i += 2) {
        chars[i] = (char)bytes[i + 1];
        chars[i + 1] = (char)bytes[i];
      }
      return new string(chars).Trim(new char[] { ' ', '\0' });
    }

    public bool ReadNameAndFirmwareRevision(out string name, out string firmwareRevision) {
      if (handle.IsClosed)
        throw new ObjectDisposedException("WindowsATASmart");

      DriveCommandParameter parameter = new DriveCommandParameter();
      DriveIdentifyResult result;
      uint bytesReturned;

      parameter.DriveNumber = (byte)driveNumber;
      parameter.Registers.Command = RegisterCommand.IdCmd;

      bool valid = NativeMethods.DeviceIoControl(handle, 
        DriveCommand.ReceiveDriveData, ref parameter, Marshal.SizeOf(parameter), 
        out result, Marshal.SizeOf(typeof(DriveIdentifyResult)),
        out bytesReturned, IntPtr.Zero);

      if (!valid) {
        name = null;
        firmwareRevision = null;
        return false;
      }

      name = GetString(result.Identify.ModelNumber);
      firmwareRevision = GetString(result.Identify.FirmwareRevision);
      return true;
    }

    public void Dispose() {
      Close();
    }
    
    protected void Dispose(bool disposing) {
      if (disposing) {
        if (!handle.IsClosed)
          handle.Close();
      }
    }
    
    protected static class NativeMethods {
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
        DriveCommand command, ref DriveCommandParameter parameter,
        int parameterSize, out DriveSmartReadDataResult result, int resultSize,
        out uint bytesReturned, IntPtr overlapped);

      [DllImport(KERNEL, CallingConvention = CallingConvention.Winapi, 
        CharSet = CharSet.Auto, SetLastError = true)]
      [return: MarshalAsAttribute(UnmanagedType.Bool)]
      public static extern bool DeviceIoControl(SafeHandle handle,
        DriveCommand command, ref DriveCommandParameter parameter,
        int parameterSize, out DriveSmartReadThresholdsResult result, 
        int resultSize, out uint bytesReturned, IntPtr overlapped);

      [DllImport(KERNEL, CallingConvention = CallingConvention.Winapi, 
        CharSet = CharSet.Auto, SetLastError = true)]
      [return: MarshalAsAttribute(UnmanagedType.Bool)]
      public static extern bool DeviceIoControl(SafeHandle handle,
        DriveCommand command, ref DriveCommandParameter parameter,
        int parameterSize, out DriveCommandResult result, int resultSize,
        out uint bytesReturned, IntPtr overlapped);

      [DllImport(KERNEL, CallingConvention = CallingConvention.Winapi, 
        CharSet = CharSet.Auto, SetLastError = true)]
      [return: MarshalAsAttribute(UnmanagedType.Bool)]
      public static extern bool DeviceIoControl(SafeHandle handle,
        DriveCommand command, ref DriveCommandParameter parameter,
        int parameterSize, out DriveIdentifyResult result, int resultSize,
        out uint bytesReturned, IntPtr overlapped);
    }    
  }
}
