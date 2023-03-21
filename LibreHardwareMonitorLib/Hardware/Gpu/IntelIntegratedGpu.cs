using System;
using System.Globalization;
using System.Linq;
using Microsoft.Win32;

namespace LibreHardwareMonitor.Hardware.Gpu;

internal class IntelIntegratedGpu : GenericGpu
{
    private const uint MSR_PP1_ENERGY_STATUS = 0x641;
    private const uint MSR_RAPL_POWER_UNIT = 0x606;

    private readonly Sensor _dedicatedMemoryUsage;
    private readonly string _deviceId;
    private readonly float _energyUnitMultiplier;
    private readonly Sensor[] _nodeUsage;
    private readonly DateTime[] _nodeUsagePrevTick;
    private readonly long[] _nodeUsagePrevValue;
    private readonly Sensor _powerSensor;
    private readonly Sensor _sharedMemoryUsage;

    private uint _lastEnergyConsumed;
    private DateTime _lastEnergyTime;

    public IntelIntegratedGpu(Cpu.IntelCpu intelCpu, string deviceId, D3DDisplayDevice.D3DDeviceInfo deviceInfo, ISettings settings)
        : base(GetName(deviceId),
               new Identifier("gpu-intel-integrated", deviceId.ToString(CultureInfo.InvariantCulture)),
               settings)
    {
        _deviceId = deviceId;

        int memorySensorIndex = 0;

        if (deviceInfo.GpuDedicatedLimit > 0)
        {
            _dedicatedMemoryUsage = new Sensor("D3D Dedicated Memory Used", memorySensorIndex++, SensorType.SmallData, this, settings);
        }

        _sharedMemoryUsage = new Sensor("D3D Shared Memory Used", memorySensorIndex++, SensorType.SmallData, this, settings);

        if (Ring0.ReadMsr(MSR_RAPL_POWER_UNIT, out uint eax, out uint _))
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
        if (D3DDisplayDevice.GetDeviceInfoByIdentifier(_deviceId, out D3DDisplayDevice.D3DDeviceInfo deviceInfo))
        {
            if (_dedicatedMemoryUsage != null)
            {
                _dedicatedMemoryUsage.Value = 1f * deviceInfo.GpuDedicatedUsed / 1024 / 1024;
                ActivateSensor(_dedicatedMemoryUsage);
            }

            _sharedMemoryUsage.Value = 1f * deviceInfo.GpuSharedUsed / 1024 / 1024;
            ActivateSensor(_sharedMemoryUsage);

            if (_powerSensor != null && Ring0.ReadMsr(MSR_PP1_ENERGY_STATUS, out uint eax, out uint _))
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
