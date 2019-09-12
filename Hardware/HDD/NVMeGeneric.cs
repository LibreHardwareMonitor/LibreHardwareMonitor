// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System.Collections.Generic;
using System.Text;
using OpenHardwareMonitor.Interop;

namespace OpenHardwareMonitor.Hardware.HDD {
  public sealed class NVMeGeneric : AbstractStorage {
    private const ulong Scale = 1000000;
    private const ulong Units = 512;

    private readonly NVMeInfo info;
    private readonly List<NVMeSensor> sensors = new List<NVMeSensor>();
    private readonly NVMeSmart smart;

    /// <summary>
    /// Gets the SMART data.
    /// </summary>
    public NVMeSmart Smart => smart;

    private NVMeGeneric(StorageInfo storageInfo, NVMeInfo info, int index, ISettings settings)
      : base(storageInfo, info.Model, info.Revision, "nvme", index, settings) {
      smart = new NVMeSmart(storageInfo);
      this.info = info;
      CreateSensors();
    }

    private static NVMeInfo GetDeviceInfo(StorageInfo storageInfo) {
      var smart = new NVMeSmart(storageInfo);
      NVMeInfo info = smart.GetInfo();
      return info;
    }

    internal static AbstractStorage CreateInstance(StorageInfo storageInfo, ISettings settings) {
      NVMeInfo nvmeInfo = GetDeviceInfo(storageInfo);
      return nvmeInfo == null ? null : new NVMeGeneric(storageInfo, nvmeInfo, storageInfo.Index, settings);
    }

    protected override void CreateSensors() {
      NVMeHealthInfo log = smart.GetHealthInfo();
      if (log != null) {
        AddSensor("Temperature", 0, false, SensorType.Temperature, health => health.Temperature);
        AddSensor("Available Spare", 1, false, SensorType.Level, health => health.AvailableSpare);
        AddSensor("Available Spare Threshold", 2, false, SensorType.Level, health => health.AvailableSpareThreshold);
        AddSensor("Percentage Used", 3, false, SensorType.Level, health => health.PercentageUsed);
        AddSensor("Data Read", 4, false, SensorType.Data, health => UnitsToData(health.DataUnitRead));
        AddSensor("Data Written", 5, false, SensorType.Data, health => UnitsToData(health.DataUnitWritten));

        int sensorIdx = 6;
        for (int i = 0; i < log.TemperatureSensors.Length; i++) {
          int idx = i;
          if (log.TemperatureSensors[idx] > short.MinValue) {
            AddSensor("Temperature " + (idx + 1), sensorIdx, false, SensorType.Temperature, health => health.TemperatureSensors[idx]);
            sensorIdx++;
          }
        }
      }

      base.CreateSensors();
    }

    private void AddSensor(string name, int index, bool defaultHidden, SensorType sensorType, GetSensorValue getValue) {
      var sensor = new NVMeSensor(name, index, defaultHidden, sensorType, this, settings, getValue);
      ActivateSensor(sensor);
      sensor.Value = 0;
      sensors.Add(sensor);
    }

    private static float UnitsToData(ulong u) {
      // one unit is 512 * 1000 bytes, return in GB (not GiB)
      return Units * u / Scale;
    }

    protected override void UpdateSensors() {
      NVMeHealthInfo health = smart.GetHealthInfo();
      foreach (NVMeSensor sensor in sensors)
        sensor.Update(health);
    }

    protected override void GetReport(StringBuilder r) {
      if (info == null)
        return;


      r.AppendLine("PCI Vendor ID: 0x" + info.VID.ToString("x04"));
      if (info.VID != info.SSVID)
        r.AppendLine("PCI Subsystem Vendor ID: 0x" + info.VID.ToString("x04"));

      r.AppendLine("IEEE OUI Identifier: 0x" + info.IEEE[2].ToString("x02") + info.IEEE[1].ToString("x02") + info.IEEE[0].ToString("x02"));
      r.AppendLine("Total NVM Capacity: " + info.TotalCapacity);
      r.AppendLine("Unallocated NVM Capacity: " + info.UnallocatedCapacity);
      r.AppendLine("Controller ID: " + info.ControllerId);
      r.AppendLine("Number of Namespaces: " + info.NumberNamespaces);

      NVMeHealthInfo health = smart.GetHealthInfo();
      if (health == null)
        return;


      if (health.CriticalWarning == Kernel32.NVME_CRITICAL_WARNING.None)
        r.AppendLine("Critical Warning: -");
      else {
        if ((health.CriticalWarning & Kernel32.NVME_CRITICAL_WARNING.AvailableSpaceLow) != 0)
          r.AppendLine("Critical Warning: the available spare space has fallen below the threshold.");

        if ((health.CriticalWarning & Kernel32.NVME_CRITICAL_WARNING.TemperatureThreshold) != 0)
          r.AppendLine("Critical Warning: a temperature is above an over temperature threshold or below an under temperature threshold.");

        if ((health.CriticalWarning & Kernel32.NVME_CRITICAL_WARNING.ReliabilityDegraded) != 0)
          r.AppendLine("Critical Warning: the device reliability has been degraded due to significant media related errors or any internal error that degrades device reliability.");

        if ((health.CriticalWarning & Kernel32.NVME_CRITICAL_WARNING.ReadOnly) != 0)
          r.AppendLine("Critical Warning: the media has been placed in read only mode.");

        if ((health.CriticalWarning & Kernel32.NVME_CRITICAL_WARNING.VolatileMemoryBackupDeviceFailed) != 0)
          r.AppendLine("Critical Warning: the volatile memory backup device has failed.");
      }

      r.AppendLine("Temperature: " + health.Temperature + " Celsius");
      r.AppendLine("Available Spare: " + health.AvailableSpare + "%");
      r.AppendLine("Available Spare Threshold: " + health.AvailableSpareThreshold + "%");
      r.AppendLine("Percentage Used: " + health.PercentageUsed + "%");
      r.AppendLine("Data Units Read: " + health.DataUnitRead);
      r.AppendLine("Data Units Written: " + health.DataUnitWritten);
      r.AppendLine("Host Read Commands: " + health.HostReadCommands);
      r.AppendLine("Host Write Commands: " + health.HostWriteCommands);
      r.AppendLine("Controller Busy Time: " + health.ControllerBusyTime);
      r.AppendLine("Power Cycles: " + health.PowerCycle);
      r.AppendLine("Power On Hours: " + health.PowerOnHours);
      r.AppendLine("Unsafe Shutdowns: " + health.UnsafeShutdowns);
      r.AppendLine("Media Errors: " + health.MediaErrors);
      r.AppendLine("Number of Error Information Log Entries: " + health.ErrorInfoLogEntryCount);
      r.AppendLine("Warning Composite Temperature Time: " + health.WarningCompositeTemperatureTime);
      r.AppendLine("Critical Composite Temperature Time: " + health.CriticalCompositeTemperatureTime);
      for (int i = 0; i < health.TemperatureSensors.Length; i++) {
        if (health.TemperatureSensors[i] > short.MinValue)
          r.AppendLine("Temperature Sensor " + (i + 1) + ": " + health.TemperatureSensors[i] + " Celsius");
      }
    }

    public override void Close() {
      smart?.Close();

      base.Close();
    }

    private delegate float GetSensorValue(NVMeHealthInfo health);

    private class NVMeSensor : Sensor {
      private readonly GetSensorValue getValue;

      public NVMeSensor(string name, int index, bool defaultHidden, SensorType sensorType, Hardware hardware, ISettings settings, GetSensorValue getValue)
        : base(name, index, defaultHidden, sensorType, hardware, null, settings) {
        this.getValue = getValue;
      }

      public void Update(NVMeHealthInfo health) {
        Value = getValue(health);
      }
    }
  }
}