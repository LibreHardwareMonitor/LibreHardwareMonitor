// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Runtime.InteropServices;
using System.Text;
using OpenHardwareMonitor.Interop;

namespace OpenHardwareMonitor.Hardware.HDD {
  public class NVMeSmart : IDisposable {
    private readonly int driveNumber;
    private readonly SafeHandle handle;

    internal NVMeSmart(StorageInfo storageInfo) {
      driveNumber = storageInfo.Index;
      NVMeDrive = null;

      //test samsung protocol
      if (NVMeDrive == null && storageInfo.Name.ToLower().Contains("samsung")) {
        handle = NVMeSamsung.IdentifyDevice(storageInfo);
        if (handle != null) {
          NVMeDrive = new NVMeSamsung();
        }
      }

      //test intel protocol
      if (NVMeDrive == null && storageInfo.Name.ToLower().Contains("intel")) {
        handle = NVMeIntel.IdentifyDevice(storageInfo);
        if (handle != null) {
          NVMeDrive = new NVMeIntel();
        }
      }

      //test intel raid protocol
      if (NVMeDrive == null && storageInfo.Name.ToLower().Contains("intel")) {
        handle = NVMeIntelRst.IdentifyDevice(storageInfo);
        if (handle != null) {
          NVMeDrive = new NVMeIntelRst();
        }
      }

      //test windows generic driver protocol
      if (NVMeDrive == null) {
        handle = NVMeWindows.IdentifyDevice(storageInfo);
        if (handle != null) {
          NVMeDrive = new NVMeWindows();
        }
      }
    }

    public bool IsValid {
      get {
        if (handle == null || handle.IsInvalid)
          return false;


        return true;
      }
    }

    internal INVMeDrive NVMeDrive { get; }

    public void Dispose() {
      Close();
    }

    private static string GetString(byte[] s) {
      return Encoding.ASCII.GetString(s).Trim('\t', '\n', '\r', ' ', '\0');
    }

    private static short KelvinToCelsius(ushort k) {
      return (short) (k > 0 ? k - 273 : short.MinValue);
    }

    private static short KelvinToCelsius(byte[] k) {
      return KelvinToCelsius(BitConverter.ToUInt16(k, 0));
    }

    public void Close() {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected void Dispose(bool disposing) {
      if (disposing) {
        if (handle != null && !handle.IsClosed)
          handle.Close();
      }
    }

    public HDD.NVMeInfo GetInfo() {
      if (handle == null || handle.IsClosed)
        return null;


      bool valid = false;
      var data = new Kernel32.NVME_IDENTIFY_CONTROLLER_DATA();
      if (NVMeDrive != null)
        valid = NVMeDrive.IdentifyController(handle, out data);

      if (!valid)
        return null;


      return new NVMeInfo(driveNumber, data);
    }

    public HDD.NVMeHealthInfo GetHealthInfo() {
      if (handle == null || handle.IsClosed)
        return null;


      bool valid = false;
      var data = new Kernel32.NVME_HEALTH_INFO_LOG();
      if (NVMeDrive != null)
        valid = NVMeDrive.HealthInfoLog(handle, out data);

      if (!valid)
        return null;


      return new NVMeHealthInfo(data);
    }

    private class NVMeInfo : HDD.NVMeInfo {
      public NVMeInfo(int index, Kernel32.NVME_IDENTIFY_CONTROLLER_DATA data) {
        Index = index;
        VID = data.VID;
        SSVID = data.SSVID;
        Serial = GetString(data.SN);
        Model = GetString(data.MN);
        Revision = GetString(data.FR);
        IEEE = data.IEEE;
        TotalCapacity = BitConverter.ToUInt64(data.TNVMCAP, 0); // 128bit little endian
        UnallocatedCapacity = BitConverter.ToUInt64(data.UNVMCAP, 0);
        ControllerId = data.CNTLID;
        NumberNamespaces = data.NN;
      }
    }

    private class NVMeHealthInfo : HDD.NVMeHealthInfo {
      public NVMeHealthInfo(Kernel32.NVME_HEALTH_INFO_LOG log) {
        CriticalWarning = (Kernel32.NVME_CRITICAL_WARNING) log.CriticalWarning;
        Temperature = KelvinToCelsius(log.CompositeTemp);
        AvailableSpare = log.AvailableSpare;
        AvailableSpareThreshold = log.AvailableSpareThreshold;
        PercentageUsed = log.PercentageUsed;
        DataUnitRead = BitConverter.ToUInt64(log.DataUnitRead, 0);
        DataUnitWritten = BitConverter.ToUInt64(log.DataUnitWritten, 0);
        HostReadCommands = BitConverter.ToUInt64(log.HostReadCommands, 0);
        HostWriteCommands = BitConverter.ToUInt64(log.HostWriteCommands, 0);
        ControllerBusyTime = BitConverter.ToUInt64(log.ControllerBusyTime, 0);
        PowerCycle = BitConverter.ToUInt64(log.PowerCycles, 0);
        PowerOnHours = BitConverter.ToUInt64(log.PowerOnHours, 0);
        UnsafeShutdowns = BitConverter.ToUInt64(log.UnsafeShutdowns, 0);
        MediaErrors = BitConverter.ToUInt64(log.MediaAndDataIntegrityErrors, 0);
        ErrorInfoLogEntryCount = BitConverter.ToUInt64(log.NumberErrorInformationLogEntries, 0);
        WarningCompositeTemperatureTime = log.WarningCompositeTemperatureTime;
        CriticalCompositeTemperatureTime = log.CriticalCompositeTemperatureTime;

        TemperatureSensors = new short[log.TemperatureSensor.Length];
        for (int i = 0; i < TemperatureSensors.Length; i++)
          TemperatureSensors[i] = KelvinToCelsius(log.TemperatureSensor[i]);
      }
    }
  }
}