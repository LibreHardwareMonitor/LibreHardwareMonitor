// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LibreHardwareMonitor.Hardware.Gpu
{
    internal class NvidiaGroup : IGroup
    {
        private readonly List<Hardware> _hardware = new List<Hardware>();
        private readonly StringBuilder _report = new StringBuilder();
        private readonly NVML _nvml = new NVML();

        public NvidiaGroup(ISettings settings)
        {
            if (!NVAPI.IsAvailable)
                return;

            _report.AppendLine("NVAPI");
            _report.AppendLine();

            string version;
            if (NVAPI.NvAPI_GetInterfaceVersionString(out version) == NvStatus.OK)
            {
                _report.Append("Version: ");
                _report.AppendLine(version);
            }

            NvPhysicalGpuHandle[] handles = new NvPhysicalGpuHandle[NVAPI.MAX_PHYSICAL_GPUS];
            int count;
            if (NVAPI.NvAPI_EnumPhysicalGPUs == null)
            {
                _report.AppendLine("Error: NvAPI_EnumPhysicalGPUs not available");
                _report.AppendLine();
                return;
            }
            else
            {
                NvStatus status = NVAPI.NvAPI_EnumPhysicalGPUs(handles, out count);
                if (status != NvStatus.OK)
                {
                    _report.AppendLine("Status: " + status);
                    _report.AppendLine();
                    return;
                }
            }

            IDictionary<NvPhysicalGpuHandle, NvDisplayHandle> displayHandles = new Dictionary<NvPhysicalGpuHandle, NvDisplayHandle>();
            if (NVAPI.NvAPI_EnumNvidiaDisplayHandle != null && NVAPI.NvAPI_GetPhysicalGPUsFromDisplay != null)
            {
                NvStatus status = NvStatus.OK;
                int i = 0;
                while (status == NvStatus.OK)
                {
                    NvDisplayHandle displayHandle = new NvDisplayHandle();
                    status = NVAPI.NvAPI_EnumNvidiaDisplayHandle(i, ref displayHandle);
                    i++;

                    if (status == NvStatus.OK)
                    {
                        NvPhysicalGpuHandle[] handlesFromDisplay = new NvPhysicalGpuHandle[NVAPI.MAX_PHYSICAL_GPUS];
                        uint countFromDisplay;
                        if (NVAPI.NvAPI_GetPhysicalGPUsFromDisplay(displayHandle, handlesFromDisplay, out countFromDisplay) == NvStatus.OK)
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
                NvDisplayHandle displayHandle;
                displayHandles.TryGetValue(handles[i], out displayHandle);
                _hardware.Add(new NvidiaGpu(i, handles[i], displayHandle, settings, _nvml));
            }

            _report.AppendLine();
        }

        public IEnumerable<IHardware> Hardware => _hardware;

        public string GetReport()
        {
            return _report.ToString();
        }

        public void Close()
        {
            foreach (Hardware gpu in _hardware)
                gpu.Close();

            _nvml.Close();
        }
    }
}
