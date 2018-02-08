/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2009-2012 Michael Möller <mmoeller@openhardwaremonitor.org>
	Copyright (C) 2011 Christian Vallières
 
*/

using System.Runtime.InteropServices;

namespace OpenHardwareMonitor.Hardware.Nvidia
{
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct NvGPUCoolerSettings
    {
        public uint Version;
        public uint Count;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NVAPI.MAX_COOLER_PER_GPU)] public NvCooler[] Cooler;
    }
}