using HidSharp;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LibreHardwareMonitor.Hardware.Controller.Nzxt
{
    /**
     * Support for the Kraken X3 devices from NZXT
     */
    internal sealed class KrakenX3 : Hardware
    {

        // Some fixed messages to send to the pump for basic monitoring and control
        private static readonly byte[] GET_FIRMWARE_INFO = { 0x10, 0x01 };
        private static readonly byte[] INITIALIZE_1 = { 0x70, 0x02, 0x01, 0xb8, 0x0b };
        private static readonly byte[] INITIALIZE_2 = { 0x70, 0x01 };
        private static readonly byte[][] SET_PUMP_TARGET_MAP = new byte[101][]; // Sacrifice memory to speed this up with a lookup instead of a copy operation
        static KrakenX3()
        {
            byte[] set_pump_speed_header = { 0x72, 0x01, 0x00, 0x00 };

            for (byte speed = 0; speed < SET_PUMP_TARGET_MAP.Length; speed++)
            {
                SET_PUMP_TARGET_MAP[speed] = set_pump_speed_header.Concat(Enumerable.Repeat(speed, 40).Concat(new byte[20])).ToArray();
            }
        }

        private readonly Sensor _temperature;
        private readonly Sensor _pumpRpm;
        private readonly Sensor _pumpControl;

        private readonly HidStream _stream;
        private readonly byte[] _rawData = new byte[64];

        private volatile bool _controlling = false;

        public string FirmwareVersion { get; private set; }

        public KrakenX3(HidDevice dev, ISettings settings) : base("Nzxt Kraken X3", new Identifier("nzxt", "krakenx3", dev.GetSerialNumber().TrimStart('0')), settings)
        {
            if (dev.TryOpen(out _stream))
            {
                _stream.ReadTimeout = 5000; // The NZXT device returns with data that we need periodically without writing... 
                _stream.Write(INITIALIZE_1);
                _stream.Write(INITIALIZE_2);
                _stream.Write(GET_FIRMWARE_INFO);
                do
                {
                    _stream.Read(_rawData);
                    if (_rawData[0] == 0x11 && _rawData[1] == 0x01)
                    {
                        FirmwareVersion = $"{_rawData[0x11]}.{_rawData[0x11]}.{_rawData[0x13]}";
                    }
                } while (FirmwareVersion == null);

                Name = $"Nzxt Kraken X3";

                _temperature = new Sensor("Internal Water", 0, SensorType.Temperature, this, new ParameterDescription[0], settings);
                ActivateSensor(_temperature);

                _pumpRpm = new Sensor("Pump", 0, SensorType.Fan, this, new ParameterDescription[0], settings);
                ActivateSensor(_pumpRpm);

                _pumpControl = new Sensor("Pump Control", 0, SensorType.Control, this, new ParameterDescription[0], settings);
                Control control = new Control(_pumpControl, settings, 0, 100);
                _pumpControl.Control = control;

                control.ControlModeChanged += SoftwareControlValueChanged;
                control.SoftwareControlValueChanged += SoftwareControlValueChanged;
                SoftwareControlValueChanged(control);

                ActivateSensor(_pumpControl);

                ThreadPool.UnsafeQueueUserWorkItem(ContinuousRead, _rawData);
            }
        }

        private void SoftwareControlValueChanged(Control control)
        {
            if (control.ControlMode == ControlMode.Software)
            {
                float value = control.SoftwareValue;
                byte pump_speed_index = (byte)((value > 100) ? 100 : (value < 0) ? 0 : value); // Clamp the value, anything out of range will fail

                _controlling = true;
                _stream.Write(SET_PUMP_TARGET_MAP[pump_speed_index]);
                _pumpControl.Value = value;
            }
            else if (control.ControlMode == ControlMode.Default)
            { // There isn't a "default" mode with this pump, but a safe setting is 40%
                _stream.Write(SET_PUMP_TARGET_MAP[40]);
            }
        }

        public override HardwareType HardwareType => HardwareType.LiquidCooler;

        public string Status => (FirmwareVersion != "2.1.0" ? $"Status: Untested Firmware Version {FirmwareVersion}! Please consider Updating to Version 2.1.0" : "Status: OK");

        public override void Close()
        {
            base.Close();
            _stream.Close();
        }

        private void ContinuousRead(object state)
        {
            byte[] buffer = new byte[_rawData.Length];
            while (_stream.CanRead)
            {
                try
                {
                    _stream.Read(buffer); // This is a blocking call, will wait for bytes to become available

                    lock (_rawData)
                    {
                        Array.Copy(buffer, _rawData, buffer.Length);
                    }
                }
                catch (TimeoutException ex)
                {
                    // Don't care, just make sure the stream is still open
                    Thread.Sleep(500);
                }
                catch (ObjectDisposedException ex)
                {
                    // Could be unplugged, or the app is stopping...
                }
            }
        }

        public override void Update()
        {
            // The NZXT Kraken X3 series sends updates periodically. We have to read it in a seperate thread, this call just reads that data.
            lock (_rawData)
            {

                if (_rawData[0] == 0x75 && _rawData[1] == 0x02)
                {
                    _temperature.Value = _rawData[15] + _rawData[16] / 10.0f;
                    _pumpRpm.Value = (_rawData[18] << 8) | _rawData[17];

                    // The following logic makes sure the pump is set to the controlling value. This pump sometimes sets itself to 0% when instructed to a value.
                    if (!_controlling)
                    {
                        _pumpControl.Value = _rawData[19];
                    }
                    else if (_pumpControl.Value != _rawData[19])
                    {
                        byte pump_speed_index = (byte)_pumpControl.Value;
                        _stream.Write(SET_PUMP_TARGET_MAP[pump_speed_index]);
                    }
                    else
                    {
                        _controlling = false;
                    }
                }
            }
        }
    }
}
