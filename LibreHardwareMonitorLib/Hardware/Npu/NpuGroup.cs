// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LibreHardwareMonitor.Hardware.Npu;

internal sealed class NpuGroup : IGroup
{
    private readonly List<NpuHardware> _hardware = new();
    private readonly StringBuilder _report = new();

    public NpuGroup(ISettings settings)
    {
        if (Software.OperatingSystem.IsUnix)
            return;

        _report.AppendLine("NPU Detection");
        _report.AppendLine();

        try
        {
            HashSet<ulong> seenLuids = new();
            NpuDxCoreEnumerator.AdapterInfo[] adapters = NpuDxCoreEnumerator.EnumerateAdapters();

            _report.Append("DXCore NPU adapters found: ");
            _report.AppendLine(adapters.Length.ToString(CultureInfo.InvariantCulture));
            _report.AppendLine();

            foreach (NpuDxCoreEnumerator.AdapterInfo adapter in adapters)
            {
                ulong luidKey = D3DDisplayDevice.GetLuidKey(adapter.Luid);

                _report.Append("AdapterLuid: 0x");
                _report.AppendLine(luidKey.ToString("X16", CultureInfo.InvariantCulture));
                _report.Append("Description: ");
                _report.AppendLine(adapter.Description);

                if (!seenLuids.Add(luidKey))
                {
                    _report.AppendLine("Skipped duplicate LUID.");
                    _report.AppendLine();
                    continue;
                }

                if (!D3DDisplayDevice.GetDeviceInfoByLuid(adapter.Luid, out D3DDisplayDevice.D3DDeviceInfo deviceInfo))
                {
                    _report.AppendLine("Failed to get device info by LUID.");
                    _report.AppendLine();
                    continue;
                }

                _report.Append("ComputeOnly: ");
                _report.AppendLine(deviceInfo.ComputeOnly.ToString(CultureInfo.InvariantCulture));
                _report.Append("Nodes: ");
                _report.AppendLine(deviceInfo.Nodes?.Length.ToString(CultureInfo.InvariantCulture) ?? "0");

                if (!deviceInfo.ComputeOnly)
                {
                    _report.AppendLine("Skipped non-compute adapter.");
                    _report.AppendLine();
                    continue;
                }

                _hardware.Add(new NpuHardware(adapter.Luid, adapter.Description, deviceInfo, settings));

                _report.AppendLine("Added DXCore-backed NPU hardware.");
                _report.AppendLine();
            }

            if (_hardware.Count > 0)
                return;

            IReadOnlyList<string> instances = NpuPdh.EnumerateInstances("NPU Engine Utilization");

            _report.Append("PDH NPU instances found: ");
            _report.AppendLine(instances.Count.ToString(CultureInfo.InvariantCulture));
            _report.AppendLine();

            if (instances.Count == 0)
            {
                _report.AppendLine("No NPU adapters found via DXCore or PDH.");
                return;
            }

            foreach (string instance in instances)
            {
                _report.Append("Instance: ");
                _report.AppendLine(instance);

                _hardware.Add(new NpuHardware(instance, instance, settings));

                _report.AppendLine("Added PDH-backed NPU hardware.");
                _report.AppendLine();
            }
        }
        catch (Exception ex)
        {
            _report.Append("Error during NPU detection: ");
            _report.AppendLine(ex.Message);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<IHardware> Hardware => _hardware;

    /// <inheritdoc />
    public string GetReport() => _report.ToString();

    /// <inheritdoc />
    public void Close()
    {
        foreach (NpuHardware hardware in _hardware)
            hardware.Close();
    }
}
