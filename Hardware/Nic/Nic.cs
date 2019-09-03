// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) 2012 Michael Möller <mmoeller@openhardwaremonitor.org>

using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.NetworkInformation;

namespace OpenHardwareMonitor.Hardware.Nic {
  internal class Nic : Hardware {
    private Sensor dataUploaded;
    private Sensor dataDownloaded;
    private Sensor uploadSpeed;
    private Sensor downloadSpeed;
    private Sensor networkUtilization;
    private long lastTick;

    private long bytesUploaded;
    private long bytesDownloaded;
    private readonly NetworkInterface networkInterface;

    public Nic(NetworkInterface networkInterface, ISettings settings)
      : base(networkInterface.Name, new Identifier("nic", networkInterface.Id), settings) {
      this.networkInterface = networkInterface;
      dataUploaded = new Sensor("Data Uploaded", 2, SensorType.Data, this, settings);
      ActivateSensor(dataUploaded);
      dataDownloaded = new Sensor("Data Downloaded", 3, SensorType.Data, this, settings);
      ActivateSensor(dataDownloaded);
      uploadSpeed = new Sensor("Upload Speed", 7, SensorType.Throughput, this, settings);
      ActivateSensor(uploadSpeed);
      downloadSpeed = new Sensor("Download Speed", 8, SensorType.Throughput, this, settings);
      ActivateSensor(downloadSpeed);
      networkUtilization = new Sensor("Network Utilization", 1, SensorType.Load, this, settings);
      ActivateSensor(networkUtilization);
      bytesUploaded = NetworkInterface.GetIPStatistics().BytesSent;
      bytesDownloaded = NetworkInterface.GetIPStatistics().BytesReceived;
      lastTick = Stopwatch.GetTimestamp();
    }

    public override HardwareType HardwareType {
      get {
        return HardwareType.NIC;
      }
    }

    internal NetworkInterface NetworkInterface {
      get {
        return networkInterface;
      }
    }

    public override void Update() {
      long newTick = Stopwatch.GetTimestamp();
      double dt = new TimeSpan(newTick - lastTick).TotalSeconds;

      IPv4InterfaceStatistics interfaceStats = NetworkInterface.GetIPv4Statistics();

      // Report out the number of GB (2^30 Bytes) that this interface has up/downloaded. Note
      // that these values can reset back at zero (eg: after waking from sleep).
      dataUploaded.Value = (float)(interfaceStats.BytesSent / (double)0x40000000);
      dataDownloaded.Value = (float)(interfaceStats.BytesReceived / (double)0x40000000);

      // Detect a reset in interface stats if the new total is less than what was previously
      // seen. While setting the previous values to zero doesn't encapsulate the value the
      // instant before the reset, it is the best approximation we have.
      if (interfaceStats.BytesSent < bytesUploaded || interfaceStats.BytesReceived < bytesDownloaded) {
        bytesUploaded = 0;
        bytesDownloaded = 0;
      }

      long dBytesUploaded = interfaceStats.BytesSent - bytesUploaded;
      long dBytesDownloaded = interfaceStats.BytesReceived - bytesDownloaded;

      // Upload and download speeds are reported as the number of bytes transfered over the
      // time difference since the last report. In this way, the values represent the average
      // number of bytes up/downloaded in a second.
      uploadSpeed.Value = (float)(dBytesUploaded / dt);
      downloadSpeed.Value = (float)(dBytesDownloaded / dt);

      // Network speed is in bits per second, so when calculating the load on the NIC we first
      // grab the total number of bits up/downloaded
      long dbits = (dBytesUploaded + dBytesDownloaded) * 8;

      // Converts the ratio of total bits transferred over time over theoretical max bits
      // transfer rate into a percentage load
      double load = (dbits / dt / NetworkInterface.Speed) * 100;

      // Finally clamp the value between 0% and 100% to avoid reporting nonsensical numbers
      networkUtilization.Value = (float)Math.Min(Math.Max(load, 0), 100);

      // Store the recorded values and time, so they can be used in the next update
      bytesUploaded = interfaceStats.BytesSent;
      bytesDownloaded = interfaceStats.BytesReceived;
      lastTick = newTick;
    }
  }
}