// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Globalization;
using LibreHardwareMonitor.Interop;

namespace LibreHardwareMonitor.Hardware.Gpu
{
    internal sealed class AmdGpu : Hardware
    {
        private readonly int _adapterIndex;
        private readonly Sensor _controlSensor;
        private readonly Sensor _coreClock;
        private readonly Sensor _socClock;
        private readonly Sensor _coreLoad;
        private readonly Sensor _coreVoltage;
        private readonly int _currentOverdriveApiLevel;
        private readonly Sensor _fan;
        private readonly Control _fanControl;
        private readonly Sensor _memoryClock;
        private readonly Sensor _memoryVoltage;

        private readonly Sensor _powerCore;
        private readonly Sensor _powerPpt;
        private readonly Sensor _powerSocket;
        private readonly Sensor _powerTotal;

        private readonly Sensor _temperatureCore;
        private readonly Sensor _temperatureMemory;
        private readonly Sensor _temperatureHotSpot;
        private readonly Sensor _temperatureLiquid;
        private readonly Sensor _temperatureMvdd;
        private readonly Sensor _temperaturePlx;
        private readonly Sensor _temperatureVddc;

        private readonly IntPtr _context = IntPtr.Zero;

        public AmdGpu(string name, int adapterIndex, int busNumber, int deviceNumber, ISettings settings)
            : base(name, new Identifier("gpu", adapterIndex.ToString(CultureInfo.InvariantCulture)), settings)
        {
            _adapterIndex = adapterIndex;
            BusNumber = busNumber;
            DeviceNumber = deviceNumber;

            _temperatureCore = new Sensor("GPU Core", 0, SensorType.Temperature, this, settings);
            _temperatureMemory = new Sensor("GPU Memory", 1, SensorType.Temperature, this, settings);
            _temperatureVddc = new Sensor("GPU VR VDDC", 2, SensorType.Temperature, this, settings);
            _temperatureMvdd = new Sensor("GPU VR MVDD", 3, SensorType.Temperature, this, settings);
            _temperatureLiquid = new Sensor("GPU Liquid", 4, SensorType.Temperature, this, settings);
            _temperaturePlx = new Sensor("GPU PLX", 5, SensorType.Temperature, this, settings);
            _temperatureHotSpot = new Sensor("GPU Hot Spot", 6, SensorType.Temperature, this, settings);

            _coreClock = new Sensor("GPU Core", 0, SensorType.Clock, this, settings);
            _socClock = new Sensor("GPU SoC", 1, SensorType.Clock, this, settings);
            _memoryClock = new Sensor("GPU Memory", 2, SensorType.Clock, this, settings);
            
            _fan = new Sensor("GPU Fan", 0, SensorType.Fan, this, settings);
            
            _coreVoltage = new Sensor("GPU Core", 0, SensorType.Voltage, this, settings);
            _memoryVoltage = new Sensor("GPU Memory", 1, SensorType.Voltage, this, settings);

            _coreLoad = new Sensor("GPU Core", 0, SensorType.Load, this, settings);
            
            _controlSensor = new Sensor("GPU Fan", 0, SensorType.Control, this, settings);

            _powerCore = new Sensor("GPU Core", 0, SensorType.Power, this, settings);
            _powerPpt = new Sensor("GPU PPT", 1, SensorType.Power, this, settings);
            _powerSocket = new Sensor("GPU Socket", 2, SensorType.Power, this, settings);
            _powerTotal = new Sensor("GPU Package", 3, SensorType.Power, this, settings);

            int supported = 0;
            int enabled = 0;
            int version = 0;

            if (AtiAdlxx.ADL_Overdrive_Caps(1, ref supported, ref enabled, ref version) == AtiAdlxx.ADL_OK)
                _currentOverdriveApiLevel = version;
            else
                _currentOverdriveApiLevel = -1;

            if (_currentOverdriveApiLevel >= 6)
            {
                if (AtiAdlxx.ADL2_Main_Control_Create(AtiAdlxx.Main_Memory_Alloc, adapterIndex, ref _context) == AtiAdlxx.ADL_OK)
                    _context = IntPtr.Zero;
            }

            AtiAdlxx.ADLFanSpeedInfo fanSpeedInfo = new AtiAdlxx.ADLFanSpeedInfo();
            if (AtiAdlxx.ADL_Overdrive5_FanSpeedInfo_Get(adapterIndex, 0, ref fanSpeedInfo) != AtiAdlxx.ADL_OK)
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
                    SpeedType = AtiAdlxx.ADL_DL_FANCTRL_SPEED_TYPE_PERCENT, Flags = AtiAdlxx.ADL_DL_FANCTRL_FLAG_USER_DEFINED_SPEED, FanSpeed = (int)control.SoftwareValue
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
                    int powerOf8 = 0;
                    if (AtiAdlxx.ADL2_Overdrive6_CurrentPower_Get(_context, _adapterIndex, AtiAdlxx.ADLODNCurrentPowerType.ODN_GPU_TOTAL_POWER, ref powerOf8) == AtiAdlxx.ADL_OK)
                    {
                        _powerTotal.Value = powerOf8 >> 8;
                        ActivateSensor(_powerTotal);
                    }
                    else
                    {
                        _powerTotal.Value = null;
                    }

                    if (AtiAdlxx.ADL2_Overdrive6_CurrentPower_Get(_context, _adapterIndex, AtiAdlxx.ADLODNCurrentPowerType.ODN_GPU_PPT_POWER, ref powerOf8) == AtiAdlxx.ADL_OK)
                    {
                        _powerPpt.Value = powerOf8 >> 8;
                        ActivateSensor(_powerPpt);
                    }
                    else
                    {
                        _powerPpt.Value = null;
                    }

                    if (AtiAdlxx.ADL2_Overdrive6_CurrentPower_Get(_context, _adapterIndex, AtiAdlxx.ADLODNCurrentPowerType.ODN_GPU_SOCKET_POWER, ref powerOf8) == AtiAdlxx.ADL_OK)
                    {
                        _powerSocket.Value = powerOf8 >> 8;
                        ActivateSensor(_powerSocket);
                    }
                    else
                    {
                        _powerSocket.Value = null;
                    }

                    if (AtiAdlxx.ADL2_Overdrive6_CurrentPower_Get(_context, _adapterIndex, AtiAdlxx.ADLODNCurrentPowerType.ODN_GPU_CHIP_POWER, ref powerOf8) == AtiAdlxx.ADL_OK)
                    {
                        _powerCore.Value = powerOf8 >> 8;
                        ActivateSensor(_powerCore);
                    }
                    else
                    {
                        _powerCore.Value = null;
                    }
                }

                if (_currentOverdriveApiLevel >= 7)
                {
                    // If a sensor isn't available, some cards report 54000 degrees C. 110C is expected for Navi, so 100 more than that should be enough to use as a maximum.
                    const int maxTemperature = 210;

                    int temp = 0;

                    if (AtiAdlxx.ADL2_OverdriveN_Temperature_Get(_context, _adapterIndex, AtiAdlxx.ADLODNTemperatureType.EDGE, ref temp) == AtiAdlxx.ADL_OK && temp < (maxTemperature * 1000))
                    {
                        _temperatureCore.Value = 0.001f * temp;
                        ActivateSensor(_temperatureCore);
                    }
                    else
                    {
                        _temperatureCore.Value = null;
                    }

                    if (AtiAdlxx.ADL2_OverdriveN_Temperature_Get(_context, _adapterIndex, AtiAdlxx.ADLODNTemperatureType.MEM, ref temp) == AtiAdlxx.ADL_OK && temp < maxTemperature)
                    {
                        _temperatureMemory.Value = temp;
                        ActivateSensor(_temperatureMemory);
                    }
                    else
                    {
                        _temperatureMemory.Value = null;
                    }

                    if (AtiAdlxx.ADL2_OverdriveN_Temperature_Get(_context, _adapterIndex, AtiAdlxx.ADLODNTemperatureType.VRVDDC, ref temp) == AtiAdlxx.ADL_OK && temp < maxTemperature)
                    {
                        _temperatureVddc.Value = temp;
                        ActivateSensor(_temperatureVddc);
                    }
                    else
                    {
                        _temperatureVddc.Value = null;
                    }

                    if (AtiAdlxx.ADL2_OverdriveN_Temperature_Get(_context, _adapterIndex, AtiAdlxx.ADLODNTemperatureType.VRMVDD, ref temp) == AtiAdlxx.ADL_OK && temp < maxTemperature)
                    {
                        _temperatureMvdd.Value = temp;
                        ActivateSensor(_temperatureMvdd);
                    }
                    else
                    {
                        _temperatureMvdd.Value = null;
                    }

                    _temperatureLiquid.Value = null;
                    if (AtiAdlxx.ADL2_OverdriveN_Temperature_Get(_context, _adapterIndex, AtiAdlxx.ADLODNTemperatureType.LIQUID, ref temp) == AtiAdlxx.ADL_OK && temp > 0 && temp < maxTemperature)
                    {
                        _temperatureLiquid.Value = temp;
                        ActivateSensor(_temperatureLiquid);
                    }

                    _temperaturePlx.Value = null;
                    if (AtiAdlxx.ADL2_OverdriveN_Temperature_Get(_context, _adapterIndex, AtiAdlxx.ADLODNTemperatureType.PLX, ref temp) == AtiAdlxx.ADL_OK && temp > 0 && temp < maxTemperature)
                    {
                        _temperaturePlx.Value = temp;
                        ActivateSensor(_temperaturePlx);
                    }

                    if (AtiAdlxx.ADL2_OverdriveN_Temperature_Get(_context, _adapterIndex, AtiAdlxx.ADLODNTemperatureType.HOTSPOT, ref temp) == AtiAdlxx.ADL_OK && temp < maxTemperature)
                    {
                        _temperatureHotSpot.Value = temp;
                        ActivateSensor(_temperatureHotSpot);
                    }
                    else
                    {
                        _temperatureHotSpot.Value = null;
                    }
                }
                else
                {
                    AtiAdlxx.ADLTemperature temperature = new AtiAdlxx.ADLTemperature();
                    if (AtiAdlxx.ADL_Overdrive5_Temperature_Get(_adapterIndex, 0, ref temperature) == AtiAdlxx.ADL_OK)
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
                if (AtiAdlxx.ADL_Overdrive5_FanSpeed_Get(_adapterIndex, 0, ref fanSpeedValue) == AtiAdlxx.ADL_OK)
                {
                    _fan.Value = fanSpeedValue.FanSpeed;
                    ActivateSensor(_fan);
                }
                else
                {
                    _fan.Value = null;
                }

                fanSpeedValue = new AtiAdlxx.ADLFanSpeedValue { SpeedType = AtiAdlxx.ADL_DL_FANCTRL_SPEED_TYPE_PERCENT };
                if (AtiAdlxx.ADL_Overdrive5_FanSpeed_Get(_adapterIndex, 0, ref fanSpeedValue) == AtiAdlxx.ADL_OK)
                {
                    _controlSensor.Value = fanSpeedValue.FanSpeed;
                    ActivateSensor(_controlSensor);
                }
                else
                {
                    _controlSensor.Value = null;
                }

                AtiAdlxx.ADLPMActivity adlpmActivity = new AtiAdlxx.ADLPMActivity();
                if (AtiAdlxx.ADL_Overdrive5_CurrentActivity_Get(_adapterIndex, ref adlpmActivity) == AtiAdlxx.ADL_OK)
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
                if (AtiAdlxx.ADL2_New_QueryPMLogData_Get(_context, _adapterIndex, ref logDataOutput) == AtiAdlxx.ADL_OK)
                {
                    _temperatureCore.Value = logDataOutput.sensors[(int)AtiAdlxx.ADLSensorType.PMLOG_TEMPERATURE_EDGE].value;
                    ActivateSensor(_temperatureCore);
                    
                    _temperatureHotSpot.Value = logDataOutput.sensors[(int)AtiAdlxx.ADLSensorType.PMLOG_TEMPERATURE_HOTSPOT].value;
                    ActivateSensor(_temperatureHotSpot);

                    _temperatureVddc.Value = logDataOutput.sensors[(int)AtiAdlxx.ADLSensorType.PMLOG_TEMPERATURE_VRVDDC].value;
                    ActivateSensor(_temperatureVddc);

                    _temperatureMemory.Value = logDataOutput.sensors[(int)AtiAdlxx.ADLSensorType.PMLOG_TEMPERATURE_MEM].value;
                    ActivateSensor(_temperatureMemory);

                    _temperatureMvdd.Value = null;
                    if (logDataOutput.sensors[(int)AtiAdlxx.ADLSensorType.PMLOG_TEMPERATURE_VRMVDD].value > 0)
                    {
                        _temperatureMvdd.Value = logDataOutput.sensors[(int)AtiAdlxx.ADLSensorType.PMLOG_TEMPERATURE_VRMVDD].value;
                        ActivateSensor(_temperatureMvdd);
                    }

                    _temperatureLiquid.Value = null;
                    if (logDataOutput.sensors[(int)AtiAdlxx.ADLSensorType.PMLOG_TEMPERATURE_LIQUID].value > 0)
                    {
                        _temperatureLiquid.Value = logDataOutput.sensors[(int)AtiAdlxx.ADLSensorType.PMLOG_TEMPERATURE_LIQUID].value;
                        ActivateSensor(_temperatureLiquid);
                    }

                    _temperaturePlx.Value = null;
                    if (logDataOutput.sensors[(int)AtiAdlxx.ADLSensorType.PMLOG_TEMPERATURE_PLX].value > 0)
                    {
                        _temperaturePlx.Value = logDataOutput.sensors[(int)AtiAdlxx.ADLSensorType.PMLOG_TEMPERATURE_PLX].value;
                        ActivateSensor(_temperaturePlx);
                    }
                    
                    _coreClock.Value = logDataOutput.sensors[(int)AtiAdlxx.ADLSensorType.PMLOG_CLK_GFXCLK].value;
                    ActivateSensor(_coreClock);

                    _socClock.Value = logDataOutput.sensors[(int)AtiAdlxx.ADLSensorType.PMLOG_CLK_SOCCLK].value;
                    ActivateSensor(_socClock);

                    _memoryClock.Value = logDataOutput.sensors[(int)AtiAdlxx.ADLSensorType.PMLOG_CLK_MEMCLK].value;
                    ActivateSensor(_memoryClock);


                    if (logDataOutput.sensors[(int)AtiAdlxx.ADLSensorType.PMLOG_FAN_RPM].value != UInt16.MaxValue)
                    {
                        _fan.Value = logDataOutput.sensors[(int)AtiAdlxx.ADLSensorType.PMLOG_FAN_RPM].value;
                        _controlSensor.Value = logDataOutput.sensors[(int)AtiAdlxx.ADLSensorType.PMLOG_FAN_PERCENTAGE].value;
                    } 
                    else
                    {
                        _fan.Value = null;
                        _controlSensor.Value = null;
                    }
                    ActivateSensor(_fan);
                    ActivateSensor(_controlSensor);

                    _coreVoltage.Value = 0.001f * logDataOutput.sensors[(int)AtiAdlxx.ADLSensorType.PMLOG_SOC_VOLTAGE].value;
                    ActivateSensor(_coreVoltage);

                    _memoryVoltage.Value = 0.001f * logDataOutput.sensors[(int)AtiAdlxx.ADLSensorType.PMLOG_MEM_VOLTAGE].value;
                    ActivateSensor(_memoryVoltage);

                    _coreLoad.Value = logDataOutput.sensors[(int)AtiAdlxx.ADLSensorType.PMLOG_INFO_ACTIVITY_GFX].value;
                    ActivateSensor(_coreLoad);

                    _powerCore.Value = null;
                    if (logDataOutput.sensors[(int)AtiAdlxx.ADLSensorType.PMLOG_SOC_POWER].value > 0)
                    {
                        _powerCore.Value = logDataOutput.sensors[(int)AtiAdlxx.ADLSensorType.PMLOG_SOC_POWER].value;
                        ActivateSensor(_powerCore);
                    }

                    _powerSocket.Value = logDataOutput.sensors[(int)AtiAdlxx.ADLSensorType.PMLOG_ASIC_POWER].value;
                    ActivateSensor(_powerSocket);
                }
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
    }
}
