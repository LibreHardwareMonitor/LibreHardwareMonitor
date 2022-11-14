using System;
using System.Runtime.InteropServices;

namespace LibreHardwareMonitor.Interop;

internal class InpOut
{
    public delegate IntPtr MapPhysToLinDelegate(IntPtr pbPhysAddr, uint dwPhysSize, out IntPtr pPhysicalMemoryHandle);

    public delegate bool UnmapPhysicalMemoryDelegate(IntPtr PhysicalMemoryHandle, IntPtr pbLinAddr);

    [DllImport("inpout.dll", EntryPoint = "MapPhysToLin", CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr MapPhysToLin(IntPtr pbPhysAddr, uint dwPhysSize, out IntPtr pPhysicalMemoryHandle);

    [DllImport("inpout.dll", EntryPoint = "UnmapPhysicalMemory", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnmapPhysicalMemory(IntPtr PhysicalMemoryHandle, IntPtr pbLinAddr);
}