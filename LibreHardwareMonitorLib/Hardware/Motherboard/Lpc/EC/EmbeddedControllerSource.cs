// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc.EC
{
    public class EmbeddedControllerSource
    {
        public EmbeddedControllerSource(string name, byte port, SensorType type, EmbeddedControllerReader reader)
        {
            Name = name;
            Port = port;
            Type = type;
            Reader = reader;
        }

        public string Name { get; }

        public byte Port { get; }

        public EmbeddedControllerReader Reader { get; }

        public SensorType Type { get; }
    }
}
