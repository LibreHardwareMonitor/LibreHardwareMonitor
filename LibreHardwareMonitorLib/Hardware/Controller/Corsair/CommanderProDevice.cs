using HidSharp;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LibreHardwareMonitor.Hardware.Controller.Corsair
{
    internal delegate void CommanderProAction(HidStream stream, byte[] readBuffer, byte[] writeBuffer);

    internal class CommanderProDevice : IDisposable
    {
        private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(3);
        private readonly HidStream _hidStream;
        private byte[] _writeBuffer = new byte[64];
        private byte[] _readBuffer = new byte[16];
        private SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public static bool TryOpen(HidDevice hidDevice, out CommanderProDevice stream)
        {
            stream = null;
            if (hidDevice.TryOpen(out HidStream deviceStream))
            {
                deviceStream.ReadTimeout = 5000;
                stream = new CommanderProDevice(deviceStream);
                return true;
            }

            return false;
        }

        private CommanderProDevice(HidStream hidStream)
        {
            _hidStream = hidStream;
        }

        public bool IsOpen => _hidStream.CanRead;

        public async Task DoAsync(CommanderProAction action)
        {
            try
            {
                await _semaphoreSlim.WaitAsync(_defaultTimeout);
                action(_hidStream, _readBuffer, _writeBuffer);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public int[] GetDetectedFanIndexes()
        {
            try
            {
                _semaphoreSlim.Wait(_defaultTimeout);

                _writeBuffer.Clear();
                Span<byte> writeSpan = _writeBuffer.GetWriteBufferSpan(1);
                writeSpan[0] = 0x20;

                _hidStream.Write(_writeBuffer);
                _hidStream.Read(_readBuffer);

                Span<byte> readSpan = _readBuffer.GetReadBufferSpan(6);

                List<int> indexes = new List<int>();
                for (int i = 0; i < readSpan.Length; i++)
                {
                    byte value = readSpan[i];
                    if (value == 0x1 || value == 0x2)
                    {
                        indexes.Add(i);
                    }
                }

                return indexes.ToArray();
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public int[] GetDetectedTemperatureSensorIndexes()
        {
            try
            {
                _semaphoreSlim.Wait(_defaultTimeout);

                _writeBuffer.Clear();

                Span<byte> writeSpan = _writeBuffer.GetWriteBufferSpan(1);
                writeSpan[0] = 0x10;

                _hidStream.Write(_writeBuffer);
                _hidStream.Read(_readBuffer);

                Span<byte> readSpan = _readBuffer.GetReadBufferSpan(4);

                List<int> indexes = new List<int>();
                for (int i = 0; i < 4; i++)
                {
                    byte value = readSpan[i];
                    if (value == 0x1)
                    {
                        indexes.Add(i);
                    }
                }

                return indexes.ToArray();
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public void Dispose() => _hidStream.Close();
    }
}
