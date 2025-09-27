using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace LibreHardwareMonitor.PawnIo;

public class PawnIo
{
    private const uint DEVICE_TYPE = 41394u << 16;
    private const uint IOCTL_PIO_LOAD_BINARY = 0x821 << 2;
    private const uint IOCTL_PIO_EXECUTE_FN = 0x841 << 2;

    private enum ControlCode : uint
    {
        LoadBinary = DEVICE_TYPE | IOCTL_PIO_LOAD_BINARY,
        Execute = DEVICE_TYPE | IOCTL_PIO_EXECUTE_FN
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(
        [MarshalAs(UnmanagedType.LPTStr)] string filename,
        [MarshalAs(UnmanagedType.U4)] FileAccess access,
        [MarshalAs(UnmanagedType.U4)] FileShare share,
        [Optional] IntPtr securityAttributes,
        [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
        [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
        [Optional] IntPtr templateFile);

    [DllImport("Kernel32.dll", SetLastError = false, CharSet = CharSet.Auto)]
    private static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        [MarshalAs(UnmanagedType.U4)] ControlCode IoControlCode,
        [In] byte[] InBuffer,
        uint nInBufferSize,
        [Out] byte[] OutBuffer,
        uint nOutBufferSize,
        ref uint pBytesReturned,
        [In][Optional] IntPtr Overlapped);

    private static readonly ConcurrentDictionary<(Assembly, string), byte[]> _resourceCache = new();

    /// <summary>
    /// Gets a value indicating whether PawnIO is installed on the system.
    /// </summary>
    public static bool IsInstalled => _version.Value is not null;

    /// <summary>
    /// Retrieves the version information for the installed PawnIO.
    /// </summary>
    public static Version Version => _version.Value ?? throw new InvalidOperationException("PawnIO is not installed.");

    private static Lazy<Version> _version = new(() => {
        using RegistryKey subKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO");
        if (subKey?.GetValue("DisplayVersion") is string displayVersion && System.Version.TryParse(displayVersion, out Version version))
            return version;

        return null;
    });

    private readonly SafeFileHandle _handle;

    private PawnIo(SafeFileHandle handle) => _handle = handle;

    public static PawnIo LoadModuleFromResource(Assembly assembly, string resourceName)
    {
        SafeFileHandle handle = CreateFile(@"\\.\PawnIO", FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);

        if (handle.IsInvalid)
            return new(null);

        uint read = 0;

        byte[] bin = _resourceCache.GetOrAdd((assembly, resourceName), key =>
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            using MemoryStream memory = new();
            stream.CopyTo(memory);
            return memory.ToArray();
        });

        if (DeviceIoControl(handle, ControlCode.LoadBinary, bin, (uint)bin.Length, null, 0u, ref read, IntPtr.Zero))
            return new(handle);

        return new(null);
    }

    public bool IsLoaded => _handle is
    {
        IsInvalid: false,
        IsClosed: false
    };

    public void Close() => _handle.Close();

    public long[] Execute(string name, long[] input, int outLength)
    {
        if (_handle is not { IsInvalid: false, IsClosed: false })
            return [];

        uint read = 0;

        byte[] output = new byte[outLength * sizeof(long)];
        byte[] inp = new byte[input.Length * sizeof(long) + 32];
        Buffer.BlockCopy(Encoding.ASCII.GetBytes(name), 0, inp, 0, Math.Min(31, name.Length));
        Buffer.BlockCopy(input, 0, inp, 32, input.Length * sizeof(long));

        if (DeviceIoControl(_handle, ControlCode.Execute, inp, (uint)inp.Length, output, (uint)output.Length, ref read, IntPtr.Zero))
        {
            long[] outp = new long[read / sizeof(long)];
            Buffer.BlockCopy(output, 0, outp, 0, (int)read);
            return outp;
        }

        return [];
    }

    public int ExecuteHr(string name, long[] inBuffer, uint inSize, long[] outBuffer, uint outSize, out uint returnSize)
    {
        if (inBuffer.Length < inSize)
            throw new ArgumentOutOfRangeException(nameof(inSize));

        if (outBuffer.Length < outSize)
            throw new ArgumentOutOfRangeException(nameof(outSize));

        if (_handle is not { IsInvalid: false, IsClosed: false })
        {
            returnSize = 0;
            return 0;
        }

        uint read = 0;

        byte[] output = new byte[outSize * sizeof(long)];
        byte[] inp = new byte[inSize * sizeof(long) + 32];
        Buffer.BlockCopy(Encoding.ASCII.GetBytes(name), 0, inp, 0, Math.Min(31, name.Length));
        Buffer.BlockCopy(inBuffer, 0, inp, 32, inBuffer.Length * sizeof(long));

        if (DeviceIoControl(_handle, ControlCode.Execute, inp, (uint)inp.Length, output, (uint)output.Length, ref read, IntPtr.Zero))
        {
            Buffer.BlockCopy(output, 0, outBuffer, 0, Math.Min((int)read, outBuffer.Length * sizeof(long)));
            returnSize = read / sizeof(long);
            return 0;
        }

        returnSize = 0;

        return Marshal.GetLastWin32Error();
    }
}
