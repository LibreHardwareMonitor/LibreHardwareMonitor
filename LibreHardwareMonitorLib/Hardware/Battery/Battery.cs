using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using LibreHardwareMonitor.Interop;

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
        private readonly SafeFileHandle _batteryHandle;
        private readonly uint _batteryTag;

        private Kernel32.BATTERY_INFORMATION _batteryInformation;

        public Battery(string name, SafeFileHandle batteryHandle, Kernel32.BATTERY_INFORMATION batteryInfo, uint batteryTag, ISettings settings) : base(name, new Identifier("battery"), settings)
        {
            Name = name;
            _batteryHandle = batteryHandle;
            _batteryInformation = batteryInfo;
            _batteryTag = batteryTag;

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

        public override void Update()
        {
            Kernel32.BATTERY_WAIT_STATUS bws = default;
            bws.BatteryTag = _batteryTag;
            Kernel32.BATTERY_STATUS bs = default;
            if (Kernel32.DeviceIoControl(_batteryHandle,
                                 Kernel32.IOCTL.IOCTL_BATTERY_QUERY_STATUS,
                                 ref bws,
                                 Marshal.SizeOf(bws),
                                 ref bs,
                                 Marshal.SizeOf(bs),
                                 out _,
                                 IntPtr.Zero))
            {

                _designedCapacity.Value = Convert.ToSingle(_batteryInformation.DesignedCapacity);
                _fullChargedCapacity.Value = Convert.ToSingle(_batteryInformation.FullChargedCapacity);
                _remainingCapacity.Value = Convert.ToSingle(bs.Capacity);
                _voltage.Value = Convert.ToSingle(bs.Voltage) / 1000f;
                _chargePercentage.Value = _remainingCapacity.Value * 100f / _fullChargedCapacity.Value;

                if (bs.Rate > 0)
                {
                    _chargeDischargeRate.Name = "Charge Rate";
                    _chargeDischargeRate.Value = bs.Rate / 1000f;
                }
                else if (bs.Rate < 0)
                {
                    _chargeDischargeRate.Name = "Discharge Rate";
                    _chargeDischargeRate.Value = Math.Abs(bs.Rate) / 1000f;
                }
                else
                {
                    _chargeDischargeRate.Name = "Charge/Discharge Rate";
                    _chargeDischargeRate.Value = 0f;
                }

                _current.Value = _chargeDischargeRate.Value / _voltage.Value;
                _degradationPercentage.Value = 100f - (_fullChargedCapacity.Value * 100f / _designedCapacity.Value);
            }
        }

        public override void Close()
        {
            base.Close();
            _batteryHandle.Close();
        }
    }
}
