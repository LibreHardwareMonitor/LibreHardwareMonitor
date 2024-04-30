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
    private readonly IntPtr _context = IntPtr.Zero;
    private readonly List<AmdGpu> _hardware = new();
    private readonly StringBuilder _report = new();
    private readonly AtiAdlxx.ADLStatus _status;

    public AmdGpuGroup(ISettings settings)
    {
        try
        {
            _status = AtiAdlxx.ADL2_Main_Control_Create(AtiAdlxx.Main_Memory_Alloc, 1, ref _context);

            _report.AppendLine("AMD Display Library");
            _report.AppendLine();
            _report.Append("Status: ");
            _report.AppendLine(_status == AtiAdlxx.ADLStatus.ADL_OK ? "OK" : _status.ToString());
            _report.AppendLine();

            if (_status == AtiAdlxx.ADLStatus.ADL_OK)
            {
                int numberOfAdapters = 0;
                AtiAdlxx.ADL2_Adapter_NumberOfAdapters_Get(_context, ref numberOfAdapters);

                _report.Append("Number of adapters: ");
                _report.AppendLine(numberOfAdapters.ToString(CultureInfo.InvariantCulture));
                _report.AppendLine();

                if (numberOfAdapters > 0)
                {
                    List<AmdGpu> potentialHardware = new();

                    AtiAdlxx.ADLAdapterInfo[] adapterInfo = new AtiAdlxx.ADLAdapterInfo[numberOfAdapters];
                    if (AtiAdlxx.ADL2_Adapter_AdapterInfo_Get(ref _context, adapterInfo) == AtiAdlxx.ADLStatus.ADL_OK)
                    {
                        for (int i = 0; i < numberOfAdapters; i++)
                        {
                            uint device = 0;
                            AtiAdlxx.ADLGcnInfo gcnInfo = new();
                            AtiAdlxx.ADLPMLogSupportInfo pmLogSupportInfo = new();
                            AtiAdlxx.ADL2_Adapter_Active_Get(_context, adapterInfo[i].AdapterIndex, out int isActive);

                            if (AtiAdlxx.ADL_Method_Exists(nameof(AtiAdlxx.ADL2_GcnAsicInfo_Get)))
                            {
                                AtiAdlxx.ADL2_GcnAsicInfo_Get(_context, adapterInfo[i].AdapterIndex, ref gcnInfo);
                            }

                            int adapterId = -1;
                            if (AtiAdlxx.ADL_Method_Exists(nameof(AtiAdlxx.ADL2_Adapter_ID_Get)))
                                AtiAdlxx.ADL2_Adapter_ID_Get(_context, adapterInfo[i].AdapterIndex, out adapterId);

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
                            _report.AppendLine("Family: " + gcnInfo.ASICFamilyId);

                            int sensorsSupported = 0;
                            if (AtiAdlxx.UsePmLogForFamily(gcnInfo.ASICFamilyId) &&
                                AtiAdlxx.ADL_Method_Exists(nameof(AtiAdlxx.ADL2_Adapter_PMLog_Support_Get)) &&
                                AtiAdlxx.ADL_Method_Exists(nameof(AtiAdlxx.ADL2_Device_PMLog_Device_Create)))
                            {
                                if (AtiAdlxx.ADLStatus.ADL_OK == AtiAdlxx.ADL2_Device_PMLog_Device_Create(_context, adapterInfo[i].AdapterIndex, ref device) &&
                                    AtiAdlxx.ADLStatus.ADL_OK == AtiAdlxx.ADL2_Adapter_PMLog_Support_Get(_context, adapterInfo[i].AdapterIndex, ref pmLogSupportInfo))
                                {
                                    int k = 0;
                                    while (pmLogSupportInfo.usSensors[k] != (ushort)AtiAdlxx.ADLPMLogSensors.ADL_SENSOR_MAXTYPES)
                                    {
                                        k++;
                                    }
                                    sensorsSupported = k;
                                }
                                _report.AppendLine("Sensors Supported: " + sensorsSupported);

                                if (device != 0)
                                {
                                    AtiAdlxx.ADL2_Device_PMLog_Device_Destroy(_context, device);
                                }
                            }

                            if (!string.IsNullOrEmpty(adapterInfo[i].UDID) && adapterInfo[i].VendorID == AtiAdlxx.ATI_VENDOR_ID &&
                                !IsAlreadyAdded(adapterInfo[i].BusNumber, adapterInfo[i].DeviceNumber))
                            {
                                if (sensorsSupported > 0)
                                {
                                    _hardware.Add(new AmdGpu(_context, adapterInfo[i], gcnInfo, settings));
                                }
                                else
                                {
                                    potentialHardware.Add(new AmdGpu(_context, adapterInfo[i], gcnInfo, settings));
                                }
                            }

                            _report.AppendLine();
                        }
                    }

                    foreach (IGrouping<string, AmdGpu> amdGpus in potentialHardware.GroupBy(x => $"{x.BusNumber}-{x.DeviceNumber}"))
                    {
                        AmdGpu amdGpu = amdGpus.OrderByDescending(x => x.Sensors.Length).FirstOrDefault();
                        if (amdGpu != null && !IsAlreadyAdded(amdGpu.BusNumber, amdGpu.DeviceNumber))
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

    private bool IsAlreadyAdded(int busNumber, int deviceNumber)
    {
        foreach (AmdGpu g in _hardware)
        {
            if (g.BusNumber == busNumber && g.DeviceNumber == deviceNumber)
            {
                return true;
            }
        }
        return false;
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

            if (_status == AtiAdlxx.ADLStatus.ADL_OK && _context != IntPtr.Zero)
                AtiAdlxx.ADL2_Main_Control_Destroy(_context);
        }
        catch (Exception)
        { }
    }
}
