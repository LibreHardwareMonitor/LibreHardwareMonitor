// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Windows.Win32.Foundation;
using Windows.Win32.System.Performance;

namespace LibreHardwareMonitor.Hardware.Npu;

internal sealed class NpuHardware : Hardware
{
    private const string UtilizationObjectName = "NPU Engine Utilization";
    private const string MemoryObjectName = "NPU Adapter Memory";
    private const string UtilizationCounterName = "Utilization Percentage";
    private const string DedicatedMemoryUsedName = "Dedicated Memory Used";
    private const string SharedMemoryUsedName = "Shared Memory Used";
    private const string SharedMemoryLimitName = "Shared Memory Budget";

    private readonly string _adapterName;
    private readonly LUID _adapterLuid;
    private readonly bool _pdhBacked;

    private readonly Sensor _dedicatedMemoryUsage;
    private readonly Sensor _sharedMemoryUsage;
    private readonly Sensor _sharedMemoryBudget;
    private readonly Sensor[] _nodeUsage;
    private readonly long[] _nodeUsagePrevValue;
    private readonly DateTime[] _nodeUsagePrevTick;

    private readonly Sensor _pdhLoad;
    private readonly Sensor _pdhDedicatedMemUsed;
    private readonly Sensor _pdhSharedMemUsed;
    private readonly Sensor _pdhSharedMemLimit;

    private IntPtr _queryHandle = IntPtr.Zero;
    private IntPtr _utilizationCounter = IntPtr.Zero;
    private IntPtr _dedicatedMemCounter = IntPtr.Zero;
    private IntPtr _sharedMemUsedCounter = IntPtr.Zero;
    private IntPtr _sharedMemLimitCounter = IntPtr.Zero;

    private bool _queryOpen;
    private bool _firstSample = true;

    public NpuHardware(LUID adapterLuid, string description, D3DDisplayDevice.D3DDeviceInfo deviceInfo, ISettings settings)
        : base(description,
               new Identifier("npu", D3DDisplayDevice.GetLuidKey(adapterLuid).ToString("X16", CultureInfo.InvariantCulture)),
               settings)
    {
        _adapterLuid = adapterLuid;

        if (deviceInfo.GpuSharedLimit > 0)
        {
            _sharedMemoryBudget = new Sensor("NPU Shared Memory Budget", 0, SensorType.Data, this, settings);
            _sharedMemoryUsage = new Sensor("NPU Shared Memory Used", 1, SensorType.Data, this, settings);
            ActivateSensor(_sharedMemoryBudget);
            ActivateSensor(_sharedMemoryUsage);
        }

        if (deviceInfo.GpuDedicatedLimit > 0)
        {
            _dedicatedMemoryUsage = new Sensor("NPU Dedicated Memory Used", 3, SensorType.Data, this, settings);
            ActivateSensor(_dedicatedMemoryUsage);
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

    public NpuHardware(string adapterName, string friendlyName, ISettings settings)
        : base(friendlyName,
               new Identifier("npu", adapterName.GetHashCode().ToString("X", CultureInfo.InvariantCulture)),
               settings)
    {
        _adapterName = adapterName;
        _pdhBacked = true;

        _nodeUsage = Array.Empty<Sensor>();
        _nodeUsagePrevValue = Array.Empty<long>();
        _nodeUsagePrevTick = Array.Empty<DateTime>();

        _pdhLoad = new Sensor("NPU Total", 0, SensorType.Load, this, settings);
        ActivateSensor(_pdhLoad);

        _pdhDedicatedMemUsed = new Sensor("NPU Dedicated Memory Used", 0, SensorType.Data, this, settings);
        ActivateSensor(_pdhDedicatedMemUsed);

        _pdhSharedMemUsed = new Sensor("NPU Shared Memory Used", 1, SensorType.Data, this, settings);
        ActivateSensor(_pdhSharedMemUsed);

        _pdhSharedMemLimit = new Sensor("NPU Shared Memory Budget", 2, SensorType.Data, this, settings);
        ActivateSensor(_pdhSharedMemLimit);

        OpenQuery();
    }

    public override HardwareType HardwareType => HardwareType.Npu;

    public override void Update()
    {
        if (_pdhBacked)
        {
            UpdatePdh();
            return;
        }

        if (!D3DDisplayDevice.GetDeviceInfoByLuid(_adapterLuid, out D3DDisplayDevice.D3DDeviceInfo deviceInfo))
            return;

        if (_dedicatedMemoryUsage != null)
        {
            _dedicatedMemoryUsage.Value = 1f * deviceInfo.GpuDedicatedUsed / 1024 / 1024 / 1024;
            ActivateSensor(_dedicatedMemoryUsage);
        }

        if (_sharedMemoryBudget != null)
        {
            _sharedMemoryBudget.Value = 1f * deviceInfo.GpuSharedLimit / 1024 / 1024 / 1024;
            ActivateSensor(_sharedMemoryBudget);

            if (_sharedMemoryUsage != null)
            {
                _sharedMemoryUsage.Value = 1f * deviceInfo.GpuSharedUsed / 1024 / 1024 / 1024;
                ActivateSensor(_sharedMemoryUsage);
            }
        }

        if (_nodeUsage.Length != deviceInfo.Nodes.Length)
            return;

        foreach (D3DDisplayDevice.D3DDeviceNodeInfo node in deviceInfo.Nodes)
        {
            if (_nodeUsage[node.Id] == null)
                continue;

            long runningTimeDiff = node.RunningTime - _nodeUsagePrevValue[node.Id];
            long timeDiff = node.QueryTime.Ticks - _nodeUsagePrevTick[node.Id].Ticks;

            if (timeDiff > 0)
                _nodeUsage[node.Id].Value = 100f * runningTimeDiff / timeDiff;

            _nodeUsagePrevValue[node.Id] = node.RunningTime;
            _nodeUsagePrevTick[node.Id] = node.QueryTime;
            ActivateSensor(_nodeUsage[node.Id]);
        }
    }

    public override string GetReport()
    {
        StringBuilder r = new();
        r.AppendLine("NPU Hardware");
        r.AppendLine();
        r.AppendFormat("Name: {0}{1}", Name, Environment.NewLine);

        if (_pdhBacked)
            r.AppendFormat("Adapter: {0}{1}", _adapterName, Environment.NewLine);
        else
            r.AppendFormat("AdapterLuid: 0x{0:X16}{1}", D3DDisplayDevice.GetLuidKey(_adapterLuid), Environment.NewLine);

        r.AppendFormat("Mode: {0}{1}", _pdhBacked ? "PDH" : "DXCore+D3DKMT", Environment.NewLine);
        r.AppendLine();
        return r.ToString();
    }

    public override void Close()
    {
        CloseQuery();
        base.Close();
    }

    private void UpdatePdh()
    {
        if (!_queryOpen)
            return;

        try
        {
            uint collectStatus = NpuPdh.PdhCollectQueryData(_queryHandle);
            if (collectStatus != 0)
                return;

            if (_firstSample)
            {
                _firstSample = false;
                return;
            }

            if (_utilizationCounter != IntPtr.Zero && NpuPdh.TryGetDoubleValue(_utilizationCounter, out double utilPct))
                _pdhLoad.Value = (float)Math.Max(0, Math.Min(100, utilPct));

            if (_dedicatedMemCounter != IntPtr.Zero && NpuPdh.TryGetDoubleValue(_dedicatedMemCounter, out double dedicatedBytes))
                _pdhDedicatedMemUsed.Value = (float)(dedicatedBytes / 1024.0 / 1024.0 / 1024.0);

            if (_sharedMemUsedCounter != IntPtr.Zero && NpuPdh.TryGetDoubleValue(_sharedMemUsedCounter, out double sharedUsedBytes))
                _pdhSharedMemUsed.Value = (float)(sharedUsedBytes / 1024.0 / 1024.0 / 1024.0);

            if (_sharedMemLimitCounter != IntPtr.Zero && NpuPdh.TryGetDoubleValue(_sharedMemLimitCounter, out double sharedLimitBytes))
                _pdhSharedMemLimit.Value = (float)(sharedLimitBytes / 1024.0 / 1024.0 / 1024.0);
        }
        catch
        {
        }
    }

    private void OpenQuery()
    {
        try
        {
            uint status = NpuPdh.PdhOpenQuery(IntPtr.Zero, IntPtr.Zero, out _queryHandle);
            if (status != 0)
                return;

            string utilPath = BuildCounterPath(UtilizationObjectName, _adapterName, UtilizationCounterName);
            NpuPdh.PdhAddEnglishCounter(_queryHandle, utilPath, IntPtr.Zero, out _utilizationCounter);

            string dedMemPath = BuildCounterPath(MemoryObjectName, _adapterName, DedicatedMemoryUsedName);
            NpuPdh.PdhAddEnglishCounter(_queryHandle, dedMemPath, IntPtr.Zero, out _dedicatedMemCounter);

            string sharedUsedPath = BuildCounterPath(MemoryObjectName, _adapterName, SharedMemoryUsedName);
            NpuPdh.PdhAddEnglishCounter(_queryHandle, sharedUsedPath, IntPtr.Zero, out _sharedMemUsedCounter);

            string sharedLimitPath = BuildCounterPath(MemoryObjectName, _adapterName, SharedMemoryLimitName);
            NpuPdh.PdhAddEnglishCounter(_queryHandle, sharedLimitPath, IntPtr.Zero, out _sharedMemLimitCounter);

            _queryOpen = true;
        }
        catch
        {
            CloseQuery();
        }
    }

    private void CloseQuery()
    {
        if (_queryOpen && _queryHandle != IntPtr.Zero)
        {
            try { NpuPdh.PdhCloseQuery(_queryHandle); } catch { /* ignore */ }
        }

        _queryHandle = IntPtr.Zero;
        _utilizationCounter = IntPtr.Zero;
        _dedicatedMemCounter = IntPtr.Zero;
        _sharedMemUsedCounter = IntPtr.Zero;
        _sharedMemLimitCounter = IntPtr.Zero;
        _queryOpen = false;
    }

    private static string BuildCounterPath(string objectName, string instanceName, string counterName)
    {
        return $@"\{objectName}({instanceName})\{counterName}";
    }
}
