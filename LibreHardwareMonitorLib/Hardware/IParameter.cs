// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware
{
    public interface IParameter : IElement
    {
        float DefaultValue { get; }

        string Description { get; }

        Identifier Identifier { get; }

        bool IsDefault { get; set; }

        string Name { get; }

        ISensor Sensor { get; }

        float Value { get; set; }
    }
}
