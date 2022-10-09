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
using LibreHardwareMonitor.Interop;

namespace LibreHardwareMonitor.Hardware.Gpu;

internal class AmdGpuGroup : IGroup
{
    private readonly List<AmdGpu> _hardware = new();
    private readonly StringBuilder _report = new();
    private readonly AtiAdlxx.ADLStatus _status;

    public AmdGpuGroup(ISettings settings)
    {
        try
        {
            _status = AtiAdlxx.ADL_Main_Control_Create(1);

            _report.AppendLine("AMD Display Library");
            _report.AppendLine();
            _report.Append("Status: ");
            _report.AppendLine(_status == AtiAdlxx.ADLStatus.ADL_OK ? "OK" : _status.ToString());
            _report.AppendLine();

            if (_status == AtiAdlxx.ADLStatus.ADL_OK)
            {
                int numberOfAdapters = 0;
                AtiAdlxx.ADL_Adapter_NumberOfAdapters_Get(ref numberOfAdapters);

                _report.Append("Number of adapters: ");
                _report.AppendLine(numberOfAdapters.ToString(CultureInfo.InvariantCulture));
                _report.AppendLine();

                if (numberOfAdapters > 0)
                {
                    List<AmdGpu> potentialHardware = new();

                    AtiAdlxx.ADLAdapterInfo[] adapterInfo = new AtiAdlxx.ADLAdapterInfo[numberOfAdapters];
                    if (AtiAdlxx.ADL_Adapter_AdapterInfo_Get(adapterInfo) == AtiAdlxx.ADLStatus.ADL_OK)
                    {
                        for (int i = 0; i < numberOfAdapters; i++)
                        {
                            AtiAdlxx.ADL_Adapter_Active_Get(adapterInfo[i].AdapterIndex, out int isActive);

                            int adapterId = -1;
                            if (AtiAdlxx.ADL_Method_Exists(nameof(AtiAdlxx.ADL_Adapter_ID_Get)))
                                AtiAdlxx.ADL_Adapter_ID_Get(adapterInfo[i].AdapterIndex, out adapterId);

                            _report.Append("AdapterIndex: ");
                            _report.AppendLine(i.ToString(CultureInfo.InvariantCulture));
                            _report.Append("isActive: ");
                            _report.AppendLine(isActive.ToString(CultureInfo.InvariantCulture));
                            _report.Append("AdapterName: ");
                            _report.AppendLine(adapterInfo[i].AdapterName);
                            _report.Append("UDID: ");
                            _report.AppendLine(adapterInfo[i].UDID);
                            _report.Append("PNPString: ");
                            _report.AppendLine(adapterInfo[i].PNPString);
                            _report.Append("Present: ");
                            _report.AppendLine(adapterInfo[i].Present.ToString(CultureInfo.InvariantCulture));
                            _report.Append("VendorID: 0x");
                            _report.AppendLine(adapterInfo[i].VendorID.ToString("X", CultureInfo.InvariantCulture));
                            _report.Append("BusNumber: ");
                            _report.AppendLine(adapterInfo[i].BusNumber.ToString(CultureInfo.InvariantCulture));
                            _report.Append("DeviceNumber: ");
                            _report.AppendLine(adapterInfo[i].DeviceNumber.ToString(CultureInfo.InvariantCulture));
                            _report.Append("FunctionNumber: ");
                            _report.AppendLine(adapterInfo[i].FunctionNumber.ToString(CultureInfo.InvariantCulture));
                            _report.Append("AdapterID: 0x");
                            _report.AppendLine(adapterId.ToString("X", CultureInfo.InvariantCulture));

                            if (!string.IsNullOrEmpty(adapterInfo[i].UDID) && adapterInfo[i].VendorID == AtiAdlxx.ATI_VENDOR_ID)
                                potentialHardware.Add(new AmdGpu(adapterInfo[i], settings));

                            _report.AppendLine();
                        }
                    }

                    foreach (IGrouping<string, AmdGpu> amdGpus in potentialHardware.GroupBy(x => $"{x.BusNumber}-{x.DeviceNumber}"))
                    {
                        AmdGpu amdGpu = amdGpus.OrderByDescending(x => x.Sensors.Length).FirstOrDefault();
                        if (amdGpu != null)
                            _hardware.Add(amdGpu);
                    }
                }
            }
        }
        catch (DllNotFoundException)
        { }
        catch (EntryPointNotFoundException e)
        {
            _report.AppendLine();
            _report.AppendLine(e.ToString());
            _report.AppendLine();
        }
    }

    public IReadOnlyList<IHardware> Hardware => _hardware;

    public string GetReport()
    {
        return _report.ToString();
    }

    public void Close()
    {
        try
        {
            foreach (AmdGpu gpu in _hardware)
                gpu.Close();

            if (_status == AtiAdlxx.ADLStatus.ADL_OK)
                AtiAdlxx.ADL_Main_Control_Destroy();
        }
        catch (Exception)
        { }
    }
}
