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
    public byte ConfigureDuty0 { get; set; }
    public byte ConfigureDuty1 { get; set; }
    public byte ConfigureDuty2 { get; set; }
    public byte ConfigureDuty3 { get; set; }
    public byte ConfigureDuty4 { get; set; }
    public byte ConfigureDuty5 { get; set; }
    public byte ConfigureDuty6 { get; set; }

    /// <summary>
    /// Temperature of Fan in degrees Celsius. This can e.g. be used to set fan curve when <see cref="Mode"/> is <see cref="MsiFanMode.Custom"/>.
    /// </summary>
    public byte ConfigureTemp0 { get; set; }
    public byte ConfigureTemp1 { get; set; }
    public byte ConfigureTemp2 { get; set; }
    public byte ConfigureTemp3 { get; set; }
    public byte ConfigureTemp4 { get; set; }
    public byte ConfigureTemp5 { get; set; }
    public byte ConfigureTemp6 { get; set; }
}
