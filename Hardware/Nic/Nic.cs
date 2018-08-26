/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2012 Michael Möller <mmoeller@openhardwaremonitor.org>
	
*/

using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.NetworkInformation;

namespace OpenHardwareMonitor.Hardware.Nic
{
    internal class Nic : Hardware
    {
        private ISettings settings;
        private Sensor dataUploaded;
        private Sensor dataDownloaded;
        private Sensor uploadSpeed;
        private Sensor downloadSpeed;
        private Sensor networkUtilization;
        private NetworkInterface networkInterface;
        private int nicIndex;
        private DateTime latesTime;
        private DateTime presentBootTime;

        private long bytesUploaded;
        private long bytesDownloaded;
        private bool shouldTotalFlowUpdate = true;

        public Nic(string name, ISettings Settings, int index, NicGroup nicGroup)
          : base(name, new Identifier("NIC",index.ToString(CultureInfo.InvariantCulture)), Settings)
        {
            settings = Settings;
            nicIndex = index;
            networkInterface = nicGroup.NetworkInterfaces[index];
            presentBootTime = DateTime.Now.AddMilliseconds(-(double)Environment.TickCount);
            dataUploaded = new Sensor("Data Uploaded", 2, SensorType.Data, this,
              settings);
            ActivateSensor(dataUploaded);
            dataDownloaded = new Sensor("Data Downloaded", 3, SensorType.Data, this,
              settings);
            ActivateSensor(dataDownloaded);
            uploadSpeed = new Sensor("Upload Speed", 7, SensorType.Throughput, this,
              settings);
            ActivateSensor(uploadSpeed);
            downloadSpeed = new Sensor("Download Speed", 8, SensorType.Throughput, this,
              settings);
            ActivateSensor(downloadSpeed);
            networkUtilization = new Sensor("Network Utilization", 1, SensorType.Load, this,
              settings);
            ActivateSensor(networkUtilization);
            bytesUploaded = NetworkInterface.GetIPStatistics().BytesSent;
            bytesDownloaded = NetworkInterface.GetIPStatistics().BytesReceived;
            latesTime = DateTime.Now;
        }

        public override HardwareType HardwareType
        {
            get
            {
                return HardwareType.NIC;
            }
        }

        internal NetworkInterface NetworkInterface
        {
            get { return networkInterface; }
        }

        public override void Update()
        {
            DateTime newTime = DateTime.Now;
            float dt = (float)(newTime - latesTime).TotalSeconds;
            latesTime = newTime;
            IPv4InterfaceStatistics interfaceStats = NetworkInterface.GetIPv4Statistics();
            long dBytesUploaded = interfaceStats.BytesSent - bytesUploaded;
            long dBytesDownloaded = interfaceStats.BytesReceived - bytesDownloaded;
            uploadSpeed.Value = (float)dBytesUploaded / dt;
            downloadSpeed.Value = (float)dBytesDownloaded / dt;
            networkUtilization.Value = Clamp((float)Math.Max(dBytesUploaded, dBytesDownloaded) * 800 / NetworkInterface.Speed / dt, 0,100);
            bytesUploaded = interfaceStats.BytesSent;
            bytesDownloaded = interfaceStats.BytesReceived;
            dataUploaded.Value = ((float)bytesUploaded / 1073741824);
            dataDownloaded.Value = ((float)bytesDownloaded / 1073741824);
        }

        /// <summary>
        /// Clamps the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        /// <returns><see cref="System.Single"/>.</returns>
        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;

            if (value > max)
                return max;

            return value;
        }
    }
}
