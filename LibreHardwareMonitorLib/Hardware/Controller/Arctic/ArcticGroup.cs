// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Controller.Arctic;

internal class ArcticGroup : IGroup
{
    private readonly List<IHardware> _hardware = new();
    private readonly StringBuilder _report = new();

    private const int VendorId = 0x3904;
    private const int ProductId = 0xF001;

    public ArcticGroup(ISettings settings)
    {
        _report.AppendLine("ARCTIC Hardware");
        _report.AppendLine();
        try
        {
            IEnumerable<HidDevice> devices = DeviceList.Local.GetHidDevices(VendorId, ProductId);

            foreach (HidDevice device in devices)
            {
                _hardware.Add(new ArcticFanController(device, settings));
                _report.AppendLine($"ARCTIC Fan Controller {device.DevicePath} initialized successfully");
            }
        }
        catch (Exception ex)
        {
            _report.AppendLine($"ARCTIC Fan Controller Plugin initialization failed: {ex.Message}");
        }
    }

    public IReadOnlyList<IHardware> Hardware => _hardware;

    public void Close()
    {
        foreach (IHardware iHardware in _hardware)
        {
            if (iHardware is Hardware hardware)
            {
                hardware.Close();
            }
        }
    }

    public string GetReport()
    {
        return _report.ToString();
    }
}
