// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LibreHardwareMonitor.Interop;
using NvAPIWrapper;
using NvAPIWrapper.GPU;
using NvAPIWrapper.Native.Exceptions;

namespace LibreHardwareMonitor.Hardware.Gpu
{
    internal class NvidiaGroup : IGroup
    {
        private readonly List<Hardware> _hardware = new();
        private readonly StringBuilder _report = new();
        private readonly bool _initialized;

        public NvidiaGroup(ISettings settings)
        {
            _report.AppendLine("NvApi");
            _report.AppendLine();

            try
            {
                NVIDIA.Initialize();
                _initialized = true;
            }
            catch (Exception e) when (e is NVIDIAApiException or DllNotFoundException)
            {
                // No NVIDIA devices.
                return;
            }

            try
            {
                string interfaceVersion = NVIDIA.InterfaceVersionString;
                
                _report.Append("Version: ");
                _report.AppendLine(interfaceVersion);
            }
            catch (NVIDIAApiException)
            { }

            try
            {
                string driverVersion = NVIDIA.DriverVersion.ToString();
                
                _report.Append("Driver: ");
                _report.AppendLine(driverVersion);
            }
            catch (NVIDIAApiException)
            { }

            try
            {
                string driverBranch = NVIDIA.DriverBranchVersion;
                
                _report.Append("Driver branch: ");
                _report.AppendLine(driverBranch);
            }
            catch (NVIDIAApiException)
            { }

            try
            {
                PhysicalGPU[] physicalGpus = PhysicalGPU.GetPhysicalGPUs();

                _report.Append("Number of GPUs: ");
                _report.AppendLine(physicalGpus.Length.ToString(CultureInfo.InvariantCulture));

                for (int i = 0; i < physicalGpus.Length; i++)
                    _hardware.Add(new NvidiaGpu(i, physicalGpus[i], settings));
            }
            catch (NVIDIAApiException)
            { }

            _report.AppendLine();
        }

        public IReadOnlyList<IHardware> Hardware => _hardware;

        public string GetReport()
        {
            return _report.ToString();
        }

        public void Close()
        {
            if (!_initialized)
                return;


            foreach (Hardware gpu in _hardware)
                gpu.Close();

            NvidiaML.Close();

            try
            {
                NVIDIA.Unload();
            }
            catch (Exception e) when (e is NVIDIAApiException or DllNotFoundException)
            { }
        }
    }
}
