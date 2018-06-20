using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenHardwareMonitor.Software {
  public static class OperatingSystem {
    static OperatingSystem() {
      // The operating system doesn't change during execution so let's query it just one time.
      var platform = Environment.OSVersion.Platform;
      IsLinux = platform == PlatformID.Unix || platform == PlatformID.MacOSX;


      if (IntPtr.Size == 8) {
        Is64Bit = true;
      } else if (!IsLinux) {
        try {
          var result = IsWow64Process(Process.GetCurrentProcess().Handle, out bool wow64Process);
          // If we are still here, this is a 64bit windows; 32bit windows does
          // not provide IsWow64Process.
          Is64Bit = true;
        }
        catch (EntryPointNotFoundException) {
          // IsWow64Process is not present on 32 bit:
          Is64Bit = false;
        }
      }
    }

    public static bool Is64Bit { get; }

    public static bool IsLinux { get; }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);
  }
}