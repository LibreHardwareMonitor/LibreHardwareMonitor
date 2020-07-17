// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;

namespace LibreHardwareMonitor.Hardware.Network
{
    internal class NetworkGroup : IGroup
    {
        private readonly Dictionary<string, Network> _networks = new Dictionary<string, Network>();
        private readonly object _scanLock = new object();
        private readonly ISettings _settings;
        private List<Network> _hardware = new List<Network>();

        public NetworkGroup(ISettings settings)
        {
            _settings = settings;
            AddNetworkInterfaces(settings);

            NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAddressChanged;
        }

        public IReadOnlyList<IHardware> Hardware => _hardware;

        public string GetReport()
        {
            var report = new StringBuilder();

            foreach (Network hw in _hardware)
            {
                report.AppendLine(hw.NetworkInterface.Description);
                report.AppendLine(hw.NetworkInterface.OperationalStatus.ToString());
                report.AppendLine();

                foreach (ISensor sensor in hw.Sensors)
                {
                    report.AppendLine(sensor.Name);
                    report.AppendLine(sensor.Value.ToString());
                    report.AppendLine();
                }
            }

            return report.ToString();
        }

        public void Close()
        {
            NetworkChange.NetworkAddressChanged -= NetworkChange_NetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged -= NetworkChange_NetworkAddressChanged;

            foreach (Network nic in _hardware)
                nic.Close();
        }

        private void AddNetworkInterfaces(ISettings settings)
        {
            // If no network is marked up (excluding loopback and tunnel) then don't scan
            // for interfaces.
            if (!NetworkInterface.GetIsNetworkAvailable())
                return;


            // When multiple events fire concurrently, we don't want threads interfering
            // with others as they manipulate non-thread safe state.
            lock (_scanLock)
            {
                IOrderedEnumerable<NetworkInterface> networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                                                        .Where(DesiredNetworkType)
                                                        .OrderBy(x => x.Name);

                var scanned = networkInterfaces.ToDictionary(x => x.Id, x => x);
                IEnumerable<KeyValuePair<string, NetworkInterface>> newNetworkInterfaces = scanned.Where(x => !_networks.ContainsKey(x.Key));
                var removedNetworkInterfaces = _networks.Where(x => !scanned.ContainsKey(x.Key)).ToList();

                foreach (KeyValuePair<string, Network> nic in removedNetworkInterfaces)
                {
                    nic.Value.Close();
                    _networks.Remove(nic.Key);
                }

                foreach (KeyValuePair<string, NetworkInterface> nic in newNetworkInterfaces)
                {
                    _networks.Add(nic.Key, new Network(nic.Value, settings));
                }

                _hardware = _networks.Values.OrderBy(x => x.Name).ToList();
            }
        }

        private void NetworkChange_NetworkAddressChanged(object sender, System.EventArgs e)
        {
            AddNetworkInterfaces(_settings);
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
    }
}
