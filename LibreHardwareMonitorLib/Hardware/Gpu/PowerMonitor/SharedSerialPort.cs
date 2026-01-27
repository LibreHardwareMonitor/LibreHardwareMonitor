using System.IO.Ports;

namespace LibreHardwareMonitor.Hardware.Gpu.PowerMonitor;

internal sealed class SharedSerialPort : SerialPort
{
    bool hasMutex = false;

    private int MutexTimeout { get; set; } = 500;

    public SharedSerialPort()
    {
    }

    public SharedSerialPort(string portName) : base(portName)
    {
    }

    public SharedSerialPort(string portName, int baudRate) : base(portName, baudRate)
    {
    }

    public SharedSerialPort(string portName, int baudRate, Parity parity) : base(portName, baudRate, parity)
    {
    }

    public SharedSerialPort(string portName, int baudRate, Parity parity, int dataBits)
        : base(portName, baudRate, parity, dataBits)
    {
    }

    public SharedSerialPort(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        : base(portName, baudRate, parity, dataBits, stopBits)
    {
    }

    public new void Open()
    {
        if (hasMutex = Mutexes.WaitUsbSensors(MutexTimeout))
        {
            base.Open();
        }
    }

    public new void Close()
    {
        if (hasMutex)
        {
            if (IsOpen)
            {
                BaseStream.Flush();
                BaseStream.Close();
            }

            hasMutex = false;
            Mutexes.ReleaseUsbSensors();
        }
    }

    public new void Write(byte[] buffer, int offset, int count)
    {
        if (hasMutex)
        {
            base.Write(buffer, offset, count);
        }
    }

    public new int Read(byte[] buffer, int offset, int count)
    {
        if (hasMutex)
        {
            return base.Read(buffer, offset, count);
        }

        return 0;
    }

    public new void DiscardInBuffer()
    {
        if (hasMutex)
        {
            base.DiscardInBuffer();
        }
    }

    public new void DiscardOutBuffer()
    {
        if (hasMutex)
        {
            base.DiscardOutBuffer();
        }
    }
}
