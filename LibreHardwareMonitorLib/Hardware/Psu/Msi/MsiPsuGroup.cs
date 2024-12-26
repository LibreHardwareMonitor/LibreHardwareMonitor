// This Source Code Form is subject to the terms of the GNU Public License, v. 2.0.
// Copyright (C) 2024 demorfi<demorfi@gmail.com>
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Text;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Psu.Msi;

public class MsiPsuGroup : IGroup
{
    private readonly List<IHardware> _hardware = new();
    private readonly StringBuilder _report = new();

    public MsiPsuGroup(ISettings settings)
    {
        _report.AppendLine("MSI Ai series PSU Hardware");
        _report.AppendLine();

        foreach (HidDevice dev in DeviceList.Local.GetHidDevices(0x0db0))
        {
            switch (dev.ProductID)
            {
                case 0x56d4: // MSI PSUMEG Ai1300P
                    var device = new MsiPsu(dev, settings);
                    _report.AppendLine($"Device name: {device.Name}");
                    _report.AppendLine();
                    _hardware.Add(device);
                    break;

                default:
                    string productName = dev.GetProductName();
                    _report.AppendLine($"Unknown Hardware PID: {dev.ProductID} Name: {productName}");
                    _report.AppendLine();
                    break;
            }
        }

        if (_hardware.Count == 0)
        {
            _report.AppendLine("No MSI PSU Hardware found.");
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
