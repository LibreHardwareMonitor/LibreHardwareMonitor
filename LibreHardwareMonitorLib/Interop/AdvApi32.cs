// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming

namespace LibreHardwareMonitor.Interop;

internal class AdvApi32
{
    private const string DllName = "advapi32.dll";

    [DllImport(DllName, SetLastError = true, CallingConvention = CallingConvention.Winapi)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern IntPtr OpenSCManager(string lpMachineName, string lpDatabaseName, SC_MANAGER_ACCESS_MASK dwDesiredAccess);

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseServiceHandle(IntPtr hSCObject);

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern IntPtr CreateService
    (
        IntPtr hSCManager,
        string lpServiceName,
        string lpDisplayName,
        SERVICE_ACCESS_MASK dwDesiredAccess,
        SERVICE_TYPE dwServiceType,
        SERVICE_START dwStartType,
        SERVICE_ERROR dwErrorControl,
        string lpBinaryPathName,
        string lpLoadOrderGroup,
        string lpdwTagId,
        string lpDependencies,
        string lpServiceStartName,
        string lpPassword);

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, SERVICE_ACCESS_MASK dwDesiredAccess);

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeleteService(IntPtr hService);

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool StartService(IntPtr hService, uint dwNumServiceArgs, string[] lpServiceArgVectors);

    [DllImport(DllName, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ControlService(IntPtr hService, SERVICE_CONTROL dwControl, ref SERVICE_STATUS lpServiceStatus);

    [Flags]
    internal enum SC_MANAGER_ACCESS_MASK : uint
    {
        SC_MANAGER_CONNECT = 0x00001,
        SC_MANAGER_CREATE_SERVICE = 0x00002,
        SC_MANAGER_ENUMERATE_SERVICE = 0x00004,
        SC_MANAGER_LOCK = 0x00008,
        SC_MANAGER_QUERY_LOCK_STATUS = 0x00010,
        SC_MANAGER_MODIFY_BOOT_CONFIG = 0x00020,
        SC_MANAGER_ALL_ACCESS = 0xF003F
    }

    internal enum SERVICE_ACCESS_MASK : uint
    {
        SERVICE_QUERY_CONFIG = 0x00001,
        SERVICE_CHANGE_CONFIG = 0x00002,
        SERVICE_QUERY_STATUS = 0x00004,
        SERVICE_ENUMERATE_DEPENDENTS = 0x00008,
        SERVICE_START = 0x00010,
        SERVICE_STOP = 0x00020,
        SERVICE_PAUSE_CONTINUE = 0x00040,
        SERVICE_INTERROGATE = 0x00080,
        SERVICE_USER_DEFINED_CONTROL = 0x00100,
        SERVICE_ALL_ACCESS = 0xF01FF
    }

    internal enum SERVICE_TYPE : uint
    {
        SERVICE_DRIVER = 0x0000000B,
        SERVICE_WIN32 = 0x00000030,
        SERVICE_ADAPTER = 0x00000004,
        SERVICE_FILE_SYSTEM_DRIVER = 0x00000002,
        SERVICE_KERNEL_DRIVER = 0x00000001,
        SERVICE_RECOGNIZER_DRIVER = 0x00000008,
        SERVICE_WIN32_OWN_PROCESS = 0x00000010,
        SERVICE_WIN32_SHARE_PROCESS = 0x00000020,
        SERVICE_USER_OWN_PROCESS = 0x00000050,
        SERVICE_USER_SHARE_PROCESS = 0x00000060,
        SERVICE_INTERACTIVE_PROCESS = 0x00000100
    }

    internal enum SERVICE_START : uint
    {
        SERVICE_BOOT_START = 0,
        SERVICE_SYSTEM_START = 1,
        SERVICE_AUTO_START = 2,
        SERVICE_DEMAND_START = 3,
        SERVICE_DISABLED = 4
    }

    internal enum SERVICE_ERROR : uint
    {
        SERVICE_ERROR_IGNORE = 0,
        SERVICE_ERROR_NORMAL = 1,
        SERVICE_ERROR_SEVERE = 2,
        SERVICE_ERROR_CRITICAL = 3
    }

    internal enum SERVICE_CONTROL : uint
    {
        SERVICE_CONTROL_STOP = 1,
        SERVICE_CONTROL_PAUSE = 2,
        SERVICE_CONTROL_CONTINUE = 3,
        SERVICE_CONTROL_INTERROGATE = 4,
        SERVICE_CONTROL_SHUTDOWN = 5,
        SERVICE_CONTROL_PARAMCHANGE = 6,
        SERVICE_CONTROL_NETBINDADD = 7,
        SERVICE_CONTROL_NETBINDREMOVE = 8,
        SERVICE_CONTROL_NETBINDENABLE = 9,
        SERVICE_CONTROL_NETBINDDISABLE = 10,
        SERVICE_CONTROL_DEVICEEVENT = 11,
        SERVICE_CONTROL_HARDWAREPROFILECHANGE = 12,
        SERVICE_CONTROL_POWEREVENT = 13,
        SERVICE_CONTROL_SESSIONCHANGE = 14
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SERVICE_STATUS
    {
        public uint dwServiceType;
        public uint dwCurrentState;
        public uint dwControlsAccepted;
        public uint dwWin32ExitCode;
        public uint dwServiceSpecificExitCode;
        public uint dwCheckPoint;
        public uint dwWaitHint;
    }
}
