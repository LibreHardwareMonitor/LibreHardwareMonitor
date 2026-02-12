using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Interop;
using LibreHardwareMonitor.PawnIo;
using Microsoft.Win32;

namespace LibreHardwareMonitor.Hardware.Gpu;

internal class IntelIntegratedGpu : GenericGpu
{
    private const uint MSR_PP1_ENERGY_STATUS = 0x641;

    private readonly Sensor _dedicatedMemoryUsage;
    private readonly Sensor _sharedMemoryLimit;
    private readonly Sensor _sharedMemoryFree;
    private readonly string _deviceId;
    private readonly float _energyUnitMultiplier;
    private readonly Sensor[] _nodeUsage;
    private readonly DateTime[] _nodeUsagePrevTick;
    private readonly long[] _nodeUsagePrevValue;
    private readonly Sensor _powerSensor;
    private readonly Sensor _sharedMemoryUsage;
    private readonly Sensor _gtCoresTemperature;
    private readonly Sensor _gpuClockFrequency;
    private readonly Sensor _gpuVoltage;

    private uint _lastEnergyConsumed;
    private DateTime _lastEnergyTime;

    private readonly IntelMsr _pawnModule;

    private readonly IntelGcl.ctl_device_adapter_handle_t? _igclHandle;

    public IntelIntegratedGpu(Cpu.IntelCpu intelCpu, string deviceId, D3DDisplayDevice.D3DDeviceInfo deviceInfo, ISettings settings)
        : base(GetName(deviceId),
               new Identifier("gpu-intel-integrated", deviceId.ToString(CultureInfo.InvariantCulture)),
               settings)
    {
        _pawnModule = new IntelMsr();
        _deviceId = deviceId;

        _igclHandle = FindIgclHandle();

        if (_igclHandle.HasValue && TryReadTelemetry(_igclHandle.Value, out IntelGcl.ctl_power_telemetry_t probeTelemetry))
        {
            if (probeTelemetry.gpuCurrentTemperature.bSupported)
            {
                _gtCoresTemperature = new Sensor("GPU Core", 0, SensorType.Temperature, this, settings);
                ActivateSensor(_gtCoresTemperature);
            }

            if (probeTelemetry.gpuCurrentClockFrequency.bSupported)
            {
                _gpuClockFrequency = new Sensor("GPU Core", 0, SensorType.Clock, this, settings);
                ActivateSensor(_gpuClockFrequency);
            }

            if (probeTelemetry.gpuVoltage.bSupported)
            {
                _gpuVoltage = new Sensor("GPU Core", 0, SensorType.Voltage, this, settings);
                ActivateSensor(_gpuVoltage);
            }
        }

        int memorySensorIndex = 0;

        if (deviceInfo.GpuDedicatedLimit > 0)
        {
            _dedicatedMemoryUsage = new Sensor("D3D Dedicated Memory Used", memorySensorIndex++, SensorType.SmallData, this, settings);
        }

        _sharedMemoryUsage = new Sensor("D3D Shared Memory Used", memorySensorIndex++, SensorType.SmallData, this, settings);

        if (deviceInfo.GpuSharedLimit > 0)
        {
            _sharedMemoryFree = new Sensor("D3D Shared Memory Free", memorySensorIndex++, SensorType.SmallData, this, settings);
            _sharedMemoryLimit = new Sensor("D3D Shared Memory Total", memorySensorIndex++, SensorType.SmallData, this, settings);
        }

        if (_pawnModule.ReadMsr(MSR_PP1_ENERGY_STATUS, out uint eax, out uint _))
        {
            _energyUnitMultiplier = intelCpu.EnergyUnitsMultiplier;
            if (_energyUnitMultiplier != 0)
            {
                _lastEnergyTime = DateTime.UtcNow;
                _lastEnergyConsumed = eax;
                _powerSensor = new Sensor("GPU Power", 0, SensorType.Power, this, settings);
                ActivateSensor(_powerSensor);
            }
        }

        _nodeUsage = new Sensor[deviceInfo.Nodes.Length];
        _nodeUsagePrevValue = new long[deviceInfo.Nodes.Length];
        _nodeUsagePrevTick = new DateTime[deviceInfo.Nodes.Length];

        int nodeSensorIndex = 0;
        foreach (D3DDisplayDevice.D3DDeviceNodeInfo node in deviceInfo.Nodes.OrderBy(x => x.Name))
        {
            _nodeUsage[node.Id] = new Sensor(node.Name, nodeSensorIndex++, SensorType.Load, this, settings);
            _nodeUsagePrevValue[node.Id] = node.RunningTime;
            _nodeUsagePrevTick[node.Id] = node.QueryTime;
        }
    }

    /// <inheritdoc />
    public override string DeviceId => D3DDisplayDevice.GetActualDeviceIdentifier(_deviceId);

    public override HardwareType HardwareType => HardwareType.GpuIntel;

    public override void Update()
    {
        // Update IGCL telemetry (temperature, clock, voltage).
        if (_igclHandle.HasValue && (_gtCoresTemperature != null || _gpuClockFrequency != null || _gpuVoltage != null))
        {
            if (TryReadTelemetry(_igclHandle.Value, out IntelGcl.ctl_power_telemetry_t telemetry))
            {
                UpdateSensorFromTelemetry(telemetry.gpuCurrentTemperature, _gtCoresTemperature);
                UpdateSensorFromTelemetry(telemetry.gpuCurrentClockFrequency, _gpuClockFrequency);
                UpdateSensorFromTelemetry(telemetry.gpuVoltage, _gpuVoltage);
            }
            else
            {
                if (_gtCoresTemperature != null) _gtCoresTemperature.Value = null;
                if (_gpuClockFrequency != null) _gpuClockFrequency.Value = null;
                if (_gpuVoltage != null) _gpuVoltage.Value = null;
            }
        }

        if (D3DDisplayDevice.GetDeviceInfoByIdentifier(_deviceId, out D3DDisplayDevice.D3DDeviceInfo deviceInfo))
        {
            if (_dedicatedMemoryUsage != null)
            {
                _dedicatedMemoryUsage.Value = 1f * deviceInfo.GpuDedicatedUsed / 1024 / 1024;
                ActivateSensor(_dedicatedMemoryUsage);
            }

            if (_sharedMemoryLimit != null)
            {
                _sharedMemoryLimit.Value = 1f * deviceInfo.GpuSharedLimit / 1024 / 1024;
                ActivateSensor(_sharedMemoryLimit);
                if (_sharedMemoryUsage != null)
                {
                    _sharedMemoryFree.Value = _sharedMemoryLimit.Value - _sharedMemoryUsage.Value;
                    ActivateSensor(_sharedMemoryFree);
                }
            }

            _sharedMemoryUsage.Value = 1f * deviceInfo.GpuSharedUsed / 1024 / 1024;
            ActivateSensor(_sharedMemoryUsage);

            if (_powerSensor != null && _pawnModule.ReadMsr(MSR_PP1_ENERGY_STATUS, out uint eax, out uint _))
            {
                DateTime time = DateTime.UtcNow;
                float deltaTime = (float)(time - _lastEnergyTime).TotalSeconds;
                if (deltaTime >= 0.01)
                {
                    _powerSensor.Value = _energyUnitMultiplier * unchecked(eax - _lastEnergyConsumed) / deltaTime;
                    _lastEnergyTime = time;
                    _lastEnergyConsumed = eax;
                }
            }

            if (_nodeUsage.Length == deviceInfo.Nodes.Length)
            {
                foreach (D3DDisplayDevice.D3DDeviceNodeInfo node in deviceInfo.Nodes)
                {
                    long runningTimeDiff = node.RunningTime - _nodeUsagePrevValue[node.Id];
                    long timeDiff = node.QueryTime.Ticks - _nodeUsagePrevTick[node.Id].Ticks;

                    _nodeUsage[node.Id].Value = 100f * runningTimeDiff / timeDiff;
                    _nodeUsagePrevValue[node.Id] = node.RunningTime;
                    _nodeUsagePrevTick[node.Id] = node.QueryTime;
                    ActivateSensor(_nodeUsage[node.Id]);
                }
            }
        }
    }

    /// <inheritdoc />
    public override void Close()
    {
        base.Close();
        _pawnModule.Close();
    }

    private static bool TryReadTelemetry(IntelGcl.ctl_device_adapter_handle_t handle, out IntelGcl.ctl_power_telemetry_t telemetry)
    {
        telemetry = new IntelGcl.ctl_power_telemetry_t
        {
            Size = (uint)Marshal.SizeOf<IntelGcl.ctl_power_telemetry_t>(),
            Version = 1,
            psu = new IntelGcl.ctl_psu_info_t[IntelGcl.CTL_PSU_COUNT],
            fanSpeed = new IntelGcl.ctl_oc_telemetry_item_t[IntelGcl.CTL_FAN_COUNT]
        };

        int result = IntelGcl.ctlPowerTelemetryGet(handle, ref telemetry);
        return result == (int)IntelGcl.ctl_result_t.CTL_RESULT_SUCCESS;
    }

    private static void UpdateSensorFromTelemetry(IntelGcl.ctl_oc_telemetry_item_t telemetryItem, Sensor sensor)
    {
        if (sensor == null)
            return;

        if (telemetryItem.bSupported)
        {
            double value = GetTelemetryValue(telemetryItem);
            sensor.Value = double.IsNaN(value) ? null : (float)value;
        }
        else
        {
            sensor.Value = null;
        }
    }

    private static double GetTelemetryValue(IntelGcl.ctl_oc_telemetry_item_t item)
    {
        return item.type switch
        {
            IntelGcl.ctl_data_type_t.CTL_DATA_TYPE_FLOAT => item.value.datafloat,
            IntelGcl.ctl_data_type_t.CTL_DATA_TYPE_DOUBLE => item.value.datadouble,
            IntelGcl.ctl_data_type_t.CTL_DATA_TYPE_UINT32 => item.value.datau32,
            IntelGcl.ctl_data_type_t.CTL_DATA_TYPE_INT32 => item.value.data32,
            IntelGcl.ctl_data_type_t.CTL_DATA_TYPE_UINT64 => item.value.datau64,
            IntelGcl.ctl_data_type_t.CTL_DATA_TYPE_INT64 => item.value.data64,
            IntelGcl.ctl_data_type_t.CTL_DATA_TYPE_UINT16 => item.value.datau16,
            IntelGcl.ctl_data_type_t.CTL_DATA_TYPE_INT16 => item.value.data16,
            IntelGcl.ctl_data_type_t.CTL_DATA_TYPE_UINT8 => item.value.datau8,
            IntelGcl.ctl_data_type_t.CTL_DATA_TYPE_INT8 => item.value.data8,
            _ => double.NaN
        };
    }

    private static IntelGcl.ctl_device_adapter_handle_t? FindIgclHandle()
    {
        if (!IntelGcl.IsInitialized)
            return null;

        IntelGcl.ctl_device_adapter_handle_t[] handles = IntelGcl.GetDeviceHandles();

        foreach (IntelGcl.ctl_device_adapter_handle_t handle in handles)
        {
            var properties = new IntelGcl.ctl_device_adapter_properties_t
            {
                Size = (uint)Marshal.SizeOf<IntelGcl.ctl_device_adapter_properties_t>(),
                Version = 2
            };

            int result = IntelGcl.ctlGetDeviceProperties(handle, ref properties);

            if (result != (int)IntelGcl.ctl_result_t.CTL_RESULT_SUCCESS)
                continue;

            if (properties.device_type != IntelGcl.ctl_device_type_t.CTL_DEVICE_TYPE_GRAPHICS)
                continue;

            // Use the IGCL integrated-adapter flag — the same method IntelGpuGroup uses
            // to filter iGPUs out of the discrete GPU enumeration path.
            if ((properties.graphics_adapter_properties & (uint)IntelGcl.ctl_adapter_properties_flag_t.CTL_ADAPTER_PROPERTIES_FLAG_INTEGRATED) != 0)
            {
                return handle;
            }
        }

        return null;
    }

    private static string GetName(string deviceId)
    {
        string path = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\" + D3DDisplayDevice.GetActualDeviceIdentifier(deviceId);

        if (Registry.GetValue(path, "DeviceDesc", null) is string deviceDesc)
        {
            return deviceDesc.Split(';').Last();
        }

        return "Intel Integrated Graphics";
    }
}
