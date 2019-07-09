// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) 2016-2019 Sebastian Grams <https://github.com/sebastian-dev>
// Copyright (C) 2016-2019 Aqua Computer <https://github.com/aquacomputer, info@aqua-computer.de>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace OpenHardwareMonitor.Hardware.CPU
{
  internal sealed class AMD17CPU : AMDCPU
  {
    // counter, to create sensor index values
    private int _sensorTemperatures = 0;
    private int _sensorPower = 0;
    private int _sensorVoltage = 0;
    private int _sensorClock = 0;
    private int _sensorMulti = 0;

    // register index names for CPUID[]
    private const int EAX = 0;
    private const int EBX = 1;
    private const int ECX = 2;
    private const int EDX = 3;

    #region amd zen registers
    private const uint PERF_CTL_0 = 0xC0010000;
    private const uint PERF_CTR_0 = 0xC0010004;
    private const uint HWCR = 0xC0010015;

    private const uint MSR_PSTATE_L = 0xC0010061;
    private const uint MSR_PSTATE_C = 0xC0010062;
    private const uint MSR_PSTATE_S = 0xC0010063;
    private const uint MSR_PSTATE_0 = 0xC0010064;

    private const uint MSR_PWR_UNIT = 0xC0010299;
    private const uint MSR_CORE_ENERGY_STAT = 0xC001029A;
    private const uint MSR_PKG_ENERGY_STAT = 0xC001029B;
    private const uint MSR_HARDWARE_PSTATE_STATUS = 0xC0010293;
    private const uint COFVID_STATUS = 0xC0010071;
    private const uint FAMILY_17H_PCI_CONTROL_REGISTER = 0x60;
    private const uint FAMILY_17H_MODEL_01_MISC_CONTROL_DEVICE_ID = 0x1463;
    private const uint F17H_M01H_THM_TCON_CUR_TMP = 0x00059800;
    private const uint F17H_M01H_SVI = 0x0005A000;

    public const uint F17H_TEMP_OFFSET_FLAG = 0x80000;
    #endregion

    #region Processor
    private class Processor
    {
      private AMD17CPU _hw = null;
      private DateTime _lastPwrTime = new DateTime(0);
      private uint _lastPwrValue = 0;
      private Sensor _packagePower = null;
      private Sensor _coreTemperatureTctl = null;
      private Sensor _coreTemperatureTdie = null;
      private Sensor _coreVoltage = null;
      private Sensor _socVoltage = null;
      public List<NumaNode> Nodes { get; private set; }

      public Processor(Hardware hw)
      {
        this._hw = (AMD17CPU)hw;
        Nodes = new List<NumaNode>();

        _packagePower = new Sensor("Package Power", this._hw._sensorPower++, SensorType.Power, this._hw, this._hw.settings);
        _coreTemperatureTctl = new Sensor("Core (Tctl)", this._hw._sensorTemperatures++, SensorType.Temperature, this._hw, this._hw.settings);
        _coreTemperatureTdie = new Sensor("Core (Tdie)", this._hw._sensorTemperatures++, SensorType.Temperature, this._hw, this._hw.settings);
        _coreVoltage = new Sensor("Core (SVI2)", this._hw._sensorVoltage++, SensorType.Voltage, this._hw, this._hw.settings);
        _socVoltage = new Sensor("SoC (SVI2)", this._hw._sensorVoltage++, SensorType.Voltage, this._hw, this._hw.settings);

        _hw.ActivateSensor(_packagePower);
        _hw.ActivateSensor(_coreTemperatureTctl);
        _hw.ActivateSensor(_coreTemperatureTdie);
        _hw.ActivateSensor(_coreVoltage);
      }

      #region UpdateSensors
      public void UpdateSensors()
      {
        var node = Nodes[0];
        if (node == null)
          return;
        Core core = node.Cores[0];
        if (core == null)
          return;
        CPUID cpu = core.Threads[0];
        if (cpu == null)
          return;
        uint eax, edx;

        ulong mask = Ring0.ThreadAffinitySet(1UL << cpu.Thread);

        // MSRC001_0299
        // TU [19:16]
        // ESU [12:8] -> Unit 15.3 micro Joule per increment
        // PU [3:0]
        Ring0.Rdmsr(MSR_PWR_UNIT, out eax, out edx);
        int tu = (int)((eax >> 16) & 0xf);
        int esu = (int)((eax >> 12) & 0xf);
        int pu = (int)(eax & 0xf);

        // MSRC001_029B
        // total_energy [31:0]
        DateTime sample_time = DateTime.Now;
        Ring0.Rdmsr(MSR_PKG_ENERGY_STAT, out eax, out edx);
        uint total_energy = eax;

        // THM_TCON_CUR_TMP
        // CUR_TEMP [31:21]
        uint temperature = 0;
        Ring0.WritePciConfig(Ring0.GetPciAddress(0, 0, 0), FAMILY_17H_PCI_CONTROL_REGISTER, F17H_M01H_THM_TCON_CUR_TMP);
        Ring0.ReadPciConfig(Ring0.GetPciAddress(0, 0, 0), FAMILY_17H_PCI_CONTROL_REGISTER + 4, out temperature);

        // SVI0_TFN_PLANE0 [0]
        // SVI0_TFN_PLANE1 [1]
        uint smusvi0_tfn = 0;
        Ring0.WritePciConfig(Ring0.GetPciAddress(0, 0, 0), FAMILY_17H_PCI_CONTROL_REGISTER, F17H_M01H_SVI + 0x8);
        Ring0.ReadPciConfig(Ring0.GetPciAddress(0, 0, 0), FAMILY_17H_PCI_CONTROL_REGISTER + 4, out smusvi0_tfn);

        // SVI0_PLANE0_VDDCOR [24:16]
        // SVI0_PLANE0_IDDCOR [7:0]
        uint smusvi0_tel_plane0 = 0;
        Ring0.WritePciConfig(Ring0.GetPciAddress(0, 0, 0), FAMILY_17H_PCI_CONTROL_REGISTER, F17H_M01H_SVI + 0xc);
        Ring0.ReadPciConfig(Ring0.GetPciAddress(0, 0, 0), FAMILY_17H_PCI_CONTROL_REGISTER + 4, out smusvi0_tel_plane0);

        // SVI0_PLANE1_VDDCOR [24:16]
        // SVI0_PLANE1_IDDCOR [7:0]
        uint smusvi0_tel_plane1 = 0;
        Ring0.WritePciConfig(Ring0.GetPciAddress(0, 0, 0), FAMILY_17H_PCI_CONTROL_REGISTER, F17H_M01H_SVI + 0x10);
        Ring0.ReadPciConfig(Ring0.GetPciAddress(0, 0, 0), FAMILY_17H_PCI_CONTROL_REGISTER + 4, out smusvi0_tel_plane1);

        Ring0.ThreadAffinitySet(mask);

        // power consumption
        // power.Value = (float) ((double)pu * 0.125);
        // esu = 15.3 micro Joule per increment
        if (_lastPwrTime.Ticks == 0)
        {
          _lastPwrTime = sample_time;
          _lastPwrValue = total_energy;
        }
        // ticks diff
        TimeSpan time = sample_time - _lastPwrTime;
        long pwr;
        if (_lastPwrValue <= total_energy)
          pwr = total_energy - _lastPwrValue;
        else
          pwr = (0xffffffff - _lastPwrValue) + total_energy;

        // update for next sample
        _lastPwrTime = sample_time;
        _lastPwrValue = total_energy;

        double energy = 15.3e-6 * pwr;
        energy /= time.TotalSeconds;

        if (!double.IsNaN(energy))
          _packagePower.Value = (float)energy;

        // current temp Bit [31:21]
        //If bit 19 of the Temperature Control register is set, there is an additional offset of 49 degrees C.
        bool temp_offset_flag = false;
        if ((temperature & F17H_TEMP_OFFSET_FLAG) != 0)
          temp_offset_flag = true;
        temperature = (temperature >> 21) * 125;

        float offset = 0.0f;
        if (string.IsNullOrWhiteSpace(cpu.Name))
          offset = 0;
        else if (cpu.Name.Contains("1600X") || cpu.Name.Contains("1700X") || cpu.Name.Contains("1800X"))
          offset = -20.0f;
        else if (cpu.Name.Contains("1920X") || cpu.Name.Contains("1950X") || cpu.Name.Contains("1900X"))
          offset = -27.0f;
        else if (cpu.Name.Contains("2600X") || cpu.Name.Contains("2700X") || cpu.Name.Contains("2800X"))
          offset = -10.0f;
        else if (cpu.Name.Contains("1910") || cpu.Name.Contains("1920") || cpu.Name.Contains("1950"))
          offset = -10.0f;
        else if (cpu.Name.Contains("2920") || cpu.Name.Contains("2950") || cpu.Name.Contains("2970") || cpu.Name.Contains("2990"))
          offset = -27.0f;

        float t = (temperature * 0.001f);
        if (temp_offset_flag)
          t += -49.0f;

        _coreTemperatureTctl.Value = t;
        _coreTemperatureTdie.Value = t + offset;

        // voltage
        double VIDStep = 0.00625;
        double vcc;
        uint svi0_plane_x_vddcor;
        uint svi0_plane_x_iddcor;

        //Core
        if ((smusvi0_tfn & 0x01) == 0)
        {
          svi0_plane_x_vddcor = (smusvi0_tel_plane0 >> 16) & 0xff;
          svi0_plane_x_iddcor = smusvi0_tel_plane0 & 0xff;
          vcc = 1.550 - (double)VIDStep * svi0_plane_x_vddcor;
          _coreVoltage.Value = (float)vcc;
        }

        // SoC
        // not every zen cpu has this voltage
        if ((smusvi0_tfn & 0x02) == 0)
        {
          svi0_plane_x_vddcor = (smusvi0_tel_plane1 >> 16) & 0xff;
          svi0_plane_x_iddcor = smusvi0_tel_plane1 & 0xff;
          vcc = 1.550 - (double)VIDStep * svi0_plane_x_vddcor;
          _socVoltage.Value = (float)vcc;
          _hw.ActivateSensor(_socVoltage);
        }

      }
      #endregion

      public void AppendThread(CPUID thread, int numa_id, int core_id)
      {
        NumaNode node = null;
        foreach (var n in Nodes)
        {
          if (n.NodeId == numa_id)
            node = n;
        }
        if (node == null)
        {
          node = new NumaNode(_hw, numa_id);
          Nodes.Add(node);
        }
        if (thread != null)
          node.AppendThread(thread, core_id);
      }
    }
    #endregion

    #region NumaNode
    private class NumaNode
    {
      private AMD17CPU _hw = null;
      public int NodeId { get; private set; }
      public List<Core> Cores { get; private set; }

      public NumaNode(Hardware hw, int id)
      {
        Cores = new List<Core>();
        NodeId = id;
        _hw = (AMD17CPU)hw;
      }

      public void AppendThread(CPUID thread, int core_id)
      {
        Core core = null;
        foreach (var c in Cores)
        {
          if (c.CoreId == core_id)
            core = c;
        }
        if (core == null)
        {
          core = new Core(_hw, core_id);
          Cores.Add(core);
        }
        if (thread != null)
          core.Threads.Add(thread);
      }

      #region UpdateSensors
      public void UpdateSensors()
      {
      }
      #endregion
    }
    #endregion

    #region Core
    private class Core
    {
      private DateTime _lastPwrTime = new DateTime(0);
      private uint _lastPwrValue = 0;
      private AMD17CPU _hw = null;
      private Sensor _clock = null;
      private Sensor _vcore = null;
      private Sensor _power = null;
      private Sensor _multiplier = null;
      public int CoreId { get; private set; }
      public List<CPUID> Threads { get; private set; }

      public Core(Hardware hw, int id)
      {
        Threads = new List<CPUID>();
        CoreId = id;
        _hw = (AMD17CPU)hw;
        _clock = new Sensor("Core #" + CoreId.ToString(), _hw._sensorClock++, SensorType.Clock, _hw, _hw.settings);
        _multiplier = new Sensor("Core #" + CoreId.ToString(), _hw._sensorMulti++, SensorType.Factor, _hw, _hw.settings);
        _power = new Sensor("Core #" + CoreId.ToString() + " (SMU)", _hw._sensorPower++, SensorType.Power, _hw, _hw.settings);
        _vcore = new Sensor("Core #" + CoreId.ToString() + " VID", _hw._sensorVoltage++, SensorType.Voltage, _hw, _hw.settings);

        _hw.ActivateSensor(_clock);
        _hw.ActivateSensor(_multiplier);
        _hw.ActivateSensor(_power);
        _hw.ActivateSensor(_vcore);
      }

      #region UpdateSensors
      public void UpdateSensors()
      {
        // CPUID cpu = threads.FirstOrDefault();
        CPUID cpu = Threads[0];
        if (cpu == null)
          return;
        uint eax, edx;
        ulong mask = Ring0.ThreadAffinitySet(1UL << cpu.Thread);

        // MSRC001_0299
        // TU [19:16]
        // ESU [12:8] -> Unit 15.3 micro Joule per increment
        // PU [3:0]
        Ring0.Rdmsr(MSR_PWR_UNIT, out eax, out edx);
        int tu = (int)((eax >> 16) & 0xf);
        int esu = (int)((eax >> 12) & 0xf);
        int pu = (int)(eax & 0xf);

        // MSRC001_029A
        // total_energy [31:0]
        DateTime sample_time = DateTime.Now;
        Ring0.Rdmsr(MSR_CORE_ENERGY_STAT, out eax, out edx);
        uint total_energy = eax;

        // MSRC001_0293
        // CurHwPstate [24:22]
        // CurCpuVid [21:14]
        // CurCpuDfsId [13:8]
        // CurCpuFid [7:0]
        Ring0.Rdmsr(MSR_HARDWARE_PSTATE_STATUS, out eax, out edx);
        int CurHwPstate = (int)((eax >> 22) & 0x3);
        int CurCpuVid = (int)((eax >> 14) & 0xff);
        int CurCpuDfsId = (int)((eax >> 8) & 0x3f);
        int CurCpuFid = (int)(eax & 0xff);

        // MSRC001_0064 + x
        // IddDiv [31:30]
        // IddValue [29:22]
        // CpuVid [21:14]
        // CpuDfsId [13:8]
        // CpuFid [7:0]
        // Ring0.Rdmsr(MSR_PSTATE_0 + (uint)CurHwPstate, out eax, out edx);
        // int IddDiv = (int)((eax >> 30) & 0x03);
        // int IddValue = (int)((eax >> 22) & 0xff);
        // int CpuVid = (int)((eax >> 14) & 0xff);
        Ring0.ThreadAffinitySet(mask);

        // clock
        // CoreCOF is (Core::X86::Msr::PStateDef[CpuFid[7:0]] / Core::X86::Msr::PStateDef[CpuDfsId]) * 200
        _clock.Value = (float)((double)CurCpuFid / (double)CurCpuDfsId * 200.0);

        // multiplier
        _multiplier.Value = (float)((double)CurCpuFid / (double)CurCpuDfsId * 2.0);

        // Voltage
        double VIDStep = 0.00625;
        double vcc = 1.550 - (double)VIDStep * CurCpuVid;
        _vcore.Value = (float)vcc;

        // power consumption
        // power.Value = (float) ((double)pu * 0.125);
        // esu = 15.3 micro Joule per increment
        if (_lastPwrTime.Ticks == 0)
        {
          _lastPwrTime = sample_time;
          _lastPwrValue = total_energy;
        }
        // ticks diff
        TimeSpan time = sample_time - _lastPwrTime;
        long pwr;
        if (_lastPwrValue <= total_energy)
          pwr = total_energy - _lastPwrValue;
        else
          pwr = (0xffffffff - _lastPwrValue) + total_energy;

        // update for next sample
        _lastPwrTime = sample_time;
        _lastPwrValue = total_energy;

        double energy = 15.3e-6 * pwr;
        energy /= time.TotalSeconds;

        if (!double.IsNaN(energy))
          _power.Value = (float)energy;
      }
      #endregion
    }
    #endregion

    private Processor _ryzen = null;

    public AMD17CPU(int processorIndex, CPUID[][] cpuid, ISettings settings)
      : base(processorIndex, cpuid, settings)
    {
      // add all numa nodes
      // Register ..1E_ECX, [10:8] + 1
      _ryzen = new Processor(this);
      int NodesPerProcessor = 1 + (int)((cpuid[0][0].ExtData[0x1e, ECX] >> 8) & 0x7);

      // add all numa nodes
      foreach (CPUID[] cpu in cpuid)
      {
        CPUID thread = cpu[0];

        // coreID
        // Register ..1E_EBX, [7:0]
        int core_id = (int)(thread.ExtData[0x1e, EBX] & 0xff);

        // nodeID
        // Register ..1E_ECX, [7:0]
        int node_id = (int)(thread.ExtData[0x1e, ECX] & 0xff);

        _ryzen.AppendThread(null, node_id, core_id);
      }

      // add all threads to numa nodes and specific core
      foreach (CPUID[] cpu in cpuid)
      {
        CPUID thread = cpu[0];

        // coreID
        // Register ..1E_EBX, [7:0]
        int core_id = (int)(thread.ExtData[0x1e, EBX] & 0xff);

        // nodeID
        // Register ..1E_ECX, [7:0]
        int node_id = (int)(thread.ExtData[0x1e, ECX] & 0xff);

        _ryzen.AppendThread(thread, node_id, core_id);
      }
      Update();
    }

    protected override uint[] GetMSRs()
    {
      return new uint[] { PERF_CTL_0, PERF_CTR_0, HWCR, MSR_PSTATE_0, COFVID_STATUS };
    }

    public override string GetReport()
    {
      StringBuilder r = new StringBuilder();
      r.Append(base.GetReport());
      r.Append("Ryzen");
      return r.ToString();
    }

    private string ReadFirstLine(Stream stream)
    {
      StringBuilder sb = new StringBuilder();
      try
      {
        stream.Seek(0, SeekOrigin.Begin);
        int b = stream.ReadByte();
        while (b != -1 && b != 10)
        {
          sb.Append((char)b);
          b = stream.ReadByte();
        }
      }
      catch { }
      return sb.ToString();
    }

    public override void Update()
    {
      base.Update();

      _ryzen.UpdateSensors();
      foreach (NumaNode node in _ryzen.Nodes)
      {
        node.UpdateSensors();

        foreach (Core c in node.Cores)
        {
          c.UpdateSensors();
        }
      }
    }

    public override void Close()
    {
      base.Close();
    }
  }
}
