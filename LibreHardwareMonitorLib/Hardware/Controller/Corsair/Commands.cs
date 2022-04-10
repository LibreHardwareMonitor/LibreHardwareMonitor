using HidSharp;
using System;
using System.Threading.Tasks;

namespace LibreHardwareMonitor.Hardware.Controller.Corsair
{
    /// References: 
    /// https://github.com/liquidctl/liquidctl/blob/main/docs/developer/protocol/lighting_node_rgb.md
    /// https://github.com/audiohacked/OpenCorsairLink/issues/70
    internal interface ICommanderProCommand
    {
        Task ExecuteAsync(CommanderProDevice device);
    }

    internal abstract class CommanderProCommand : ICommanderProCommand
    {
        public async Task ExecuteAsync(CommanderProDevice device)
        {
            await device.DoAsync(DoExecute);
        }

        public abstract void DoExecute(HidStream hidStream, byte[] readBuffer, byte[] writeBuffer);
    }

    internal class GetFanRpmCommand: CommanderProCommand
    {
        private readonly Sensor _sensor;

        public GetFanRpmCommand(Sensor sensor)
        {
            _sensor = sensor;
        }

        public override void DoExecute(HidStream hidStream, byte[] readBuffer, byte[] writeBuffer)
        {
            writeBuffer[0] = 0x21;
            writeBuffer[1] = Convert.ToByte(_sensor.Index);
            Array.Clear(writeBuffer, 2, writeBuffer.Length - 2);

            hidStream.Write(writeBuffer);
            hidStream.Read(readBuffer);

            // big endian 2 bytes response

            int value = readBuffer[1] << 8 | readBuffer[2];

            _sensor.Value = value;
        }
    }

    internal class SetFanSpeedPercentCommand: CommanderProCommand
    {
        private Control _control;
        private Sensor _sensor;
        private byte _value;

        public SetFanSpeedPercentCommand(Control control)
        {
            _control = control;
            _sensor = control.Sensor as Sensor;
        }

        public override void DoExecute(HidStream stream, byte[] readBuffer, byte[] writeBuffer)
        {
            if ( _control.ControlMode != ControlMode.Software )
            {
                return;
            }

            writeBuffer[0] = 0x23;
            writeBuffer[1] = Convert.ToByte(_control.Sensor.Index);
            writeBuffer[2] = Convert.ToByte(Convert.ToInt32(Math.Round(_control.SoftwareValue)));
            Array.Clear(writeBuffer, 3, writeBuffer.Length - 3);

            stream.Write(writeBuffer);
            _sensor.Value = _control.SoftwareValue;
        }
    }

    internal class SetFanRpmCommand : CommanderProCommand
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

        public override void DoExecute(HidStream stream, byte[] readBuffer, byte[] writeBuffer)
        {
            if (_control.ControlMode != ControlMode.Software)
            {
                return;
            }

            writeBuffer[0] = 0x24;
            writeBuffer[1] = Convert.ToByte(_control.Sensor.Index);
            writeBuffer[2] = _rpmAsBytes[1];
            writeBuffer[3] = _rpmAsBytes[0];

            Array.Clear(writeBuffer, 4, writeBuffer.Length - 4);

            stream.Write(writeBuffer);
            _sensor.Value = _control.SoftwareValue;
        }
    }


    internal class SetCurrentFanModeCommand: CommanderProCommand
    {
        private Control _control;

        public SetCurrentFanModeCommand(Control sensor)
        {
            _control = sensor;
        }

        public override void DoExecute(HidStream stream, byte[] readBuffer, byte[] writeBuffer)
        {
            if (_control.ControlMode != ControlMode.Software)
            {
                return;
            }

            writeBuffer[0] = 0x28;
            writeBuffer[1] = Convert.ToByte(_control.Sensor.Index);
            writeBuffer[2] = GetControlModeByte(_control.ControlMode);
            Array.Clear(writeBuffer, 3, writeBuffer.Length - 3);

            stream.Write(writeBuffer);
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

    internal class SetFanModeCommand : CommanderProCommand
    {
        private readonly Control _control;
        private readonly byte _mode;

        public SetFanModeCommand(Control sensor, byte mode)
        {
            _control = sensor;
            _mode = mode;
        }

        public override void DoExecute(HidStream stream, byte[] readBuffer, byte[] writeBuffer)
        {
            if (_control.ControlMode != ControlMode.Software)
            {
                return;
            }

            writeBuffer[0] = 0x28;
            writeBuffer[1] = Convert.ToByte(_control.Sensor.Index);
            writeBuffer[2] = _mode;
            Array.Clear(writeBuffer, 3, writeBuffer.Length - 3);

            stream.Write(writeBuffer);
        }
    }

    internal class GetTemperatureCommand : CommanderProCommand
    {
        private readonly Sensor _sensor;

        public GetTemperatureCommand(Sensor sensor)
        {
            _sensor = sensor;
        }

        public override void DoExecute(HidStream stream, byte[] readBuffer, byte[] writeBuffer)
        {
            writeBuffer[0] = 0x11;
            writeBuffer[1] = Convert.ToByte(_sensor.Index);
            Array.Clear(writeBuffer, 2, writeBuffer.Length - 2);

            stream.Write(writeBuffer);
            stream.Read(readBuffer);

            _sensor.Value = (int)(readBuffer[1] << 8 | readBuffer[2]) / 100f;
        }
    }
}
