// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.IO;
using System.Runtime.InteropServices;
using OpenHardwareMonitor.Interop;

namespace OpenHardwareMonitor.Hardware.HDD {
  internal class WindowsSmart : ISmart {
    private readonly int driveNumber;
    private readonly SafeHandle handle;

    public WindowsSmart(int driveNumber) {
      this.driveNumber = driveNumber;
      handle = Kernel32.CreateFile(@"\\.\PhysicalDrive" + driveNumber,
                                   FileAccess.ReadWrite,
                                   FileShare.ReadWrite,
                                   IntPtr.Zero,
                                   FileMode.Open,
                                   FileAttributes.Normal,
                                   IntPtr.Zero);
    }

    public bool IsValid => !handle.IsInvalid;

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


      var parameter = new Kernel32.SENDCMDINPARAMS {
        bDriveNumber = (byte) driveNumber,
        irDriveRegs = {
          bFeaturesReg = Kernel32.SMART_FEATURES.ENABLE_SMART,
          bCylLowReg = Kernel32.SMART_LBA_MID,
          bCylHighReg = Kernel32.SMART_LBA_HI,
          bCommandReg = Kernel32.ATA_COMMAND.ATA_SMART
        }
      };


      return Kernel32.DeviceIoControl(handle,
                                      Kernel32.DFP.DFP_SEND_DRIVE_COMMAND,
                                      ref parameter,
                                      Marshal.SizeOf(parameter),
                                      out Kernel32.SENDCMDOUTPARAMS _,
                                      Marshal.SizeOf<Kernel32.SENDCMDOUTPARAMS>(),
                                      out _,
                                      IntPtr.Zero);
    }

    public Kernel32.SMART_ATTRIBUTE[] ReadSmartData() {
      if (handle.IsClosed)
        throw new ObjectDisposedException("WindowsATASmart");


      var parameter = new Kernel32.SENDCMDINPARAMS {
        bDriveNumber = (byte) driveNumber,
        irDriveRegs = {
          bFeaturesReg = Kernel32.SMART_FEATURES.SMART_READ_DATA,
          bCylLowReg = Kernel32.SMART_LBA_MID,
          bCylHighReg = Kernel32.SMART_LBA_HI,
          bCommandReg = Kernel32.ATA_COMMAND.ATA_SMART
        }
      };


      bool isValid = Kernel32.DeviceIoControl(handle,
                                              Kernel32.DFP.DFP_RECEIVE_DRIVE_DATA,
                                              ref parameter,
                                              Marshal.SizeOf(parameter),
                                              out Kernel32.ATTRIBUTECMDOUTPARAMS result,
                                              Marshal.SizeOf<Kernel32.ATTRIBUTECMDOUTPARAMS>(),
                                              out _,
                                              IntPtr.Zero);

      return (isValid) ? result.Attributes : new Kernel32.SMART_ATTRIBUTE[0];
    }

    public Kernel32.SMART_THRESHOLD[] ReadSmartThresholds() {
      if (handle.IsClosed)
        throw new ObjectDisposedException("WindowsATASmart");


      var parameter = new Kernel32.SENDCMDINPARAMS {
        bDriveNumber = (byte) driveNumber,
        irDriveRegs = {
          bFeaturesReg = Kernel32.SMART_FEATURES.READ_THRESHOLDS,
          bCylLowReg = Kernel32.SMART_LBA_MID,
          bCylHighReg = Kernel32.SMART_LBA_HI,
          bCommandReg = Kernel32.ATA_COMMAND.ATA_SMART
        }
      };


      bool isValid = Kernel32.DeviceIoControl(handle,
                                              Kernel32.DFP.DFP_RECEIVE_DRIVE_DATA,
                                              ref parameter,
                                              Marshal.SizeOf(parameter),
                                              out Kernel32.THRESHOLDCMDOUTPARAMS result,
                                              Marshal.SizeOf<Kernel32.THRESHOLDCMDOUTPARAMS>(),
                                              out _,
                                              IntPtr.Zero);

      return (isValid) ? result.Thresholds : new Kernel32.SMART_THRESHOLD[0];
    }

    public bool ReadNameAndFirmwareRevision(out string name, out string firmwareRevision) {
      if (handle.IsClosed)
        throw new ObjectDisposedException("WindowsATASmart");


      var parameter = new Kernel32.SENDCMDINPARAMS { bDriveNumber = (byte) driveNumber, irDriveRegs = { bCommandReg = Kernel32.ATA_COMMAND.ATA_IDENTIFY_DEVICE } };


      bool valid = Kernel32.DeviceIoControl(handle,
                                            Kernel32.DFP.DFP_RECEIVE_DRIVE_DATA,
                                            ref parameter,
                                            Marshal.SizeOf(parameter),
                                            out Kernel32.IDENTIFYCMDOUTPARAMS result,
                                            Marshal.SizeOf<Kernel32.IDENTIFYCMDOUTPARAMS>(),
                                            out _,
                                            IntPtr.Zero);

      if (!valid) {
        name = null;
        firmwareRevision = null;
        return false;
      }

      name = GetString(result.Identify.ModelNumber);
      firmwareRevision = GetString(result.Identify.FirmwareRevision);
      return true;
    }

    protected void Dispose(bool disposing) {
      if (disposing) {
        if (!handle.IsClosed)
          handle.Close();
      }
    }

    private string GetString(byte[] bytes) {
      char[] chars = new char[bytes.Length];
      for (int i = 0; i < bytes.Length; i += 2) {
        chars[i] = (char) bytes[i + 1];
        chars[i + 1] = (char) bytes[i];
      }

      return new string(chars).Trim(' ', '\0');
    }
  }
}