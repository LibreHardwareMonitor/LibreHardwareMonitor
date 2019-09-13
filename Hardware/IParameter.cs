// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

namespace LibreHardwareMonitor.Hardware
{
    public interface IParameter : IElement
    {
        ISensor Sensor { get; }
        Identifier Identifier { get; }
        string Name { get; }
        string Description { get; }
        float Value { get; set; }
        float DefaultValue { get; }
        bool IsDefault { get; set; }
    }
}
