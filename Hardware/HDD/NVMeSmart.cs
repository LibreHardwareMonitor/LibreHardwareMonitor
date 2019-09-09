// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Runtime.InteropServices;
using System.Text;
using OpenHardwareMonitor.Interop;

namespace OpenHardwareMonitor.Hardware.HDD {
  internal class NVMeSmart : IDisposable {
    private readonly int driveNumber;
    private readonly SafeHandle handle;

    public NVMeSmart(StorageInfo storageInfo) {
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

    public INVMeDrive NVMeDrive { get; }

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
      var data = new Kernel32.NVMeIdentifyControllerData();
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
      var data = new Kernel32.NVMeHealthInfoLog();
      if (NVMeDrive != null)
        valid = NVMeDrive.HealthInfoLog(handle, out data);

      if (!valid)
        return null;


      return new NVMeHealthInfo(data);
    }

    private class NVMeInfo : HDD.NVMeInfo {
      public NVMeInfo(int index, Kernel32.NVMeIdentifyControllerData data) {
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
      }
    }

    private class NVMeHealthInfo : HDD.NVMeHealthInfo {
      public NVMeHealthInfo(Kernel32.NVMeHealthInfoLog log) {
        CriticalWarning = (Kernel32.NVMeCriticalWarning) log.CriticalWarning;
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
        for (int i = 0; i < TemperatureSensors.Length; i++)
          TemperatureSensors[i] = KelvinToCelsius(log.TemperatureSensors[i]);
      }
    }
  }
}