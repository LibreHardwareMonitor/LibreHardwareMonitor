// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

namespace LibreHardwareMonitor.Hardware
{
    public interface ISettings
    {
        bool Contains(string name);

        void SetValue(string name, string value);

        string GetValue(string name, string value);

        void Remove(string name);
    }
}
