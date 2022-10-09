// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware;

public enum ControlMode
{
    Undefined,
    Software,
    Default
}

public interface IControl
{
    ControlMode ControlMode { get; }

    Identifier Identifier { get; }

    float MaxSoftwareValue { get; }

    float MinSoftwareValue { get; }

    ISensor Sensor { get; }

    float SoftwareValue { get; }

    void SetDefault();

    void SetSoftware(float value);
}