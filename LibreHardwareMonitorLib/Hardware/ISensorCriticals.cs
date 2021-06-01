// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;

namespace LibreHardwareMonitor.Hardware
{
    public interface ISensorNormalLimits
    {
        float? LowLimit { get; }
        float? HighLimit { get; }
    }

    public interface ISensorCriticalLimiits
    {
        float? LowCriticalLimit { get; }
        float? HighCriticalLimit { get; }
    }
}
