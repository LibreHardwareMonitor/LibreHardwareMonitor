// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace LibreHardwareMonitor.Hardware.Memory
{
    internal enum ManufacturereID : ushort
    {
        Unknown = 0,

        OnSemi = 0x1B09,
        MicroChip = 0x0054,
        ST = 0x104A,
    }

    internal class DimmSensor : Sensor
    {
        protected static ushort[] _manufacturerID = {
            0x1B09,     // On Semiconductor
            0x0054,     // MicroChip
            0x104A,     // ST
            0x00B3,     // RENESAS
            0x1131,     // NXP
            0x1C85,     // ABLIC

            // please, add more
        };

        protected byte _address;

        public DimmSensor(string name, int index, Hardware hardware, ISettings settings, byte address) : base(name, index, SensorType.Temperature, hardware, settings)
        {
            _address = address;
        }

        public virtual void UpdateSensor()
        {

        }
    }
}
