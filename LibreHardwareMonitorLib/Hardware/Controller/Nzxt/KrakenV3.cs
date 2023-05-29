using System;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Controller.Nzxt;

/**
     * Support for the KrakenZ devices from NZXT
     */
internal sealed class KrakenV3 : Hardware
{
    private static readonly byte[] _getFirmwareInfo = { 0x10, 0x01 };
    private static readonly byte[] _status_req = { 0x74, 0x01 };
    private static readonly byte[] _setPumpTarget = new byte[64];
    private static readonly byte[] _setFanTarget = new byte[64];

    private readonly Sensor _pump;
    private readonly Sensor _pumpRpm;
    private readonly Sensor _fan;
    private readonly Sensor _fanRpm;
    private readonly byte[] _rawData = new byte[64];
    private readonly HidStream _stream;
    private readonly Sensor _temperature;
    private readonly bool _fanControl;
    private readonly string _supportedFirmware;
    private volatile bool _controllingFans;
    private volatile bool _controllingPump;

    private static void FillTargetArray(byte[] targetArray, byte value)
    {
        for (byte i = 4; i < targetArray.Length; i++)
            targetArray[i] = value;
    }

    public KrakenV3(HidDevice dev, ISettings settings) : base("Nzxt Kraken Z", new Identifier("nzxt", "krakenz", dev.GetSerialNumber().TrimStart('0')), settings)
    {
        if (dev.ProductID == 0x3008)
        {
            Name = "NZXT Kraken Z3";
            _fanControl = true;
            _supportedFirmware = "5.7.0";
            Array.Copy(new byte[] { 0x72, 0x01, 0x00, 0x00 }, 0, _setPumpTarget, 0, 4);
            Array.Copy(new byte[] { 0x72, 0x02, 0x00, 0x00 }, 0, _setFanTarget, 0, 4);

        }
        else if (dev.ProductID == 0x300C)
        {
            Name = "NZXT Kraken Elite";
            _fanControl = true;
            _supportedFirmware = "1.2.4";
            Array.Copy(new byte[] { 0x72, 0x01, 0x01, 0x00 }, 0, _setPumpTarget, 0, 4);
            Array.Copy(new byte[] { 0x72, 0x02, 0x01, 0x01 }, 0, _setFanTarget, 0, 4);

        }
        else if (dev.ProductID == 0x300E)
        {
            Name = "NZXT Kraken";
            _fanControl = true;
            _supportedFirmware = "1.2.4"; // Firmware version to be confirmed
            Array.Copy(new byte[] { 0x72, 0x01, 0x01, 0x00 }, 0, _setPumpTarget, 0, 4);
            Array.Copy(new byte[] { 0x72, 0x02, 0x01, 0x01 }, 0, _setFanTarget, 0, 4);
        }
        else
        {
            Name = "NZXT Kraken X3";
            _fanControl = false;
            _supportedFirmware = "2.1.0";
            Array.Copy(new byte[] { 0x72, 0x01, 0x00, 0x00 }, 0, _setPumpTarget, 0, 4);
        }

        FillTargetArray(_setPumpTarget, 60);
        FillTargetArray(_setFanTarget, 40);

        if (dev.TryOpen(out _stream))
        {
            _stream.ReadTimeout = 5000;

            _stream.Write(_getFirmwareInfo);
            do
            {
                _stream.Read(_rawData);
                if (_rawData[0] == 0x11 && _rawData[1] == 0x01)
                {
                    FirmwareVersion = $"{_rawData[0x11]}.{_rawData[0x12]}.{_rawData[0x13]}";
                }
            }
            while (FirmwareVersion == null);

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
        }
    }

    public string FirmwareVersion { get; }

    public override HardwareType HardwareType => HardwareType.Cooler;

    public string Status => FirmwareVersion != _supportedFirmware ? $"Status: Untested Firmware Version {FirmwareVersion}! Please consider Updating to Version {_supportedFirmware}" : "Status: OK";

    private void PumpSoftwareControlValueChanged(Control control)
    {
        try
        {
            if (control.ControlMode == ControlMode.Software)
            {
                float value = control.SoftwareValue;

                FillTargetArray(_setPumpTarget, (byte)(value > 100 ? 100 : value < 0 ? 0 : value));

                _controllingPump = true;
                _stream.Write(_setPumpTarget);
                _pump.Value = value;
            }
            else if (control.ControlMode == ControlMode.Default)
            {
                // There isn't a "default" mode with this pump, but a safe setting is 60%
                FillTargetArray(_setPumpTarget, 60);
                _stream.Write(_setPumpTarget);
            }
        }
        catch (ObjectDisposedException)
        {
            // Could be unplugged, or the app is stopping...
            return;
        }
    }

    private void FanSoftwareControlValueChanged(Control control)
    {
        try
        {
            if (control.ControlMode == ControlMode.Software)
            {
                float value = control.SoftwareValue;
                FillTargetArray(_setFanTarget, (byte)(value > 100 ? 100 : value < 0 ? 0 : value));

                _controllingFans = true;
                _stream.Write(_setFanTarget);
                _fan.Value = value;
            }
            else if (control.ControlMode == ControlMode.Default)
            {
                // There isn't a "default" mode with this fan, but a safe setting is 40%
                FillTargetArray(_setFanTarget, 40);
                _stream.Write(_setFanTarget);
            }
        }
        catch (ObjectDisposedException)
        {
            // Could be unplugged, or the app is stopping...
            return;
        }
    }

    public override void Close()
    {
        base.Close();
        _stream?.Close();
    }

    public override void Update()
    {
        try
        {
            _stream.Write(_status_req);
            do
            {
                _stream.Read(_rawData);
            } while (_rawData[0] != 0x75 || _rawData[1] != 0x1);

            _temperature.Value = _rawData[15] + (_rawData[16] / 10.0f);
            _pumpRpm.Value = (_rawData[18] << 8) | _rawData[17];

            // The following logic makes sure the pump is set to the controlling value. This pump sometimes sets itself to 0% when instructed to a value.
            if (!_controllingPump)
            {
                _pump.Value = _rawData[19];
            }
            else if (_pump.Value != _rawData[19])
            {
                float value = _pump.Value.GetValueOrDefault();
                FillTargetArray(_setPumpTarget, (byte)value);
                _stream.Write(_setPumpTarget);
            }
            else
            {
                _controllingPump = false;
            }

            if (_fanControl)
            {
                _fanRpm.Value = (_rawData[24] << 8) | _rawData[23];
                if (!_controllingFans)
                {
                    _fan.Value = _rawData[25];
                }
                else if (_fan.Value != _rawData[25])
                {
                    float value = _fan.Value.GetValueOrDefault();
                    FillTargetArray(_setFanTarget, (byte)value);
                    _stream.Write(_setFanTarget);
                }
                else
                {
                    _controllingFans = false;
                }
            }
        }
        catch (ObjectDisposedException)
        {
            // Could be unplugged, or the app is stopping...
            return;
        }
    }
}
