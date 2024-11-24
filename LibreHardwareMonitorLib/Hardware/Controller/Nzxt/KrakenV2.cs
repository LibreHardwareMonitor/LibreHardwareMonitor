using System;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Controller.Nzxt;

/// <summary>
/// Support for the Kraken X (X42, X52, X62 or X72) devices.
/// </summary>
internal sealed class KrakenV2 : Hardware
{
    private static readonly byte[] _getFirmwareInfo = [0x10, 0x01];

    private readonly HidDevice _device;
    private readonly Sensor _fan;
    private readonly byte _fanChannel;
    private readonly bool _fanControl;
    private readonly Sensor _fanRpm;
    private readonly TimeSpan _interval = TimeSpan.FromMilliseconds(5000);
    private readonly Sensor _liquidTemperature;
    private readonly Sensor _pump;
    private readonly byte _pumpChannel;
    private readonly Sensor _pumpRpm;
    private readonly byte[] _rawData = new byte[64];
    private readonly string _supportedFirmware;

    private DateTime _lastUpdate = DateTime.MinValue;

    public KrakenV2(HidDevice dev, ISettings settings) : base("Nzxt Kraken X", new Identifier(dev), settings)
    {
        _device = dev;

        switch (dev.ProductID)
        {
            case 0x170e:
            default:
                Name = "NZXT Kraken X";

                _fanControl = true;

                _fanChannel = 0x00;
                _pumpChannel = 0x40;

                _supportedFirmware = "6.2.0";
                break;
        }

        try
        {
            using HidStream stream = dev.Open();

            stream.Write(_getFirmwareInfo);

            int tries = 0;

            while (FirmwareVersion == null && tries++ < 10)
            {
                stream.Read(_rawData);

                if (_rawData[0] == 0x11 && _rawData[1] == 0x01)
                    FirmwareVersion = $"{_rawData[0x11]}.{_rawData[0x12]}.{_rawData[0x13]}";
            }

            if (FirmwareVersion == null)
                return;

            // Liquid temperature
            _liquidTemperature = new Sensor("Liquid", 0, SensorType.Temperature, this, [], settings);
            ActivateSensor(_liquidTemperature);

            // Pump Control
            _pump = new Sensor("Pump Control", 0, SensorType.Control, this, [], settings);
            Control pumpControl = new(_pump, settings, 60, 100);
            _pump.Control = pumpControl;
            pumpControl.ControlModeChanged += c => ControlValueChanged(_pump, c);
            pumpControl.SoftwareControlValueChanged += c => ControlValueChanged(_pump, c);
            ControlValueChanged(_pump, pumpControl);
            ActivateSensor(_pump);

            // Pump RPM
            _pumpRpm = new Sensor("Pump", 0, SensorType.Fan, this, [], settings);
            ActivateSensor(_pumpRpm);

            if (_fanControl)
            {
                // Fan Control
                _fan = new Sensor("Fans Control", 1, SensorType.Control, this, [], settings);
                Control fanControl = new(_fan, settings, 0, 100);
                _fan.Control = fanControl;
                fanControl.ControlModeChanged += c => ControlValueChanged(_fan, c);
                fanControl.SoftwareControlValueChanged += c => ControlValueChanged(_fan, c);
                ControlValueChanged(_fan, fanControl);
                ActivateSensor(_fan);

                // Fan RPM
                _fanRpm = new Sensor("Fans", 1, SensorType.Fan, this, [], settings);
                ActivateSensor(_fanRpm);
            }

            IsValid = true;
        }
        catch
        { }
    }

    public string FirmwareVersion { get; }

    public override HardwareType HardwareType => HardwareType.Cooler;

    public bool IsValid { get; }

    public string Status => FirmwareVersion != _supportedFirmware ? $"Status: Untested firmware version {FirmwareVersion}! Please consider updating to version {_supportedFirmware}" : "Status: OK";

    private void ControlValueChanged(Sensor sensor, IControl control)
    {
        try
        {
            if (control.ControlMode == ControlMode.Software)
            {
                //value will be updated at next Update()
                sensor.Value = control.SoftwareValue;
                _lastUpdate = DateTime.MinValue;
                Update();
            }
            else
            {
                //will let the device handle the value
                sensor.Value = null;
            }
        }
        catch (ObjectDisposedException)
        {
            // Could be unplugged, or the app is stopping...
        }
    }

    public override void Update()
    {
        try
        {
            using HidStream stream = _device.Open();

            stream.Read(_rawData);

            // if not 0x04, it is not temperature data
            if (_rawData[0] != 0x04)
                return;

            // some packet may have 0 as temperature, don't know why just ignore it
            if (_rawData[1] == 0x00)
                return;

            _liquidTemperature.Value = _rawData[1] + (_rawData[2] / 10.0f);
            _fanRpm.Value = (_rawData[3] << 8) | _rawData[4];
            _pumpRpm.Value = (_rawData[5] << 8) | _rawData[6];

            // if we don't have control over the fan or pump, we don't need to update
            if (!_pump.Value.HasValue && (!_fanControl || !_fan.Value.HasValue))
                return;

            //control value need to be updated every 5 seconds or it falls back to default
            if (DateTime.Now - _lastUpdate < _interval)
                return;

            if (_fanControl && _fan.Value.HasValue)
                SetDuty(stream, _fanChannel, (byte)_fan.Value);

            if (_fanControl && _pump.Value.HasValue)
                SetDuty(stream, _pumpChannel, (byte)_pump.Value);
        }
        catch (ObjectDisposedException)
        {
            // Could be unplugged, or the app is stopping...
        }
    }

    private void SetDuty(HidStream stream, byte channel, byte duty)
    {
        SetDuty(stream, channel, 0, duty);
    }

    private void SetDuty(HidStream stream, byte channel, byte temperature, byte duty)
    {
        stream.Write([0x2, 0x4d, channel, temperature, duty]);
        _lastUpdate = DateTime.Now;
    }
}
