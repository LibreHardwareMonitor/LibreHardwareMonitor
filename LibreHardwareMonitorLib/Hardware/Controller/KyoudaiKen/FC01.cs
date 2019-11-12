using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LibreHardwareMonitor.Hardware.Controller.KyoudaiKen
{
    internal sealed class FC01 : Hardware, IDisposable
    {
        private readonly bool _available;
        private readonly StringBuilder _buffer = new StringBuilder();
        private readonly Sensor[] _controls;
        private readonly Sensor[] _fans;
        private readonly Sensor[] _temperatures;
        private float[] t, m, s;
        private readonly string _portName;
        private SerialPort _serialPort;

        public FC01(string portName, ISettings settings) : base("KyoudaiKen", new Identifier("kyoudaiken", portName.TrimStart('/').ToLowerInvariant()), settings)
        {
            t = new float[3];
            m = new float[3];
            s = new float[3];
            _portName = portName;
            try
            {
                _serialPort = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One);
                _serialPort.ReadBufferSize = 1024;
                _serialPort.Open();
                _serialPort.NewLine = ("\r\n").ToString();
                updateSensors();
                _fans = new Sensor[3];
                _fans[0] = new Sensor("Fan Speed Ch 1", 0, SensorType.Control, this, settings) { Value = s[0] }; ActivateSensor(_fans[0]);
                _fans[1] = new Sensor("Fan Speed Ch 2", 1, SensorType.Control, this, settings) { Value = s[1] }; ActivateSensor(_fans[1]);
                _fans[2] = new Sensor("Fan Speed Ch 3", 2, SensorType.Control, this, settings) { Value = s[2] }; ActivateSensor(_fans[2]);
                _controls = new Sensor[3];
                _controls[0] = new Sensor("Matrix Value Ch 1", 3, SensorType.Factor, this, settings) { Value = m[0] }; ActivateSensor(_controls[0]);
                _controls[1] = new Sensor("Matrix Value Ch 2", 4, SensorType.Factor, this, settings) { Value = m[1] }; ActivateSensor(_controls[1]);
                _controls[2] = new Sensor("Matrix Value Ch 3", 5, SensorType.Factor, this, settings) { Value = m[2] }; ActivateSensor(_controls[2]);
                _temperatures = new Sensor[3];
                _temperatures[0] = new Sensor("Temperature Sensor 1", 6, SensorType.Temperature, this, settings) { Value = t[0] }; ActivateSensor(_temperatures[0]);
                _temperatures[1] = new Sensor("Temperature Sensor 2", 7, SensorType.Temperature, this, settings) { Value = t[1] }; ActivateSensor(_temperatures[1]);
                _temperatures[2] = new Sensor("Temperature Sensor 3", 8, SensorType.Temperature, this, settings) { Value = t[2] }; ActivateSensor(_temperatures[2]);
            }
            catch (IOException)
            { }
            catch (TimeoutException)
            { }
            _available = true;
        }

        public override void Update()
        {
            if (!_available)
                return;

            updateSensors();

            for(int i=0;i<3;i++)
            {
                _fans[i].Value = s[i];
                _controls[i].Value = m[i];
                _temperatures[i].Value = t[i];
            }
        }

        private void updateSensors()
        {
            if (!_serialPort.IsOpen) return;
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();

            _serialPort.WriteLine("bs");
            int i = 0;
            while (_serialPort.BytesToRead != 36 && i < 400)
            {
                Thread.Sleep(1);
                i++;
            }

            if (_serialPort.BytesToRead == 36)
            {
                byte[] data = new byte[36];

                i = 0;
                while (_serialPort.BytesToRead > 0)
                {
                    data[i] = (byte)_serialPort.ReadByte();
                    i++;
                }

                for (i = 0; i < 3; i++)
                {
                    t[i] = BitConverter.ToSingle(data, i * 4);
                }
                for (; i < 6; i++)
                {
                    m[i - 3] = BitConverter.ToSingle(data, i * 4);
                }
                for (; i < 9; i++)
                {
                    s[i - 6] = BitConverter.ToSingle(data, i * 4);
                }
            }
        }

        public override HardwareType HardwareType
        {
            get { return HardwareType.KyoudaiKen_FC01; }
        }

        public override string GetReport()
        {
            StringBuilder r = new StringBuilder();
            r.AppendLine("KyoudaiKen FC01");
            r.AppendLine();
            r.Append("Port: ");
            r.AppendLine(_portName);
            r.Append("Hardware Revision: ");
            r.AppendLine("01".ToString(CultureInfo.InvariantCulture));
            r.AppendLine();
            return r.ToString();
        }

        public override void Close()
        {
            _serialPort.Close();
            _serialPort.Dispose();
            _serialPort = null;
            base.Close();
        }

        public void Dispose()
        {
            if (_serialPort != null)
            {
                _serialPort.Close();
                _serialPort.Dispose();
                _serialPort = null;

            }
            base.Close();
        }
    }
}
