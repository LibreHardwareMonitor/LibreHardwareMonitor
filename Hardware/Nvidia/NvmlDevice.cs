using System;
using System.Runtime.InteropServices;

namespace OpenHardwareMonitor.Hardware.Nvidia {
    [StructLayout(LayoutKind.Sequential)]
    internal struct NvmlDevice {
        public IntPtr Handle;
    }
}
