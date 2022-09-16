using System;

namespace LibreHardwareMonitor.Hardware.Controller.Corsair
{
    internal static class BufferExtensions
    {
        public static Span<byte> GetWriteBufferSpan(this byte[] _writeBuffer, int _lenght)
        {
            return _writeBuffer.AsSpan().Slice(Constants.WRITE_OFFSET, _lenght);
        }

        public static Span<byte> GetReadBufferSpan(this byte[] _readBuffer, int _lenght)
        {
            return _readBuffer.AsSpan().Slice(Constants.READ_OFFSET, _lenght);
        }

        public static void Clear<T>(this T[] _array)
        {
            Array.Clear(_array, 0, _array.Length);
        }
    }
}
