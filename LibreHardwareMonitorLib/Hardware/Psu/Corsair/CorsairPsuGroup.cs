// This Source Code Form is subject to the terms of the GNU Public License, v. 2.0.
// Copyright(C) 2020 Wilken Gottwalt<wilken.gottwalt@posteo.net>
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.
// Implemented after the Linuix kernel driver corsair_psu by Wilken Gottwalt and contributers

using System.Collections.Generic;
using System.Linq;
using System.Text;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Psu.Corsair;

public class CorsairPsuGroup : IGroup
{
    private static readonly int[] _productIds =
    {
        0x1c03, // HX550i
        0x1c04, // HX650i
        0x1c05, // HX750i
        0x1c06, // HX850i
        0x1c07, // HX1000i
        0x1c1e, // HX1000i REV2
        0x1c08, // HX1200i
        0x1c1f, // HX1500i

        0x1c09, // RM550i
        0x1c0a, // RM650i
        0x1c0b, // RM750i
        0x1c0c, // RM850i
        0x1c0d, // RM1000i

        // 0x1c11, // AX1600i
    };

    private static readonly ushort _vendorId = 0x1b1c;
    private readonly List<IHardware> _hardware;
    private readonly StringBuilder _report;

    public CorsairPsuGroup(ISettings settings)
    {
        _report = new StringBuilder();
        _report.AppendLine("Corsair HXi/RMi series PSU Hardware");
        _report.AppendLine();

        _hardware = new List<IHardware>();
        foreach (HidDevice dev in DeviceList.Local.GetHidDevices(_vendorId))
        {
            if (_productIds.Contains(dev.ProductID))
            {
                var device = new CorsairPsu(dev, settings, _hardware.Count);
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
