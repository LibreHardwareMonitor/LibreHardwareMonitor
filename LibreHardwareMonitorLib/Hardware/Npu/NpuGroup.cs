// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.Win32;

namespace LibreHardwareMonitor.Hardware.Npu;

/// <summary>
/// Detects and enumerates Neural Processing Units (NPUs) on Windows.
/// </summary>
/// <remarks>
/// Detection relies on the Windows PDH performance counter objects
/// "NPU Engine Utilization" and "NPU Adapter Memory", which are present
/// on Windows 11 24H2 (build 26100) and later when an NPU driver is installed.
/// Qualcomm Hexagon NPUs on Snapdragon X-series devices are the primary
/// target, but the implementation is vendor-neutral: any NPU that registers
/// these PDH counters will be detected.
/// </remarks>
internal sealed class NpuGroup : IGroup
{
    private readonly List<NpuHardware> _hardware = new();
    private readonly StringBuilder _report = new();

    public NpuGroup(ISettings settings)
    {
        if (Software.OperatingSystem.IsUnix)
            return;

        _report.AppendLine("NPU Detection (PDH)");
        _report.AppendLine();

        try
        {
            IReadOnlyList<string> instances = NpuPdh.EnumerateInstances("NPU Engine Utilization");

            _report.Append("NPU Engine Utilization instances found: ");
            _report.AppendLine(instances.Count.ToString(CultureInfo.InvariantCulture));
            _report.AppendLine();

            if (instances.Count == 0)
            {
                _report.AppendLine("No NPU performance counters found.");
                _report.AppendLine("Requires Windows 11 24H2 (build 26100+) with an NPU driver installed.");
                return;
            }

            foreach (string instance in instances)
            {
                _report.Append("Instance: ");
                _report.AppendLine(instance);

                string friendlyName = ResolveFriendlyName(instance);

                _report.Append("Friendly name: ");
                _report.AppendLine(friendlyName);

                var hardware = new NpuHardware(instance, friendlyName, settings);
                _hardware.Add(hardware);

                _report.AppendLine("Added NPU hardware.");
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

    /// <summary>
    /// Attempts to resolve a human-readable device name for the NPU identified
    /// by the PDH instance name.  Falls back to a generic name when the registry
    /// key is absent (e.g. the driver populates the PDH name from a GUID).
    /// </summary>
    private static string ResolveFriendlyName(string instanceName)
    {
        // The PDH instance name for Qualcomm's Hexagon NPU is typically of the
        // form "Qualcomm(R) NPU" or a numeric index.  Try to resolve through
        // the device registry; fall back to the instance name itself.
        try
        {
            // Check for a device whose DeviceDesc contains the instance name,
            // or look for known Qualcomm NPU hardware IDs.
            string hklm = @"SYSTEM\CurrentControlSet\Enum";
            string[] buses = { "ACPI", "PCI" };

            foreach (string bus in buses)
            {
                using RegistryKey busKey = Registry.LocalMachine.OpenSubKey($@"{hklm}\{bus}");
                if (busKey == null)
                    continue;

                foreach (string hardwareId in busKey.GetSubKeyNames())
                {
                    // Qualcomm NPU hardware IDs: QCOM6490, QCOM8280 (Hexagon), etc.
                    if (!hardwareId.StartsWith("QCOM", StringComparison.OrdinalIgnoreCase))
                        continue;

                    using RegistryKey hwKey = busKey.OpenSubKey(hardwareId);
                    if (hwKey == null)
                        continue;

                    foreach (string instanceKey in hwKey.GetSubKeyNames())
                    {
                        using RegistryKey instKey = hwKey.OpenSubKey(instanceKey);
                        if (instKey == null)
                            continue;

                        object service = instKey.GetValue("Service");
                        if (service is string svc &&
                            (svc.IndexOf("npu", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             svc.IndexOf("hexagon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             svc.IndexOf("cdsp", StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            object desc = instKey.GetValue("DeviceDesc");
                            if (desc is string deviceDesc)
                            {
                                string[] parts = deviceDesc.Split(';');
                                string name = parts[parts.Length - 1].Trim();
                                if (!string.IsNullOrEmpty(name))
                                    return name;
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Registry access may fail; fall through to default.
        }

        // If the instance name looks like a human-readable label, use it directly.
        if (!string.IsNullOrWhiteSpace(instanceName) &&
            instanceName.IndexOf("0x", StringComparison.OrdinalIgnoreCase) == -1 &&
            !instanceName.StartsWith("{", StringComparison.Ordinal))
        {
            return instanceName;
        }

        return "NPU";
    }
}
