﻿/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2009-2010 Michael Möller <mmoeller@openhardwaremonitor.org>
	
*/

namespace OpenHardwareMonitor.Hardware {

  public delegate void HardwareEventHandler(IHardware hardware);

  public interface IComputer : IElement {

    IHardware[] Hardware { get; }

    bool MainboardEnabled { get; }
    bool CPUEnabled { get; }
    bool RAMEnabled { get; }
    bool GPUEnabled { get; }
    bool FanControllerEnabled { get; }
    bool HDDEnabled { get; }
    bool NICEnabled { get; }


    string GetReport();

    event HardwareEventHandler HardwareAdded;
    event HardwareEventHandler HardwareRemoved;
  }
}
