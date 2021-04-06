using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LibreHardwareMonitor.Interop
{
    class InpOut
    {
        [DllImport("inpoutx64.dll", EntryPoint = "MapPhysToLin", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr MapPhysToLin(UIntPtr pbPhysAddr, uint dwPhysSize, out IntPtr pPhysicalMemoryHandle);

        [DllImport("inpoutx64.dll", EntryPoint = "UnmapPhysicalMemory", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnmapPhysicalMemory(IntPtr PhysicalMemoryHandle, IntPtr pbLinAddr);

    }
}
