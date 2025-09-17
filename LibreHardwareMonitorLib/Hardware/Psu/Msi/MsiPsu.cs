// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) 2024 demorfi<demorfi@gmail.com>
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Psu.Msi;

internal class SensorIndices
{
    private readonly Dictionary<SensorType, int> _lastIndices = new();

    public int NextIndex(SensorType type)
    {
        if (!_lastIndices.ContainsKey(type))
        {
            _lastIndices.Add(type, 0);
            return 0;
        }

        int res = _lastIndices[type] + 1;
        _lastIndices[type] = res;
        return res;
    }
}

internal sealed class MsiPsu : Hardware
{
    private readonly HidDevice _device;
    private readonly List<PsuSensor> _sensors = new();
    private readonly Sensor[] _rpmSensors = new Sensor[1];

    public MsiPsu(HidDevice device, ISettings settings) : base("MSI PSU", new Identifier(device), settings)
    {
        _device = device;
        using HidStream stream = device.Open();
        Name = $"MSI PSU{UsbApi.ProductName(stream)}";

        AddSensors(settings);
    }

    public override HardwareType HardwareType => HardwareType.Psu;

    public override void Update()
    {
        using HidStream stream = _device.Open();
        float[] info = UsbApi.InfoList(stream);
        _sensors.ForEach(s => s.Update(info));
    }

    private void AddSensors(ISettings settings)
    {
        SensorIndices indices = new();
        _sensors.Add(new PsuSensor("Case", indices, SensorType.Fan, this, settings, UsbApi.IndexInfo.FAN_RPM));
        _sensors.Add(new PsuSensor("Case", indices, SensorType.Temperature, this, settings, UsbApi.IndexInfo.TEMP));

        _sensors.Add(new PsuSensor("+12V", indices, SensorType.Voltage, this, settings, UsbApi.IndexInfo.VOLTS_12));
        _sensors.Add(new PsuSensor("+12V", indices, SensorType.Current, this, settings, UsbApi.IndexInfo.AMPS_12));

        _sensors.Add(new PsuSensor("+5V", indices, SensorType.Voltage, this, settings, UsbApi.IndexInfo.VOLTS_5));
        _sensors.Add(new PsuSensor("+5V", indices, SensorType.Current, this, settings, UsbApi.IndexInfo.AMPS_5));

        _sensors.Add(new PsuSensor("+3.3V", indices, SensorType.Voltage, this, settings, UsbApi.IndexInfo.VOLTS_3V3));
        _sensors.Add(new PsuSensor("+3.3V", indices, SensorType.Current, this, settings, UsbApi.IndexInfo.AMPS_3V3));

        _sensors.Add(new PsuSensor("PSU Efficiency", indices, SensorType.Level, this, settings, UsbApi.IndexInfo.EFFICIENCY));
        _sensors.Add(new PsuSensor("PSU Out", indices, SensorType.Power, this, settings, UsbApi.IndexInfo.PSU_OUT));
        _sensors.Add(new PsuSensor("Total Runtime", indices, SensorType.TimeSpan, this, settings, UsbApi.IndexInfo.RUNTIME, true));
    }

    private class PsuSensor : Sensor
    {
        private readonly UsbApi.IndexInfo _index;

        public PsuSensor(string name, SensorIndices indices, SensorType type, MsiPsu hardware, ISettings settings, UsbApi.IndexInfo index, bool noHistory = false)
            : base(name, indices.NextIndex(type), false, type, hardware, null, settings, noHistory)
        {
            _index = index;
            hardware.ActivateSensor(this);
        }

        public void Update(float[] info)
        {
            Value = info[(int)_index];
        }
    }
}

#region Exception classes

public class CommunicationProtocolError : ApplicationException
{
    public CommunicationProtocolError(HidDevice device, string message) : base($"Error communicating with the PSU controller at {device.DevicePath}: {message}")
    { }
}

#endregion

#region PSU USB communication protocol implementation

internal static class UsbApi
{
    static float Linear11ToFloat32(ushort val)
    {
        int exp = (short)val >> 11;
        int mant = ((short)(val & 0x7ff) << 5) >> 5;
        return mant * (float)Math.Pow(2, exp);
    }

    static bool request(HidStream stream, byte[] command, out byte[] response)
    {
        byte[] buffer = new byte[64];
        Array.Copy(command, 0, buffer, 1, 2);
        stream.Write(buffer);

        byte[] reply = stream.Read();
        response = new byte[42];
        Array.Copy(reply, 2, response, 0, 42);

        return reply[0] == buffer[0] && reply[1] == buffer[1];
    }

    public static String ProductName(HidStream stream)
    {
        if (!request(stream, new byte[2] { 0xFA, 0x51 }, out byte[] productArr))
            throw new CommunicationProtocolError(stream.Device, "Can't read product name");

        return Encoding.ASCII.GetString(productArr.TakeWhile(x => x != 0).ToArray());
    }

    public static float[] InfoList(HidStream stream)
    {
        int length = Enum.GetNames(typeof(IndexInfo)).Length;
        float[] info = new float[length];

        if (!request(stream, new byte[2] { 0x51, 0xE0 }, out byte[] basic))
            throw new CommunicationProtocolError(stream.Device, "Can't read basic info");

        // basic has information only about the first 20 sensors
        for (int i = 0; i < 20; i++)
        {
            byte[] replyData = new byte[4];
            Array.Copy(basic, (i * 2) + 1, replyData, 0, 2);
            info[i] = Linear11ToFloat32((ushort)BitConverter.ToInt32(replyData, 0));
        }

        // runtime info
        request(stream, new byte[2] { 0x51, 0xD1 }, out byte[] runtime);
        info[(int)IndexInfo.RUNTIME] = BitConverter.ToInt32(runtime, 0) / 100;

        return info;
    }

    public enum IndexInfo : int
    {
        VOLTS_12V1 = 0,
        VOLTS_12V2 = 2,
        VOLTS_12V3 = 4,
        VOLTS_12V4 = 6,
        VOLTS_12V5 = 8,
        VOLTS_12 = 10,
        VOLTS_5 = 12,
        VOLTS_3V3 = 14,
        AMPS_12V1 = 1,
        AMPS_12V2 = 3,
        AMPS_12V3 = 5,
        AMPS_12V4 = 7,
        AMPS_12V5 = 9,
        AMPS_12 = 11,
        AMPS_5 = 13,
        AMPS_3V3 = 15,

        PSU_OUT = 16,
        EFFICIENCY = 17,
        TEMP = 18,
        FAN_RPM = 19,
        RUNTIME = 20
    }
}

#endregion
