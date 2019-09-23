// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Interop;

namespace LibreHardwareMonitor.Hardware.Gpu
{
    internal sealed class AmdGpu : Hardware
    {
        private readonly int _adapterIndex;
        private readonly Sensor _controlSensor;
        private readonly Sensor _coreClock;
        private readonly Sensor _coreLoad;
        private readonly Sensor _coreVoltage;
        private readonly Sensor _fan;
        private readonly Control _fanControl;
        private readonly bool _isOverdriveNSupported;
        private readonly Sensor _memoryClock;

        private readonly Sensor _temperatureCore;
        private readonly Sensor _temperatureHbm;
        private readonly Sensor _temperatureHotSpot;
        private readonly Sensor _temperatureMvdd;
        private readonly Sensor _temperatureVddc;

        public AmdGpu(string name, int adapterIndex, int busNumber, int deviceNumber, ISettings settings)
            : base(name, new Identifier("gpu", adapterIndex.ToString(CultureInfo.InvariantCulture)), settings)
        {
            _adapterIndex = adapterIndex;
            BusNumber = busNumber;
            DeviceNumber = deviceNumber;

            _temperatureCore = new Sensor("GPU Core", 0, SensorType.Temperature, this, settings);
            _temperatureHbm = new Sensor("GPU HBM", 1, SensorType.Temperature, this, settings);
            _temperatureVddc = new Sensor("GPU VDDC", 2, SensorType.Temperature, this, settings);
            _temperatureMvdd = new Sensor("GPU MVDD", 3, SensorType.Temperature, this, settings);
            _temperatureHotSpot = new Sensor("GPU Hot Spot", 4, SensorType.Temperature, this, settings);
            _fan = new Sensor("GPU Fan", 0, SensorType.Fan, this, settings);
            _coreClock = new Sensor("GPU Core", 0, SensorType.Clock, this, settings);
            _memoryClock = new Sensor("GPU Memory", 1, SensorType.Clock, this, settings);
            _coreVoltage = new Sensor("GPU Core", 0, SensorType.Voltage, this, settings);
            _coreLoad = new Sensor("GPU Core", 0, SensorType.Load, this, settings);
            _controlSensor = new Sensor("GPU Fan", 0, SensorType.Control, this, settings);

            int supported = 0;
            int enabled = 0;
            int version = 0;
            _isOverdriveNSupported = AtiAdlxx.ADL_Overdrive_Caps(1, ref supported, ref enabled, ref version) == AtiAdlxx.ADL_OK && version >= 7;

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
            if (_isOverdriveNSupported)
            {
                int temp = 0;
                IntPtr context = IntPtr.Zero;

                if (AtiAdlxx.ADL2_OverdriveN_Temperature_Get(context, _adapterIndex, 1, ref temp) == AtiAdlxx.ADL_OK)
                {
                    _temperatureCore.Value = 0.001f * temp;
                    ActivateSensor(_temperatureCore);
                }
                else
                    _temperatureCore.Value = null;

                if (AtiAdlxx.ADL2_OverdriveN_Temperature_Get(context, _adapterIndex, 2, ref temp) == AtiAdlxx.ADL_OK)
                {
                    _temperatureHbm.Value = temp;
                    ActivateSensor(_temperatureHbm);
                }
                else
                    _temperatureHbm.Value = null;

                if (AtiAdlxx.ADL2_OverdriveN_Temperature_Get(context, _adapterIndex, 3, ref temp) == AtiAdlxx.ADL_OK)
                {
                    _temperatureVddc.Value = temp;
                    ActivateSensor(_temperatureVddc);
                }
                else
                    _temperatureVddc.Value = null;

                if (AtiAdlxx.ADL2_OverdriveN_Temperature_Get(context, _adapterIndex, 4, ref temp) == AtiAdlxx.ADL_OK)
                {
                    _temperatureMvdd.Value = temp;
                    ActivateSensor(_temperatureMvdd);
                }
                else
                    _temperatureMvdd.Value = null;

                if (AtiAdlxx.ADL2_OverdriveN_Temperature_Get(context, _adapterIndex, 7, ref temp) == AtiAdlxx.ADL_OK)
                {
                    _temperatureHotSpot.Value = temp;
                    ActivateSensor(_temperatureHotSpot);
                }
                else
                    _temperatureHotSpot.Value = null;

                if (context != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(context);
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
                    _temperatureCore.Value = null;
            }

            AtiAdlxx.ADLFanSpeedValue fanSpeedValue = new AtiAdlxx.ADLFanSpeedValue { SpeedType = AtiAdlxx.ADL_DL_FANCTRL_SPEED_TYPE_RPM };
            if (AtiAdlxx.ADL_Overdrive5_FanSpeed_Get(_adapterIndex, 0, ref fanSpeedValue) == AtiAdlxx.ADL_OK)
            {
                _fan.Value = fanSpeedValue.FanSpeed;
                ActivateSensor(_fan);
            }
            else
                _fan.Value = null;

            fanSpeedValue = new AtiAdlxx.ADLFanSpeedValue { SpeedType = AtiAdlxx.ADL_DL_FANCTRL_SPEED_TYPE_PERCENT };
            if (AtiAdlxx.ADL_Overdrive5_FanSpeed_Get(_adapterIndex, 0, ref fanSpeedValue) == AtiAdlxx.ADL_OK)
            {
                _controlSensor.Value = fanSpeedValue.FanSpeed;
                ActivateSensor(_controlSensor);
            }
            else
                _controlSensor.Value = null;

            AtiAdlxx.ADLPMActivity adlpmActivity = new AtiAdlxx.ADLPMActivity();
            if (AtiAdlxx.ADL_Overdrive5_CurrentActivity_Get(_adapterIndex, ref adlpmActivity) == AtiAdlxx.ADL_OK)
            {
                if (adlpmActivity.EngineClock > 0)
                {
                    _coreClock.Value = 0.01f * adlpmActivity.EngineClock;
                    ActivateSensor(_coreClock);
                }
                else
                    _coreClock.Value = null;

                if (adlpmActivity.MemoryClock > 0)
                {
                    _memoryClock.Value = 0.01f * adlpmActivity.MemoryClock;
                    ActivateSensor(_memoryClock);
                }
                else
                    _memoryClock.Value = null;

                if (adlpmActivity.Vddc > 0)
                {
                    _coreVoltage.Value = 0.001f * adlpmActivity.Vddc;
                    ActivateSensor(_coreVoltage);
                }
                else
                    _coreVoltage.Value = null;

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

        public override void Close()
        {
            _fanControl.ControlModeChanged -= ControlModeChanged;
            _fanControl.SoftwareControlValueChanged -= SoftwareControlValueChanged;
            if (_fanControl.ControlMode != ControlMode.Undefined)
                SetDefaultFanSpeed();

            base.Close();
        }
    }
}
