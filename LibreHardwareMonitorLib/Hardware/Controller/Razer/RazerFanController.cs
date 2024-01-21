// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Controller.Razer;

internal sealed class RazerFanController : Hardware
{
    private const int DEFAULT_SPEED_CHANNEL_POWER = 50;
    private const byte PERCENT_MIN = 0;
    private const byte PERCENT_MAX = 100;
    private const int DEVICE_READ_DELAY_MS = 5;
    private const int DEVICE_READ_TIMEOUT_MS = 500;
    private const int CHANNEL_COUNT = 8;
    //private const int FORCE_WRITE_SPEEDS_INTERVAL_MS = 2500; // TODO: Add timer

    private HidStream _stream;
    private readonly HidDevice _device;
    private readonly SequenceCounter _sequenceCounter = new();

    private readonly float?[] _pwm = new float?[CHANNEL_COUNT];
    private readonly Sensor[] _pwmControls = new Sensor[CHANNEL_COUNT];
    private readonly Sensor[] _rpmSensors = new Sensor[CHANNEL_COUNT];

    public RazerFanController(HidDevice dev, ISettings settings) : base("Razer PWM PC Fan Controller", new Identifier(dev.DevicePath), settings)
    {
        _device = dev;

        if (_device.TryOpen(out _stream))
        {
            _stream.ReadTimeout = 5000;

            Packet packet = new Packet
            {
                SequenceNumber = _sequenceCounter.Next(),
                DataLength = 0,
                CommandClass = CommandClass.Info,
                Command = 0x87,
            };

            if (Mutexes.WaitRazer(250))
            {
                while (FirmwareVersion == null)
                {
                    Thread.Sleep(DEVICE_READ_DELAY_MS);

                    try
                    {
                        Packet response = TryWriteAndRead(packet);
                        FirmwareVersion = $"{response.Data[0]:D}.{response.Data[1]:D2}.{response.Data[2]:D2}";
                    }
                    catch { }
                }

                Mutexes.ReleaseRazer();
            }

            Name = "Razer PWM PC Fan Controller";

            for (int i = 0; i < CHANNEL_COUNT; i++)
            {
                // Fan Control
                _pwmControls[i] = new Sensor("Fan Control #" + (i + 1), i, SensorType.Control, this, Array.Empty<ParameterDescription>(), settings);
                Control fanControl = new(_pwmControls[i], settings, PERCENT_MIN, PERCENT_MAX);
                _pwmControls[i].Control = fanControl;
                fanControl.ControlModeChanged += FanSoftwareControlValueChanged;
                fanControl.SoftwareControlValueChanged += FanSoftwareControlValueChanged;
                //fanControl.SetDefault();
                FanSoftwareControlValueChanged(fanControl);
                ActivateSensor(_pwmControls[i]);

                // Fan RPM
                _rpmSensors[i] = new Sensor("Fan #" + (i + 1), i, SensorType.Fan, this, Array.Empty<ParameterDescription>(), settings);
                ActivateSensor(_rpmSensors[i]);
            }
        }
    }

    public string FirmwareVersion { get; }

    public override HardwareType HardwareType => HardwareType.Cooler;

    public string Status => FirmwareVersion != "1.01.00" ? $"Status: Untested Firmware Version {FirmwareVersion}! Please consider Updating to Version 1.01.00" : "Status: OK";

    private void FanSoftwareControlValueChanged(Control control) // TODO: Add timer here
    {
        if (control.ControlMode == ControlMode.Undefined || !Mutexes.WaitRazer(250))
            return;

        if (control.ControlMode == ControlMode.Software)
        {
            SetChannelModeToManual(control.Sensor.Index);

            float value = control.SoftwareValue;
            byte fanSpeed = (byte)(value > 100 ? 100 : value < 0 ? 0 : value);

            var packet = new Packet
            {
                SequenceNumber = _sequenceCounter.Next(),
                DataLength = 3,
                CommandClass = CommandClass.Pwm,
                Command = PwmCommand.SetChannelPercent,
            };

            packet.Data[0] = 0x01;
            packet.Data[1] = (byte)(0x05 + control.Sensor.Index);
            packet.Data[2] = fanSpeed;

            TryWriteAndRead(packet);

            _pwm[control.Sensor.Index] = value;
        }
        else if (control.ControlMode == ControlMode.Default)
        {
            SetChannelModeToManual(control.Sensor.Index); // TODO: switch to auto mode here if it enabled before

            var packet = new Packet
            {
                SequenceNumber = _sequenceCounter.Next(),
                DataLength = 3,
                CommandClass = CommandClass.Pwm,
                Command = PwmCommand.SetChannelPercent,
            };

            packet.Data[0] = 0x01;
            packet.Data[1] = (byte)(0x05 + control.Sensor.Index);
            packet.Data[2] = DEFAULT_SPEED_CHANNEL_POWER;

            TryWriteAndRead(packet);

            _pwm[control.Sensor.Index] = DEFAULT_SPEED_CHANNEL_POWER;
        }

        Mutexes.ReleaseRazer();
    }

    private int GetChannelSpeed(int channel)
    {
        Packet packet = new Packet
        {
            SequenceNumber = _sequenceCounter.Next(),
            DataLength = 6,
            CommandClass = CommandClass.Pwm,
            Command = PwmCommand.GetChannelSpeed,
        };

        packet.Data[0] = 0x01;
        packet.Data[1] = (byte)(0x05 + channel);

        Packet response = TryWriteAndRead(packet);
        return (response.Data[4] << 8) | response.Data[5];
    }

    private void SetChannelModeToManual(int channel)
    {
        Packet packet = new Packet
        {
            SequenceNumber = _sequenceCounter.Next(),
            DataLength = 3,
            CommandClass = CommandClass.Pwm,
            Command = PwmCommand.SetChannelMode,
        };

        packet.Data[0] = 0x01;
        packet.Data[1] = (byte)(0x05 + channel);
        packet.Data[2] = 0x04;

        TryWriteAndRead(packet);
    }

    private void ThrowIfNotReady()
    {
        bool @throw;
        try
        {
            @throw = _stream is null;
        }
        catch (ObjectDisposedException)
        {
            @throw = true;
        }

        if (@throw)
        {
            throw new InvalidOperationException("The device is not ready.");
        }
    }

    private Packet TryWriteAndRead(Packet packet)
    {
        Packet readPacket = null;
        int devTimeout = 400;
        int devReconnectTimeout = 3000;

        do
        {
            try
            {
                byte[] response = Packet.CreateBuffer();
                byte[] buffer = packet.ToBuffer();

                ThrowIfNotReady();
                _stream?.SetFeature(buffer, 0, buffer.Length);
                Thread.Sleep(DEVICE_READ_DELAY_MS);
                ThrowIfNotReady();
                _stream?.GetFeature(response, 0, response.Length);
                readPacket = Packet.FromBuffer(response);

                if (readPacket.Status == DeviceStatus.Busy)
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();

                    while (stopwatch.ElapsedMilliseconds < DEVICE_READ_TIMEOUT_MS && readPacket.Status == DeviceStatus.Busy)
                    {
                        Thread.Sleep(DEVICE_READ_DELAY_MS);
                        ThrowIfNotReady();
                        _stream?.GetFeature(response, 0, response.Length);
                        readPacket = Packet.FromBuffer(response);
                    }
                }
            }
            catch (IOException) // Unexpected device disconnect or fan plug/unplug
            {
                if (devTimeout <= 0)
                {
                    while (devReconnectTimeout > 0)
                    {
                        _stream?.Close();
                        if (_device.TryOpen(out _stream))
                            break;

                        Thread.Sleep(1000);
                        devReconnectTimeout -= 500;
                    }

                    if (devReconnectTimeout <= 0) // Device disconnected
                    {
                        for (int i = 0; i < CHANNEL_COUNT; i++)
                        {
                            _pwmControls[i].Control = null;
                            _pwmControls[i].Value = null;
                            _rpmSensors[i].Value = null;
                            _pwm[i] = null;

                            DeactivateSensor(_pwmControls[i]);
                            DeactivateSensor(_rpmSensors[i]);
                        }

                        Close();

                        Packet ret = new Packet();
                        for (int i = 0; i < 80; i++)
                            ret.Data[i] = 0;
                        return ret;
                    }

                    devTimeout = 400;
                }

                Thread.Sleep(DEVICE_READ_DELAY_MS);
                devTimeout -= DEVICE_READ_DELAY_MS;
            }
        } while (readPacket == null);

        return readPacket;
    }

    public override void Close()
    {
        base.Close();
        _stream?.Close();
    }

    public override void Update()
    {
        if (!Mutexes.WaitRazer(250))
            return;

        for (int i = 0; i < CHANNEL_COUNT; i++)
        {
            _rpmSensors[i].Value = GetChannelSpeed(i);
            _pwmControls[i].Value = _pwm[i];
        }

        Mutexes.ReleaseRazer();
    }

    private enum DeviceStatus : byte
    {
        Default = 0x00,
        Busy = 0x01,
        Success = 0x02,
        Error = 0x03,
        Timeout = 0x04,
        Invalid = 0x05,
    }

    private enum ProtocolType : byte
    {
        Default = 0x00,
    }

    private static class CommandClass
    {
        public static readonly byte Info = 0x00;
        public static readonly byte Pwm = 0x0d;
    }

    private static class PwmCommand
    {
        public static readonly byte SetChannelPercent = 0x0d;
        public static readonly byte SetChannelMode = 0x02;
        public static readonly byte GetChannelSpeed = 0x81;
    }

    private sealed class Packet
    {
        public byte ReportId { get; set; }
        public DeviceStatus Status { get; set; }
        public byte SequenceNumber { get; set; }
        public short RemainingCount { get; set; }
        public ProtocolType ProtocolType { get; set; }
        public byte DataLength { get; set; }
        public byte CommandClass { get; set; }
        public byte Command { get; set; }
        public byte[] Data { get; } = new byte[80];
        public byte CRC { get; set; }
        public byte Reserved { get; set; }

        public byte[] ToBuffer()
        {
            byte[] buffer = CreateBuffer();
            buffer[0] = ReportId;
            buffer[1] = (byte)Status;
            buffer[2] = SequenceNumber;
            buffer[3] = (byte)((RemainingCount >> 8) & 0xff);
            buffer[4] = (byte)(RemainingCount & 0xff);
            buffer[5] = (byte)ProtocolType;
            buffer[6] = DataLength;
            buffer[7] = CommandClass;
            buffer[8] = Command;

            for (int i = 0; i < Data.Length; i++)
                buffer[9 + i] = Data[i];

            buffer[89] = GenerateChecksum(buffer);
            buffer[90] = Reserved;
            return buffer;
        }

        public static Packet FromBuffer(byte[] buffer)
        {
            var packet = new Packet
            {
                ReportId = buffer[0],
                Status = (DeviceStatus)buffer[1],
                SequenceNumber = buffer[2],
                RemainingCount = (short)((buffer[3] << 8) | buffer[4]),
                ProtocolType = (ProtocolType)buffer[5],
                DataLength = buffer[6],
                CommandClass = buffer[7],
                Command = buffer[8],
                CRC = buffer[89],
                Reserved = buffer[90]
            };

            for (int i = 0; i < packet.Data.Length; i++)
                packet.Data[i] = buffer[9 + i];

            return packet;
        }

        public static byte[] CreateBuffer() => new byte[91];

        internal static byte GenerateChecksum(byte[] buffer)
        {
            byte result = 0;
            for (int i = 3; i < 89; i++)
            {
                result = (byte)(result ^ buffer[i]);
            }
            return result;
        }
    }

    private sealed class SequenceCounter
    {
        private byte _sequenceId = 0x00;

        public byte Next()
        {
            while (_sequenceId == 0x00)
            {
                _sequenceId += 0x08;
            }

            return _sequenceId;
        }
    }
}
