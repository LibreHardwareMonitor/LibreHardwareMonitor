// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) 2012 Michael Möller <mmoeller@openhardwaremonitor.org>

using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;

namespace OpenHardwareMonitor.Hardware.Nic {
  internal class NicGroup : IGroup {
    private readonly ISettings _settings;
    private List<Nic> _hardware = new List<Nic>();
    private readonly object _scanLock = new object();
    private readonly Dictionary<string, Nic> _nics = new Dictionary<string, Nic>();

    public NicGroup(ISettings settings) {
      _settings = settings;
      ScanNics(settings);
      NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
      NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAddressChanged;
    }

    private void ScanNics(ISettings settings) {
      // If no network is marked up (excluding loopback and tunnel) then don't scan
      // for interfaces.
      if (!NetworkInterface.GetIsNetworkAvailable()) {
        return;
      }

      // When multiple events fire concurrently, we don't want threads interferring
      // with others as they manipulate non-thread safe state.
      lock (_scanLock) {
        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(DesiredNetworkType)
            .OrderBy(x => x.Name);

        var scanned = networkInterfaces.ToDictionary(x => x.Id, x => x);
        var newNics = scanned.Where(x => !_nics.ContainsKey(x.Key));
        var removedNics = _nics.Where(x => !scanned.ContainsKey(x.Key)).ToList();



        foreach (var nic in removedNics) {
          nic.Value.Close();
          _nics.Remove(nic.Key);
        }

        foreach (var nic in newNics) {
          _nics.Add(nic.Key, new Nic(nic.Value, settings));
        }

        _hardware = _nics.Values.OrderBy(x => x.Name).ToList();
      }
    }

    private void NetworkChange_NetworkAddressChanged(object sender, System.EventArgs e) {
      ScanNics(_settings);
    }

    private static bool DesiredNetworkType(NetworkInterface nic) {
      switch (nic.NetworkInterfaceType) {
        case NetworkInterfaceType.Loopback:
        case NetworkInterfaceType.Tunnel:
        case NetworkInterfaceType.Unknown:
          return false;
        default:
          return true;
      }
    }

    public string GetReport() {
      var report = new StringBuilder();

      foreach (Nic hw in _hardware) {
        report.AppendLine(hw.NetworkInterface.Description);
        report.AppendLine(hw.NetworkInterface.OperationalStatus.ToString());
        report.AppendLine();
        foreach (var sensor in hw.Sensors) {
          report.AppendLine(sensor.Name);
          report.AppendLine(sensor.Value.ToString() + sensor.SensorType);
          report.AppendLine();
        }
      }

      return report.ToString();
    }

    public IEnumerable<IHardware> Hardware {
      get {
        return _hardware;
      }
    }

    public void Close() {
      NetworkChange.NetworkAddressChanged -= NetworkChange_NetworkAddressChanged;
      NetworkChange.NetworkAvailabilityChanged -= NetworkChange_NetworkAddressChanged;
      foreach (var nic in _hardware)
        nic.Close();
    }
  }
}