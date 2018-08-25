using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;

namespace OpenHardwareMonitor.Hardware.Nic
{
    internal class NicGroup : IGroup
    {

        private List<Hardware> hardware = new List<Hardware>();
        private NetworkInterface[] nicArr;

        public NicGroup(ISettings settings)
        {
            int p = (int)Environment.OSVersion.Platform;
            if ((p == 4) || (p == 128))
            {
                hardware = new List<Hardware>();
                return;
            }
            nicArr = NetworkInterface.GetAllNetworkInterfaces();
            for (int i = 0; i < nicArr.Length; i++)
            {
                if (nicArr[i].NetworkInterfaceType != NetworkInterfaceType.Unknown && nicArr[i].NetworkInterfaceType != NetworkInterfaceType.Loopback && nicArr[i].NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                {
                    hardware.Add(new Nic(nicArr[i].Name, settings, i, this));
                }
                
            }
                
        }

        public string GetReport()
        {
            return null;
        }

        public IHardware[] Hardware
        {
            get
            {
                return hardware.ToArray();
            }
        }
        public NetworkInterface[] NicArr
        {
            get
            {
                return nicArr;
            }
            set
            {
                nicArr = value;
            }
        }
        public void Close()
        {
            foreach (Hardware nic in hardware)
                nic.Close();
        }
    }
}
