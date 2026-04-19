// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LibreHardwareMonitor.Hardware.Gpu;

/// <summary>
/// Detects and enumerates Qualcomm Adreno GPUs via D3D display device enumeration.
/// </summary>
/// <remarks>
/// Qualcomm Adreno GPUs on Windows ARM devices do not expose AMD ADL, NVIDIA NVAPI,
/// or Intel GCL interfaces. They are, however, visible to the D3DKMT display adapter
/// enumeration API, which allows reading GPU engine utilization and memory usage.
/// Detection is based on the absence of known x86 GPU vendor IDs (Intel 8086, AMD 1002,
/// NVIDIA 10DE) combined with the device being marked as integrated.
/// </remarks>
internal class QualcommGpuGroup : IGroup
{
    private readonly List<QualcommGpu> _hardware = new();
    private readonly StringBuilder _report = new();

    public QualcommGpuGroup(ISettings settings)
    {
        if (Software.OperatingSystem.IsUnix)
            return;

        _report.AppendLine("Qualcomm GPU Detection (D3D)");
        _report.AppendLine();

        try
        {
            string[] ids = D3DDisplayDevice.GetDeviceIdentifiers();
            if (ids == null || ids.Length == 0)
            {
                _report.AppendLine("No D3D display devices found.");
                return;
            }

            _report.Append("Number of D3D adapters: ");
            _report.AppendLine(ids.Length.ToString(CultureInfo.InvariantCulture));
            _report.AppendLine();

            for (int i = 0; i < ids.Length; i++)
            {
                string deviceId = ids[i];

                // Skip known x86 GPU vendors — they are handled by their own groups
                bool isIntel = deviceId.IndexOf("VEN_8086", StringComparison.Ordinal) != -1;
                bool isAmd = deviceId.IndexOf("VEN_1002", StringComparison.Ordinal) != -1;
                bool isNvidia = deviceId.IndexOf("VEN_10DE", StringComparison.Ordinal) != -1;

                // Check for Qualcomm vendor ID (QCOM = 17CB) or generic non-PCI devices
                bool isQualcomm = deviceId.IndexOf("VEN_17CB", StringComparison.Ordinal) != -1
                               || deviceId.IndexOf("QCOM", StringComparison.OrdinalIgnoreCase) != -1;

                _report.Append("AdapterIndex: ");
                _report.AppendLine(i.ToString(CultureInfo.InvariantCulture));
                _report.Append("DeviceId: ");
                _report.AppendLine(deviceId);
                _report.Append("IsIntel: ");
                _report.AppendLine(isIntel.ToString(CultureInfo.InvariantCulture));
                _report.Append("IsAmd: ");
                _report.AppendLine(isAmd.ToString(CultureInfo.InvariantCulture));
                _report.Append("IsNvidia: ");
                _report.AppendLine(isNvidia.ToString(CultureInfo.InvariantCulture));
                _report.Append("IsQualcomm: ");
                _report.AppendLine(isQualcomm.ToString(CultureInfo.InvariantCulture));

                if (isIntel || isAmd || isNvidia)
                {
                    _report.AppendLine("Skipping — handled by vendor-specific group.");
                    _report.AppendLine();
                    continue;
                }

                if (D3DDisplayDevice.GetDeviceInfoByIdentifier(deviceId, out D3DDisplayDevice.D3DDeviceInfo deviceInfo))
                {
                    _report.Append("GpuSharedLimit: ");
                    _report.AppendLine(deviceInfo.GpuSharedLimit.ToString(CultureInfo.InvariantCulture));
                    _report.Append("GpuSharedUsed: ");
                    _report.AppendLine(deviceInfo.GpuSharedUsed.ToString(CultureInfo.InvariantCulture));
                    _report.Append("GpuDedicatedLimit: ");
                    _report.AppendLine(deviceInfo.GpuDedicatedLimit.ToString(CultureInfo.InvariantCulture));
                    _report.Append("GpuDedicatedUsed: ");
                    _report.AppendLine(deviceInfo.GpuDedicatedUsed.ToString(CultureInfo.InvariantCulture));
                    _report.Append("Integrated: ");
                    _report.AppendLine(deviceInfo.Integrated.ToString(CultureInfo.InvariantCulture));
                    _report.Append("Nodes: ");
                    _report.AppendLine(deviceInfo.Nodes?.Length.ToString(CultureInfo.InvariantCulture) ?? "0");

                    // On ARM devices, Qualcomm Adreno appears as an integrated adapter
                    // that is not from Intel/AMD/NVIDIA. Also accept explicitly Qualcomm-identified devices.
                    if (isQualcomm || (OpCode.IsArm && deviceInfo.Nodes != null && deviceInfo.Nodes.Length > 0))
                    {
                        var gpu = new QualcommGpu(deviceId, deviceInfo, settings);
                        _hardware.Add(gpu);
                        _report.AppendLine("Added Qualcomm Adreno GPU.");
                    }
                    else
                    {
                        _report.AppendLine("Skipped — not identified as Qualcomm/Adreno.");
                    }
                }
                else
                {
                    _report.AppendLine("Failed to get device info.");
                }

                _report.AppendLine();
            }
        }
        catch (Exception ex)
        {
            _report.Append("Error during Qualcomm GPU detection: ");
            _report.AppendLine(ex.Message);
        }
    }

    public IReadOnlyList<IHardware> Hardware => _hardware;

    public string GetReport()
    {
        return _report.ToString();
    }

    public void Close()
    {
        foreach (QualcommGpu gpu in _hardware)
            gpu.Close();
    }
}
