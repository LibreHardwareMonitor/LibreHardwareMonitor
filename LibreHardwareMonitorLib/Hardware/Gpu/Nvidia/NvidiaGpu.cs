// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Globalization;
using System.Text;

namespace LibreHardwareMonitor.Hardware.Gpu
{
    internal class NvidiaGpu : Hardware
    {
        private readonly int _adapterIndex;
        private readonly NvPhysicalGpuHandle _handle;
        private readonly NvDisplayHandle? _displayHandle;
        private readonly NVML _nvml;
        private readonly NvmlDevice? _nvmlDevice;

        private readonly Sensor[] _temperatures;
        private readonly Sensor _fan;
        private readonly Sensor[] _clocks;
        private readonly Sensor[] _loads;
        private readonly Sensor _control;
        private readonly Sensor _memoryLoad;
        private readonly Sensor _memoryUsed;
        private readonly Sensor _memoryFree;
        private readonly Sensor _memoryAvail;
        private readonly Sensor _powerUsage;
        private readonly Control _fanControl;

        public NvidiaGpu(int adapterIndex, NvPhysicalGpuHandle handle, NvDisplayHandle? displayHandle, ISettings settings, NVML nvml)
          : base(GetName(handle), new Identifier("gpu", adapterIndex.ToString(CultureInfo.InvariantCulture)), settings)
        {
            _adapterIndex = adapterIndex;
            _handle = handle;
            _displayHandle = displayHandle;
            _nvml = nvml;

            NvGPUThermalSettings thermalSettings = GetThermalSettings();
            _temperatures = new Sensor[thermalSettings.Count];
            for (int i = 0; i < _temperatures.Length; i++)
            {
                NvSensor sensor = thermalSettings.Sensor[i];
                string name;
                switch (sensor.Target)
                {
                    case NvThermalTarget.BOARD: name = "GPU Board"; break;
                    case NvThermalTarget.GPU: name = "GPU Core"; break;
                    case NvThermalTarget.MEMORY: name = "GPU Memory"; break;
                    case NvThermalTarget.POWER_SUPPLY: name = "GPU Power Supply"; break;
                    case NvThermalTarget.UNKNOWN: name = "GPU Unknown"; break;
                    default: name = "GPU"; break;
                }
                _temperatures[i] = new Sensor(name, i, SensorType.Temperature, this, new ParameterDescription[0], settings);
                ActivateSensor(_temperatures[i]);
            }

            int value;
            if (NVAPI.NvAPI_GPU_GetTachReading != null && NVAPI.NvAPI_GPU_GetTachReading(handle, out value) == NvStatus.OK)
            {
                if (value >= 0)
                {
                    _fan = new Sensor("GPU", 0, SensorType.Fan, this, settings);
                    ActivateSensor(_fan);
                }
            }

            _clocks = new Sensor[3];
            _clocks[0] = new Sensor("GPU Core", 0, SensorType.Clock, this, settings);
            _clocks[1] = new Sensor("GPU Memory", 1, SensorType.Clock, this, settings);
            _clocks[2] = new Sensor("GPU Shader", 2, SensorType.Clock, this, settings);
            for (int i = 0; i < _clocks.Length; i++)
                ActivateSensor(_clocks[i]);

            _loads = new Sensor[3];
            _loads[0] = new Sensor("GPU Core", 0, SensorType.Load, this, settings);
            _loads[1] = new Sensor("GPU Memory Controller", 1, SensorType.Load, this, settings);
            _loads[2] = new Sensor("GPU Video Engine", 2, SensorType.Load, this, settings);
            _memoryLoad = new Sensor("GPU Memory", 3, SensorType.Load, this, settings);
            _memoryFree = new Sensor("GPU Memory Free", 1, SensorType.SmallData, this, settings);
            _memoryUsed = new Sensor("GPU Memory Used", 2, SensorType.SmallData, this, settings);
            _memoryAvail = new Sensor("GPU Memory Total", 3, SensorType.SmallData, this, settings);
            _control = new Sensor("GPU Fan", 0, SensorType.Control, this, settings);

            NvGPUCoolerSettings coolerSettings = GetCoolerSettings();
            if (coolerSettings.Count > 0)
            {
                _fanControl = new Control(_control, settings, coolerSettings.Cooler[0].DefaultMin, coolerSettings.Cooler[0].DefaultMax);
                _fanControl.ControlModeChanged += ControlModeChanged;
                _fanControl.SoftwareControlValueChanged += SoftwareControlValueChanged;
                ControlModeChanged(_fanControl);
                _control.Control = _fanControl;
            }

            if (nvml.Initialised)
            {
                _nvmlDevice = nvml.NvmlDeviceGetHandleByIndex(adapterIndex);
                if (_nvmlDevice.HasValue)
                {
                    _powerUsage = new Sensor("GPU Package", 0, SensorType.Power, this, settings);
                }
            }

            Update();
        }

        private static string GetName(NvPhysicalGpuHandle handle)
        {
            string gpuName;
            if (NVAPI.NvAPI_GPU_GetFullName(handle, out gpuName) == NvStatus.OK)
            {
                return "NVIDIA " + gpuName.Trim();
            }
            else
            {
                return "NVIDIA";
            }
        }

        public override HardwareType HardwareType
        {
            get { return HardwareType.GpuNvidia; }
        }

        private NvGPUThermalSettings GetThermalSettings()
        {
            NvGPUThermalSettings settings = new NvGPUThermalSettings();
            settings.Version = NVAPI.GPU_THERMAL_SETTINGS_VER;
            settings.Count = NVAPI.MAX_THERMAL_SENSORS_PER_GPU;
            settings.Sensor = new NvSensor[NVAPI.MAX_THERMAL_SENSORS_PER_GPU];
            if (!(NVAPI.NvAPI_GPU_GetThermalSettings != null && NVAPI.NvAPI_GPU_GetThermalSettings(_handle, (int)NvThermalTarget.ALL, ref settings) == NvStatus.OK))
            {
                settings.Count = 0;
            }
            return settings;
        }

        private NvGPUCoolerSettings GetCoolerSettings()
        {
            NvGPUCoolerSettings settings = new NvGPUCoolerSettings();
            settings.Version = NVAPI.GPU_COOLER_SETTINGS_VER;
            settings.Cooler = new NvCooler[NVAPI.MAX_COOLER_PER_GPU];
            if (!(NVAPI.NvAPI_GPU_GetCoolerSettings != null && NVAPI.NvAPI_GPU_GetCoolerSettings(_handle, 0, ref settings) == NvStatus.OK))
            {
                settings.Count = 0;
            }
            return settings;
        }

        private uint[] GetClocks()
        {
            NvClocks allClocks = new NvClocks();
            allClocks.Version = NVAPI.GPU_CLOCKS_VER;
            allClocks.Clock = new uint[NVAPI.MAX_CLOCKS_PER_GPU];
            if (NVAPI.NvAPI_GPU_GetAllClocks != null && NVAPI.NvAPI_GPU_GetAllClocks(_handle, ref allClocks) == NvStatus.OK)
            {
                return allClocks.Clock;
            }
            return null;
        }

        public override void Update()
        {
            NvGPUThermalSettings settings = GetThermalSettings();
            foreach (Sensor sensor in _temperatures)
                sensor.Value = settings.Sensor[sensor.Index].CurrentTemp;

            if (_fan != null)
            {
                int value = 0;
                NVAPI.NvAPI_GPU_GetTachReading(_handle, out value);
                _fan.Value = value;
            }

            uint[] values = GetClocks();
            if (values != null)
            {
                _clocks[1].Value = 0.001f * values[8];
                if (values[30] != 0)
                {
                    _clocks[0].Value = 0.0005f * values[30];
                    _clocks[2].Value = 0.001f * values[30];
                }
                else
                {
                    _clocks[0].Value = 0.001f * values[0];
                    _clocks[2].Value = 0.001f * values[14];
                }
            }

            NvPStates states = new NvPStates();
            states.Version = NVAPI.GPU_PSTATES_VER;
            states.PStates = new NvPState[NVAPI.MAX_PSTATES_PER_GPU];
            if (NVAPI.NvAPI_GPU_GetPStates != null && NVAPI.NvAPI_GPU_GetPStates(_handle, ref states) == NvStatus.OK)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (states.PStates[i].Present)
                    {
                        _loads[i].Value = states.PStates[i].Percentage;
                        ActivateSensor(_loads[i]);
                    }
                }
            }
            else
            {
                NvUsages usages = new NvUsages();
                usages.Version = NVAPI.GPU_USAGES_VER;
                usages.Usage = new uint[NVAPI.MAX_USAGES_PER_GPU];
                if (NVAPI.NvAPI_GPU_GetUsages != null && NVAPI.NvAPI_GPU_GetUsages(_handle, ref usages) == NvStatus.OK)
                {
                    _loads[0].Value = usages.Usage[2];
                    _loads[1].Value = usages.Usage[6];
                    _loads[2].Value = usages.Usage[10];
                    for (int i = 0; i < 3; i++)
                        ActivateSensor(_loads[i]);
                }
            }


            NvGPUCoolerSettings coolerSettings = GetCoolerSettings();
            if (coolerSettings.Count > 0)
            {
                _control.Value = coolerSettings.Cooler[0].CurrentLevel;
                ActivateSensor(_control);
            }

            NvMemoryInfo memoryInfo = new NvMemoryInfo();
            memoryInfo.Version = NVAPI.GPU_MEMORY_INFO_VER;
            memoryInfo.Values = new uint[NVAPI.MAX_MEMORY_VALUES_PER_GPU];
            if (NVAPI.NvAPI_GPU_GetMemoryInfo != null && _displayHandle.HasValue && NVAPI.NvAPI_GPU_GetMemoryInfo(_displayHandle.Value, ref memoryInfo) == NvStatus.OK)
            {
                uint totalMemory = memoryInfo.Values[0];
                uint freeMemory = memoryInfo.Values[4];
                float usedMemory = Math.Max(totalMemory - freeMemory, 0);
                _memoryFree.Value = (float)freeMemory / 1024;
                _memoryAvail.Value = (float)totalMemory / 1024;
                _memoryUsed.Value = usedMemory / 1024;
                _memoryLoad.Value = 100f * usedMemory / totalMemory;
                ActivateSensor(_memoryAvail);
                ActivateSensor(_memoryUsed);
                ActivateSensor(_memoryFree);
                ActivateSensor(_memoryLoad);
            }

            if (_nvml.Initialised && _nvmlDevice.HasValue)
            {
                var result = _nvml.NvmlDeviceGetPowerUsage(_nvmlDevice.Value);
                if (result.HasValue)
                {
                    _powerUsage.Value = (float)result.Value / 1000;
                    ActivateSensor(_powerUsage);
                }
            }
        }

        public override string GetReport()
        {
            StringBuilder r = new StringBuilder();

            r.AppendLine("Nvidia GPU");
            r.AppendLine();
            r.AppendFormat("Name: {0}{1}", name, Environment.NewLine);
            r.AppendFormat("Index: {0}{1}", _adapterIndex, Environment.NewLine);

            if (_displayHandle.HasValue && NVAPI.NvAPI_GetDisplayDriverVersion != null)
            {
                NvDisplayDriverVersion driverVersion = new NvDisplayDriverVersion();
                driverVersion.Version = NVAPI.DISPLAY_DRIVER_VERSION_VER;
                if (NVAPI.NvAPI_GetDisplayDriverVersion(_displayHandle.Value, ref driverVersion) == NvStatus.OK)
                {
                    r.Append("Driver Version: ");
                    r.Append(driverVersion.DriverVersion / 100);
                    r.Append(".");
                    r.Append((driverVersion.DriverVersion % 100).ToString("00", CultureInfo.InvariantCulture));
                    r.AppendLine();
                    r.Append("Driver Branch: ");
                    r.AppendLine(driverVersion.BuildBranch);
                }
            }
            r.AppendLine();

            if (NVAPI.NvAPI_GPU_GetPCIIdentifiers != null)
            {
                uint deviceId, subSystemId, revisionId, extDeviceId;

                NvStatus status = NVAPI.NvAPI_GPU_GetPCIIdentifiers(_handle, out deviceId, out subSystemId, out revisionId, out extDeviceId);
                if (status == NvStatus.OK)
                {
                    r.Append("DeviceID: 0x");
                    r.AppendLine(deviceId.ToString("X", CultureInfo.InvariantCulture));
                    r.Append("SubSystemID: 0x");
                    r.AppendLine(subSystemId.ToString("X", CultureInfo.InvariantCulture));
                    r.Append("RevisionID: 0x");
                    r.AppendLine(revisionId.ToString("X", CultureInfo.InvariantCulture));
                    r.Append("ExtDeviceID: 0x");
                    r.AppendLine(extDeviceId.ToString("X", CultureInfo.InvariantCulture));
                    r.AppendLine();
                }
            }

            if (NVAPI.NvAPI_GPU_GetThermalSettings != null)
            {
                NvGPUThermalSettings settings = new NvGPUThermalSettings();
                settings.Version = NVAPI.GPU_THERMAL_SETTINGS_VER;
                settings.Count = NVAPI.MAX_THERMAL_SENSORS_PER_GPU;
                settings.Sensor = new NvSensor[NVAPI.MAX_THERMAL_SENSORS_PER_GPU];

                NvStatus status = NVAPI.NvAPI_GPU_GetThermalSettings(_handle, (int)NvThermalTarget.ALL, ref settings);
                r.AppendLine("Thermal Settings");
                r.AppendLine();
                if (status == NvStatus.OK)
                {
                    for (int i = 0; i < settings.Count; i++)
                    {
                        r.AppendFormat(" Sensor[{0}].Controller: {1}{2}", i, settings.Sensor[i].Controller, Environment.NewLine);
                        r.AppendFormat(" Sensor[{0}].DefaultMinTemp: {1}{2}", i, settings.Sensor[i].DefaultMinTemp, Environment.NewLine);
                        r.AppendFormat(" Sensor[{0}].DefaultMaxTemp: {1}{2}", i, settings.Sensor[i].DefaultMaxTemp, Environment.NewLine);
                        r.AppendFormat(" Sensor[{0}].CurrentTemp: {1}{2}", i, settings.Sensor[i].CurrentTemp, Environment.NewLine);
                        r.AppendFormat(" Sensor[{0}].Target: {1}{2}", i, settings.Sensor[i].Target, Environment.NewLine);
                    }
                }
                else
                {
                    r.Append(" Status: ");
                    r.AppendLine(status.ToString());
                }
                r.AppendLine();
            }

            if (NVAPI.NvAPI_GPU_GetAllClocks != null)
            {
                NvClocks allClocks = new NvClocks();
                allClocks.Version = NVAPI.GPU_CLOCKS_VER;
                allClocks.Clock = new uint[NVAPI.MAX_CLOCKS_PER_GPU];
                NvStatus status = NVAPI.NvAPI_GPU_GetAllClocks(_handle, ref allClocks);

                r.AppendLine("Clocks");
                r.AppendLine();
                if (status == NvStatus.OK)
                {
                    for (int i = 0; i < allClocks.Clock.Length; i++)
                        if (allClocks.Clock[i] > 0)
                        {
                            r.AppendFormat(" Clock[{0}]: {1}{2}", i, allClocks.Clock[i], Environment.NewLine);
                        }
                }
                else
                {
                    r.Append(" Status: ");
                    r.AppendLine(status.ToString());
                }
                r.AppendLine();
            }

            if (NVAPI.NvAPI_GPU_GetTachReading != null)
            {
                int tachValue;
                NvStatus status = NVAPI.NvAPI_GPU_GetTachReading(_handle, out tachValue);

                r.AppendLine("Tachometer");
                r.AppendLine();
                if (status == NvStatus.OK)
                {
                    r.AppendFormat(" Value: {0}{1}", tachValue, Environment.NewLine);
                }
                else
                {
                    r.Append(" Status: ");
                    r.AppendLine(status.ToString());
                }
                r.AppendLine();
            }

            if (NVAPI.NvAPI_GPU_GetPStates != null)
            {
                NvPStates states = new NvPStates();
                states.Version = NVAPI.GPU_PSTATES_VER;
                states.PStates = new NvPState[NVAPI.MAX_PSTATES_PER_GPU];
                NvStatus status = NVAPI.NvAPI_GPU_GetPStates(_handle, ref states);

                r.AppendLine("P-States");
                r.AppendLine();
                if (status == NvStatus.OK)
                {
                    for (int i = 0; i < states.PStates.Length; i++)
                    {
                        if (states.PStates[i].Present)
                            r.AppendFormat(" Percentage[{0}]: {1}{2}", i, states.PStates[i].Percentage, Environment.NewLine);
                    }
                }
                else
                {
                    r.Append(" Status: ");
                    r.AppendLine(status.ToString());
                }
                r.AppendLine();
            }

            if (NVAPI.NvAPI_GPU_GetUsages != null)
            {
                NvUsages usages = new NvUsages();
                usages.Version = NVAPI.GPU_USAGES_VER;
                usages.Usage = new uint[NVAPI.MAX_USAGES_PER_GPU];
                NvStatus status = NVAPI.NvAPI_GPU_GetUsages(_handle, ref usages);

                r.AppendLine("Usages");
                r.AppendLine();
                if (status == NvStatus.OK)
                {
                    for (int i = 0; i < usages.Usage.Length; i++)
                    {
                        if (usages.Usage[i] > 0)
                            r.AppendFormat(" Usage[{0}]: {1}{2}", i, usages.Usage[i], Environment.NewLine);
                    }
                }
                else
                {
                    r.Append(" Status: ");
                    r.AppendLine(status.ToString());
                }
                r.AppendLine();
            }

            if (NVAPI.NvAPI_GPU_GetCoolerSettings != null)
            {
                NvGPUCoolerSettings settings = new NvGPUCoolerSettings();
                settings.Version = NVAPI.GPU_COOLER_SETTINGS_VER;
                settings.Cooler = new NvCooler[NVAPI.MAX_COOLER_PER_GPU];
                NvStatus status = NVAPI.NvAPI_GPU_GetCoolerSettings(_handle, 0, ref settings);
                r.AppendLine("Cooler Settings");
                r.AppendLine();
                if (status == NvStatus.OK)
                {
                    for (int i = 0; i < settings.Count; i++)
                    {
                        r.AppendFormat(" Cooler[{0}].Type: {1}{2}", i, settings.Cooler[i].Type, Environment.NewLine);
                        r.AppendFormat(" Cooler[{0}].Controller: {1}{2}", i, settings.Cooler[i].Controller, Environment.NewLine);
                        r.AppendFormat(" Cooler[{0}].DefaultMin: {1}{2}", i, settings.Cooler[i].DefaultMin, Environment.NewLine);
                        r.AppendFormat(" Cooler[{0}].DefaultMax: {1}{2}", i, settings.Cooler[i].DefaultMax, Environment.NewLine);
                        r.AppendFormat(" Cooler[{0}].CurrentMin: {1}{2}", i, settings.Cooler[i].CurrentMin, Environment.NewLine);
                        r.AppendFormat(" Cooler[{0}].CurrentMax: {1}{2}", i, settings.Cooler[i].CurrentMax, Environment.NewLine);
                        r.AppendFormat(" Cooler[{0}].CurrentLevel: {1}{2}", i, settings.Cooler[i].CurrentLevel, Environment.NewLine);
                        r.AppendFormat(" Cooler[{0}].DefaultPolicy: {1}{2}", i, settings.Cooler[i].DefaultPolicy, Environment.NewLine);
                        r.AppendFormat(" Cooler[{0}].CurrentPolicy: {1}{2}", i, settings.Cooler[i].CurrentPolicy, Environment.NewLine);
                        r.AppendFormat(" Cooler[{0}].Target: {1}{2}", i, settings.Cooler[i].Target, Environment.NewLine);
                        r.AppendFormat(" Cooler[{0}].ControlType: {1}{2}", i, settings.Cooler[i].ControlType, Environment.NewLine);
                        r.AppendFormat(" Cooler[{0}].Active: {1}{2}", i, settings.Cooler[i].Active, Environment.NewLine);
                    }
                }
                else
                {
                    r.Append(" Status: ");
                    r.AppendLine(status.ToString());
                }
                r.AppendLine();
            }

            if (NVAPI.NvAPI_GPU_GetMemoryInfo != null && _displayHandle.HasValue)
            {
                NvMemoryInfo memoryInfo = new NvMemoryInfo();
                memoryInfo.Version = NVAPI.GPU_MEMORY_INFO_VER;
                memoryInfo.Values = new uint[NVAPI.MAX_MEMORY_VALUES_PER_GPU];
                NvStatus status = NVAPI.NvAPI_GPU_GetMemoryInfo(_displayHandle.Value, ref memoryInfo);

                r.AppendLine("Memory Info");
                r.AppendLine();
                if (status == NvStatus.OK)
                {
                    for (int i = 0; i < memoryInfo.Values.Length; i++)
                        r.AppendFormat(" Value[{0}]: {1}{2}", i, memoryInfo.Values[i], Environment.NewLine);
                }
                else
                {
                    r.Append(" Status: ");
                    r.AppendLine(status.ToString());
                }
                r.AppendLine();
            }
            return r.ToString();
        }

        private void SoftwareControlValueChanged(IControl control)
        {
            NvGPUCoolerLevels coolerLevels = new NvGPUCoolerLevels();
            coolerLevels.Version = NVAPI.GPU_COOLER_LEVELS_VER;
            coolerLevels.Levels = new NvLevel[NVAPI.MAX_COOLER_PER_GPU];
            coolerLevels.Levels[0].Level = (int)control.SoftwareValue;
            coolerLevels.Levels[0].Policy = 1;
            NVAPI.NvAPI_GPU_SetCoolerLevels(_handle, 0, ref coolerLevels);
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
            NvGPUCoolerLevels coolerLevels = new NvGPUCoolerLevels();
            coolerLevels.Version = NVAPI.GPU_COOLER_LEVELS_VER;
            coolerLevels.Levels = new NvLevel[NVAPI.MAX_COOLER_PER_GPU];
            coolerLevels.Levels[0].Policy = 0x20;
            NVAPI.NvAPI_GPU_SetCoolerLevels(_handle, 0, ref coolerLevels);
        }

        public override void Close()
        {
            if (_fanControl != null)
            {
                _fanControl.ControlModeChanged -= ControlModeChanged;
                _fanControl.SoftwareControlValueChanged -= SoftwareControlValueChanged;
                if (_fanControl.ControlMode != ControlMode.Undefined)
                    SetDefaultFanSpeed();
            }
            base.Close();
        }
    }
}
