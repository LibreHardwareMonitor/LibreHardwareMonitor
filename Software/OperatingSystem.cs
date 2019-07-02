using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenHardwareMonitor.Software {
  public static class OperatingSystem {
    static OperatingSystem() {
      // The operating system doesn't change during execution so let's query it just one time.
      var platform = Environment.OSVersion.Platform;
      IsLinux = platform == PlatformID.Unix || platform == PlatformID.MacOSX;

      if (System.Environment.Is64BitProcess)
        Is64Bit = true;
    }

    public static bool Is64Bit { get; }

    public static bool IsLinux { get; }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);
  }
}