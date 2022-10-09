// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Text;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Controller.AeroCool;

public class AeroCoolGroup : IGroup
{
    private readonly List<IHardware> _hardware = new();
    private readonly StringBuilder _report = new();

    public AeroCoolGroup(ISettings settings)
    {
        _report.AppendLine("AeroCool Hardware");
        _report.AppendLine();

        foreach (HidDevice dev in DeviceList.Local.GetHidDevices(0x2E97))
        {
            int hubno = dev.ProductID - 0x1000;
            if (dev.DevicePath.Contains("mi_02") && hubno is >= 1 and <= 8)
            {
                var device = new P7H1(dev, settings);
                _report.AppendLine($"Device name: {device.Name}");
                _report.AppendLine($"HUB number: {device.HubNumber}");
                _report.AppendLine();
                _hardware.Add(device);
            }
        }

        if (_hardware.Count == 0)
        {
            _report.AppendLine("No AeroCool Hardware found.");
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