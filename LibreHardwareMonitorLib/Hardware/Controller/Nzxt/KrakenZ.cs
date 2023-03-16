using System;
using System.Threading;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Controller.Nzxt;

/**
     * Support for the KrakenZ devices from NZXT
     */
internal sealed class KrakenZ : Hardware
{
    private static readonly byte[] _getFirmwareInfo = { 0x10, 0x01 };
    private static readonly byte[] _setPumpSpeedHeader = { 0x72, 0x01, 0x00, 0x00 };
    private static readonly byte[] _setFanSpeedHeader = { 0x72, 0x02, 0x00, 0x00 };
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

    private volatile bool _controlling;

    private static void FillTargetArray(byte[] targetArray, byte value)
    {
        for (byte i = 4; i < targetArray.Length; i++)
            targetArray[i] = value;
    }

    static KrakenZ()
    {
        // Init pump control target array
        Array.Copy(_setPumpSpeedHeader, 0, _setPumpTarget, 0, 4);
        // Fill target array with a pump duty of 60%
        FillTargetArray(_setPumpTarget, 60);

        // Init fan control target array
        Array.Copy(_setFanSpeedHeader, 0, _setFanTarget, 0, 4);
        // Fill target array with a fan duty of 40%
        FillTargetArray(_setFanTarget, 40);
    }

    public KrakenZ(HidDevice dev, ISettings settings) : base("Nzxt Kraken Z", new Identifier("nzxt", "krakenz", dev.GetSerialNumber().TrimStart('0')), settings)
    {
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

            Name = "NZXT KrakenZ";

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

            // Liquid temperature
            _temperature = new Sensor("Liquid", 0, SensorType.Temperature, this, Array.Empty<ParameterDescription>(), settings);
            ActivateSensor(_temperature);

            ThreadPool.UnsafeQueueUserWorkItem(ContinuousRead, _rawData);
        }
    }

    public string FirmwareVersion { get; }

    public override HardwareType HardwareType => HardwareType.Cooler;

    public string Status => FirmwareVersion != "5.7.0" ? $"Status: Untested Firmware Version {FirmwareVersion}! Please consider Updating to Version 5.7.0" : "Status: OK";

    private void PumpSoftwareControlValueChanged(Control control)
    {
        if (control.ControlMode == ControlMode.Software)
        {
            float value = control.SoftwareValue;
            FillTargetArray(_setPumpTarget, (byte)value);

            _controlling = true;
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

    private void FanSoftwareControlValueChanged(Control control)
    {
        if (control.ControlMode == ControlMode.Software)
        {
            float value = control.SoftwareValue;
            FillTargetArray(_setFanTarget, (byte)value);

            _controlling = true;
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

    public override void Close()
    {
        base.Close();
        _stream?.Close();
    }

    private void ContinuousRead(object state)
    {
        byte[] buffer = new byte[_rawData.Length];
        while (_stream.CanRead)
        {
            try
            {
                _stream.Write(_status_req); // Request status
                _stream.Read(buffer); // This is a blocking call, will wait for bytes to become available

                lock (_rawData)
                {
                    Array.Copy(buffer, _rawData, buffer.Length);
                }
            }
            catch (TimeoutException)
            {
                // Don't care, just make sure the stream is still open
                Thread.Sleep(500);
            }
            catch (ObjectDisposedException)
            {
                // Could be unplugged, or the app is stopping...
                return;
            }
        }
    }

    public override void Update()
    {
        lock (_rawData)
        {
            if (_rawData[0] == 0x75 /*status response*/ && _rawData.Length >= 30)
            {
                _temperature.Value = _rawData[15] + (_rawData[16] / 10.0f);
                _pumpRpm.Value = (_rawData[18] << 8) | _rawData[17];
                _fanRpm.Value = (_rawData[24] << 8) | _rawData[23];

                // The following logic makes sure the pump is set to the controlling value. This pump sometimes sets itself to 0% when instructed to a value.
                if (!_controlling)
                {
                    _pump.Value = _rawData[19];
                    _fan.Value = _rawData[25];
                }
                else if (_pump.Value != _rawData[19] || _fan.Value != _rawData[25])
                {
                    if (_pump.Value != _rawData[19])
                    {
                        float value = _pump.Value.GetValueOrDefault();
                        FillTargetArray(_setPumpTarget, (byte)value);
                        _stream.Write(_setPumpTarget);
                    }

                    if (_fan.Value != _rawData[25])
                    {
                        float value = _fan.Value.GetValueOrDefault();
                        FillTargetArray(_setFanTarget, (byte)value);
                        _stream.Write(_setFanTarget);
                    }
                }
                else
                {
                    _controlling = false;
                }
            }
        }
    }
}
