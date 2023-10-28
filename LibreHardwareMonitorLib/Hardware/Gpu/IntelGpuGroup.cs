// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LibreHardwareMonitor.Hardware.Cpu;

namespace LibreHardwareMonitor.Hardware.Gpu;

internal class IntelGpuGroup : IGroup
{
    private readonly List<Hardware> _hardware = new();
    private readonly StringBuilder _report = new();

    public IntelGpuGroup(List<IntelCpu> intelCpus, ISettings settings)
    {
        if (!Software.OperatingSystem.IsUnix && intelCpus?.Count > 0)
        {
            _report.AppendLine("Intel GPU (D3D)");
            _report.AppendLine();

            string[] ids = D3DDisplayDevice.GetDeviceIdentifiers();

            _report.Append("Number of adapters: ");
            _report.AppendLine(ids.Length.ToString(CultureInfo.InvariantCulture));
            _report.AppendLine();

            for (int i = 0; i < ids.Length; i++)
            {
                string deviceId = ids[i];
                bool isIntel = deviceId.IndexOf("VEN_8086", StringComparison.Ordinal) != -1;

                _report.Append("AdapterIndex: ");
                _report.AppendLine(i.ToString(CultureInfo.InvariantCulture));
                _report.Append("DeviceId: ");
                _report.AppendLine(deviceId);
                _report.Append("IsIntel: ");
                _report.AppendLine(isIntel.ToString(CultureInfo.InvariantCulture));

                if (isIntel && D3DDisplayDevice.GetDeviceInfoByIdentifier(deviceId, out D3DDisplayDevice.D3DDeviceInfo deviceInfo))
                {
                    _report.Append("GpuSharedLimit: ");
                    _report.AppendLine(deviceInfo.GpuSharedLimit.ToString(CultureInfo.InvariantCulture));
                    _report.Append("GpuSharedUsed: ");
                    _report.AppendLine(deviceInfo.GpuSharedUsed.ToString(CultureInfo.InvariantCulture));
                    _report.Append("GpuSharedMax: ");
                    _report.AppendLine(deviceInfo.GpuSharedMax.ToString(CultureInfo.InvariantCulture));
                    _report.Append("GpuDedicatedLimit: ");
                    _report.AppendLine(deviceInfo.GpuDedicatedLimit.ToString(CultureInfo.InvariantCulture));
                    _report.Append("GpuDedicatedUsed: ");
                    _report.AppendLine(deviceInfo.GpuDedicatedUsed.ToString(CultureInfo.InvariantCulture));
                    _report.Append("GpuDedicatedMax: ");
                    _report.AppendLine(deviceInfo.GpuDedicatedMax.ToString(CultureInfo.InvariantCulture));
                    _report.Append("Integrated: ");
                    _report.AppendLine(deviceInfo.Integrated.ToString(CultureInfo.InvariantCulture));

                    if (deviceInfo.Integrated)
                    {
                        // It may seem strange to only use the first cpu here, but in-case we have a multi cpu system with integrated graphics (does that exist?),
                        // we would pick up the multiple device identifiers above and would add one instance for each CPU.
                        _hardware.Add(new IntelIntegratedGpu(intelCpus[0], deviceId, deviceInfo, settings));
                    }
                }

                _report.AppendLine();
            }
        }
    }

    public IReadOnlyList<IHardware> Hardware => _hardware;

    public string GetReport()
    {
        return _report.ToString();
    }

    public void Close()
    {
        foreach (Hardware gpu in _hardware)
            gpu.Close();
    }
}