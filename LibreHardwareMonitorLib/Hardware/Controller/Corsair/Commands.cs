using HidSharp;
using System;

namespace LibreHardwareMonitor.Hardware.Controller.Corsair
{
    /// References: 
    /// https://github.com/liquidctl/liquidctl/blob/main/docs/developer/protocol/lighting_node_rgb.md
    /// https://github.com/audiohacked/OpenCorsairLink/issues/70
    internal interface ICommanderProCommand
    {
        void Execute(HidStream _stream, byte[] _readBuffer, byte[] _writeBuffer);
    }

    internal class GetFanRpmCommand: ICommanderProCommand
    {
        private readonly Sensor _sensor;

        public GetFanRpmCommand(Sensor sensor)
        {
            _sensor = sensor;
        }

        public void Execute(HidStream _stream, byte[] _readBuffer, byte[] _writeBuffer)
        {
            _writeBuffer[0] = 0x21;
            _writeBuffer[1] = Convert.ToByte(_sensor.Index);
            Array.Clear(_writeBuffer, 2, _writeBuffer.Length - 2);

            _stream.Write(_writeBuffer);
            _stream.Read(_readBuffer);

            // big endian 2 bytes response

            int value = _readBuffer[1] << 8 | _readBuffer[2];

            _sensor.Value = value;
        }
    }

    internal class SetFanSpeedPercentCommand: ICommanderProCommand
    {
        private Control _control;
        private Sensor _sensor;

        public SetFanSpeedPercentCommand(Control control)
        {
            _control = control;
            _sensor = control.Sensor as Sensor;
        }

        public byte Value { get; set; }

        public void Execute(HidStream _stream, byte[] _readBuffer, byte[] _writeBuffer)
        {
            if ( _control.ControlMode != ControlMode.Software )
            {
                return;
            }

            _writeBuffer[0] = 0x23;
            _writeBuffer[1] = Convert.ToByte(_control.Sensor.Index);
            _writeBuffer[2] = Convert.ToByte(_control.SoftwareValue);
            Array.Clear(_writeBuffer, 3, _writeBuffer.Length - 3);

            _stream.Write(_writeBuffer);
            _sensor.Value = _control.SoftwareValue;
        }
    }

    internal class SetFanRpmCommand : ICommanderProCommand
    {
        private Control _control;
        private readonly byte[] _rpmAsBytes;
        private Sensor _sensor;

        public SetFanRpmCommand(Control control, ushort rpm)
        {
            _control = control;
            _rpmAsBytes = BitConverter.GetBytes(rpm);
            _sensor = control.Sensor as Sensor;
        }

        public byte Value { get; set; }

        public void Execute(HidStream _stream, byte[] _readBuffer, byte[] _writeBuffer)
        {
            if (_control.ControlMode != ControlMode.Software)
            {
                return;
            }

            _writeBuffer[0] = 0x24;
            _writeBuffer[1] = Convert.ToByte(_control.Sensor.Index);
            _writeBuffer[2] = _rpmAsBytes[1];
            _writeBuffer[3] = _rpmAsBytes[0];

            Array.Clear(_writeBuffer, 4, _writeBuffer.Length - 4);

            _stream.Write(_writeBuffer);
            _sensor.Value = _control.SoftwareValue;
        }
    }


    internal class GetFansModeCommand : ICommanderProCommand
    {
        public void Execute(HidStream _stream, byte[] _readBuffer, byte[] _writeBuffer)
        {
            _writeBuffer[0] = 0x20;
            Array.Clear(_writeBuffer, 1, _writeBuffer.Length - 1);

            _stream.Write(_writeBuffer);
            _stream.Read(_readBuffer);
        }
    }

    internal class SetCurrentFanModeCommand: ICommanderProCommand
    {
        private Control _control;

        public SetCurrentFanModeCommand(Control sensor)
        {
            _control = sensor;
        }

        public void Execute(HidStream _stream, byte[] _readBuffer, byte[] _writeBuffer)
        {
            if (_control.ControlMode != ControlMode.Software)
            {
                return;
            }

            _writeBuffer[0] = 0x28;
            _writeBuffer[1] = Convert.ToByte(_control.Sensor.Index);
            _writeBuffer[2] = GetControlModeByte(_control.ControlMode);
            Array.Clear(_writeBuffer, 3, _writeBuffer.Length - 3);

            _stream.Write(_writeBuffer);
        }

        public byte GetControlModeByte(ControlMode controlMode)
        {
            switch (controlMode)
            {
                case ControlMode.Software : return 0x02;
                default: return 0x00;
            }
        }
    }

    internal class SetFanModeCommand : ICommanderProCommand
    {
        private readonly Control _control;
        private readonly byte _mode;

        public SetFanModeCommand(Control sensor, byte mode)
        {
            _control = sensor;
            _mode = mode;
        }

        public void Execute(HidStream _stream, byte[] _readBuffer, byte[] _writeBuffer)
        {
            if (_control.ControlMode != ControlMode.Software)
            {
                return;
            }

            _writeBuffer[0] = 0x28;
            _writeBuffer[1] = Convert.ToByte(_control.Sensor.Index);
            _writeBuffer[2] = _mode;
            Array.Clear(_writeBuffer, 3, _writeBuffer.Length - 3);

            _stream.Write(_writeBuffer);
        }
    }

    internal class GetTemperatureSensorsConnectedStatusCommand: ICommanderProCommand
    {
        public GetTemperatureSensorsConnectedStatusCommand()
        {
        }

        public void Execute(HidStream _stream, byte[] _readBuffer, byte[] _writeBuffer)
        {
            _writeBuffer[0] = 0x10;
            Array.Clear(_writeBuffer, 1, _writeBuffer.Length - 1);

            _stream.Write(_writeBuffer);
            _stream.Read(_readBuffer);
        }
    }

    internal class GetTemperatureCommand : ICommanderProCommand
    {
        private readonly Sensor _sensor;

        public GetTemperatureCommand(Sensor sensor)
        {
            _sensor = sensor;
        }

        public void Execute(HidStream _stream, byte[] _readBuffer, byte[] _writeBuffer)
        {
            _writeBuffer[0] = 0x11;
            _writeBuffer[1] = Convert.ToByte(_sensor.Index);
            Array.Clear(_writeBuffer, 2, _writeBuffer.Length - 2);

            _stream.Write(_writeBuffer);
            _stream.Read(_readBuffer);

            _sensor.Value = (int)(_readBuffer[1] << 8 | _readBuffer[2]) / 100f;
        }
    }
}
