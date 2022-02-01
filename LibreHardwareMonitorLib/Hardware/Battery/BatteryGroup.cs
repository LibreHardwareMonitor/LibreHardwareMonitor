using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using LibreHardwareMonitor.Interop;

namespace LibreHardwareMonitor.Hardware.Battery
{
    internal struct BatteryReportInfo
    {
        public string Name { get; set; }
        public uint Tag { get; set; }
        public SafeFileHandle BatteryHandle { get; set; }
        public Kernel32.BATTERY_INFORMATION MoreInformation { get; set; }
    }

    internal class BatteryGroup : IGroup
    {
        private List<Hardware> _hardware = new();
        private List<BatteryReportInfo> _batteriesToReport = new();

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
                                                    string name = Marshal.PtrToStringUni(ptrDevName);
                                                    _batteriesToReport.Add(new BatteryReportInfo { Name = name, Tag = bqi.BatteryTag, BatteryHandle = battery, MoreInformation = bi });
                                                    _hardware.Add(new Battery(name, battery, bi, bqi.BatteryTag, settings));
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
            foreach (BatteryReportInfo bat in _batteriesToReport)
            {
                reportBuilder
                    .AppendLine($"Battery #{count}:")
                    .AppendLine($" Name: {bat.Name}")
                    .AppendLine($" Degradation Level: {100f - (bat.MoreInformation.FullChargedCapacity * 100f / bat.MoreInformation.DesignedCapacity):F2} %")
                    .AppendLine($" Designed Capacity: {bat.MoreInformation.DesignedCapacity} mWh")
                    .AppendLine($" Full Charged Capacity: {bat.MoreInformation.FullChargedCapacity} mWh");

                Kernel32.BATTERY_WAIT_STATUS bws = default;
                bws.BatteryTag = bat.Tag;
                Kernel32.BATTERY_STATUS bs = default;
                if (Kernel32.DeviceIoControl(bat.BatteryHandle,
                                     Kernel32.IOCTL.IOCTL_BATTERY_QUERY_STATUS,
                                     ref bws,
                                     Marshal.SizeOf(bws),
                                     ref bs,
                                     Marshal.SizeOf(bs),
                                     out _,
                                     IntPtr.Zero))
                {
                    reportBuilder
                        .AppendLine($" Remaining Capacity: {bs.Capacity} mWh")
                        .AppendLine($" Charge Level: {bs.Capacity * 100f / bat.MoreInformation.FullChargedCapacity:F2} %")
                        .AppendLine($" Voltage: {Convert.ToSingle(bs.Voltage) / 1000f:F3} V");

                    if (bs.Rate > 0)
                    {
                        reportBuilder
                            .AppendLine($" Charge Rate: {bs.Rate / 1000f:F1} W")
                            .AppendLine($" Charge Current: {(float)bs.Rate / (float)bs.Voltage:F3} A");
                    }
                    else if (bs.Rate < 0)
                    {
                        reportBuilder
                            .AppendLine($" Discharge Rate: {Math.Abs(bs.Rate) / 1000f:F1} W")
                            .AppendLine($" Discharge Current: {(float)Math.Abs(bs.Rate) / (float)bs.Voltage:F3} A");
                    }
                    else
                    {
                        reportBuilder
                            .AppendLine($" Charge/Discharge Rate: {0:F1} W")
                            .AppendLine($" Charge/Discharge Current: {0:F3} A");
                    }
                }

                reportBuilder
                    .AppendLine();

                count++;
            }

            return reportBuilder.ToString();
        }
    }
}
