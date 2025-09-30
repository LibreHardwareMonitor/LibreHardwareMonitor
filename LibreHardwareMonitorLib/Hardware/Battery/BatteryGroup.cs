// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Win32;
using Windows.Win32.Devices.DeviceAndDriverInstallation;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.System.Power;
using LibreHardwareMonitor.Interop;
using Microsoft.Win32.SafeHandles;

namespace LibreHardwareMonitor.Hardware.Battery;

internal class BatteryGroup : IGroup
{
    private readonly List<Battery> _hardware = [];

    public unsafe BatteryGroup(ISettings settings)
    {
        // No implementation for battery information on Unix systems
        if (Software.OperatingSystem.IsUnix)
            return;

        SetupDiDestroyDeviceInfoListSafeHandle hdev = PInvoke.SetupDiGetClassDevs(PInvoke.GUID_DEVICE_BATTERY,
                                                                                  null,
                                                                                  HWND.Null,
                                                                                  SETUP_DI_GET_CLASS_DEVS_FLAGS.DIGCF_PRESENT | SETUP_DI_GET_CLASS_DEVS_FLAGS.DIGCF_DEVICEINTERFACE);

        if (!hdev.IsInvalid)
        {
            for (uint i = 0;; i++)
            {
                SP_DEVICE_INTERFACE_DATA data = default;
                data.cbSize = (uint)sizeof(SP_DEVICE_INTERFACE_DATA);

                if (!PInvoke.SetupDiEnumDeviceInterfaces(hdev,
                                                         null,
                                                         PInvoke.GUID_DEVICE_BATTERY,
                                                         i,
                                                         ref data))
                {
                    if (Marshal.GetLastWin32Error() == (int)WIN32_ERROR.ERROR_NO_MORE_ITEMS)
                        break;
                }
                else
                {
                    uint cbRequired = 0;

                    PInvoke.SetupDiGetDeviceInterfaceDetail(hdev,
                                                            data,
                                                            null,
                                                            0,
                                                            &cbRequired,
                                                            null);

                    if (Marshal.GetLastWin32Error() == (int)WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER)
                    {
                        IntPtr buffer = Marshal.AllocHGlobal((int)cbRequired);
                        SP_DEVICE_INTERFACE_DETAIL_DATA_W* pData = (SP_DEVICE_INTERFACE_DETAIL_DATA_W*)buffer;
                        pData->cbSize = (uint)sizeof(SP_DEVICE_INTERFACE_DETAIL_DATA_W);

                        if (PInvoke.SetupDiGetDeviceInterfaceDetail(hdev,
                                                                    data,
                                                                    pData,
                                                                    cbRequired,
                                                                    &cbRequired,
                                                                    null))
                        {
                            string devicePath = pData->DevicePath.ToString();

                            SafeFileHandle battery = PInvoke.CreateFile(devicePath, (uint)FileAccess.ReadWrite, FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE, null, FILE_CREATION_DISPOSITION.OPEN_EXISTING, FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL, null);
                            if (!battery.IsInvalid)
                            {
                                BATTERY_QUERY_INFORMATION bqi = default;

                                uint dwWait = 0;
                                uint bytesReturned = 0;
                                if (PInvoke.DeviceIoControl(battery,
                                                            PInvoke.IOCTL_BATTERY_QUERY_TAG,
                                                            &dwWait,
                                                            sizeof(uint),
                                                            &bqi.BatteryTag,
                                                            sizeof(uint),
                                                            &bytesReturned,
                                                            null))
                                {
                                    BATTERY_INFORMATION bi = default;
                                    bqi.InformationLevel = BATTERY_QUERY_INFORMATION_LEVEL.BatteryInformation;

                                    if (PInvoke.DeviceIoControl(battery,
                                                                PInvoke.IOCTL_BATTERY_QUERY_INFORMATION,
                                                                &bqi,
                                                                (uint)sizeof(BATTERY_QUERY_INFORMATION),
                                                                &bi,
                                                                (uint)sizeof(BATTERY_INFORMATION),
                                                                &bytesReturned,
                                                                null))
                                    {
                                        // Only batteries count.

                                        if ((bi.Capabilities & PInvoke.BATTERY_SYSTEM_BATTERY) == PInvoke.BATTERY_SYSTEM_BATTERY)
                                        {
                                            bqi.InformationLevel = BATTERY_QUERY_INFORMATION_LEVEL.BatteryDeviceName;
                                            QueryStringFromBatteryInfo(battery, bqi, out string batteryName);
                                            bqi.InformationLevel = BATTERY_QUERY_INFORMATION_LEVEL.BatteryManufactureName;
                                            QueryStringFromBatteryInfo(battery, bqi, out string manufacturer);

                                            _hardware.Add(new Battery(batteryName, manufacturer, battery, bi, bqi.BatteryTag, settings));
                                        }
                                    }
                                }
                            }
                        }

                        Marshal.FreeHGlobal(buffer);
                    }
                }
            }

            hdev.Dispose();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<IHardware> Hardware => _hardware;

    private static unsafe bool QueryStringFromBatteryInfo(SafeFileHandle battery, BATTERY_QUERY_INFORMATION bqi, out string value)
    {
        value = null;
        bool result = false;

        Span<char> span = stackalloc char[100];

        fixed (char* pSpan = span)
        {
            uint returnBytes = 0;

            if (PInvoke.DeviceIoControl(battery,
                                        PInvoke.IOCTL_BATTERY_QUERY_INFORMATION,
                                        &bqi,
                                        (uint)sizeof(BATTERY_QUERY_INFORMATION),
                                        pSpan,
                                        200,
                                        &returnBytes,
                                        null))
            {
                value = span.ToString();
                result = true;
            }
        }

        return result;
    }

    /// <inheritdoc />
    public void Close()
    {
        foreach (Battery battery in _hardware)
            battery.Close();
    }

    /// <inheritdoc />
    public string GetReport()
    {
        StringBuilder reportBuilder = new();

        uint count = 1;

        foreach (Battery bat in _hardware)
        {
            string chemistry = bat.Chemistry switch
            {
                BatteryChemistry.LeadAcid => "Lead Acid",
                BatteryChemistry.NickelCadmium => "Nickel-Cadmium",
                BatteryChemistry.NickelMetalHydride => "Nickel-Metal Hydride",
                BatteryChemistry.LithiumIon => "Lithium Ion",
                BatteryChemistry.NickelZinc => "Nickel-Zinc",
                BatteryChemistry.AlkalineManganese => "Rechargeable Alkaline-Manganese",
                _ => "Unknown"
            };

            reportBuilder.Append("Battery #").Append(count).AppendLine(":")
                         .Append(" Name: ").AppendLine(bat.Name)
                         .Append(" Manufacturer: ").AppendLine(bat.Manufacturer)
                         .Append(" Chemistry: ").AppendLine(chemistry);

            if (bat.DegradationLevel.HasValue)
                reportBuilder.Append(" Degradation Level: ").AppendFormat("{0:F2}", bat.DegradationLevel).AppendLine(" %");

            if (bat.DesignedCapacity.HasValue)
                reportBuilder.Append(" Designed Capacity: ").Append(bat.DesignedCapacity).AppendLine(" mWh");

            if (bat.FullChargedCapacity.HasValue)
                reportBuilder.Append(" Fully-Charged Capacity: ").Append(bat.FullChargedCapacity).AppendLine(" mWh");

            if (bat.RemainingCapacity.HasValue)
                reportBuilder.Append(" Remaining Capacity: ").Append(bat.RemainingCapacity).AppendLine(" mWh");

            if (bat.ChargeLevel.HasValue)
                reportBuilder.Append(" Charge Level: ").AppendFormat("{0:F2}", bat.ChargeLevel).AppendLine(" %");

            if (bat.Voltage.HasValue)
                reportBuilder.Append(" Voltage: ").AppendFormat("{0:F3}", bat.Voltage).AppendLine(" V");

            if (bat.Temperature.HasValue)
                reportBuilder.Append(" Temperature: ").AppendFormat("{0:F3}", bat.Temperature).AppendLine(" ºC");

            if (bat.RemainingTime.HasValue)
                reportBuilder.Append(" Remaining Time (Estimated): ").AppendFormat("{0:g}", TimeSpan.FromSeconds(bat.RemainingTime.Value)).AppendLine();

            string cdRateSensorName;
            string cdCurrentSensorName;
            if (bat.ChargeDischargeRate > 0)
            {
                cdRateSensorName = " Charge Rate: ";
                cdCurrentSensorName = " Charge Current: ";
            }
            else if (bat.ChargeDischargeRate < 0)
            {
                cdRateSensorName = " Discharge Rate: ";
                cdCurrentSensorName = " Discharge Current: ";
            }
            else
            {
                cdRateSensorName = " Charge/Discharge Rate: ";
                cdCurrentSensorName = " Charge/Discharge Current: ";
            }

            if (bat.ChargeDischargeRate.HasValue)
                reportBuilder.Append(cdRateSensorName).AppendFormat("{0:F1}", Math.Abs(bat.ChargeDischargeRate.Value)).AppendLine(" W");

            if (bat.ChargeDischargeCurrent.HasValue)
                reportBuilder.Append(cdCurrentSensorName).AppendFormat("{0:F3}", Math.Abs(bat.ChargeDischargeCurrent.Value)).AppendLine(" A");

            reportBuilder.AppendLine();
            count++;
        }

        return reportBuilder.ToString();
    }
}
