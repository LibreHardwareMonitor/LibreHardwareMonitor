﻿// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

namespace LibreHardwareMonitor.Hardware
{
    public enum ControlMode
    {
        Undefined,
        Software,
        Default
    }

    public interface IControl
    {
        Identifier Identifier { get; }
        ControlMode ControlMode { get; }
        float SoftwareValue { get; }
        void SetDefault();
        float MinSoftwareValue { get; }
        float MaxSoftwareValue { get; }
        void SetSoftware(float value);
    }
}
