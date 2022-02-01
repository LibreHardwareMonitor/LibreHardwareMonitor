using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using LibreHardwareMonitor.Interop;

namespace LibreHardwareMonitor.Hardware.Battery
{
    internal class BatteryGroup : IGroup
    {
        private List<Battery> _hardware = new();

        public IReadOnlyList<IHardware> Hardware => _hardware;

        private static IntPtr Offset(IntPtr pointer, long offset) => new IntPtr(pointer.ToInt64() + offset);
        private static int GetCharSize(CharSet charSet = CharSet.Auto) => charSet == CharSet.Auto ? Marshal.SystemDefaultCharSize : (charSet == CharSet.Unicode ? 2 : 1);
        private static bool IsValue(IntPtr ptr) => ptr.ToInt64() >> 16 == 0;

        private static string GetString(IntPtr ptr, CharSet charSet = CharSet.Auto, long allocatedBytes = long.MaxValue)
        {
            if (IsValue(ptr)) return null;
            var sb = new StringBuilder();
            unsafe
            {
                var chkLen = 0L;
                if (GetCharSize(charSet) == 1)
                {
                    for (var uptr = (byte*)ptr; chkLen < allocatedBytes && *uptr != 0; chkLen++, uptr++)
                        sb.Append((char)*uptr);
                }
                else
                {
                    for (var uptr = (ushort*)ptr; chkLen + 2 <= allocatedBytes && *uptr != 0; chkLen += 2, uptr++)
                        sb.Append((char)*uptr);
                }
            }
            return sb.ToString();
        }

        public BatteryGroup(ISettings settings)
        {
            // No implementation for battery information on Unix systems
            if (Software.OperatingSystem.IsUnix)
            {
                return;
            }

            IntPtr hdev = SetupApi.SetupDiGetClassDevs(ref SetupApi.GUID_DEVICE_BATTERY, IntPtr.Zero, IntPtr.Zero, SetupApi.DIGCF_PRESENT | SetupApi.DIGCF_DEVICEINTERFACE);
            if (SetupApi.INVALID_HANDLE_VALUE != hdev)
            {
                for (uint idev = 0; ; idev++)
                {
                    SetupApi.SP_DEVICE_INTERFACE_DATA did = default;
                    did.cbSize = (uint)Marshal.SizeOf(typeof(SetupApi.SP_DEVICE_INTERFACE_DATA));

                    if (!SetupApi.SetupDiEnumDeviceInterfaces(hdev,
                                                             IntPtr.Zero,
                                                             ref SetupApi.GUID_DEVICE_BATTERY,
                                                             idev,
                                                             ref did))
                    {
                        if (Marshal.GetLastWin32Error() == SetupApi.ERROR_NO_MORE_ITEMS)
                        {
                            break;
                        }
                    }
                    else
                    {
                        SetupApi.SetupDiGetDeviceInterfaceDetailW(hdev,
                                                                 did,
                                                                 IntPtr.Zero,
                                                                 0,
                                                                 out uint cbRequired,
                                                                 IntPtr.Zero);
                        if (Marshal.GetLastWin32Error() == SetupApi.ERROR_INSUFFICIENT_BUFFER)
                        {
                            IntPtr pdidd = Kernel32.LocalAlloc(Kernel32.LPTR, cbRequired);
                            Marshal.StructureToPtr(SetupApi.SP_DEVICE_INTERFACE_DETAIL_DATA_W.Default, pdidd, true);

                            if (SetupApi.SetupDiGetDeviceInterfaceDetailW(hdev,
                                                                         did,
                                                                         pdidd,
                                                                         cbRequired,
                                                                         out cbRequired,
                                                                         IntPtr.Zero))
                            {
                                string devicePath = GetString(Offset(pdidd, 4), CharSet.Unicode, cbRequired - 4);

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
                                            // Only batteries count
                                            if (bi.Capabilities == Kernel32.BatteryCapabilities.BATTERY_SYSTEM_BATTERY)
                                            {
                                                const int MAX_LOADSTRING = 100;

                                                IntPtr ptrDevName = Marshal.AllocCoTaskMem(MAX_LOADSTRING);
                                                bqi.InformationLevel = Kernel32.BATTERY_QUERY_INFORMATION_LEVEL.BatteryDeviceName;
                                                if (Kernel32.DeviceIoControl(battery,
                                                                     Kernel32.IOCTL.IOCTL_BATTERY_QUERY_INFORMATION,
                                                                     ref bqi,
                                                                     Marshal.SizeOf(bqi),
                                                                     ptrDevName,
                                                                     MAX_LOADSTRING,
                                                                     out _,
                                                                     IntPtr.Zero))
                                                {
                                                    IntPtr ptrManName = Marshal.AllocCoTaskMem(MAX_LOADSTRING);
                                                    bqi.InformationLevel = Kernel32.BATTERY_QUERY_INFORMATION_LEVEL.BatteryManufactureName;
                                                    if (Kernel32.DeviceIoControl(battery,
                                                                         Kernel32.IOCTL.IOCTL_BATTERY_QUERY_INFORMATION,
                                                                         ref bqi,
                                                                         Marshal.SizeOf(bqi),
                                                                         ptrManName,
                                                                         MAX_LOADSTRING,
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

        public void Close()
        {
            foreach (Hardware battery in _hardware)
            {
                battery.Close();
            }
        }

        public string GetReport()
        {
            StringBuilder reportBuilder = new();

            uint count = 1;
            foreach (Battery bat in _hardware)
            {
                string chemistry = "Unknown";
                switch (bat.Chemistry)
                {
                    case BatteryChemistry.LeadAcid:
                    chemistry = "Lead Acid";
                    break;
                    case BatteryChemistry.NickelCadmium:
                    chemistry = "Nickel-Cadmium";
                    break;
                    case BatteryChemistry.NickelMetalHydride:
                    chemistry = "Nickel-Metal Hydride";
                    break;
                    case BatteryChemistry.LithiumIon:
                    chemistry = "Lithium Ion";
                    break;
                    case BatteryChemistry.NickelZinc:
                    chemistry = "Nickel-Zinc";
                    break;
                    case BatteryChemistry.AlkalineManganese:
                    chemistry = "Rechargeable Alkaline-Manganese";
                    break;
                }

                reportBuilder
                    .AppendLine($"Battery #{count}:")
                    .AppendLine($" Name: {bat.Name}")
                    .AppendLine($" Manufacturer: {bat.Manufacturer}")
                    .AppendLine($" Chemistry: {chemistry}")
                    .AppendLine($" Degradation Level: {bat.DegradationLevel:F2} %")
                    .AppendLine($" Designed Capacity: {bat.DesignedCapacity} mWh")
                    .AppendLine($" Full Charged Capacity: {bat.FullChargedCapacity} mWh")
                    .AppendLine($" Remaining Capacity: {bat.RemainingCapacity} mWh")
                    .AppendLine($" Charge Level: {bat.RemainingCapacity * 100f / bat.FullChargedCapacity:F2} %")
                    .AppendLine($" Voltage: {bat.Voltage:F3} V");

                if (bat.ChargeDischargeRate > 0)
                {
                    reportBuilder
                        .AppendLine($" Charge Rate: {bat.ChargeDischargeRate:F1} W")
                        .AppendLine($" Charge Current: {bat.ChargeDischargeRate / bat.Voltage:F3} A");
                }
                else if (bat.ChargeDischargeRate < 0)
                {
                    reportBuilder
                        .AppendLine($" Discharge Rate: {Math.Abs(bat.ChargeDischargeRate):F1} W")
                        .AppendLine($" Discharge Current: {Math.Abs(bat.ChargeDischargeRate) / bat.Voltage:F3} A");
                }
                else
                {
                    reportBuilder
                        .AppendLine($" Charge/Discharge Rate: {0:F1} W")
                        .AppendLine($" Charge/Discharge Current: {0:F3} A");
                }

                reportBuilder
                    .AppendLine();

                count++;
            }

            return reportBuilder.ToString();
        }
    }
}
