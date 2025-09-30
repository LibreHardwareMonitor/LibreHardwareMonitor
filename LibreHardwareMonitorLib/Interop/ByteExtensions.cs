using Windows.Win32;

namespace LibreHardwareMonitor.Interop;

#if NET8_0_OR_GREATER

internal static class ByteExtensions
{
    public static byte[] ToArray(this __byte_2 bytes)
    {
        return bytes.AsReadOnlySpan().ToArray();
    }

    public static byte[] ToArray(this __byte_3 bytes)
    {
        return bytes.AsReadOnlySpan().ToArray();
    }

    public static byte[] ToArray(this __byte_4 bytes)
    {
        return bytes.AsReadOnlySpan().ToArray();
    }

    public static byte[] ToArray(this __byte_8 bytes)
    {
        return bytes.AsReadOnlySpan().ToArray();
    }

    public static byte[] ToArray(this __byte_16 bytes)
    {
        return bytes.AsReadOnlySpan().ToArray();
    }

    public static byte[] ToArray(this __byte_20 bytes)
    {
        return bytes.AsReadOnlySpan().ToArray();
    }

    public static byte[] ToArray(this __byte_40 bytes)
    {
        return bytes.AsReadOnlySpan().ToArray();
    }
}

#endif
