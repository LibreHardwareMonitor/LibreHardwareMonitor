// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
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
                    TryAddDevice(AddD5Next, dev, settings, productName);
                    break;

                case 0xf0b6:
                    TryAddDevice(AddAquastreamXT, dev, settings, productName);
                    break;

                case 0xf003:
                    TryAddDevice(AddMPS, dev, settings, productName);
                    break;
                    
                case 0xF00D:
                    TryAddDevice(AddQuadro, dev, settings, productName);
                    break;
                    
                case 0xF00B:
                    TryAddDevice(AddAquastreamUltimate, dev, settings, productName);
                    break;
                    
                case 0xF011:
                    TryAddDevice(AddOcto, dev, settings, productName);
                    break;

                case 0xF00A:
                    TryAddDevice(AddFarbwerk, dev, settings, productName);
                    break;
                
                case 0xF010:
                    TryAddDevice(AddFarbwerk360, dev, settings, productName);
                    break;

                case 0xF012:
                    TryAddDevice(AddHighFlowNext, dev, settings, productName);
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

    private void TryAddDevice(Func<HidDevice, ISettings, string, IHardware> createDevice, HidDevice dev, ISettings settings, string productName)
    {
        try
        {
            IHardware hardware = createDevice(dev, settings, productName);
            _hardware.Add(hardware);
        }
        catch (Exception e)
        {
            _report.AppendLine($"Error while adding {productName}: {e.Message}");
            _report.AppendLine();
        }
    }

    private IHardware AddD5Next(HidDevice dev, ISettings settings, string productName)
    {
        var d5Next = new D5Next(dev, settings);
        _report.AppendLine($"Device name: {productName}");
        _report.AppendLine($"Firmware version: {d5Next.FirmwareVersion}");
        _report.AppendLine();

        return d5Next;
    }

    private IHardware AddAquastreamXT(HidDevice dev, ISettings settings, string productName)
    {
        var aquastreamXt = new AquastreamXT(dev, settings);
        _report.AppendLine($"Device name: {productName}");
        _report.AppendLine($"Device variant: {aquastreamXt.Variant}");
        _report.AppendLine($"Firmware version: {aquastreamXt.FirmwareVersion}");
        _report.AppendLine($"{aquastreamXt.Status}");
        _report.AppendLine();

        return aquastreamXt;
    }

    private IHardware AddMPS(HidDevice dev, ISettings settings, string productName)
    {
        var mps = new MPS(dev, settings);
        _report.AppendLine($"Device name: {productName}");
        _report.AppendLine($"Firmware version: {mps.FirmwareVersion}");
        _report.AppendLine($"{mps.Status}");
        _report.AppendLine();

        return mps;
    }

    private IHardware AddQuadro(HidDevice dev, ISettings settings, string productName)
    {
        var quadro = new Quadro(dev, settings);
        _report.AppendLine($"Device name: {productName}");
        _report.AppendLine($"Firmware version: {quadro.FirmwareVersion}");
        _report.AppendLine();

        return quadro;
    }

    private IHardware AddAquastreamUltimate(HidDevice dev, ISettings settings, string productName)
    {
        var aquastreamUltimate = new AquastreamUltimate(dev, settings);
        _report.AppendLine($"Device name: {productName}");
        _report.AppendLine($"Firmware version: {aquastreamUltimate.FirmwareVersion}");
        _report.AppendLine();

        return aquastreamUltimate;
    }

    private IHardware AddOcto(HidDevice dev, ISettings settings, string productName)
    {
        var octo = new Octo(dev, settings);
        _report.AppendLine($"Device name: {productName}");
        _report.AppendLine($"Firmware version: {octo.FirmwareVersion}");
        _report.AppendLine();

        return octo;
    }

    private IHardware AddFarbwerk(HidDevice dev, ISettings settings, string productName)
    {
        var farbwerk = new Farbwerk(dev, settings);
        _report.AppendLine($"Device name: {productName}");
        _report.AppendLine($"Firmware version: {farbwerk.FirmwareVersion}");
        _report.AppendLine($"{farbwerk.Status}");
        _report.AppendLine();

        return farbwerk;
    }

    private IHardware AddFarbwerk360(HidDevice dev, ISettings settings, string productName)
    {
        var farbwerk360 = new Farbwerk360(dev, settings);
        _report.AppendLine($"Device name: {productName}");
        _report.AppendLine($"Firmware version: {farbwerk360.FirmwareVersion}");
        _report.AppendLine($"{farbwerk360.Status}");
        _report.AppendLine();

        return farbwerk360;
    }

    private IHardware AddHighFlowNext(HidDevice dev, ISettings settings, string productName)
    {
        var highflownext = new HighFlowNext(dev, settings);
        _report.AppendLine($"Device name: {productName}");
        _report.AppendLine($"Firmware version: {highflownext.FirmwareVersion}");
        _report.AppendLine();

        return highflownext;
    }
}
