// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) 2020 Wilken Gottwalt<wilken.gottwalt@posteo.net>
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.
// Implemented after the Linuix kernel driver corsair_psu by Wilken Gottwalt and contributers

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Psu.Corsair;

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

// might need refactoring into two classes if AXi series API differs significantly
internal sealed class CorsairPsu : Hardware
{
    private readonly List<CompositeSensor> _compositeSensors = new();
    private readonly HidDevice _device;
    private readonly List<PsuSensor> _sensors = new();

    public CorsairPsu(HidDevice device, ISettings settings, int index)
        : base("Corsair PSU", new Identifier("psu", "corsair", index.ToString()), settings)
    {
        _device = device;
        using HidStream stream = device.Open();

        UsbApi.Init(stream);
        UsbApi.FirmwareInfo fwInfo = UsbApi.FwInfo(stream);
        Name = $"{CultureInfo.InvariantCulture.TextInfo.ToTitleCase(fwInfo.Vendor.ToLowerInvariant())} {fwInfo.Product}";

        AddSensors(UsbApi.GetOptionalCommands(stream), UsbApi.GetCriticals(stream), settings);
    }

    public override HardwareType HardwareType => HardwareType.Psu;

    public override IDictionary<string, string> Properties
    {
        get
        {
            SortedDictionary<string, string> properties = new();

            using HidStream stream = _device.Open();

            float? mode = UsbApi.GetValue(stream, UsbApi.Command.OCPMODE, 0);
            if (mode.HasValue)
                properties.Add("Over-current protection", mode > 1.0 ? "multi-rail" : "single-rail");

            return properties;
        }
    }

    public override void Update()
    {
        using HidStream stream = _device.Open();
        _sensors.ForEach(s => s.Update(stream));
    }

    private void AddSensors(UsbApi.OptionalCommands optionalCommands, UsbApi.Criticals criticals, ISettings settings)
    {
        SensorIndices indices = new();
        _sensors.Add(new PsuSensorWithLimits("VRM",
                                             indices,
                                             SensorType.Temperature,
                                             this,
                                             settings,
                                             UsbApi.Command.TEMP0,
                                             null,
                                             criticals.TempMax[0]));

        _sensors.Add(new PsuSensorWithLimits("Case",
                                             indices,
                                             SensorType.Temperature,
                                             this,
                                             settings,
                                             UsbApi.Command.TEMP1,
                                             null,
                                             criticals.TempMax[1]));

        _sensors.Add(new PsuSensor("Case", indices, SensorType.Fan, this, settings, UsbApi.Command.FAN_RPM));

        _sensors.Add(new PsuSensor("Input", indices, SensorType.Voltage, this, settings, UsbApi.Command.IN_VOLTS));
        _sensors.Add(new PsuSensorWithLimits("+12V",
                                             indices,
                                             SensorType.Voltage,
                                             this,
                                             settings,
                                             UsbApi.Command.RAIL_VOLTS,
                                             criticals.VoltageMin[(byte)Rail._12V],
                                             criticals.VoltageMax[(byte)Rail._12V]));

        _sensors.Add(new PsuSensorWithLimits("+5V",
                                             indices,
                                             SensorType.Voltage,
                                             this,
                                             settings,
                                             UsbApi.Command.RAIL_VOLTS,
                                             criticals.VoltageMin[(byte)Rail._5V],
                                             criticals.VoltageMax[(byte)Rail._5V],
                                             Rail._5V));

        _sensors.Add(new PsuSensorWithLimits("+3.3V",
                                             indices,
                                             SensorType.Voltage,
                                             this,
                                             settings,
                                             UsbApi.Command.RAIL_VOLTS,
                                             criticals.VoltageMin[(byte)Rail._3V],
                                             criticals.VoltageMax[(byte)Rail._3V],
                                             Rail._3V));

        if (optionalCommands.HasFlag(UsbApi.OptionalCommands.InputCurrent))
        {
            _sensors.Add(new PsuSensor("Input", indices, SensorType.Current, this, settings, UsbApi.Command.IN_AMPS));
        }

        _sensors.Add(new PsuSensorWithLimits("+12V",
                                             indices,
                                             SensorType.Current,
                                             this,
                                             settings,
                                             UsbApi.Command.RAIL_AMPS,
                                             null,
                                             criticals.CurrentMax[(byte)Rail._12V]));

        _sensors.Add(new PsuSensorWithLimits("+5V",
                                             indices,
                                             SensorType.Current,
                                             this,
                                             settings,
                                             UsbApi.Command.RAIL_AMPS,
                                             null,
                                             criticals.CurrentMax[(byte)Rail._5V],
                                             Rail._5V));

        _sensors.Add(new PsuSensorWithLimits("+3.3V",
                                             indices,
                                             SensorType.Current,
                                             this,
                                             settings,
                                             UsbApi.Command.RAIL_AMPS,
                                             null,
                                             criticals.CurrentMax[(byte)Rail._3V],
                                             Rail._3V));

        PsuSensor[] powerRails =
        {
            new("+12V", indices, SensorType.Power, this, settings, UsbApi.Command.RAIL_WATTS),
            new("+5V", indices, SensorType.Power, this, settings, UsbApi.Command.RAIL_WATTS, Rail._5V),
            new("+3.3V", indices, SensorType.Power, this, settings, UsbApi.Command.RAIL_WATTS, Rail._3V)
        };

        _sensors.AddRange(powerRails);
        _sensors.Add(new PsuSensor("Total watts", indices, SensorType.Power, this, settings, UsbApi.Command.TOTAL_WATTS));
        _compositeSensors.Add(new CompositeSensor("Total Output",
                                                  indices.NextIndex(SensorType.Power),
                                                  SensorType.Power,
                                                  this,
                                                  settings,
                                                  powerRails,
                                                  (acc, sensor) => acc + sensor.Value ?? 0f));

        ActivateSensor(_compositeSensors[_compositeSensors.Count - 1]);

        _sensors.Add(new PsuSensor("Uptime", indices, SensorType.TimeSpan, this, settings, UsbApi.Command.UPTIME, Rail._12V, true));
        _sensors.Add(new PsuSensor("Total uptime", indices, SensorType.TimeSpan, this, settings, UsbApi.Command.TOTAL_UPTIME, Rail._12V, true));
    }

    private class PsuSensor : Sensor
    {
        private readonly UsbApi.Command _cmd;
        private readonly byte _rail;

        public PsuSensor(string name, SensorIndices indices, SensorType type, CorsairPsu hardware, ISettings settings, UsbApi.Command cmd, Rail rail = Rail._12V, bool noHistory = false)
            : base(name, indices.NextIndex(type), false, type, hardware, null, settings, noHistory)
        {
            _cmd = cmd;
            _rail = (byte)rail;

            hardware.ActivateSensor(this);
        }

        public void Update(HidStream stream)
        {
            Value = UsbApi.GetValue(stream, _cmd, _rail);
        }
    }

    private class PsuSensorWithLimits : PsuSensor, ICriticalSensorLimits
    {
        public PsuSensorWithLimits
        (
            string name,
            SensorIndices indices,
            SensorType type,
            CorsairPsu hardware,
            ISettings settings,
            UsbApi.Command cmd,
            float? lowCritical,
            float? highCritical,
            Rail rail = Rail._12V)
            : base(name, indices, type, hardware, settings, cmd, rail)
        {
            CriticalLowLimit = lowCritical;
            CriticalHighLimit = highCritical;
        }

        public float? CriticalHighLimit { get; }

        public float? CriticalLowLimit { get; }
    }

    private enum Rail : byte
    {
        _12V = 0,
        _5V = 1,
        _3V = 2
    }
}

#region Exception classes

public class CommunicationProtocolError : ApplicationException
{
    public CommunicationProtocolError(HidDevice device, string message)
        : base($"Error communicating with the PSU controller at {device.DevicePath}: {message}")
    { }
}

#endregion

#region PSU USB communication protocol implementation

internal static class UsbApi
{
    /* some values are SMBus LINEAR11 data which need a conversion */
#if false
        static int Linear11ToInt(ushort val, int scale)
        {
            int exp = ((short)val) >> 11;
            int mant = (((short)(val & 0x7ff)) << 5) >> 5;
            int result = mant * scale;

            return (exp >= 0) ? (result << exp) : (result >> -exp);
        }
#endif
    static float Linear11ToFloat32(ushort val)
    {
        int exp = (short)val >> 11;
        int mant = ((short)(val & 0x7ff) << 5) >> 5;
        return mant * (float)Math.Pow(2, exp);
    }

    static bool SendCommand(HidStream stream, byte length, Command cmd, byte arg, out byte[] replyData)
    {
        /*
         * Corsair protocol for PSUs
         *
         * message size = 64 bytes (request and response, little endian)
         * request:
         *	[length][command][param0][param1][paramX]...
         * reply:
         *	[echo of length][echo of command][data0][data1][dataX]...
         *
         *	- commands are byte sized opcodes
         *	- length is the sum of all bytes of the commands/params
         *	- the micro-controller of most of these PSUs support concatenation in the request and reply,
         *	  but it is better to not rely on this (it is also hard to parse)
         *	- the driver uses raw events to be accessible from userspace (though this is not really
         *	  supported, it is just there for convenience, may be removed in the future)
         *	- a reply always start with the length and command in the same order the request used it
         *	- length of the reply data is specific to the command used
         *	- some of the commands work on a rail and can be switched to a specific rail (0 = 12v,
         *	  1 = 5v, 2 = 3.3v)
         *	- the format of the init command 0xFE is swapped length/command bytes
         *	- parameter bytes amount and values are specific to the command (rail setting is the only
         *	  for now that uses non-zero values)
         *	- there are much more commands, especially for configuring the device, but they are not
         *	  supported because a wrong command/length can lockup the micro-controller
         *	- the driver supports debugfs for values not fitting into the hwmon class
         *	- not every device class (HXi, RMi or AXi) supports all commands
         *	- it is a pure sensors reading driver (will not support configuring)
        */

        const int cmdBufferSize = 64;
        const int replySize = 16;

        byte[] cmdBuffer = new byte[cmdBufferSize + 1];
        cmdBuffer[0] = 0; // report id
        cmdBuffer[1] = length;
        cmdBuffer[2] = (byte)cmd;
        cmdBuffer[3] = arg;

        stream.Write(cmdBuffer);
        byte[] reply = stream.Read();
        replyData = new byte[replySize];
        Array.Copy(reply, 3, replyData, 0, replySize);

        return reply[1] == cmdBuffer[1] && reply[2] == cmdBuffer[2];
    }

    public static void Init(HidStream stream)
    {
        /*
         * PSU_CMD_INIT uses swapped length/command and expects 2 parameter bytes, this command
         * actually generates a reply, but we don't need it
         */
        SendCommand(stream, (byte)Command.INIT, (Command)3, 0, out _);
    }

    public struct FirmwareInfo
    {
        public string Vendor;
        public string Product;
    }

    public static FirmwareInfo FwInfo(HidStream stream)
    {
        if (!SendCommand(stream, 3, Command.VEND_STR, 0, out byte[] vendorArr))
            throw new CommunicationProtocolError(stream.Device, "Can't read vendor string");

        if (!SendCommand(stream, 3, Command.PROD_STR, 0, out byte[] productArr))
            throw new CommunicationProtocolError(stream.Device, "Can't read product");

        string ArrayToString(byte[] ar)
        {
            return Encoding.ASCII.GetString(ar.TakeWhile(x => x != 0).ToArray());
        }

        return new FirmwareInfo { Vendor = ArrayToString(vendorArr), Product = ArrayToString(productArr) };
    }

    static bool Request(HidStream stream, Command cmd, byte rail, out byte[] data)
    {
        //mutex_lock(&priv->lock) ;
        switch (cmd)
        {
            case Command.RAIL_VOLTS_HCRIT:
            case Command.RAIL_VOLTS_LCRIT:
            case Command.RAIL_AMPS_HCRIT:
            case Command.RAIL_VOLTS:
            case Command.RAIL_AMPS:
            case Command.RAIL_WATTS:
                if (!SendCommand(stream, 2, Command.SELECT_RAIL, rail, out _))
                {
                    data = null;
                    return false;
                }

                break;
        }

        return SendCommand(stream, 3, cmd, 0, out data);

        //  mutex_unlock(&priv->lock) ;
        //  return ret;
    }

    public static float? GetValue(HidStream stream, Command cmd, byte rail)
    {
        if (!Request(stream, cmd, rail, out byte[] data))
        {
            return null;
        }

        /*
         * the biggest value here comes from the uptime command and to exceed MAXINT total uptime
         * needs to be about 68 years, the rest are u16 values and the biggest value coming out of
         * the LINEAR11 conversion are the watts values which are about 1200 for the strongest psu
         * supported (HX1200i)
         */
        int tmp = BitConverter.ToInt32(data, 0); // ((int)data[3] << 24) + (data[2] << 16) + (data[1] << 8) + data[0];
        return cmd switch
        {
            Command.RAIL_VOLTS_HCRIT or Command.RAIL_VOLTS_LCRIT or Command.RAIL_AMPS_HCRIT or Command.TEMP_HCRIT or Command.IN_VOLTS or Command.IN_AMPS or Command.RAIL_VOLTS or Command.RAIL_AMPS
                or Command.TEMP0 or Command.TEMP1 or Command.FAN_RPM or Command.RAIL_WATTS or Command.TOTAL_WATTS => Linear11ToFloat32((ushort)tmp), // Linear11ToInt((ushort)tmp, 1000000);
            Command.TOTAL_UPTIME or Command.UPTIME or Command.OCPMODE => tmp,
            _ => null
        };
    }

    public struct Criticals
    {
        public float?[] TempMax;

        public float?[] VoltageMin;
        public float?[] VoltageMax;

        public float?[] CurrentMax;
    }

    public static Criticals GetCriticals(HidStream stream)
    {
        Criticals res = new();
        const byte tempCount = 2;
        res.TempMax = new float?[tempCount];

        for (byte rail = 0; rail < tempCount; rail++)
        {
            res.TempMax[rail] = GetValue(stream, Command.TEMP_HCRIT, rail);
        }

        const byte railCount = 3; /* 3v + 5v + 12v */
        res.VoltageMin = new float?[railCount];
        res.VoltageMax = new float?[railCount];
        res.CurrentMax = new float?[railCount];

        for (byte rail = 0; rail < railCount; rail++)
        {
            res.VoltageMax[rail] = GetValue(stream, Command.RAIL_VOLTS_HCRIT, rail);
            res.VoltageMin[rail] = GetValue(stream, Command.RAIL_VOLTS_LCRIT, rail);
            res.CurrentMax[rail] = GetValue(stream, Command.RAIL_AMPS_HCRIT, rail);
        }

        return res;
    }

    [Flags]
    public enum OptionalCommands
    {
        None = 0x0,
        InputCurrent = 0x1
    }

    public static OptionalCommands GetOptionalCommands(HidStream stream)
    {
        OptionalCommands res = OptionalCommands.None;
        if (GetValue(stream, Command.IN_AMPS, 0).HasValue)
        {
            res |= OptionalCommands.InputCurrent;
        }

        return res;
    }

    public enum Command : byte
    {
        SELECT_RAIL = 0x00, /* expects length 2 */
        RAIL_VOLTS_HCRIT = 0x40, /* the rest of the commands expect length 3 */
        RAIL_VOLTS_LCRIT = 0x44,
        RAIL_AMPS_HCRIT = 0x46,
        TEMP_HCRIT = 0x4F,
        IN_VOLTS = 0x88,
        IN_AMPS = 0x89,
        RAIL_VOLTS = 0x8B,
        RAIL_AMPS = 0x8C,
        TEMP0 = 0x8D,
        TEMP1 = 0x8E,
        FAN_RPM = 0x90,
        RAIL_WATTS = 0x96,
        VEND_STR = 0x99,
        PROD_STR = 0x9A,
        TOTAL_UPTIME = 0xD1,
        UPTIME = 0xD2,
        OCPMODE = 0xD8,
        TOTAL_WATTS = 0xEE,
        INIT = 0xFE
    }
}

#endregion
