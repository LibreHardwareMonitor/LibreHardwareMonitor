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
        private readonly string _portName;
        private SerialPort _serialPort;

        public FC01(string portName, ISettings settings) : base("KyoudaiKen", new Identifier("kyoudaiken", portName.TrimStart('/').ToLowerInvariant()), settings)
        {
            _portName = portName;
            try
            {
                _serialPort = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One);
                _serialPort.ReadBufferSize = 1024;
                _serialPort.Open();
                _serialPort.NewLine = ('\r').ToString();
                _fans = new Sensor[3];
                _fans[0] = new Sensor("Fan Speed Ch 1", 0, SensorType.Control, this, settings) { Value = 0 }; ActivateSensor(_fans[0]);
                _fans[1] = new Sensor("Fan Speed Ch 2", 1, SensorType.Control, this, settings) { Value = 0 }; ActivateSensor(_fans[1]);
                _fans[2] = new Sensor("Fan Speed Ch 3", 2, SensorType.Control, this, settings) { Value = 0 }; ActivateSensor(_fans[2]);
                _controls = new Sensor[3];
                _controls[0] = new Sensor("Matrix Value Ch 1", 3, SensorType.SmallData, this, settings) { Value = 0 }; ActivateSensor(_controls[0]);
                _controls[1] = new Sensor("Matrix Value Ch 2", 4, SensorType.SmallData, this, settings) { Value = 0 }; ActivateSensor(_controls[1]);
                _controls[2] = new Sensor("Matrix Value Ch 3", 5, SensorType.SmallData, this, settings) { Value = 0 }; ActivateSensor(_controls[2]);
                _temperatures = new Sensor[3];
                _temperatures[0] = new Sensor("Temperature Sensor 1", 6, SensorType.Temperature, this, settings) { Value = 0 }; ActivateSensor(_temperatures[0]);
                _temperatures[1] = new Sensor("Temperature Sensor 2", 7, SensorType.Temperature, this, settings) { Value = 0 }; ActivateSensor(_temperatures[1]);
                _temperatures[2] = new Sensor("Temperature Sensor 3", 8, SensorType.Temperature, this, settings) { Value = 0 }; ActivateSensor(_temperatures[2]);
                updateSensors();
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
        }

        private void updateSensors()
        {
            if (!_serialPort.IsOpen) return;
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();

            _serialPort.WriteLine("rs");
            int j = 0;
            while (_serialPort.BytesToRead < 127 && j < 10)
            {
                Thread.Sleep(20);
                j++;
            }

            string data = String.Empty;
            while (_serialPort.BytesToRead > 0)
            {
                char b = (char)_serialPort.ReadByte();
                if(b!='\n') data += b;
            }
            data = data.TrimEnd('\r');
            if (data.Length < 96) return; //not all data received, we try again on the next interval
            string[] vT, vM, vS;
            int iT = data.IndexOf("t") + 2;
            int iM = data.IndexOf("m") + 2;
            int iS = data.IndexOf("s") + 2;
            vT = data.Substring(iT, iM - iT - 4).Split(' ');
            vM = data.Substring(iM, iS - iM - 4).Split(' ');
            vS = data.Substring(iS).Split(' ');

            for(int i = 0; i < vT.Length; i++)
            {
                _temperatures[i].Value = float.Parse(vT[i], CultureInfo.InvariantCulture);
            }
            for (int i = 0; i < vM.Length; i++)
            {
                _controls[i].Value = float.Parse(vM[i], CultureInfo.InvariantCulture);
            }
            for (int i = 0; i < vS.Length; i++)
            {
                _fans[i].Value = float.Parse(vS[i], CultureInfo.InvariantCulture);
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
                _serialPort.Dispose();
                _serialPort = null;
            }
        }
    }
}
