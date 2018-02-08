using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenHardwareMonitor.Software
{
    public static class OperatingSystem
    {
        static OperatingSystem()
        {
            //The operative system doesn't change during execution so let's query it just one time
            var platform = (int) Environment.OSVersion.Platform;
            IsLinux = platform == 4 || platform == 128;

            if (IntPtr.Size == 8)
                Is64BitOperatingSystem = true;

            try
            {
                var result = IsWow64Process(Process.GetCurrentProcess().Handle, out bool wow64Process);

                Is64BitOperatingSystem = result && wow64Process;
            }
            catch (EntryPointNotFoundException)
            {
                Is64BitOperatingSystem = false;
            }
        }

        public static bool Is64BitOperatingSystem { get; }


        public static bool IsLinux { get; }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);
    }
}