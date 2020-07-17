// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Text;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Controller.AquaComputer
{
    public class AquaComputerGroup : IGroup
    {
        private readonly List<IHardware> _hardware = new List<IHardware>();
        private readonly StringBuilder _report = new StringBuilder();

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
                    case 0xf0b6:
                    {
                        var device = new AquastreamXT(dev, settings);
                        _report.AppendLine($"Device name: {productName}");
                        _report.AppendLine($"Device variant: {device.Variant}");
                        _report.AppendLine($"Firmware version: {device.FirmwareVersion}");
                        _report.AppendLine($"{device.Status}");
                        _report.AppendLine();
                        _hardware.Add(device);
                        break;
                    }
                    case 0xf003:
                    {
                        var device = new MPS(dev, settings);
                        _report.AppendLine($"Device name: {productName}");
                        _report.AppendLine($"Firmware version: {device.FirmwareVersion}");
                        _report.AppendLine($"{device.Status}");
                        _report.AppendLine();
                        _hardware.Add(device);
                        break;
                    }
                    default:
                    {
                        _report.AppendLine($"Unknown Hardware PID: {dev.ProductID} Name: {productName}");
                        _report.AppendLine();
                        break;
                    }
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
}
