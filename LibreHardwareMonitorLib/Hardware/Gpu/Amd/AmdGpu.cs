// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace LibreHardwareMonitor.Hardware.Gpu
{
    internal sealed class AmdGpu : Hardware
    {
        private readonly int _adapterIndex;
        private readonly int _busNumber;
        private readonly int _deviceNumber;

        private readonly Sensor _temperatureCore;
        private readonly Sensor _temperatureHBM;
        private readonly Sensor _temperatureVDDC;
        private readonly Sensor _temperatureMVDD;
        private readonly Sensor _temperatureHotSpot;
        private readonly Sensor _fan;
        private readonly Sensor _coreClock;
        private readonly Sensor _memoryClock;
        private readonly Sensor _coreVoltage;
        private readonly Sensor _coreLoad;
        private readonly Sensor _controlSensor;
        private readonly Control _fanControl;
        private readonly bool _isOverdriveNSupported;

        public AmdGpu(string name, int adapterIndex, int busNumber, int deviceNumber, ISettings settings)
            : base(name, new Identifier("gpu", adapterIndex.ToString(CultureInfo.InvariantCulture)), settings)
        {
            _adapterIndex = adapterIndex;
            _busNumber = busNumber;
            _deviceNumber = deviceNumber;

            _temperatureCore = new Sensor("GPU Core", 0, SensorType.Temperature, this, settings);
            _temperatureHBM = new Sensor("GPU HBM", 1, SensorType.Temperature, this, settings);
            _temperatureVDDC = new Sensor("GPU VDDC", 2, SensorType.Temperature, this, settings);
            _temperatureMVDD = new Sensor("GPU MVDD", 3, SensorType.Temperature, this, settings);
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
            _isOverdriveNSupported = ADL.ADL_Overdrive_Caps(1, ref supported, ref enabled, ref version) == ADL.ADL_OK && version >= 7;

            ADLFanSpeedInfo afsi = new ADLFanSpeedInfo();
            if (ADL.ADL_Overdrive5_FanSpeedInfo_Get(adapterIndex, 0, ref afsi) != ADL.ADL_OK)
            {
                afsi.MaxPercent = 100;
                afsi.MinPercent = 0;
            }

            _fanControl = new Control(_controlSensor, settings, afsi.MinPercent, afsi.MaxPercent);
            _fanControl.ControlModeChanged += ControlModeChanged;
            _fanControl.SoftwareControlValueChanged += SoftwareControlValueChanged;
            ControlModeChanged(_fanControl);
            _controlSensor.Control = _fanControl;
            Update();
        }

        private void SoftwareControlValueChanged(IControl control)
        {
            if (control.ControlMode == ControlMode.Software)
            {
                ADLFanSpeedValue adlf = new ADLFanSpeedValue();
                adlf.SpeedType = ADL.ADL_DL_FANCTRL_SPEED_TYPE_PERCENT;
                adlf.Flags = ADL.ADL_DL_FANCTRL_FLAG_USER_DEFINED_SPEED;
                adlf.FanSpeed = (int)control.SoftwareValue;
                ADL.ADL_Overdrive5_FanSpeed_Set(_adapterIndex, 0, ref adlf);
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
            ADL.ADL_Overdrive5_FanSpeedToDefault_Set(_adapterIndex, 0);
        }

        public int BusNumber { get { return _busNumber; } }

        public int DeviceNumber { get { return _deviceNumber; } }


        public override HardwareType HardwareType
        {
            get { return HardwareType.GpuAmd; }
        }

        public override void Update()
        {

            if (_isOverdriveNSupported)
            {
                int temp = 0;
                IntPtr context = IntPtr.Zero;

                if (ADL.ADL2_OverdriveN_Temperature_Get(context, _adapterIndex, 1, ref temp) == ADL.ADL_OK)
                {
                    _temperatureCore.Value = 0.001f * temp;
                    ActivateSensor(_temperatureCore);
                }
                else
                    _temperatureCore.Value = null;

                if (ADL.ADL2_OverdriveN_Temperature_Get(context, _adapterIndex, 2, ref temp) == ADL.ADL_OK)
                {
                    _temperatureHBM.Value = temp;
                    ActivateSensor(_temperatureHBM);
                }
                else
                    _temperatureHBM.Value = null;

                if (ADL.ADL2_OverdriveN_Temperature_Get(context, _adapterIndex, 3, ref temp) == ADL.ADL_OK)
                {
                    _temperatureVDDC.Value = temp;
                    ActivateSensor(_temperatureVDDC);
                }
                else
                    _temperatureVDDC.Value = null;

                if (ADL.ADL2_OverdriveN_Temperature_Get(context, _adapterIndex, 4, ref temp) == ADL.ADL_OK)
                {
                    _temperatureMVDD.Value = temp;
                    ActivateSensor(_temperatureMVDD);
                }
                else
                    _temperatureMVDD.Value = null;

                if (ADL.ADL2_OverdriveN_Temperature_Get(context, _adapterIndex, 7, ref temp) == ADL.ADL_OK)
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
                ADLTemperature adlt = new ADLTemperature();
                if (ADL.ADL_Overdrive5_Temperature_Get(_adapterIndex, 0, ref adlt) == ADL.ADL_OK)
                {
                    _temperatureCore.Value = 0.001f * adlt.Temperature;
                    ActivateSensor(_temperatureCore);
                }
                else
                    _temperatureCore.Value = null;
            }

            ADLFanSpeedValue adlf = new ADLFanSpeedValue();
            adlf.SpeedType = ADL.ADL_DL_FANCTRL_SPEED_TYPE_RPM;
            if (ADL.ADL_Overdrive5_FanSpeed_Get(_adapterIndex, 0, ref adlf) == ADL.ADL_OK)
            {
                _fan.Value = adlf.FanSpeed;
                ActivateSensor(_fan);
            }
            else
                _fan.Value = null;

            adlf = new ADLFanSpeedValue();
            adlf.SpeedType = ADL.ADL_DL_FANCTRL_SPEED_TYPE_PERCENT;
            if (ADL.ADL_Overdrive5_FanSpeed_Get(_adapterIndex, 0, ref adlf) == ADL.ADL_OK)
            {
                _controlSensor.Value = adlf.FanSpeed;
                ActivateSensor(_controlSensor);
            }
            else
                _controlSensor.Value = null;

            ADLPMActivity adlp = new ADLPMActivity();
            if (ADL.ADL_Overdrive5_CurrentActivity_Get(_adapterIndex, ref adlp) == ADL.ADL_OK)
            {
                if (adlp.EngineClock > 0)
                {
                    _coreClock.Value = 0.01f * adlp.EngineClock;
                    ActivateSensor(_coreClock);
                }
                else
                    _coreClock.Value = null;

                if (adlp.MemoryClock > 0)
                {
                    _memoryClock.Value = 0.01f * adlp.MemoryClock;
                    ActivateSensor(_memoryClock);
                }
                else
                    _memoryClock.Value = null;

                if (adlp.Vddc > 0)
                {
                    _coreVoltage.Value = 0.001f * adlp.Vddc;
                    ActivateSensor(_coreVoltage);
                }
                else
                    _coreVoltage.Value = null;

                _coreLoad.Value = Math.Min(adlp.ActivityPercent, 100);
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
