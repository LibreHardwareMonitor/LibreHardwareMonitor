// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

namespace LibreHardwareMonitor.Wmi
{
    interface IWmiObject
    {
        // Both of these get exposed to WMI
        string Name { get; }
        string Identifier { get; }

        // Not exposed.
        void Update();
    }
}
