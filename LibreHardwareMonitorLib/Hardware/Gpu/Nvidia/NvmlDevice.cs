// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Runtime.InteropServices;

namespace LibreHardwareMonitor.Hardware.Gpu
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct NvmlDevice
    {
        public IntPtr Handle;
    }
}
