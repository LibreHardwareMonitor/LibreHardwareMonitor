﻿// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Collections.Generic;

namespace LibreHardwareMonitor.Hardware.Storage
{
    [NamePrefix(""), RequireSmart(0x01), RequireSmart(0x09), RequireSmart(0x0C), RequireSmart(0xD1), RequireSmart(0xCE), RequireSmart(0xCF)]
    internal class SsdIndilinx : AtaStorage
    {
        private static readonly IReadOnlyList<SmartAttribute> _smartAttributes = new List<SmartAttribute>
        {
            new SmartAttribute(0x01, SmartNames.ReadErrorRate),
            new SmartAttribute(0x09, SmartNames.PowerOnHours),
            new SmartAttribute(0x0C, SmartNames.PowerCycleCount),
            new SmartAttribute(0xB8, SmartNames.InitialBadBlockCount),
            new SmartAttribute(0xC3, SmartNames.ProgramFailure),
            new SmartAttribute(0xC4, SmartNames.EraseFailure),
            new SmartAttribute(0xC5, SmartNames.ReadFailure),
            new SmartAttribute(0xC6, SmartNames.SectorsRead),
            new SmartAttribute(0xC7, SmartNames.SectorsWritten),
            new SmartAttribute(0xC8, SmartNames.ReadCommands),
            new SmartAttribute(0xC9, SmartNames.WriteCommands),
            new SmartAttribute(0xCA, SmartNames.BitErrors),
            new SmartAttribute(0xCB, SmartNames.CorrectedErrors),
            new SmartAttribute(0xCC, SmartNames.BadBlockFullFlag),
            new SmartAttribute(0xCD, SmartNames.MaxCellCycles),
            new SmartAttribute(0xCE, SmartNames.MinErase),
            new SmartAttribute(0xCF, SmartNames.MaxErase),
            new SmartAttribute(0xD0, SmartNames.AverageEraseCount),
            new SmartAttribute(0xD1, SmartNames.RemainingLife, null, SensorType.Level, 0, SmartNames.RemainingLife),
            new SmartAttribute(0xD2, SmartNames.UnknownUnique),
            new SmartAttribute(0xD3, SmartNames.SataErrorCountCrc),
            new SmartAttribute(0xD4, SmartNames.SataErrorCountHandshake)
        };

        public SsdIndilinx(StorageInfo storageInfo, ISmart smart, string name, string firmwareRevision, int index, ISettings settings)
            : base(storageInfo, smart, name, firmwareRevision, "ssd", index, _smartAttributes, settings)
        { }
    }
}
