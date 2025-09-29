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
using LibreHardwareMonitor.Interop;
using Microsoft.Win32.SafeHandles;

namespace LibreHardwareMonitor.Hardware.Battery;

internal class BatteryGroup : IGroup
{
    private readonly List<Battery> _hardware = new();

    static bool QueryStringFromBatteryInfo(SafeFileHandle battery, Kernel32.BATTERY_QUERY_INFORMATION bqi, out string value)
    {
        const int maxLoadString = 100;

        value = null;

        bool result = false;
        IntPtr ptrString = Marshal.AllocHGlobal(maxLoadString);
        if (Kernel32.DeviceIoControl(battery,
                                     Kernel32.IOCTL.IOCTL_BATTERY_QUERY_INFORMATION,
                                     ref bqi,
                                     Marshal.SizeOf(bqi),
                                     ptrString,
                                     maxLoadString,
                                     out uint stringSizeBytes,
                                     IntPtr.Zero))
        {
            // Use the value stored in stringSizeBytes to avoid relying on a
            // terminator char.
            // See https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/pull/1158#issuecomment-1979559929
            int stringSizeChars = (int)stringSizeBytes / 2;
            value = Marshal.PtrToStringUni(ptrString, stringSizeChars);
            result = true;
        }

        Marshal.FreeHGlobal(ptrString);
        return result;
    }

    public unsafe BatteryGroup(ISettings settings)
    {
        // No implementation for battery information on Unix systems
        if (Software.OperatingSystem.IsUnix)
            return;

        SetupDiDestroyDeviceInfoListSafeHandle hdev = PInvoke.SetupDiGetClassDevs(PInvoke.GUID_DEVICE_BATTERY, null, HWND.Null, SETUP_DI_GET_CLASS_DEVS_FLAGS.DIGCF_PRESENT | SETUP_DI_GET_CLASS_DEVS_FLAGS.DIGCF_DEVICEINTERFACE);
        if (!hdev.IsInvalid)
        {
            for (uint i = 0; ; i++)
            {
                SP_DEVICE_INTERFACE_DATA data = default;
                data.cbSize = (uint)sizeof(SP_DEVICE_INTERFACE_DATA);

                if (!PInvoke.SetupDiEnumDeviceInterfaces(hdev,
                                                          null,
                                                          PInvoke.GUID_DEVICE_BATTERY,
                                                          i,
                                                          ref data))
                {
                    if (Marshal.GetLastWin32Error() == (int) WIN32_ERROR.ERROR_NO_MORE_ITEMS)
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

                    if (Marshal.GetLastWin32Error() == (int) WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER)
                    {
                        IntPtr buffer = Marshal.AllocHGlobal((int)cbRequired);
                        SP_DEVICE_INTERFACE_DETAIL_DATA_W* pData = (SP_DEVICE_INTERFACE_DETAIL_DATA_W*) buffer;
                        pData->cbSize = (uint)sizeof(SP_DEVICE_INTERFACE_DETAIL_DATA_W);

                        if (PInvoke.SetupDiGetDeviceInterfaceDetail(hdev,
                                                                     data,
                                                                     pData,
                                                                     cbRequired,
                                                                     &cbRequired,
                                                                     null) )
                        {
                            string devicePath = pData->DevicePath.ToString();

                            SafeFileHandle battery = Kernel32.CreateFile(devicePath, FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
                            if (!battery.IsInvalid)
                            {
                                Kernel32.BATTERY_QUERY_INFORMATION bqi = default;

                                uint dwWait = 0;
                                if (Kernel32.DeviceIoControl(battery,
                                                             Kernel32.IOCTL.IOCTL_BATTERY_QUERY_TAG,
                                                             ref dwWait,
                                                             Marshal.SizeOf(dwWait),
                                                             ref bqi.BatteryTag,
                                                             Marshal.SizeOf(bqi.BatteryTag),
                                                             out _,
                                                             IntPtr.Zero))
                                {
                                    Kernel32.BATTERY_INFORMATION bi = default;
                                    bqi.InformationLevel = Kernel32.BATTERY_QUERY_INFORMATION_LEVEL.BatteryInformation;

                                    if (Kernel32.DeviceIoControl(battery,
                                                                 Kernel32.IOCTL.IOCTL_BATTERY_QUERY_INFORMATION,
                                                                 ref bqi,
                                                                 Marshal.SizeOf(bqi),
                                                                 ref bi,
                                                                 Marshal.SizeOf(bi),
                                                                 out _,
                                                                 IntPtr.Zero))
                                    {
                                        // Only batteries count.
                                        if (bi.Capabilities.HasFlag(Kernel32.BatteryCapabilities.BATTERY_SYSTEM_BATTERY))
                                        {
                                            bqi.InformationLevel = Kernel32.BATTERY_QUERY_INFORMATION_LEVEL.BatteryDeviceName;
                                            QueryStringFromBatteryInfo(battery, bqi, out string batteryName);
                                            bqi.InformationLevel = Kernel32.BATTERY_QUERY_INFORMATION_LEVEL.BatteryManufactureName;
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
