using System;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Controller.Nzxt;

/**
     * Support for the Kraken X (X42, X52, X62 or X72) devices from NZXT
     */
internal sealed class KrakenV2 : Hardware
{

    private readonly HidDevice _device;

    private readonly Sensor _fan;
    private readonly bool _fanControl;
    private readonly Sensor _fanRpm;
    private readonly Sensor _pump;
    private readonly Sensor _pumpRpm;
    private readonly byte[] _rawData = new byte[64];

    private readonly string _supportedFirmware;
    private readonly Sensor _temperature;

    private byte[] _standardTemperatures;

    public KrakenV2(HidDevice dev, ISettings settings) : base("Nzxt Kraken X", new Identifier("nzxt", "krakenx", dev.GetSerialNumber().TrimStart('0')), settings)
    {
        _device = dev;

        switch (dev.ProductID)
        {
            case 0x170e:
            default:
                Name = "NZXT Kraken X";
                _fanControl = true;
                _supportedFirmware = "6.2.0";
                break;
        }

        SetStandardTemps();

        if (dev.TryOpen(out HidStream stream))
        {
            stream.ReadTimeout = 5000;

            stream.Write(new byte[]{ 0x10, 0x01 });

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
            _temperature = new Sensor("Liquid", 0, SensorType.Temperature, this, Array.Empty<ParameterDescription>(), settings);
            ActivateSensor(_temperature);

            // Pump Control
            _pump = new Sensor("Pump Control", 0, SensorType.Control, this, Array.Empty<ParameterDescription>(), settings);
            Control pumpControl = new(_pump, settings, 20, 100);
            _pump.Control = pumpControl;
            pumpControl.ControlModeChanged += PumpSoftwareControlValueChanged;
            pumpControl.SoftwareControlValueChanged += PumpSoftwareControlValueChanged;
            PumpSoftwareControlValueChanged(pumpControl);
            ActivateSensor(_pump);

            // Pump RPM
            _pumpRpm = new Sensor("Pump", 0, SensorType.Fan, this, Array.Empty<ParameterDescription>(), settings);
            ActivateSensor(_pumpRpm);

            if (_fanControl)
            {
                // Fan Control
                _fan = new Sensor("Fans Control", 1, SensorType.Control, this, Array.Empty<ParameterDescription>(), settings);
                Control fanControl = new(_fan, settings, 20, 100);
                _fan.Control = fanControl;
                fanControl.ControlModeChanged += FanSoftwareControlValueChanged;
                fanControl.SoftwareControlValueChanged += FanSoftwareControlValueChanged;
                FanSoftwareControlValueChanged(fanControl);
                ActivateSensor(_fan);

                // Fan RPM
                _fanRpm = new Sensor("Fans", 1, SensorType.Fan, this, Array.Empty<ParameterDescription>(), settings);
                ActivateSensor(_fanRpm);
            }

            IsValid = true;
        }
    }

    public string FirmwareVersion { get; }

    public override HardwareType HardwareType => HardwareType.Cooler;

    public bool IsValid { get; }

    public string Status => FirmwareVersion != _supportedFirmware ? $"Status: Untested firmware version {FirmwareVersion}! Please consider updating to version {_supportedFirmware}" : "Status: OK";

    private void PumpSoftwareControlValueChanged(Control control)
    {
        try
        {
            switch (control.ControlMode)
            {
                case ControlMode.Software:
                    float value = control.SoftwareValue;
                    SetPumpDuty((byte)(value > 100 ? 100 : value < 0 ? 0 : value));

                    _pump.Value = value;
                    break;
                case ControlMode.Default:
                    SetPumpDutyDefault();
                    break;
            }
        }
        catch (ObjectDisposedException)
        {
            // Could be unplugged, or the app is stopping...
        }
    }

    private void FanSoftwareControlValueChanged(Control control)
    {
        try
        {
            switch (control.ControlMode)
            {
                case ControlMode.Software:
                    float value = control.SoftwareValue;
                    SetFanDuty((byte)(value > 100 ? 100 : value < 0 ? 0 : value));
                    _fan.Value = value;
                    break;
                case ControlMode.Default:
                    SetFanDutyDefault();
                    break;
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
            if (_device.TryOpen(out HidStream stream))
            {

                stream.Read(_rawData);
                if (_rawData[0] != 0x04 || _rawData[1] == 0x00) return;

                _temperature.Value = _rawData[1] + (_rawData[2] / 10.0f);
                _fanRpm.Value = (_rawData[3] << 8) | _rawData[4];
                _pumpRpm.Value = (_rawData[5] << 8) | _rawData[6];

                stream.Close();
            }
        }
        catch (ObjectDisposedException)
        {
            // Could be unplugged, or the app is stopping...
        }
    }


    private void SetStandardTemps()
    {
        _standardTemperatures = new byte[36];
        int i =0;
        for (byte t=20; t < 50; t++) { _standardTemperatures[i++] = t; }
        for (byte t=50; t <= 60; t+=2) { _standardTemperatures[i++] = t; }
    }

    private void SetFanDuty(byte duty)
    {
        if (_device.TryOpen(out HidStream stream))
        {
            byte channel = 0x80;
            foreach(byte temp in _standardTemperatures)
            {
                if (temp < 60) SetProfileValue(stream, channel++, temp, duty);
                else SetProfileValue(stream, channel++, temp, 100);
            }
            stream.Close();
        }
    }

    private void SetPumpDuty(byte duty)
    {
        if (_device.TryOpen(out HidStream stream))
        {
            byte channel = 0xc0;
            foreach(byte temp in _standardTemperatures)
            {
                if (temp < 50) SetProfileValue(stream, channel++, temp, duty);
                else SetProfileValue(stream, channel++, temp, 100);
            }
            stream.Close();
        }
    }

    private void SetFanDutyDefault()
    {
        if (_device.TryOpen(out HidStream stream))
        {
            byte channel = 0x80;
            foreach(byte temp in _standardTemperatures)
            {
                if (temp < 20) SetProfileValue(stream, channel++, temp, 25);
                else if (temp < 30) SetProfileValue(stream, channel++, temp, Interpolate(temp,20,30,25,50));
                else if (temp < 50) SetProfileValue(stream, channel++, temp, Interpolate(temp,30,50,50,90));
                else if (temp < 60) SetProfileValue(stream, channel++, temp, Interpolate(temp,50,60,90,100));
                else SetProfileValue(stream, channel++, temp, 100);
            }
            stream.Close();
        }
    }

    private void SetPumpDutyDefault()
    {
        if (_device.TryOpen(out HidStream stream))
        {
            byte channel = 0xC0;
            foreach(byte temp in _standardTemperatures)
            {
                if (temp < 20) SetProfileValue(stream, channel++, temp, 50);
                else if (temp < 30) SetProfileValue(stream, channel++, temp, Interpolate(temp,20,30,50,60));
                else if (temp < 40) SetProfileValue(stream, channel++, temp, Interpolate(temp,30,40,60,90));
                else if (temp < 50) SetProfileValue(stream, channel++, temp, Interpolate(temp,40,50,90,100));
                else SetProfileValue(stream, channel++, temp, 100);
            }
            stream.Close();
        }
    }

    private byte Interpolate(byte temp, byte minTemp, byte maxTemp, byte minDuty, byte maxDuty)
    {
        double ratio = (double)(temp - minTemp) / (double)(maxTemp - minTemp);
        return (byte)(minDuty + ((maxDuty - minDuty) * ratio));
    }

    private void SetProfileValue(HidStream stream, byte channel, byte temp, byte duty)
    {
            stream.Write(new byte[]{0x2,0x4d,channel,temp,duty});
    }


}
