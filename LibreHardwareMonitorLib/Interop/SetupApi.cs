using System;
using System.Runtime.InteropServices;

namespace LibreHardwareMonitor.Interop
{
    internal class SetupApi
    {
        internal const int ERROR_INSUFFICIENT_BUFFER = 122;
        internal const int ERROR_NO_MORE_ITEMS = 259;
        internal const int DIGCF_PRESENT = 0x00000002;
        internal const int DIGCF_DEVICEINTERFACE = 0x00000010;
        internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        internal static Guid GUID_DEVICE_BATTERY = new Guid(0x72631e54, 0x78A4, 0x11d0, 0xbc, 0xf7, 0x00, 0xaa, 0x00, 0xb7, 0xb3, 0x2a);
        private const string DllName = "SetupAPI.dll";

        [StructLayout(LayoutKind.Sequential)]
        internal struct SP_DEVICE_INTERFACE_DATA
        {
            public uint cbSize;
            public Guid InterfaceClassGuid;
            public uint Flags;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct SP_DEVICE_INTERFACE_DETAIL_DATA_W
        {
            public uint cbSize;

            public char DevicePath;

            public static readonly SP_DEVICE_INTERFACE_DETAIL_DATA_W Default = new SP_DEVICE_INTERFACE_DETAIL_DATA_W { cbSize = IntPtr.Size == 4 ? 4U + (uint)Marshal.SystemDefaultCharSize : 8U };
        }

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        internal static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, uint Flags);

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, IntPtr DeviceInfoData, ref Guid InterfaceClassGuid, uint MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport(DllName, SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetupDiGetDeviceInterfaceDetailW(IntPtr DeviceInfoSet,
                                                                    in SP_DEVICE_INTERFACE_DATA DeviceInterfaceData,
                                                                    [Out, Optional] IntPtr DeviceInterfaceDetailData,
                                                                    uint DeviceInterfaceDetailDataSize,
                                                                    out uint RequiredSize,
                                                                    IntPtr DeviceInfoData = default);

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);
    }
}
