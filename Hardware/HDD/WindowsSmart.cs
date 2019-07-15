// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) 2009-2012 Michael Möller <mmoeller@openhardwaremonitor.org>
// Copyright (C) 2010 Paul Werelds
// Copyright (C) 2011 Roland Reinl <roland-reinl@gmx.de>

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace OpenHardwareMonitor.Hardware.HDD {

  public class WindowsSmart : ISmart {

    private SafeHandle handle { get; } = null;
    private int driveNumber { get; set; }
    public bool IsValid {
      get { return !handle.IsInvalid; }
    }

    public WindowsSmart(int driveNumber) {
      this.driveNumber = driveNumber;
      handle = Interop.CreateFile(@"\\.\PhysicalDrive" + driveNumber,
        FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
    }

    protected void Dispose(bool disposing) {
      if (disposing) {
        if (!handle.IsClosed)
          handle.Close();
      }
    }

    public void Dispose() {
      Close();
    }

    public void Close() {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    public bool EnableSmart() {
      if (handle.IsClosed)
        throw new ObjectDisposedException("WindowsATASmart");

      Interop.DriveCommandParameter parameter = new Interop.DriveCommandParameter();
      Interop.DriveCommandResult result;
      uint bytesReturned;

      parameter.DriveNumber = (byte)driveNumber;
      parameter.Registers.Features = Interop.RegisterFeature.SmartEnableOperations;
      parameter.Registers.LBAMid = Interop.SMART_LBA_MID;
      parameter.Registers.LBAHigh = Interop.SMART_LBA_HI;
      parameter.Registers.Command = Interop.RegisterCommand.SmartCmd;

      return Interop.DeviceIoControl(
        handle,
        Interop.DriveCommand.SendDriveCommand,
        ref parameter, Marshal.SizeOf(parameter), out result,
        Marshal.SizeOf<Interop.DriveCommandResult>(), out bytesReturned, IntPtr.Zero);
    }

    public Interop.DriveAttributeValue[] ReadSmartData() {
      if (handle.IsClosed)
        throw new ObjectDisposedException("WindowsATASmart");

      Interop.DriveCommandParameter parameter = new Interop.DriveCommandParameter();
      Interop.DriveSmartReadDataResult result;
      uint bytesReturned;

      parameter.DriveNumber = (byte)driveNumber;
      parameter.Registers.Features = Interop.RegisterFeature.SmartReadData;
      parameter.Registers.LBAMid = Interop.SMART_LBA_MID;
      parameter.Registers.LBAHigh = Interop.SMART_LBA_HI;
      parameter.Registers.Command = Interop.RegisterCommand.SmartCmd;

      bool isValid = Interop.DeviceIoControl(handle,
        Interop.DriveCommand.ReceiveDriveData, ref parameter, Marshal.SizeOf(parameter),
        out result, Marshal.SizeOf<Interop.DriveSmartReadDataResult>(),
        out bytesReturned, IntPtr.Zero);

      return (isValid) ? result.Attributes : new Interop.DriveAttributeValue[0];
    }

    public Interop.DriveThresholdValue[] ReadSmartThresholds() {
      if (handle.IsClosed)
        throw new ObjectDisposedException("WindowsATASmart");

      Interop.DriveCommandParameter parameter = new Interop.DriveCommandParameter();
      Interop.DriveSmartReadThresholdsResult result;
      uint bytesReturned = 0;

      parameter.DriveNumber = (byte)driveNumber;
      parameter.Registers.Features = Interop.RegisterFeature.SmartReadThresholds;
      parameter.Registers.LBAMid = Interop.SMART_LBA_MID;
      parameter.Registers.LBAHigh = Interop.SMART_LBA_HI;
      parameter.Registers.Command = Interop.RegisterCommand.SmartCmd;

      bool isValid = Interop.DeviceIoControl(handle,
        Interop.DriveCommand.ReceiveDriveData, ref parameter, Marshal.SizeOf(parameter),
        out result, Marshal.SizeOf<Interop.DriveSmartReadThresholdsResult>(),
        out bytesReturned, IntPtr.Zero);

      return (isValid) ? result.Thresholds : new Interop.DriveThresholdValue[0];
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

      Interop.DriveCommandParameter parameter = new Interop.DriveCommandParameter();
      Interop.DriveIdentifyResult result;
      uint bytesReturned;

      parameter.DriveNumber = (byte)driveNumber;
      parameter.Registers.Command = Interop.RegisterCommand.IdCmd;

      bool valid = Interop.DeviceIoControl(handle,
        Interop.DriveCommand.ReceiveDriveData, ref parameter, Marshal.SizeOf(parameter),
        out result, Marshal.SizeOf<Interop.DriveIdentifyResult>(),
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
  }
}