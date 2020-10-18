// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Globalization;
using System.Text;
using LibreHardwareMonitor.Interop;

namespace LibreHardwareMonitor.Hardware.Gpu
{
    internal sealed class AmdGpu : Hardware
    {
        private readonly int _adapterIndex;

        private readonly IntPtr _context = IntPtr.Zero;

        private readonly Sensor _controlSensor;
        private readonly Sensor _coreClock;
        private readonly Sensor _coreLoad;
        private readonly Sensor _coreVoltage;
        private readonly int _currentOverdriveApiLevel;
        private readonly Sensor _fan;
        private readonly Control _fanControl;
        private readonly Sensor _memoryClock;
        private readonly Sensor _memoryLoad;
        private readonly Sensor _memoryVoltage;
        private readonly Sensor _powerCore;
        private readonly Sensor _powerPpt;
        private readonly Sensor _powerSoC;
        private readonly Sensor _powerTotal;
        private readonly Sensor _socClock;
        private readonly Sensor _socVoltage;
        private readonly Sensor _temperatureCore;
        private readonly Sensor _temperatureHotSpot;
        private readonly Sensor _temperatureLiquid;
        private readonly Sensor _temperatureMemory;
        private readonly Sensor _temperatureMvdd;
        private readonly Sensor _temperaturePlx;
        private readonly Sensor _temperatureSoC;
        private readonly Sensor _temperatureVddc;

        public AmdGpu(string name, int adapterIndex, int busNumber, int deviceNumber, ISettings settings)
            : base(name, new Identifier("gpu-amd", adapterIndex.ToString(CultureInfo.InvariantCulture)), settings)
        {
            _adapterIndex = adapterIndex;
            BusNumber = busNumber;
            DeviceNumber = deviceNumber;

            _temperatureCore = new Sensor("GPU Core", 0, SensorType.Temperature, this, settings);
            _temperatureMemory = new Sensor("GPU Memory", 1, SensorType.Temperature, this, settings);
            _temperatureVddc = new Sensor("GPU VR VDDC", 2, SensorType.Temperature, this, settings);
            _temperatureMvdd = new Sensor("GPU VR MVDD", 3, SensorType.Temperature, this, settings);
            _temperatureSoC = new Sensor("GPU VR SoC", 4, SensorType.Temperature, this, settings);
            _temperatureLiquid = new Sensor("GPU Liquid", 5, SensorType.Temperature, this, settings);
            _temperaturePlx = new Sensor("GPU PLX", 6, SensorType.Temperature, this, settings);
            _temperatureHotSpot = new Sensor("GPU Hot Spot", 7, SensorType.Temperature, this, settings);

            _coreClock = new Sensor("GPU Core", 0, SensorType.Clock, this, settings);
            _socClock = new Sensor("GPU SoC", 1, SensorType.Clock, this, settings);
            _memoryClock = new Sensor("GPU Memory", 2, SensorType.Clock, this, settings);

            _fan = new Sensor("GPU Fan", 0, SensorType.Fan, this, settings);

            _coreVoltage = new Sensor("GPU Core", 0, SensorType.Voltage, this, settings);
            _memoryVoltage = new Sensor("GPU Memory", 1, SensorType.Voltage, this, settings);
            _socVoltage = new Sensor("GPU SoC", 2, SensorType.Voltage, this, settings);

            _coreLoad = new Sensor("GPU Core", 0, SensorType.Load, this, settings);
            _memoryLoad = new Sensor("GPU Memory", 1, SensorType.Load, this, settings);

            _controlSensor = new Sensor("GPU Fan", 0, SensorType.Control, this, settings);

            _powerCore = new Sensor("GPU Core", 0, SensorType.Power, this, settings);
            _powerPpt = new Sensor("GPU PPT", 1, SensorType.Power, this, settings);
            _powerSoC = new Sensor("GPU SoC", 2, SensorType.Power, this, settings);
            _powerTotal = new Sensor("GPU Package", 3, SensorType.Power, this, settings);

            int supported = 0;
            int enabled = 0;
            int version = 0;

            if (AtiAdlxx.ADL_Overdrive_Caps(1, ref supported, ref enabled, ref version) == AtiAdlxx.ADLStatus.ADL_OK)
                _currentOverdriveApiLevel = version;
            else
                _currentOverdriveApiLevel = -1;

            if (_currentOverdriveApiLevel >= 6)
            {
                if (AtiAdlxx.ADL2_Main_Control_Create(AtiAdlxx.Main_Memory_Alloc, adapterIndex, ref _context) == AtiAdlxx.ADLStatus.ADL_OK)
                    _context = IntPtr.Zero;
            }

            AtiAdlxx.ADLFanSpeedInfo fanSpeedInfo = new AtiAdlxx.ADLFanSpeedInfo();
            if (AtiAdlxx.ADL_Overdrive5_FanSpeedInfo_Get(adapterIndex, 0, ref fanSpeedInfo) != AtiAdlxx.ADLStatus.ADL_OK)
            {
                fanSpeedInfo.MaxPercent = 100;
                fanSpeedInfo.MinPercent = 0;
            }

            _fanControl = new Control(_controlSensor, settings, fanSpeedInfo.MinPercent, fanSpeedInfo.MaxPercent);
            _fanControl.ControlModeChanged += ControlModeChanged;
            _fanControl.SoftwareControlValueChanged += SoftwareControlValueChanged;
            ControlModeChanged(_fanControl);
            _controlSensor.Control = _fanControl;
            Update();
        }

        public int BusNumber { get; }

        public int DeviceNumber { get; }

        public override HardwareType HardwareType
        {
            get { return HardwareType.GpuAmd; }
        }

        private void SoftwareControlValueChanged(IControl control)
        {
            if (control.ControlMode == ControlMode.Software)
            {
                AtiAdlxx.ADLFanSpeedValue fanSpeedValue = new AtiAdlxx.ADLFanSpeedValue
                {
                    SpeedType = AtiAdlxx.ADL_DL_FANCTRL_SPEED_TYPE_PERCENT,
                    Flags = AtiAdlxx.ADL_DL_FANCTRL_FLAG_USER_DEFINED_SPEED,
                    FanSpeed = (int)control.SoftwareValue
                };

                AtiAdlxx.ADL_Overdrive5_FanSpeed_Set(_adapterIndex, 0, ref fanSpeedValue);
            }
        }

        private void ControlModeChanged(IControl control)
        {
            switch (control.ControlMode)
            {
                case ControlMode.Undefined:
                    return;
                case ControlMode.Default:
                    SetDefaultFanSpeed();
                    break;
                case ControlMode.Software:
                    SoftwareControlValueChanged(control);
                    break;
                default:
                    return;
            }
        }

        /// <summary>
        /// Sets the default fan speed.
        /// </summary>
        private void SetDefaultFanSpeed()
        {
            AtiAdlxx.ADL_Overdrive5_FanSpeedToDefault_Set(_adapterIndex, 0);
        }

        public override void Update()
        {
            if (_currentOverdriveApiLevel < 8)
            {
                if (_currentOverdriveApiLevel >= 6)
                {
                    GetOD6Power(AtiAdlxx.ADLODNCurrentPowerType.ODN_GPU_TOTAL_POWER, _powerTotal);
                    GetOD6Power(AtiAdlxx.ADLODNCurrentPowerType.ODN_GPU_PPT_POWER, _powerPpt);
                    GetOD6Power(AtiAdlxx.ADLODNCurrentPowerType.ODN_GPU_SOCKET_POWER, _powerSoC);
                    GetOD6Power(AtiAdlxx.ADLODNCurrentPowerType.ODN_GPU_CHIP_POWER, _powerCore);
                }

                if (_currentOverdriveApiLevel >= 7)
                {
                    GetODNTemperature(AtiAdlxx.ADLODNTemperatureType.EDGE, _temperatureCore, -200, 0.001);
                    GetODNTemperature(AtiAdlxx.ADLODNTemperatureType.MEM, _temperatureMemory, 0);
                    GetODNTemperature(AtiAdlxx.ADLODNTemperatureType.VRVDDC, _temperatureVddc, 0);
                    GetODNTemperature(AtiAdlxx.ADLODNTemperatureType.VRMVDD, _temperatureMvdd, 0);
                    GetODNTemperature(AtiAdlxx.ADLODNTemperatureType.LIQUID, _temperatureLiquid, 0);
                    GetODNTemperature(AtiAdlxx.ADLODNTemperatureType.PLX, _temperaturePlx, 0);
                    GetODNTemperature(AtiAdlxx.ADLODNTemperatureType.HOTSPOT, _temperatureHotSpot, 0);
                }
                else
                {
                    AtiAdlxx.ADLTemperature temperature = new AtiAdlxx.ADLTemperature();
                    if (AtiAdlxx.ADL_Overdrive5_Temperature_Get(_adapterIndex, 0, ref temperature) == AtiAdlxx.ADLStatus.ADL_OK)
                    {
                        _temperatureCore.Value = 0.001f * temperature.Temperature;
                        ActivateSensor(_temperatureCore);
                    }
                    else
                    {
                        _temperatureCore.Value = null;
                    }
                }

                AtiAdlxx.ADLFanSpeedValue fanSpeedValue = new AtiAdlxx.ADLFanSpeedValue { SpeedType = AtiAdlxx.ADL_DL_FANCTRL_SPEED_TYPE_RPM };
                if (AtiAdlxx.ADL_Overdrive5_FanSpeed_Get(_adapterIndex, 0, ref fanSpeedValue) == AtiAdlxx.ADLStatus.ADL_OK)
                {
                    _fan.Value = fanSpeedValue.FanSpeed;
                    ActivateSensor(_fan);
                }
                else
                {
                    _fan.Value = null;
                }

                fanSpeedValue = new AtiAdlxx.ADLFanSpeedValue { SpeedType = AtiAdlxx.ADL_DL_FANCTRL_SPEED_TYPE_PERCENT };
                if (AtiAdlxx.ADL_Overdrive5_FanSpeed_Get(_adapterIndex, 0, ref fanSpeedValue) == AtiAdlxx.ADLStatus.ADL_OK)
                {
                    _controlSensor.Value = fanSpeedValue.FanSpeed;
                    ActivateSensor(_controlSensor);
                }
                else
                {
                    _controlSensor.Value = null;
                }

                AtiAdlxx.ADLPMActivity adlpmActivity = new AtiAdlxx.ADLPMActivity();
                if (AtiAdlxx.ADL_Overdrive5_CurrentActivity_Get(_adapterIndex, ref adlpmActivity) == AtiAdlxx.ADLStatus.ADL_OK)
                {
                    if (adlpmActivity.EngineClock > 0)
                    {
                        _coreClock.Value = 0.01f * adlpmActivity.EngineClock;
                        ActivateSensor(_coreClock);
                    }
                    else
                    {
                        _coreClock.Value = null;
                    }

                    if (adlpmActivity.MemoryClock > 0)
                    {
                        _memoryClock.Value = 0.01f * adlpmActivity.MemoryClock;
                        ActivateSensor(_memoryClock);
                    }
                    else
                    {
                        _memoryClock.Value = null;
                    }

                    if (adlpmActivity.Vddc > 0)
                    {
                        _coreVoltage.Value = 0.001f * adlpmActivity.Vddc;
                        ActivateSensor(_coreVoltage);
                    }
                    else
                    {
                        _coreVoltage.Value = null;
                    }

                    _coreLoad.Value = Math.Min(adlpmActivity.ActivityPercent, 100);
                    ActivateSensor(_coreLoad);
                }
                else
                {
                    _coreClock.Value = null;
                    _memoryClock.Value = null;
                    _coreVoltage.Value = null;
                    _coreLoad.Value = null;
                }
            }
            else
            {
                AtiAdlxx.ADLPMLogDataOutput logDataOutput = new AtiAdlxx.ADLPMLogDataOutput();
                if (AtiAdlxx.ADL2_New_QueryPMLogData_Get(_context, _adapterIndex, ref logDataOutput) == AtiAdlxx.ADLStatus.ADL_OK)
                {
                    GetPMLog(logDataOutput, AtiAdlxx.ADLSensorType.PMLOG_TEMPERATURE_EDGE, _temperatureCore);
                    GetPMLog(logDataOutput, AtiAdlxx.ADLSensorType.PMLOG_TEMPERATURE_HOTSPOT, _temperatureHotSpot);
                    GetPMLog(logDataOutput, AtiAdlxx.ADLSensorType.PMLOG_TEMPERATURE_VRVDDC, _temperatureVddc);
                    GetPMLog(logDataOutput, AtiAdlxx.ADLSensorType.PMLOG_TEMPERATURE_MEM, _temperatureMemory);
                    GetPMLog(logDataOutput, AtiAdlxx.ADLSensorType.PMLOG_TEMPERATURE_VRMVDD, _temperatureMvdd);
                    GetPMLog(logDataOutput, AtiAdlxx.ADLSensorType.PMLOG_TEMPERATURE_LIQUID, _temperatureLiquid);
                    GetPMLog(logDataOutput, AtiAdlxx.ADLSensorType.PMLOG_TEMPERATURE_PLX, _temperaturePlx);
                    GetPMLog(logDataOutput, AtiAdlxx.ADLSensorType.PMLOG_TEMPERATURE_SOC, _temperatureSoC);

                    GetPMLog(logDataOutput, AtiAdlxx.ADLSensorType.PMLOG_CLK_GFXCLK, _coreClock);
                    GetPMLog(logDataOutput, AtiAdlxx.ADLSensorType.PMLOG_CLK_SOCCLK, _socClock);
                    GetPMLog(logDataOutput, AtiAdlxx.ADLSensorType.PMLOG_CLK_MEMCLK, _memoryClock);

                    const int fanRpmIndex = (int)AtiAdlxx.ADLSensorType.PMLOG_FAN_RPM;
                    const int fanPercentageIndex = (int)AtiAdlxx.ADLSensorType.PMLOG_FAN_PERCENTAGE;

                    if (fanRpmIndex < logDataOutput.sensors.Length && fanPercentageIndex < logDataOutput.sensors.Length && logDataOutput.sensors[fanRpmIndex].value != ushort.MaxValue)
                    {
                        _fan.Value = logDataOutput.sensors[fanRpmIndex].value;
                        _controlSensor.Value = logDataOutput.sensors[fanPercentageIndex].value;

                        ActivateSensor(_fan);
                        ActivateSensor(_controlSensor);
                    }
                    else
                    {
                        _fan.Value = null;
                        _controlSensor.Value = null;
                    }

                    GetPMLog(logDataOutput, AtiAdlxx.ADLSensorType.PMLOG_GFX_VOLTAGE, _coreVoltage, 0.001f);
                    GetPMLog(logDataOutput, AtiAdlxx.ADLSensorType.PMLOG_SOC_VOLTAGE, _socVoltage, 0.001f);
                    GetPMLog(logDataOutput, AtiAdlxx.ADLSensorType.PMLOG_MEM_VOLTAGE, _memoryVoltage, 0.001f);

                    GetPMLog(logDataOutput, AtiAdlxx.ADLSensorType.PMLOG_INFO_ACTIVITY_GFX, _coreLoad);
                    GetPMLog(logDataOutput, AtiAdlxx.ADLSensorType.PMLOG_INFO_ACTIVITY_MEM, _memoryLoad);

                    GetPMLog(logDataOutput, AtiAdlxx.ADLSensorType.PMLOG_GFX_POWER, _powerCore);
                    GetPMLog(logDataOutput, AtiAdlxx.ADLSensorType.PMLOG_SOC_POWER, _powerSoC);
                    GetPMLog(logDataOutput, AtiAdlxx.ADLSensorType.PMLOG_ASIC_POWER, _powerTotal);
                }
            }
        }

        /// <summary>
        /// Gets the OverdriveN temperature.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="sensor">The sensor.</param>
        private void GetODNTemperature(AtiAdlxx.ADLODNTemperatureType type, Sensor sensor, double minTemperature = -200, double scale = 1)
        {
            // If a sensor isn't available, some cards report 54000 degrees C. 110C is expected for Navi, so 100 more than that should be enough to use as a maximum.
            int maxTemperature = (int)(210.0 / scale);
            minTemperature = (int)(minTemperature / scale);

            int temperature = 0;
            if (AtiAdlxx.ADL2_OverdriveN_Temperature_Get(_context, _adapterIndex, type, ref temperature) == AtiAdlxx.ADLStatus.ADL_OK && temperature > minTemperature && temperature < maxTemperature)
            {
                sensor.Value = (float)(scale * temperature);
                ActivateSensor(sensor);
            }
            else
            {
                sensor.Value = null;
            }
        }

        /// <summary>
        /// Gets a PMLog sensor value.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="sensorType">Type of the sensor.</param>
        /// <param name="sensor">The sensor.</param>
        /// <param name="factor">The factor.</param>
        private void GetPMLog(AtiAdlxx.ADLPMLogDataOutput data, AtiAdlxx.ADLSensorType sensorType, Sensor sensor, float factor = 1.0f)
        {
            int i = (int)sensorType;
            if (i < data.sensors.Length && data.sensors[i].supported != 0)
            {
                sensor.Value = data.sensors[i].value * factor;
                ActivateSensor(sensor);
            }
            else
            {
                sensor.Value = null;
            }
        }

        /// <summary>
        /// Gets the Overdrive6 power.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="sensor">The sensor.</param>
        private void GetOD6Power(AtiAdlxx.ADLODNCurrentPowerType type, Sensor sensor)
        {
            int powerOf8 = 0;
            if (AtiAdlxx.ADL2_Overdrive6_CurrentPower_Get(_context, _adapterIndex, type, ref powerOf8) == AtiAdlxx.ADLStatus.ADL_OK)
            {
                sensor.Value = powerOf8 >> 8;
                ActivateSensor(sensor);
            }
            else
            {
                sensor.Value = null;
            }
        }

        public override void Close()
        {
            _fanControl.ControlModeChanged -= ControlModeChanged;
            _fanControl.SoftwareControlValueChanged -= SoftwareControlValueChanged;

            if (_fanControl.ControlMode != ControlMode.Undefined)
                SetDefaultFanSpeed();

            if (_context != IntPtr.Zero)
                AtiAdlxx.ADL2_Main_Control_Destroy(_context);

            base.Close();
        }

        public override string GetReport()
        {
            var r = new StringBuilder();

            r.AppendLine("AMD GPU");
            r.AppendLine();

            r.Append("AdapterIndex: ");
            r.AppendLine(_adapterIndex.ToString(CultureInfo.InvariantCulture));
            r.AppendLine();

            r.AppendLine("Overdrive Caps");
            r.AppendLine();
            try
            {
                int supported = 0;
                int enabled = 0;
                int version = 0;
                AtiAdlxx.ADLStatus status = AtiAdlxx.ADL_Overdrive_Caps(_adapterIndex, ref supported, ref enabled, ref version);
                r.Append(" Status: ");
                r.AppendLine(status.ToString());
                r.Append(" Supported: ");
                r.AppendLine(supported.ToString(CultureInfo.InvariantCulture));
                r.Append(" Enabled: ");
                r.AppendLine(enabled.ToString(CultureInfo.InvariantCulture));
                r.Append(" Version: ");
                r.AppendLine(version.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception e)
            {
                r.AppendLine(" Status: " + e.Message);
            }

            r.AppendLine();

            r.AppendLine("Overdrive5 Parameters");
            r.AppendLine();
            try
            {
                AtiAdlxx.ADLStatus status = AtiAdlxx.ADL_Overdrive5_ODParameters_Get(_adapterIndex, out AtiAdlxx.ADLODParameters p);
                r.Append(" Status: ");
                r.AppendLine(status.ToString());
                r.AppendFormat(" NumberOfPerformanceLevels: {0}{1}", p.NumberOfPerformanceLevels, Environment.NewLine);
                r.AppendFormat(" ActivityReportingSupported: {0}{1}", p.ActivityReportingSupported, Environment.NewLine);
                r.AppendFormat(" DiscretePerformanceLevels: {0}{1}", p.DiscretePerformanceLevels, Environment.NewLine);
                r.AppendFormat(" EngineClock.Min: {0}{1}", p.EngineClock.Min, Environment.NewLine);
                r.AppendFormat(" EngineClock.Max: {0}{1}", p.EngineClock.Max, Environment.NewLine);
                r.AppendFormat(" EngineClock.Step: {0}{1}", p.EngineClock.Step, Environment.NewLine);
                r.AppendFormat(" MemoryClock.Min: {0}{1}", p.MemoryClock.Min, Environment.NewLine);
                r.AppendFormat(" MemoryClock.Max: {0}{1}", p.MemoryClock.Max, Environment.NewLine);
                r.AppendFormat(" MemoryClock.Step: {0}{1}", p.MemoryClock.Step, Environment.NewLine);
                r.AppendFormat(" Vddc.Min: {0}{1}", p.Vddc.Min, Environment.NewLine);
                r.AppendFormat(" Vddc.Max: {0}{1}", p.Vddc.Max, Environment.NewLine);
                r.AppendFormat(" Vddc.Step: {0}{1}", p.Vddc.Step, Environment.NewLine);
            }
            catch (Exception e)
            {
                r.AppendLine(" Status: " + e.Message);
            }

            r.AppendLine();

            r.AppendLine("Overdrive5 Temperature");
            r.AppendLine();
            try
            {
                var adlt = new AtiAdlxx.ADLTemperature();
                AtiAdlxx.ADLStatus status = AtiAdlxx.ADL_Overdrive5_Temperature_Get(_adapterIndex, 0, ref adlt);
                r.Append(" Status: ");
                r.AppendLine(status.ToString());
                r.AppendFormat(" Value: {0}{1}", 0.001f * adlt.Temperature, Environment.NewLine);
            }
            catch (Exception e)
            {
                r.AppendLine(" Status: " + e.Message);
            }

            r.AppendLine();

            r.AppendLine("Overdrive5 FanSpeed");
            r.AppendLine();
            try
            {
                var adlf = new AtiAdlxx.ADLFanSpeedValue { SpeedType = AtiAdlxx.ADL_DL_FANCTRL_SPEED_TYPE_RPM };
                var status = AtiAdlxx.ADL_Overdrive5_FanSpeed_Get(_adapterIndex, 0, ref adlf);
                r.Append(" Status RPM: ");
                r.AppendLine(status.ToString());
                r.AppendFormat(" Value RPM: {0}{1}", adlf.FanSpeed, Environment.NewLine);

                adlf.SpeedType = AtiAdlxx.ADL_DL_FANCTRL_SPEED_TYPE_PERCENT;
                status = AtiAdlxx.ADL_Overdrive5_FanSpeed_Get(_adapterIndex, 0, ref adlf);
                r.Append(" Status Percent: ");
                r.AppendLine(status.ToString());
                r.AppendFormat(" Value Percent: {0}{1}", adlf.FanSpeed, Environment.NewLine);
            }
            catch (Exception e)
            {
                r.AppendLine(" Status: " + e.Message);
            }

            r.AppendLine();

            r.AppendLine("Overdrive5 CurrentActivity");
            r.AppendLine();
            try
            {
                var adlp = new AtiAdlxx.ADLPMActivity();
                AtiAdlxx.ADLStatus status = AtiAdlxx.ADL_Overdrive5_CurrentActivity_Get(_adapterIndex, ref adlp);

                r.Append(" Status: ");
                r.AppendLine(status.ToString());
                r.AppendFormat(" EngineClock: {0}{1}", 0.01f * adlp.EngineClock, Environment.NewLine);
                r.AppendFormat(" MemoryClock: {0}{1}", 0.01f * adlp.MemoryClock, Environment.NewLine);
                r.AppendFormat(" Vddc: {0}{1}", 0.001f * adlp.Vddc, Environment.NewLine);
                r.AppendFormat(" ActivityPercent: {0}{1}", adlp.ActivityPercent, Environment.NewLine);
                r.AppendFormat(" CurrentPerformanceLevel: {0}{1}", adlp.CurrentPerformanceLevel, Environment.NewLine);
                r.AppendFormat(" CurrentBusSpeed: {0}{1}", adlp.CurrentBusSpeed, Environment.NewLine);
                r.AppendFormat(" CurrentBusLanes: {0}{1}", adlp.CurrentBusLanes, Environment.NewLine);
                r.AppendFormat(" MaximumBusLanes: {0}{1}", adlp.MaximumBusLanes, Environment.NewLine);
            }
            catch (Exception e)
            {
                r.AppendLine(" Status: " + e.Message);
            }

            r.AppendLine();

            if (_context != IntPtr.Zero)
            {
                r.AppendLine("Overdrive6 CurrentPower");
                r.AppendLine();
                try
                {
                    int power = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        string pt = ((AtiAdlxx.ADLODNCurrentPowerType)i).ToString();
                        AtiAdlxx.ADLStatus status = AtiAdlxx.ADL2_Overdrive6_CurrentPower_Get(_context, _adapterIndex, (AtiAdlxx.ADLODNCurrentPowerType)i, ref power);

                        if (status == AtiAdlxx.ADLStatus.ADL_OK)
                        {
                            r.AppendFormat(" Power[{0}].Value: {1}{2}", pt, power * (1.0f / 0xFF), Environment.NewLine);
                        }
                        else
                        {
                            r.AppendFormat(" Power[{0}].Status: {1}{2}", pt, status.ToString(), Environment.NewLine);
                        }
                    }
                }
                catch (EntryPointNotFoundException)
                {
                    r.AppendLine(" Status: Entry point not found");
                }
                catch (Exception e)
                {
                    r.AppendLine(" Status: " + e.Message);
                }

                r.AppendLine();
            }

            if (_context != IntPtr.Zero)
            {
                r.AppendLine("OverdriveN Temperature");
                r.AppendLine();
                try
                {
                    for (int i = 1; i < 8; i++)
                    {
                        int temperature = 0;
                        string tt = ((AtiAdlxx.ADLODNTemperatureType)i).ToString();
                        AtiAdlxx.ADLStatus status = AtiAdlxx.ADL2_OverdriveN_Temperature_Get(_context, _adapterIndex, (AtiAdlxx.ADLODNTemperatureType)i, ref temperature);
                        if (status == AtiAdlxx.ADLStatus.ADL_OK)
                        {
                            r.AppendFormat(" Temperature[{0}].Value: {1}{2}", tt, 0.001f * temperature, Environment.NewLine);
                        }
                        else
                        {
                            r.AppendFormat(" Temperature[{0}].Status: {1}{2}", tt, status.ToString(), Environment.NewLine);
                        }
                    }
                }
                catch (EntryPointNotFoundException)
                {
                    r.AppendLine(" Status: Entry point not found");
                }
                catch (Exception e)
                {
                    r.AppendLine(" Status: " + e.Message);
                }

                r.AppendLine();
            }

            if (_context != IntPtr.Zero)
            {
                r.AppendLine("OverdriveN Performance Status");
                r.AppendLine();
                try
                {
                    var status = AtiAdlxx.ADL2_OverdriveN_PerformanceStatus_Get(_context, _adapterIndex, out var ps);
                    r.Append(" Status: ");
                    r.AppendLine(status.ToString());
                    r.AppendFormat(" CoreClock: {0}{1}", ps.CoreClock, Environment.NewLine);
                    r.AppendFormat(" MemoryClock: {0}{1}", ps.MemoryClock, Environment.NewLine);
                    r.AppendFormat(" DCEFClock: {0}{1}", ps.DCEFClock, Environment.NewLine);
                    r.AppendFormat(" GFXClock: {0}{1}", ps.GFXClock, Environment.NewLine);
                    r.AppendFormat(" UVDClock: {0}{1}", ps.UVDClock, Environment.NewLine);
                    r.AppendFormat(" VCEClock: {0}{1}", ps.VCEClock, Environment.NewLine);
                    r.AppendFormat(" GPUActivityPercent: {0}{1}", ps.GPUActivityPercent, Environment.NewLine);
                    r.AppendFormat(" CurrentCorePerformanceLevel: {0}{1}", ps.CurrentCorePerformanceLevel, Environment.NewLine);
                    r.AppendFormat(" CurrentMemoryPerformanceLevel: {0}{1}", ps.CurrentMemoryPerformanceLevel, Environment.NewLine);
                    r.AppendFormat(" CurrentDCEFPerformanceLevel: {0}{1}", ps.CurrentDCEFPerformanceLevel, Environment.NewLine);
                    r.AppendFormat(" CurrentGFXPerformanceLevel: {0}{1}", ps.CurrentGFXPerformanceLevel, Environment.NewLine);
                    r.AppendFormat(" UVDPerformanceLevel: {0}{1}", ps.UVDPerformanceLevel, Environment.NewLine);
                    r.AppendFormat(" VCEPerformanceLevel: {0}{1}", ps.VCEPerformanceLevel, Environment.NewLine);
                    r.AppendFormat(" CurrentBusSpeed: {0}{1}", ps.CurrentBusSpeed, Environment.NewLine);
                    r.AppendFormat(" CurrentBusLanes: {0}{1}", ps.CurrentBusLanes, Environment.NewLine);
                    r.AppendFormat(" MaximumBusLanes: {0}{1}", ps.MaximumBusLanes, Environment.NewLine);
                    r.AppendFormat(" VDDC: {0}{1}", ps.VDDC, Environment.NewLine);
                    r.AppendFormat(" VDDCI: {0}{1}", ps.VDDCI, Environment.NewLine);
                }
                catch (EntryPointNotFoundException)
                {
                    r.AppendLine(" Status: Entry point not found");
                }
                catch (Exception e)
                {
                    r.AppendLine(" Status: " + e.Message);
                }

                r.AppendLine();
            }

            if (_context != IntPtr.Zero)
            {
                r.AppendLine("Performance Metrics");
                r.AppendLine();
                try
                {
                    var data = new AtiAdlxx.ADLPMLogDataOutput();
                    AtiAdlxx.ADLStatus status = AtiAdlxx.ADL2_New_QueryPMLogData_Get(_context, _adapterIndex, ref data);
                    if (status == AtiAdlxx.ADLStatus.ADL_OK)
                    {
                        for (int i = 0; i < data.sensors.Length; i++)
                        {
                            if (data.sensors[i].supported != 0)
                            {
                                string st = ((AtiAdlxx.ADLSensorType)i).ToString();
                                r.AppendFormat(" Sensor[{0}].Value: {1}{2}", st, data.sensors[i].value, Environment.NewLine);
                            }
                        }
                    }
                    else
                    {
                        r.Append(" Status: ");
                        r.AppendLine(status.ToString());
                    }
                }
                catch (EntryPointNotFoundException)
                {
                    r.AppendLine(" Status: Entry point not found");
                }
                catch (Exception e)
                {
                    r.AppendLine(" Status: " + e.Message);
                }

                r.AppendLine();
            }

            return r.ToString();
        }
    }
}
