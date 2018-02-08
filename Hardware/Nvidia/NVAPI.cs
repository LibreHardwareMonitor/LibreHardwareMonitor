/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2009-2012 Michael Möller <mmoeller@openhardwaremonitor.org>
	Copyright (C) 2011 Christian Vallières
 
*/

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenHardwareMonitor.Hardware.Nvidia
{

    public class NVAPI {

    public const int MAX_PHYSICAL_GPUS = 64;
    public const int SHORT_STRING_MAX = 64;

    public const int MAX_THERMAL_SENSORS_PER_GPU = 3;
    public const int MAX_CLOCKS_PER_GPU = 0x120;
    public const int MAX_PSTATES_PER_GPU = 8;
    public const int MAX_USAGES_PER_GPU = 33;
    public const int MAX_COOLER_PER_GPU = 20;
    public const int MAX_MEMORY_VALUES_PER_GPU = 5;

    public static readonly uint GPU_THERMAL_SETTINGS_VER = (uint)
      Marshal.SizeOf(typeof(NvGPUThermalSettings)) | 0x10000;
    public static readonly uint GPU_CLOCKS_VER = (uint)
      Marshal.SizeOf(typeof(NvClocks)) | 0x20000;
    public static readonly uint GPU_PSTATES_VER = (uint)
      Marshal.SizeOf(typeof(NvPStates)) | 0x10000;
    public static readonly uint GPU_USAGES_VER = (uint)
      Marshal.SizeOf(typeof(NvUsages)) | 0x10000;
    public static readonly uint GPU_COOLER_SETTINGS_VER = (uint)
      Marshal.SizeOf(typeof(NvGPUCoolerSettings)) | 0x20000;
    public static readonly uint GPU_MEMORY_INFO_VER = (uint)
      Marshal.SizeOf(typeof(NvMemoryInfo)) | 0x20000;
    public static readonly uint DISPLAY_DRIVER_VERSION_VER = (uint)
      Marshal.SizeOf(typeof(NvDisplayDriverVersion)) | 0x10000;
    public static readonly uint GPU_COOLER_LEVELS_VER = (uint)
      Marshal.SizeOf(typeof(NvGPUCoolerLevels)) | 0x10000;

    private delegate IntPtr nvapi_QueryInterfaceDelegate(uint id);
    private delegate NvStatus NvAPI_InitializeDelegate();
    private delegate NvStatus NvAPI_GPU_GetFullNameDelegate(
      NvPhysicalGpuHandle gpuHandle, StringBuilder name);

    public delegate NvStatus NvAPI_GPU_GetThermalSettingsDelegate(
      NvPhysicalGpuHandle gpuHandle, int sensorIndex,
      ref NvGPUThermalSettings nvGPUThermalSettings);
    public delegate NvStatus NvAPI_EnumNvidiaDisplayHandleDelegate(int thisEnum,
      ref NvDisplayHandle displayHandle);
    public delegate NvStatus NvAPI_GetPhysicalGPUsFromDisplayDelegate(
      NvDisplayHandle displayHandle, [Out] NvPhysicalGpuHandle[] gpuHandles,
      out uint gpuCount);
    public delegate NvStatus NvAPI_EnumPhysicalGPUsDelegate(
      [Out] NvPhysicalGpuHandle[] gpuHandles, out int gpuCount);
    public delegate NvStatus NvAPI_GPU_GetTachReadingDelegate(
      NvPhysicalGpuHandle gpuHandle, out int value);
    public delegate NvStatus NvAPI_GPU_GetAllClocksDelegate(
      NvPhysicalGpuHandle gpuHandle, ref NvClocks nvClocks);
    public delegate NvStatus NvAPI_GPU_GetPStatesDelegate(
      NvPhysicalGpuHandle gpuHandle, ref NvPStates nvPStates);
    public delegate NvStatus NvAPI_GPU_GetUsagesDelegate(
      NvPhysicalGpuHandle gpuHandle, ref NvUsages nvUsages);
    public delegate NvStatus NvAPI_GPU_GetCoolerSettingsDelegate(
      NvPhysicalGpuHandle gpuHandle, int coolerIndex,
      ref NvGPUCoolerSettings nvGPUCoolerSettings);
    public delegate NvStatus NvAPI_GPU_SetCoolerLevelsDelegate(
      NvPhysicalGpuHandle gpuHandle, int coolerIndex,
      ref NvGPUCoolerLevels NvGPUCoolerLevels);
    public delegate NvStatus NvAPI_GPU_GetMemoryInfoDelegate(
      NvDisplayHandle displayHandle, ref NvMemoryInfo nvMemoryInfo);
    public delegate NvStatus NvAPI_GetDisplayDriverVersionDelegate(
      NvDisplayHandle displayHandle, [In, Out] ref NvDisplayDriverVersion
      nvDisplayDriverVersion);
    public delegate NvStatus NvAPI_GetInterfaceVersionStringDelegate(
      StringBuilder version);
    public delegate NvStatus NvAPI_GPU_GetPCIIdentifiersDelegate(
      NvPhysicalGpuHandle gpuHandle, out uint deviceId, out uint subSystemId, 
      out uint revisionId, out uint extDeviceId);

    private static readonly bool available;
    [DllImport(@"nvapi.dll", EntryPoint = @"nvapi_QueryInterface", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]    private static extern IntPtr NvAPI32_QueryInterface(uint interfaceId);    [DllImport(@"nvapi64.dll", EntryPoint = @"nvapi_QueryInterface", CallingConvention = CallingConvention.Cdecl, PreserveSig = true)]    private static extern IntPtr NvAPI64_QueryInterface(uint interfaceId);
    private static readonly NvAPI_InitializeDelegate NvAPI_Initialize;
    private static readonly NvAPI_GPU_GetFullNameDelegate
      _NvAPI_GPU_GetFullName;
    private static readonly NvAPI_GetInterfaceVersionStringDelegate
      _NvAPI_GetInterfaceVersionString;

    public static readonly NvAPI_GPU_GetThermalSettingsDelegate
      NvAPI_GPU_GetThermalSettings;
    public static readonly NvAPI_EnumNvidiaDisplayHandleDelegate
      NvAPI_EnumNvidiaDisplayHandle;
    public static readonly NvAPI_GetPhysicalGPUsFromDisplayDelegate
      NvAPI_GetPhysicalGPUsFromDisplay;
    public static readonly NvAPI_EnumPhysicalGPUsDelegate
      NvAPI_EnumPhysicalGPUs;
    public static readonly NvAPI_GPU_GetTachReadingDelegate
      NvAPI_GPU_GetTachReading;
    public static readonly NvAPI_GPU_GetAllClocksDelegate
      NvAPI_GPU_GetAllClocks;
    public static readonly NvAPI_GPU_GetPStatesDelegate
      NvAPI_GPU_GetPStates;
    public static readonly NvAPI_GPU_GetUsagesDelegate
      NvAPI_GPU_GetUsages;
    public static readonly NvAPI_GPU_GetCoolerSettingsDelegate
      NvAPI_GPU_GetCoolerSettings;
    public static readonly NvAPI_GPU_SetCoolerLevelsDelegate
      NvAPI_GPU_SetCoolerLevels;
    public static readonly NvAPI_GPU_GetMemoryInfoDelegate
      NvAPI_GPU_GetMemoryInfo;
    public static readonly NvAPI_GetDisplayDriverVersionDelegate
      NvAPI_GetDisplayDriverVersion;
    public static readonly NvAPI_GPU_GetPCIIdentifiersDelegate
      NvAPI_GPU_GetPCIIdentifiers;

    private NVAPI() { }

    public static NvStatus NvAPI_GPU_GetFullName(NvPhysicalGpuHandle gpuHandle,
      out string name) {
      StringBuilder builder = new StringBuilder(SHORT_STRING_MAX);
      NvStatus status;
      if (_NvAPI_GPU_GetFullName != null)
        status = _NvAPI_GPU_GetFullName(gpuHandle, builder);
      else
        status = NvStatus.FUNCTION_NOT_FOUND;
      name = builder.ToString();
      return status;
    }

    public static NvStatus NvAPI_GetInterfaceVersionString(out string version) {
      StringBuilder builder = new StringBuilder(SHORT_STRING_MAX);
      NvStatus status;
      if (_NvAPI_GetInterfaceVersionString != null)
        status = _NvAPI_GetInterfaceVersionString(builder);
      else
        status = NvStatus.FUNCTION_NOT_FOUND;
      version = builder.ToString();
      return status;
    }

    private static string GetDllName() {
      if (IntPtr.Size == 4) {
        return "nvapi.dll";
      } else {
        return "nvapi64.dll";
      }
    }

    private static void GetDelegate<T>(uint id, out T newDelegate)
      where T : class {
      IntPtr ptr;
      if (IntPtr.Size == 4)
      {
          ptr = NvAPI32_QueryInterface(id);
      }
      else
      {
          ptr = NvAPI64_QueryInterface(id);
      }
      if (ptr != IntPtr.Zero) {
        newDelegate =
          Marshal.GetDelegateForFunctionPointer(ptr, typeof(T)) as T;
      } else {
        newDelegate = null;
      }
    }

    static NVAPI() {
      try {
        GetDelegate(0x0150E828, out NvAPI_Initialize);
      } catch (DllNotFoundException) { return; } 
        catch (EntryPointNotFoundException) { return; } 
        catch (ArgumentNullException) { return; }

      if (NvAPI_Initialize() == NvStatus.OK) {
        GetDelegate(0xE3640A56, out NvAPI_GPU_GetThermalSettings);
        GetDelegate(0xCEEE8E9F, out _NvAPI_GPU_GetFullName);
        GetDelegate(0x9ABDD40D, out NvAPI_EnumNvidiaDisplayHandle);
        GetDelegate(0x34EF9506, out NvAPI_GetPhysicalGPUsFromDisplay);
        GetDelegate(0xE5AC921F, out NvAPI_EnumPhysicalGPUs);
        GetDelegate(0x5F608315, out NvAPI_GPU_GetTachReading);
        GetDelegate(0x1BD69F49, out NvAPI_GPU_GetAllClocks);
        GetDelegate(0x60DED2ED, out NvAPI_GPU_GetPStates);
        GetDelegate(0x189A1FDF, out NvAPI_GPU_GetUsages);
        GetDelegate(0xDA141340, out NvAPI_GPU_GetCoolerSettings);
        GetDelegate(0x891FA0AE, out NvAPI_GPU_SetCoolerLevels);
        GetDelegate(0x774AA982, out NvAPI_GPU_GetMemoryInfo);
        GetDelegate(0xF951A4D1, out NvAPI_GetDisplayDriverVersion);
        GetDelegate(0x01053FA5, out _NvAPI_GetInterfaceVersionString);
        GetDelegate(0x2DDFB66E, out NvAPI_GPU_GetPCIIdentifiers);

        available = true;
      }
    }

    public static bool IsAvailable {
      get { return available; }
    }

  }
}
