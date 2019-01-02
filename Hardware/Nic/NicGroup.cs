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
        private readonly object _scanLock = new object();
        private readonly Dictionary<string, Nic> _nics = new Dictionary<string, Nic>();

        public NicGroup(ISettings settings)
        {
            _settings = settings;
            ScanNics(settings);
            NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged; 
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAddressChanged;
        }

        private void ScanNics(ISettings settings)
        {
            // If no network is marked up (excluding loopback and tunnel) then don't scan
            // for interfaces.
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                return;
            }

            // When multiple events fire concurrently, we don't want threads interferring
            // with others as they manipulate non-thread safe state.
            lock (_scanLock)
            {
                NetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(DesiredNetworkType)
                    .OrderBy(x => x.Name)
                    .ToArray();

                var scanned = NetworkInterfaces.ToDictionary(x => x.Id, x => x);
                var newNics = scanned.Where(x => !_nics.ContainsKey(x.Key));
                var removedNics = _nics.Where(x => !  scanned.ContainsKey(x.Key)).ToList();

                foreach (var nic in removedNics)
                {
                    nic.Value.Close();
                    _nics.Remove(nic.Key);
                }

                foreach (var nic in newNics.Select((x, i) => new { Id = x.Key, Nic = new Nic(x.Value, settings, i) }))
                {
                    _nics.Add(nic.Id, nic.Nic);
                }

                _hardware = _nics.Values.OrderBy(x => x.Name).ToList();
            }
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
