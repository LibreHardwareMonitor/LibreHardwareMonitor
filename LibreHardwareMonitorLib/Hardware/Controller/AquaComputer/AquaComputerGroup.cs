// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Text;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Controller.AquaComputer;

public class AquaComputerGroup : IGroup
{
    private readonly List<IHardware> _hardware = new();
    private readonly StringBuilder _report = new();

    public AquaComputerGroup(ISettings settings)
    {
        _report.AppendLine("AquaComputer Hardware");
        _report.AppendLine();

        foreach (HidDevice dev in DeviceList.Local.GetHidDevices(0x0c70))
        {
            string productName = dev.GetProductName();
            productName = productName.Substring(0, 1).ToUpper() + productName.Substring(1);

            switch (dev.ProductID)
            {
                case 0xF00E:
                    var d5Next = new D5Next(dev, settings);
                    _report.AppendLine($"Device name: {productName}");
                    _report.AppendLine($"Firmware version: {d5Next.FirmwareVersion}");
                    _report.AppendLine();
                    _hardware.Add(d5Next);
                    break;

                case 0xf0b6:
                    var aquastreamXt = new AquastreamXT(dev, settings);
                    _report.AppendLine($"Device name: {productName}");
                    _report.AppendLine($"Device variant: {aquastreamXt.Variant}");
                    _report.AppendLine($"Firmware version: {aquastreamXt.FirmwareVersion}");
                    _report.AppendLine($"{aquastreamXt.Status}");
                    _report.AppendLine();
                    _hardware.Add(aquastreamXt);
                    break;

                case 0xf003:
                    var mps = new MPS(dev, settings);
                    _report.AppendLine($"Device name: {productName}");
                    _report.AppendLine($"Firmware version: {mps.FirmwareVersion}");
                    _report.AppendLine($"{mps.Status}");
                    _report.AppendLine();
                    _hardware.Add(mps);
                    break;
                    
                case 0xF00D:
                    var quadro = new Quadro(dev, settings);
                    _report.AppendLine($"Device name: {productName}");
                    _report.AppendLine($"Firmware version: {quadro.FirmwareVersion}");
                    _report.AppendLine();
                    _hardware.Add(quadro);
                    break;
                    
                case 0xF011:
                    var octo = new Octo(dev, settings);
                    _report.AppendLine($"Device name: {productName}");
                    _report.AppendLine($"Firmware version: {octo.FirmwareVersion}");
                    _report.AppendLine();
                    _hardware.Add(octo);
                    break;

                case 0xF00A:
                    var farbwerk = new Farbwerk(dev, settings);
                    _report.AppendLine($"Device name: {productName}");
                    _report.AppendLine($"Firmware version: {farbwerk.FirmwareVersion}");
                    _report.AppendLine($"{farbwerk.Status}");
                    _report.AppendLine();
                    _hardware.Add(farbwerk);
                    break;

                default:
                    _report.AppendLine($"Unknown Hardware PID: {dev.ProductID} Name: {productName}");
                    _report.AppendLine();
                    break;
            }
        }

        if (_hardware.Count == 0)
        {
            _report.AppendLine("No AquaComputer Hardware found.");
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
