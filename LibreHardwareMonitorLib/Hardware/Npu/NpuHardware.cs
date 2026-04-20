// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Win32.System.Performance;

namespace LibreHardwareMonitor.Hardware.Npu;

/// <summary>
/// Represents a Neural Processing Unit (NPU) hardware device.
/// Reads utilization and memory metrics from Windows Performance Data Helper (PDH)
/// counters, which are the same counters exposed by Windows Task Manager on
/// Windows 11 24H2 and later.
/// </summary>
internal sealed class NpuHardware : Hardware
{
    // Counter name constants — identical to those used by Windows Task Manager.
    private const string UtilizationObjectName = "NPU Engine Utilization";
    private const string MemoryObjectName = "NPU Adapter Memory";

    private const string UtilizationCounterName = "Utilization Percentage";
    private const string DedicatedMemoryUsedName = "Dedicated Memory Used";
    private const string SharedMemoryUsedName = "Shared Memory Used";
    private const string SharedMemoryLimitName = "Shared Memory Budget";

    private readonly string _adapterName;

    // PDH query and counter handles
    private IntPtr _queryHandle = IntPtr.Zero;
    private IntPtr _utilizationCounter = IntPtr.Zero;
    private IntPtr _dedicatedMemCounter = IntPtr.Zero;
    private IntPtr _sharedMemUsedCounter = IntPtr.Zero;
    private IntPtr _sharedMemLimitCounter = IntPtr.Zero;

    private bool _queryOpen;
    private bool _firstSample = true;

    // Sensors
    private readonly Sensor _load;
    private readonly Sensor _dedicatedMemUsed;
    private readonly Sensor _sharedMemUsed;
    private readonly Sensor _sharedMemLimit;

    /// <summary>
    /// Creates a new <see cref="NpuHardware"/> instance.
    /// </summary>
    /// <param name="adapterName">
    /// The PDH instance name for the NPU adapter, as returned by
    /// <c>PdhEnumObjectItemsW</c> on the "NPU Engine Utilization" object.
    /// </param>
    /// <param name="friendlyName">Human-readable device name (e.g. "Qualcomm Hexagon NPU").</param>
    /// <param name="settings">Settings storage passed down from <see cref="Computer"/>.</param>
    public NpuHardware(string adapterName, string friendlyName, ISettings settings)
        : base(friendlyName,
               new Identifier("npu", adapterName.GetHashCode().ToString("X", CultureInfo.InvariantCulture)),
               settings)
    {
        _adapterName = adapterName;

        _load = new Sensor("NPU Total", 0, SensorType.Load, this, settings);
        ActivateSensor(_load);

        _dedicatedMemUsed = new Sensor("NPU Dedicated Memory Used", 0, SensorType.SmallData, this, settings);
        ActivateSensor(_dedicatedMemUsed);

        _sharedMemUsed = new Sensor("NPU Shared Memory Used", 1, SensorType.SmallData, this, settings);
        ActivateSensor(_sharedMemUsed);

        _sharedMemLimit = new Sensor("NPU Shared Memory Total", 2, SensorType.SmallData, this, settings);
        ActivateSensor(_sharedMemLimit);

        OpenQuery();
    }

    /// <inheritdoc />
    public override HardwareType HardwareType => HardwareType.Npu;

    /// <inheritdoc />
    public override void Update()
    {
        if (!_queryOpen)
            return;

        try
        {
            uint collectStatus = NpuPdh.PdhCollectQueryData(_queryHandle);
            if (collectStatus != 0)
                return;

            // The first collection after opening only seeds the running-time
            // accumulators; a valid utilization percentage requires two samples.
            if (_firstSample)
            {
                _firstSample = false;
                return;
            }

            // Utilization
            if (_utilizationCounter != IntPtr.Zero)
            {
                if (NpuPdh.TryGetDoubleValue(_utilizationCounter, out double utilPct))
                    _load.Value = (float)Math.Max(0, Math.Min(100, utilPct));
            }

            // Dedicated memory (MB)
            if (_dedicatedMemCounter != IntPtr.Zero)
            {
                if (NpuPdh.TryGetDoubleValue(_dedicatedMemCounter, out double dedicatedBytes))
                    _dedicatedMemUsed.Value = (float)(dedicatedBytes / 1024.0 / 1024.0);
            }

            // Shared memory used (MB)
            if (_sharedMemUsedCounter != IntPtr.Zero)
            {
                if (NpuPdh.TryGetDoubleValue(_sharedMemUsedCounter, out double sharedUsedBytes))
                    _sharedMemUsed.Value = (float)(sharedUsedBytes / 1024.0 / 1024.0);
            }

            // Shared memory budget/limit (MB)
            if (_sharedMemLimitCounter != IntPtr.Zero)
            {
                if (NpuPdh.TryGetDoubleValue(_sharedMemLimitCounter, out double sharedLimitBytes))
                    _sharedMemLimit.Value = (float)(sharedLimitBytes / 1024.0 / 1024.0);
            }
        }
        catch
        {
            // PDH calls should not throw, but guard defensively.
        }
    }

    /// <inheritdoc />
    public override string GetReport()
    {
        StringBuilder r = new();
        r.AppendLine("NPU Hardware");
        r.AppendLine();
        r.AppendFormat("Name: {0}{1}", Name, Environment.NewLine);
        r.AppendFormat("Adapter: {0}{1}", _adapterName, Environment.NewLine);
        r.AppendFormat("Query open: {0}{1}", _queryOpen, Environment.NewLine);
        r.AppendLine();
        return r.ToString();
    }

    /// <inheritdoc />
    public override void Close()
    {
        CloseQuery();
        base.Close();
    }

    private void OpenQuery()
    {
        try
        {
            uint status = NpuPdh.PdhOpenQuery(IntPtr.Zero, IntPtr.Zero, out _queryHandle);
            if (status != 0)
                return;

            // Utilization: \NPU Engine Utilization(<adapter>)\Utilization Percentage
            string utilPath = BuildCounterPath(UtilizationObjectName, _adapterName, UtilizationCounterName);
            NpuPdh.PdhAddEnglishCounter(_queryHandle, utilPath, IntPtr.Zero, out _utilizationCounter);

            // Memory counters: \NPU Adapter Memory(<adapter>)\...
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
