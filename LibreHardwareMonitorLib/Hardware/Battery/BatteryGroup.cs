// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using LibreHardwareMonitor.Interop;
using Microsoft.Win32.SafeHandles;

namespace LibreHardwareMonitor.Hardware.Battery;

internal class BatteryGroup : IGroup
{
    private readonly List<Battery> _hardware = new();

    public unsafe BatteryGroup(ISettings settings)
    {
        // No implementation for battery information on Unix systems
        if (Software.OperatingSystem.IsUnix)
            return;

        IntPtr hdev = SetupApi.SetupDiGetClassDevs(ref SetupApi.GUID_DEVICE_BATTERY, IntPtr.Zero, IntPtr.Zero, SetupApi.DIGCF_PRESENT | SetupApi.DIGCF_DEVICEINTERFACE);
        if (hdev != SetupApi.INVALID_HANDLE_VALUE)
        {
            for (uint i = 0; ; i++)
            {
                SetupApi.SP_DEVICE_INTERFACE_DATA did = default;
                did.cbSize = (uint)Marshal.SizeOf(typeof(SetupApi.SP_DEVICE_INTERFACE_DATA));

                if (!SetupApi.SetupDiEnumDeviceInterfaces(hdev,
                                                          IntPtr.Zero,
                                                          ref SetupApi.GUID_DEVICE_BATTERY,
                                                          i,
                                                          ref did))
                {
                    if (Marshal.GetLastWin32Error() == SetupApi.ERROR_NO_MORE_ITEMS)
                        break;
                }
                else
                {
                    SetupApi.SetupDiGetDeviceInterfaceDetail(hdev,
                                                             did,
                                                             IntPtr.Zero,
                                                             0,
                                                             out uint cbRequired,
                                                             IntPtr.Zero);

                    if (Marshal.GetLastWin32Error() == SetupApi.ERROR_INSUFFICIENT_BUFFER)
                    {
                        IntPtr pdidd = Kernel32.LocalAlloc(Kernel32.LPTR, cbRequired);
                        Marshal.WriteInt32(pdidd, Environment.Is64BitOperatingSystem ? 8 : 4); // cbSize.

                        if (SetupApi.SetupDiGetDeviceInterfaceDetail(hdev,
                                                                     did,
                                                                     pdidd,
                                                                     cbRequired,
                                                                     out _,
                                                                     IntPtr.Zero))
                        {
                            string devicePath = new((char*)(pdidd + 4));

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
                                        if (bi.Capabilities == Kernel32.BatteryCapabilities.BATTERY_SYSTEM_BATTERY)
                                        {
                                            const int maxLoadString = 100;

                                            IntPtr ptrDevName = Marshal.AllocCoTaskMem(maxLoadString);
                                            bqi.InformationLevel = Kernel32.BATTERY_QUERY_INFORMATION_LEVEL.BatteryDeviceName;

                                            if (Kernel32.DeviceIoControl(battery,
                                                                         Kernel32.IOCTL.IOCTL_BATTERY_QUERY_INFORMATION,
                                                                         ref bqi,
                                                                         Marshal.SizeOf(bqi),
                                                                         ptrDevName,
                                                                         maxLoadString,
                                                                         out _,
                                                                         IntPtr.Zero))
                                            {
                                                IntPtr ptrManName = Marshal.AllocCoTaskMem(maxLoadString);
                                                bqi.InformationLevel = Kernel32.BATTERY_QUERY_INFORMATION_LEVEL.BatteryManufactureName;

                                                if (Kernel32.DeviceIoControl(battery,
                                                                             Kernel32.IOCTL.IOCTL_BATTERY_QUERY_INFORMATION,
                                                                             ref bqi,
                                                                             Marshal.SizeOf(bqi),
                                                                             ptrManName,
                                                                             maxLoadString,
                                                                             out _,
                                                                             IntPtr.Zero))
                                                {
                                                    string name = Marshal.PtrToStringUni(ptrDevName);
                                                    string manufacturer = Marshal.PtrToStringUni(ptrManName);

                                                    _hardware.Add(new Battery(name, manufacturer, battery, bi, bqi.BatteryTag, settings));
                                                }

                                                Marshal.FreeCoTaskMem(ptrManName);
                                            }

                                            Marshal.FreeCoTaskMem(ptrDevName);
                                        }
                                    }
                                }
                            }
                        }

                        Kernel32.LocalFree(pdidd);
                    }
                }
            }

            SetupApi.SetupDiDestroyDeviceInfoList(hdev);
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
                         .Append(" Chemistry: ").AppendLine(chemistry)
                         .Append(" Degradation Level: ").AppendFormat("{0:F2}", bat.DegradationLevel).AppendLine(" %")
                         .Append(" Designed Capacity: ").Append(bat.DesignedCapacity).AppendLine(" mWh")
                         .Append(" Full Charged Capacity: ").Append(bat.FullChargedCapacity).AppendLine(" mWh")
                         .Append(" Remaining Capacity: ").Append(bat.RemainingCapacity).AppendLine(" mWh")
                         .Append(" Charge Level: ").AppendFormat("{0:F2}", bat.RemainingCapacity * 100f / bat.FullChargedCapacity).AppendLine(" %")
                         .Append(" Voltage: ").AppendFormat("{0:F3}", bat.Voltage).AppendLine(" V");

            if (bat.RemainingTime != Kernel32.BATTERY_UNKNOWN_TIME)
            {
                reportBuilder.Append(" Remaining Time (Estimated): ").AppendFormat("{0:g}", TimeSpan.FromSeconds(bat.RemainingTime)).AppendLine();
            }

            switch (bat.ChargeDischargeRate)
            {
                case > 0:
                    reportBuilder.Append(" Charge Rate: ").AppendFormat("{0:F1}", bat.ChargeDischargeRate).AppendLine(" W")
                                 .Append(" Charge Current: ").AppendFormat("{0:F3}", bat.ChargeDischargeRate / bat.Voltage).AppendLine(" A");

                    break;
                case < 0:
                    reportBuilder.Append(" Discharge Rate: ").AppendFormat("{0:F1}", Math.Abs(bat.ChargeDischargeRate)).AppendLine(" W")
                                 .Append(" Discharge Current: ").AppendFormat("{0:F3}", Math.Abs(bat.ChargeDischargeRate) / bat.Voltage).AppendLine(" A");

                    break;
                default:
                    reportBuilder.AppendLine(" Charge/Discharge Rate: 0 W")
                                 .AppendLine(" Charge/Discharge Current: 0 A");

                    break;
            }

            reportBuilder.AppendLine();
            count++;
        }

        return reportBuilder.ToString();
    }
}