/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2009-2012 Michael Möller <mmoeller@openhardwaremonitor.org>
	Copyright (C) 2011 Christian Vallières
 
*/


namespace OpenHardwareMonitor.Hardware.Nvidia
{
    public enum NvThermalTarget
    {
        NONE = 0,
        GPU = 1,
        MEMORY = 2,
        POWER_SUPPLY = 4,
        BOARD = 8,
        ALL = 15,
        UNKNOWN = -1
    }
}