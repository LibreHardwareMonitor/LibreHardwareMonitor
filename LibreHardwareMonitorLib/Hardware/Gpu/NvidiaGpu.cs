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
    internal sealed class NvidiaGpu : Hardware
    {
        private readonly int _adapterIndex;
        private readonly Sensor[] _clocks;
        private readonly Sensor _control;
        private readonly NvApi.NvDisplayHandle? _displayHandle;
        private readonly Sensor _fan;
        private readonly Control _fanControl;
        private readonly NvApi.NvPhysicalGpuHandle _handle;
        private readonly Sensor[] _loads;
        private readonly Sensor _memoryAvail;
        private readonly Sensor _memoryFree;
        private readonly Sensor _memoryLoad;
        private readonly Sensor _memoryUsed;
        private readonly NvidiaML.NvmlDevice? _nvmlDevice;
        private readonly Sensor _pcieThroughputRx;
        private readonly Sensor _pcieThroughputTx;
        private readonly Sensor _powerUsage;
        private readonly Sensor[] _temperatures;

        public NvidiaGpu(int adapterIndex, NvApi.NvPhysicalGpuHandle handle, NvApi.NvDisplayHandle? displayHandle, ISettings settings)
            : base(GetName(handle), new Identifier("gpu-nvidia", adapterIndex.ToString(CultureInfo.InvariantCulture)), settings)
        {
            _adapterIndex = adapterIndex;
            _handle = handle;
            _displayHandle = displayHandle;

            bool hasPciBusId = NvApi.NvAPI_GPU_GetBusId(handle, out uint busId) == NvApi.NvStatus.OK;

            NvApi.NvGPUThermalSettings thermalSettings = GetThermalSettings();
            _temperatures = new Sensor[thermalSettings.Count];
            for (int i = 0; i < _temperatures.Length; i++)
            {
                NvApi.NvSensor sensor = thermalSettings.Sensor[i];
                string name;
                switch (sensor.Target)
                {
                    case NvApi.NvThermalTarget.BOARD:
                        name = "GPU Board";
                        break;
                    case NvApi.NvThermalTarget.GPU:
                        name = "GPU Core";
                        break;
                    case NvApi.NvThermalTarget.MEMORY:
                        name = "GPU Memory";
                        break;
                    case NvApi.NvThermalTarget.POWER_SUPPLY:
                        name = "GPU Power Supply";
                        break;
                    case NvApi.NvThermalTarget.UNKNOWN:
                        name = "GPU Unknown";
                        break;
                    default:
                        name = "GPU";
                        break;
                }

                _temperatures[i] = new Sensor(name, i, SensorType.Temperature, this, new ParameterDescription[0], settings);
                ActivateSensor(_temperatures[i]);
            }

            if (NvApi.NvAPI_GPU_GetTachReading != null && NvApi.NvAPI_GPU_GetTachReading(handle, out _) == NvApi.NvStatus.OK)
                _fan = new Sensor("GPU", 0, SensorType.Fan, this, settings);
            else if (NvApi.NvAPI_GPU_ClientFanCoolersGetStatus != null && GetCoolerSettings().Count > 0)
                _fan = new Sensor("GPU", 0, SensorType.Fan, this, settings);

            _clocks = new Sensor[3];
            _clocks[0] = new Sensor("GPU Core", 0, SensorType.Clock, this, settings);
            _clocks[1] = new Sensor("GPU Memory", 1, SensorType.Clock, this, settings);
            _clocks[2] = new Sensor("GPU Shader", 2, SensorType.Clock, this, settings);
            for (int i = 0; i < _clocks.Length; i++)
                ActivateSensor(_clocks[i]);

            _loads = new Sensor[4];
            _loads[0] = new Sensor("GPU Core", 0, SensorType.Load, this, settings);
            _loads[1] = new Sensor("GPU Memory Controller", 1, SensorType.Load, this, settings);
            _loads[2] = new Sensor("GPU Video Engine", 2, SensorType.Load, this, settings);
            _loads[3] = new Sensor("GPU Bus", 4, SensorType.Load, this, settings);

            _memoryLoad = new Sensor("GPU Memory", 3, SensorType.Load, this, settings);
            _memoryFree = new Sensor("GPU Memory Free", 1, SensorType.SmallData, this, settings);
            _memoryUsed = new Sensor("GPU Memory Used", 2, SensorType.SmallData, this, settings);
            _memoryAvail = new Sensor("GPU Memory Total", 3, SensorType.SmallData, this, settings);

            _control = new Sensor("GPU Fan", 0, SensorType.Control, this, settings);

            NvApi.NvGPUCoolerSettings coolerSettings = GetCoolerSettings();
            if (coolerSettings.Count > 0)
            {
                _fanControl = new Control(_control, settings, coolerSettings.Cooler[0].DefaultMin, coolerSettings.Cooler[0].DefaultMax);
                _fanControl.ControlModeChanged += ControlModeChanged;
                _fanControl.SoftwareControlValueChanged += SoftwareControlValueChanged;
                ControlModeChanged(_fanControl);
                _control.Control = _fanControl;
            }

            if (NvidiaML.IsAvailable || NvidiaML.Initialize())
            {
                if (hasPciBusId)
                    _nvmlDevice = NvidiaML.NvmlDeviceGetHandleByPciBusId($" 0000:{busId:X2}:00.0") ?? NvidiaML.NvmlDeviceGetHandleByIndex(_adapterIndex);
                else
                    _nvmlDevice = NvidiaML.NvmlDeviceGetHandleByIndex(_adapterIndex);

                if (_nvmlDevice.HasValue)
                {
                    _powerUsage = new Sensor("GPU Package", 0, SensorType.Power, this, settings);

                    _pcieThroughputRx = new Sensor("GPU PCIe Rx", 0, SensorType.Throughput, this, settings);
                    _pcieThroughputTx = new Sensor("GPU PCIe Tx", 1, SensorType.Throughput, this, settings);
                }
            }

            Update();
        }

        public override HardwareType HardwareType
        {
            get { return HardwareType.GpuNvidia; }
        }

        private static string GetName(NvApi.NvPhysicalGpuHandle handle)
        {
            if (NvApi.NvAPI_GPU_GetFullName(handle, out string gpuName) == NvApi.NvStatus.OK)
            {
                return "NVIDIA " + gpuName.Trim();
            }

            return "NVIDIA";
        }

        private NvApi.NvGPUThermalSettings GetThermalSettings()
        {
            NvApi.NvGPUThermalSettings settings = new NvApi.NvGPUThermalSettings
            {
                Version = NvApi.GPU_THERMAL_SETTINGS_VER, Count = NvApi.MAX_THERMAL_SENSORS_PER_GPU, Sensor = new NvApi.NvSensor[NvApi.MAX_THERMAL_SENSORS_PER_GPU]
            };

            if (!(NvApi.NvAPI_GPU_GetThermalSettings != null && NvApi.NvAPI_GPU_GetThermalSettings(_handle, (int)NvApi.NvThermalTarget.ALL, ref settings) == NvApi.NvStatus.OK))
            {
                settings.Count = 0;
            }

            return settings;
        }

        private NvApi.NvFanCoolersStatus GetFanCoolersStatus()
        {
            var coolers = new NvApi.NvFanCoolersStatus { Version = NvApi.GPU_FAN_COOLERS_STATUS_VER, Items = new NvApi.NvFanCoolersStatusItem[NvApi.MAX_FAN_COOLERS_STATUS_ITEMS] };

            if (!(NvApi.NvAPI_GPU_ClientFanCoolersGetStatus != null && NvApi.NvAPI_GPU_ClientFanCoolersGetStatus(_handle, ref coolers) == NvApi.NvStatus.OK))
            {
                coolers.Count = 0;
            }

            return coolers;
        }

        private NvApi.NvGPUCoolerSettings GetCoolerSettings()
        {
            NvApi.NvGPUCoolerSettings settings = new NvApi.NvGPUCoolerSettings { Version = NvApi.GPU_COOLER_SETTINGS_VER, Cooler = new NvApi.NvCooler[NvApi.MAX_COOLER_PER_GPU] };
            if (!(NvApi.NvAPI_GPU_GetCoolerSettings != null && NvApi.NvAPI_GPU_GetCoolerSettings(_handle, 0, ref settings) == NvApi.NvStatus.OK))
            {
                settings.Count = 0;
            }

            return settings;
        }

        private uint[] GetClocks()
        {
            NvApi.NvClocks allClocks = new NvApi.NvClocks { Version = NvApi.GPU_CLOCKS_VER, Clock = new uint[NvApi.MAX_CLOCKS_PER_GPU] };
            if (NvApi.NvAPI_GPU_GetAllClocks != null && NvApi.NvAPI_GPU_GetAllClocks(_handle, ref allClocks) == NvApi.NvStatus.OK)
            {
                return allClocks.Clock;
            }

            return null;
        }

        public override void Update()
        {
            NvApi.NvGPUThermalSettings settings = GetThermalSettings();
            foreach (Sensor sensor in _temperatures)
                sensor.Value = settings.Sensor[sensor.Index].CurrentTemp;

            bool readTach = false;
            if (_fan != null)
            {
                if (NvApi.NvAPI_GPU_GetTachReading(_handle, out int value) == NvApi.NvStatus.OK)
                {
                    _fan.Value = value;
                    ActivateSensor(_fan);

                    readTach = true;
                }
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

            NvApi.NvPStates states = new NvApi.NvPStates { Version = NvApi.GPU_PSTATES_VER, PStates = new NvApi.NvPState[NvApi.MAX_PSTATES_PER_GPU] };
            if (NvApi.NvAPI_GPU_GetPStates != null && NvApi.NvAPI_GPU_GetPStates(_handle, ref states) == NvApi.NvStatus.OK)
            {
                for (int i = 0; i < _loads.Length; i++)
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
                NvApi.NvUsages usages = new NvApi.NvUsages { Version = NvApi.GPU_USAGES_VER, Usage = new uint[NvApi.MAX_USAGES_PER_GPU] };
                if (NvApi.NvAPI_GPU_GetUsages != null && NvApi.NvAPI_GPU_GetUsages(_handle, ref usages) == NvApi.NvStatus.OK)
                {
                    _loads[0].Value = usages.Usage[2];
                    _loads[1].Value = usages.Usage[6];
                    _loads[2].Value = usages.Usage[10];
                    for (int i = 0; i < 3; i++)
                        ActivateSensor(_loads[i]);
                }
            }

            bool readCoolerSettings = false;
            NvApi.NvGPUCoolerSettings coolerSettings = GetCoolerSettings();
            if (coolerSettings.Count > 0)
            {
                _control.Value = coolerSettings.Cooler[0].CurrentLevel;
                ActivateSensor(_control);

                readCoolerSettings = true;
            }

            if (!readTach || !readCoolerSettings)
            {
                NvApi.NvFanCoolersStatus coolersStatus = GetFanCoolersStatus();
                if (coolersStatus.Count > 0)
                {
                    if (!readCoolerSettings)
                    {
                        _control.Value = coolersStatus.Items[0].CurrentLevel;
                        ActivateSensor(_control);
                    }

                    if (!readTach && _fan != null)
                    {
                        _fan.Value = coolersStatus.Items[0].CurrentRpm;
                        ActivateSensor(_fan);
                    }
                }
            }

            NvApi.NvMemoryInfo memoryInfo = new NvApi.NvMemoryInfo { Version = NvApi.GPU_MEMORY_INFO_VER, Values = new uint[NvApi.MAX_MEMORY_VALUES_PER_GPU] };
            if (NvApi.NvAPI_GPU_GetMemoryInfo != null && _displayHandle.HasValue && NvApi.NvAPI_GPU_GetMemoryInfo(_displayHandle.Value, ref memoryInfo) == NvApi.NvStatus.OK)
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

            if (NvidiaML.IsAvailable && _nvmlDevice.HasValue)
            {
                int? result = NvidiaML.NvmlDeviceGetPowerUsage(_nvmlDevice.Value);
                if (result.HasValue)
                {
                    _powerUsage.Value = (float)result.Value / 1000;
                    ActivateSensor(_powerUsage);
                }

                // In MB/s, throughput sensors are passed as in KB/s.
                uint? rx = NvidiaML.NvmlDeviceGetPcieThroughput(_nvmlDevice.Value, NvidiaML.NvmlPcieUtilCounter.RxBytes);
                if (rx.HasValue)
                {
                    _pcieThroughputRx.Value = rx * 1024;
                    ActivateSensor(_pcieThroughputRx);
                }

                uint? tx = NvidiaML.NvmlDeviceGetPcieThroughput(_nvmlDevice.Value, NvidiaML.NvmlPcieUtilCounter.TxBytes);
                if (tx.HasValue)
                {
                    _pcieThroughputTx.Value = tx * 1024;
                    ActivateSensor(_pcieThroughputTx);
                }
            }
        }

        public override string GetReport()
        {
            StringBuilder r = new StringBuilder();

            r.AppendLine("Nvidia GPU");
            r.AppendLine();
            r.AppendFormat("Name: {0}{1}", _name, Environment.NewLine);
            r.AppendFormat("Index: {0}{1}", _adapterIndex, Environment.NewLine);

            if (_displayHandle.HasValue && NvApi.NvAPI_GetDisplayDriverVersion != null)
            {
                NvApi.NvDisplayDriverVersion driverVersion = new NvApi.NvDisplayDriverVersion { Version = NvApi.DISPLAY_DRIVER_VERSION_VER };
                if (NvApi.NvAPI_GetDisplayDriverVersion(_displayHandle.Value, ref driverVersion) == NvApi.NvStatus.OK)
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

            if (NvApi.NvAPI_GPU_GetPCIIdentifiers != null)
            {
                NvApi.NvStatus status = NvApi.NvAPI_GPU_GetPCIIdentifiers(_handle, out uint deviceId, out uint subSystemId, out uint revisionId, out uint extDeviceId);
                if (status == NvApi.NvStatus.OK)
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

            if (NvApi.NvAPI_GPU_GetThermalSettings != null)
            {
                NvApi.NvGPUThermalSettings settings = new NvApi.NvGPUThermalSettings
                {
                    Version = NvApi.GPU_THERMAL_SETTINGS_VER, Count = NvApi.MAX_THERMAL_SENSORS_PER_GPU, Sensor = new NvApi.NvSensor[NvApi.MAX_THERMAL_SENSORS_PER_GPU]
                };

                NvApi.NvStatus status = NvApi.NvAPI_GPU_GetThermalSettings(_handle, (int)NvApi.NvThermalTarget.ALL, ref settings);
                r.AppendLine("Thermal Settings");
                r.AppendLine();
                if (status == NvApi.NvStatus.OK)
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

            if (NvApi.NvAPI_GPU_GetAllClocks != null)
            {
                NvApi.NvClocks allClocks = new NvApi.NvClocks { Version = NvApi.GPU_CLOCKS_VER, Clock = new uint[NvApi.MAX_CLOCKS_PER_GPU] };
                NvApi.NvStatus status = NvApi.NvAPI_GPU_GetAllClocks(_handle, ref allClocks);

                r.AppendLine("Clocks");
                r.AppendLine();
                if (status == NvApi.NvStatus.OK)
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

            if (NvApi.NvAPI_GPU_GetTachReading != null)
            {
                NvApi.NvStatus status = NvApi.NvAPI_GPU_GetTachReading(_handle, out int tachValue);

                r.AppendLine("Tachometer");
                r.AppendLine();
                if (status == NvApi.NvStatus.OK)
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

            if (NvApi.NvAPI_GPU_GetPStates != null)
            {
                NvApi.NvPStates states = new NvApi.NvPStates { Version = NvApi.GPU_PSTATES_VER, PStates = new NvApi.NvPState[NvApi.MAX_PSTATES_PER_GPU] };
                NvApi.NvStatus status = NvApi.NvAPI_GPU_GetPStates(_handle, ref states);

                r.AppendLine("P-States");
                r.AppendLine();
                if (status == NvApi.NvStatus.OK)
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

            if (NvApi.NvAPI_GPU_GetUsages != null)
            {
                NvApi.NvUsages usages = new NvApi.NvUsages { Version = NvApi.GPU_USAGES_VER, Usage = new uint[NvApi.MAX_USAGES_PER_GPU] };
                NvApi.NvStatus status = NvApi.NvAPI_GPU_GetUsages(_handle, ref usages);

                r.AppendLine("Usages");
                r.AppendLine();
                if (status == NvApi.NvStatus.OK)
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

            if (NvApi.NvAPI_GPU_GetCoolerSettings != null)
            {
                NvApi.NvGPUCoolerSettings settings = new NvApi.NvGPUCoolerSettings { Version = NvApi.GPU_COOLER_SETTINGS_VER, Cooler = new NvApi.NvCooler[NvApi.MAX_COOLER_PER_GPU] };
                NvApi.NvStatus status = NvApi.NvAPI_GPU_GetCoolerSettings(_handle, 0, ref settings);
                r.AppendLine("Cooler Settings");
                r.AppendLine();
                if (status == NvApi.NvStatus.OK)
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

            if (NvApi.NvAPI_GPU_ClientFanCoolersGetStatus != null)
            {
                var coolers = new NvApi.NvFanCoolersStatus
                {
                    Version = NvApi.GPU_FAN_COOLERS_STATUS_VER, 
                    Items = new NvApi.NvFanCoolersStatusItem[NvApi.MAX_FAN_COOLERS_STATUS_ITEMS]
                };

                NvApi.NvStatus status = NvApi.NvAPI_GPU_ClientFanCoolersGetStatus(_handle, ref coolers);

                r.AppendLine("Fan Coolers Status");
                r.AppendLine();
                if (status == NvApi.NvStatus.OK)
                {
                    for (int i = 0; i < coolers.Count; i++)
                    {
                        r.AppendFormat(" Items[{0}].Type: {1}{2}", i,
                                       coolers.Items[i].Type, Environment.NewLine);
                        r.AppendFormat(" Items[{0}].CurrentRpm: {1}{2}", i,
                                       coolers.Items[i].CurrentRpm, Environment.NewLine);
                        r.AppendFormat(" Items[{0}].CurrentMinLevel: {1}{2}", i,
                                       coolers.Items[i].CurrentMinLevel, Environment.NewLine);
                        r.AppendFormat(" Items[{0}].CurrentMaxLevel: {1}{2}", i,
                                       coolers.Items[i].CurrentMaxLevel, Environment.NewLine);
                        r.AppendFormat(" Items[{0}].CurrentLevel: {1}{2}", i,
                                       coolers.Items[i].CurrentLevel, Environment.NewLine);
                    }
                }
                else
                {
                    r.Append(" Status: ");
                    r.AppendLine(status.ToString());
                }
                r.AppendLine();
            }

            if (NvApi.NvAPI_GPU_GetMemoryInfo != null && _displayHandle.HasValue)
            {
                NvApi.NvMemoryInfo memoryInfo = new NvApi.NvMemoryInfo { Version = NvApi.GPU_MEMORY_INFO_VER, Values = new uint[NvApi.MAX_MEMORY_VALUES_PER_GPU] };
                NvApi.NvStatus status = NvApi.NvAPI_GPU_GetMemoryInfo(_displayHandle.Value, ref memoryInfo);

                r.AppendLine("Memory Info");
                r.AppendLine();
                if (status == NvApi.NvStatus.OK)
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
            NvApi.NvGPUCoolerLevels coolerLevels = new NvApi.NvGPUCoolerLevels { Version = NvApi.GPU_COOLER_LEVELS_VER, Levels = new NvApi.NvLevel[NvApi.MAX_COOLER_PER_GPU] };
            coolerLevels.Levels[0].Level = (int)control.SoftwareValue;
            coolerLevels.Levels[0].Policy = 1;
            NvApi.NvAPI_GPU_SetCoolerLevels(_handle, 0, ref coolerLevels);
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
            NvApi.NvGPUCoolerLevels coolerLevels = new NvApi.NvGPUCoolerLevels { Version = NvApi.GPU_COOLER_LEVELS_VER, Levels = new NvApi.NvLevel[NvApi.MAX_COOLER_PER_GPU] };
            coolerLevels.Levels[0].Policy = 0x20;
            NvApi.NvAPI_GPU_SetCoolerLevels(_handle, 0, ref coolerLevels);
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
