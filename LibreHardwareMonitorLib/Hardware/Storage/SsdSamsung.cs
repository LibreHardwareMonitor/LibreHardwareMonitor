// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Collections.Generic;

namespace LibreHardwareMonitor.Hardware.Storage;

[NamePrefix(""), RequireSmart(0xB1), RequireSmart(0xB3), RequireSmart(0xB5), RequireSmart(0xB6), RequireSmart(0xB7), RequireSmart(0xBB), RequireSmart(0xC3), RequireSmart(0xC7)]
internal class SsdSamsung : AtaStorage
{
    private static readonly IReadOnlyList<SmartAttribute> _smartAttributes = new List<SmartAttribute>
    {
        new(0x05, SmartNames.ReallocatedSectorsCount),
        new(0x09, SmartNames.PowerOnHours, RawToInt),
        new(0x0C, SmartNames.PowerCycleCount, RawToInt),
        new(0xAF, SmartNames.ProgramFailCountChip, RawToInt),
        new(0xB0, SmartNames.EraseFailCountChip, RawToInt),
        new(0xB1, SmartNames.WearLevelingCount, RawToInt),
        new(0xB2, SmartNames.UsedReservedBlockCountChip, RawToInt),
        new(0xB3, SmartNames.UsedReservedBlockCountTotal, RawToInt),

        // Unused Reserved Block Count (Total)
        new(0xB4, SmartNames.RemainingLife, null, SensorType.Level, 0, SmartNames.RemainingLife),
        new(0xB5, SmartNames.ProgramFailCountTotal, RawToInt),
        new(0xB6, SmartNames.EraseFailCountTotal, RawToInt),
        new(0xB7, SmartNames.RuntimeBadBlockTotal, RawToInt),
        new(0xBB, SmartNames.UncorrectableErrorCount, RawToInt),
        new(0xBE,
            SmartNames.Temperature,
            (r, _, p) => r[0] + (p?[0].Value ?? 0),
            SensorType.Temperature,
            0,
            SmartNames.Temperature,
            false,
            new[] { new ParameterDescription("Offset [°C]", "Temperature offset of the thermal sensor.\nTemperature = Value + Offset.", 0) }),
        new(0xC2, SmartNames.AirflowTemperature),
        new(0xC3, SmartNames.EccRate),
        new(0xC6, SmartNames.OffLineUncorrectableErrorCount, RawToInt),
        new(0xC7, SmartNames.CrcErrorCount, RawToInt),
        new(0xC9, SmartNames.SupercapStatus),
        new(0xCA, SmartNames.ExceptionModeStatus),
        new(0xEB, SmartNames.PowerRecoveryCount),
        new(0xF1,
            SmartNames.TotalLbasWritten,
            (r, _, _) => (((long)r[5] << 40) | ((long)r[4] << 32) | ((long)r[3] << 24) | ((long)r[2] << 16) | ((long)r[1] << 8) | r[0]) * (512.0f / 1024 / 1024 / 1024),
            SensorType.Data,
            0,
            "Total Bytes Written")
    };

    public SsdSamsung(StorageInfo storageInfo, ISmart smart, string name, string firmwareRevision, int index, ISettings settings)
        : base(storageInfo, smart, name, firmwareRevision, "ssd", index, _smartAttributes, settings)
    { }
}
