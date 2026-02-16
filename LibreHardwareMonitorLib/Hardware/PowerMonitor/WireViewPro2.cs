// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) Florian K. (Blacktempel)
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using LibreHardwareMonitor.Interop.PowerMonitor;

namespace LibreHardwareMonitor.Hardware.PowerMonitor;

/// <summary>
/// Thermal Grizzly WireView Pro II power monitor.
/// </summary>
public sealed class WireViewPro2 : Hardware, IPowerMonitor
{
    public const string WelcomeMessage = "Thermal Grizzly WireView Pro II";

    /// <summary>
    /// Max RPM according to the8auer. This is a custom made fan.
    /// </summary>
    private const int MaxFanRPM = 5000;

    private const byte VendorID = 0xEF;
    private const byte ProductID = 0x05;

    /// <summary>
    /// Time the fan needs to ramp up by 10%.
    /// </summary>
    private static readonly TimeSpan FanRampupTime = TimeSpan.FromSeconds(3.5);

    private double _lastFanSpeedRpm;
    private DateTime _lastFanUpdateTime = DateTime.MinValue;

    private readonly int _baudRate;
    private readonly string _portName;
    private readonly List<WireViewPro2Sensor> _sensors = [];

    private SharedSerialPort _serialPort;

    public WireViewPro2(string portName, ISettings settings, int baud = 115200)
        : base("WireView Pro II", new Identifier("gpu-powermonitor", portName), settings)
    {
        _portName = portName;
        _baudRate = baud;

        Connect();

        if (IsConnected)
        {
            CreateSensors();
        }
    }

    public override HardwareType HardwareType => HardwareType.PowerMonitor;

    public bool IsConnected { get; private set; }

    public string UniqueID { get; private set; }

    public VendorDataStruct? VendorData { get; private set; }

    public int ConfigVersion => VendorData?.FwVersion > 2 ? 1 : 0;

    public static List<WireViewPro2> TryFindDevices(ISettings settings)
    {
        var devices = new List<WireViewPro2>();

        if (!Software.OperatingSystem.IsWindows8OrGreater)
        {
            return devices; //No Linux implementation yet
        }

        List<string> matches = Stm32PortFinder.FindMatchingComPorts(0x0483, 0x5740);

        WireViewPro2 wireViewPro2 = null;

        foreach (string port in matches)
        {
            try
            {
                wireViewPro2 = new WireViewPro2(port, settings);
                if (wireViewPro2.IsConnected)
                {
                    devices.Add(wireViewPro2);
                    continue;
                }

                wireViewPro2.Close();
                wireViewPro2 = null;
            }
            catch
            {
                wireViewPro2?.Close();
                wireViewPro2 = null;
            }
        }

        return devices;
    }

    public override string GetReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Name);

        foreach (WireViewPro2Sensor sensor in _sensors)
        {
            sb.AppendLine($"  Sensor: {sensor.Name} = {sensor.Value}");
        }

        if (VendorData.HasValue)
        {
            sb.AppendLine($"  {nameof(VendorData.Value.VendorId)} = {VendorData.Value.VendorId}");
            sb.AppendLine($"  {nameof(VendorData.Value.ProductId)} = {VendorData.Value.ProductId}");
            sb.AppendLine($"  {nameof(VendorData.Value.FwVersion)} = {VendorData.Value.FwVersion}");
        }

        sb.AppendLine($"  {nameof(UniqueID)} = {UniqueID}");

        return sb.ToString();
    }

    public override void Close()
    {
        Disconnect();

        base.Close();
    }

    public override void Update()
    {
        var deviceData = GetDeviceData();

        if (deviceData != null)
        {
            _sensors.ForEach(wvps => wvps.Update(deviceData));
        }
    }

    public DeviceData GetDeviceData()
    {
        SensorStruct? sensorValues = null;
        DeviceConfigStructV2? config = null;

        try
        {
            sensorValues = ReadSensorValues();

            config = ReadConfig();
        }
        catch (IOException)
        {
            //"A device attached to the system is not functioning."
            //Can happen rarely
        }
        catch (InvalidOperationException)
        {
            //Can happen sometimes if the device is disconnecting while reading
        }

        if (sensorValues.HasValue && config.HasValue)
        {
            return MapSensorStructure(sensorValues.Value, config.Value);
        }

        return null;
    }

    public DeviceConfigStructV2? ReadConfig()
    {
        if (!IsConnected)
        {
            return null;
        }

        int size = 0;

        switch (ConfigVersion)
        {
            case 0:
                size = Marshal.SizeOf<DeviceConfigStructV1>();
                break;
            case 1:
                size = Marshal.SizeOf<DeviceConfigStructV2>();
                break;
            default:
                return null;
        }

        var buf = SendCmd(UsbCmd.CMD_READ_CONFIG, size);

        if (buf == null)
        {
            return null;
        }

        switch (ConfigVersion)
        {
            case 0:
                var s = BytesToStructure<DeviceConfigStructV1>(buf);
                return StructureConversion.ConvertConfigV1ToV2(s);
            case 1:
                return BytesToStructure<DeviceConfigStructV2>(buf);
            default:
                return null;
        }
    }

    public void WriteConfig(DeviceConfigStructV2 config)
    {
        if (!IsConnected)
        {
            return;
        }

        byte[] payload = [];

        switch (ConfigVersion)
        {
            case 0:
                var s = StructureConversion.ConvertConfigV2ToV1(config);
                payload = StructureToBytes(s);
                break;
            case 1:
                payload = StructureToBytes(config);
                break;
            default:
                return;
        }

        byte[] frame = new byte[64];
        frame[0] = (byte)UsbCmd.CMD_WRITE_CONFIG;

        try
        {
            _serialPort.Open();
            _serialPort.DiscardInBuffer();

            const int maxPayloadPerFrame = 62;

            for (byte offset = 0; offset < payload.Length; offset += maxPayloadPerFrame)
            {
                int bytesToWrite = Math.Min(maxPayloadPerFrame, payload.Length - offset);

                frame[1] = offset;
                Buffer.BlockCopy(payload, offset, frame, 2, bytesToWrite);

                _serialPort.Write(frame, 0, bytesToWrite + 2);
            }
        }
        finally
        {
            _serialPort.Close();
        }
    }

    public void NonVolatileMemoryCommand(NVM_CMD cmd)
    {
        if (!IsConnected)
        {
            return;
        }

        SendData(
            [
                (byte)UsbCmd.CMD_NVM_CONFIG,
                0x55, //Magic
                0xAA, //Magic
                0x55, //Magic
                0xAA, //Magic
                (byte)cmd
            ]);
    }

    public void ScreenCmd(SCREEN_CMD cmd)
    {
        if (!IsConnected)
        {
            return;
        }

        SendData([(byte)UsbCmd.CMD_SCREEN_CHANGE, (byte)cmd]);
    }

    public void ClearFaults(int faultStatusMask = 0xFFFF, int faultLogMask = 0xFFFF)
    {
        if (!IsConnected)
        {
            return;
        }

        SendData(
            [
                (byte)UsbCmd.CMD_CLEAR_FAULTS,
                (byte)(faultStatusMask & 0xFF),
                (byte)((faultStatusMask >> 8) & 0xFF),
                (byte)(faultLogMask & 0xFF),
                (byte)((faultLogMask >> 8) & 0xFF)
            ]);
    }

    private void CreateSensors()
    {
        //Onboard temperature sensors
        AddSensor("Onboard Temperature In", 0, SensorType.Temperature, dd => (float)dd.OnboardTempInC);
        AddSensor("Onboard Temperature Out", 1, SensorType.Temperature, dd => (float)dd.OnboardTempOutC);

        //External temperature sensors, requires shipped temperature sensors to be connected
        AddSensor("External Temperature 1", 2, SensorType.Temperature, dd => (float)dd.ExternalTemp1C);
        AddSensor("External Temperature 2", 3, SensorType.Temperature, dd => (float)dd.ExternalTemp2C);

        //Pin voltages
        AddSensor("Pin 1 Voltage", 10, SensorType.Voltage, dd => (float)dd.PinVoltage[0]);
        AddSensor("Pin 2 Voltage", 11, SensorType.Voltage, dd => (float)dd.PinVoltage[1]);
        AddSensor("Pin 3 Voltage", 12, SensorType.Voltage, dd => (float)dd.PinVoltage[2]);
        AddSensor("Pin 4 Voltage", 13, SensorType.Voltage, dd => (float)dd.PinVoltage[3]);
        AddSensor("Pin 5 Voltage", 14, SensorType.Voltage, dd => (float)dd.PinVoltage[4]);
        AddSensor("Pin 6 Voltage", 15, SensorType.Voltage, dd => (float)dd.PinVoltage[5]);

        //Pin currents
        AddSensor("Total Current", 20, SensorType.Current, dd => (float)dd.SumCurrentA);
        AddSensor("Pin 1 Current", 21, SensorType.Current, dd => (float)dd.PinCurrent[0]);
        AddSensor("Pin 2 Current", 22, SensorType.Current, dd => (float)dd.PinCurrent[1]);
        AddSensor("Pin 3 Current", 23, SensorType.Current, dd => (float)dd.PinCurrent[2]);
        AddSensor("Pin 4 Current", 24, SensorType.Current, dd => (float)dd.PinCurrent[3]);
        AddSensor("Pin 5 Current", 25, SensorType.Current, dd => (float)dd.PinCurrent[4]);
        AddSensor("Pin 6 Current", 26, SensorType.Current, dd => (float)dd.PinCurrent[5]);

        //Power
        AddSensor("Total Power", 30, SensorType.Power, dd => (float)dd.SumPowerW);

        //Fan
        var fan = AddSensor("Fan", 40, SensorType.Fan, dd => CalculateFanSpeed(dd));
        var fanControl = new Control(fan, _settings, 0, 100);

        fan.Control = fanControl;
        fanControl.ControlModeChanged += OnFanControlModeChanged;
        fanControl.SoftwareControlValueChanged += OnSoftwareControlValueChanged;
    }

    private WireViewPro2Sensor AddSensor(string name, int index, SensorType sensorType, GetWireViewPro2SensorValue getValue)
    {
        var sensor = new WireViewPro2Sensor(name, index, sensorType, this, _settings, getValue);

        _sensors.Add(sensor);

        ActivateSensor(sensor);

        return sensor;
    }

    private double FromTemp(short temp)
    {
        return temp == 0 ? 0 : temp / 10.0;
    }

    private short ToTemp(double temp)
    {
        return (short)(temp * 10);
    }

    private void OnFanControlModeChanged(Control control)
    {
        var deviceData = GetDeviceData();

        if (deviceData == null)
        {
            return;
        }

        switch (control.ControlMode)
        {
            case ControlMode.Software:
                deviceData.Config.FanConfig.Mode = FanMode.FanModeFixed;
                break;
            case ControlMode.Default:
                //Set default values of TG Software
                deviceData.Config.FanConfig.Mode = FanMode.FanModeCurve;
                deviceData.Config.FanConfig.TempSource = TempSource.TempSourceTmax;

                deviceData.Config.FanConfig.TempMin = ToTemp(50);
                deviceData.Config.FanConfig.TempMax = ToTemp(80);

                deviceData.Config.FanConfig.DutyMin = 0;
                deviceData.Config.FanConfig.DutyMax = 100;
                break;
            default:
                break;
        }

        WriteConfig(deviceData.Config);
    }

    private void OnSoftwareControlValueChanged(Control control)
    {
        var deviceData = GetDeviceData();

        if (deviceData == null)
        {
            return;
        }

        byte value = (byte)control.SoftwareValue;

        if (value < 0)
        {
            value = 0;
        }
        else if (value > 100)
        {
            value = 100;
        }

        deviceData.Config.FanConfig.TempMin = ToTemp(0);
        deviceData.Config.FanConfig.TempMax = ToTemp(80);

        deviceData.Config.FanConfig.DutyMin = value;
        deviceData.Config.FanConfig.DutyMax = value;

        WriteConfig(deviceData.Config);
    }

    /// <summary>
    /// Fan speed for this device is an approximation based on the curve configuration and current temperatures.<br/>
    /// The device itself does not report actual fan speed.
    /// </summary>
    private float CalculateFanSpeed(DeviceData dd)
    {
        var fanConfig = dd.Config.FanConfig;
        double targetRpm;

        switch (fanConfig.Mode)
        {
            case FanMode.FanModeCurve:
                var tempMin = FromTemp(fanConfig.TempMin);
                var tempMax = FromTemp(fanConfig.TempMax);
                var currentTemperature = GetActiveTemperature(dd);

                if (tempMax <= tempMin || currentTemperature <= tempMin)
                {
                    targetRpm = fanConfig.DutyMin;
                }
                else if (currentTemperature >= tempMax)
                {
                    targetRpm = fanConfig.DutyMax;
                }
                else
                {
                    var temp = (currentTemperature - tempMin) / (tempMax - tempMin);
                    var fanSpeedInPercent = fanConfig.DutyMin + temp * (fanConfig.DutyMax - fanConfig.DutyMin);
                    targetRpm = fanSpeedInPercent;
                }

                targetRpm = targetRpm == 0 ? 0 : targetRpm / 100.0 * MaxFanRPM;
                break;
            case FanMode.FanModeFixed:
                targetRpm = fanConfig.DutyMin == 0 ? 0 : fanConfig.DutyMin / 100.0 * MaxFanRPM;
                break;
            default:
                return -1;
        }

        return (float)ApplyFanRamp(targetRpm);
    }

    private double ApplyFanRamp(double targetRpm)
    {
        var now = DateTime.UtcNow;

        if (_lastFanUpdateTime == DateTime.MinValue)
        {
            _lastFanSpeedRpm = targetRpm;
            _lastFanUpdateTime = now;
            return targetRpm;
        }

        var elapsed = now - _lastFanUpdateTime;

        if (elapsed <= TimeSpan.Zero)
        {
            return _lastFanSpeedRpm;
        }

        var maxPercentDeltaPerSecond = 0.1 / FanRampupTime.TotalSeconds; //10% per FanRampupTime
        var allowedPercentDelta = elapsed.TotalSeconds * maxPercentDeltaPerSecond;

        var rpmDelta = targetRpm - _lastFanSpeedRpm;
        var percentDelta = rpmDelta / MaxFanRPM;

        if (Math.Abs(percentDelta) > allowedPercentDelta)
        {
            rpmDelta = Math.Sign(percentDelta) * allowedPercentDelta * MaxFanRPM;
        }

        _lastFanSpeedRpm += rpmDelta;
        _lastFanUpdateTime = now;

        return _lastFanSpeedRpm;
    }

    private double GetActiveTemperature(DeviceData dd)
    {
        double temperature = 0;

        switch (dd.Config.FanConfig.TempSource)
        {
            case TempSource.TempSourceTsIn:
                temperature = dd.OnboardTempInC;
                break;
            case TempSource.TempSourceTsOut:
                temperature = dd.OnboardTempOutC;
                break;
            case TempSource.TempSourceTs1:
                temperature = dd.ExternalTemp1C;
                break;
            case TempSource.TempSourceTs2:
                temperature = dd.ExternalTemp2C;
                break;
            case TempSource.TempSourceTmax:
                temperature = Math.Max(
                    Math.Max(dd.OnboardTempInC, dd.OnboardTempOutC),
                    Math.Max(dd.ExternalTemp1C, dd.ExternalTemp2C));
                break;
            default:
                break;
        }

        return temperature;
    }

    private void Connect()
    {
        if (IsConnected)
        {
            return;
        }

        try
        {
            _serialPort = new SharedSerialPort(_portName, _baudRate, Parity.None, 8, StopBits.One);
            _serialPort.ReadTimeout = 1000;
            _serialPort.WriteTimeout = 1000;

            if (!ReadWelcomeMessage())
            {
                IsConnected = false;
                return;
            }

            VendorDataStruct? vendorData = ReadVendorData();

            if (vendorData.HasValue &&
                vendorData.Value.VendorId == VendorID &&
                vendorData.Value.ProductId == ProductID)
            {
                VendorData = vendorData.Value;
                UniqueID = ReadUniqueID();

                IsConnected = true;
            }
        }
        catch
        {
            if (_serialPort != null)
            {
                try
                {
                    if (_serialPort.IsOpen)
                    {
                        _serialPort.Close();
                    }
                }
                catch
                {
                    //Ignore exceptions during cleanup
                }
                finally
                {
                    _serialPort.Dispose();
                    _serialPort = null;
                }
            }

            IsConnected = false;
        }
    }

    private void Disconnect()
    {
        if (!IsConnected)
        {
            return;
        }

        if (_serialPort != null)
        {
            _serialPort.Close();
            _serialPort.Dispose();
            _serialPort = null;
        }

        IsConnected = false;

        VendorData = null;
        UniqueID = null;
    }

    private bool ReadWelcomeMessage(bool sendCmd = false)
    {
        int size = WelcomeMessage.Length + 1;

        var bytes = SendData([(byte)UsbCmd.CMD_WELCOME], size, true);

        return bytes == null ? false : Encoding.ASCII.GetString(bytes, 0, size).TrimEnd('\0').CompareTo(WelcomeMessage) == 0;
    }

    private VendorDataStruct? ReadVendorData()
    {
        var bytes = SendCmd(UsbCmd.CMD_READ_VENDOR_DATA, Marshal.SizeOf<VendorDataStruct>());

        return bytes == null ? null : BytesToStructure<VendorDataStruct>(bytes);
    }

    private string ReadUniqueID()
    {
        const int UIDBytes = 12;

        var bytes = SendCmd(UsbCmd.CMD_READ_UID, UIDBytes);

        return bytes == null ? null : BitConverter.ToString(bytes).Replace("-", string.Empty);
    }

    private SensorStruct? ReadSensorValues()
    {
        if (!IsConnected)
        {
            return null;
        }

        var bytes = SendCmd(UsbCmd.CMD_READ_SENSOR_VALUES, Marshal.SizeOf<SensorStruct>());

        return bytes == null ? null : BytesToStructure<SensorStruct>(bytes);
    }

    private DeviceData MapSensorStructure(SensorStruct sensorStruct, DeviceConfigStructV2 config)
    {
        var deviceData = new DeviceData
        {
            Connected = true,
            HardwareRevision = $"{VendorData?.VendorId}{VendorData?.ProductId}",
            FirmwareVersion = $"{VendorData?.FwVersion}",
            OnboardTempInC = sensorStruct.Ts[(int)SensorTs.SENSOR_TS_IN] / 10.0,
            OnboardTempOutC = sensorStruct.Ts[(int)SensorTs.SENSOR_TS_OUT] / 10.0,
            ExternalTemp1C = sensorStruct.Ts[(int)SensorTs.SENSOR_TS3] / 10.0,
            ExternalTemp2C = sensorStruct.Ts[(int)SensorTs.SENSOR_TS4] / 10.0,
            FaultStatus = sensorStruct.FaultStatus,
            FaultLog = sensorStruct.FaultLog,
            Config = config,
        };

        switch (sensorStruct.HpwrCapability)
        {
            case HpwrCapability.PSU_CAP_600W:
                deviceData.PsuCapabilityW = 600;
                break;
            case HpwrCapability.PSU_CAP_450W:
                deviceData.PsuCapabilityW = 450;
                break;
            case HpwrCapability.PSU_CAP_300W:
                deviceData.PsuCapabilityW = 300;
                break;
            case HpwrCapability.PSU_CAP_150W:
                deviceData.PsuCapabilityW = 150;
                break;
        }

        for (int i = 0; i < 6; ++i)
        {
            deviceData.PinVoltage[i] = sensorStruct.PowerReadings[i].Voltage / 1000.0;
            deviceData.PinCurrent[i] = sensorStruct.PowerReadings[i].Current / 1000.0;
        }

        return deviceData;
    }

    private byte[] SendCmd(UsbCmd cmd, int responseSize = 0, bool rts = false)
    {
        return SendData(new[] { (byte)cmd }, responseSize, rts);
    }

    private byte[] SendData(byte[] data, int responseSize = 0, bool rts = false)
    {
        if (_serialPort == null)
        {
            return null;
        }

        byte[] buf = null;

        try
        {
            lock (_serialPort)
            {
                _serialPort.Open();
                _serialPort.DiscardInBuffer();

                if (rts)
                {
                    _serialPort.RtsEnable = true;
                    Thread.Sleep(10);
                }

                _serialPort.Write(data, 0, data.Length);

                if (responseSize > 0)
                {
                    buf = ReadExact(responseSize);
                }

                if (rts)
                {
                    Thread.Sleep(10);
                    _serialPort.RtsEnable = false;
                }
            }
        }
        finally
        {
            _serialPort.Close();
        }

        return buf;
    }

    private byte[] ReadExact(int size)
    {
        byte[] buffer = new byte[size];

        int offset = 0;

        var sw = Stopwatch.StartNew();

        while (offset < size && sw.ElapsedMilliseconds < _serialPort.ReadTimeout)
        {
            try
            {
                if (_serialPort.BytesToRead > 0)
                {
                    offset += _serialPort.Read(buffer, offset, size - offset);
                }
            }
            catch (TimeoutException)
            {
                //Ignore timeout exceptions and continue reading
            }
        }

        return offset != size ? null : buffer;
    }

    private T BytesToStructure<T>(byte[] bytes) where T : struct
    {
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);

        try
        {
            return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    private byte[] StructureToBytes<T>(T value) where T : struct
    {
        int size = Marshal.SizeOf<T>();

        byte[] bytes = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);

        try
        {
            Marshal.StructureToPtr(value, ptr, false);
            Marshal.Copy(ptr, bytes, 0, size);
            return bytes;
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }
}
