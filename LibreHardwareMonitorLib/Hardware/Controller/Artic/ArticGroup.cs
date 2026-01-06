using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Controller.Artic
{
    internal class ArticGroup : IGroup
    {
        private readonly List<IHardware> _hardware = new();
        private readonly StringBuilder _report = new();

        private const int VendorId = 0x3904;
        private const int ProductId = 0xF001;

        public ArticGroup(ISettings settings)
        {
            _report.AppendLine("Artic Hardware");
            _report.AppendLine();
            try
            {
                var devices = DeviceList.Local.GetHidDevices(VendorId, ProductId);
                var hidDevice = devices.FirstOrDefault();

                if (hidDevice != null)
                {
                    _hardware.Add(new ArticFanController(hidDevice, settings));
                    _report.AppendLine("Arctic Fan Controller initialized successfully");
                }
            }
            catch (Exception ex)
            {
                _report.AppendLine($"Arctic Fan Controller Plugin initialization failed: {ex.Message}");
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
