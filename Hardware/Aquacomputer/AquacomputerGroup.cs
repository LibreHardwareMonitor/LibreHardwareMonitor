using HidLibrary;
using System.Collections.Generic;
using System.Text;

namespace OpenHardwareMonitor.Hardware.Aquacomputer
{
    public class AquacomputerGroup : IGroup
    {
        private readonly List<IHardware> _hardware = new List<IHardware>();
        private readonly StringBuilder _report = new StringBuilder();

        public AquacomputerGroup(ISettings settings)
        {
            _report.AppendLine($"Aquacomputer Hardware");
            _report.AppendLine();
            
            foreach (HidDevice dev in HidDevices.Enumerate(0x0c70))
            {
                byte[] productNameBytes;
                dev.ReadProduct(out productNameBytes);
                string ProductName = UnicodeEncoding.Unicode.GetString(productNameBytes).Replace("\0", string.Empty);
                ProductName = ProductName.Substring(0,1).ToUpper() + ProductName.Substring(1);

                switch (dev.Attributes.ProductId)
                {
                    case 0xf0b6:
                        {
                            var device = new AquastreamXT(dev, settings);
                            _report.AppendLine($"Device name: {ProductName}");
                            _report.AppendLine($"Device variant: {device.Variant}");
                            _report.AppendLine($"Firmware version: {device.FirmwareVersion}");
                            _report.AppendLine($"{device.Status}");
                            _report.AppendLine();
                            _hardware.Add(device);
                            break;
                        }
                    default:
                        {
                            _report.AppendLine($"Unknown Hardware PID: {dev.Attributes.ProductHexId} Name: {ProductName}");
                            _report.AppendLine();
                            break;
                        }
                }
            }
            if (_hardware.Count == 0)
            {
                _report.AppendLine("No Aquacomputer Hardware found.");
                _report.AppendLine();
            }
        }

        public IEnumerable<IHardware> Hardware => _hardware;

        public void Close()
        {
            foreach (Hardware h in _hardware)
            {
                h.Close();
            }
        }

        public string GetReport()
        {
            return _report.ToString();
        }
    }
}
