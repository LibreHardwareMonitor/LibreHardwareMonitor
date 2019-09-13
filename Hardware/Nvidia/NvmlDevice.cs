using System;
using System.Runtime.InteropServices;

namespace LibreHardwareMonitor.Hardware.Nvidia {
    [StructLayout(LayoutKind.Sequential)]
    internal struct NvmlDevice {
        public IntPtr Handle;
    }
}
