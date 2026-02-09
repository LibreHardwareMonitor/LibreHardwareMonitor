// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) Florian K. (Blacktempel)
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware.Controller.MSI;

public class MsiFan
{
    /// <summary>
    /// Speed of Fan in RPM.
    /// </summary>
    public int Speed { get; set; }
    public int Duty { get; set; }
    public MsiFanMode Mode { get; set; } = MsiFanMode.Unknown;

    /// <summary>
    /// Speed of Fan in percentage 0-100. This can e.g. be used to set fan curve when <see cref="Mode"/> is <see cref="MsiFanMode.Custom"/>.
    /// </summary>
    public byte Configure_Duty_0 { get; set; }
    public byte Configure_Duty_1 { get; set; }
    public byte Configure_Duty_2 { get; set; }
    public byte Configure_Duty_3 { get; set; }
    public byte Configure_Duty_4 { get; set; }
    public byte Configure_Duty_5 { get; set; }
    public byte Configure_Duty_6 { get; set; }

    /// <summary>
    /// Temperature of Fan in degrees Celsius. This can e.g. be used to set fan curve when <see cref="Mode"/> is <see cref="MsiFanMode.Custom"/>.
    /// </summary>
    public byte Configure_Temp_0 { get; set; }
    public byte Configure_Temp_1 { get; set; }
    public byte Configure_Temp_2 { get; set; }
    public byte Configure_Temp_3 { get; set; }
    public byte Configure_Temp_4 { get; set; }
    public byte Configure_Temp_5 { get; set; }
    public byte Configure_Temp_6 { get; set; }
}
