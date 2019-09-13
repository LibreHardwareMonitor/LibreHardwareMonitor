// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System.Management.Instrumentation;
using OpenHardwareMonitor.Hardware;

namespace OpenHardwareMonitor.WMI
{
    [InstrumentationClass(InstrumentationType.Instance)]
    public class Hardware : IWmiObject
    {
        #region WMI Exposed

        public string HardwareType { get; }
        public string Identifier { get; }
        public string Name { get; }
        public string Parent { get; }

        #endregion

        public Hardware(IHardware hardware)
        {
            Name = hardware.Name;
            Identifier = hardware.Identifier.ToString();
            HardwareType = hardware.HardwareType.ToString();
            Parent = (hardware.Parent != null)
              ? hardware.Parent.Identifier.ToString()
              : "";
        }

        public void Update() { }
    }
}
