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
        private byte[] _writeBuffer = new byte[63];
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
            await _semaphoreSlim.WaitAsync(_defaultTimeout);

            action(_hidStream, _readBuffer, _writeBuffer);

            _semaphoreSlim.Release();
        }

        public IEnumerable<int> GetDetectedFanIndexes()
        {
            _semaphoreSlim.Wait(_defaultTimeout);

            _writeBuffer[0] = 0x20;
            Array.Clear(_writeBuffer, 1, _writeBuffer.Length - 1);

            _hidStream.Write(_writeBuffer);
            _hidStream.Read(_readBuffer);

            for (int i = 0; i < 6; i++)
            {
                byte value = _readBuffer[i];
                if (value == 0x1 || value == 0x2)
                {
                    yield return i;
                }
            }

            _semaphoreSlim.Release();
        }

        public IEnumerable<int> GetDetectedTemperatureSensorIndexes()
        {
            _semaphoreSlim.Wait(_defaultTimeout);

            _writeBuffer[0] = 0x10;
            Array.Clear(_writeBuffer, 1, _writeBuffer.Length - 1);

            _hidStream.Write(_writeBuffer);
            _hidStream.Read(_readBuffer);

            for (int i = 0; i < 4; i++)
            {
                byte value = _readBuffer[i];
                if (value == 0x1)
                {
                    yield return i;
                }
            }

            _semaphoreSlim.Release();
        }

        public void Dispose() => _hidStream.Close();
    }
}
