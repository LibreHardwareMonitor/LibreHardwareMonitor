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
using NvAPIWrapper.GPU;
using NvAPIWrapper.Native.Exceptions;
using NvAPIWrapper.Native.GPU;
using NvAPIWrapper.Native.GPU.Structures;

namespace LibreHardwareMonitor.Hardware.Gpu
{
    internal sealed class NvidiaGpu : Hardware
    {
        private readonly int _adapterIndex;
        private readonly Sensor[] _clocks;
        private readonly Sensor[] _controls;
        private readonly Control[] _fanControls;
        private readonly Sensor[] _fans;
        private readonly Sensor _gpuDedicatedMemoryUsage;
        private readonly Sensor[] _gpuNodeUsage;
        private readonly DateTime[] _gpuNodeUsagePrevTick;
        private readonly long[] _gpuNodeUsagePrevValue;
        private readonly Sensor _gpuSharedMemoryUsage;
        private readonly Sensor[] _loads;
        private readonly Sensor _memoryFree;
        private readonly Sensor _memoryTotal;
        private readonly Sensor _memoryUsed;
        private readonly NvidiaML.NvmlDevice? _nvmlDevice;
        private readonly Sensor _pcieThroughputRx;
        private readonly Sensor _pcieThroughputTx;
        private readonly PhysicalGPU _physicalGpu;
        private readonly Sensor[] _powers;
        private readonly Sensor _powerUsage;
        private readonly Sensor[] _temperatures;
        private readonly string _windowsDeviceName;

        public NvidiaGpu(int adapterIndex, PhysicalGPU physicalGpu, ISettings settings)
            : base(GetName(physicalGpu),
                   new Identifier("gpu-nvidia", adapterIndex.ToString(CultureInfo.InvariantCulture)),
                   settings)
        {
            _adapterIndex = adapterIndex;
            _physicalGpu = physicalGpu;

            int busId = -1;

            try
            {
                busId = physicalGpu.BusInformation.BusId;
            }
            catch (Exception e) when (e is NVIDIAApiException or NVIDIANotSupportedException)
            { }

            try
            {
                GPUThermalSensor[] thermalSensors = physicalGpu.ThermalInformation.ThermalSensors.ToArray();
                _temperatures = new Sensor[thermalSensors.Length];

                for (int i = 0; i < thermalSensors.Length; i++)
                {
                    GPUThermalSensor sensor = thermalSensors[i];

                    string name = sensor.Target switch
                    {
                        ThermalSettingsTarget.GPU => "GPU Core",
                        ThermalSettingsTarget.Memory => "GPU Memory",
                        ThermalSettingsTarget.PowerSupply => "GPU Power Supply",
                        ThermalSettingsTarget.Board => "GPU Board",
                        ThermalSettingsTarget.VisualComputingBoard => "GPU Visual Computing Board",
                        ThermalSettingsTarget.VisualComputingInlet => "GPU Visual Computing Inlet",
                        ThermalSettingsTarget.VisualComputingOutlet => "GPU Visual Computing Outlet",
                        _ => "GPU"
                    };

                    _temperatures[i] = new Sensor(name, i, SensorType.Temperature, this, new ParameterDescription[0], settings);
                    ActivateSensor(_temperatures[i]);
                }
            }
            catch (Exception e) when (e is NVIDIAApiException or NVIDIANotSupportedException)
            { }

            try
            {
                KeyValuePair<PublicClockDomain, ClockDomainInfo>[] clocks = physicalGpu.CurrentClockFrequencies.Clocks.OrderBy(x => x.Key).ToArray();
                _clocks = new Sensor[clocks.Length];

                for (int i = 0; i < clocks.Length; i++)
                {
                    KeyValuePair<PublicClockDomain, ClockDomainInfo> clock = clocks[i];

                    string name = clock.Key switch
                    {
                        PublicClockDomain.Graphics => "GPU Core",
                        PublicClockDomain.Memory => "GPU Memory",
                        PublicClockDomain.Processor => "GPU Shader",
                        PublicClockDomain.Video => "GPU Video",
                        _ => null
                    };

                    _clocks[i] = new Sensor(name, i, SensorType.Clock, this, settings);
                    ActivateSensor(_clocks[i]);
                }
            }
            catch (Exception e) when (e is NVIDIAApiException or NVIDIANotSupportedException)
            { }

            try
            {
                GPUUsageDomainStatus[] usages = physicalGpu.UsageInformation.UtilizationDomainsStatus.ToArray();
                _loads = new Sensor[usages.Length + 1];

                for (int i = 0; i < usages.Length; i++)
                {
                    string name = usages[i].Domain switch
                    {
                        UtilizationDomain.GPU => "GPU Core",
                        UtilizationDomain.FrameBuffer => "GPU Memory Controller",
                        UtilizationDomain.VideoEngine => "GPU Video Engine",
                        UtilizationDomain.BusInterface => "GPU Bus",
                        _ => null
                    };

                    _loads[i] = new Sensor(name, i, SensorType.Load, this, settings);
                    ActivateSensor(_loads[i]);
                }

                _loads[_loads.Length - 1] = new Sensor("GPU Memory", _loads.Length - 1, SensorType.Load, this, settings);
                ActivateSensor(_loads[_loads.Length - 1]);
            }
            catch (Exception e) when (e is NVIDIAApiException or NVIDIANotSupportedException)
            { }

            try
            {
                GPUCooler[] coolers = physicalGpu.CoolerInformation.Coolers.ToArray();
                if (coolers.Length > 0)
                {
                    _controls = new Sensor[coolers.Length];
                    _fanControls = new Control[coolers.Length];
                    _fans = new Sensor[coolers.Length];

                    for (int i = 0; i < coolers.Length; i++)
                    {
                        GPUCooler cooler = coolers[i];
                        string name = "GPU Fan" + (coolers.Length > 1 ? " " + (cooler.CoolerId) : string.Empty);

                        _fans[i] = new Sensor(name, i, SensorType.Fan, this, settings);
                        ActivateSensor(_fans[i]);

                        _controls[i] = new Sensor(name, i, SensorType.Control, this, settings);
                        ActivateSensor(_controls[i]);

                        _fanControls[i] = new Control(_controls[i], settings, cooler.DefaultMinimumLevel, cooler.DefaultMaximumLevel);
                        _fanControls[i].ControlModeChanged += ControlModeChanged;
                        _fanControls[i].SoftwareControlValueChanged += SoftwareControlValueChanged;
                        _controls[i].Control = _fanControls[i];

                        ControlModeChanged(_fanControls[i]);
                    }
                }
            }
            catch (Exception e) when (e is NVIDIAApiException or NVIDIANotSupportedException)
            { }

            try
            {
                GPUPowerTopologyStatus[] powerTopologies = physicalGpu.PowerTopologyInformation.PowerTopologyEntries.ToArray();
                if (powerTopologies.Length > 0)
                {
                    _powers = new Sensor[powerTopologies.Length];

                    for (int i = 0; i < powerTopologies.Length; i++)
                    {
                        GPUPowerTopologyStatus powerTopology = powerTopologies[i];

                        string name = powerTopology.Domain switch
                        {
                            PowerTopologyDomain.GPU => "GPU Power",
                            PowerTopologyDomain.Board => "GPU Board Power",
                            _ => null
                        };

                        _powers[i] = new Sensor(name, i + (_loads?.Length ?? 0), SensorType.Load, this, settings);
                        ActivateSensor(_powers[i]);
                    }
                }
            }
            catch (Exception e) when (e is NVIDIAApiException or NVIDIANotSupportedException)
            { }

            if (NvidiaML.IsAvailable || NvidiaML.Initialize())
            {
                if (busId != -1)
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
                            string[] deviceIdentifiers = D3DDisplayDevice.GetDeviceIdentifiers();
                            if (deviceIdentifiers != null)
                            {
                                foreach (string deviceIdentifier in deviceIdentifiers)
                                {
                                    if (deviceIdentifier.IndexOf("VEN_" + pci.pciVendorId.ToString("X"), StringComparison.OrdinalIgnoreCase) != -1 &&
                                        deviceIdentifier.IndexOf("DEV_" + pci.pciDeviceId.ToString("X"), StringComparison.OrdinalIgnoreCase) != -1 &&
                                        deviceIdentifier.IndexOf("SUBSYS_" + pci.pciSubSystemId.ToString("X"), StringComparison.OrdinalIgnoreCase) != -1)
                                    {
                                        bool isMatch = false;

                                        try
                                        {
                                            if (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Enum", adapterIndex.ToString(), null) is string adapterPnpId)
                                            {
                                                if (deviceIdentifier.IndexOf(adapterPnpId.Replace('\\', '#'), StringComparison.OrdinalIgnoreCase) != -1)
                                                    isMatch = true;
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
                                                string path = deviceIdentifier;
                                                if (path.StartsWith(@"\\?\"))
                                                    path = path.Substring(4);

                                                path = path.Replace('#', '\\');
                                                int index = path.IndexOf('{');
                                                if (index != -1)
                                                    path = path.Substring(0, index);

                                                path = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\" + path;

                                                if (Registry.GetValue(path, "LocationInformation", null) is string locationInformation)
                                                {
                                                    // For example:
                                                    // @System32\drivers\pci.sys,#65536;PCI bus %1, device %2, function %3;(38,0,0)

                                                    index = locationInformation.IndexOf('(');
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

                                        if (isMatch && D3DDisplayDevice.GetDeviceInfoByIdentifier(deviceIdentifier, out D3DDisplayDevice.D3DDeviceInfo deviceInfo))
                                        {
                                            int nodeSensorIndex = (_loads?.Length ?? 0) + (_powers?.Length ?? 0);
                                            int memorySensorIndex = 3; // There are three normal GPU memory sensors.

                                            _windowsDeviceName = deviceIdentifier;

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

            Update();
        }

        public override HardwareType HardwareType
        {
            get { return HardwareType.GpuNvidia; }
        }

        private static string GetName(PhysicalGPU physicalGpu)
        {
            string gpuName = null;

            try
            {
                gpuName = physicalGpu.FullName?.Trim();
            }
            catch (NVIDIAApiException)
            { }

            gpuName ??= "Unknown";

            if (gpuName.StartsWith("NVIDIA", StringComparison.OrdinalIgnoreCase))
                return gpuName;


            return "NVIDIA " + gpuName.Trim();
        }

        public override void Update()
        {
            if (_windowsDeviceName != null && D3DDisplayDevice.GetDeviceInfoByIdentifier(_windowsDeviceName, out D3DDisplayDevice.D3DDeviceInfo deviceInfo))
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

            try
            {
                if (_temperatures is { Length: > 0 })
                {
                    GPUThermalSensor[] thermalSensors = _physicalGpu.ThermalInformation.ThermalSensors.ToArray();
                    for (int i = 0; i < thermalSensors.Length; i++)
                    {
                        GPUThermalSensor sensor = thermalSensors[i];
                        _temperatures[i].Value = sensor.CurrentTemperature;
                    }
                }
            }
            catch (Exception e) when (e is NVIDIAApiException or NVIDIANotSupportedException)
            { }

            try
            {
                if (_clocks is { Length: > 0 })
                {
                    KeyValuePair<PublicClockDomain, ClockDomainInfo>[] clocks = _physicalGpu.CurrentClockFrequencies.Clocks.OrderBy(x => x.Key).ToArray();
                    for (int i = 0; i < clocks.Length; i++)
                    {
                        KeyValuePair<PublicClockDomain, ClockDomainInfo> clock = clocks[i];
                        _clocks[i].Value = clock.Value.Frequency / 1000f;
                    }
                }
            }
            catch (Exception e) when (e is NVIDIAApiException or NVIDIANotSupportedException)
            { }

            GPUMemoryInformation memoryInformation = _physicalGpu.MemoryInformation;

            try
            {
                GPUUsageDomainStatus[] usages = _physicalGpu.UsageInformation.UtilizationDomainsStatus.ToArray();
                for (int i = 0; i < usages.Length; i++)
                    _loads[i].Value = usages[i].Percentage;

                uint current = memoryInformation.CurrentAvailableDedicatedVideoMemoryInkB;
                uint total = memoryInformation.DedicatedVideoMemoryInkB;

                _loads[_loads.Length - 1].Value = 100f * (total - current) / total;
            }
            catch (Exception e) when (e is NVIDIAApiException or NVIDIANotSupportedException)
            { }

            try
            {
                GPUCooler[] coolers = _physicalGpu.CoolerInformation.Coolers.ToArray();
                if (coolers.Length > 0)
                {
                    for (int i = 0; i < coolers.Length; i++)
                    {
                        GPUCooler cooler = coolers[i];

                        _fans[i].Value = cooler.CurrentFanSpeedInRPM;
                        _controls[i].Value = cooler.CurrentLevel;
                    }
                }
            }
            catch (Exception e) when (e is NVIDIAApiException or NVIDIANotSupportedException)
            { }

            try
            {
                GPUPowerTopologyStatus[] powerTopologies = _physicalGpu.PowerTopologyInformation.PowerTopologyEntries.ToArray();
                if (powerTopologies.Length > 0)
                {
                    for (int i = 0; i < powerTopologies.Length; i++)
                    {
                        GPUPowerTopologyStatus powerTopology = powerTopologies[i];
                        _powers[i].Value = powerTopology.PowerUsageInPercent;
                    }
                }
            }
            catch (Exception e) when (e is NVIDIAApiException or NVIDIANotSupportedException)
            { }

            try
            {
                uint current = memoryInformation.CurrentAvailableDedicatedVideoMemoryInkB;
                uint total = memoryInformation.DedicatedVideoMemoryInkB;

                _memoryTotal.Value = total / 1024;
                ActivateSensor(_memoryTotal);

                _memoryUsed.Value = current / 1024;
                ActivateSensor(_memoryUsed);

                _memoryFree.Value = (total - current) / 1024;
                ActivateSensor(_memoryFree);
            }
            catch (Exception e) when (e is NVIDIAApiException or NVIDIANotSupportedException)
            { }

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
            StringBuilder r = new();

            r.AppendLine("Nvidia GPU");
            r.AppendLine();
            r.AppendFormat("Name: {0}{1}", _name, Environment.NewLine);
            r.AppendFormat("Index: {0}{1}", _adapterIndex, Environment.NewLine);

            try
            {
                PCIIdentifiers pciIdentifiers = _physicalGpu.BusInformation.PCIIdentifiers;
                r.Append("DeviceID: 0x");
                r.AppendLine(pciIdentifiers.DeviceId.ToString("X", CultureInfo.InvariantCulture));
                r.Append("SubSystemID: 0x");
                r.AppendLine(pciIdentifiers.SubSystemId.ToString("X", CultureInfo.InvariantCulture));
                r.Append("RevisionID: 0x");
                r.AppendLine(pciIdentifiers.RevisionId.ToString("X", CultureInfo.InvariantCulture));
                r.Append("ExtDeviceID: 0x");
                r.AppendLine(pciIdentifiers.ExternalDeviceId.ToString("X", CultureInfo.InvariantCulture));
                r.AppendLine();
            }
            catch (Exception e) when (e is NVIDIAApiException or NVIDIANotSupportedException)
            { }

            try
            {
                GPUThermalSensor[] thermalSensors = _physicalGpu.ThermalInformation.ThermalSensors.ToArray();

                r.AppendLine("Thermal Settings");
                r.AppendLine();

                for (int i = 0; i < thermalSensors.Length; i++)
                {
                    r.AppendFormat(" Sensor[{0}].Target: {1}{2}", i, thermalSensors[i].Target, Environment.NewLine);
                    r.AppendFormat(" Sensor[{0}].SensorId: {1}{2}", i, thermalSensors[i].SensorId, Environment.NewLine);
                    r.AppendFormat(" Sensor[{0}].DefaultMinimumTemperature: {1}{2}", i, thermalSensors[i].DefaultMinimumTemperature, Environment.NewLine);
                    r.AppendFormat(" Sensor[{0}].DefaultMaximumTemperature: {1}{2}", i, thermalSensors[i].DefaultMaximumTemperature, Environment.NewLine);
                    r.AppendFormat(" Sensor[{0}].CurrentTemperature: {1}{2}", i, thermalSensors[i].CurrentTemperature, Environment.NewLine);
                }

                r.AppendLine();
            }
            catch (Exception e) when (e is NVIDIAApiException or NVIDIANotSupportedException)
            { }

            try
            {
                KeyValuePair<PublicClockDomain, ClockDomainInfo>[] clocks = _physicalGpu.CurrentClockFrequencies.Clocks.OrderBy(x => x.Key).ToArray();

                r.AppendLine("Clocks");
                r.AppendLine();

                for (int i = 0; i < clocks.Length; i++)
                {
                    KeyValuePair<PublicClockDomain, ClockDomainInfo> clock = clocks[i];
                    r.AppendFormat(" Clock[{0}]: {1}{2}", i, clock, Environment.NewLine);
                }

                r.AppendLine();
            }
            catch (Exception e) when (e is NVIDIAApiException or NVIDIANotSupportedException)
            { }

            try
            {
                GPUCooler[] coolers = _physicalGpu.CoolerInformation.Coolers.ToArray();

                r.AppendLine("Coolers");
                r.AppendLine();

                if (coolers.Length > 0)
                {
                    for (int i = 0; i < coolers.Length; i++)
                    {
                        GPUCooler cooler = coolers[i];

                        r.AppendFormat(" Cooler[{0}].CoolerId: {1}{2}", i, cooler.CoolerId, Environment.NewLine);
                        r.AppendFormat(" Cooler[{0}].CoolerType: {1}{2}", i, cooler.CoolerType, Environment.NewLine);
                        r.AppendFormat(" Cooler[{0}].ControlMode: {1}{2}", i, cooler.ControlMode, Environment.NewLine);
                        r.AppendFormat(" Cooler[{0}].CurrentPolicy: {1}{2}", i, cooler.CurrentPolicy, Environment.NewLine);
                        r.AppendFormat(" Cooler[{0}].CurrentLevel: {1}{2}", i, cooler.CurrentLevel, Environment.NewLine);
                        r.AppendFormat(" Cooler[{0}].CurrentFanSpeedInRPM: {1}{2}", i, cooler.CurrentFanSpeedInRPM, Environment.NewLine);
                    }
                }

                r.AppendLine();
            }
            catch (Exception e) when (e is NVIDIAApiException or NVIDIANotSupportedException)
            { }

            try
            {
                GPUUsageDomainStatus[] usages = _physicalGpu.UsageInformation.UtilizationDomainsStatus.ToArray();

                r.AppendLine("Usages");
                r.AppendLine();

                for (int i = 0; i < usages.Length; i++)
                {
                    GPUUsageDomainStatus usage = usages[i];

                    r.AppendFormat(" Usage[{0}].Domain: {1}{2}", i, usage.Domain, Environment.NewLine);
                    r.AppendFormat(" Usage[{0}].Percentage: {1}{2}", i, usage.Percentage, Environment.NewLine);
                }

                r.AppendLine();
            }
            catch (Exception e) when (e is NVIDIAApiException or NVIDIANotSupportedException)
            { }

            try
            {
                GPUMemoryInformation memoryInformation = _physicalGpu.MemoryInformation;

                r.AppendLine("Memory Info");
                r.AppendLine();

                r.AppendFormat(" DedicatedVideoMemoryInkB: {0}{1}", memoryInformation.DedicatedVideoMemoryInkB, Environment.NewLine);
                r.AppendFormat(" CurrentAvailableDedicatedVideoMemoryInkB: {0}{1}", memoryInformation.CurrentAvailableDedicatedVideoMemoryInkB, Environment.NewLine);
                r.AppendFormat(" AvailableDedicatedVideoMemoryInkB: {0}{1}", memoryInformation.AvailableDedicatedVideoMemoryInkB, Environment.NewLine);
                r.AppendLine();
            }
            catch (Exception e) when (e is NVIDIAApiException or NVIDIANotSupportedException)
            { }

            return r.ToString();
        }

        private void ControlModeChanged(IControl control)
        {
            switch (control.ControlMode)
            {
                case ControlMode.Default:
                {
                    RestoreDefaultFanBehavior(control.Sensor.Index);
                    break;
                }
                case ControlMode.Software:
                {
                    SoftwareControlValueChanged(control);
                    break;
                }
                default:
                {
                    return;
                }
            }
        }

        private void SoftwareControlValueChanged(IControl control)
        {
            int coolerId = -1;
            int index = control.Sensor?.Index ?? 0;

            try
            {
                GPUCooler[] coolers = _physicalGpu.CoolerInformation.Coolers.ToArray();
                if (coolers.Length > index)
                {
                    GPUCooler cooler = coolers[index];
                    coolerId = cooler.CoolerId;
                }

                if (coolerId != -1)
                    _physicalGpu.CoolerInformation.SetCoolerSettings(coolerId, CoolerPolicy.Manual, (int)control.SoftwareValue);
            }
            catch (Exception e) when (e is NVIDIAApiException or NVIDIANotSupportedException)
            { }
        }

        private void RestoreDefaultFanBehavior(int index)
        {
            int coolerId = -1;

            try
            {
                GPUCooler[] coolers = _physicalGpu.CoolerInformation.Coolers.ToArray();
                if (coolers.Length > index)
                {
                    GPUCooler cooler = coolers[index];
                    coolerId = cooler.CoolerId;
                }

                if (coolerId != -1)
                    _physicalGpu.CoolerInformation.RestoreCoolerSettingsToDefault(coolerId);
            }
            catch (Exception e) when (e is NVIDIAApiException or NVIDIANotSupportedException)
            { }
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
}
