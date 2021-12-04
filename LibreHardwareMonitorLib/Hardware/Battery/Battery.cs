using System;
using System.Management;
using System.Linq;

namespace LibreHardwareMonitor.Hardware.Battery
{
    internal sealed class Battery : Hardware
    {
        private readonly Sensor _chargePercentage;
        private readonly Sensor _voltage;
        private readonly Sensor _current;
        private readonly Sensor _designedCapacity;
        private readonly Sensor _fullChargedCapacity;
        private readonly Sensor _remainingCapacity;
        private readonly Sensor _chargeDischargeRate;
        private readonly Sensor _degradationPercentage;

        private readonly string _instanceName;
        private int _wmiFailureCount;

        public Battery(string name, string instanceName, ISettings settings) : base(name, new Identifier("battery"), settings)
        {
            Name = name;
            _instanceName = instanceName;

            _chargePercentage = new Sensor("Charge Level", 0, SensorType.Level, this, settings);
            ActivateSensor(_chargePercentage);

            _voltage = new Sensor("Voltage", 1, SensorType.Voltage, this, settings);
            ActivateSensor(_voltage);

            _current = new Sensor("Current", 2, SensorType.Current, this, settings);
            ActivateSensor(_current);

            _designedCapacity = new Sensor("Designed Capacity", 3, SensorType.Energy, this, settings);
            ActivateSensor(_designedCapacity);

            _fullChargedCapacity = new Sensor("Full Charged Capacity", 4, SensorType.Energy, this, settings);
            ActivateSensor(_fullChargedCapacity);

            _remainingCapacity = new Sensor("Remaining Capacity", 5, SensorType.Energy, this, settings);
            ActivateSensor(_remainingCapacity);

            _chargeDischargeRate = new Sensor("Charge/Discharge Rate", 0, SensorType.Power, this, settings);
            ActivateSensor(_chargeDischargeRate);

            _degradationPercentage = new Sensor("Degradation Level", 0, SensorType.Level, this, settings);
            ActivateSensor(_degradationPercentage);
        }

        public override HardwareType HardwareType => HardwareType.Battery;

        private string EscapeBackslash(string s)
        {
            return s.Replace(@"\", @"\\");
        }

        private void UpdateStatisticsFromWmi()
        {
            string queryStaticData = $"SELECT * FROM BatteryStaticData WHERE InstanceName=\"{EscapeBackslash(_instanceName)}\"";
            using var batteryStaticDataInfo = new ManagementObjectSearcher(@"root\WMI", queryStaticData) { Options = { Timeout = TimeSpan.FromSeconds(7.5) } };
            using ManagementObjectCollection collectionStaticData = batteryStaticDataInfo.Get();
            using ManagementObject batteryStaticData = collectionStaticData.OfType<ManagementObject>().FirstOrDefault();

            string queryFullChargedCapacity = $"SELECT * FROM BatteryFullChargedCapacity WHERE InstanceName=\"{EscapeBackslash(_instanceName)}\"";
            using var batteryFullChargedCapacityInfo = new ManagementObjectSearcher(@"root\WMI", queryFullChargedCapacity) { Options = { Timeout = TimeSpan.FromSeconds(7.5) } };
            using ManagementObjectCollection collectionFCC = batteryFullChargedCapacityInfo.Get();
            using ManagementObject fullChargedCapacity = collectionFCC.OfType<ManagementObject>().FirstOrDefault();

            string queryStatus = $"SELECT * FROM BatteryStatus WHERE InstanceName=\"{EscapeBackslash(_instanceName)}\"";
            using var batteryStatusInfo = new ManagementObjectSearcher(@"root\WMI", queryStatus) { Options = { Timeout = TimeSpan.FromSeconds(7.5) } };
            using ManagementObjectCollection collectionStatus = batteryStatusInfo.Get();
            using ManagementObject batteryStatus = collectionStatus.OfType<ManagementObject>().FirstOrDefault();

            //if (batteryStatus == null)
            //    return;


            _designedCapacity.Value = Convert.ToSingle(batteryStaticData.Properties["DesignedCapacity"].Value);
            _fullChargedCapacity.Value = Convert.ToSingle(fullChargedCapacity.Properties["FullChargedCapacity"].Value);
            _remainingCapacity.Value = Convert.ToSingle(batteryStatus.Properties["RemainingCapacity"].Value);
            _voltage.Value = Convert.ToSingle(batteryStatus.Properties["Voltage"].Value) / 1000f;
            _chargePercentage.Value = _remainingCapacity.Value * 100f / _fullChargedCapacity.Value;

            float chargeRate = Convert.ToSingle(batteryStatus.Properties["ChargeRate"].Value);
            float dischargeRate = Convert.ToSingle(batteryStatus.Properties["DischargeRate"].Value);
            if (chargeRate > 0)
            {
                _chargeDischargeRate.Name = "Charge Rate";
                _chargeDischargeRate.Value = chargeRate / 1000f;
            }
            else if (dischargeRate > 0)
            {
                _chargeDischargeRate.Name = "Discharge Rate";
                _chargeDischargeRate.Value = dischargeRate / 1000f;
            }
            else
            {
                _chargeDischargeRate.Name = "Charge/Discharge Rate";
                _chargeDischargeRate.Value = 0f;
            }

            _current.Value = _chargeDischargeRate.Value / _voltage.Value;
            _degradationPercentage.Value = 100f - (_fullChargedCapacity.Value * 100f / _designedCapacity.Value);
        }

        public override void Update()
        {
            const int wmiRetries = 10;

            //update statistics from WMI on every update
            if (_wmiFailureCount <= wmiRetries)
            {
                try
                {
                    UpdateStatisticsFromWmi();
                    _wmiFailureCount = 0;
                }
                catch
                {
                    if (++_wmiFailureCount == wmiRetries)
                    {
                        DeactivateSensor(_chargePercentage);
                        DeactivateSensor(_voltage);
                        DeactivateSensor(_current);
                        DeactivateSensor(_designedCapacity);
                        DeactivateSensor(_fullChargedCapacity);
                        DeactivateSensor(_remainingCapacity);
                        DeactivateSensor(_chargeDischargeRate);
                        DeactivateSensor(_degradationPercentage);
                    }
                }
            }
        }
    }
}
