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
    internal class DimmSensor : Sensor
    {
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
