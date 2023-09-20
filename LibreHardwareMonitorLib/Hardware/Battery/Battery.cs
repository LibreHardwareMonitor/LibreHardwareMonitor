// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Interop;
using Microsoft.Win32.SafeHandles;

namespace LibreHardwareMonitor.Hardware.Battery;

internal sealed class Battery : Hardware
{
    private readonly SafeFileHandle _batteryHandle;
    private readonly Kernel32.BATTERY_INFORMATION _batteryInformation;
    private readonly uint _batteryTag;
    private readonly Sensor _chargeDischargeCurrent;
    private readonly Sensor _chargeDischargeRate;
    private readonly Sensor _chargeLevel;
    private readonly Sensor _degradationLevel;
    private readonly Sensor _designedCapacity;
    private readonly Sensor _fullChargedCapacity;
    private readonly Sensor _remainingCapacity;
    private readonly Sensor _remainingTime;
    private readonly Sensor _temperature;
    private readonly Sensor _voltage;

    public Battery
    (
        string name,
        string manufacturer,
        SafeFileHandle batteryHandle,
        Kernel32.BATTERY_INFORMATION batteryInfo,
        uint batteryTag,
        ISettings settings) :
        base(name, new Identifier("battery", $"{name.Replace(' ', '-')}_{batteryTag}"), settings)
    {
        Name = name;
        Manufacturer = manufacturer;

        _batteryTag = batteryTag;
        _batteryHandle = batteryHandle;
        _batteryInformation = batteryInfo;

        if (batteryInfo.Chemistry.SequenceEqual(new[] { 'P', 'b', 'A', 'c' }))
        {
            Chemistry = BatteryChemistry.LeadAcid;
        }
        else if (batteryInfo.Chemistry.SequenceEqual(new[] { 'L', 'I', 'O', 'N' }) || batteryInfo.Chemistry.SequenceEqual(new[] { 'L', 'i', '-', 'I' }))
        {
            Chemistry = BatteryChemistry.LithiumIon;
        }
        else if (batteryInfo.Chemistry.SequenceEqual(new[] { 'N', 'i', 'C', 'd' }))
        {
            Chemistry = BatteryChemistry.NickelCadmium;
        }
        else if (batteryInfo.Chemistry.SequenceEqual(new[] { 'N', 'i', 'M', 'H' }))
        {
            Chemistry = BatteryChemistry.NickelMetalHydride;
        }
        else if (batteryInfo.Chemistry.SequenceEqual(new[] { 'N', 'i', 'Z', 'n' }))
        {
            Chemistry = BatteryChemistry.NickelZinc;
        }
        else if (batteryInfo.Chemistry.SequenceEqual(new[] { 'R', 'A', 'M', '\x00' }))
        {
            Chemistry = BatteryChemistry.AlkalineManganese;
        }
        else
        {
            Chemistry = BatteryChemistry.Unknown;
        }

        _chargeLevel = new Sensor("Charge Level", 0, SensorType.Level, this, settings);
        ActivateSensor(_chargeLevel);

        _voltage = new Sensor("Voltage", 0, SensorType.Voltage, this, settings);
        ActivateSensor(_voltage);

        _chargeDischargeCurrent = new Sensor("Charge/Discharge Current", 0, SensorType.Current, this, settings);
        ActivateSensor(_chargeDischargeCurrent);

        _designedCapacity = new Sensor("Designed Capacity", 0, SensorType.Energy, this, settings);
        ActivateSensor(_designedCapacity);

        _fullChargedCapacity = new Sensor("Fully-Charged Capacity", 1, SensorType.Energy, this, settings);
        ActivateSensor(_fullChargedCapacity);

        _remainingCapacity = new Sensor("Remaining Capacity", 2, SensorType.Energy, this, settings);
        ActivateSensor(_remainingCapacity);

        _chargeDischargeRate = new Sensor("Charge/Discharge Rate", 0, SensorType.Power, this, settings);
        ActivateSensor(_chargeDischargeRate);

        _degradationLevel = new Sensor("Degradation Level", 1, SensorType.Level, this, settings);
        ActivateSensor(_degradationLevel);

        _remainingTime = new Sensor("Remaining Time (Estimated)", 0, SensorType.TimeSpan, this, settings);
        ActivateSensor(_remainingTime);

        _temperature = new Sensor("Battery Temperature", 0, SensorType.Temperature, this, settings);
        ActivateSensor(_temperature);

        if (batteryInfo.FullChargedCapacity is not Kernel32.BATTERY_UNKNOWN_CAPACITY &&
            batteryInfo.DesignedCapacity is not Kernel32.BATTERY_UNKNOWN_CAPACITY)
        {
            _degradationLevel.Value = 100f - (batteryInfo.FullChargedCapacity * 100f / batteryInfo.DesignedCapacity);
            DesignedCapacity = batteryInfo.DesignedCapacity;
            FullChargedCapacity = batteryInfo.FullChargedCapacity;
        }
    }

    public float? ChargeDischargeCurrent { get; private set; }

    public float? ChargeDischargeRate { get; private set; }

    public float? ChargeLevel => _chargeLevel.Value;

    public BatteryChemistry Chemistry { get; }

    public float? DegradationLevel => _degradationLevel.Value;

    public float? DesignedCapacity { get; }

    public float? FullChargedCapacity { get; }

    public override HardwareType HardwareType => HardwareType.Battery;

    public string Manufacturer { get; }

    public float? RemainingCapacity => _remainingCapacity.Value;

    public uint RemainingTime { get; private set; }

    public float? Temperature => _temperature.Value;

    public float? Voltage => _voltage.Value;

    private void SetSensorValue(Sensor sensor, uint value, uint notAvailableValue)
    {
        if (value != notAvailableValue)
            sensor.Value = Convert.ToSingle(value);
        else
            sensor.Value = null;
    }

    public override void Update()
    {
        Kernel32.BATTERY_WAIT_STATUS bws = default;
        bws.BatteryTag = _batteryTag;
        Kernel32.BATTERY_STATUS batteryStatus = default;
        if (Kernel32.DeviceIoControl(_batteryHandle,
                                     Kernel32.IOCTL.IOCTL_BATTERY_QUERY_STATUS,
                                     ref bws,
                                     Marshal.SizeOf(bws),
                                     ref batteryStatus,
                                     Marshal.SizeOf(batteryStatus),
                                     out _,
                                     IntPtr.Zero))
        {
            SetSensorValue(_designedCapacity, _batteryInformation.DesignedCapacity, Kernel32.BATTERY_UNKNOWN_CAPACITY);
            SetSensorValue(_fullChargedCapacity, _batteryInformation.FullChargedCapacity, Kernel32.BATTERY_UNKNOWN_CAPACITY);

            SetSensorValue(_remainingCapacity, batteryStatus.Capacity, Kernel32.BATTERY_UNKNOWN_CAPACITY);

            _chargeLevel.Value = _remainingCapacity.Value * 100f / _fullChargedCapacity.Value;

            if (batteryStatus.Voltage is not Kernel32.BATTERY_UNKNOWN_VOLTAGE)
                _voltage.Value = batteryStatus.Voltage / 1000f;
            else
                _voltage.Value = null;

            if (batteryStatus.Rate is Kernel32.BATTERY_UNKNOWN_RATE)
            {
                ChargeDischargeCurrent = null;
                _chargeDischargeCurrent.Value = null;

                ChargeDischargeRate = null;
                _chargeDischargeRate.Value = null;
            }
            else
            {
                float rateWatts = batteryStatus.Rate / 1000f;
                ChargeDischargeRate = rateWatts;
                _chargeDischargeRate.Value = Math.Abs(rateWatts);

                float? current = rateWatts / _voltage.Value;
                ChargeDischargeCurrent = current;
                if (current is not null)
                    _chargeDischargeCurrent.Value = Math.Abs(current.Value);
                else
                    _chargeDischargeCurrent.Value = null;

                if (rateWatts > 0)
                {
                    _chargeDischargeRate.Name = "Charge Rate";
                    _chargeDischargeCurrent.Name = "Charge Current";
                }
                else if (rateWatts < 0)
                {
                    _chargeDischargeRate.Name = "Discharge Rate";
                    _chargeDischargeCurrent.Name = "Discharge Current";
                }
                else
                {
                    _chargeDischargeRate.Name = "Charge/Discharge Rate";
                    _chargeDischargeCurrent.Name = "Charge/Discharge Current";
                }
            }

            _degradationLevel.Value = 100f - (_fullChargedCapacity.Value * 100f / _designedCapacity.Value);
        }

        uint estimatedRunTime = 0;
        Kernel32.BATTERY_QUERY_INFORMATION bqi = default;
        bqi.BatteryTag = _batteryTag;
        bqi.InformationLevel = Kernel32.BATTERY_QUERY_INFORMATION_LEVEL.BatteryEstimatedTime;
        if (Kernel32.DeviceIoControl(_batteryHandle,
                                     Kernel32.IOCTL.IOCTL_BATTERY_QUERY_INFORMATION,
                                     ref bqi,
                                     Marshal.SizeOf(bqi),
                                     ref estimatedRunTime,
                                     Marshal.SizeOf<uint>(),
                                     out _,
                                     IntPtr.Zero))
        {
            RemainingTime = estimatedRunTime;
            if (estimatedRunTime != Kernel32.BATTERY_UNKNOWN_TIME)
            {
                ActivateSensor(_remainingTime);
                _remainingTime.Value = estimatedRunTime;
            }
            else
            {
                DeactivateSensor(_remainingTime);
            }
        }

        uint temperature = 0;
        bqi.InformationLevel = Kernel32.BATTERY_QUERY_INFORMATION_LEVEL.BatteryTemperature;
        if (Kernel32.DeviceIoControl(_batteryHandle,
                                     Kernel32.IOCTL.IOCTL_BATTERY_QUERY_INFORMATION,
                                     ref bqi,
                                     Marshal.SizeOf(bqi),
                                     ref temperature,
                                     Marshal.SizeOf<uint>(),
                                     out _,
                                     IntPtr.Zero))
        {
            _temperature.Value = (temperature / 10f) - 273.15f;
            ActivateSensor(_temperature);
        }
        else
        {
            DeactivateSensor(_temperature);
        }
    }

    public override void Close()
    {
        base.Close();
        _batteryHandle.Close();
    }
}
