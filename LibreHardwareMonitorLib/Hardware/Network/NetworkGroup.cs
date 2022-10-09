// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;

namespace LibreHardwareMonitor.Hardware.Network;

internal class NetworkGroup : IGroup, IHardwareChanged
{
    public event HardwareEventHandler HardwareAdded;
    public event HardwareEventHandler HardwareRemoved;

    private readonly Dictionary<string, Network> _networks = new();
    private readonly object _scanLock = new();
    private readonly ISettings _settings;
    private readonly List<Network> _hardware = new();

    public NetworkGroup(ISettings settings)
    {
        _settings = settings;
        UpdateNetworkInterfaces(settings);

        NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
        NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAddressChanged;
    }

    public IReadOnlyList<IHardware> Hardware => _hardware;

    public string GetReport()
    {
        var report = new StringBuilder();

        foreach (Network network in _hardware)
        {
            report.AppendLine(network.NetworkInterface.Description);
            report.AppendLine(network.NetworkInterface.OperationalStatus.ToString());
            report.AppendLine();

            foreach (ISensor sensor in network.Sensors)
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

        foreach (Network network in _hardware)
            network.Close();
    }

    private void UpdateNetworkInterfaces(ISettings settings)
    {
        // When multiple events fire concurrently, we don't want threads interfering
        // with others as they manipulate non-thread safe state.
        lock (_scanLock)
        {
            IOrderedEnumerable<NetworkInterface> networkInterfaces = GetNetworkInterfaces();
            if (networkInterfaces == null)
                return;

            var foundNetworkInterfaces = networkInterfaces.ToDictionary(x => x.Id, x => x);

            // Remove network interfaces that no longer exist.
            List<string> removeKeys = new();
            foreach (KeyValuePair<string, Network> networkInterfacePair in _networks)
            {
                if (foundNetworkInterfaces.ContainsKey(networkInterfacePair.Key))
                    continue;

                removeKeys.Add(networkInterfacePair.Key);
            }

            foreach (string key in removeKeys)
            {
                Network network = _networks[key];
                network.Close();
                _networks.Remove(key);

                _hardware.Remove(network);
                HardwareRemoved?.Invoke(network);
            }

            // Add new network interfaces.
            foreach (KeyValuePair<string, NetworkInterface> networkInterfacePair in foundNetworkInterfaces)
            {
                if (!_networks.ContainsKey(networkInterfacePair.Key))
                {
                    _networks.Add(networkInterfacePair.Key, new Network(networkInterfacePair.Value, settings));
                    _hardware.Add(_networks[networkInterfacePair.Key]);
                    HardwareAdded?.Invoke(_networks[networkInterfacePair.Key]);
                }
            }
        }
    }

    private static IOrderedEnumerable<NetworkInterface> GetNetworkInterfaces()
    {
        int retry = 0;

        while (retry++ < 5)
        {
            try
            {
                return NetworkInterface.GetAllNetworkInterfaces()
                                       .Where(DesiredNetworkType)
                                       .OrderBy(x => x.Name);
            }
            catch (NetworkInformationException)
            {
                // Disabling IPv4 while running can cause a NetworkInformationException: The pipe is being closed.
                // This can be retried.
            }
        }

        return null;
    }

    private void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
    {
        UpdateNetworkInterfaces(_settings);
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
