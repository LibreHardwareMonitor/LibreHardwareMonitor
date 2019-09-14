// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Globalization;

namespace LibreHardwareMonitor.Hardware
{
    public struct ParameterDescription
    {
        public ParameterDescription(string name, string description, float defaultValue)
        {
            Name = name;
            Description = description;
            DefaultValue = defaultValue;
        }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public float DefaultValue { get; private set; }
    }
}
