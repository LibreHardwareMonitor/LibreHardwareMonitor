using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LibreHardwareMonitor.Hardware.Controller.KyoudaiKen
{
    class KyoudaiKenGroup : IGroup
    {
        private readonly List<IHardware> _hardware = new List<IHardware>();
        private readonly StringBuilder _report = new StringBuilder();

        private SerialPort Serial;

        public KyoudaiKenGroup(ISettings settings)
        {
            // No implementation for KyoudaiKen's controllers on Unix systems
            if (Software.OperatingSystem.IsLinux)
                return;

            string[] ports = SerialPort.GetPortNames();
            foreach(string port in ports)
            {
                bool isValid = false;
                try
                {
                    using (SerialPort serialPort = new SerialPort(port, 115200, Parity.None, 8, StopBits.One))
                    {
                        serialPort.NewLine = ('\r').ToString();
                        _report.Append("Port Name: ");
                        _report.AppendLine(port);
                        try
                        {
                            serialPort.Open();
                        }
                        catch (UnauthorizedAccessException)
                        {
                            _report.AppendLine("Exception: Access Denied");
                        }

                        if (serialPort.IsOpen)
                        {
                            try
                            {
                                serialPort.DiscardInBuffer();
                                serialPort.DiscardOutBuffer();

                                serialPort.WriteLine("i");
                                int j = 0;
                                while (serialPort.BytesToRead == 0 && j < 10)
                                {
                                    Thread.Sleep(20);
                                    j++;
                                }

                                string response = String.Empty;
                                while (serialPort.BytesToRead > 0)
                                {
                                    char b = (char)serialPort.ReadByte();
                                    switch (b)
                                    {
                                        case '\n': break;
                                        default:
                                            response += b;
                                            break;
                                    }
                                }

                                if (!response.StartsWith("KyoudaiKen FC02"))
                                {
                                    _report.AppendLine("Status: Wrong Hardware\nResponse: " + response);
                                }
                                else
                                {
                                    isValid = true;
                                }
                            }
                            catch (TimeoutException)
                            {
                                _report.AppendLine("Status: No Response");
                            }

                            serialPort.DiscardInBuffer();
                        }
                        else
                        {
                            _report.AppendLine("Status: Port not Open");
                        }
                    }
                }
                catch (Exception e)
                {
                    _report.AppendLine(e.ToString());
                }

                if (isValid)
                {
                    _report.AppendLine("Status: OK");
                    _hardware.Add(new FC01(port, settings));
                }

                _report.AppendLine();
            }
        }

        private static string ReadLine(SerialPort port, int timeout)
        {
            int i = 0;
            StringBuilder builder = new StringBuilder();
            while (i < timeout)
            {
                while (port.BytesToRead > 0)
                {
                    byte b = (byte)port.ReadByte();
                    switch (b)
                    {
                        case 0x0D: return builder.ToString();
                        default:
                            builder.Append((char)b);
                            break;
                    }
                }

                i++;
                Thread.Sleep(1);
            }

            throw new TimeoutException();
        }

        public IEnumerable<IHardware> Hardware => _hardware;

        public void Close()
        {
            foreach (IHardware iHardware in _hardware)
            {
                if (iHardware is Hardware hardware)
                    hardware.Close();
            }
        }

        public string GetReport()
        {
            if (_report.Length > 0)
            {
                StringBuilder r = new StringBuilder();
                r.AppendLine("Serial Port KyoudaiKen FC01");
                r.AppendLine();
                r.Append(_report);
                r.AppendLine();
                return r.ToString();
            }

            return null;
        }
    }
}
