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
    private readonly Sensor _degradationPercentage;
    private readonly Sensor _designedCapacity;
    private readonly Sensor _fullChargedCapacity;
    private readonly Sensor _remainingCapacity;
    private readonly Sensor _remainingTime;
    private readonly Sensor _voltage;

    public Battery
    (
        string name,
        string manufacturer,
        SafeFileHandle batteryHandle,
        Kernel32.BATTERY_INFORMATION batteryInfo,
        uint batteryTag,
        ISettings settings) :
        base(name, new Identifier("battery"), settings)
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

        DegradationLevel = 100f - (batteryInfo.FullChargedCapacity * 100f / batteryInfo.DesignedCapacity);
        DesignedCapacity = batteryInfo.DesignedCapacity;
        FullChargedCapacity = batteryInfo.FullChargedCapacity;

        _chargeLevel = new Sensor("Charge Level", 0, SensorType.Level, this, settings);
        ActivateSensor(_chargeLevel);

        _voltage = new Sensor("Voltage", 1, SensorType.Voltage, this, settings);
        ActivateSensor(_voltage);

        _chargeDischargeCurrent = new Sensor("Current", 2, SensorType.Current, this, settings);
        ActivateSensor(_chargeDischargeCurrent);

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

        _remainingTime = new Sensor("Remaining Time (Estimated)", 0, SensorType.TimeSpan, this, settings);
        ActivateSensor(_remainingTime);
    }

    public float ChargeDischargeCurrent { get; private set; }

    public float ChargeDischargeRate { get; private set; }

    public float ChargeLevel { get; private set; }

    public BatteryChemistry Chemistry { get; }

    public float DegradationLevel { get; }

    public float DesignedCapacity { get; }

    public float FullChargedCapacity { get; }

    public override HardwareType HardwareType => HardwareType.Battery;

    public string Manufacturer { get; }

    public float RemainingCapacity { get; private set; }

    public uint RemainingTime { get; private set; }

    public float Voltage { get; private set; }

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
            _designedCapacity.Value = Convert.ToSingle(_batteryInformation.DesignedCapacity);
            _fullChargedCapacity.Value = Convert.ToSingle(_batteryInformation.FullChargedCapacity);

            _remainingCapacity.Value = Convert.ToSingle(batteryStatus.Capacity);
            RemainingCapacity = Convert.ToSingle(batteryStatus.Capacity);

            _voltage.Value = Convert.ToSingle(batteryStatus.Voltage) / 1000f;
            Voltage = Convert.ToSingle(batteryStatus.Voltage) / 1000f;

            _chargeLevel.Value = _remainingCapacity.Value * 100f / _fullChargedCapacity.Value;
            ChargeLevel = (_remainingCapacity.Value * 100f / _fullChargedCapacity.Value).GetValueOrDefault();

            ChargeDischargeRate = batteryStatus.Rate / 1000f;

            switch (batteryStatus.Rate)
            {
                case > 0:
                    _chargeDischargeRate.Name = "Charge Rate";
                    _chargeDischargeRate.Value = batteryStatus.Rate / 1000f;

                    _chargeDischargeCurrent.Name = "Charge Current";
                    _chargeDischargeCurrent.Value = _chargeDischargeRate.Value / _voltage.Value;
                    ChargeDischargeCurrent = (_chargeDischargeRate.Value / _voltage.Value).GetValueOrDefault();

                    break;
                case < 0:
                    _chargeDischargeRate.Name = "Discharge Rate";
                    _chargeDischargeRate.Value = Math.Abs(batteryStatus.Rate / 1000f);

                    _chargeDischargeCurrent.Name = "Discharge Current";
                    _chargeDischargeCurrent.Value = _chargeDischargeRate.Value / _voltage.Value;
                    ChargeDischargeCurrent = (_chargeDischargeRate.Value / _voltage.Value).GetValueOrDefault();

                    break;
                default:
                    _chargeDischargeRate.Name = "Charge/Discharge Rate";
                    _chargeDischargeRate.Value = 0f;
                    ChargeDischargeRate = 0f;

                    _chargeDischargeCurrent.Name = "Charge/Discharge Current";
                    _chargeDischargeCurrent.Value = 0f;
                    ChargeDischargeCurrent = 0f;

                    break;
            }

            _degradationPercentage.Value = 100f - (_fullChargedCapacity.Value * 100f / _designedCapacity.Value);
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
    }

    public override void Close()
    {
        base.Close();
        _batteryHandle.Close();
    }
}
