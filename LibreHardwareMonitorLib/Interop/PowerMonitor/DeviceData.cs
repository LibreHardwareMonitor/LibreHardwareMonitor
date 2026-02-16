// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Linq;

namespace LibreHardwareMonitor.Interop.PowerMonitor;

public sealed class DeviceData
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public bool Connected { get; set; }

    public string HardwareRevision { get; set; } = "A0";

    public string FirmwareVersion { get; set; } = "0.0.0";

    public double[] PinVoltage { get; set; } = new double[6];

    public double[] PinCurrent { get; set; } = new double[6];

    public double OnboardTempInC { get; set; }

    public double OnboardTempOutC { get; set; }

    public double ExternalTemp1C { get; set; }

    public double ExternalTemp2C { get; set; }

    public int PsuCapabilityW { get; set; }

    public double SumCurrentA => PinCurrent.Sum();

    public double SumPowerW => PinVoltage.Zip(PinCurrent, (v, i) => v * i).Sum();

    public ushort FaultStatus { get; set; }

    public ushort FaultLog { get; set; }

    public DeviceConfigStructV2 Config;
}
