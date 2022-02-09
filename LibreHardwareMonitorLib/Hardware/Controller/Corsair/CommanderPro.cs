using HidSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LibreHardwareMonitor.Hardware.Controller.Corsair
{
    internal sealed class CommanderPro : Hardware
    {
        private HidStream _hidStream;
        private byte[] _writeBuffer = new byte[63];
        private byte[] _readBuffer = new byte[16];
        private List<Sensor> _fanSensors = new();
        private List<Sensor> _tempSensors = new();
        private List<Control> _controlSensors = new();
        private ConcurrentQueue<ICommanderProCommand> _commands = new();
        private List<ICommanderProCommand> _updateCommands = new();
        private ushort[] _rpmBackup = new ushort[6];
        private Task _task;
        private object _lock = new();
        private StringBuilder _report = new();

        public CommanderPro(HidDevice dev, ISettings settings) : base("Commander Pro", new Identifier(dev.DevicePath), settings)
        {
            if (dev.TryOpen(out _hidStream))
            {
                _hidStream.ReadTimeout = 5000;

                try
                {
                    new GetFansModeCommand().Execute(_hidStream, _readBuffer, _writeBuffer);
                    for (int i = 0; i < 6; i++)
                    {
                        byte value = _readBuffer[i];
                        if (value == 0x1 || value == 0x2)
                        {
                            Sensor fan = new Sensor("Commander Pro Fan #1", i, SensorType.Fan, this, settings);
                            Sensor controlSensor = new Sensor("Commander Pro Fan Control #1", i, SensorType.Control, this, settings);
                            Control control = new Control(controlSensor, settings, 0, 100);

                            _report.AppendLine($"Fan sensor index {i} found: State {value}");
                            controlSensor.Value = float.NaN;

                            _controlSensors.Add(control);
                            _fanSensors.Add(fan);

                            ActivateSensor(fan);
                            ActivateSensor(controlSensor);

                            GetFanRpmCommand getRpmCommand = new GetFanRpmCommand(fan);
                            getRpmCommand.Execute(_hidStream, _readBuffer, _writeBuffer);
                            _rpmBackup[i] = Convert.ToUInt16((int)fan.Value);

                            // not sure we should change the modes at all, maybe set a default speed instead <---- ? 
                            // Like get the rpm at startup, then when disabling, set that RPM
                            SetCurrentFanModeCommand modeCommand = new SetCurrentFanModeCommand(control);
                            SetFanRpmCommand setRpmCommand = new SetFanRpmCommand(control, _rpmBackup[i]);
                            control.ControlModeChanged += c =>
                            {
                                //_commands.Enqueue(modeCommand);
                                _commands.Enqueue(setRpmCommand);
                            };

                            var valueCommand = new SetFanSpeedPercentCommand(control);
                            control.SoftwareControlValueChanged += c =>
                            {
                                valueCommand.Value = (byte)Convert.ToInt32(Math.Round(c.SoftwareValue));
                                _commands.Enqueue(valueCommand);
                            };

                            _updateCommands.Add(new GetFanRpmCommand(fan));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _report.AppendLine($"Error while initializing fan sensors {ex.ToString()}");
                }

                try
                {
                    new GetTemperatureSensorsConnectedStatusCommand().Execute(_hidStream, _readBuffer, _writeBuffer);

                    for (int i = 0; i < 4; i++)
                    {
                        byte val = _readBuffer[i];
                        if (val == 0x01)
                        {
                            Sensor item = new Sensor("Commander Pro Temp Sensor #1", i, SensorType.Temperature, this, settings);
                            _report.AppendLine($"Temperature sensor index {i} found ");
                            _tempSensors.Add(item);
                            _updateCommands.Add(new GetTemperatureCommand(item));
                            ActivateSensor(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _report.AppendLine($"Error while initializing temp sensors {ex.ToString()}");
                }
            }
        }

        public override HardwareType HardwareType => HardwareType.EmbeddedController;

        public override void Update()
        {
            foreach (ICommanderProCommand command in _updateCommands)
            {
                _commands.Enqueue(command);
            }

            if (_task == null)
            {
                _task = Task.Run(CommandLoop);
            }
        }

        public async Task CommandLoop()
        {
            while (_hidStream.CanRead)
            {
                lock (_lock)
                {
                    while (_commands.TryDequeue(out ICommanderProCommand command))
                    {
                        try
                        {
                            command.Execute(_hidStream, _readBuffer, _writeBuffer);
                        }
                        catch (Exception ex)
                        {
                            // skip for now
                        }
                    }

                    Thread.Sleep(500);
                }
            }
        }

        public override void Close()
        {
            lock (_lock)
            {
                try
                {
                    // revert to default
                    // not sure we should change the modes at all, maybe set a default speed instead <---- ? 
                    // Like get the rpm at startup, then when disabling, set that RPM
                    _controlSensors.ForEach(control =>
                    {
                        //control.SetDefault();
                        new SetFanRpmCommand(control, _rpmBackup[control.Sensor.Index]).Execute(_hidStream, _readBuffer, _writeBuffer);
                        //new SetFanModeCommand(control, _modesBackup[control.Sensor.Index]).Execute(_hidStream, _readBuffer, _writeBuffer);
                    });
                }
                catch (Exception ex)
                {
                    // skip
                }

                _hidStream?.Close();

                Array.Clear(_readBuffer, 0, _readBuffer.Length);
                Array.Clear(_writeBuffer, 0, _writeBuffer.Length);
                while (_commands.TryDequeue(out _))
                {
                    // dequeue all
                }

                _updateCommands.Clear();
                _controlSensors.Clear();
                _fanSensors.Clear();
                _tempSensors.Clear();

                base.Close();
            }
        }

        public override string GetReport()
        {
            return _report.ToString();
        }
    }
}
