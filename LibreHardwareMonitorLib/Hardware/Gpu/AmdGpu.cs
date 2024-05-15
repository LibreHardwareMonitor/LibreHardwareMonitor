// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using LibreHardwareMonitor.Interop;

namespace LibreHardwareMonitor.Hardware.Gpu;

internal sealed class AmdGpu : GenericGpu
{
    private readonly AtiAdlxx.ADLAdapterInfo _adapterInfo;
    private readonly AtiAdlxx.ADLPMLogStartOutput _adlPMLogStartOutput;
    private readonly AtiAdlxx.ADLPMLogSupportInfo _adlPMLogSupportInfo;
    private readonly AtiAdlxx.ADLGcnInfo _adlGcnInfo;
    private readonly IntPtr _context = IntPtr.Zero;
    private readonly Sensor _controlSensor;
    private readonly Sensor _coreClock;
    private readonly Sensor _coreLoad;
    private readonly Sensor _coreVoltage;
    private readonly int _currentOverdriveApiLevel;
    private readonly string _d3dDeviceId;
    private readonly uint _device;
    private readonly Sensor _fan;
    private readonly Control _fanControl;
    private readonly bool _frameMetricsStarted;
    private readonly Sensor _fullscreenFps;
    private readonly Sensor _gpuDedicatedMemoryUsage;
    private readonly Sensor _gpuDedicatedMemoryFree;
    private readonly Sensor _gpuDedicatedMemoryTotal;
    private readonly Sensor[] _gpuNodeUsage;
    private readonly DateTime[] _gpuNodeUsagePrevTick;
    private readonly long[] _gpuNodeUsagePrevValue;
    private readonly Sensor _gpuSharedMemoryUsage;
    private readonly Sensor _gpuSharedMemoryFree;
    private readonly Sensor _gpuSharedMemoryTotal;
    private readonly Sensor _memoryClock;
    private readonly Sensor _memoryLoad;
    private readonly Sensor _memoryVoltage;
    private readonly Sensor _memoryTotal;
    private readonly Sensor _memoryUsed;
    private readonly Sensor _memoryFree;
    private readonly bool _overdriveApiSupported;
    private readonly bool _pmLogStarted;
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
    private readonly ushort _pmLogSampleRate = 1000;
    private bool _overdrive8LogExists;

    public AmdGpu(IntPtr amdContext, AtiAdlxx.ADLAdapterInfo adapterInfo, AtiAdlxx.ADLGcnInfo gcnInfo, ISettings settings)
        : base(adapterInfo.AdapterName.Trim(), new Identifier("gpu-amd", adapterInfo.AdapterIndex.ToString(CultureInfo.InvariantCulture)), settings)
    {
        _context = amdContext;
        _adlGcnInfo = gcnInfo;
        _adapterInfo = adapterInfo;
        BusNumber = adapterInfo.BusNumber;
        DeviceNumber = adapterInfo.DeviceNumber;

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

        _fullscreenFps = new Sensor("Fullscreen FPS", 0, SensorType.Factor, this, settings);

        _memoryUsed = new Sensor("GPU Memory Used", 0, SensorType.SmallData, this, settings);
        _memoryFree = new Sensor("GPU Memory Free", 1, SensorType.SmallData, this, settings);
        _memoryTotal = new Sensor("GPU Memory Total", 2, SensorType.SmallData, this, settings);

        if (!Software.OperatingSystem.IsUnix)
        {
            string[] deviceIds = D3DDisplayDevice.GetDeviceIdentifiers();
            if (deviceIds != null)
            {
                foreach (string deviceId in deviceIds)
                {
                    string actualDeviceId = D3DDisplayDevice.GetActualDeviceIdentifier(deviceId);

                    if ((actualDeviceId.IndexOf(adapterInfo.PNPString, StringComparison.OrdinalIgnoreCase) != -1 ||
                         adapterInfo.PNPString.IndexOf(actualDeviceId, StringComparison.OrdinalIgnoreCase) != -1) &&
                        D3DDisplayDevice.GetDeviceInfoByIdentifier(deviceId, out D3DDisplayDevice.D3DDeviceInfo deviceInfo))
                    {
                        _d3dDeviceId = deviceId;

                        int nodeSensorIndex = 2;
                        int memorySensorIndex = 3;

                        _gpuDedicatedMemoryUsage = new Sensor("D3D Dedicated Memory Used", memorySensorIndex++, SensorType.SmallData, this, settings);
                        _gpuDedicatedMemoryFree = new Sensor("D3D Dedicated Memory Free", memorySensorIndex++, SensorType.SmallData, this, settings);
                        _gpuDedicatedMemoryTotal = new Sensor("D3D Dedicated Memory Total", memorySensorIndex++, SensorType.SmallData, this, settings);
                        _gpuSharedMemoryUsage = new Sensor("D3D Shared Memory Used", memorySensorIndex++, SensorType.SmallData, this, settings);
                        _gpuSharedMemoryFree = new Sensor("D3D Shared Memory Free", memorySensorIndex++, SensorType.SmallData, this, settings);
                        _gpuSharedMemoryTotal = new Sensor("D3D Shared Memory Total", memorySensorIndex++, SensorType.SmallData, this, settings);

                        _gpuNodeUsage = new Sensor[deviceInfo.Nodes.Length];
                        _gpuNodeUsagePrevValue = new long[deviceInfo.Nodes.Length];
                        _gpuNodeUsagePrevTick = new DateTime[deviceInfo.Nodes.Length];

                        foreach (D3DDisplayDevice.D3DDeviceNodeInfo node in deviceInfo.Nodes.OrderBy(x => x.Name))
                        {
                            _gpuNodeUsage[node.Id] = new Sensor(node.Name, nodeSensorIndex++, SensorType.Load, this, settings);
                            _gpuNodeUsagePrevValue[node.Id] = node.RunningTime;
                            _gpuNodeUsagePrevTick[node.Id] = node.QueryTime;
                        }

                        break;
                    }
                }
            }
        }

        AtiAdlxx.ADLMemoryInfoX4 memoryInfo = new();
        if (AtiAdlxx.ADL_Method_Exists(nameof(AtiAdlxx.ADL2_Adapter_MemoryInfoX4_Get)) &&
            AtiAdlxx.ADL2_Adapter_MemoryInfoX4_Get(_context, _adapterInfo.AdapterIndex, out memoryInfo) == AtiAdlxx.ADLStatus.ADL_OK)
        {
            _memoryTotal.Value = memoryInfo.iMemorySize / 1024 / 1024;
            ActivateSensor(_memoryTotal);
        }

        int supported = 0;
        int enabled = 0;
        int version = 0;

        if (AtiAdlxx.ADL_Method_Exists(nameof(AtiAdlxx.ADL2_Adapter_FrameMetrics_Caps)) &&
            AtiAdlxx.ADL2_Adapter_FrameMetrics_Caps(_context, _adapterInfo.AdapterIndex, ref supported) == AtiAdlxx.ADLStatus.ADL_OK &&
            supported == AtiAdlxx.ADL_TRUE &&
            AtiAdlxx.ADL2_Adapter_FrameMetrics_Start(_context, _adapterInfo.AdapterIndex, 0) == AtiAdlxx.ADLStatus.ADL_OK)
        {
            _frameMetricsStarted = true;
            _fullscreenFps.Value = -1;
            ActivateSensor(_fullscreenFps);
        }

        if (AtiAdlxx.UsePmLogForFamily(_adlGcnInfo.ASICFamilyId) &&
            AtiAdlxx.ADL_Method_Exists(nameof(AtiAdlxx.ADL2_Adapter_PMLog_Support_Get)) &&
            AtiAdlxx.ADL_Method_Exists(nameof(AtiAdlxx.ADL2_Device_PMLog_Device_Create)) &&
            AtiAdlxx.ADL_Method_Exists(nameof(AtiAdlxx.ADL2_Adapter_PMLog_Start)))
        {
            AtiAdlxx.ADLPMLogStartInput _adlPMLogStartInput = new();
            _adlPMLogSupportInfo = new();
            _adlPMLogStartOutput = new AtiAdlxx.ADLPMLogStartOutput();
            _adlPMLogStartInput.usSensors = new ushort[AtiAdlxx.ADL_PMLOG_MAX_SENSORS];

            if (_device == 0 &&
                AtiAdlxx.ADLStatus.ADL_OK == AtiAdlxx.ADL2_Device_PMLog_Device_Create(_context, _adapterInfo.AdapterIndex, ref _device) &&
                AtiAdlxx.ADLStatus.ADL_OK == AtiAdlxx.ADL2_Adapter_PMLog_Support_Get(_context, _adapterInfo.AdapterIndex, ref _adlPMLogSupportInfo))
            {
                int i = 0;
                while (_adlPMLogSupportInfo.usSensors[i] != (ushort)AtiAdlxx.ADLPMLogSensors.ADL_SENSOR_MAXTYPES)
                {
                    _adlPMLogStartInput.usSensors[i] = _adlPMLogSupportInfo.usSensors[i];
                    i++;
                }

                _adlPMLogStartInput.usSensors[i] = (ushort)AtiAdlxx.ADLPMLogSensors.ADL_SENSOR_MAXTYPES;
                _adlPMLogStartInput.ulSampleRate = _pmLogSampleRate;

                if (AtiAdlxx.ADL2_Adapter_PMLog_Start(_context,
                                                      adapterInfo.AdapterIndex,
                                                      ref _adlPMLogStartInput,
                                                      ref _adlPMLogStartOutput,
                                                      _device) == AtiAdlxx.ADLStatus.ADL_OK)
                {
                    _pmLogStarted = true;
                }
            }
        }

        if (AtiAdlxx.ADL_Method_Exists(nameof(AtiAdlxx.ADL2_Overdrive_Caps)) &&
            AtiAdlxx.ADL2_Overdrive_Caps(_context, _adapterInfo.AdapterIndex, ref supported, ref enabled, ref version) == AtiAdlxx.ADLStatus.ADL_OK)
        {
            _overdriveApiSupported = supported == AtiAdlxx.ADL_TRUE;
            _currentOverdriveApiLevel = version;
        }
        else
        {
            if (AtiAdlxx.ADL_Method_Exists(nameof(AtiAdlxx.ADL2_Overdrive6_Capabilities_Get)))
            {
                AtiAdlxx.ADLOD6Capabilities capabilities = new();
                if (AtiAdlxx.ADL2_Overdrive6_Capabilities_Get(_context, _adapterInfo.AdapterIndex, ref capabilities) == AtiAdlxx.ADLStatus.ADL_OK && capabilities.iCapabilities > 0)
                {
                    _overdriveApiSupported = true;
                    _currentOverdriveApiLevel = 6;
                }
            }

            if (!_overdriveApiSupported)
            {
                if (AtiAdlxx.ADL_Method_Exists(nameof(AtiAdlxx.ADL2_Overdrive5_ODParameters_Get)) &&
                    AtiAdlxx.ADL2_Overdrive5_ODParameters_Get(_context, _adapterInfo.AdapterIndex, out AtiAdlxx.ADLODParameters p) == AtiAdlxx.ADLStatus.ADL_OK &&
                    p.iActivityReportingSupported > 0)
                {
                    _overdriveApiSupported = true;
                    _currentOverdriveApiLevel = 5;
                }
                else
                {
                    _currentOverdriveApiLevel = -1;
                }
            }
        }

        AtiAdlxx.ADLFanSpeedInfo fanSpeedInfo = new();
        if (AtiAdlxx.ADL2_Overdrive5_FanSpeedInfo_Get(_context, _adapterInfo.AdapterIndex, 0, ref fanSpeedInfo) != AtiAdlxx.ADLStatus.ADL_OK)
        {
            fanSpeedInfo.iMaxPercent = 100;
            fanSpeedInfo.iMinPercent = 0;
        }

        _fanControl = new Control(_controlSensor, settings, fanSpeedInfo.iMinPercent, fanSpeedInfo.iMaxPercent);
        _fanControl.ControlModeChanged += ControlModeChanged;
        _fanControl.SoftwareControlValueChanged += SoftwareControlValueChanged;
        ControlModeChanged(_fanControl);
        _controlSensor.Control = _fanControl;

        Update();
    }

    public int BusNumber { get; }

    /// <inheritdoc />
    public override string DeviceId => _adapterInfo.PNPString;

    public int DeviceNumber { get; }

    public override HardwareType HardwareType
    {
        get { return HardwareType.GpuAmd; }
    }

    private void SoftwareControlValueChanged(IControl control)
    {
        if (control.ControlMode == ControlMode.Software)
        {
            AtiAdlxx.ADLFanSpeedValue fanSpeedValue = new()
            {
                iSpeedType = AtiAdlxx.ADL_DL_FANCTRL_SPEED_TYPE_PERCENT,
                iFlags = AtiAdlxx.ADL_DL_FANCTRL_FLAG_USER_DEFINED_SPEED,
                iFanSpeed = (int)control.SoftwareValue
            };

            AtiAdlxx.ADL2_Overdrive5_FanSpeed_Set(_context, _adapterInfo.AdapterIndex, 0, ref fanSpeedValue);
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
        AtiAdlxx.ADL2_Overdrive5_FanSpeedToDefault_Set(_context, _adapterInfo.AdapterIndex, 0);
    }

    public override void Update()
    {
        if (_d3dDeviceId != null && D3DDisplayDevice.GetDeviceInfoByIdentifier(_d3dDeviceId, out D3DDisplayDevice.D3DDeviceInfo deviceInfo))
        {
            _gpuDedicatedMemoryTotal.Value = 1f * deviceInfo.GpuVideoMemoryLimit / 1024 / 1024;
            _gpuDedicatedMemoryUsage.Value = 1f * deviceInfo.GpuDedicatedUsed / 1024 / 1024;
            _gpuDedicatedMemoryFree.Value = _gpuDedicatedMemoryTotal.Value - _gpuDedicatedMemoryUsage.Value;
            _gpuSharedMemoryUsage.Value = 1f * deviceInfo.GpuSharedUsed / 1024 / 1024;
            _gpuSharedMemoryTotal.Value = 1f * deviceInfo.GpuSharedLimit / 1024 / 1024;
            _gpuSharedMemoryFree.Value = _gpuSharedMemoryTotal.Value - _gpuSharedMemoryUsage.Value;
            ActivateSensor(_gpuDedicatedMemoryTotal);
            ActivateSensor(_gpuDedicatedMemoryFree);
            ActivateSensor(_gpuDedicatedMemoryUsage);
            ActivateSensor(_gpuSharedMemoryUsage);
            ActivateSensor(_gpuSharedMemoryFree);
            ActivateSensor(_gpuSharedMemoryTotal);

            foreach (D3DDisplayDevice.D3DDeviceNodeInfo node in deviceInfo.Nodes)
            {
                long runningTimeDiff = node.RunningTime - _gpuNodeUsagePrevValue[node.Id];
                long timeDiff = node.QueryTime.Ticks - _gpuNodeUsagePrevTick[node.Id].Ticks;

                _gpuNodeUsage[node.Id].Value = 100f * runningTimeDiff / timeDiff;
                _gpuNodeUsagePrevValue[node.Id] = node.RunningTime;
                _gpuNodeUsagePrevTick[node.Id] = node.QueryTime;
                ActivateSensor(_gpuNodeUsage[node.Id]);
            }
        }

        int vramUsed = 0;
        if (AtiAdlxx.ADL_Method_Exists(nameof(AtiAdlxx.ADL2_Adapter_DedicatedVRAMUsage_Get)) &&
            AtiAdlxx.ADL2_Adapter_DedicatedVRAMUsage_Get(_context, _adapterInfo.AdapterIndex, out vramUsed) == AtiAdlxx.ADLStatus.ADL_OK)
        {
            _memoryUsed.Value = vramUsed;
            ActivateSensor(_memoryUsed);
        }

        if (_memoryTotal.Value > 0)
        {
            _memoryFree.Value = _memoryTotal.Value - _memoryUsed.Value;
            ActivateSensor(_memoryFree);
        }

        if (_frameMetricsStarted)
        {
            float framesPerSecond = 0;
            if (AtiAdlxx.ADL2_Adapter_FrameMetrics_Get(_context, _adapterInfo.AdapterIndex, 0, ref framesPerSecond) == AtiAdlxx.ADLStatus.ADL_OK)
            {
                _fullscreenFps.Value = framesPerSecond;
            }
        }

        if (_overdriveApiSupported)
        {
            GetOD5Temperature(_temperatureCore);
            GetOD5FanSpeed(AtiAdlxx.ADL_DL_FANCTRL_SPEED_TYPE_RPM, _fan);
            GetOD5FanSpeed(AtiAdlxx.ADL_DL_FANCTRL_SPEED_TYPE_PERCENT, _controlSensor);
            GetOD5CurrentActivity();

            if (_currentOverdriveApiLevel >= 6)
            {
                GetOD6Power(AtiAdlxx.ADLODNCurrentPowerType.ODN_GPU_TOTAL_POWER, _powerTotal);
                GetOD6Power(AtiAdlxx.ADLODNCurrentPowerType.ODN_GPU_PPT_POWER, _powerPpt);
                GetOD6Power(AtiAdlxx.ADLODNCurrentPowerType.ODN_GPU_SOCKET_POWER, _powerSoC);
                GetOD6Power(AtiAdlxx.ADLODNCurrentPowerType.ODN_GPU_CHIP_POWER, _powerCore);
            }

            if (_currentOverdriveApiLevel >= 7)
            {
                GetODNTemperature(AtiAdlxx.ADLODNTemperatureType.EDGE, _temperatureCore, -256, 0.001, false);
                GetODNTemperature(AtiAdlxx.ADLODNTemperatureType.MEM, _temperatureMemory);
                GetODNTemperature(AtiAdlxx.ADLODNTemperatureType.VRVDDC, _temperatureVddc);
                GetODNTemperature(AtiAdlxx.ADLODNTemperatureType.VRMVDD, _temperatureMvdd);
                GetODNTemperature(AtiAdlxx.ADLODNTemperatureType.LIQUID, _temperatureLiquid);
                GetODNTemperature(AtiAdlxx.ADLODNTemperatureType.PLX, _temperaturePlx);
                GetODNTemperature(AtiAdlxx.ADLODNTemperatureType.HOTSPOT, _temperatureHotSpot);
            }
        }

        if (_currentOverdriveApiLevel >= 8 || !_overdriveApiSupported)
        {
            _overdrive8LogExists = false;
            AtiAdlxx.ADLPMLogDataOutput logDataOutput = new();
            if (AtiAdlxx.ADL_Method_Exists(nameof(AtiAdlxx.ADL2_New_QueryPMLogData_Get)) &&
                AtiAdlxx.ADL2_New_QueryPMLogData_Get(_context, _adapterInfo.AdapterIndex, ref logDataOutput) == AtiAdlxx.ADLStatus.ADL_OK)
            {
                _overdrive8LogExists = true;
            }

            AtiAdlxx.ADLPMLogData adlPMLogData = new();
            if (_pmLogStarted)
            {
                adlPMLogData = (AtiAdlxx.ADLPMLogData)Marshal.PtrToStructure(_adlPMLogStartOutput.pLoggingAddress, typeof(AtiAdlxx.ADLPMLogData));
            }

            GetAdlSensor(adlPMLogData, logDataOutput, AtiAdlxx.ADLPMLogSensors.ADL_PMLOG_CLK_GFXCLK, _coreClock, reset: false);
            GetAdlSensor(adlPMLogData, logDataOutput, AtiAdlxx.ADLPMLogSensors.ADL_PMLOG_CLK_SOCCLK, _socClock);
            GetAdlSensor(adlPMLogData, logDataOutput, AtiAdlxx.ADLPMLogSensors.ADL_PMLOG_CLK_MEMCLK, _memoryClock, reset: false);

            GetAdlSensor(adlPMLogData, logDataOutput, AtiAdlxx.ADLPMLogSensors.ADL_PMLOG_TEMPERATURE_EDGE, _temperatureCore, reset: false);
            GetAdlSensor(adlPMLogData, logDataOutput, AtiAdlxx.ADLPMLogSensors.ADL_PMLOG_TEMPERATURE_MEM, _temperatureMemory, reset: false);
            GetAdlSensor(adlPMLogData, logDataOutput, AtiAdlxx.ADLPMLogSensors.ADL_PMLOG_TEMPERATURE_VRVDDC, _temperatureVddc, reset: false);
            GetAdlSensor(adlPMLogData, logDataOutput, AtiAdlxx.ADLPMLogSensors.ADL_PMLOG_TEMPERATURE_VRMVDD, _temperatureMvdd, reset: false);
            GetAdlSensor(adlPMLogData, logDataOutput, AtiAdlxx.ADLPMLogSensors.ADL_PMLOG_TEMPERATURE_LIQUID, _temperatureLiquid, reset: false);
            GetAdlSensor(adlPMLogData, logDataOutput, AtiAdlxx.ADLPMLogSensors.ADL_PMLOG_TEMPERATURE_PLX, _temperaturePlx, reset: false);
            GetAdlSensor(adlPMLogData, logDataOutput, AtiAdlxx.ADLPMLogSensors.ADL_PMLOG_TEMPERATURE_HOTSPOT, _temperatureHotSpot, reset: false);
            GetAdlSensor(adlPMLogData, logDataOutput, AtiAdlxx.ADLPMLogSensors.ADL_PMLOG_TEMPERATURE_SOC, _temperatureSoC);


            GetAdlSensor(adlPMLogData, logDataOutput, AtiAdlxx.ADLPMLogSensors.ADL_PMLOG_FAN_RPM, _fan, reset: false);
            GetAdlSensor(adlPMLogData, logDataOutput, AtiAdlxx.ADLPMLogSensors.ADL_PMLOG_FAN_PERCENTAGE, _controlSensor, reset: false);

            GetAdlSensor(adlPMLogData, logDataOutput, AtiAdlxx.ADLPMLogSensors.ADL_PMLOG_GFX_VOLTAGE, _coreVoltage, 0.001f, false);
            GetAdlSensor(adlPMLogData, logDataOutput, AtiAdlxx.ADLPMLogSensors.ADL_PMLOG_SOC_VOLTAGE, _socVoltage, 0.001f);
            GetAdlSensor(adlPMLogData, logDataOutput, AtiAdlxx.ADLPMLogSensors.ADL_PMLOG_MEM_VOLTAGE, _memoryVoltage, 0.001f);

            GetAdlSensor(adlPMLogData, logDataOutput, AtiAdlxx.ADLPMLogSensors.ADL_PMLOG_INFO_ACTIVITY_GFX, _coreLoad, reset: false);
            GetAdlSensor(adlPMLogData, logDataOutput, AtiAdlxx.ADLPMLogSensors.ADL_PMLOG_INFO_ACTIVITY_MEM, _memoryLoad);

            if (_adlGcnInfo.ASICFamilyId >= (int)AtiAdlxx.GCNFamilies.FAMILY_NV3 || !GetAdlSensor(adlPMLogData, logDataOutput, AtiAdlxx.ADLPMLogSensors.ADL_PMLOG_ASIC_POWER, _powerTotal, reset: false))
            {
                GetAdlSensor(adlPMLogData, logDataOutput, AtiAdlxx.ADLPMLogSensors.ADL_PMLOG_BOARD_POWER, _powerTotal, reset: false);
            }

            GetAdlSensor(adlPMLogData, logDataOutput, AtiAdlxx.ADLPMLogSensors.ADL_PMLOG_GFX_POWER, _powerCore, reset: false);
            GetAdlSensor(adlPMLogData, logDataOutput, AtiAdlxx.ADLPMLogSensors.ADL_PMLOG_SOC_POWER, _powerSoC, reset: false);
        }
    }

    private bool IsSensorSupportedByPMLog(AtiAdlxx.ADLPMLogSensors sensorType)
    {
        if (!_pmLogStarted || (int)sensorType == 0)
            return false;

        for (int i = 0; i < AtiAdlxx.ADL_PMLOG_MAX_SENSORS; i++)
        {
            if (_adlPMLogSupportInfo.usSensors[i] == (int)sensorType)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets a sensor value.
    /// </summary>
    ///
    /// <param name="adlPMLogData">Current pmlog struct, used with pmlog-support/start.</param>
    /// <param name="od8Log">Legacy pmlogdataoutput struct, used with ADL2_New_QueryPMLogData_Get.</param>
    /// <param name="sensorType">Type of the sensor.</param>
    /// <param name="sensor">The sensor.</param>
    /// <param name="factor">The factor.</param>
    /// <param name="reset">If set to <c>true</c>, resets the sensor value to <c>null</c>.</param>
    /// <returns>true if sensor is supported, false otherwise</returns>
    private bool GetAdlSensor(AtiAdlxx.ADLPMLogData adlPMLogData, AtiAdlxx.ADLPMLogDataOutput od8Log,
                              AtiAdlxx.ADLPMLogSensors sensorType, Sensor sensor, float factor = 1.0f, bool reset = true)
    {
        int i = (int)sensorType;
        bool supportedByPMLog = IsSensorSupportedByPMLog(sensorType);
        bool supportedByOD8 = _overdrive8LogExists && i < od8Log.sensors.Length && od8Log.sensors[i].supported != 0;

        if (!supportedByPMLog && !supportedByOD8)
        {
            if (reset)
                sensor.Value = null;

            return false;
        }

        if (_pmLogStarted)
        {
            //check if ulLastUpdated is a valid number, avoid timezone issues with unspecified kind and 48h offset
            DateTime now = new(DateTime.Now.Ticks, DateTimeKind.Unspecified);
            if (adlPMLogData.ulLastUpdated == 0 ||
                adlPMLogData.ulActiveSampleRate < 0 ||
                adlPMLogData.ulActiveSampleRate > 86400000 ||
                now.AddHours(48).ToFileTime() < (long)adlPMLogData.ulLastUpdated ||
                now.AddHours(-48).ToFileTime() > (long)adlPMLogData.ulLastUpdated)
            {
                supportedByPMLog = false;
            }
        }

        if (supportedByPMLog)
        {
            bool found = false;

            if (adlPMLogData.ulValues != null)
            {
                for (int k = 0; k < adlPMLogData.ulValues.Length - 1; k += 2)
                {
                    if (adlPMLogData.ulValues[k] == (ushort)AtiAdlxx.ADLPMLogSensors.ADL_SENSOR_MAXTYPES)
                        break;

                    if (adlPMLogData.ulValues[k] == i)
                    {
                        sensor.Value = adlPMLogData.ulValues[k + 1] * factor;
                        ActivateSensor(sensor);
                        found = true;
                    }
                }
            }

            if (!found && reset)
            {
                sensor.Value = null;
            }
        }
        else if (_overdrive8LogExists)
        {
            if (supportedByOD8)
            {
                sensor.Value = od8Log.sensors[i].value * factor;
                ActivateSensor(sensor);
            }
            else if (reset)
            {
                sensor.Value = null;
            }
        }
        return true;
    }

    private void GetOD5CurrentActivity()
    {
        AtiAdlxx.ADLPMActivity adlpmActivity = new();
        if (AtiAdlxx.ADL2_Overdrive5_CurrentActivity_Get(_context, _adapterInfo.AdapterIndex, ref adlpmActivity) == AtiAdlxx.ADLStatus.ADL_OK)
        {
            if (adlpmActivity.iEngineClock > 0)
            {
                _coreClock.Value = 0.01f * adlpmActivity.iEngineClock;
                ActivateSensor(_coreClock);
            }
            else
            {
                _coreClock.Value = null;
            }

            if (adlpmActivity.iMemoryClock > 0)
            {
                _memoryClock.Value = 0.01f * adlpmActivity.iMemoryClock;
                ActivateSensor(_memoryClock);
            }
            else
            {
                _memoryClock.Value = null;
            }

            if (adlpmActivity.iVddc > 0)
            {
                _coreVoltage.Value = 0.001f * adlpmActivity.iVddc;
                ActivateSensor(_coreVoltage);
            }
            else
            {
                _coreVoltage.Value = null;
            }

            _coreLoad.Value = Math.Min(adlpmActivity.iActivityPercent, 100);
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

    private void GetOD5FanSpeed(int speedType, Sensor sensor)
    {
        AtiAdlxx.ADLFanSpeedValue fanSpeedValue = new() { iSpeedType = speedType };
        if (AtiAdlxx.ADL2_Overdrive5_FanSpeed_Get(_context, _adapterInfo.AdapterIndex, 0, ref fanSpeedValue) == AtiAdlxx.ADLStatus.ADL_OK)
        {
            sensor.Value = fanSpeedValue.iFanSpeed;
            ActivateSensor(sensor);
        }
        else
        {
            sensor.Value = null;
        }
    }

    private void GetOD5Temperature(Sensor temperatureCore)
    {
        AtiAdlxx.ADLTemperature temperature = new();
        if (AtiAdlxx.ADL2_Overdrive5_Temperature_Get(_context, _adapterInfo.AdapterIndex, 0, ref temperature) == AtiAdlxx.ADLStatus.ADL_OK)
        {
            temperatureCore.Value = 0.001f * temperature.iTemperature;
            ActivateSensor(temperatureCore);
        }
        else
        {
            temperatureCore.Value = null;
        }
    }

    /// <summary>
    /// Gets the OverdriveN temperature.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="sensor">The sensor.</param>
    /// <param name="minTemperature">The minimum temperature.</param>
    /// <param name="scale">The scale.</param>
    /// <param name="reset">If set to <c>true</c>, resets the sensor value to <c>null</c>.</param>
    private void GetODNTemperature(AtiAdlxx.ADLODNTemperatureType type, Sensor sensor, double minTemperature = -256, double scale = 1, bool reset = true)
    {
        // If a sensor isn't available, some cards report 54000 degrees C.
        // 110C is expected for Navi, so 256C should be enough to use as a maximum.

        int maxTemperature = (int)(256 / scale);
        minTemperature = (int)(minTemperature / scale);

        int temperature = 0;
        if (AtiAdlxx.ADL2_OverdriveN_Temperature_Get(_context, _adapterInfo.AdapterIndex, type, ref temperature) == AtiAdlxx.ADLStatus.ADL_OK &&
            temperature >= minTemperature &&
            temperature <= maxTemperature)
        {
            sensor.Value = (float)(scale * temperature);
            ActivateSensor(sensor);
        }
        else if (reset)
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
        if (AtiAdlxx.ADL2_Overdrive6_CurrentPower_Get(_context, _adapterInfo.AdapterIndex, type, ref powerOf8) == AtiAdlxx.ADLStatus.ADL_OK)
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

        if (_frameMetricsStarted)
            AtiAdlxx.ADL2_Adapter_FrameMetrics_Stop(_context, _adapterInfo.AdapterIndex, 0);

        if (_pmLogStarted && _device != 0)
        {
            AtiAdlxx.ADL2_Adapter_PMLog_Stop(_context, _adapterInfo.AdapterIndex, _device);
        }

        if (_device != 0)
        {
            AtiAdlxx.ADL2_Device_PMLog_Device_Destroy(_context, _device);
        }

        base.Close();
    }

    public override string GetReport()
    {
        var r = new StringBuilder();

        r.AppendLine("AMD GPU");
        r.AppendLine();

        r.Append("AdapterIndex: ");
        r.AppendLine(_adapterInfo.AdapterIndex.ToString(CultureInfo.InvariantCulture));
        r.AppendLine();

        r.AppendLine("Overdrive Caps");
        r.AppendLine();

        try
        {
            int supported = 0;
            int enabled = 0;
            int version = 0;
            AtiAdlxx.ADLStatus status = AtiAdlxx.ADL2_Overdrive_Caps(_context, _adapterInfo.AdapterIndex, ref supported, ref enabled, ref version);

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
            AtiAdlxx.ADLStatus status = AtiAdlxx.ADL2_Overdrive5_ODParameters_Get(_context, _adapterInfo.AdapterIndex, out AtiAdlxx.ADLODParameters p);

            r.Append(" Status: ");
            r.AppendLine(status.ToString());
            r.AppendFormat(" NumberOfPerformanceLevels: {0}{1}", p.iNumberOfPerformanceLevels, Environment.NewLine);
            r.AppendFormat(" ActivityReportingSupported: {0}{1}", p.iActivityReportingSupported, Environment.NewLine);
            r.AppendFormat(" DiscretePerformanceLevels: {0}{1}", p.iDiscretePerformanceLevels, Environment.NewLine);
            r.AppendFormat(" EngineClock.Min: {0}{1}", p.sEngineClock.iMin, Environment.NewLine);
            r.AppendFormat(" EngineClock.Max: {0}{1}", p.sEngineClock.iMax, Environment.NewLine);
            r.AppendFormat(" EngineClock.Step: {0}{1}", p.sEngineClock.iStep, Environment.NewLine);
            r.AppendFormat(" MemoryClock.Min: {0}{1}", p.sMemoryClock.iMin, Environment.NewLine);
            r.AppendFormat(" MemoryClock.Max: {0}{1}", p.sMemoryClock.iMax, Environment.NewLine);
            r.AppendFormat(" MemoryClock.Step: {0}{1}", p.sMemoryClock.iStep, Environment.NewLine);
            r.AppendFormat(" Vddc.Min: {0}{1}", p.sVddc.iMin, Environment.NewLine);
            r.AppendFormat(" Vddc.Max: {0}{1}", p.sVddc.iMax, Environment.NewLine);
            r.AppendFormat(" Vddc.Step: {0}{1}", p.sVddc.iStep, Environment.NewLine);
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
            AtiAdlxx.ADLStatus status = AtiAdlxx.ADL2_Overdrive5_Temperature_Get(_context, _adapterInfo.AdapterIndex, 0, ref adlt);
            r.Append(" Status: ");
            r.AppendLine(status.ToString());
            r.AppendFormat(" Value: {0}{1}", 0.001f * adlt.iTemperature, Environment.NewLine);
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
            var adlf = new AtiAdlxx.ADLFanSpeedValue { iSpeedType = AtiAdlxx.ADL_DL_FANCTRL_SPEED_TYPE_RPM };
            AtiAdlxx.ADLStatus status = AtiAdlxx.ADL2_Overdrive5_FanSpeed_Get(_context, _adapterInfo.AdapterIndex, 0, ref adlf);
            r.Append(" Status RPM: ");
            r.AppendLine(status.ToString());
            r.AppendFormat(" Value RPM: {0}{1}", adlf.iFanSpeed, Environment.NewLine);

            adlf.iSpeedType = AtiAdlxx.ADL_DL_FANCTRL_SPEED_TYPE_PERCENT;
            status = AtiAdlxx.ADL2_Overdrive5_FanSpeed_Get(_context, _adapterInfo.AdapterIndex, 0, ref adlf);
            r.Append(" Status Percent: ");
            r.AppendLine(status.ToString());
            r.AppendFormat(" Value Percent: {0}{1}", adlf.iFanSpeed, Environment.NewLine);
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
            AtiAdlxx.ADLStatus status = AtiAdlxx.ADL2_Overdrive5_CurrentActivity_Get(_context, _adapterInfo.AdapterIndex, ref adlp);

            r.Append(" Status: ");
            r.AppendLine(status.ToString());
            r.AppendFormat(" EngineClock: {0}{1}", 0.01f * adlp.iEngineClock, Environment.NewLine);
            r.AppendFormat(" MemoryClock: {0}{1}", 0.01f * adlp.iMemoryClock, Environment.NewLine);
            r.AppendFormat(" Vddc: {0}{1}", 0.001f * adlp.iVddc, Environment.NewLine);
            r.AppendFormat(" ActivityPercent: {0}{1}", adlp.iActivityPercent, Environment.NewLine);
            r.AppendFormat(" CurrentPerformanceLevel: {0}{1}", adlp.iCurrentPerformanceLevel, Environment.NewLine);
            r.AppendFormat(" CurrentBusSpeed: {0}{1}", adlp.iCurrentBusSpeed, Environment.NewLine);
            r.AppendFormat(" CurrentBusLanes: {0}{1}", adlp.iCurrentBusLanes, Environment.NewLine);
            r.AppendFormat(" MaximumBusLanes: {0}{1}", adlp.iMaximumBusLanes, Environment.NewLine);
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
                    AtiAdlxx.ADLStatus status = AtiAdlxx.ADL2_Overdrive6_CurrentPower_Get(_context, _adapterInfo.AdapterIndex, (AtiAdlxx.ADLODNCurrentPowerType)i, ref power);

                    r.AppendFormat(" Power[{0}].Status: {1}{2}", pt, status.ToString(), Environment.NewLine);
                    r.AppendFormat(" Power[{0}].Value: {1}{2}", pt, power * (1.0f / 0xFF), Environment.NewLine);
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
                    AtiAdlxx.ADLStatus status = AtiAdlxx.ADL2_OverdriveN_Temperature_Get(_context, _adapterInfo.AdapterIndex, (AtiAdlxx.ADLODNTemperatureType)i, ref temperature);

                    r.AppendFormat(" Temperature[{0}].Status: {1}{2}", tt, status.ToString(), Environment.NewLine);
                    r.AppendFormat(" Temperature[{0}].Value: {1}{2}", tt, 0.001f * temperature, Environment.NewLine);
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
                AtiAdlxx.ADLStatus status = AtiAdlxx.ADL2_OverdriveN_PerformanceStatus_Get(_context, _adapterInfo.AdapterIndex, out AtiAdlxx.ADLODNPerformanceStatus ps);

                r.Append(" Status: ");
                r.AppendLine(status.ToString());
                r.AppendFormat(" CoreClock: {0}{1}", ps.iCoreClock, Environment.NewLine);
                r.AppendFormat(" MemoryClock: {0}{1}", ps.iMemoryClock, Environment.NewLine);
                r.AppendFormat(" DCEFClock: {0}{1}", ps.iDCEFClock, Environment.NewLine);
                r.AppendFormat(" GFXClock: {0}{1}", ps.iGFXClock, Environment.NewLine);
                r.AppendFormat(" UVDClock: {0}{1}", ps.iUVDClock, Environment.NewLine);
                r.AppendFormat(" VCEClock: {0}{1}", ps.iVCEClock, Environment.NewLine);
                r.AppendFormat(" GPUActivityPercent: {0}{1}", ps.iGPUActivityPercent, Environment.NewLine);
                r.AppendFormat(" CurrentCorePerformanceLevel: {0}{1}", ps.iCurrentCorePerformanceLevel, Environment.NewLine);
                r.AppendFormat(" CurrentMemoryPerformanceLevel: {0}{1}", ps.iCurrentMemoryPerformanceLevel, Environment.NewLine);
                r.AppendFormat(" CurrentDCEFPerformanceLevel: {0}{1}", ps.iCurrentDCEFPerformanceLevel, Environment.NewLine);
                r.AppendFormat(" CurrentGFXPerformanceLevel: {0}{1}", ps.iCurrentGFXPerformanceLevel, Environment.NewLine);
                r.AppendFormat(" UVDPerformanceLevel: {0}{1}", ps.iUVDPerformanceLevel, Environment.NewLine);
                r.AppendFormat(" VCEPerformanceLevel: {0}{1}", ps.iVCEPerformanceLevel, Environment.NewLine);
                r.AppendFormat(" CurrentBusSpeed: {0}{1}", ps.iCurrentBusSpeed, Environment.NewLine);
                r.AppendFormat(" CurrentBusLanes: {0}{1}", ps.iCurrentBusLanes, Environment.NewLine);
                r.AppendFormat(" MaximumBusLanes: {0}{1}", ps.iMaximumBusLanes, Environment.NewLine);
                r.AppendFormat(" VDDC: {0}{1}", ps.iVDDC, Environment.NewLine);
                r.AppendFormat(" VDDCI: {0}{1}", ps.iVDDCI, Environment.NewLine);
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
                AtiAdlxx.ADLStatus status = AtiAdlxx.ADL2_New_QueryPMLogData_Get(_context, _adapterInfo.AdapterIndex, ref data);
                AtiAdlxx.ADLPMLogData adlPMLogData = new();
                if (_pmLogStarted)
                {
                    adlPMLogData = (AtiAdlxx.ADLPMLogData)Marshal.PtrToStructure(_adlPMLogStartOutput.pLoggingAddress, typeof(AtiAdlxx.ADLPMLogData));
                }

                foreach (AtiAdlxx.ADLPMLogSensors sensorType in Enum.GetValues(typeof(AtiAdlxx.ADLPMLogSensors)))
                {
                    int i = (int)sensorType;
                    if (i == 0)
                        continue;

                    bool supported = false;

                    string st = ((AtiAdlxx.ADLPMLogSensors)i).ToString();
                    if (IsSensorSupportedByPMLog(sensorType))
                    {
                        for (int k = 0; k < adlPMLogData.ulValues.Length - 1; k += 2)
                        {
                            if (adlPMLogData.ulValues[k] == (ushort)AtiAdlxx.ADLPMLogSensors.ADL_SENSOR_MAXTYPES)
                                break;

                            if (adlPMLogData.ulValues[k] == i)
                            {
                                r.AppendFormat(" Sensor[{0}].Value: {1}{2}", st, adlPMLogData.ulValues[k + 1], Environment.NewLine);
                                supported = true;
                            }
                        }
                    }
                    else if (_overdrive8LogExists && i < data.sensors.Length && data.sensors[i].supported != 0)
                    {
                        r.AppendFormat(" Sensor[{0}].Value: {1}{2}", st, data.sensors[i].value, Environment.NewLine);
                        supported = true;
                    }

                    r.AppendFormat(" Sensor[{0}].Supported: {1}{2}", st, supported, Environment.NewLine);
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

        if (_d3dDeviceId != null)
        {
            r.AppendLine("D3D");
            r.AppendLine();
            r.AppendLine(" Id: " + _d3dDeviceId);
        }

        return r.ToString();
    }
}
