// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) 2020 Wilken Gottwalt<wilken.gottwalt@posteo.net>
// Copyright (C) 2023 Jannis234
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

// Implemented after the Linux kernel driver corsair_psu by Wilken Gottwalt and contributers
// Implemented after the Linux kernel driver cm_psu by Jannis234 and contributers https://github.com/Jannis234/cm-psu



using System.Collections.Generic;
using System.Linq;
using System.Text;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Psu.CoolerMaster;

public class CoolerMasterPsuGroup : IGroup
{
    private static readonly int[] _productIds =
    {
        0x0030, /* MasterWatt 1200 */
	    0x018D, /* V550 GOLD i MULTI */
	    0x018F, /* V650 GOLD i MULTI */
	    0x0191, /* V750 GOLD i MULTI */
	    0x0193, /* V850 GOLD i MULTI */
	    0x0195, /* V550 GOLD i 12VO */
	    0x0197, /* V650 GOLD i 12VO */
	    0x0199, /* V750 GOLD i 12VO */
	    0x019B, /* V850 GOLD i 12VO */
	    0x019D, /* V650 PLATINUM i 12VO */
	    0x019F, /* V750 PLATINUM i 12VO */
	    0x01A1, /* V850 PLATINUM i 12VO */
        0x01A5, /* FANLESS 1300 */
    };

    private static readonly ushort _vendorId = 0x2516;
    private readonly List<IHardware> _hardware;
    private readonly StringBuilder _report;

    public CoolerMasterPsuGroup(ISettings settings)
    {
        _report = new StringBuilder();
        _report.AppendLine("CoolerMaster V Gold i series PSU Hardware");
        _report.AppendLine();

        _hardware = new List<IHardware>();
        foreach (HidDevice dev in DeviceList.Local.GetHidDevices(_vendorId))
        {
            if (_productIds.Contains(dev.ProductID))
            {
                var device = new CoolerMasterPsu(dev, settings, _hardware.Count);
                _hardware.Add(device);
                _report.AppendLine($"Device name: {device.Name}");
                _report.AppendLine();
            }
        }
    }

    public IReadOnlyList<IHardware> Hardware => _hardware;

    public void Close()
    {
        foreach (IHardware iHardware in _hardware)
        {
            if (iHardware is Hardware hardware)
                hardware.Close();
        }
    }

    public string GetReport()
    {
        return _report.ToString();
    }
}
