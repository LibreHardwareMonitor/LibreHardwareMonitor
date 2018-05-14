using HidLibrary;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenHardwareMonitor.Hardware.Aquacomputer
{
    class AquacomputerGroup : IGroup
    {
        private readonly List<IHardware> hardware = new List<IHardware>();
        private readonly StringBuilder report = new StringBuilder();
        public AquacomputerGroup(ISettings settings)
        {
            report.AppendLine($"Aquacomputer Hardware"); report.AppendLine();
            
            foreach (HidDevice dev in HidDevices.Enumerate(0x0c70))
            {
                switch (dev.Attributes.ProductId)
                {
                    case 0xf0b6:
                        {
                            var device = new AquastreamXT(dev, settings);
                            report.AppendLine($"Device name: {dev.ProductName}");
                            report.AppendLine($"Device variant: {device.Variant}");
                            report.AppendLine($"Firmware version: {device.FirmwareVersion}");
                            report.AppendLine($"{device.Status}");
                            report.AppendLine();
                            hardware.Add(device);
                            break;
                        }
                    default:
                        {
                            report.AppendLine($"Unknown Hardware PID: {dev.Attributes.ProductHexId} Name: {dev.ProductName}");report.AppendLine();
                            break;
                        }
                }
            }
            if (hardware.Count == 0)
            {
                report.AppendLine("No Aquacomputer Hardware found.");report.AppendLine();
            }
        }

        public IHardware[] Hardware => hardware.ToArray();

        public void Close()
        {
            foreach (Hardware h in hardware)
            {
                h.Close();
            }
        }

        public string GetReport()
        {
            return report.ToString();
        }
    }
}
