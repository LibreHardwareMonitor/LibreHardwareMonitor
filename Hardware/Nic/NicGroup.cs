using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;

namespace OpenHardwareMonitor.Hardware.Nic
{
    internal class NicGroup : IGroup
    {

        private List<Hardware> hardware = new List<Hardware>();
        private NetworkInterface[] _networkInterfaces;

        public NicGroup(ISettings settings)
        {
            int p = (int)Environment.OSVersion.Platform;
            if ((p == 4) || (p == 128))
            {
                hardware = new List<Hardware>();
                return;
            }
            _networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            for (int i = 0; i < _networkInterfaces.Length; i++)
            {
                if (_networkInterfaces[i].NetworkInterfaceType != NetworkInterfaceType.Unknown && _networkInterfaces[i].NetworkInterfaceType != NetworkInterfaceType.Loopback && _networkInterfaces[i].NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                {
                    hardware.Add(new Nic(_networkInterfaces[i].Name, settings, i, this));
                }
                
            }
                
        }

        public string GetReport()
        {
            if (NetworkInterfaces == null)
                return null;

            var report = new StringBuilder();

            foreach (Nic hw in hardware)
            {
                report.AppendLine(hw.NetworkInterface.Description);
                report.AppendLine(hw.NetworkInterface.OperationalStatus.ToString());
                report.AppendLine();
                foreach (var sensor in hw.Sensors)
                {
                    report.AppendLine(sensor.Name);
                    report.AppendLine(sensor.Value.ToString() + sensor.SensorType);
                    report.AppendLine();
                }
            }

            return report.ToString();
        }

        public IHardware[] Hardware
        {
            get
            {
                return hardware.ToArray();
            }
        }
        public NetworkInterface[] NetworkInterfaces
        {
            get
            {
                return _networkInterfaces;
            }
            set
            {
                _networkInterfaces = value;
            }
        }
        public void Close()
        {
            foreach (Hardware nic in hardware)
                nic.Close();
        }
    }
}
