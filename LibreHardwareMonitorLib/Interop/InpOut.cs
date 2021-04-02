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
        [DllImport("inpout32.dll", EntryPoint = "GetPhysLong", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetPhysLong32(UIntPtr memAddress, out uint data);

        [DllImport("inpoutx64.dll", EntryPoint = "GetPhysLong", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetPhysLong64(UIntPtr memAddress, out uint data);

        public static bool GetPhysLong(UIntPtr memAddress, out uint data)
        {
            if (Environment.Is64BitProcess)
                return GetPhysLong64(memAddress, out data);

            return GetPhysLong32(memAddress, out data);
        }
    }
}
