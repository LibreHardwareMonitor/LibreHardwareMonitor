// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) Florian K. (Blacktempel)
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Controller.MSI;

internal class MsiGroup : IGroup
{
    private readonly List<IHardware> _hardware = new();
    private readonly StringBuilder _report = new();

    public MsiGroup(ISettings settings)
    {
        _report.AppendLine("MSI Hardware:");
        _report.AppendLine();

        //Get all HID devices
        var hidDevices = DeviceList.Local.GetHidDevices();

        foreach (var hidDevice in hidDevices)
        {
            //Check if the device is in the supported devices list
            var found = MsiConstants.SupportedDevices.FirstOrDefault(md => md.VendorId == hidDevice.VendorID && md.ProductIdController == hidDevice.ProductID);

            if (found == null)
            {
                continue;
            }

            try
            {
                var coreLiquidController = new CoreLiquidController(found, hidDevice, settings);

                _hardware.Add(coreLiquidController);
                _report.AppendLine($"MSI Controller for '{hidDevice.GetProductName()}' ({hidDevice.VendorID:X4}:{hidDevice.ProductID:X4}) initialized successfully");
            }
            catch (Exception e)
            {
                _report.AppendLine($"Msi Controller Plugin initialization failed: {e.Message}");
            }
        }
    }

    public IReadOnlyList<IHardware> Hardware => _hardware;

    public void Close()
    {
        foreach (IHardware hw in _hardware)
        {
            if (hw is Hardware hardware)
            {
                hardware.Close();
            }
        }
    }

    public string GetReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine(_report.ToString());

        _hardware.ForEach(hw => sb.AppendLine(hw.GetReport()));

        return sb.ToString();
    }
}
