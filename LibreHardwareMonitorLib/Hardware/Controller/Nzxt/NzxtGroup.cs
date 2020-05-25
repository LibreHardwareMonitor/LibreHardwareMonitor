using HidSharp;
using System.Collections.Generic;
using System.Text;

namespace LibreHardwareMonitor.Hardware.Controller.Nzxt
{
    class NzxtGroup : IGroup
    {
        private readonly List<IHardware> _hardware = new List<IHardware>();
        private readonly StringBuilder _report = new StringBuilder();

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
                    {
                        var device = new KrakenX3(dev, settings);
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
                _report.AppendLine("No Nzxt Hardware found.");
                _report.AppendLine();
            }
        }

        public IEnumerable<IHardware> Hardware => _hardware;

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
