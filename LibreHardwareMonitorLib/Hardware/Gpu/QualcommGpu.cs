// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace LibreHardwareMonitor.Hardware.Gpu;

/// <summary>
/// Qualcomm Adreno GPU monitoring via the D3D/DXGI display adapter interface.
/// Uses the same D3DKMT API surface as <see cref="IntelIntegratedGpu"/> to
/// read GPU engine utilization and memory usage, since no Qualcomm-specific
/// GPU monitoring SDK is publicly available.
/// </summary>
internal sealed class QualcommGpu : GenericGpu
{
    private readonly string _deviceId;

    // Memory sensors
    private readonly Sensor _dedicatedMemoryUsage;
    private readonly Sensor _sharedMemoryUsage;
    private readonly Sensor _sharedMemoryLimit;
    private readonly Sensor _sharedMemoryFree;

    // Per-engine GPU node utilization
    private readonly Sensor[] _nodeUsage;
    private readonly long[] _nodeUsagePrevValue;
    private readonly DateTime[] _nodeUsagePrevTick;

    public QualcommGpu(string deviceId, D3DDisplayDevice.D3DDeviceInfo deviceInfo, ISettings settings)
        : base(GetName(deviceId), new Identifier("gpu-qualcomm", deviceId.GetHashCode().ToString("X", CultureInfo.InvariantCulture)), settings)
    {
        _deviceId = deviceId;

        // Memory sensors
        if (deviceInfo.GpuSharedLimit > 0)
        {
            _sharedMemoryLimit = new Sensor("GPU Shared Memory Total", 0, SensorType.SmallData, this, settings);
            _sharedMemoryUsage = new Sensor("GPU Shared Memory Used", 1, SensorType.SmallData, this, settings);
            _sharedMemoryFree = new Sensor("GPU Shared Memory Free", 2, SensorType.SmallData, this, settings);
            ActivateSensor(_sharedMemoryLimit);
            ActivateSensor(_sharedMemoryUsage);
            ActivateSensor(_sharedMemoryFree);
        }

        if (deviceInfo.GpuDedicatedLimit > 0)
        {
            _dedicatedMemoryUsage = new Sensor("GPU Dedicated Memory Used", 3, SensorType.SmallData, this, settings);
            ActivateSensor(_dedicatedMemoryUsage);
        }

        // Engine utilization sensors (3D, Compute, Video Decode, etc.)
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

    public override HardwareType HardwareType => HardwareType.GpuQualcomm;

    public override void Update()
    {
        if (!D3DDisplayDevice.GetDeviceInfoByIdentifier(_deviceId, out D3DDisplayDevice.D3DDeviceInfo deviceInfo))
            return;

        // Update memory sensors
        if (_dedicatedMemoryUsage != null)
        {
            _dedicatedMemoryUsage.Value = 1f * deviceInfo.GpuDedicatedUsed / 1024 / 1024;
            ActivateSensor(_dedicatedMemoryUsage);
        }

        if (_sharedMemoryLimit != null)
        {
            _sharedMemoryLimit.Value = 1f * deviceInfo.GpuSharedLimit / 1024 / 1024;
            ActivateSensor(_sharedMemoryLimit);

            if (_sharedMemoryUsage != null && _sharedMemoryFree != null)
            {
                _sharedMemoryUsage.Value = 1f * deviceInfo.GpuSharedUsed / 1024 / 1024;
                _sharedMemoryFree.Value = _sharedMemoryLimit.Value - _sharedMemoryUsage.Value;
                ActivateSensor(_sharedMemoryUsage);
                ActivateSensor(_sharedMemoryFree);
            }
        }

        // Update per-engine utilization
        if (_nodeUsage.Length == deviceInfo.Nodes.Length)
        {
            foreach (D3DDisplayDevice.D3DDeviceNodeInfo node in deviceInfo.Nodes)
            {
                if (_nodeUsage[node.Id] == null)
                    continue;

                long runningTimeDiff = node.RunningTime - _nodeUsagePrevValue[node.Id];
                long timeDiff = node.QueryTime.Ticks - _nodeUsagePrevTick[node.Id].Ticks;

                if (timeDiff > 0)
                {
                    _nodeUsage[node.Id].Value = 100f * runningTimeDiff / timeDiff;
                }

                _nodeUsagePrevValue[node.Id] = node.RunningTime;
                _nodeUsagePrevTick[node.Id] = node.QueryTime;
                ActivateSensor(_nodeUsage[node.Id]);
            }
        }
    }

    public override string GetReport()
    {
        StringBuilder r = new();

        r.AppendLine("Qualcomm Adreno GPU");
        r.AppendLine();
        r.AppendFormat("Name: {0}{1}", Name, Environment.NewLine);
        r.AppendFormat("DeviceId: {0}{1}", DeviceId, Environment.NewLine);
        r.AppendLine();

        return r.ToString();
    }

    private static string GetName(string deviceId)
    {
        string path = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\" + D3DDisplayDevice.GetActualDeviceIdentifier(deviceId);

        if (Registry.GetValue(path, "DeviceDesc", null) is string deviceDesc)
        {
            return deviceDesc.Split(';').Last();
        }

        return "Qualcomm Adreno GPU";
    }
}
