// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace LibreHardwareMonitor.Hardware.Network;

internal sealed class Network : Hardware
{
    private readonly Sensor _dataDownloaded;
    private readonly Sensor _dataUploaded;
    private readonly Sensor _downloadSpeed;
    private readonly Sensor _networkUtilization;
    private readonly Sensor _uploadSpeed;
    private long _bytesDownloaded;
    private long _bytesUploaded;
    private long _lastTick;

    public Network(NetworkInterface networkInterface, ISettings settings)
        : base(networkInterface.Name, new Identifier("nic", networkInterface.Id), settings)
    {
        NetworkInterface = networkInterface;
        _dataUploaded = new Sensor("Data Uploaded", 2, SensorType.Data, this, settings);
        ActivateSensor(_dataUploaded);
        _dataDownloaded = new Sensor("Data Downloaded", 3, SensorType.Data, this, settings);
        ActivateSensor(_dataDownloaded);
        _uploadSpeed = new Sensor("Upload Speed", 7, SensorType.Throughput, this, settings);
        ActivateSensor(_uploadSpeed);
        _downloadSpeed = new Sensor("Download Speed", 8, SensorType.Throughput, this, settings);
        ActivateSensor(_downloadSpeed);
        _networkUtilization = new Sensor("Network Utilization", 1, SensorType.Load, this, settings);
        ActivateSensor(_networkUtilization);
        _bytesUploaded = NetworkInterface.GetIPStatistics().BytesSent;
        _bytesDownloaded = NetworkInterface.GetIPStatistics().BytesReceived;
        _lastTick = Stopwatch.GetTimestamp();
    }

    public override HardwareType HardwareType
    {
        get { return HardwareType.Network; }
    }

    internal NetworkInterface NetworkInterface { get; private set; }

    public override void Update()
    {
        try
        {
            if (NetworkInterface == null)
                return;

            long newTick = Stopwatch.GetTimestamp();
            double dt = new TimeSpan(newTick - _lastTick).TotalSeconds;

            IPv4InterfaceStatistics interfaceStats = NetworkInterface.GetIPv4Statistics();

            // Report out the number of GB (2^30 Bytes) that this interface has up/downloaded. Note
            // that these values can reset back at zero (eg: after waking from sleep).
            _dataUploaded.Value = (float)(interfaceStats.BytesSent / (double)0x40000000);
            _dataDownloaded.Value = (float)(interfaceStats.BytesReceived / (double)0x40000000);

            // Detect a reset in interface stats if the new total is less than what was previously
            // seen. While setting the previous values to zero doesn't encapsulate the value the
            // instant before the reset, it is the best approximation we have.
            if (interfaceStats.BytesSent < _bytesUploaded || interfaceStats.BytesReceived < _bytesDownloaded)
            {
                _bytesUploaded = 0;
                _bytesDownloaded = 0;
            }

            long dBytesUploaded = interfaceStats.BytesSent - _bytesUploaded;
            long dBytesDownloaded = interfaceStats.BytesReceived - _bytesDownloaded;

            // Upload and download speeds are reported as the number of bytes transfered over the
            // time difference since the last report. In this way, the values represent the average
            // number of bytes up/downloaded in a second.
            _uploadSpeed.Value = (float)(dBytesUploaded / dt);
            _downloadSpeed.Value = (float)(dBytesDownloaded / dt);

            // Network speed is in bits per second, so when calculating the load on the NIC we first
            // grab the total number of bits up/downloaded
            long dbits = (dBytesUploaded + dBytesDownloaded) * 8;

            // Converts the ratio of total bits transferred over time over theoretical max bits
            // transfer rate into a percentage load
            double load = (dbits / dt / NetworkInterface.Speed) * 100;

            // Finally clamp the value between 0% and 100% to avoid reporting nonsensical numbers
            _networkUtilization.Value = (float)Math.Min(Math.Max(load, 0), 100);

            // Store the recorded values and time, so they can be used in the next update
            _bytesUploaded = interfaceStats.BytesSent;
            _bytesDownloaded = interfaceStats.BytesReceived;
            _lastTick = newTick;
        }
        catch (NetworkInformationException networkInformationException) when (unchecked(networkInformationException.HResult == (int)0x80004005))
        {
            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.Id.Equals(NetworkInterface?.Id))
                {
                    NetworkInterface = networkInterface;
                    break;
                }
            }
        }
    }
}