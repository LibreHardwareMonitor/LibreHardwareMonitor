// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Text;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Controller.Nzxt;

internal class NzxtGroup : IGroup
{
    private readonly List<IHardware> _hardware = new();
    private readonly StringBuilder _report = new();

    public NzxtGroup(ISettings settings)
    {
        _report.AppendLine("Nzxt Hardware");
        _report.AppendLine();

        foreach (HidDevice dev in DeviceList.Local.GetHidDevices(0x1e71))
        {
            string productName = dev.GetProductName();

            switch (dev.ProductID)
            {
                case 0x2007:
                    var device = new KrakenX3(dev, settings);
                    _report.AppendLine($"Device name: {productName}");
                    _report.AppendLine($"Firmware version: {device.FirmwareVersion}");
                    _report.AppendLine($"{device.Status}");
                    _report.AppendLine();
                    _hardware.Add(device);
                    break;
                case 0x1711:
                    var gridv3 = new GridV3(dev, settings);
                    _report.AppendLine($"Device name: {productName}");
                    _report.AppendLine($"Firmware version: {gridv3.FirmwareVersion}");
                    _report.AppendLine();
                    _hardware.Add(gridv3);
                    break;
                case 0x3008: // NZXT KrakenZ Device
                    var krakenZ = new KrakenZ(dev, settings);
                    _report.AppendLine($"Device name: {productName}");
                    _report.AppendLine($"Firmware version: {krakenZ.FirmwareVersion}");
                    _report.AppendLine($"{krakenZ.Status}");
                    _report.AppendLine();
                    _hardware.Add(krakenZ);
                    break;

                default:
                    _report.AppendLine($"Unknown Hardware PID: {dev.ProductID} Name: {productName}");
                    _report.AppendLine();
                    break;
            }
        }

        if (_hardware.Count == 0)
        {
            _report.AppendLine("No Nzxt Hardware found.");
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
