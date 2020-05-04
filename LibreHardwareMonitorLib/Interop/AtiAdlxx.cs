// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

// ReSharper disable InconsistentNaming

namespace LibreHardwareMonitor.Interop
{
    internal static class AtiAdlxx
    {
        public const int ADL_DL_FANCTRL_FLAG_USER_DEFINED_SPEED = 1;

        public const int ADL_DL_FANCTRL_SPEED_TYPE_PERCENT = 1;
        public const int ADL_DL_FANCTRL_SPEED_TYPE_RPM = 2;

        public const int ADL_DL_FANCTRL_SUPPORTS_PERCENT_READ = 1;
        public const int ADL_DL_FANCTRL_SUPPORTS_PERCENT_WRITE = 2;
        public const int ADL_DL_FANCTRL_SUPPORTS_RPM_READ = 4;
        public const int ADL_DL_FANCTRL_SUPPORTS_RPM_WRITE = 8;

        public const int ADL_DRIVER_OK = 0;

        public const int ADL_MAX_ADAPTERS = 40;
        public const int ADL_MAX_DEVICENAME = 32;
        public const int ADL_MAX_DISPLAYS = 40;
        public const int ADL_MAX_GLSYNC_PORT_LEDS = 8;
        public const int ADL_MAX_GLSYNC_PORTS = 8;
        public const int ADL_MAX_NUM_DISPLAYMODES = 1024;
        public const int ADL_MAX_PATH = 256;

        public const int ADL_OK = 0;
        public const int ADL_ERR = -1;

        public const int ATI_VENDOR_ID = 0x1002;

        internal const string DllName = "atiadlxx.dll";

        public static Context Context_Alloc = Marshal.AllocHGlobal;

        // create a Main_Memory_Alloc delegate and keep it alive
        public static ADL_Main_Memory_AllocDelegate Main_Memory_Alloc = Marshal.AllocHGlobal;

        public delegate IntPtr ADL_Main_Memory_AllocDelegate(int size);

        public delegate IntPtr Context(int size);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ADL_Main_Control_Create(ADL_Main_Memory_AllocDelegate callback, int enumConnectedAdapters);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ADL_Main_Control_Destroy();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ADL_Adapter_AdapterInfo_Get(IntPtr info, int size);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ADL_Adapter_NumberOfAdapters_Get();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ADL_Adapter_NumberOfAdapters_Get(ref int numAdapters);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ADL_Adapter_ID_Get(int adapterIndex, out int adapterId);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ADL_Display_AdapterID_Get(int adapterIndex, out int adapterId);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ADL_Adapter_Active_Get(int adapterIndex, out int status);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ADL_Overdrive5_CurrentActivity_Get(int iAdapterIndex, ref ADLPMActivity activity);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ADL_Overdrive5_Temperature_Get(int adapterIndex, int thermalControllerIndex, ref ADLTemperature temperature);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ADL2_OverdriveN_Temperature_Get(IntPtr context, int adapterIndex, ADLODNTemperatureType iTemperatureType, ref int temp);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ADL_Overdrive5_FanSpeed_Get(int adapterIndex, int thermalControllerIndex, ref ADLFanSpeedValue fanSpeedValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ADL_Overdrive5_FanSpeedInfo_Get(int adapterIndex, int thermalControllerIndex, ref ADLFanSpeedInfo fanSpeedInfo);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ADL_Overdrive5_FanSpeedToDefault_Set(int adapterIndex, int thermalControllerIndex);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ADL_Overdrive5_FanSpeed_Set(int adapterIndex, int thermalControllerIndex, ref ADLFanSpeedValue fanSpeedValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ADL_Overdrive_Caps(int adapterIndex, ref int supported, ref int enabled, ref int version);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ADL2_Overdrive6_CurrentPower_Get(IntPtr context, int adapterIndex, ADLODNCurrentPowerType powerType, ref int currentValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ADL2_Main_Control_Create(ADL_Main_Memory_AllocDelegate callback, int connectedAdapters, ref IntPtr context);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ADL2_Main_Control_Destroy(IntPtr context);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ADL2_New_QueryPMLogData_Get(IntPtr context, int adapterIndex, ref ADLPMLogDataOutput aDLPMLogDataOutput);

        public static int ADL_Main_Control_Create(int enumConnectedAdapters)
        {
            try
            {
                return Kernel32.LoadLibrary(DllName) != IntPtr.Zero ? ADL_Main_Control_Create(Main_Memory_Alloc, enumConnectedAdapters) : ADL_ERR;
            }
            catch
            {
                return ADL_ERR;
            }
        }

        public static int ADL_Adapter_AdapterInfo_Get(ADLAdapterInfo[] info)
        {
            int elementSize = Marshal.SizeOf(typeof(ADLAdapterInfo));
            int size = info.Length * elementSize;
            IntPtr ptr = Marshal.AllocHGlobal(size);
            int result = ADL_Adapter_AdapterInfo_Get(ptr, size);
            for (int i = 0; i < info.Length; i++)
                info[i] = (ADLAdapterInfo)Marshal.PtrToStructure((IntPtr)((long)ptr + i * elementSize), typeof(ADLAdapterInfo));

            Marshal.FreeHGlobal(ptr);

            // the ADLAdapterInfo.VendorID field reported by ADL is wrong on
            // Windows systems (parse error), so we fix this here
            for (int i = 0; i < info.Length; i++)
            {
                // try Windows UDID format
                Match m = Regex.Match(info[i].UDID, "PCI_VEN_([A-Fa-f0-9]{1,4})&.*");
                if (m.Success && m.Groups.Count == 2)
                {
                    info[i].VendorID = Convert.ToInt32(m.Groups[1].Value, 16);
                    continue;
                }

                // if above failed, try Unix UDID format
                m = Regex.Match(info[i].UDID, "[0-9]+:[0-9]+:([0-9]+):[0-9]+:[0-9]+");
                if (m.Success && m.Groups.Count == 2)
                {
                    info[i].VendorID = Convert.ToInt32(m.Groups[1].Value, 10);
                }
            }

            return result;
        }

        public static void Main_Memory_Free(IntPtr buffer)
        {
            if (IntPtr.Zero != buffer)
                Marshal.FreeHGlobal(buffer);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ADLAdapterInfo
        {
            public int Size;
            public int AdapterIndex;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL_MAX_PATH)]
            public string UDID;

            public int BusNumber;
            public int DeviceNumber;
            public int FunctionNumber;
            public int VendorID;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL_MAX_PATH)]
            public string AdapterName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL_MAX_PATH)]
            public string DisplayName;

            public int Present;
            public int Exist;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL_MAX_PATH)]
            public string DriverPath;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL_MAX_PATH)]
            public string DriverPathExt;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL_MAX_PATH)]
            public string PNPString;

            public int OSDisplayIndex;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ADLPMActivity
        {
            public int Size;
            public int EngineClock;
            public int MemoryClock;
            public int Vddc;
            public int ActivityPercent;
            public int CurrentPerformanceLevel;
            public int CurrentBusSpeed;
            public int CurrentBusLanes;
            public int MaximumBusLanes;
            public int Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ADLTemperature
        {
            public int Size;
            public int Temperature;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ADLFanSpeedValue
        {
            public int Size;
            public int SpeedType;
            public int FanSpeed;
            public int Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ADLFanSpeedInfo
        {
            public int Size;
            public int Flags;
            public int MinPercent;
            public int MaxPercent;
            public int MinRPM;
            public int MaxRPM;
        }

        internal enum ADLODNCurrentPowerType
        {
            ODN_GPU_TOTAL_POWER = 0,
            ODN_GPU_PPT_POWER,
            ODN_GPU_SOCKET_POWER,
            ODN_GPU_CHIP_POWER
        }

        internal enum ADLODNTemperatureType
        {
            // This typed is named like this in the documentation but for some reason AMD failed to include it...
            // Yet it seems these correspond with ADL_PMLOG_TEMPERATURE_xxx.
            EDGE = 1,
            MEM = 2,
            VRVDDC = 3,
            VRMVDD = 4,
            LIQUID = 5,
            PLX = 6,
            HOTSPOT = 7
        }

        internal const int ADL_PMLOG_MAX_SENSORS = 256;

        [StructLayout(LayoutKind.Sequential)]
        internal struct ADLSingleSensorData
        {
            public int supported;
            public int value;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ADLPMLogDataOutput
        {
            public int size;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = ADL_PMLOG_MAX_SENSORS)]
            public ADLSingleSensorData[] sensors;
        }

        internal enum ADLSensorType
        {
            SENSOR_MAXTYPES = 0,
            PMLOG_CLK_GFXCLK = 1,
            PMLOG_CLK_MEMCLK = 2,
            PMLOG_CLK_SOCCLK = 3,
            PMLOG_CLK_UVDCLK1 = 4,
            PMLOG_CLK_UVDCLK2 = 5,
            PMLOG_CLK_VCECLK = 6,
            PMLOG_CLK_VCNCLK = 7,
            PMLOG_TEMPERATURE_EDGE = 8,
            PMLOG_TEMPERATURE_MEM = 9,
            PMLOG_TEMPERATURE_VRVDDC = 10,
            PMLOG_TEMPERATURE_VRMVDD = 11,
            PMLOG_TEMPERATURE_LIQUID = 12,
            PMLOG_TEMPERATURE_PLX = 13,
            PMLOG_FAN_RPM = 14,
            PMLOG_FAN_PERCENTAGE = 15,
            PMLOG_SOC_VOLTAGE = 16,
            PMLOG_SOC_POWER = 17,
            PMLOG_SOC_CURRENT = 18,
            PMLOG_INFO_ACTIVITY_GFX = 19,
            PMLOG_INFO_ACTIVITY_MEM = 20,
            PMLOG_GFX_VOLTAGE = 21,
            PMLOG_MEM_VOLTAGE = 22,
            PMLOG_ASIC_POWER = 23,
            PMLOG_TEMPERATURE_VRSOC = 24,
            PMLOG_TEMPERATURE_VRMVDD0 = 25,
            PMLOG_TEMPERATURE_VRMVDD1 = 26,
            PMLOG_TEMPERATURE_HOTSPOT = 27
        }
    }
}
