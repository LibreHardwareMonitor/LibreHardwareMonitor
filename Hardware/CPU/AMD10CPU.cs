/*

  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.

  Copyright (C) 2009-2013 Michael Möller <mmoeller@openhardwaremonitor.org>

*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace OpenHardwareMonitor.Hardware.CPU {

  internal sealed class AMD10CPU : AMDCPU {

    private readonly Sensor coreTemperature;
    private readonly Sensor[] coreClocks;
    private readonly Sensor busClock;
    private readonly Sensor[] cStatesResidency;
    private readonly Sensor coreVoltage;
    private readonly Sensor northbridgeVoltage;

    private const uint PERF_CTL_0 = 0xC0010000;
    private const uint PERF_CTR_0 = 0xC0010004;
    private const uint HWCR = 0xC0010015;
    private const uint P_STATE_0 = 0xC0010064;
    private const uint COFVID_STATUS = 0xC0010071;

    private const byte MISCELLANEOUS_CONTROL_FUNCTION = 3;
    private const ushort FAMILY_10H_MISCELLANEOUS_CONTROL_DEVICE_ID = 0x1203;
    private const ushort FAMILY_11H_MISCELLANEOUS_CONTROL_DEVICE_ID = 0x1303;
    private const ushort FAMILY_12H_MISCELLANEOUS_CONTROL_DEVICE_ID = 0x1703;
    private const ushort FAMILY_14H_MISCELLANEOUS_CONTROL_DEVICE_ID = 0x1703;
    private const ushort FAMILY_15H_MODEL_00_MISC_CONTROL_DEVICE_ID = 0x1603;
    private const ushort FAMILY_15H_MODEL_10_MISC_CONTROL_DEVICE_ID = 0x1403;
    private const ushort FAMILY_15H_MODEL_30_MISC_CONTROL_DEVICE_ID = 0x141D;
    private const ushort FAMILY_15H_MODEL_60_MISC_CONTROL_DEVICE_ID = 0x1573;
    private const ushort FAMILY_16H_MODEL_00_MISC_CONTROL_DEVICE_ID = 0x1533;
    private const ushort FAMILY_16H_MODEL_30_MISC_CONTROL_DEVICE_ID = 0x1583;
    private const ushort FAMILY_17H_MODEL_00_MISC_CONTROL_DEVICE_ID = 0x1577;

    private const uint REPORTED_TEMPERATURE_CONTROL_REGISTER = 0xA4;
    private const uint CLOCK_POWER_TIMING_CONTROL_0_REGISTER = 0xD4;

    private const uint F15H_M60H_REPORTED_TEMP_CTRL_OFFSET = 0xD8200CA4;

    private readonly uint miscellaneousControlAddress;
    private readonly ushort miscellaneousControlDeviceId;

    private const uint cStatesIoPort = 0xCD6;
    private readonly byte cStatesIoOffset;

    private readonly bool isSVI2;

    private readonly FileStream temperatureStream;

    private readonly double timeStampCounterMultiplier;
    private readonly bool corePerformanceBoostSupport;

    public AMD10CPU(int processorIndex, CPUID[][] cpuid, ISettings settings)
      : base(processorIndex, cpuid, settings) 
    {
      // AMD family 1Xh processors support only one temperature sensor
      coreTemperature = new Sensor("CPU Cores", 0, SensorType.Temperature, this, new[] {
            new ParameterDescription("Offset [°C]", "Temperature offset.", 0)
          }, settings);

      coreVoltage = new Sensor("CPU Cores", 0, SensorType.Voltage, this, settings);
      ActivateSensor(coreVoltage);
      northbridgeVoltage = new Sensor("Northbridge", 0,
        SensorType.Voltage, this, settings);
      ActivateSensor(northbridgeVoltage);
      isSVI2 = (family == 0x15 && model >= 0x10) || family == 0x16;

      switch (family) {
        case 0x10:
          miscellaneousControlDeviceId =
 FAMILY_10H_MISCELLANEOUS_CONTROL_DEVICE_ID; break;
        case 0x11:
          miscellaneousControlDeviceId =
 FAMILY_11H_MISCELLANEOUS_CONTROL_DEVICE_ID; break;
        case 0x12:
          miscellaneousControlDeviceId =
 FAMILY_12H_MISCELLANEOUS_CONTROL_DEVICE_ID; break;
        case 0x14:
          miscellaneousControlDeviceId =
 FAMILY_14H_MISCELLANEOUS_CONTROL_DEVICE_ID; break;
        case 0x15:
          switch (model & 0xF0) {
            case 0x00:
              miscellaneousControlDeviceId =
     FAMILY_15H_MODEL_00_MISC_CONTROL_DEVICE_ID; break;
            case 0x10:
              miscellaneousControlDeviceId =
     FAMILY_15H_MODEL_10_MISC_CONTROL_DEVICE_ID; break;
            case 0x30:
              miscellaneousControlDeviceId =
     FAMILY_15H_MODEL_30_MISC_CONTROL_DEVICE_ID; break;
            case 0x70:
            case 0x60:
              miscellaneousControlDeviceId =
     FAMILY_15H_MODEL_60_MISC_CONTROL_DEVICE_ID; break;
            default: miscellaneousControlDeviceId = 0; break;
          }
          break;
        case 0x16:
          switch (model & 0xF0) {
            case 0x00:
              miscellaneousControlDeviceId =
     FAMILY_16H_MODEL_00_MISC_CONTROL_DEVICE_ID; break;
            case 0x30:
              miscellaneousControlDeviceId =
     FAMILY_16H_MODEL_30_MISC_CONTROL_DEVICE_ID; break;
            default: miscellaneousControlDeviceId = 0; break;
          }
          break;
        case 0x17:
          miscellaneousControlDeviceId =
   FAMILY_17H_MODEL_00_MISC_CONTROL_DEVICE_ID; break;
        default: miscellaneousControlDeviceId = 0; break;
      }

      // get the pci address for the Miscellaneous Control registers
      miscellaneousControlAddress = GetPciAddress(
        MISCELLANEOUS_CONTROL_FUNCTION, miscellaneousControlDeviceId);

      busClock = new Sensor("Bus Speed", 0, SensorType.Clock, this, settings);
      coreClocks = new Sensor[coreCount];
      for (int i = 0; i < coreClocks.Length; i++) {
        coreClocks[i] = new Sensor(CoreString(i), i + 1, SensorType.Clock,
          this, settings);
        if (HasTimeStampCounter)
          ActivateSensor(coreClocks[i]);
      }

      corePerformanceBoostSupport = (cpuid[0][0].ExtData[7, 3] & (1 << 9)) > 0;

      // set affinity to the first thread for all frequency estimations
      ulong mask = ThreadAffinity.Set(1UL << cpuid[0][0].Thread);

      // disable core performance boost
      uint hwcrEax, hwcrEdx;
      Ring0.Rdmsr(HWCR, out hwcrEax, out hwcrEdx);
      if (corePerformanceBoostSupport)
        Ring0.Wrmsr(HWCR, hwcrEax | (1 << 25), hwcrEdx);

      uint ctlEax, ctlEdx;
      Ring0.Rdmsr(PERF_CTL_0, out ctlEax, out ctlEdx);
      uint ctrEax, ctrEdx;
      Ring0.Rdmsr(PERF_CTR_0, out ctrEax, out ctrEdx);

      timeStampCounterMultiplier = estimateTimeStampCounterMultiplier();

      // restore the performance counter registers
      Ring0.Wrmsr(PERF_CTL_0, ctlEax, ctlEdx);
      Ring0.Wrmsr(PERF_CTR_0, ctrEax, ctrEdx);

      // restore core performance boost
      if (corePerformanceBoostSupport)
        Ring0.Wrmsr(HWCR, hwcrEax, hwcrEdx);

      // restore the thread affinity.
      ThreadAffinity.Set(mask);

      // the file reader for lm-sensors support on Linux
      temperatureStream = null;

      if (Software.OperatingSystem.IsLinux) {
        string[] devicePaths = Directory.GetDirectories("/sys/class/hwmon/");
        foreach (string path in devicePaths) {
          string name = null;
          try {
            using (StreamReader reader = new StreamReader(path + "/device/name"))
              name = reader.ReadLine();
          } catch (IOException) { }
          switch (name) {
            case "k10temp":
              temperatureStream = new FileStream(path + "/device/temp1_input",
                FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
              break;
          }
        }
      }

      uint addr = Ring0.GetPciAddress(0, 20, 0);
      if (Ring0.ReadPciConfig(addr, 0, out uint dev)) {
        Ring0.ReadPciConfig(addr, 8, out uint rev);

        if (dev == 0x43851002)
          cStatesIoOffset = (byte)((rev & 0xFF) < 0x40 ? 0xB3 : 0x9C);
        else if (dev == 0x780B1022 || dev == 0x790B1022)
          cStatesIoOffset = (byte)0x9C;
      }
      if (cStatesIoOffset != 0) {
        cStatesResidency = new Sensor[]{
          new Sensor("CPU Package C2", 0, SensorType.Level, this, settings),
          new Sensor("CPU Package C3", 1, SensorType.Level, this, settings) };
        ActivateSensor(cStatesResidency[0]);
        ActivateSensor(cStatesResidency[1]);
      }
      Update();
    }

    private double estimateTimeStampCounterMultiplier() {
      // preload the function
      estimateTimeStampCounterMultiplier(0);
      estimateTimeStampCounterMultiplier(0);

      // estimate the multiplier
      List<double> estimate = new List<double>(3);
      for (int i = 0; i < 3; i++)
        estimate.Add(estimateTimeStampCounterMultiplier(0.025));
      estimate.Sort();
      return estimate[1];
    }

    private double estimateTimeStampCounterMultiplier(double timeWindow) {
      uint eax, edx;

      // select event "076h CPU Clocks not Halted" and enable the counter
      Ring0.Wrmsr(PERF_CTL_0,
        (1 << 22) | // enable performance counter
        (1 << 17) | // count events in user mode
        (1 << 16) | // count events in operating-system mode
        0x76, 0x00000000);

      // set the counter to 0
      Ring0.Wrmsr(PERF_CTR_0, 0, 0);

      long ticks = (long)(timeWindow * Stopwatch.Frequency);
      uint lsbBegin, msbBegin, lsbEnd, msbEnd;

      long timeBegin = Stopwatch.GetTimestamp() +
        (long)Math.Ceiling(0.001 * ticks);
      long timeEnd = timeBegin + ticks;
      while (Stopwatch.GetTimestamp() < timeBegin) { }
      Ring0.Rdmsr(PERF_CTR_0, out lsbBegin, out msbBegin);

      while (Stopwatch.GetTimestamp() < timeEnd) { }
      Ring0.Rdmsr(PERF_CTR_0, out lsbEnd, out msbEnd);
      Ring0.Rdmsr(COFVID_STATUS, out eax, out edx);
      double coreMultiplier = GetCoreMultiplier(eax);

      ulong countBegin = ((ulong)msbBegin << 32) | lsbBegin;
      ulong countEnd = ((ulong)msbEnd << 32) | lsbEnd;

      double coreFrequency = 1e-6 *
        (((double)(countEnd - countBegin)) * Stopwatch.Frequency) /
        (timeEnd - timeBegin);

      double busFrequency = coreFrequency / coreMultiplier;

      return 0.25 * Math.Round(4 * TimeStampCounterFrequency / busFrequency);
    }

    protected override uint[] GetMSRs() {
      return new uint[] { PERF_CTL_0, PERF_CTR_0, HWCR, P_STATE_0,
        COFVID_STATUS };
    }

    public override string GetReport() {
      StringBuilder r = new StringBuilder();
      r.Append(base.GetReport());

      r.Append("Miscellaneous Control Address: 0x");
      r.AppendLine((miscellaneousControlAddress).ToString("X",
        CultureInfo.InvariantCulture));
      r.Append("Time Stamp Counter Multiplier: ");
      r.AppendLine(timeStampCounterMultiplier.ToString(
        CultureInfo.InvariantCulture));
      if (family == 0x14) {
        uint value = 0;
        Ring0.ReadPciConfig(miscellaneousControlAddress,
          CLOCK_POWER_TIMING_CONTROL_0_REGISTER, out value);
        r.Append("PCI Register D18F3xD4: ");
        r.AppendLine(value.ToString("X8", CultureInfo.InvariantCulture));
      }
      r.AppendLine();

      return r.ToString();
    }

    private double GetCoreMultiplier(uint cofvidEax) {
      switch (family) {
        case 0x10:
        case 0x11:
        case 0x15:
        case 0x16: {
            // 8:6 CpuDid: current core divisor ID
            // 5:0 CpuFid: current core frequency ID
            uint cpuDid = (cofvidEax >> 6) & 7;
            uint cpuFid = cofvidEax & 0x1F;
            return 0.5 * (cpuFid + 0x10) / (1 << (int)cpuDid);
          }
        case 0x12: {
            // 8:4 CpuFid: current CPU core frequency ID
            // 3:0 CpuDid: current CPU core divisor ID
            uint cpuFid = (cofvidEax >> 4) & 0x1F;
            uint cpuDid = cofvidEax & 0xF;
            double divisor;
            switch (cpuDid) {
              case 0: divisor = 1; break;
              case 1: divisor = 1.5; break;
              case 2: divisor = 2; break;
              case 3: divisor = 3; break;
              case 4: divisor = 4; break;
              case 5: divisor = 6; break;
              case 6: divisor = 8; break;
              case 7: divisor = 12; break;
              case 8: divisor = 16; break;
              default: divisor = 1; break;
            }
            return (cpuFid + 0x10) / divisor;
          }
        case 0x14: {
            // 8:4: current CPU core divisor ID most significant digit
            // 3:0: current CPU core divisor ID least significant digit
            uint divisorIdMSD = (cofvidEax >> 4) & 0x1F;
            uint divisorIdLSD = cofvidEax & 0xF;
            uint value = 0;
            Ring0.ReadPciConfig(miscellaneousControlAddress,
              CLOCK_POWER_TIMING_CONTROL_0_REGISTER, out value);
            uint frequencyId = value & 0x1F;
            return (frequencyId + 0x10) /
              (divisorIdMSD + (divisorIdLSD * 0.25) + 1);
          }
        default:
          return 1;
      }
    }

    private string ReadFirstLine(Stream stream) {
      StringBuilder sb = new StringBuilder();
      try {
        stream.Seek(0, SeekOrigin.Begin);
        int b = stream.ReadByte();
        while (b != -1 && b != 10) {
          sb.Append((char)b);
          b = stream.ReadByte();
        }
      } catch { }
      return sb.ToString();
    }

    public override void Update() {
      base.Update();

      if (temperatureStream == null) {
        if (miscellaneousControlAddress != Ring0.InvalidPciAddress) {
          uint value;
          if (miscellaneousControlAddress == FAMILY_15H_MODEL_60_MISC_CONTROL_DEVICE_ID) {
            Ring0.WritePciConfig(Ring0.GetPciAddress(0, 0, 0), 0xB8, F15H_M60H_REPORTED_TEMP_CTRL_OFFSET);
            Ring0.ReadPciConfig(Ring0.GetPciAddress(0, 0, 0), 0xBC, out value);
          } else {
            Ring0.ReadPciConfig(miscellaneousControlAddress,
              REPORTED_TEMPERATURE_CONTROL_REGISTER, out value);
          }
          if ((family == 0x15 || family == 0x16) && (value & 0x30000) == 0x3000) {
            if (family == 0x15 && (model & 0xF0) == 0x00) {
              coreTemperature.Value = ((value >> 21) & 0x7FC) / 8.0f +
                coreTemperature.Parameters[0].Value - 49;
            } else {
              coreTemperature.Value = ((value >> 21) & 0x7FF) / 8.0f +
                coreTemperature.Parameters[0].Value - 49;
            }
          } else {
            coreTemperature.Value = ((value >> 21) & 0x7FF) / 8.0f +
              coreTemperature.Parameters[0].Value;
          }
          ActivateSensor(coreTemperature);
        } else {
          DeactivateSensor(coreTemperature);
        }
      } else {
        string s = ReadFirstLine(temperatureStream);
        try {
          coreTemperature.Value = 0.001f *
            long.Parse(s, CultureInfo.InvariantCulture);
          ActivateSensor(coreTemperature);
        } catch {
          DeactivateSensor(coreTemperature);
        }
      }

      if (HasTimeStampCounter) {
        double newBusClock = 0;
        float maxCoreVoltage = 0, maxNBVoltage = 0;

        for (int i = 0; i < coreClocks.Length; i++) {
          Thread.Sleep(1);

          uint curEax, curEdx;
          if (Ring0.RdmsrTx(COFVID_STATUS, out curEax, out curEdx, 1UL << cpuid[i][0].Thread)) {
            double multiplier;
            multiplier = GetCoreMultiplier(curEax);

            coreClocks[i].Value =
              (float)(multiplier * TimeStampCounterFrequency /
              timeStampCounterMultiplier);
            newBusClock =
              (float)(TimeStampCounterFrequency / timeStampCounterMultiplier);
          } else {
            coreClocks[i].Value = (float)TimeStampCounterFrequency;
          }

          float SVI2Volt(uint vid) => vid < 0b1111_1000 ? 1.5500f - 0.00625f * vid : 0;
          float SVI1Volt(uint vid) => vid < 0x7C ? 1.550f - 0.0125f * vid : 0;
          float newCoreVoltage, newNBVoltage;
          uint coreVid60 = (curEax >> 9) & 0x7F;
          if (isSVI2) {
            newCoreVoltage = SVI2Volt(curEax >> 13 & 0x80 | coreVid60);
            newNBVoltage = SVI2Volt(curEax >> 24);
          } else {
            newCoreVoltage = SVI1Volt(coreVid60);
            newNBVoltage = SVI1Volt(curEax >> 25);
          }
          if (newCoreVoltage > maxCoreVoltage)
            maxCoreVoltage = newCoreVoltage;
          if (newNBVoltage > maxNBVoltage)
            maxNBVoltage = newNBVoltage;
        }
        coreVoltage.Value = maxCoreVoltage;
        northbridgeVoltage.Value = maxNBVoltage;

        if (newBusClock > 0) {
          this.busClock.Value = (float)newBusClock;
          ActivateSensor(this.busClock);
        }
      }

      if (cStatesResidency != null) {
        for (int i = 0; i < cStatesResidency.Length; i++) {
          Ring0.WriteIoPort(cStatesIoPort, (byte)(cStatesIoOffset + i));
          cStatesResidency[i].Value = Ring0.ReadIoPort(cStatesIoPort + 1) / 256f * 100;
        }
      }
    }

    public override void Close() {
      if (temperatureStream != null) {
        temperatureStream.Close();
      }
      base.Close();
    }
  }
}
