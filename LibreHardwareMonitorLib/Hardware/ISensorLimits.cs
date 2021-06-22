// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware
{
    public interface ISensorLimits
    {
        float? HighLimit { get; }

        float? LowLimit { get; }
    }

    public interface ICriticalSensorLimits
    {
        float? CriticalHighLimit { get; }

        float? CriticalLowLimit { get; }
    }
}
