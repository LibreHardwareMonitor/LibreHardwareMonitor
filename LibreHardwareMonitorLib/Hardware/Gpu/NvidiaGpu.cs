// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using LibreHardwareMonitor.Interop;
using Microsoft.Win32;

namespace LibreHardwareMonitor.Hardware.Gpu;

internal sealed class NvidiaGpu : GenericGpu
{
    private readonly int _adapterIndex;
    private readonly Sensor[] _clocks;
    private readonly int _clockVersion;
    private readonly Sensor[] _controls;
    private readonly string _d3dDeviceId;
    private readonly NvApi.NvDisplayHandle? _displayHandle;
    private readonly Control[] _fanControls;
    private readonly Sensor[] _fans;
    private readonly Sensor _gpuDedicatedMemoryUsage;
    private readonly Sensor[] _gpuNodeUsage;
    private readonly DateTime[] _gpuNodeUsagePrevTick;
    private readonly long[] _gpuNodeUsagePrevValue;
    private readonly Sensor _gpuSharedMemoryUsage;
    private readonly NvApi.NvPhysicalGpuHandle _handle;
    private readonly Sensor _hotSpotTemperature;
    private readonly Sensor[] _loads;
    private readonly Sensor _memoryFree;
    private readonly Sensor _memoryJunctionTemperature;
    private readonly Sensor _memoryTotal;
    private readonly Sensor _memoryUsed;
    private readonly Sensor _memoryLoad;
    private readonly NvidiaML.NvmlDevice? _nvmlDevice;
    private readonly Sensor _pcieThroughputRx;
    private readonly Sensor _pcieThroughputTx;
    private readonly Sensor[] _powers;
    private readonly Sensor _powerUsage;
    private readonly Sensor[] _temperatures;
    private readonly uint _thermalSensorsMask;

    public NvidiaGpu(int adapterIndex, NvApi.NvPhysicalGpuHandle handle, NvApi.NvDisplayHandle? displayHandle, ISettings settings)
        : base(GetName(handle),
               new Identifier("gpu-nvidia", adapterIndex.ToString(CultureInfo.InvariantCulture)),
               settings)
    {
        _adapterIndex = adapterIndex;
        _handle = handle;
        _displayHandle = displayHandle;

        bool hasBusId = NvApi.NvAPI_GPU_GetBusId(handle, out uint busId) == NvApi.NvStatus.OK;

        // Thermal settings.
        NvApi.NvThermalSettings thermalSettings = GetThermalSettings(out NvApi.NvStatus status);
        if (status == NvApi.NvStatus.OK && thermalSettings.Count > 0)
        {
            _temperatures = new Sensor[thermalSettings.Count];

            for (int i = 0; i < thermalSettings.Count; i++)
            {
                NvApi.NvSensor sensor = thermalSettings.Sensor[i];

                string name = sensor.Target switch
                {
                    NvApi.NvThermalTarget.Gpu => "GPU Core",
                    NvApi.NvThermalTarget.Memory => "GPU Memory",
                    NvApi.NvThermalTarget.PowerSupply => "GPU Power Supply",
                    NvApi.NvThermalTarget.Board => "GPU Board",
                    NvApi.NvThermalTarget.VisualComputingBoard => "GPU Visual Computing Board",
                    NvApi.NvThermalTarget.VisualComputingInlet => "GPU Visual Computing Inlet",
                    NvApi.NvThermalTarget.VisualComputingOutlet => "GPU Visual Computing Outlet",
                    _ => "GPU"
                };

                _temperatures[i] = new Sensor(name, i, SensorType.Temperature, this, Array.Empty<ParameterDescription>(), settings);
                ActivateSensor(_temperatures[i]);
            }
        }

        // Thermal sensors.
        _hotSpotTemperature = new Sensor("GPU Hot Spot", (int)thermalSettings.Count + 1, SensorType.Temperature, this, settings);
        _memoryJunctionTemperature = new Sensor("GPU Memory Junction", (int)thermalSettings.Count + 2, SensorType.Temperature, this, settings);
        bool hasAnyThermalSensor = false;

        for (int thermalSensorsMaxBit = 0; thermalSensorsMaxBit < 32; thermalSensorsMaxBit++)
        {
            // Find the maximum thermal sensor mask value.
            _thermalSensorsMask = 1u << thermalSensorsMaxBit;

            GetThermalSensors(_thermalSensorsMask, out NvApi.NvStatus thermalSensorsStatus);
            if (thermalSensorsStatus == NvApi.NvStatus.OK)
            {
                hasAnyThermalSensor = true;
                continue;
            }

            _thermalSensorsMask--;
            break;
        }

        if (!hasAnyThermalSensor)
        {
            _thermalSensorsMask = 0;
        }

        // Clock frequencies.
        for (int clockVersion = 1; clockVersion <= 3; clockVersion++)
        {
            _clockVersion = clockVersion;

            NvApi.NvGpuClockFrequencies clockFrequencies = GetClockFrequencies(out status);
            if (status == NvApi.NvStatus.OK)
            {
                var clocks = new List<Sensor>();
                for (int i = 0; i < clockFrequencies.Clocks.Length; i++)
                {
                    NvApi.NvGpuClockFrequenciesDomain clock = clockFrequencies.Clocks[i];
                    if (clock.IsPresent && Enum.IsDefined(typeof(NvApi.NvGpuPublicClockId), i))
                    {
                        var clockId = (NvApi.NvGpuPublicClockId)i;
                        string name = clockId switch
                        {
                            NvApi.NvGpuPublicClockId.Graphics => "GPU Core",
                            NvApi.NvGpuPublicClockId.Memory => "GPU Memory",
                            NvApi.NvGpuPublicClockId.Processor => "GPU Shader",
                            NvApi.NvGpuPublicClockId.Video => "GPU Video",
                            _ => null
                        };

                        if (name != null)
                            clocks.Add(new Sensor(name, i, SensorType.Clock, this, settings));
                    }
                }

                if (clocks.Count > 0)
                {
                    _clocks = clocks.ToArray();

                    foreach (Sensor sensor in clocks)
                        ActivateSensor(sensor);

                    break;
                }
            }
        }

        // Fans + controllers.
        NvApi.NvFanCoolersStatus fanCoolers = GetFanCoolersStatus(out status);
        if (status == NvApi.NvStatus.OK && fanCoolers.Count > 0)
        {
            _fans = new Sensor[fanCoolers.Count];

            for (int i = 0; i < fanCoolers.Count; i++)
            {
                NvApi.NvFanCoolersStatusItem item = fanCoolers.Items[i];

                string name = "GPU Fan" + (fanCoolers.Count > 1 ? " " + (i + 1) : string.Empty);

                _fans[i] = new Sensor(name, (int)item.CoolerId, SensorType.Fan, this, settings);
                ActivateSensor(_fans[i]);
            }
        }
        else
        {
            GetTachReading(out status);
            if (status == NvApi.NvStatus.OK)
            {
                _fans = new[] { new Sensor("GPU", 1, SensorType.Fan, this, settings) };
                ActivateSensor(_fans[0]);
            }
        }

        NvApi.NvFanCoolerControl fanControllers = GetFanCoolersControllers(out status);
        if (status == NvApi.NvStatus.OK && fanControllers.Count > 0 && fanCoolers.Count > 0)
        {
            _controls = new Sensor[fanControllers.Count];
            _fanControls = new Control[fanControllers.Count];

            for (int i = 0; i < fanControllers.Count; i++)
            {
                NvApi.NvFanCoolerControlItem item = fanControllers.Items[i];

                string name = "GPU Fan" + (fanControllers.Count > 1 ? " " + (i + 1) : string.Empty);

                NvApi.NvFanCoolersStatusItem fanItem = Array.Find(fanCoolers.Items, x => x.CoolerId == item.CoolerId);
                if (!fanItem.Equals(default(NvApi.NvFanCoolersStatusItem)))
                {
                    _controls[i] = new Sensor(name, (int)item.CoolerId, SensorType.Control, this, settings);
                    ActivateSensor(_controls[i]);

                    _fanControls[i] = new Control(_controls[i], settings, fanItem.CurrentMinLevel, fanItem.CurrentMaxLevel);
                    _fanControls[i].ControlModeChanged += ControlModeChanged;
                    _fanControls[i].SoftwareControlValueChanged += SoftwareControlValueChanged;
                    _controls[i].Control = _fanControls[i];

                    ControlModeChanged(_fanControls[i]);
                }
            }
        }
        else
        {
            NvApi.NvCoolerSettings coolerSettings = GetCoolerSettings(out status);
            if (status == NvApi.NvStatus.OK && coolerSettings.Count > 0)
            {
                _controls = new Sensor[coolerSettings.Count];
                _fanControls = new Control[coolerSettings.Count];

                for (int i = 0; i < coolerSettings.Count; i++)
                {
                    NvApi.NvCooler cooler = coolerSettings.Cooler[i];
                    string name = "GPU Fan" + (coolerSettings.Count > 1 ? " " + cooler.Controller : string.Empty);

                    _controls[i] = new Sensor(name, i, SensorType.Control, this, settings);
                    ActivateSensor(_controls[i]);

                    _fanControls[i] = new Control(_controls[i], settings, cooler.DefaultMin, cooler.DefaultMax);
                    _fanControls[i].ControlModeChanged += ControlModeChanged;
                    _fanControls[i].SoftwareControlValueChanged += SoftwareControlValueChanged;
                    _controls[i].Control = _fanControls[i];

                    ControlModeChanged(_fanControls[i]);
                }
            }
        }

        // Load usages.
        NvApi.NvDynamicPStatesInfo pStatesInfo = GetDynamicPstatesInfoEx(out status);
        if (status == NvApi.NvStatus.OK)
        {
            var loads = new List<Sensor>();
            for (int index = 0; index < pStatesInfo.Utilizations.Length; index++)
            {
                NvApi.NvDynamicPState load = pStatesInfo.Utilizations[index];
                if (load.IsPresent && Enum.IsDefined(typeof(NvApi.NvUtilizationDomain), index))
                {
                    var utilizationDomain = (NvApi.NvUtilizationDomain)index;
                    string name = GetUtilizationDomainName(utilizationDomain);

                    if (name != null)
                        loads.Add(new Sensor(name, index, SensorType.Load, this, settings));
                }
            }

            if (loads.Count > 0)
            {
                _loads = loads.ToArray();

                foreach (Sensor sensor in loads)
                    ActivateSensor(sensor);
            }
        }
        else
        {
            NvApi.NvUsages usages = GetUsages(out status);
            if (status == NvApi.NvStatus.OK)
            {
                var loads = new List<Sensor>();
                for (int index = 0; index < usages.Entries.Length; index++)
                {
                    NvApi.NvUsagesEntry load = usages.Entries[index];
                    if (load.IsPresent > 0 && Enum.IsDefined(typeof(NvApi.NvUtilizationDomain), index))
                    {
                        var utilizationDomain = (NvApi.NvUtilizationDomain)index;
                        string name = GetUtilizationDomainName(utilizationDomain);

                        if (name != null)
                            loads.Add(new Sensor(name, index, SensorType.Load, this, settings));
                    }
                }

                if (loads.Count > 0)
                {
                    _loads = loads.ToArray();

                    foreach (Sensor sensor in loads)
                        ActivateSensor(sensor);
                }
            }
        }

        // Power.
        NvApi.NvPowerTopology powerTopology = GetPowerTopology(out NvApi.NvStatus powerStatus);
        if (powerStatus == NvApi.NvStatus.OK && powerTopology.Count > 0)
        {
            _powers = new Sensor[powerTopology.Count];
            for (int i = 0; i < powerTopology.Count; i++)
            {
                NvApi.NvPowerTopologyEntry entry = powerTopology.Entries[i];
                string name = entry.Domain switch
                {
                    NvApi.NvPowerTopologyDomain.Gpu => "GPU Power",
                    NvApi.NvPowerTopologyDomain.Board => "GPU Board Power",
                    _ => null
                };

                if (name != null)
                {
                    _powers[i] = new Sensor(name, i + (_loads?.Length ?? 0), SensorType.Load, this, settings);
                    ActivateSensor(_powers[i]);
                }
            }
        }

        if (NvidiaML.IsAvailable || NvidiaML.Initialize())
        {
            if (hasBusId)
                _nvmlDevice = NvidiaML.NvmlDeviceGetHandleByPciBusId($" 0000:{busId:X2}:00.0") ?? NvidiaML.NvmlDeviceGetHandleByIndex(_adapterIndex);
            else
                _nvmlDevice = NvidiaML.NvmlDeviceGetHandleByIndex(_adapterIndex);

            if (_nvmlDevice.HasValue)
            {
                _powerUsage = new Sensor("GPU Package", 0, SensorType.Power, this, settings);

                _pcieThroughputRx = new Sensor("GPU PCIe Rx", 0, SensorType.Throughput, this, settings);
                _pcieThroughputTx = new Sensor("GPU PCIe Tx", 1, SensorType.Throughput, this, settings);

                if (!Software.OperatingSystem.IsUnix)
                {
                    NvidiaML.NvmlPciInfo? pciInfo = NvidiaML.NvmlDeviceGetPciInfo(_nvmlDevice.Value);

                    if (pciInfo is { } pci)
                    {
                        string[] deviceIds = D3DDisplayDevice.GetDeviceIdentifiers();
                        if (deviceIds != null)
                        {
                            foreach (string deviceId in deviceIds)
                            {
                                if (deviceId.IndexOf("VEN_" + pci.pciVendorId.ToString("X"), StringComparison.OrdinalIgnoreCase) != -1 &&
                                    deviceId.IndexOf("DEV_" + pci.pciDeviceId.ToString("X"), StringComparison.OrdinalIgnoreCase) != -1 &&
                                    deviceId.IndexOf("SUBSYS_" + pci.pciSubSystemId.ToString("X"), StringComparison.OrdinalIgnoreCase) != -1)
                                {
                                    bool isMatch = false;

                                    string actualDeviceId = D3DDisplayDevice.GetActualDeviceIdentifier(deviceId);

                                    try
                                    {
                                        if (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Enum", adapterIndex.ToString(), null) is string adapterPnpId)
                                        {
                                            if (actualDeviceId.IndexOf(adapterPnpId, StringComparison.OrdinalIgnoreCase) != -1 ||
                                                adapterPnpId.IndexOf(actualDeviceId, StringComparison.OrdinalIgnoreCase) != -1)
                                            {
                                                isMatch = true;
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        // Ignored.
                                    }

                                    if (!isMatch)
                                    {
                                        try
                                        {
                                            string path = actualDeviceId;
                                            path = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\" + path;

                                            if (Registry.GetValue(path, "LocationInformation", null) is string locationInformation)
                                            {
                                                // For example:
                                                // @System32\drivers\pci.sys,#65536;PCI bus %1, device %2, function %3;(38,0,0)

                                                int index = locationInformation.IndexOf('(');
                                                if (index != -1)
                                                {
                                                    index++;
                                                    int secondIndex = locationInformation.IndexOf(',', index);
                                                    if (secondIndex != -1)
                                                    {
                                                        string bus = locationInformation.Substring(index, secondIndex - index);

                                                        if (pci.bus.ToString() == bus)
                                                            isMatch = true;
                                                    }
                                                }
                                            }
                                        }
                                        catch
                                        {
                                            // Ignored.
                                        }
                                    }

                                    if (isMatch && D3DDisplayDevice.GetDeviceInfoByIdentifier(deviceId, out D3DDisplayDevice.D3DDeviceInfo deviceInfo))
                                    {
                                        int nodeSensorIndex = (_loads?.Length ?? 0) + (_powers?.Length ?? 0);
                                        int memorySensorIndex = 4; // There are 4 normal GPU memory sensors.

                                        _d3dDeviceId = deviceId;

                                        _gpuDedicatedMemoryUsage = new Sensor("D3D Dedicated Memory Used", memorySensorIndex++, SensorType.SmallData, this, settings);
                                        _gpuSharedMemoryUsage = new Sensor("D3D Shared Memory Used", memorySensorIndex, SensorType.SmallData, this, settings);

                                        _gpuNodeUsage = new Sensor[deviceInfo.Nodes.Length];
                                        _gpuNodeUsagePrevValue = new long[deviceInfo.Nodes.Length];
                                        _gpuNodeUsagePrevTick = new DateTime[deviceInfo.Nodes.Length];

                                        foreach (D3DDisplayDevice.D3DDeviceNodeInfo node in deviceInfo.Nodes.OrderBy(x => x.Name))
                                        {
                                            _gpuNodeUsage[node.Id] = new Sensor(node.Name, nodeSensorIndex++, SensorType.Load, this, settings);
                                            _gpuNodeUsagePrevValue[node.Id] = node.RunningTime;
                                            _gpuNodeUsagePrevTick[node.Id] = node.QueryTime;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        _memoryFree = new Sensor("GPU Memory Free", 0, SensorType.SmallData, this, settings);
        _memoryUsed = new Sensor("GPU Memory Used", 1, SensorType.SmallData, this, settings);
        _memoryTotal = new Sensor("GPU Memory Total", 2, SensorType.SmallData, this, settings);
        _memoryLoad = new Sensor("GPU Memory", 3, SensorType.Load, this, settings);

        Update();
    }

    /// <inheritdoc />
    public override string DeviceId
    {
        get
        {
            return _d3dDeviceId != null ? D3DDisplayDevice.GetActualDeviceIdentifier(_d3dDeviceId) : null;
        }
    }

    public override HardwareType HardwareType
    {
        get { return HardwareType.GpuNvidia; }
    }

    public override void Update()
    {
        if (_d3dDeviceId != null && D3DDisplayDevice.GetDeviceInfoByIdentifier(_d3dDeviceId, out D3DDisplayDevice.D3DDeviceInfo deviceInfo))
        {
            _gpuDedicatedMemoryUsage.Value = 1f * deviceInfo.GpuDedicatedUsed / 1024 / 1024;
            _gpuSharedMemoryUsage.Value = 1f * deviceInfo.GpuSharedUsed / 1024 / 1024;
            ActivateSensor(_gpuDedicatedMemoryUsage);
            ActivateSensor(_gpuSharedMemoryUsage);

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

        NvApi.NvStatus status;

        if (_temperatures is { Length: > 0 })
        {
            NvApi.NvThermalSettings settings = GetThermalSettings(out status);
            // settings.Count is 0 when no valid data available, this happens when you try to read out this value with a high polling interval.
            if (status == NvApi.NvStatus.OK && settings.Count > 0)
            {
                foreach (Sensor sensor in _temperatures)
                    sensor.Value = settings.Sensor[sensor.Index].CurrentTemp;
            }
        }

        if (_thermalSensorsMask > 0)
        {
            NvApi.NvThermalSensors thermalSensors = GetThermalSensors(_thermalSensorsMask, out status);

            if (status == NvApi.NvStatus.OK)
            {
                _hotSpotTemperature.Value = thermalSensors.Temperatures[1] / 256.0f;
                _memoryJunctionTemperature.Value = thermalSensors.Temperatures[9] / 256.0f;
            }

            if (_hotSpotTemperature.Value != 0)
                ActivateSensor(_hotSpotTemperature);

            if (_memoryJunctionTemperature.Value != 0)
                ActivateSensor(_memoryJunctionTemperature);
        }
        else
        {
            _hotSpotTemperature.Value = null;
            _memoryJunctionTemperature.Value = null;
        }

        if (_clocks is { Length: > 0 })
        {
            NvApi.NvGpuClockFrequencies clockFrequencies = GetClockFrequencies(out status);
            if (status == NvApi.NvStatus.OK)
            {
                int current = 0;
                for (int i = 0; i < clockFrequencies.Clocks.Length; i++)
                {
                    NvApi.NvGpuClockFrequenciesDomain clock = clockFrequencies.Clocks[i];
                    if (clock.IsPresent && Enum.IsDefined(typeof(NvApi.NvGpuPublicClockId), i))
                        _clocks[current++].Value = clock.Frequency / 1000f;
                }
            }
        }

        if (_fans is { Length: > 0 })
        {
            NvApi.NvFanCoolersStatus fanCoolers = GetFanCoolersStatus(out status);
            if (status == NvApi.NvStatus.OK && fanCoolers.Count > 0)
            {
                for (int i = 0; i < fanCoolers.Count; i++)
                {
                    NvApi.NvFanCoolersStatusItem item = fanCoolers.Items[i];
                    _fans[i].Value = item.CurrentRpm;
                }
            }
            else
            {
                int tachReading = GetTachReading(out status);
                if (status == NvApi.NvStatus.OK)
                    _fans[0].Value = tachReading;
            }
        }

        if (_controls is { Length: > 0 })
        {
            NvApi.NvFanCoolersStatus fanCoolers = GetFanCoolersStatus(out status);
            if (status == NvApi.NvStatus.OK && fanCoolers.Count > 0 && fanCoolers.Count == _controls.Length)
            {
                for (int i = 0; i < fanCoolers.Count; i++)
                {
                    NvApi.NvFanCoolersStatusItem item = fanCoolers.Items[i];

                    if (Array.Find(_controls, c => c.Index == item.CoolerId) is { } control)
                        control.Value = item.CurrentLevel;
                }
            }
            else
            {
                NvApi.NvCoolerSettings coolerSettings = GetCoolerSettings(out status);
                if (status == NvApi.NvStatus.OK && coolerSettings.Count > 0)
                {
                    for (int i = 0; i < coolerSettings.Count; i++)
                    {
                        NvApi.NvCooler cooler = coolerSettings.Cooler[i];
                        _controls[i].Value = cooler.CurrentLevel;
                    }
                }
            }
        }

        if (_loads is { Length: > 0 })
        {
            NvApi.NvDynamicPStatesInfo pStatesInfo = GetDynamicPstatesInfoEx(out status);
            if (status == NvApi.NvStatus.OK)
            {
                for (int index = 0; index < pStatesInfo.Utilizations.Length; index++)
                {
                    NvApi.NvDynamicPState load = pStatesInfo.Utilizations[index];
                    if (load.IsPresent && Enum.IsDefined(typeof(NvApi.NvUtilizationDomain), index))
                        _loads[index].Value = load.Percentage;
                }
            }
            else
            {
                NvApi.NvUsages usages = GetUsages(out status);
                if (status == NvApi.NvStatus.OK)
                {
                    for (int index = 0; index < usages.Entries.Length; index++)
                    {
                        NvApi.NvUsagesEntry load = usages.Entries[index];
                        if (load.IsPresent > 0 && Enum.IsDefined(typeof(NvApi.NvUtilizationDomain), index))
                            _loads[index].Value = load.Percentage;
                    }
                }
            }
        }

        if (_powers is { Length: > 0 })
        {
            NvApi.NvPowerTopology powerTopology = GetPowerTopology(out status);
            if (status == NvApi.NvStatus.OK && powerTopology.Count > 0)
            {
                for (int i = 0; i < powerTopology.Count; i++)
                {
                    NvApi.NvPowerTopologyEntry entry = powerTopology.Entries[i];
                    _powers[i].Value = entry.PowerUsage / 1000f;
                }
            }
        }

        if (_displayHandle != null)
        {
            NvApi.NvMemoryInfo memoryInfo = GetMemoryInfo(out status);
            if (status == NvApi.NvStatus.OK)
            {
                uint free = memoryInfo.CurrentAvailableDedicatedVideoMemory;
                uint total = memoryInfo.DedicatedVideoMemory;

                _memoryTotal.Value = total / 1024;
                ActivateSensor(_memoryTotal);

                _memoryFree.Value = free / 1024;
                ActivateSensor(_memoryFree);

                _memoryUsed.Value = (total - free) / 1024;
                ActivateSensor(_memoryUsed);

                _memoryLoad.Value = ((float)(total - free) / total) * 100;
                ActivateSensor(_memoryLoad);
            }
        }

        if (NvidiaML.IsAvailable && _nvmlDevice.HasValue)
        {
            int? result = NvidiaML.NvmlDeviceGetPowerUsage(_nvmlDevice.Value);
            if (result.HasValue)
            {
                _powerUsage.Value = result.Value / 1000f;
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
        StringBuilder r = new();

        r.AppendLine("Nvidia GPU");
        r.AppendLine();
        r.AppendFormat("Name: {0}{1}", _name, Environment.NewLine);
        r.AppendFormat("Index: {0}{1}", _adapterIndex, Environment.NewLine);

        if (_displayHandle.HasValue && NvApi.NvAPI_GetDisplayDriverVersion != null)
        {
            NvApi.NvDisplayDriverVersion driverVersion = new() { Version = (uint)NvApi.MAKE_NVAPI_VERSION<NvApi.NvDisplayDriverVersion>(1) };
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
            NvApi.NvThermalSettings thermalSettings = GetThermalSettings(out NvApi.NvStatus status);

            r.AppendLine("Thermal Settings");
            r.AppendLine();

            if (status == NvApi.NvStatus.OK)
            {
                for (int i = 0; i < thermalSettings.Count; i++)
                {
                    r.AppendFormat(" Sensor[{0}].Controller: {1}{2}", i, thermalSettings.Sensor[i].Controller, Environment.NewLine);
                    r.AppendFormat(" Sensor[{0}].DefaultMinTemp: {1}{2}", i, thermalSettings.Sensor[i].DefaultMinTemp, Environment.NewLine);
                    r.AppendFormat(" Sensor[{0}].DefaultMaxTemp: {1}{2}", i, thermalSettings.Sensor[i].DefaultMaxTemp, Environment.NewLine);
                    r.AppendFormat(" Sensor[{0}].CurrentTemp: {1}{2}", i, thermalSettings.Sensor[i].CurrentTemp, Environment.NewLine);
                    r.AppendFormat(" Sensor[{0}].Target: {1}{2}", i, thermalSettings.Sensor[i].Target, Environment.NewLine);
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
            NvApi.NvGpuClockFrequencies clocks = GetClockFrequencies(out NvApi.NvStatus status);

            r.AppendLine("Clocks");
            r.AppendLine();
            if (status == NvApi.NvStatus.OK)
            {
                for (int i = 0; i < clocks.Clocks.Length; i++)
                {
                    if (clocks.Clocks[i].IsPresent)
                        r.AppendFormat(" Clock[{0}]: {1}{2}", i, clocks.Clocks[i].Frequency, Environment.NewLine);
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

        if (NvApi.NvAPI_GPU_GetDynamicPstatesInfoEx != null)
        {
            NvApi.NvDynamicPStatesInfo pStatesInfo = GetDynamicPstatesInfoEx(out NvApi.NvStatus status);

            r.AppendLine("P-States");
            r.AppendLine();
            if (status == NvApi.NvStatus.OK)
            {
                for (int i = 0; i < pStatesInfo.Utilizations.Length; i++)
                {
                    if (pStatesInfo.Utilizations[i].IsPresent)
                        r.AppendFormat(" Percentage[{0}]: {1}{2}", i, pStatesInfo.Utilizations[i].Percentage, Environment.NewLine);
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
            NvApi.NvUsages usages = GetUsages(out NvApi.NvStatus status);

            r.AppendLine("Usages");
            r.AppendLine();
            if (status == NvApi.NvStatus.OK)
            {
                for (int i = 0; i < usages.Entries.Length; i++)
                {
                    if (usages.Entries[i].IsPresent > 0)
                        r.AppendFormat(" Usage[{0}]: {1}{2}", i, usages.Entries[i].Percentage, Environment.NewLine);
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
            NvApi.NvCoolerSettings coolerSettings = GetCoolerSettings(out NvApi.NvStatus status);
            r.AppendLine("Cooler Settings");
            r.AppendLine();
            if (status == NvApi.NvStatus.OK)
            {
                for (int i = 0; i < coolerSettings.Count; i++)
                {
                    r.AppendFormat(" Cooler[{0}].Type: {1}{2}", i, coolerSettings.Cooler[i].Type, Environment.NewLine);
                    r.AppendFormat(" Cooler[{0}].Controller: {1}{2}", i, coolerSettings.Cooler[i].Controller, Environment.NewLine);
                    r.AppendFormat(" Cooler[{0}].DefaultMin: {1}{2}", i, coolerSettings.Cooler[i].DefaultMin, Environment.NewLine);
                    r.AppendFormat(" Cooler[{0}].DefaultMax: {1}{2}", i, coolerSettings.Cooler[i].DefaultMax, Environment.NewLine);
                    r.AppendFormat(" Cooler[{0}].CurrentMin: {1}{2}", i, coolerSettings.Cooler[i].CurrentMin, Environment.NewLine);
                    r.AppendFormat(" Cooler[{0}].CurrentMax: {1}{2}", i, coolerSettings.Cooler[i].CurrentMax, Environment.NewLine);
                    r.AppendFormat(" Cooler[{0}].CurrentLevel: {1}{2}", i, coolerSettings.Cooler[i].CurrentLevel, Environment.NewLine);
                    r.AppendFormat(" Cooler[{0}].DefaultPolicy: {1}{2}", i, coolerSettings.Cooler[i].DefaultPolicy, Environment.NewLine);
                    r.AppendFormat(" Cooler[{0}].CurrentPolicy: {1}{2}", i, coolerSettings.Cooler[i].CurrentPolicy, Environment.NewLine);
                    r.AppendFormat(" Cooler[{0}].Target: {1}{2}", i, coolerSettings.Cooler[i].Target, Environment.NewLine);
                    r.AppendFormat(" Cooler[{0}].ControlType: {1}{2}", i, coolerSettings.Cooler[i].ControlType, Environment.NewLine);
                    r.AppendFormat(" Cooler[{0}].Active: {1}{2}", i, coolerSettings.Cooler[i].Active, Environment.NewLine);
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
            NvApi.NvFanCoolersStatus coolers = GetFanCoolersStatus(out NvApi.NvStatus status);

            r.AppendLine("Fan Coolers Status");
            r.AppendLine();
            if (status == NvApi.NvStatus.OK)
            {
                for (int i = 0; i < coolers.Count; i++)
                {
                    r.AppendFormat(" Items[{0}].CoolerId: {1}{2}",
                                   i,
                                   coolers.Items[i].CoolerId,
                                   Environment.NewLine);

                    r.AppendFormat(" Items[{0}].CurrentRpm: {1}{2}",
                                   i,
                                   coolers.Items[i].CurrentRpm,
                                   Environment.NewLine);

                    r.AppendFormat(" Items[{0}].CurrentMinLevel: {1}{2}",
                                   i,
                                   coolers.Items[i].CurrentMinLevel,
                                   Environment.NewLine);

                    r.AppendFormat(" Items[{0}].CurrentMaxLevel: {1}{2}",
                                   i,
                                   coolers.Items[i].CurrentMaxLevel,
                                   Environment.NewLine);

                    r.AppendFormat(" Items[{0}].CurrentLevel: {1}{2}",
                                   i,
                                   coolers.Items[i].CurrentLevel,
                                   Environment.NewLine);
                }
            }
            else
            {
                r.Append(" Status: ");
                r.AppendLine(status.ToString());
            }

            r.AppendLine();
        }

        if (NvApi.NvAPI_GPU_ClientPowerTopologyGetStatus != null)
        {
            NvApi.NvPowerTopology powerTopology = GetPowerTopology(out NvApi.NvStatus status);

            r.AppendLine("Power Topology");
            r.AppendLine();

            if (status == NvApi.NvStatus.OK)
            {
                for (int i = 0; i < powerTopology.Count; i++)
                {
                    NvApi.NvPowerTopologyEntry entry = powerTopology.Entries[i];
                    _powers[i].Value = entry.PowerUsage / 1000f;

                    r.AppendFormat(" Entries[{0}].Domain: {1}{2}", i, entry.Domain, Environment.NewLine);
                    r.AppendFormat(" Entries[{0}].PowerUsage: {1}{2}", i, entry.PowerUsage, Environment.NewLine);
                }
            }
            else
            {
                r.Append(" Status: ");
                r.AppendLine(status.ToString());
            }

            r.AppendLine();
        }

        if (NvApi.NvAPI_GPU_GetMemoryInfo != null)
        {
            NvApi.NvMemoryInfo memoryInfo = GetMemoryInfo(out NvApi.NvStatus status);

            r.AppendLine("Memory Info");
            r.AppendLine();
            if (status == NvApi.NvStatus.OK)
            {
                r.AppendFormat(" AvailableDedicatedVideoMemory: {0}{1}", memoryInfo.AvailableDedicatedVideoMemory, Environment.NewLine);
                r.AppendFormat(" DedicatedVideoMemory: {0}{1}", memoryInfo.DedicatedVideoMemory, Environment.NewLine);
                r.AppendFormat(" CurrentAvailableDedicatedVideoMemory: {0}{1}", memoryInfo.CurrentAvailableDedicatedVideoMemory, Environment.NewLine);
                r.AppendFormat(" SharedSystemMemory: {0}{1}", memoryInfo.SharedSystemMemory, Environment.NewLine);
                r.AppendFormat(" SystemVideoMemory: {0}{1}", memoryInfo.SystemVideoMemory, Environment.NewLine);
            }
            else
            {
                r.Append(" Status: ");
                r.AppendLine(status.ToString());
            }

            r.AppendLine();
        }

        if (_d3dDeviceId != null)
        {
            r.AppendLine("D3D");
            r.AppendLine();
            r.AppendLine(" Id: " + _d3dDeviceId);

            r.AppendLine();
        }

        return r.ToString();
    }

    private static string GetName(NvApi.NvPhysicalGpuHandle handle)
    {
        if (NvApi.NvAPI_GPU_GetFullName(handle, out string gpuName) == NvApi.NvStatus.OK)
        {
            string name = gpuName.Trim();
            return name.StartsWith("NVIDIA", StringComparison.OrdinalIgnoreCase) ? name : "NVIDIA " + name;
        }

        return "NVIDIA";
    }

    private NvApi.NvMemoryInfo GetMemoryInfo(out NvApi.NvStatus status)
    {
        if (NvApi.NvAPI_GPU_GetMemoryInfo == null || _displayHandle == null)
        {
            status = NvApi.NvStatus.Error;
            return default;
        }

        NvApi.NvMemoryInfo memoryInfo = new()
        {
            Version = (uint)NvApi.MAKE_NVAPI_VERSION<NvApi.NvMemoryInfo>(2)
        };

        status = NvApi.NvAPI_GPU_GetMemoryInfo(_displayHandle.Value, ref memoryInfo);
        return status == NvApi.NvStatus.OK ? memoryInfo : default;
    }

    private NvApi.NvGpuClockFrequencies GetClockFrequencies(out NvApi.NvStatus status)
    {
        if (NvApi.NvAPI_GPU_GetAllClockFrequencies == null)
        {
            status = NvApi.NvStatus.Error;
            return default;
        }

        NvApi.NvGpuClockFrequencies clockFrequencies = new()
        {
            Version = (uint)NvApi.MAKE_NVAPI_VERSION<NvApi.NvGpuClockFrequencies>(_clockVersion)
        };

        status = NvApi.NvAPI_GPU_GetAllClockFrequencies(_handle, ref clockFrequencies);
        return status == NvApi.NvStatus.OK ? clockFrequencies : default;
    }

    private NvApi.NvThermalSettings GetThermalSettings(out NvApi.NvStatus status)
    {
        if (NvApi.NvAPI_GPU_GetThermalSettings == null)
        {
            status = NvApi.NvStatus.Error;
            return default;
        }

        NvApi.NvThermalSettings settings = new()
        {
            Version = (uint)NvApi.MAKE_NVAPI_VERSION<NvApi.NvThermalSettings>(1),
            Count = NvApi.MAX_THERMAL_SENSORS_PER_GPU
        };

        status = NvApi.NvAPI_GPU_GetThermalSettings(_handle, (int)NvApi.NvThermalTarget.All, ref settings);
        return status == NvApi.NvStatus.OK ? settings : default;
    }

    private NvApi.NvThermalSensors GetThermalSensors(uint mask, out NvApi.NvStatus status)
    {
        if (NvApi.NvAPI_GPU_ThermalGetSensors == null)
        {
            status = NvApi.NvStatus.Error;
            return default;
        }

        var thermalSensors = new NvApi.NvThermalSensors
        {
            Version = (uint)NvApi.MAKE_NVAPI_VERSION<NvApi.NvThermalSensors>(2),
            Mask = mask
        };

        status = NvApi.NvAPI_GPU_ThermalGetSensors(_handle, ref thermalSensors);
        return status == NvApi.NvStatus.OK ? thermalSensors : default;
    }

    private NvApi.NvFanCoolersStatus GetFanCoolersStatus(out NvApi.NvStatus status)
    {
        if (NvApi.NvAPI_GPU_ClientFanCoolersGetStatus == null)
        {
            status = NvApi.NvStatus.Error;
            return default;
        }

        var coolers = new NvApi.NvFanCoolersStatus
        {
            Version = (uint)NvApi.MAKE_NVAPI_VERSION<NvApi.NvFanCoolersStatus>(1),
            Items = new NvApi.NvFanCoolersStatusItem[NvApi.MAX_FAN_COOLERS_STATUS_ITEMS]
        };

        status = NvApi.NvAPI_GPU_ClientFanCoolersGetStatus(_handle, ref coolers);
        return status == NvApi.NvStatus.OK ? coolers : default;
    }

    private NvApi.NvFanCoolerControl GetFanCoolersControllers(out NvApi.NvStatus status)
    {
        if (NvApi.NvAPI_GPU_ClientFanCoolersGetControl == null)
        {
            status = NvApi.NvStatus.Error;
            return default;
        }

        var controllers = new NvApi.NvFanCoolerControl
        {
            Version = (uint)NvApi.MAKE_NVAPI_VERSION<NvApi.NvFanCoolerControl>(1)
        };

        status = NvApi.NvAPI_GPU_ClientFanCoolersGetControl(_handle, ref controllers);
        return status == NvApi.NvStatus.OK ? controllers : default;
    }

    private NvApi.NvCoolerSettings GetCoolerSettings(out NvApi.NvStatus status)
    {
        if (NvApi.NvAPI_GPU_GetCoolerSettings == null)
        {
            status = NvApi.NvStatus.Error;
            return default;
        }

        NvApi.NvCoolerSettings settings = new()
        {
            Version = (uint)NvApi.MAKE_NVAPI_VERSION<NvApi.NvCoolerSettings>(2),
            Cooler = new NvApi.NvCooler[NvApi.MAX_COOLERS_PER_GPU]
        };

        status = NvApi.NvAPI_GPU_GetCoolerSettings(_handle, NvApi.NvCoolerTarget.All, ref settings);
        return status == NvApi.NvStatus.OK ? settings : default;
    }

    private NvApi.NvDynamicPStatesInfo GetDynamicPstatesInfoEx(out NvApi.NvStatus status)
    {
        if (NvApi.NvAPI_GPU_GetDynamicPstatesInfoEx == null)
        {
            status = NvApi.NvStatus.Error;
            return default;
        }

        NvApi.NvDynamicPStatesInfo pStatesInfo = new()
        {
            Version = (uint)NvApi.MAKE_NVAPI_VERSION<NvApi.NvDynamicPStatesInfo>(1),
            Utilizations = new NvApi.NvDynamicPState[NvApi.MAX_GPU_UTILIZATIONS]
        };

        status = NvApi.NvAPI_GPU_GetDynamicPstatesInfoEx(_handle, ref pStatesInfo);
        return status == NvApi.NvStatus.OK ? pStatesInfo : default;
    }

    private NvApi.NvUsages GetUsages(out NvApi.NvStatus status)
    {
        if (NvApi.NvAPI_GPU_GetUsages == null)
        {
            status = NvApi.NvStatus.Error;
            return default;
        }

        NvApi.NvUsages usages = new()
        {
            Version = (uint)NvApi.MAKE_NVAPI_VERSION<NvApi.NvUsages>(1)
        };

        status = NvApi.NvAPI_GPU_GetUsages(_handle, ref usages);
        return status == NvApi.NvStatus.OK ? usages : default;
    }

    private NvApi.NvPowerTopology GetPowerTopology(out NvApi.NvStatus status)
    {
        if (NvApi.NvAPI_GPU_ClientPowerTopologyGetStatus == null)
        {
            status = NvApi.NvStatus.Error;
            return default;
        }

        NvApi.NvPowerTopology powerTopology = new()
        {
            Version = NvApi.MAKE_NVAPI_VERSION<NvApi.NvPowerTopology>(1)
        };

        status = NvApi.NvAPI_GPU_ClientPowerTopologyGetStatus(_handle, ref powerTopology);
        return status == NvApi.NvStatus.OK ? powerTopology : default;
    }

    private int GetTachReading(out NvApi.NvStatus status)
    {
        if (NvApi.NvAPI_GPU_GetTachReading == null)
        {
            status = NvApi.NvStatus.Error;
            return default;
        }

        status = NvApi.NvAPI_GPU_GetTachReading(_handle, out int value);
        return value;
    }

    private static string GetUtilizationDomainName(NvApi.NvUtilizationDomain utilizationDomain) => utilizationDomain switch
    {
        NvApi.NvUtilizationDomain.Gpu => "GPU Core",
        NvApi.NvUtilizationDomain.FrameBuffer => "GPU Memory Controller",
        NvApi.NvUtilizationDomain.VideoEngine => "GPU Video Engine",
        NvApi.NvUtilizationDomain.BusInterface => "GPU Bus",
        _ => null
    };

    private void ControlModeChanged(IControl control)
    {
        switch (control.ControlMode)
        {
            case ControlMode.Default:
                RestoreDefaultFanBehavior(control.Sensor.Index);
                break;
            case ControlMode.Software:
                SoftwareControlValueChanged(control);
                break;
        }
    }

    private void SoftwareControlValueChanged(IControl control)
    {
        int index = control.Sensor?.Index ?? 0;

        NvApi.NvCoolerLevels coolerLevels = new() { Version = (uint)NvApi.MAKE_NVAPI_VERSION<NvApi.NvCoolerLevels>(1), Levels = new NvApi.NvLevel[NvApi.MAX_COOLERS_PER_GPU] };
        coolerLevels.Levels[0].Level = (int)control.SoftwareValue;
        coolerLevels.Levels[0].Policy = NvApi.NvLevelPolicy.Manual;
        if (NvApi.NvAPI_GPU_SetCoolerLevels(_handle, index, ref coolerLevels) == NvApi.NvStatus.OK)
            return;

        NvApi.NvFanCoolerControl fanCoolersControllers = GetFanCoolersControllers(out _);

        for (int i = 0; i < fanCoolersControllers.Count; i++)
        {
            NvApi.NvFanCoolerControlItem nvFanCoolerControlItem = fanCoolersControllers.Items[i];
            if (nvFanCoolerControlItem.CoolerId == index)
            {
                nvFanCoolerControlItem.ControlMode = NvApi.NvFanControlMode.Manual;
                nvFanCoolerControlItem.Level = (uint)control.SoftwareValue;

                fanCoolersControllers.Items[i] = nvFanCoolerControlItem;
            }
        }

        NvApi.NvAPI_GPU_ClientFanCoolersSetControl(_handle, ref fanCoolersControllers);
    }

    private void RestoreDefaultFanBehavior(int index)
    {
        NvApi.NvCoolerLevels coolerLevels = new() { Version = (uint)NvApi.MAKE_NVAPI_VERSION<NvApi.NvCoolerLevels>(1), Levels = new NvApi.NvLevel[NvApi.MAX_COOLERS_PER_GPU] };
        coolerLevels.Levels[0].Policy = NvApi.NvLevelPolicy.Auto;
        if (NvApi.NvAPI_GPU_SetCoolerLevels(_handle, index, ref coolerLevels) == NvApi.NvStatus.OK)
            return;

        NvApi.NvFanCoolerControl fanCoolersControllers = GetFanCoolersControllers(out _);

        for (int i = 0; i < fanCoolersControllers.Count; i++)
        {
            NvApi.NvFanCoolerControlItem nvFanCoolerControlItem = fanCoolersControllers.Items[i];
            if (nvFanCoolerControlItem.CoolerId == index)
            {
                nvFanCoolerControlItem.ControlMode = NvApi.NvFanControlMode.Auto;
                nvFanCoolerControlItem.Level = 0;

                fanCoolersControllers.Items[i] = nvFanCoolerControlItem;
            }
        }

        NvApi.NvAPI_GPU_ClientFanCoolersSetControl(_handle, ref fanCoolersControllers);
    }

    public override void Close()
    {
        if (_fanControls != null)
        {
            for (int i = 0; i < _fanControls.Length; i++)
            {
                _fanControls[i].ControlModeChanged -= ControlModeChanged;
                _fanControls[i].SoftwareControlValueChanged -= SoftwareControlValueChanged;

                if (_fanControls[i].ControlMode != ControlMode.Undefined)
                    RestoreDefaultFanBehavior(i);
            }
        }

        base.Close();
    }
}
