// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System.Collections.Generic;

namespace LibreHardwareMonitor.Hardware.Mainboard
{
    internal class MainboardGroup : IGroup
    {
        private readonly Mainboard[] _mainboards;

        public MainboardGroup(SmBios smbios, ISettings settings)
        {
            _mainboards = new Mainboard[1];
            _mainboards[0] = new Mainboard(smbios, settings);
        }

        public void Close()
        {
            foreach (Mainboard mainboard in _mainboards)
                mainboard.Close();
        }

        public string GetReport()
        {
            return null;
        }

        public IEnumerable<IHardware> Hardware
        {
            get { return _mainboards; }

        }
    }
}
