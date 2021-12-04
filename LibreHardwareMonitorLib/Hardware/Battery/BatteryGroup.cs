using System;
using System.Collections.Generic;
using System.Management;

namespace LibreHardwareMonitor.Hardware.Battery
{
    internal class BatteryGroup : IGroup
    {
        private List<Hardware> _hardware = new List<Hardware>();
        public IReadOnlyList<IHardware> Hardware => _hardware;

        public BatteryGroup(ISettings settings)
        {
            // No implementation for battery information on Unix systems
            if (Software.OperatingSystem.IsUnix)
            {
                return;
            }

            string queryStaticData = $"SELECT * FROM BatteryStaticData";
            using (ManagementObjectSearcher batteries = new ManagementObjectSearcher(@"root\WMI", queryStaticData) { Options = { Timeout = TimeSpan.FromSeconds(7.5) } })
            {
                foreach (ManagementObject mo in batteries.Get())
                {
                    _hardware.Add(new Battery(mo.Properties["DeviceName"].Value.ToString(), mo.Properties["InstanceName"].Value.ToString(), settings));
                }
            }
        }

        public void Close()
        {
            foreach (Hardware battery in _hardware)
            {
                battery.Close();
            }
        }
        public string GetReport() => null;
    }
}
