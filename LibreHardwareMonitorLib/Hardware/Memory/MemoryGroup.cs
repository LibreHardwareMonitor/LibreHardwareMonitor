// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Collections.Generic;

namespace LibreHardwareMonitor.Hardware.Memory
{
    internal class MemoryGroup : IGroup
    {
        private readonly Hardware[] _hardware;

        public MemoryGroup(ISettings settings)
        {
            if (Software.OperatingSystem.IsUnix)
            {
                _hardware = new Hardware[] { new GenericLinuxMemory("Generic Memory", settings) };
            }
            else
            {
                _hardware = new Hardware[] { new GenericWindowsMemory("Generic Memory", settings) };
            }
        }

        public string GetReport()
        {
            return null;
        }

        public IReadOnlyList<IHardware> Hardware => _hardware;

        public void Close()
        {
            foreach (Hardware ram in _hardware)
                ram.Close();
        }
    }
}
