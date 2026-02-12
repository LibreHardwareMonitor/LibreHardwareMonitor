// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) Florian K. (Blacktempel)
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware.Controller.MSI;

public enum MsiFanMode : byte
{
    Silent = 0,
    Bios = 1,
    Game = 2,
    Custom = 3,
    Unknown = 4,
    Smart = 5,
}
