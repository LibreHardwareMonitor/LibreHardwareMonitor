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
        private Sensor connectionSpeed;
        private Sensor dataUploaded;
        private Sensor dataDownloaded;
        private Sensor uploadSpeed;
        private Sensor downloadSpeed;
        private Sensor networkUtilization;
        private Sensor totalDataDownloaded;
        private Sensor totalDataUploaded;
        private Sensor totalDataFlowed;
        private NetworkInterface nic;
        private int nicIndex;
        private DateTime latesTime;
        private DateTime presentBootTime;

        private long bytesUploaded;
        private long bytesDownloaded;
        private long totalBytesDownloaded;
        private long totalBytesUploaded;
        private bool shouldTotalFlowUpdate = true;

        public Nic(string name, ISettings Settings, int index, NicGroup nicGroup)
          : base(name, new Identifier("NIC",index.ToString(CultureInfo.InvariantCulture)), Settings)
        {
            settings = Settings;
            nicIndex = index;
            nic = nicGroup.NicArr[index];
            totalBytesDownloaded = Convert.ToInt64(settings.GetValue("TotalDownloadedBeforeLastBoot"+nic.Name, "-1"));
            totalBytesUploaded = Convert.ToInt64(settings.GetValue("TotalUploadedBeforeLastBoot" + nic.Name, "-1"));
            if (totalBytesDownloaded == -1)
            {
                settings.SetValue("TotalDownloadedBeforeLastBoot"+nic.Name, "0");
                settings.SetValue("TotalUploadedBeforeLastBoot" + nic.Name, "0");
                totalBytesDownloaded = 0;
                totalBytesUploaded = 0;
            }
            presentBootTime = DateTime.Now.AddMilliseconds(-(double)Environment.TickCount);
            string lastBootTime = settings.GetValue("lastBootTime" + nic.Name, "-1");
            if(lastBootTime == string.Format("{0:g}", presentBootTime))
            {
                    shouldTotalFlowUpdate = false;
            }
            connectionSpeed = new Sensor("Connection Speed", 0, SensorType.Throughput, this,
              settings);
            ActivateSensor(connectionSpeed);
            dataUploaded = new Sensor("Data Uploaded", 2, SensorType.Data, this,
              settings);
            ActivateSensor(dataUploaded);
            dataDownloaded = new Sensor("Data Downloaded", 3, SensorType.Data, this,
              settings);
            ActivateSensor(dataDownloaded);
            totalDataDownloaded = new Sensor("Total Data Downloaded", 5, SensorType.Data, this,
              settings);
            ActivateSensor(totalDataDownloaded);
            totalDataUploaded = new Sensor("Total Data Uploaded", 4, SensorType.Data, this,
              settings);
            ActivateSensor(totalDataUploaded);
            totalDataFlowed = new Sensor("Total Data Flowed", 6, SensorType.Data, this,
              settings);
            ActivateSensor(totalDataFlowed);
            uploadSpeed = new Sensor("Upload Speed", 7, SensorType.Throughput, this,
              settings);
            ActivateSensor(uploadSpeed);
            downloadSpeed = new Sensor("Download Speed", 8, SensorType.Throughput, this,
              settings);
            ActivateSensor(downloadSpeed);
            networkUtilization = new Sensor("Network Utilization", 1, SensorType.Load, this,
              settings);
            ActivateSensor(networkUtilization);
            bytesUploaded = nic.GetIPStatistics().BytesSent;
            bytesDownloaded = nic.GetIPStatistics().BytesReceived;
            latesTime = DateTime.Now;
        }

        public override HardwareType HardwareType
        {
            get
            {
                return HardwareType.NIC;
            }
        }
        public override void Update()
        {
            DateTime newTime = DateTime.Now;
            float dt = (float)(newTime - latesTime).TotalSeconds;
            latesTime = newTime;
            IPv4InterfaceStatistics interfaceStats = nic.GetIPv4Statistics();
            connectionSpeed.Value = nic.Speed;
            long dBytesUploaded = interfaceStats.BytesSent - bytesUploaded;
            long dBytesDownloaded = interfaceStats.BytesReceived - bytesDownloaded;
            uploadSpeed.Value = (float)dBytesUploaded / dt;
            downloadSpeed.Value = (float)dBytesDownloaded / dt;
            networkUtilization.Value = Clamp((Math.Max(dBytesUploaded, dBytesDownloaded) * 800 / nic.Speed) / dt, 0,100);
            bytesUploaded = interfaceStats.BytesSent;
            bytesDownloaded = interfaceStats.BytesReceived;
            dataUploaded.Value = ((float)bytesUploaded / 1073741824);
            dataDownloaded.Value = ((float)bytesDownloaded / 1073741824);
            if (shouldTotalFlowUpdate)
            {
                long totalDownloadedBeforeLastBoot = Convert.ToInt64(settings.GetValue("TotalDownloadedBeforeNextShutdown" + nic.Name, "-1"));
                long totalUploadedBeforeLastBoot = Convert.ToInt64(settings.GetValue("TotalUploadedBeforeNextShutdown" + nic.Name, "-1"));
                if (totalDownloadedBeforeLastBoot == -1)
                {
                    settings.SetValue("TotalDownloadedBeforeNextShutdown" + nic.Name, "0");
                    settings.SetValue("TotalUploadedBeforeNextShutdown" + nic.Name, "0");
                }
                else
                {
                    totalBytesDownloaded += totalDownloadedBeforeLastBoot;
                    totalBytesUploaded += totalUploadedBeforeLastBoot;
                    settings.SetValue("TotalDownloadedBeforeLastBoot" + nic.Name, totalBytesDownloaded.ToString());
                    settings.SetValue("TotalUploadedBeforeLastBoot" + nic.Name, totalBytesUploaded.ToString());
                }
                settings.SetValue("lastBootTime" + nic.Name, string.Format("{0:g}", presentBootTime));
                shouldTotalFlowUpdate = false;
            }
            settings.SetValue("TotalDownloadedBeforeNextShutdown"+nic.Name, bytesDownloaded.ToString());
            settings.SetValue("TotalUploadedBeforeNextShutdown" + nic.Name, bytesUploaded.ToString());
            totalDataDownloaded.Value = ((float)(totalBytesDownloaded + bytesDownloaded) / 1073741824);
            totalDataUploaded.Value = ((float)(totalBytesUploaded + bytesUploaded) / 1073741824);
            totalDataFlowed.Value = totalDataDownloaded.Value + totalDataUploaded.Value;
        }

        private float Clamp(float value, float min, float max)
        {
            if (value < min) return min; else if (value > max) return max; else return value;
        }
    }
}
