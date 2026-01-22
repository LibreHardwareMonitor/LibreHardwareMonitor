// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Runtime.InteropServices;

namespace LibreHardwareMonitor.Interop;

internal static class SetupAPI
{
    const string DLLNAME = "setupapi.dll";

    internal const int DIGCF_PRESENT = 0x00000002;

    internal const uint SPDRP_HARDWAREID = 0x00000001;
    internal const uint SPDRP_FRIENDLYNAME = 0x0000000C;

    [DllImport(DLLNAME, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr SetupDiGetClassDevs(
        ref Guid ClassGuid,
        IntPtr Enumerator,
        IntPtr hwndParent,
        uint Flags);

    [DllImport(DLLNAME, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool SetupDiEnumDeviceInfo(
        IntPtr DeviceInfoSet,
        uint MemberIndex,
        ref SP_DEVINFO_DATA DeviceInfoData);

    [DllImport(DLLNAME, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool SetupDiGetDeviceRegistryProperty(
        IntPtr DeviceInfoSet,
        ref SP_DEVINFO_DATA DeviceInfoData,
        uint Property,
        out uint PropertyRegDataType,
        char[] PropertyBuffer,
        uint PropertyBufferSize,
        out uint RequiredSize);

    [DllImport(DLLNAME, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool SetupDiGetDeviceRegistryProperty(
        IntPtr deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        uint property,
        out uint propertyRegDataType,
        ref uint propertyBuffer,
        uint propertyBufferSize,
        out uint requiredSize
        );

    [DllImport(DLLNAME, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

    [StructLayout(LayoutKind.Sequential)]
    internal struct SP_DEVINFO_DATA
    {
        public uint cbSize;
        public Guid classGuid;
        public uint devInst;
        public IntPtr reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SP_DEVICE_INTERFACE_DATA
    {
        public int cbSize;
        public Guid InterfaceClassGuid;
        public int Flags;
        public IntPtr Reserved;
    }
}
