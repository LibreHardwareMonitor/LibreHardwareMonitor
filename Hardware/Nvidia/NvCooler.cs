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
    public struct NvCooler
    {
        public int Type;
        public int Controller;
        public int DefaultMin;
        public int DefaultMax;
        public int CurrentMin;
        public int CurrentMax;
        public int CurrentLevel;
        public int DefaultPolicy;
        public int CurrentPolicy;
        public int Target;
        public int ControlType;
        public int Active;
    }
}