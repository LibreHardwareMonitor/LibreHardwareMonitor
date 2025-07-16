// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Interop;

namespace LibreHardwareMonitor.Hardware.Gpu
{
    internal sealed class IntelDiscreteGpu : GenericGpu
    {
        // Constants
        private const double MEMORY_FREQUENCY_DIVISOR = 8.0; // Intel GCL returns memory frequency multiplied by 8

        // Intel GCL properties and data
        private IntelGcl.ctl_device_adapter_handle_t _handle;
        private IntelGcl.ctl_device_adapter_properties_t _properties;
        public uint VendorId { get; private set; }
        public uint RevisionId { get; private set; }
        public uint DriverVersion { get; private set; }
        public bool IsValid { get; private set; }
        private string _deviceId;

        // Telemetry data
        private IntelGcl.ctl_power_telemetry_t _telemetry;

        // Timestamps
        private double _currentTimestamp = double.NaN;
        private double _lastTimestamp = double.NaN;

        // Power calculation support
        private double _lastEnergyReading = double.NaN;
        private double _lastTotalCardEnergyReading = double.NaN;

        // Activity counter calculation support
        private double _lastGlobalActivityCounter = double.NaN;
        private double _lastRenderComputeActivityCounter = double.NaN;
        private double _lastMediaActivityCounter = double.NaN;

        // Temperature sensors
        private readonly Sensor _temperatureGpuCore;
        private readonly Sensor _temperatureMemory;

        // Clock sensors
        private readonly Sensor _clockCore;
        private readonly Sensor _clockMemory;

        // Voltage sensors
        private readonly Sensor _voltageCore;
        private readonly Sensor _voltageMemory;

        // Power sensors
        private readonly Sensor _powerGpu;
        private readonly Sensor _powerTotal;

        // Utilization sensors
        private readonly Sensor _loadGlobalActivity;
        private readonly Sensor _loadRenderCompute;
        private readonly Sensor _loadMedia;

        // Fan sensors
        private readonly Sensor[] _fans;

        public IntelDiscreteGpu(IntelGcl.ctl_device_adapter_handle_t handle, ISettings settings)
            : base(GetDeviceName(handle), new Identifier("gpu-intel", GetDeviceId(handle)), settings)
        {
            _handle = handle;
            IsValid = false;

            // Initialize device properties
            if (!InitializeDevice())
                return;

            // Initialize temperature sensors
            _temperatureGpuCore = new Sensor("GPU Core", 0, SensorType.Temperature, this, settings);
            _temperatureMemory = new Sensor("GPU Memory", 1, SensorType.Temperature, this, settings);

            // Initialize clock sensors
            _clockCore = new Sensor("GPU Core", 0, SensorType.Clock, this, settings);
            _clockMemory = new Sensor("GPU Memory", 1, SensorType.Clock, this, settings);

            // Initialize voltage sensors
            _voltageCore = new Sensor("GPU Core", 0, SensorType.Voltage, this, settings);
            _voltageMemory = new Sensor("GPU Memory", 1, SensorType.Voltage, this, settings);

            // Initialize power sensors
            _powerGpu = new Sensor("GPU Package", 0, SensorType.Power, this, settings);
            _powerTotal = new Sensor("GPU Total", 1, SensorType.Power, this, settings);

            // Initialize utilization sensors
            _loadGlobalActivity = new Sensor("GPU Core", 0, SensorType.Load, this, settings);
            _loadRenderCompute = new Sensor("GPU Render/Compute", 1, SensorType.Load, this, settings);
            _loadMedia = new Sensor("GPU Media", 2, SensorType.Load, this, settings);

            // Initialize fan sensors based on available fans
            int fanCount = (int) GetFanCount();
            _fans = new Sensor[fanCount];
            for (int i = 0; i < fanCount; i++)
            {
                string fanName = fanCount == 1 ? "GPU Fan" : $"GPU Fan {i + 1}";
                _fans[i] = new Sensor(fanName, i, SensorType.Fan, this, settings);
            }

            Update();
        }

        private static bool TryGetDeviceProperties(IntelGcl.ctl_device_adapter_handle_t handle, out IntelGcl.ctl_device_adapter_properties_t properties)
        {
            properties = new IntelGcl.ctl_device_adapter_properties_t();
            properties.Size = (uint)Marshal.SizeOf(typeof(IntelGcl.ctl_device_adapter_properties_t));
            properties.Version = 2;

            int result = IntelGcl.ctlGetDeviceProperties(handle, ref properties);
            return result == (int)IntelGcl.ctl_result_t.CTL_RESULT_SUCCESS &&
                   properties.device_type == IntelGcl.ctl_device_type_t.CTL_DEVICE_TYPE_GRAPHICS;
        }

        private static string GetDeviceName(IntelGcl.ctl_device_adapter_handle_t handle)
        {
            if (TryGetDeviceProperties(handle, out var properties))
            {
                return properties.name;
            }
            return "Intel GPU";
        }

        private static string GetDeviceId(IntelGcl.ctl_device_adapter_handle_t handle)
        {
            if (TryGetDeviceProperties(handle, out var properties))
            {
                return $"0x{properties.pci_device_id:X4}";
            }
            return "0x0000";
        }

        // Device initialization
        private bool InitializeDevice()
        {
            if (TryGetDeviceProperties(_handle, out _properties))
            {
                _deviceId = $"0x{_properties.pci_device_id:X4}";
                VendorId = _properties.pci_vendor_id;
                RevisionId = _properties.rev_id;
                DriverVersion = (uint)_properties.driver_version;
                IsValid = true;
                return true;
            }
            return false;
        }

        public override string DeviceId => _deviceId ?? GetDeviceId(_handle);

        public override HardwareType HardwareType => HardwareType.GpuIntel;

        public override void Update()
        {
            if (!IsValid)
                return;

            try
            {
                // Update telemetry data from Intel GCL
                if (!UpdateTelemetry())
                    return;

                // Update power sensors
                UpdatePowerFromEnergyCounter(_telemetry.gpuEnergyCounter, ref _lastEnergyReading, _powerGpu);
                UpdatePowerFromEnergyCounter(_telemetry.totalCardEnergyCounter, ref _lastTotalCardEnergyReading, _powerTotal);

                // Update temperature sensors
                UpdateSensorFromTelemetry(_telemetry.gpuCurrentTemperature, _temperatureGpuCore);
                UpdateSensorFromTelemetry(_telemetry.vramCurrentTemperature, _temperatureMemory);

                // Update clock sensors
                UpdateSensorFromTelemetry(_telemetry.gpuCurrentClockFrequency, _clockCore);
                UpdateMemoryFrequency(_clockMemory);

                // Update voltage sensors
                UpdateSensorFromTelemetry(_telemetry.gpuVoltage, _voltageCore);
                UpdateSensorFromTelemetry(_telemetry.vramVoltage, _voltageMemory);

                // Update utilization sensors
                UpdateUtilizationFromActivityCounter(_telemetry.globalActivityCounter, ref _lastGlobalActivityCounter, _loadGlobalActivity);
                UpdateUtilizationFromActivityCounter(_telemetry.renderComputeActivityCounter, ref _lastRenderComputeActivityCounter, _loadRenderCompute);
                UpdateUtilizationFromActivityCounter(_telemetry.mediaActivityCounter, ref _lastMediaActivityCounter, _loadMedia);

                // Update fan sensors
                UpdateFanSpeeds(_fans);
            }
            catch (Exception ex)
            {
                // Log error but don't crash the update
                System.Diagnostics.Debug.WriteLine($"Error updating Intel GPU sensors: {ex.Message}");
            }
        }

        public override void Close()
        {
            // Clean up any resources if needed
            base.Close();
        }

        private bool UpdateTelemetry()
        {
            if (!IsValid)
                return false;

            var telemetry = new IntelGcl.ctl_power_telemetry_t();
            telemetry.Size = (uint)Marshal.SizeOf(typeof(IntelGcl.ctl_power_telemetry_t));
            telemetry.Version = 1;

            telemetry.psu = new IntelGcl.ctl_psu_info_t[IntelGcl.CTL_PSU_COUNT];
            telemetry.fanSpeed = new IntelGcl.ctl_oc_telemetry_item_t[IntelGcl.CTL_FAN_COUNT];

            if (IntelGcl.ctlPowerTelemetryGet(_handle, ref telemetry) == (int)IntelGcl.ctl_result_t.CTL_RESULT_SUCCESS)
            {
                _telemetry = telemetry;
                _lastTimestamp = _currentTimestamp;
                _currentTimestamp = _telemetry.timeStamp.bSupported ? GetTelemetryValue(_telemetry.timeStamp) : DateTimeOffset.UtcNow.Ticks;
                return true;
            }

            return false;
        }

        private void UpdateMemoryFrequency(Sensor sensor)
        {
            double frequency = double.NaN;

            uint freqCount = 0;
            int result = IntelGcl.ctlEnumFrequencyDomains(_handle, ref freqCount, null);

            if (result == (int)IntelGcl.ctl_result_t.CTL_RESULT_SUCCESS && freqCount > 0)
            {
                var freqHandles = new IntelGcl.ctl_freq_handle_t[freqCount];
                result = IntelGcl.ctlEnumFrequencyDomains(_handle, ref freqCount, freqHandles);

                if (result == (int)IntelGcl.ctl_result_t.CTL_RESULT_SUCCESS)
                {
                    for (int i = 0; i < freqCount; i++)
                    {
                        var properties = new IntelGcl.ctl_freq_properties_t();
                        properties.Size = (uint)Marshal.SizeOf(typeof(IntelGcl.ctl_freq_properties_t));
                        properties.Version = 0;

                        result = IntelGcl.ctlFrequencyGetProperties(freqHandles[i], ref properties);

                        if (result == (int)IntelGcl.ctl_result_t.CTL_RESULT_SUCCESS &&
                            properties.type == IntelGcl.ctl_freq_domain_t.CTL_FREQ_DOMAIN_MEMORY)
                        {
                            var state = new IntelGcl.ctl_freq_state_t();
                            state.Size = (uint)Marshal.SizeOf(typeof(IntelGcl.ctl_freq_state_t));
                            state.Version = 0;

                            result = IntelGcl.ctlFrequencyGetState(freqHandles[i], ref state);

                            if (result == (int)IntelGcl.ctl_result_t.CTL_RESULT_SUCCESS && state.actual >= 0)
                            {
                                frequency = state.actual / MEMORY_FREQUENCY_DIVISOR;
                                break;
                            }
                        }
                    }
                }
            }

            if (double.IsNaN(frequency) && _telemetry.vramCurrentClockFrequency.bSupported)
            {
                frequency = GetTelemetryValue(_telemetry.vramCurrentClockFrequency);
            }

            if (!double.IsNaN(frequency))
            {
                sensor.Value = (float)frequency;
                ActivateSensor(sensor);
            }
            else
            {
                sensor.Value = null;
            }
        }

        private uint GetFanCount()
        {
            uint fanCount = 0;
            int result = IntelGcl.ctlEnumFans(_handle, ref fanCount, null);

            if (result == (int)IntelGcl.ctl_result_t.CTL_RESULT_SUCCESS)
            {
                return fanCount;
            }
            return 0;
        }

        private void UpdateFanSpeeds(Sensor[] fanSensors)
        {
            uint fanCount = (uint) Math.Min(Math.Max(0, GetFanCount()), fanSensors.Length);
            if (fanCount == 0)
                return;

            var fanHandles = new IntelGcl.ctl_fan_handle_t[fanCount];
            int result = IntelGcl.ctlEnumFans(_handle, ref fanCount, fanHandles);

            if (result == (int)IntelGcl.ctl_result_t.CTL_RESULT_SUCCESS)
            {
                for (int i = 0; i < fanCount; i++)
                {
                    int fanSpeed = -1;
                    result = IntelGcl.ctlFanGetState(fanHandles[i], IntelGcl.ctl_fan_speed_units_t.CTL_FAN_SPEED_UNITS_RPM, ref fanSpeed);

                    if (result == (int)IntelGcl.ctl_result_t.CTL_RESULT_SUCCESS && fanSpeed >= 0)
                    {
                        fanSensors[i].Value = fanSpeed;
                        ActivateSensor(fanSensors[i]);
                    }
                    else
                    {
                        fanSensors[i].Value = null;
                    }
                }
                for (int i = (int) fanCount; i < fanSensors.Length; i++)
                {
                    fanSensors[i].Value = null;
                }
            }
        }

        private void UpdateSensorFromTelemetry(IntelGcl.ctl_oc_telemetry_item_t telemetryItem, Sensor sensor)
        {
            if (telemetryItem.bSupported)
            {
                sensor.Value = (float)GetTelemetryValue(telemetryItem);
                ActivateSensor(sensor);
            }
            else
            {
                sensor.Value = null;
            }
        }

        private double GetTelemetryValue(IntelGcl.ctl_oc_telemetry_item_t item)
        {
            switch (item.type)
            {
                case IntelGcl.ctl_data_type_t.CTL_DATA_TYPE_FLOAT:
                    return item.value.datafloat;
                case IntelGcl.ctl_data_type_t.CTL_DATA_TYPE_DOUBLE:
                    return item.value.datadouble;
                case IntelGcl.ctl_data_type_t.CTL_DATA_TYPE_UINT32:
                    return item.value.datau32;
                case IntelGcl.ctl_data_type_t.CTL_DATA_TYPE_INT32:
                    return item.value.data32;
                case IntelGcl.ctl_data_type_t.CTL_DATA_TYPE_UINT64:
                    return item.value.datau64;
                case IntelGcl.ctl_data_type_t.CTL_DATA_TYPE_INT64:
                    return item.value.data64;
                case IntelGcl.ctl_data_type_t.CTL_DATA_TYPE_UINT16:
                    return item.value.datau16;
                case IntelGcl.ctl_data_type_t.CTL_DATA_TYPE_INT16:
                    return item.value.data16;
                case IntelGcl.ctl_data_type_t.CTL_DATA_TYPE_UINT8:
                    return item.value.datau8;
                case IntelGcl.ctl_data_type_t.CTL_DATA_TYPE_INT8:
                    return item.value.data8;
                default:
                    return double.NaN;
            }
        }

        private void UpdatePowerFromEnergyCounter(IntelGcl.ctl_oc_telemetry_item_t energyCounter, ref double lastEnergyReading, Sensor powerSensor)
        {
            if (!IsValid || powerSensor == null)
                return;

            double currentEnergy = energyCounter.bSupported ? GetTelemetryValue(energyCounter) : double.NaN;
            double deltaTime = _currentTimestamp - _lastTimestamp;

            if (deltaTime > 0.0 && !double.IsNaN(currentEnergy) && !double.IsNaN(lastEnergyReading))
            {
                double deltaEnergy = currentEnergy - lastEnergyReading;
                double power = deltaEnergy / deltaTime;
                power = power < 0 ? 0 : power;

                powerSensor.Value = (float)power;
                ActivateSensor(powerSensor);
            }
            else
            {
                powerSensor.Value = null;
            }

            lastEnergyReading = currentEnergy;
        }

        private void UpdateUtilizationFromActivityCounter(IntelGcl.ctl_oc_telemetry_item_t activityCounter, ref double lastActivityReading, Sensor activitySensor)
        {
            if (!IsValid || activitySensor == null)
                return;

            double currentActivity = activityCounter.bSupported ? GetTelemetryValue(activityCounter) : double.NaN;
            double deltaTime = _currentTimestamp - _lastTimestamp;

            if (deltaTime > 0 && !double.IsNaN(currentActivity) && !double.IsNaN(lastActivityReading))
            {
                double activeDiff = currentActivity - lastActivityReading;
                if (activeDiff >= 0)
                {
                    double activity = (activeDiff / deltaTime) * 100.0;
                    activity = Math.Min(Math.Max(activity, 0.0), 100.0);

                    activitySensor.Value = (float)activity;
                    ActivateSensor(activitySensor);
                }
                else
                {
                    activitySensor.Value = null;
                }
            }
            else
            {
                activitySensor.Value = null;
            }

            lastActivityReading = currentActivity;
        }
    }
}