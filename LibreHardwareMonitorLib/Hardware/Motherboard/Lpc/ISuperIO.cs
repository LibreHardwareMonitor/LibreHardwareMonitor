// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc;

internal interface ISuperIO
{
    Chip Chip { get; }

    float?[] Controls { get; }

    float?[] Fans { get; }

    float?[] Temperatures { get; }

    // get voltage, temperature, fan and control channel values
    float?[] Voltages { get; }

    // set control value, null = auto
    void SetControl(int index, byte? value);

    // read and write GPIO
    byte? ReadGpio(int index);

    void WriteGpio(int index, byte value);

    string GetReport();

    void Update();
}