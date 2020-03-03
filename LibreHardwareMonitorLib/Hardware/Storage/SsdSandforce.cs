﻿// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using LibreHardwareMonitor.Interop;

namespace LibreHardwareMonitor.Hardware.Storage
{
    [NamePrefix(""), RequireSmart(0xAB), RequireSmart(0xB1)]
    internal class SsdSandforce : AtaStorage
    {
        private static readonly IReadOnlyList<SmartAttribute> _smartAttributes = new List<SmartAttribute>
        {
            new SmartAttribute(0x01, SmartNames.RawReadErrorRate),
            new SmartAttribute(0x05, SmartNames.RetiredBlockCount, RawToInt),
            new SmartAttribute(0x09, SmartNames.PowerOnHours, RawToInt),
            new SmartAttribute(0x0C, SmartNames.PowerCycleCount, RawToInt),
            new SmartAttribute(0xAB, SmartNames.ProgramFailCount, RawToInt),
            new SmartAttribute(0xAC, SmartNames.EraseFailCount, RawToInt),
            new SmartAttribute(0xAE, SmartNames.UnexpectedPowerLossCount, RawToInt),
            new SmartAttribute(0xB1, SmartNames.WearRangeDelta, RawToInt),
            new SmartAttribute(0xB5, SmartNames.AlternativeProgramFailCount, RawToInt),
            new SmartAttribute(0xB6, SmartNames.AlternativeEraseFailCount, RawToInt),
            new SmartAttribute(0xBB, SmartNames.UncorrectableErrorCount, RawToInt),
            new SmartAttribute(0xC2,
                               SmartNames.Temperature,
                               (raw, value, p) => value + (p?[0].Value ?? 0),
                               SensorType.Temperature,
                               0,
                               SmartNames.Temperature,
                               true,
                               new[] { new ParameterDescription("Offset [°C]", "Temperature offset of the thermal sensor.\nTemperature = Value + Offset.", 0) }),
            new SmartAttribute(0xC3, SmartNames.UnrecoverableEcc),
            new SmartAttribute(0xC4, SmartNames.ReallocationEventCount, RawToInt),
            new SmartAttribute(0xE7, SmartNames.RemainingLife, null, SensorType.Level, 0, SmartNames.RemainingLife),
            new SmartAttribute(0xE9, SmartNames.ControllerWritesToNand, RawToInt, SensorType.Data, 0, SmartNames.ControllerWritesToNand),
            new SmartAttribute(0xEA, SmartNames.HostWritesToController, RawToInt, SensorType.Data, 1, SmartNames.HostWritesToController),
            new SmartAttribute(0xF1, SmartNames.HostWrites, RawToInt, SensorType.Data, 1, SmartNames.HostWrites),
            new SmartAttribute(0xF2, SmartNames.HostReads, RawToInt, SensorType.Data, 2, SmartNames.HostReads)
        };

        private readonly Sensor _writeAmplification;

        public SsdSandforce(StorageInfo storageInfo, ISmart smart, string name, string firmwareRevision, int index, ISettings settings)
            : base(storageInfo, smart, name, firmwareRevision, "ssd", index, _smartAttributes, settings)
        {
            _writeAmplification = new Sensor("Write Amplification", 1, SensorType.Factor, this, settings);
        }

        protected override void UpdateAdditionalSensors(Kernel32.SMART_ATTRIBUTE[] values)
        {
            float? controllerWritesToNand = null;
            float? hostWritesToController = null;
            foreach (Kernel32.SMART_ATTRIBUTE value in values)
            {
                if (value.Id == 0xE9)
                    controllerWritesToNand = RawToInt(value.RawValue, value.CurrentValue, null);

                if (value.Id == 0xEA)
                    hostWritesToController = RawToInt(value.RawValue, value.CurrentValue, null);
            }

            if (controllerWritesToNand.HasValue && hostWritesToController.HasValue)
            {
                if (hostWritesToController.Value > 0)
                    _writeAmplification.Value = controllerWritesToNand.Value / hostWritesToController.Value;
                else
                    _writeAmplification.Value = 0;

                ActivateSensor(_writeAmplification);
            }
        }
    }
}
