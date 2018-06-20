/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2009-2012 Michael Möller <mmoeller@openhardwaremonitor.org>
	
*/

using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace OpenHardwareMonitor.Hardware.ATI {
  
  [StructLayout(LayoutKind.Sequential)]
  internal struct ADLAdapterInfo {
    public int Size;
    public int AdapterIndex;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL.ADL_MAX_PATH)]
    public string UDID;
    public int BusNumber;
    public int DeviceNumber;
    public int FunctionNumber;
    public int VendorID;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL.ADL_MAX_PATH)]
    public string AdapterName;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL.ADL_MAX_PATH)]
    public string DisplayName;
    public int Present;
    public int Exist;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL.ADL_MAX_PATH)]
    public string DriverPath;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL.ADL_MAX_PATH)]
    public string DriverPathExt;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL.ADL_MAX_PATH)]
    public string PNPString;
    public int OSDisplayIndex;
  }

  [StructLayout(LayoutKind.Sequential)]
  internal struct ADLPMActivity {
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
  internal struct ADLTemperature {
    public int Size;
    public int Temperature;
  }

  [StructLayout(LayoutKind.Sequential)]
  internal struct ADLFanSpeedValue {
    public int Size;
    public int SpeedType;
    public int FanSpeed;
    public int Flags;
  }

  [StructLayout(LayoutKind.Sequential)]
  internal struct ADLFanSpeedInfo {
    public int Size;
    public int Flags;
    public int MinPercent;
    public int MaxPercent;
    public int MinRPM;
    public int MaxRPM;
  }

  internal class ADL {
    public const int ADL_MAX_PATH = 256;
    public const int ADL_MAX_ADAPTERS = 40;
    public const int ADL_MAX_DISPLAYS = 40;
    public const int ADL_MAX_DEVICENAME = 32;
    public const int ADL_OK = 0;
    public const int ADL_ERR = -1;
    public const int ADL_DRIVER_OK = 0;
    public const int ADL_MAX_GLSYNC_PORTS = 8;
    public const int ADL_MAX_GLSYNC_PORT_LEDS = 8;
    public const int ADL_MAX_NUM_DISPLAYMODES = 1024;

    public const int ADL_DL_FANCTRL_SPEED_TYPE_PERCENT = 1;
    public const int ADL_DL_FANCTRL_SPEED_TYPE_RPM = 2;

    public const int ADL_DL_FANCTRL_SUPPORTS_PERCENT_READ = 1;
    public const int ADL_DL_FANCTRL_SUPPORTS_PERCENT_WRITE = 2;
    public const int ADL_DL_FANCTRL_SUPPORTS_RPM_READ = 4;
    public const int ADL_DL_FANCTRL_SUPPORTS_RPM_WRITE = 8;
    public const int ADL_DL_FANCTRL_FLAG_USER_DEFINED_SPEED = 1;

    public const int ATI_VENDOR_ID = 0x1002;

    internal const string Atiadlxx_FileName = "atiadlxx.dll";
    [DllImport(Atiadlxx_FileName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int ADL_Main_Control_Create(ADL_Main_Memory_AllocDelegate callback, int enumConnectedAdapters);
    [DllImport(Atiadlxx_FileName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int ADL_Main_Control_Destroy();
    [DllImport(Atiadlxx_FileName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int ADL_Adapter_AdapterInfo_Get(IntPtr info, int size);
    [DllImport(Atiadlxx_FileName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int ADL_Adapter_NumberOfAdapters_Get();
    [DllImport(Atiadlxx_FileName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int ADL_Adapter_NumberOfAdapters_Get(ref int numAdapters);
    [DllImport(Atiadlxx_FileName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int ADL_Adapter_ID_Get(int adapterIndex, out int adapterID);
    [DllImport(Atiadlxx_FileName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int ADL_Display_AdapterID_Get(int adapterIndex, out int adapterID);
    [DllImport(Atiadlxx_FileName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int ADL_Adapter_Active_Get(int adapterIndex, out int status);
    [DllImport(Atiadlxx_FileName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int ADL_Overdrive5_CurrentActivity_Get(int iAdapterIndex, ref ADLPMActivity activity);
    [DllImport(Atiadlxx_FileName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int ADL_Overdrive5_Temperature_Get(int adapterIndex, int thermalControllerIndex, ref ADLTemperature temperature);
    [DllImport(Atiadlxx_FileName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int ADL_Overdrive5_FanSpeed_Get(int adapterIndex, int thermalControllerIndex, ref ADLFanSpeedValue fanSpeedValue);
    [DllImport(Atiadlxx_FileName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int ADL_Overdrive5_FanSpeedInfo_Get(int adapterIndex, int thermalControllerIndex, ref ADLFanSpeedInfo fanSpeedInfo);
    [DllImport(Atiadlxx_FileName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int ADL_Overdrive5_FanSpeedToDefault_Set(int adapterIndex, int thermalControllerIndex);
    [DllImport(Atiadlxx_FileName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int ADL_Overdrive5_FanSpeed_Set(int adapterIndex, int thermalControllerIndex, ref ADLFanSpeedValue fanSpeedValue);

    public static int ADL_Main_Control_Create(int enumConnectedAdapters) {
      try {
        return ADL_Main_Control_Create(Main_Memory_Alloc,
        enumConnectedAdapters);
      } catch {
        return ADL_ERR;
      }
    }

    public static int ADL_Adapter_AdapterInfo_Get(ADLAdapterInfo[] info) {
      int elementSize = Marshal.SizeOf(typeof(ADLAdapterInfo));
      int size = info.Length * elementSize;
      IntPtr ptr = Marshal.AllocHGlobal(size);
      int result = ADL_Adapter_AdapterInfo_Get(ptr, size);
      for (int i = 0; i < info.Length; i++)
        info[i] = (ADLAdapterInfo)
          Marshal.PtrToStructure((IntPtr)((long)ptr + i * elementSize),
          typeof(ADLAdapterInfo));
      Marshal.FreeHGlobal(ptr);

      // the ADLAdapterInfo.VendorID field reported by ADL is wrong on 
      // Windows systems (parse error), so we fix this here
      for (int i = 0; i < info.Length; i++) {
        // try Windows UDID format
        Match m = Regex.Match(info[i].UDID, "PCI_VEN_([A-Fa-f0-9]{1,4})&.*");
        if (m.Success && m.Groups.Count == 2) {
          info[i].VendorID = Convert.ToInt32(m.Groups[1].Value, 16);
          continue;
        }
        // if above failed, try Unix UDID format
        m = Regex.Match(info[i].UDID, "[0-9]+:[0-9]+:([0-9]+):[0-9]+:[0-9]+");
        if (m.Success && m.Groups.Count == 2) {
          info[i].VendorID = Convert.ToInt32(m.Groups[1].Value, 10);
        }
      }

      return result;
    }

    public delegate IntPtr ADL_Main_Memory_AllocDelegate(int size);

        // create a Main_Memory_Alloc delegate and keep it alive
        public static ADL_Main_Memory_AllocDelegate Main_Memory_Alloc =
      delegate(int size) {
        return Marshal.AllocHGlobal(size);
      };

    public static void Main_Memory_Free(IntPtr buffer) {
      if (IntPtr.Zero != buffer)
        Marshal.FreeHGlobal(buffer);
    }
  }
}
