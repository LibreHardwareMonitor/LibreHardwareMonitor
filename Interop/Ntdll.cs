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
    public class Ntdll
    {
        private const string DllName = "ntdll";

        [StructLayout(LayoutKind.Sequential)]
        public struct SystemProcessorPerformanceInformation
        {
            public long IdleTime;
            public long KernelTime;
            public long UserTime;
            public long Reserved0;
            public long Reserved1;
            public ulong Reserved2;
        }

        public enum SystemInformationClass
        {
            SystemBasicInformation = 0,
            SystemCpuInformation = 1,
            SystemPerformanceInformation = 2,
            SystemTimeOfDayInformation = 3,
            SystemProcessInformation = 5,
            SystemProcessorPerformanceInformation = 8
        }

        [DllImport(DllName)]
        public static extern int NtQuerySystemInformation(SystemInformationClass informationClass, [Out] SystemProcessorPerformanceInformation[] informations, int structSize, out IntPtr returnLength);
    }
}
