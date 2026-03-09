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
using System.Threading;
using System.Threading.Tasks;
using LibreHardwareMonitor.Interop;

namespace LibreHardwareMonitor.Hardware.Gpu;

internal class NvidiaGroup : IGroup, IHardwareChanged
{
    private readonly Dictionary<NvApi.NvPhysicalGpuHandle, NvidiaGpu> _hardwareByHandle = new();
    private readonly List<IHardware> _hardwareInternal = new();
    private readonly StringBuilder _report = new();
    private readonly ISettings _settings;

    private CancellationTokenSource _cancellationTokenSource;
    private Task _monitorTask;
    private bool _disposed;
    private bool _nvidiaWasAvailable;

    public NvidiaGroup(ISettings settings)
    {
        _settings = settings;

        _report.AppendLine("NvApi");
        _report.AppendLine();

        if (Software.OperatingSystem.IsUnix)
        {
            _report.AppendLine("Status: Not supported on Unix for NvApi group");
            _report.AppendLine();
            return;
        }

        RefreshHardware(raiseEvents: false);
        StartMonitorTask();
    }

    public event HardwareEventHandler HardwareAdded;
    public event HardwareEventHandler HardwareRemoved;

    public IReadOnlyList<IHardware> Hardware
    {
        get { return _hardwareInternal; }
    }

    public string GetReport() => _report.ToString();

    public void Close()
    {
        _disposed = true;

        _cancellationTokenSource?.Cancel();

        try
        {
            _monitorTask?.Wait(TimeSpan.FromSeconds(1));
        }
        catch (AggregateException)
        {
            // ignored
        }

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _monitorTask = null;

        var toClose = _hardwareInternal.ToList();

        _hardwareInternal.Clear();
        _hardwareByHandle.Clear();

        foreach (Hardware gpu in toClose)
        {
            gpu.Close();
        }

        NvidiaML.Close();
    }

    private void StartMonitorTask()
    {
        CancellationTokenSource cts = new();
        _cancellationTokenSource = cts;

        CancellationToken token = cts.Token;

        _monitorTask = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), token).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                if (_disposed)
                {
                    break;
                }

                RefreshHardware(raiseEvents: true);
            }
        }, token);
    }

    private void RefreshHardware(bool raiseEvents)
    {
        if (_disposed)
        {
            return;
        }

        bool isAvailable = TryProbeNvidia(out NvApi.NvPhysicalGpuHandle[] handles, out int count, out _);

        if (!isAvailable)
        {
            if (_nvidiaWasAvailable)
            {
                NvidiaML.Close();
            }

            _nvidiaWasAvailable = false;
            RemoveAllHardware(raiseEvents);

            return;
        }

        //Driver was unavailable and came back: force NVML re-init
        if (!_nvidiaWasAvailable)
        {
            NvidiaML.Close();

            if (NvApi.NvAPI_GetInterfaceVersionString(out string version) == NvApi.NvStatus.OK)
            {
                _report.Append("Version: ");
                _report.AppendLine(version);
            }

            _report.Append("Number of GPUs: ");
            _report.AppendLine(count.ToString(CultureInfo.InvariantCulture));
            _report.AppendLine();
        }

        _nvidiaWasAvailable = true;

        var displayHandles = GetDisplayHandles();

        HashSet<NvApi.NvPhysicalGpuHandle> currentSet = new(handles.Take(count));
        List<Hardware> removed = [];

        foreach (KeyValuePair<NvApi.NvPhysicalGpuHandle, NvidiaGpu> pair in _hardwareByHandle.ToList())
        {
            if (!currentSet.Contains(pair.Key))
            {
                _hardwareByHandle.Remove(pair.Key);
                _hardwareInternal.Remove(pair.Value);
                removed.Add(pair.Value);
            }
        }

        for (int i = 0; i < count; ++i)
        {
            NvApi.NvPhysicalGpuHandle handle = handles[i];

            if (_hardwareByHandle.ContainsKey(handle))
            {
                continue;
            }

            displayHandles.TryGetValue(handle, out NvApi.NvDisplayHandle displayHandle);

            NvidiaGpu gpu = new(i, handle, displayHandle, _settings);
            _hardwareByHandle.Add(handle, gpu);
            _hardwareInternal.Add(gpu);

            if (raiseEvents)
            {
                HardwareAdded?.Invoke(gpu);
            }
        }

        foreach (Hardware gpu in removed)
        {
            if (raiseEvents)
            {
                HardwareRemoved?.Invoke(gpu);
            }

            gpu.Close();
        }
    }

    private void RemoveAllHardware(bool raiseEvents)
    {
        List<IHardware> removed;

        if (_hardwareInternal.Count == 0)
        {
            return;
        }

        removed = _hardwareInternal.ToList();

        _hardwareInternal.Clear();
        _hardwareByHandle.Clear();

        foreach (Hardware gpu in removed)
        {
            if (raiseEvents)
            {
                HardwareRemoved?.Invoke(gpu);
            }

            gpu.Close();
        }
    }

    private static IDictionary<NvApi.NvPhysicalGpuHandle, NvApi.NvDisplayHandle> GetDisplayHandles()
    {
        Dictionary<NvApi.NvPhysicalGpuHandle, NvApi.NvDisplayHandle> displayHandles = new();

        if (NvApi.NvAPI_EnumNvidiaDisplayHandle == null || NvApi.NvAPI_GetPhysicalGPUsFromDisplay == null)
        {
            return displayHandles;
        }

        NvApi.NvStatus status = NvApi.NvStatus.OK;
        int i = 0;

        while (status == NvApi.NvStatus.OK)
        {
            NvApi.NvDisplayHandle displayHandle = new();
            status = NvApi.NvAPI_EnumNvidiaDisplayHandle(i, ref displayHandle);
            i++;

            if (status != NvApi.NvStatus.OK)
            {
                continue;
            }

            NvApi.NvPhysicalGpuHandle[] handlesFromDisplay = new NvApi.NvPhysicalGpuHandle[NvApi.MAX_PHYSICAL_GPUS];
            if (NvApi.NvAPI_GetPhysicalGPUsFromDisplay(displayHandle, handlesFromDisplay, out uint countFromDisplay) != NvApi.NvStatus.OK)
            {
                continue;
            }

            for (int j = 0; j < countFromDisplay; j++)
            {
                if (!displayHandles.ContainsKey(handlesFromDisplay[j]))
                {
                    displayHandles.Add(handlesFromDisplay[j], displayHandle);
                }
            }
        }

        return displayHandles;
    }

    private static bool TryProbeNvidia(out NvApi.NvPhysicalGpuHandle[] handles, out int count, out NvApi.NvStatus status)
    {
        handles = new NvApi.NvPhysicalGpuHandle[NvApi.MAX_PHYSICAL_GPUS];
        count = 0;
        status = NvApi.NvStatus.ApiNotInitialized;

        NvApi.Initialize();

        if (!NvApi.IsAvailable || NvApi.NvAPI_EnumPhysicalGPUs == null)
        {
            return false;
        }

        status = NvApi.NvAPI_EnumPhysicalGPUs(handles, out count);

        return status == NvApi.NvStatus.OK && count > 0;
    }
}
