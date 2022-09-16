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
            Array.Clear(writeBuffer, 0, writeBuffer.Length);
            Span<byte> writeSpan = writeBuffer.GetWriteBufferSpan(2);

            writeSpan[0] = 0x21;
            writeSpan[1] = Convert.ToByte(_sensor.Index);

            hidStream.Write(writeBuffer);
            hidStream.Read(readBuffer);

            Span<byte> readSpan = readBuffer.GetReadBufferSpan(2);

            // big endian 2 bytes response
            int value = (ushort)((readSpan[0] << 8) | readSpan[1]);

            _sensor.Value = value;
        }
    }

    internal class SetFanSpeedPercentCommand: CommanderProCommand
    {
        private Control _control;
        private Sensor _sensor;

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

            writeBuffer.Clear();
            Span<byte> writeSpan = writeBuffer.GetWriteBufferSpan(3);

            writeSpan[0] = 0x23;
            writeSpan[1] = Convert.ToByte(_control.Sensor.Index);
            writeSpan[2] = Convert.ToByte(Convert.ToInt32(Math.Round(_control.SoftwareValue)));

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

            writeBuffer.Clear();

            Span<byte> writeSpan = writeBuffer.GetWriteBufferSpan(4);

            writeSpan[0] = 0x24;
            writeSpan[1] = Convert.ToByte(_control.Sensor.Index);
            writeSpan[2] = _rpmAsBytes[1];
            writeSpan[3] = _rpmAsBytes[0];


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

            writeBuffer.Clear();

            Span<byte> writeSpan = writeBuffer.GetWriteBufferSpan(3);

            writeSpan[0] = 0x28;
            writeSpan[1] = Convert.ToByte(_control.Sensor.Index);
            writeSpan[2] = GetControlModeByte(_control.ControlMode);

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

            writeBuffer.Clear();

            Span<byte> writeSpan = writeBuffer.GetWriteBufferSpan(3);

            writeSpan[0] = 0x28;
            writeSpan[1] = Convert.ToByte(_control.Sensor.Index);
            writeSpan[2] = _mode;

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
            writeBuffer.Clear();

            Span<byte> writeSpan = writeBuffer.GetWriteBufferSpan(2);

            writeSpan[0] = 0x11;
            writeSpan[1] = Convert.ToByte(_sensor.Index);

            stream.Write(writeBuffer);
            stream.Read(readBuffer);

            Span<byte> readSpan = readBuffer.GetReadBufferSpan(2);

            _sensor.Value = ((readSpan[0] << 8) | readSpan[1]) / 100f;
        }
    }
}
