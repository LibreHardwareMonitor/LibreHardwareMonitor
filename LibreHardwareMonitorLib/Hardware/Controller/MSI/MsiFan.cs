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

    /// <summary>
    /// Speed of Fan in percentage 0-100. This can e.g. be used to set fan curve when <see cref="MsiFanConfigure.Mode"/> is <see cref="MsiFanMode.Custom"/>.
    /// </summary>
    public MsiFanConfigure ConfigureDuty;

    /// <summary>
    /// Temperature of Fan in degrees Celsius. This can e.g. be used to set fan curve when <see cref="MsiFanConfigure.Mode"/> is <see cref="MsiFanMode.Custom"/>.
    /// </summary>
    public MsiFanConfigure ConfigureTemp;
}

public struct MsiFanConfigure
{
    public MsiFanConfigure()
    {
    }

    public MsiFanMode Mode = MsiFanMode.Unknown;
    public byte Item0;
    public byte Item1;
    public byte Item2;
    public byte Item3;
    public byte Item4;
    public byte Item5;
    public byte Item6;
}
