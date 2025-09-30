// This Source Code Form is subject to the terms of the GNU Public License, v. 2.0.
// Copyright (C) 2024 demorfi<demorfi@gmail.com>
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Psu.Msi;

public class MsiPsuGroup : IGroup
{
    private static readonly int[] _productIds =
    {
        0x56d4, // MEG Ai1300P
    };

    private static readonly ushort _vendorId = 0x0db0;
    private readonly List<IHardware> _hardware;
    private readonly StringBuilder _report;

    public MsiPsuGroup(ISettings settings)
    {
        _report = new StringBuilder();
        _report.AppendLine("MSI Ai series PSU Hardware");
        _report.AppendLine();

        _hardware = new List<IHardware>();
        foreach (HidDevice dev in DeviceList.Local.GetHidDevices(_vendorId))
        {
            if (_productIds.Contains(dev.ProductID))
            {
                var device = new MsiPsu(dev, settings, _hardware.Count);
                _hardware.Add(device);
                _report.AppendLine($"Device name: {device.Name}");
                _report.AppendLine();
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
