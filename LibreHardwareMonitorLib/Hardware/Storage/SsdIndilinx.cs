// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Collections.Generic;

namespace LibreHardwareMonitor.Hardware.Storage;

[NamePrefix(""), RequireSmart(0x01), RequireSmart(0x09), RequireSmart(0x0C), RequireSmart(0xD1), RequireSmart(0xCE), RequireSmart(0xCF)]
internal class SsdIndilinx : AtaStorage
{
    private static readonly IReadOnlyList<SmartAttribute> _smartAttributes = new List<SmartAttribute>
    {
        new(0x01, SmartNames.ReadErrorRate),
        new(0x09, SmartNames.PowerOnHours),
        new(0x0C, SmartNames.PowerCycleCount),
        new(0xB8, SmartNames.InitialBadBlockCount),
        new(0xC3, SmartNames.ProgramFailure),
        new(0xC4, SmartNames.EraseFailure),
        new(0xC5, SmartNames.ReadFailure),
        new(0xC6, SmartNames.SectorsRead),
        new(0xC7, SmartNames.SectorsWritten),
        new(0xC8, SmartNames.ReadCommands),
        new(0xC9, SmartNames.WriteCommands),
        new(0xCA, SmartNames.BitErrors),
        new(0xCB, SmartNames.CorrectedErrors),
        new(0xCC, SmartNames.BadBlockFullFlag),
        new(0xCD, SmartNames.MaxCellCycles),
        new(0xCE, SmartNames.MinErase),
        new(0xCF, SmartNames.MaxErase),
        new(0xD0, SmartNames.AverageEraseCount),
        new(0xD1, SmartNames.RemainingLife, null, SensorType.Level, 0, SmartNames.RemainingLife),
        new(0xD2, SmartNames.UnknownUnique),
        new(0xD3, SmartNames.SataErrorCountCrc),
        new(0xD4, SmartNames.SataErrorCountHandshake)
    };

    public SsdIndilinx(StorageInfo storageInfo, ISmart smart, string name, string firmwareRevision, int index, ISettings settings)
        : base(storageInfo, smart, name, firmwareRevision, "ssd", index, _smartAttributes, settings)
    { }
}
