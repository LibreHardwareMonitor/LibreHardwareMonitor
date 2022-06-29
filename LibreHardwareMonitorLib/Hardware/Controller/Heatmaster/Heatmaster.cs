﻿// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace LibreHardwareMonitor.Hardware.Controller.Heatmaster
{
    internal sealed class Heatmaster : Hardware, IDisposable
    {
        private readonly bool _available;
        private readonly StringBuilder _buffer = new();
        private readonly Sensor[] _controls;
        private readonly Sensor[] _fans;
        private readonly int _firmwareCrc;
        private readonly int _firmwareRevision;
        private readonly Sensor[] _flows;
        private readonly int _hardwareRevision;
        private readonly string _portName;
        private readonly Sensor[] _relays;
        private readonly Sensor[] _temperatures;
        private SerialPort _serialPort;

        public Heatmaster(string portName, ISettings settings) : base("Heatmaster", new Identifier("heatmaster", portName.TrimStart('/').ToLowerInvariant()), settings)
        {
            _portName = portName;
            try
            {
                _serialPort = new SerialPort(portName, 38400, Parity.None, 8, StopBits.One);
                _serialPort.Open();
                _serialPort.NewLine = ((char)0x0D).ToString();

                _hardwareRevision = ReadInteger(0, 'H');
                _firmwareRevision = ReadInteger(0, 'V');
                _firmwareCrc = ReadInteger(0, 'C');

                int fanCount = Math.Min(ReadInteger(32, '?'), 4);
                int temperatureCount = Math.Min(ReadInteger(48, '?'), 6);
                int flowCount = Math.Min(ReadInteger(64, '?'), 1);
                int relayCount = Math.Min(ReadInteger(80, '?'), 1);

                _fans = new Sensor[fanCount];
                _controls = new Sensor[fanCount];
                for (int i = 0; i < fanCount; i++)
                {
                    int device = 33 + i;
                    string name = ReadString(device, 'C');
                    _fans[i] = new Sensor(name, device, SensorType.Fan, this, settings) { Value = ReadInteger(device, 'R') };
                    ActivateSensor(_fans[i]);
                    _controls[i] = new Sensor(name, device, SensorType.Control, this, settings) { Value = (100 / 255.0f) * ReadInteger(device, 'P') };
                    ActivateSensor(_controls[i]);
                }

                _temperatures = new Sensor[temperatureCount];
                for (int i = 0; i < temperatureCount; i++)
                {
                    int device = 49 + i;
                    string name = ReadString(device, 'C');
                    _temperatures[i] = new Sensor(name, device, SensorType.Temperature, this, settings);
                    int value = ReadInteger(device, 'T');
                    _temperatures[i].Value = 0.1f * value;
                    if (value != -32768)
                        ActivateSensor(_temperatures[i]);
                }

                _flows = new Sensor[flowCount];
                for (int i = 0; i < flowCount; i++)
                {
                    int device = 65 + i;
                    string name = ReadString(device, 'C');
                    _flows[i] = new Sensor(name, device, SensorType.Flow, this, settings) { Value = 0.1f * ReadInteger(device, 'L') };
                    ActivateSensor(_flows[i]);
                }

                _relays = new Sensor[relayCount];
                for (int i = 0; i < relayCount; i++)
                {
                    int device = 81 + i;
                    string name = ReadString(device, 'C');
                    _relays[i] = new Sensor(name, device, SensorType.Control, this, settings);
                    _relays[i].Value = 100 * ReadInteger(device, 'S');
                    ActivateSensor(_relays[i]);
                }

                // set the update rate to 2 Hz
                WriteInteger(0, 'L', 2);
                _available = true;
            }
            catch (IOException)
            { }
            catch (TimeoutException)
            { }
        }

        public override HardwareType HardwareType
        {
            get { return HardwareType.Cooler; }
        }

        public void Dispose()
        {
            if (_serialPort != null)
            {
                _serialPort.Dispose();
                _serialPort = null;
            }
        }

        private string ReadLine(int timeout)
        {
            int i = 0;
            StringBuilder builder = new();
            while (i <= timeout)
            {
                while (_serialPort.BytesToRead > 0)
                {
                    byte b = (byte)_serialPort.ReadByte();
                    switch (b)
                    {
                        case 0xAA: return ((char)b).ToString();
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

        private string ReadField(int device, char field)
        {
            _serialPort.WriteLine("[0:" + device + "]R" + field);
            for (int i = 0; i < 5; i++)
            {
                string s = ReadLine(200);
                Match match = Regex.Match(s, @"-\[0:" + device.ToString(CultureInfo.InvariantCulture) + @"\]R" + Regex.Escape(field.ToString(CultureInfo.InvariantCulture)) + ":(.*)");
                if (match.Success)
                    return match.Groups[1].Value;
            }

            return null;
        }

        private string ReadString(int device, char field)
        {
            string s = ReadField(device, field);
            if (s != null && s[0] == '"' && s[s.Length - 1] == '"')
                return s.Substring(1, s.Length - 2);

            return null;
        }

        private int ReadInteger(int device, char field)
        {
            string s = ReadField(device, field);
            if (int.TryParse(s, out int i))
                return i;

            return 0;
        }

        private void WriteField(int device, char field, string value)
        {
            _serialPort.WriteLine("[0:" + device + "]W" + field + ":" + value);
            for (int i = 0; i < 5; i++)
            {
                string s = ReadLine(200);
                Match match = Regex.Match(s, @"-\[0:" + device.ToString(CultureInfo.InvariantCulture) + @"\]W" + Regex.Escape(field.ToString(CultureInfo.InvariantCulture)) + ":" + value);
                if (match.Success)
                    return;
            }
        }

        private void WriteInteger(int device, char field, int value)
        {
            WriteField(device, field, value.ToString(CultureInfo.InvariantCulture));
        }

        private void ProcessUpdateLine(string line)
        {
            Match match = Regex.Match(line, @">\[0:(\d+)\]([0-9:\|-]+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int device))
            {
                foreach (string s in match.Groups[2].Value.Split('|'))
                {
                    string[] strings = s.Split(':');
                    int[] ints = new int[strings.Length];
                    bool valid = true;
                    for (int i = 0; i < ints.Length; i++)
                    {
                        if (!int.TryParse(strings[i], out ints[i]))
                        {
                            valid = false;
                            break;
                        }
                    }

                    if (!valid)
                        continue;

                    switch (device)
                    {
                        case 32:
                            if (ints.Length == 3 && ints[0] <= _fans.Length)
                            {
                                _fans[ints[0] - 1].Value = ints[1];
                                _controls[ints[0] - 1].Value = (100 / 255.0f) * ints[2];
                            }

                            break;
                        case 48:
                            if (ints.Length == 2 && ints[0] <= _temperatures.Length)
                                _temperatures[ints[0] - 1].Value = 0.1f * ints[1];

                            break;
                        case 64:
                            if (ints.Length == 3 && ints[0] <= _flows.Length)
                                _flows[ints[0] - 1].Value = 0.1f * ints[1];

                            break;
                        case 80:
                            if (ints.Length == 2 && ints[0] <= _relays.Length)
                                _relays[ints[0] - 1].Value = 100 * ints[1];

                            break;
                    }
                }
            }
        }

        public override void Update()
        {
            if (!_available)
                return;

            while (_serialPort.IsOpen && _serialPort.BytesToRead > 0)
            {
                byte b = (byte)_serialPort.ReadByte();
                if (b == 0x0D)
                {
                    ProcessUpdateLine(_buffer.ToString());
                    _buffer.Length = 0;
                }
                else
                {
                    _buffer.Append((char)b);
                }
            }
        }

        public override string GetReport()
        {
            StringBuilder r = new();
            r.AppendLine("Heatmaster");
            r.AppendLine();
            r.Append("Port: ");
            r.AppendLine(_portName);
            r.Append("Hardware Revision: ");
            r.AppendLine(_hardwareRevision.ToString(CultureInfo.InvariantCulture));
            r.Append("Firmware Revision: ");
            r.AppendLine(_firmwareRevision.ToString(CultureInfo.InvariantCulture));
            r.Append("Firmware CRC: ");
            r.AppendLine(_firmwareCrc.ToString(CultureInfo.InvariantCulture));
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
    }
}
