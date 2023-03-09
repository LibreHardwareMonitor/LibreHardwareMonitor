using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using LibreHardwareMonitor.Interop;

namespace LibreHardwareMonitor.Hardware;

internal static class InpOut
{
    private static string _filePath;
    private static IntPtr _libraryHandle;
    private static Interop.InpOut.MapPhysToLinDelegate _mapPhysToLin;
    private static Interop.InpOut.UnmapPhysicalMemoryDelegate _unmapPhysicalMemory;

    public static bool IsOpen { get; private set; }

    public static bool Open()
    {
        if (Software.OperatingSystem.IsUnix)
            return false;

        if (IsOpen)
            return true;

        _filePath = GetFilePath();
        if (_filePath != null && (File.Exists(_filePath) || Extract(_filePath)))
        {
            _libraryHandle = Kernel32.LoadLibrary(_filePath);
            if (_libraryHandle != IntPtr.Zero)
            {
                IntPtr mapPhysToLinAddress = Kernel32.GetProcAddress(_libraryHandle, "MapPhysToLin");
                IntPtr unmapPhysicalMemoryAddress = Kernel32.GetProcAddress(_libraryHandle, "UnmapPhysicalMemory");

                if (mapPhysToLinAddress != IntPtr.Zero)
                    _mapPhysToLin = Marshal.GetDelegateForFunctionPointer<Interop.InpOut.MapPhysToLinDelegate>(mapPhysToLinAddress);

                if (unmapPhysicalMemoryAddress != IntPtr.Zero)
                    _unmapPhysicalMemory = Marshal.GetDelegateForFunctionPointer<Interop.InpOut.UnmapPhysicalMemoryDelegate>(unmapPhysicalMemoryAddress);

                IsOpen = true;
            }
        }

        if (!IsOpen)
            Delete();

        return IsOpen;
    }

    public static void Close()
    {
        if (_libraryHandle != IntPtr.Zero)
        {
            Kernel32.FreeLibrary(_libraryHandle);
            Delete();

            _libraryHandle = IntPtr.Zero;
        }

        IsOpen = false;
    }

    public static byte[] ReadMemory(IntPtr baseAddress, uint size)
    {
        if (_mapPhysToLin != null && _unmapPhysicalMemory != null)
        {
            IntPtr pdwLinAddr = _mapPhysToLin(baseAddress, size, out IntPtr pPhysicalMemoryHandle);
            if (pdwLinAddr != IntPtr.Zero)
            {
                byte[] bytes = new byte[size];
                Marshal.Copy(pdwLinAddr, bytes, 0, bytes.Length);
                _unmapPhysicalMemory(pPhysicalMemoryHandle, pdwLinAddr);

                return bytes;
            }
        }

        return null;
    }

    public static bool WriteMemory(IntPtr baseAddress, byte value)
    {
        if (_mapPhysToLin == null || _unmapPhysicalMemory == null)
            return false;

        IntPtr pdwLinAddr = _mapPhysToLin(baseAddress, 1, out IntPtr pPhysicalMemoryHandle);
        if (pdwLinAddr == IntPtr.Zero)
            return false;

        Marshal.WriteByte(pdwLinAddr, value);
        _unmapPhysicalMemory(pPhysicalMemoryHandle, pdwLinAddr);

        return true;
    }

    public static IntPtr MapMemory(IntPtr baseAddress, uint size, out IntPtr handle)
    {
        if (_mapPhysToLin == null)
        {
            handle = IntPtr.Zero;
            return IntPtr.Zero;
        }

        return _mapPhysToLin(baseAddress, size, out handle);
    }

    public static bool UnmapMemory(IntPtr handle, IntPtr address)
    {
        if (_unmapPhysicalMemory == null)
            return false;

        return _unmapPhysicalMemory(handle, address);
    }

    private static void Delete()
    {
        try
        {
            // try to delete the DLL
            if (_filePath != null && File.Exists(_filePath))
                File.Delete(_filePath);

            _filePath = null;
        }
        catch
        { }
    }

    private static string GetFilePath()
    {
        string filePath;

        try
        {
            filePath = Path.GetTempFileName();
            if (!string.IsNullOrEmpty(filePath))
                return Path.ChangeExtension(filePath, ".dll");
        }
        catch (IOException)
        { }

        const string fileName = "inpout.dll";

        try
        {
            ProcessModule processModule = Process.GetCurrentProcess().MainModule;
            if (!string.IsNullOrEmpty(processModule?.FileName))
                return Path.Combine(Path.GetDirectoryName(processModule.FileName) ?? string.Empty, fileName);
        }
        catch
        {
            // Continue with the other options.
        }

        filePath = GetPathFromAssembly(Assembly.GetExecutingAssembly());
        if (!string.IsNullOrEmpty(filePath))
            return Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty, fileName);

        filePath = GetPathFromAssembly(typeof(InpOut).Assembly);
        if (!string.IsNullOrEmpty(filePath))
            return Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty, fileName);

        return null;

        static string GetPathFromAssembly(Assembly assembly)
        {
            try
            {
                string location = assembly?.Location;
                return !string.IsNullOrEmpty(location) ? location : null;
            }
            catch
            {
                return null;
            }
        }
    }

    private static bool Extract(string filePath)
    {
        string resourceName = $"{nameof(LibreHardwareMonitor)}.Resources.{(Software.OperatingSystem.Is64Bit ? "inpoutx64.gz" : "inpout32.gz")}";

        Assembly assembly = typeof(InpOut).Assembly;
        long requiredLength = 0;

        try
        {
            using Stream stream = assembly.GetManifestResourceStream(resourceName);

            if (stream != null)
            {
                using FileStream target = new(filePath, FileMode.Create);

                stream.Position = 1; // Skip first byte.

                using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);

                gzipStream.CopyTo(target);

                requiredLength = target.Length;
            }
        }
        catch
        {
            return false;
        }

        if (HasValidFile())
            return true;

        // Ensure the file is actually written to the file system.
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        while (stopwatch.ElapsedMilliseconds < 2000)
        {
            if (HasValidFile())
                return true;

            Thread.Yield();
        }

        return false;

        bool HasValidFile()
        {
            try
            {
                return File.Exists(filePath) && new FileInfo(filePath).Length == requiredLength;
            }
            catch
            {
                return false;
            }
        }
    }
}
