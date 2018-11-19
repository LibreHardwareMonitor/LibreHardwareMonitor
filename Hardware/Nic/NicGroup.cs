using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;

namespace OpenHardwareMonitor.Hardware.Nic
{
    internal class NicGroup : IGroup
    {
        private readonly ISettings _settings;
        private List<Nic> _hardware = new List<Nic>();

        public NicGroup(ISettings settings)
        {
            _settings = settings;
            ScanNics(settings);
            NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged; 
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAddressChanged;
        }

        private void ScanNics(ISettings settings)
        {
            NetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            _hardware = NetworkInterfaces.Where(DesiredNetworkType)
                .Select((x, i) => new Nic(x, settings, i))
                .ToList();
        }

        private void NetworkChange_NetworkAddressChanged(object sender, System.EventArgs e)
        {
            ScanNics(_settings);
        }

        private static bool DesiredNetworkType(NetworkInterface nic)
        {
            switch (nic.NetworkInterfaceType)
            {
                case NetworkInterfaceType.Loopback:
                case NetworkInterfaceType.Tunnel:
                case NetworkInterfaceType.Unknown:
                    return false;
                default:
                    return true;
            }
        }

        public string GetReport()
        {
            if (NetworkInterfaces == null)
                return null;

            var report = new StringBuilder();

            foreach (Nic hw in _hardware)
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

        public IEnumerable<IHardware> Hardware
        {
            get
            {
                return _hardware;
            }
        }

        public NetworkInterface[] NetworkInterfaces { get; set; }

        public void Close()
        {
            NetworkChange.NetworkAddressChanged -= NetworkChange_NetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged -= NetworkChange_NetworkAddressChanged;
            foreach (var nic in _hardware)
                nic.Close();
        }
    }
}
