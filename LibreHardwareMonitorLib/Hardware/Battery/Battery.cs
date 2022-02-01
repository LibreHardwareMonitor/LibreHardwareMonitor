using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using LibreHardwareMonitor.Interop;

namespace LibreHardwareMonitor.Hardware.Battery
{
    internal enum BatteryChemistry
    {
        Unknown,
        LeadAcid,
        NickelCadmium,
        NickelMetalHydride,
        LithiumIon,
        NickelZinc,
        AlkalineManganese
    }

    internal sealed class Battery : Hardware
    {
        private readonly Sensor _chargeLevel;
        private readonly Sensor _voltage;
        private readonly Sensor _chargeDischargeeCurrent;
        private readonly Sensor _designedCapacity;
        private readonly Sensor _fullChargedCapacity;
        private readonly Sensor _remainingCapacity;
        private readonly Sensor _chargeDischargeRate;
        private readonly Sensor _degradationPercentage;
        private readonly SafeFileHandle _batteryHandle;
        private readonly uint _batteryTag;

        private Kernel32.BATTERY_INFORMATION _batteryInformation;

        public Battery(string name, string manufacturer, SafeFileHandle batteryHandle, Kernel32.BATTERY_INFORMATION batteryInfo, uint batteryTag, ISettings settings) : base(name, new Identifier("battery"), settings)
        {
            Name = name;
            Manufacturer = manufacturer;
            _batteryTag = batteryTag;
            _batteryHandle = batteryHandle;
            _batteryInformation = batteryInfo;

            if (batteryInfo.Chemistry.SequenceEqual(new char[] { 'P', 'b', 'A', 'c' }))
            {
                Chemistry = BatteryChemistry.LeadAcid;
            }
            else if (batteryInfo.Chemistry.SequenceEqual(new char[] { 'L', 'I', 'O', 'N' }) || batteryInfo.Chemistry.SequenceEqual(new char[] { 'L', 'i', '-', 'I' }))
            {
                Chemistry = BatteryChemistry.LithiumIon;
            }
            else if (batteryInfo.Chemistry.SequenceEqual(new char[] { 'N', 'i', 'C', 'd' }))
            {
                Chemistry = BatteryChemistry.NickelCadmium;
            }
            else if (batteryInfo.Chemistry.SequenceEqual(new char[] { 'N', 'i', 'M', 'H' }))
            {
                Chemistry = BatteryChemistry.NickelMetalHydride;
            }
            else if (batteryInfo.Chemistry.SequenceEqual(new char[] { 'N', 'i', 'Z', 'n' }))
            {
                Chemistry = BatteryChemistry.NickelZinc;
            }
            else if (batteryInfo.Chemistry.SequenceEqual(new char[] { 'R', 'A', 'M', '\x00' }))
            {
                Chemistry = BatteryChemistry.AlkalineManganese;
            }
            else
            {
                Chemistry = BatteryChemistry.Unknown;
            }

            DegradationLevel = 100f - (batteryInfo.FullChargedCapacity * 100f / batteryInfo.DesignedCapacity);
            DesignedCapacity = batteryInfo.DesignedCapacity;
            FullChargedCapacity = batteryInfo.FullChargedCapacity;

            _chargeLevel = new Sensor("Charge Level", 0, SensorType.Level, this, settings);
            ActivateSensor(_chargeLevel);

            _voltage = new Sensor("Voltage", 1, SensorType.Voltage, this, settings);
            ActivateSensor(_voltage);

            _chargeDischargeeCurrent = new Sensor("Current", 2, SensorType.Current, this, settings);
            ActivateSensor(_chargeDischargeeCurrent);

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
        
        public string Manufacturer { get; }
        public BatteryChemistry Chemistry { get; }
        public float DegradationLevel { get; private set; }
        public float DesignedCapacity { get; private set; }
        public float FullChargedCapacity { get; private set; }
        public float RemainingCapacity { get; private set; }
        public float ChargeLevel { get; private set; }
        public float Voltage { get; private set; }
        public float ChargeDischargeRate { get; private set; }
        public float ChargeDischargeCurrent { get; private set; }

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
                RemainingCapacity = Convert.ToSingle(bs.Capacity);

                _voltage.Value = Convert.ToSingle(bs.Voltage) / 1000f;
                Voltage = Convert.ToSingle(bs.Voltage) / 1000f;

                _chargeLevel.Value = _remainingCapacity.Value * 100f / _fullChargedCapacity.Value;
                ChargeLevel = (_remainingCapacity.Value * 100f / _fullChargedCapacity.Value).GetValueOrDefault();

                ChargeDischargeRate = bs.Rate / 1000f;
                if (bs.Rate > 0)
                {
                    _chargeDischargeRate.Name = "Charge Rate";
                    _chargeDischargeRate.Value = bs.Rate / 1000f;

                    _chargeDischargeeCurrent.Name = "Charge Current";
                    _chargeDischargeeCurrent.Value = _chargeDischargeRate.Value / _voltage.Value;
                    ChargeDischargeCurrent = (_chargeDischargeRate.Value / _voltage.Value).GetValueOrDefault();
                }
                else if (bs.Rate < 0)
                {
                    _chargeDischargeRate.Name = "Discharge Rate";
                    _chargeDischargeRate.Value = Math.Abs(bs.Rate) / 1000f;

                    _chargeDischargeeCurrent.Name = "Discharge Current";
                    _chargeDischargeeCurrent.Value = _chargeDischargeRate.Value / _voltage.Value;
                    ChargeDischargeCurrent = (_chargeDischargeRate.Value / _voltage.Value).GetValueOrDefault();
                }
                else
                {
                    _chargeDischargeRate.Name = "Charge/Discharge Rate";
                    _chargeDischargeRate.Value = 0f;
                    ChargeDischargeRate = 0f;

                    _chargeDischargeeCurrent.Name = "Charge/Discharge Current";
                    _chargeDischargeeCurrent.Value = 0f;
                    ChargeDischargeCurrent = 0f;
                }

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
