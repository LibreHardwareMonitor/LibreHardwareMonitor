using System;
using System.IO.Ports;

namespace LibreHardwareMonitor.Hardware.Gpu.PowerMonitor
{
    /// <summary>
    /// Provides a guard that ensures a specified SerialPort is opened upon construction and closed when disposed.<br/>
    /// This does not call <see cref="SerialPort.Dispose"/>.
    /// </summary>
    /// <remarks>This is supposed to be used for shared serial ports - open, read data, close, repeat.</remarks>
    internal sealed class SerialPortGuard : IDisposable
    {
        private readonly SerialPort _serialPort;

        public SerialPortGuard(SerialPort serialPort)
        {
            _serialPort = serialPort;

            _serialPort.Open();
        }

        public void Dispose()
        {
            _serialPort.Close();
        }
    }
}
