// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LibreHardwareMonitor.Interop;

namespace LibreHardwareMonitor.Hardware.Gpu;

internal class NvidiaGroup : IGroup
{
    private readonly List<Hardware> _hardware = new();
    private readonly StringBuilder _report = new();

    public NvidiaGroup(ISettings settings)
    {
        if (!NvApi.IsAvailable)
            return;

        _report.AppendLine("NvApi");
        _report.AppendLine();

        if (NvApi.NvAPI_GetInterfaceVersionString(out string version) == NvApi.NvStatus.OK)
        {
            _report.Append("Version: ");
            _report.AppendLine(version);
        }

        NvApi.NvPhysicalGpuHandle[] handles = new NvApi.NvPhysicalGpuHandle[NvApi.MAX_PHYSICAL_GPUS];
        if (NvApi.NvAPI_EnumPhysicalGPUs == null)
        {
            _report.AppendLine("Error: NvAPI_EnumPhysicalGPUs not available");
            _report.AppendLine();
            return;
        }

        NvApi.NvStatus status = NvApi.NvAPI_EnumPhysicalGPUs(handles, out int count);
        if (status != NvApi.NvStatus.OK)
        {
            _report.AppendLine("Status: " + status);
            _report.AppendLine();
            return;
        }

        IDictionary<NvApi.NvPhysicalGpuHandle, NvApi.NvDisplayHandle> displayHandles = new Dictionary<NvApi.NvPhysicalGpuHandle, NvApi.NvDisplayHandle>();
        if (NvApi.NvAPI_EnumNvidiaDisplayHandle != null && NvApi.NvAPI_GetPhysicalGPUsFromDisplay != null)
        {
            status = NvApi.NvStatus.OK;
            int i = 0;
            while (status == NvApi.NvStatus.OK)
            {
                NvApi.NvDisplayHandle displayHandle = new();
                status = NvApi.NvAPI_EnumNvidiaDisplayHandle(i, ref displayHandle);
                i++;

                if (status == NvApi.NvStatus.OK)
                {
                    NvApi.NvPhysicalGpuHandle[] handlesFromDisplay = new NvApi.NvPhysicalGpuHandle[NvApi.MAX_PHYSICAL_GPUS];
                    if (NvApi.NvAPI_GetPhysicalGPUsFromDisplay(displayHandle, handlesFromDisplay, out uint countFromDisplay) == NvApi.NvStatus.OK)
                    {
                        for (int j = 0; j < countFromDisplay; j++)
                        {
                            if (!displayHandles.ContainsKey(handlesFromDisplay[j]))
                                displayHandles.Add(handlesFromDisplay[j], displayHandle);
                        }
                    }
                }
            }
        }

        _report.Append("Number of GPUs: ");
        _report.AppendLine(count.ToString(CultureInfo.InvariantCulture));

        for (int i = 0; i < count; i++)
        {
            displayHandles.TryGetValue(handles[i], out NvApi.NvDisplayHandle displayHandle);
            _hardware.Add(new NvidiaGpu(i, handles[i], displayHandle, settings));
        }

        _report.AppendLine();
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

        NvidiaML.Close();
    }
}