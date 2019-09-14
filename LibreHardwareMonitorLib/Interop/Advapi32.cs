// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

// ReSharper disable InconsistentNaming

namespace LibreHardwareMonitor.Interop
{
    internal class Advapi32
    {
        private const string DllName = "advapi32.dll";

        internal enum ServiceAccessRights : uint
        {
            SERVICE_ALL_ACCESS = 0xF01FF
        }

        internal enum ServiceControlManagerAccessRights : uint
        {
            SC_MANAGER_ALL_ACCESS = 0xF003F
        }

        internal enum ServiceType : uint
        {
            SERVICE_KERNEL_DRIVER = 1,
            SERVICE_FILE_SYSTEM_DRIVER = 2
        }

        internal enum StartType : uint
        {
            SERVICE_BOOT_START = 0,
            SERVICE_SYSTEM_START = 1,
            SERVICE_AUTO_START = 2,
            SERVICE_DEMAND_START = 3,
            SERVICE_DISABLED = 4
        }

        internal enum ErrorControl : uint
        {
            SERVICE_ERROR_IGNORE = 0,
            SERVICE_ERROR_NORMAL = 1,
            SERVICE_ERROR_SEVERE = 2,
            SERVICE_ERROR_CRITICAL = 3
        }

        internal enum ServiceControl : uint
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
        internal struct ServiceStatus
        {
            public uint dwServiceType;
            public uint dwCurrentState;
            public uint dwControlsAccepted;
            public uint dwWin32ExitCode;
            public uint dwServiceSpecificExitCode;
            public uint dwCheckPoint;
            public uint dwWaitHint;
        }


        [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
        internal static extern IntPtr OpenSCManager(string machineName, string databaseName, ServiceControlManagerAccessRights dwAccess);

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseServiceHandle(IntPtr hSCObject);

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        internal static extern IntPtr CreateService(IntPtr hSCManager, string lpServiceName, string lpDisplayName,
            ServiceAccessRights dwDesiredAccess, ServiceType dwServiceType, StartType dwStartType, ErrorControl dwErrorControl,
            string lpBinaryPathName, string lpLoadOrderGroup, string lpdwTagId, string lpDependencies, string lpServiceStartName, string lpPassword);

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        internal static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, ServiceAccessRights dwDesiredAccess);

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteService(IntPtr hService);

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool StartService(IntPtr hService, uint dwNumServiceArgs, string[] lpServiceArgVectors);

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ControlService(IntPtr hService, ServiceControl dwControl, ref ServiceStatus lpServiceStatus);
    }
}
