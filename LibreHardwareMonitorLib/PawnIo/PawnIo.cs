using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Interop;
using Microsoft.Win32;

namespace LibreHardwareMonitor.PawnIo;

public unsafe class PawnIo
{
    private readonly IntPtr _handle;

    private PawnIo()
    {
        TryLoadDll();
        pawnio_open(out _handle);
    }

    /// <summary>
    /// Gets the installation path of PawnIO, if it is installed on the system.
    /// </summary>
    public static string InstallPath
    {
        get
        {
            if ((Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO", "InstallLocation", null) ??
                 Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\PawnIO", "Install_Dir", null) ??
                 Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + Path.DirectorySeparatorChar + "PawnIO") is string
                {
                    Length: > 0
                } path)
            {
                if (Directory.Exists(path))
                    return path;
            }

            return null;
        }
    }

    /// <summary>
    /// Gets a value indicating whether PawnIO is installed on the system.
    /// </summary>
    public static bool IsInstalled
    {
        get { return !string.IsNullOrEmpty(InstallPath); }
    }

    [DllImport("PawnIOLib", ExactSpelling = true, PreserveSig = false)]
    private static extern void pawnio_version(out uint version);

    [DllImport("PawnIOLib", ExactSpelling = true, PreserveSig = false)]
    private static extern void pawnio_open(out IntPtr handle);

    [DllImport("PawnIOLib", ExactSpelling = true, PreserveSig = false)]
    private static extern void pawnio_load(IntPtr handle, byte* blob, IntPtr size);

    [DllImport("PawnIOLib", ExactSpelling = true, PreserveSig = false)]
    private static extern void pawnio_execute
    (
        IntPtr handle,
        [MarshalAs(UnmanagedType.LPStr)] string name,
        long[] inArray,
        IntPtr inSize,
        long[] outArray,
        IntPtr outSize,
        out IntPtr returnSize);

    [DllImport("PawnIOLib", ExactSpelling = true, EntryPoint = "pawnio_execute")]
    private static extern int pawnio_execute_hr
    (
        IntPtr handle,
        [MarshalAs(UnmanagedType.LPStr)] string name,
        long[] inArray,
        IntPtr inSize,
        long[] outArray,
        IntPtr outSize,
        out IntPtr returnSize);

    [DllImport("PawnIOLib", ExactSpelling = true, PreserveSig = false)]
    private static extern void pawnio_close(IntPtr handle);

    private static void TryLoadDll()
    {
        try
        {
            // If already loaded, return immediately.
            pawnio_version(out uint _);
            return;
        }
        catch
        {
            // ignored
        }

        try
        {
            if (IsInstalled)
                Kernel32.LoadLibrary(InstallPath + Path.DirectorySeparatorChar + "PawnIOLib");
        }
        catch
        {
            // ignored
        }
    }

    /// <summary>
    /// Retrieves the version information for the underlying PawnIO library.
    /// </summary>
    public static Version Version()
    {
        try
        {
            pawnio_version(out uint version);

            return new Version((int)((version >> 16) & 0xFF),
                               (int)((version >> 8) & 0xFF),
                               (int)(version & 0xFF),
                               0);
        }
        catch
        {
            return new Version();
        }
    }

    public void Close()
    {
        pawnio_close(_handle);
    }

    public static PawnIo LoadModuleFromResource(Assembly assembly, string resourceName)
    {
        using Stream s = assembly.GetManifestResourceStream(resourceName);

        if (s is not UnmanagedMemoryStream ums)
            throw new InvalidOperationException();

        var pawnIO = new PawnIo();
        pawnio_load(pawnIO._handle, ums.PositionPointer, (IntPtr)ums.Length);

        return pawnIO;
    }

    public long[] Execute(string name, long[] input, int outLength)
    {
        long[] outArray = new long[outLength];

        pawnio_execute(_handle,
                       name,
                       input,
                       (IntPtr)input.Length,
                       outArray,
                       (IntPtr)outArray.Length,
                       out nint returnLength);

        Array.Resize(ref outArray, (int)returnLength);
        return outArray;
    }

    public int ExecuteHr(string name, long[] inBuffer, uint inSize, long[] outBuffer, uint outSize, out uint returnSize)
    {
        if (inBuffer.Length < inSize)
            throw new ArgumentOutOfRangeException(nameof(inSize));

        if (outBuffer.Length < outSize)
            throw new ArgumentOutOfRangeException(nameof(outSize));

        int ret = pawnio_execute_hr(_handle, name, inBuffer, (IntPtr)inSize, outBuffer, (IntPtr)outSize, out IntPtr retSize);

        returnSize = (uint)retSize;

        return ret;
    }
}
