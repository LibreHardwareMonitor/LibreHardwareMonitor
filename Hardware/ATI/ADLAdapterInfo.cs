/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2009-2012 Michael Möller <mmoeller@openhardwaremonitor.org>
	
*/

using System.Runtime.InteropServices;

namespace OpenHardwareMonitor.Hardware.ATI
{
    [StructLayout(LayoutKind.Sequential)]
  public struct ADLAdapterInfo {
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
}
