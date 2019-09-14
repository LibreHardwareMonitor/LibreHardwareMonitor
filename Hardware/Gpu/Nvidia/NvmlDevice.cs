using System;
using System.Runtime.InteropServices;

namespace LibreHardwareMonitor.Hardware.Gpu {
    [StructLayout(LayoutKind.Sequential)]
    internal struct NvmlDevice {
        public IntPtr Handle;
    }
}
