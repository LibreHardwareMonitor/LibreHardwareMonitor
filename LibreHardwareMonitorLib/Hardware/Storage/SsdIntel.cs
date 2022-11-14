// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Collections.Generic;

namespace LibreHardwareMonitor.Hardware.Storage;

[NamePrefix("INTEL SSD"), RequireSmart(0xE1), RequireSmart(0xE8), RequireSmart(0xE9)]
internal class SsdIntel : AtaStorage
{
    private static readonly IReadOnlyList<SmartAttribute> _smartAttributes = new List<SmartAttribute>
    {
        new(0x01, SmartNames.ReadErrorRate),
        new(0x03, SmartNames.SpinUpTime),
        new(0x04, SmartNames.StartStopCount, RawToInt),
        new(0x05, SmartNames.ReallocatedSectorsCount),
        new(0x09, SmartNames.PowerOnHours, RawToInt),
        new(0x0C, SmartNames.PowerCycleCount, RawToInt),
        new(0xAA, SmartNames.AvailableReservedSpace),
        new(0xAB, SmartNames.ProgramFailCount),
        new(0xAC, SmartNames.EraseFailCount),
        new(0xAE, SmartNames.UnexpectedPowerLossCount, RawToInt),
        new(0xB7, SmartNames.SataDownshiftErrorCount, RawToInt),
        new(0xB8, SmartNames.EndToEndError),
        new(0xBB, SmartNames.UncorrectableErrorCount, RawToInt),
        new(0xBE,
            SmartNames.Temperature,
            (r, _, p) => r[0] + (p?[0].Value ?? 0),
            SensorType.Temperature,
            0,
            SmartNames.AirflowTemperature,
            false,
            new[] { new ParameterDescription("Offset [°C]", "Temperature offset of the thermal sensor.\nTemperature = Value + Offset.", 0) }),
        new(0xC0, SmartNames.UnsafeShutdownCount),
        new(0xC7, SmartNames.CrcErrorCount, RawToInt),
        new(0xE1, SmartNames.HostWrites, (r, v, p) => RawToInt(r, v, p) / 0x20, SensorType.Data, 0, SmartNames.HostWrites),
        new(0xE8, SmartNames.RemainingLife, null, SensorType.Level, 0, SmartNames.RemainingLife),
        new(0xE9, SmartNames.MediaWearOutIndicator),
        new(0xF1, SmartNames.HostWrites, (r, v, p) => RawToInt(r, v, p) / 0x20, SensorType.Data, 0, SmartNames.HostWrites),
        new(0xF2, SmartNames.HostReads, (r, v, p) => RawToInt(r, v, p) / 0x20, SensorType.Data, 1, SmartNames.HostReads)
    };

    public SsdIntel(StorageInfo storageInfo, ISmart smart, string name, string firmwareRevision, int index, ISettings settings)
        : base(storageInfo, smart, name, firmwareRevision, "ssd", index, _smartAttributes, settings)
    { }
}
