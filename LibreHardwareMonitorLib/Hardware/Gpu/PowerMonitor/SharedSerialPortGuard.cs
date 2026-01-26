using System;
using System.IO.Ports;
using LibreHardwareMonitor.Interop.PowerMonitor;

namespace LibreHardwareMonitor.Hardware.Gpu.PowerMonitor
{
    /// <summary>
    /// Provides a guard that ensures a specified SerialPort is opened upon construction and closed when disposed.<br/>
    /// This does not call <see cref="SerialPort.Dispose"/>.
    /// </summary>
    /// <remarks>This is supposed to be used for shared serial ports - open, read data, close, repeat.</remarks>
    internal sealed class SharedSerialPortGuard : IDisposable
    {
        private readonly SharedSerialPort _serialPort;

        public SharedSerialPortGuard(SharedSerialPort serialPort)
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
