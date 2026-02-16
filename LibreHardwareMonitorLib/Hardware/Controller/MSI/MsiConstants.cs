// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) Florian K. (Blacktempel)
// All Rights Reserved.

using System.Collections.Generic;

namespace LibreHardwareMonitor.Hardware.Controller.MSI;

internal static class MsiConstants
{
    public static readonly IReadOnlyList<MsiDevice> SupportedDevices = new List<MsiDevice>
    {
        new MsiDevice(MsiDeviceType.S280, 0x0DB0, 0x75B6, 0x6A04),
        new MsiDevice(MsiDeviceType.S360, 0x0DB0, 0x9BA6, 0x6A05),
        new MsiDevice(MsiDeviceType.S360MEG, 0x1462, 0x9BA6, 0x6A05),
        new MsiDevice(MsiDeviceType.X360, 0x0DB0, 0x5259, 0x6A11),
        new MsiDevice(MsiDeviceType.X240, 0x0DB0, 0xC7B2, 0x6A10),
        new MsiDevice(MsiDeviceType.D360, 0x0DB0, 0x8DBF, 0x6A15),
        new MsiDevice(MsiDeviceType.D240, 0x0DB0, 0xD085, 0x6A16),
    };
}
