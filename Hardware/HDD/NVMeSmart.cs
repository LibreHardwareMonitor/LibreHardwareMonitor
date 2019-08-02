// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using OpenHardwareMonitor.Interop;

namespace OpenHardwareMonitor.Hardware.HDD {

  internal class NVMeSmart : IDisposable {

    public INVMeDrive NVMeDrive { get; set; }
    private SafeHandle handle;
    private int driveNumber;

    private class NVMeInfoImpl : NVMeInfo {
      public NVMeInfoImpl(int index, Kernel32.NVMeIdentifyControllerData data) {
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

    private class NVMeHealthInfoImpl : NVMeHealthInfo {
      public NVMeHealthInfoImpl(Kernel32.NVMeHealthInfoLog log) {
        CriticalWarning = (Kernel32.NVMeCriticalWarning)log.CriticalWarning;
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

    private static string GetString(byte[] s) {
      return Encoding.ASCII.GetString(s).Trim('\t', '\n', '\r', ' ', '\0');
    }

    private double int128_to_double(byte[] buffer) {
      int i;
      double result = 0;
      for (i = 0; i < 16; i++) {
        result *= 256;
        result += buffer[15 - i];
      }
      return result;
    }

    private static short KelvinToCelsius(ushort k) {
      return (short)((k > 0) ? (int)k - 273 : short.MinValue);
    }

    private static short KelvinToCelsius(byte[] k) {
      return KelvinToCelsius(BitConverter.ToUInt16(k, 0));
    }

    public NVMeSmart(StorageInfo _storageInfo) {
      this.driveNumber = _storageInfo.Index;
      this.NVMeDrive = null;

      //test samsung protocol
      if (this.NVMeDrive == null && _storageInfo.Name.ToLower().Contains("samsung")) {
        handle = NVMeSamsung._Identify(_storageInfo);
        if (handle != null) {
          NVMeDrive = new NVMeSamsung();
        }
      }

      //test intel protocol
      if (this.NVMeDrive == null && _storageInfo.Name.ToLower().Contains("intel")) {
        handle = NVMeIntel._Identify(_storageInfo);
        if (handle != null) {
          NVMeDrive = new NVMeIntel();
        }
      }

      //test intel raid protocol
      if (this.NVMeDrive == null && _storageInfo.Name.ToLower().Contains("intel")) {
        handle = NVMeIntelRst._Identify(_storageInfo);
        if (handle != null) {
          NVMeDrive = new NVMeIntelRst();
        }
      }

      //test windows generic driver protocol
      if (this.NVMeDrive == null) {
        handle = NVMeWindows._Identify(_storageInfo);
        if (handle != null) {
          NVMeDrive = new NVMeWindows();
        }
      }
    }

    public void Close() {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    public void Dispose() {
      Close();
    }

    protected void Dispose(bool disposing) {
      if (disposing) {
        if (handle != null && !handle.IsClosed)
          handle.Close();
      }
    }

    public bool IsValid {
      get {
        if (handle == null || handle.IsInvalid)
          return false;
        return true;
      }
    }

    public NVMeInfo GetInfo() {
      if (handle == null || handle.IsClosed)
        return null;
      bool valid = false;
      Kernel32.NVMeIdentifyControllerData data = new Kernel32.NVMeIdentifyControllerData();
      if (NVMeDrive != null)
        valid = NVMeDrive.IdentifyController(handle, out data);
      if (!valid)
        return null;
      return new NVMeInfoImpl(driveNumber, data);
    }

    public NVMeHealthInfo GetHealthInfo() {
      if (handle == null || handle.IsClosed)
        return null;
      bool valid = false;
      Kernel32.NVMeHealthInfoLog data = new Kernel32.NVMeHealthInfoLog();
      if (NVMeDrive != null)
        valid = NVMeDrive.HealthInfoLog(handle, out data);
      if (!valid)
        return null;
      return new NVMeHealthInfoImpl(data);
    }

  }
}