using HidSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LibreHardwareMonitor.Hardware.Controller.Corsair
{
    internal sealed class CommanderPro : Hardware
    {
        private List<Sensor> _fanSensors = new();
        private List<Sensor> _tempSensors = new();
        private List<Control> _controlSensors = new();
        private ConcurrentQueue<ICommanderProCommand> _commands = new();
        private List<ICommanderProCommand> _updateCommands = new();
        private ushort[] _rpmBackup = new ushort[6];
        private Task _task;
        private StringBuilder _report = new();
        private CommanderProDevice _device;

        public CommanderPro(HidDevice dev, ISettings settings) : base("Commander Pro", new Identifier(dev.DevicePath), settings)
        {
            try
            {
                if (CommanderProDevice.TryOpen(dev, out CommanderProDevice device))
                {
                    _device = device;
                    _report.AppendLine($"Commander Pro device product Id: {dev.ProductID}");

                    foreach (int i in device.GetDetectedFanIndexes())
                    {
                        Sensor fan = new($"Commander Pro Fan #{i}", i, SensorType.Fan, this, settings);
                        Sensor controlSensor = new("Commander Pro Fan Control #1", i, SensorType.Control, this, settings);
                        Control control = new(controlSensor, settings, 0, 100);

                        _report.AppendLine($"Fan index {i} found ");

                        controlSensor.Value = float.NaN;

                        _controlSensors.Add(control);
                        _fanSensors.Add(fan);

                        ActivateSensor(fan);
                        ActivateSensor(controlSensor);

                        new GetFanRpmCommand(fan).ExecuteAsync(device).Wait();
                        _rpmBackup[i] = Convert.ToUInt16((int)fan.Value);

                        // not sure we should change the modes at all, maybe set a default speed instead <---- ? 
                        // Like get the rpm at startup, then when disabling, set that RPM
                        SetCurrentFanModeCommand modeCommand = new(control);
                        SetFanRpmCommand setRpmCommand = new(control, _rpmBackup[i]);
                        control.ControlModeChanged += c =>
                        {
                            //_commands.Enqueue(modeCommand);
                            _commands.Enqueue(setRpmCommand);
                        };

                        SetFanSpeedPercentCommand valueCommand = new(control);
                        control.SoftwareControlValueChanged += c =>
                        {
                            _commands.Enqueue(valueCommand);
                        };

                        _updateCommands.Add(new GetFanRpmCommand(fan));
                    }

                    foreach (int i in device.GetDetectedTemperatureSensorIndexes())
                    {
                        Sensor item = new($"Commander Pro Temp Sensor #{i}", i, SensorType.Temperature, this, settings);
                        _report.AppendLine($"Temperature sensor index {i} found ");
                        _tempSensors.Add(item);
                        _updateCommands.Add(new GetTemperatureCommand(item));
                        ActivateSensor(item);
                    }
                }

            }
            catch (Exception ex)
            {
                _report.AppendLine($"Error while initializing commander pro: {ex}");
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
            while (_device.IsOpen)
            {
                while (_commands.TryDequeue(out ICommanderProCommand command))
                {
                    try
                    {
                        await command.ExecuteAsync(_device);
                    }
                    catch (Exception ex)
                    {
                        // skip for now
                    }
                }

                await Task.Delay(500);
            }
        }

        public override void Close()
        {
            try
            {
                // revert to default
                // not sure we should change the modes at all, maybe set a default speed instead <---- ? 
                // Like get the rpm at startup, then when disabling, set that RPM
                _controlSensors.ForEach(control =>
                {
                    //control.SetDefault();
                    new SetFanRpmCommand(control, _rpmBackup[control.Sensor.Index]).ExecuteAsync(_device).Wait();
                    //new SetFanModeCommand(control, _modesBackup[control.Sensor.Index]).Execute(_hidStream, _readBuffer, _writeBuffer);
                });
            }
            catch (Exception ex)
            {
                // skip
            }

            _device?.Dispose();

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

        public override string GetReport()
        {
            return _report.ToString();
        }
    }
}
