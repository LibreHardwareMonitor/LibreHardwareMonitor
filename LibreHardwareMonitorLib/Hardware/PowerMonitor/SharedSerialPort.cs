using System.IO.Ports;

namespace LibreHardwareMonitor.Hardware.PowerMonitor;

internal sealed class SharedSerialPort : SerialPort
{
    private bool _hasMutex;

    public SharedSerialPort()
    { }

    public SharedSerialPort(string portName) : base(portName)
    { }

    public SharedSerialPort(string portName, int baudRate) : base(portName, baudRate)
    { }

    public SharedSerialPort(string portName, int baudRate, Parity parity) : base(portName, baudRate, parity)
    { }

    public SharedSerialPort(string portName, int baudRate, Parity parity, int dataBits)
        : base(portName, baudRate, parity, dataBits)
    { }

    public SharedSerialPort(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        : base(portName, baudRate, parity, dataBits, stopBits)
    { }

    private int MutexTimeout { get; set; } = 500;

    public new void Open()
    {
        _hasMutex = Mutexes.WaitUsbSensors(MutexTimeout);
        if (_hasMutex)
        {
            base.Open();
        }
    }

    public new void Close()
    {
        if (_hasMutex)
        {
            if (IsOpen)
            {
                BaseStream.Flush();
                BaseStream.Close();
            }

            _hasMutex = false;
            Mutexes.ReleaseUsbSensors();
        }
    }

    public new void Write(byte[] buffer, int offset, int count)
    {
        if (_hasMutex)
        {
            base.Write(buffer, offset, count);
        }
    }

    public new int Read(byte[] buffer, int offset, int count)
    {
        if (_hasMutex)
        {
            return base.Read(buffer, offset, count);
        }

        return 0;
    }

    public new void DiscardInBuffer()
    {
        if (_hasMutex)
        {
            base.DiscardInBuffer();
        }
    }

    public new void DiscardOutBuffer()
    {
        if (_hasMutex)
        {
            base.DiscardOutBuffer();
        }
    }
}
