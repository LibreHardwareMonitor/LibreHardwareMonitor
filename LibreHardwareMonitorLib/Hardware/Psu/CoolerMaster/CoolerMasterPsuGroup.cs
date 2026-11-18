// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Psu.CoolerMaster;

public class CoolerMasterPsuGroup : IGroup
{
    private static readonly Dictionary<int, string> _products = new()
    {
        { 0x017b, "XG750 Plus Platinum" },
    };

    private static readonly ushort _vendorId = 0x2516;
    private readonly List<IHardware> _hardware;
    private readonly StringBuilder _report;

    public CoolerMasterPsuGroup(ISettings settings)
    {
        _report = new StringBuilder();
        _report.AppendLine("Cooler Master XG Plus series PSU Hardware");
        _report.AppendLine();

        _hardware = new List<IHardware>();
        foreach (HidDevice dev in DeviceList.Local.GetHidDevices(_vendorId).Where(d => _products.ContainsKey(d.ProductID) && UsbApi.IsVendorInterface(d)))
        {
            try
            {
                var device = new CoolerMasterPsu(dev, _products[dev.ProductID], settings, _hardware.Count);
                _hardware.Add(device);
                _report.AppendLine($"Device name: {device.Name}");
                _report.AppendLine($"Firmware version: {device.FirmwareVersion}");
                _report.AppendLine();
            }
            catch (Exception e)
            {
                _report.AppendLine($"Error while adding {_products[dev.ProductID]} ({dev.VendorID:X4}:{dev.ProductID:X4}): {e.GetType().Name}: {e.Message}");
                _report.AppendLine();
            }
        }

        if (_hardware.Count == 0)
        {
            _report.AppendLine("No Cooler Master PSU Hardware found.");
            _report.AppendLine();
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
