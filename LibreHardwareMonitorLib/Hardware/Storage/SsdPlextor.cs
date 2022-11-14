// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Collections.Generic;

namespace LibreHardwareMonitor.Hardware.Storage;

[NamePrefix("PLEXTOR")]
internal class SsdPlextor : AtaStorage
{
    private static readonly IReadOnlyList<SmartAttribute> _smartAttributes = new List<SmartAttribute>
    {
        new(0x09, SmartNames.PowerOnHours, RawToInt),
        new(0x0C, SmartNames.PowerCycleCount, RawToInt),
        new(0xF1, SmartNames.HostWrites, RawToGb, SensorType.Data, 0, SmartNames.HostWrites),
        new(0xF2, SmartNames.HostReads, RawToGb, SensorType.Data, 1, SmartNames.HostReads)
    };

    public SsdPlextor(StorageInfo storageInfo, ISmart smart, string name, string firmwareRevision, int index, ISettings settings)
        : base(storageInfo, smart, name, firmwareRevision, "ssd", index, _smartAttributes, settings)
    { }

    private static float RawToGb(byte[] rawValue, byte value, IReadOnlyList<IParameter> parameters)
    {
        return RawToInt(rawValue, value, parameters) / 32;
    }
}
