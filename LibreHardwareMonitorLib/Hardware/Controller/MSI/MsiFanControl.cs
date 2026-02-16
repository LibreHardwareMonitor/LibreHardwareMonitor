// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) Florian K. (Blacktempel)
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware.Controller.MSI;

public class MsiFanControl
{
    public MsiFan Fan1 { get; set; } = new();
    public MsiFan Fan2 { get; set; } = new();
    public MsiFan Fan3 { get; set; } = new();
    public MsiFan Fan4 { get; set; } = new();
    public MsiFan Fan5 { get; set; } = new();

    public int TemperatureInlet { get; set; }
    public int TemperatureOutlet { get; set; }

    public int TemperatureSensor1 { get; set; }
    public int TemperatureSensor2 { get; set; }
}
