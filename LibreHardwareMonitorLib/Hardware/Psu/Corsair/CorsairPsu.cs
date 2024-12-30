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
    private readonly List<CompositeSensor> _compositeSensors = [];
    private readonly HidDevice _device;
    private readonly List<PsuSensor> _sensors = [];
    private InputPowerSensor _inputPowerSensor;
    private EfficiencySensor _efficiencySensor;

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
        _inputPowerSensor.Update();
        _efficiencySensor.Update();
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

        var inputVoltage = new PsuSensor("Input", indices, SensorType.Voltage, this, settings, UsbApi.Command.IN_VOLTS);
        _sensors.Add(inputVoltage);
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
        var totalWatts = new PsuSensor("Total watts", indices, SensorType.Power, this, settings, UsbApi.Command.TOTAL_WATTS);
        _sensors.Add(totalWatts);
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

        _inputPowerSensor = new InputPowerSensor("Input watts",
            indices.NextIndex(SensorType.Power),
            this,
            settings,
            totalWatts,
            inputVoltage,
            _device.ProductID
            );

        _efficiencySensor = new EfficiencySensor("Efficiency",
            indices.NextIndex(SensorType.Power),
            this,
            settings,
            totalWatts,
            _inputPowerSensor
            );
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

    private class InputPowerSensor : Sensor
    {
        private readonly ISensor _power;
        private readonly ISensor _voltage;
        private readonly int _productId = 0;

        private EfficiencyCurve _efficiency;
        private float _oldVoltage = 0;

        public InputPowerSensor
        (
            string name,
            int index,
            CorsairPsu hardware,
            ISettings settings,
            ISensor power,
            ISensor voltage,
            int productId
            )
            : base(name, index, SensorType.Power, hardware, settings)
        {
            _power = power;
            _voltage = voltage;
            _productId = productId;
            hardware.ActivateSensor(this);
        }

        public void Update()
        {
            if (_voltage.Value is { } voltage && voltage != _oldVoltage)
            {
                _efficiency = voltage < 200 ? EfficiencyCurve.GetInputCurve115V(_productId) : EfficiencyCurve.GetInputCurve230V(_productId);
                _oldVoltage = voltage;
            }
            if (_power.Value is { } power) Value = _efficiency?.GetInputPower(power);
        }

    }

    private class EfficiencySensor : Sensor
    {
        private readonly ISensor _outputPower;
        private readonly ISensor _inputPower;

        public EfficiencySensor
        (
            string name,
            int index,
            CorsairPsu hardware,
            ISettings settings,
            ISensor outputPower,
            ISensor inputPower
        )
            : base(name, index, SensorType.Factor, hardware, settings)
        {
            _outputPower = outputPower;
            _inputPower = inputPower;
            hardware.ActivateSensor(this);
        }
        public void Update()
        {
            if (_outputPower.Value is { } o && _inputPower.Value is { } i) Value = o / i;
            else Value = null;
        }
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
        FAN_PWM = 0x3B, /* the rest of the commands expect length 3 */
        RAIL_VOLTS_HCRIT = 0x40,
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
        FAN_PWM_ENABLE = 0xF0,
        INIT = 0xFE
    }
}

#endregion

internal abstract class EfficiencyCurve
{
    public abstract float GetInputPower(float outputPower);

    public static EfficiencyCurve GetInputCurve115V(int id)
    {
        return id switch
        {
            // HX550i (real values unknown, just copied from HX750i)
            0x1c03 => new QuadraticEfficiency(0.00013153276902318052f, 1.0118732314945875f, 9.783796618886313f),
            // HX650i (real values unknown, just copied from HX750i)
            0x1c04 => new QuadraticEfficiency(0.00013153276902318052f, 1.0118732314945875f, 9.783796618886313f),
            
            // HX750i CP-9020072
            0x1c05 => // new QuadraticEfficiency(0.00013153276902318052f, 1.0118732314945875f, 9.783796618886313f), // from liquidctl https://github.com/liquidctl/liquidctl/blob/main/liquidctl/driver/corsair_hid_psu.py
            new ExtrapolatedEfficiency(
                // Data from https://www.cybenetics.com/evaluations/psus/101/
                [19.634f, 39.737f, 59.860f, 74.806f, 79.826f, 149.769f, 224.850f, 299.735f, 374.710f, 449.578f, 524.607f, 599.537f, 674.564f, 749.405f, 824.225f],
                [28.695f, 49.827f, 71.253f, 86.632f, 91.626f, 165.686f, 245.014f, 325.199f, 407.015f, 489.925f, 574.310f, 659.943f, 746.898f, 834.895f, 925.037f]
            ),

            // HX850i CP-9020073
            0x1c06 => //new QuadraticEfficiency(0.00011552923724840388f, 1.0111311876704099f, 12.015296651918918f),
            new ExtrapolatedEfficiency(
                // Data from https://www.cybenetics.com/evaluations/psus/100/
                [19.679f, 39.824f, 59.870f, 79.838f, 84.787f, 169.625f, 254.889f, 339.718f, 424.650f, 509.548f, 594.574f, 679.439f, 764.426f, 849.225f, 934.022f],
                [28.814f, 49.896f, 71.078f, 91.400f, 96.768f, 186.355f, 276.753f, 368.191f, 461.198f, 556.315f, 652.988f, 751.567f, 851.445f, 954.393f, 1060.079f]
            ),

            // HX1000i CP-9020074
            0x1c07 => //new QuadraticEfficiency(9.48609754417109e-05f, 1.0170509865269720f, 11.619826520447452f),
            new ExtrapolatedEfficiency(
                // Data from https://www.cybenetics.com/evaluations/psus/99/
                [19.665f, 39.774f, 59.892f, 79.794f, 99.797f, 199.687f, 299.877f, 399.625f, 499.583f, 599.525f, 699.445f, 799.266f, 899.275f, 998.926f, 1098.750f, 151.586f, 1007.857f],
                [29.882f, 50.309f, 71.673f, 91.883f, 113.368f, 218.451f, 324.952f, 432.914f, 543.473f, 655.991f, 770.696f, 887.245f, 1007.663f, 1134.313f, 1263.513f, 181.553f, 1139.730f]
            ),

            // HX1200i CP-9020070
            0x1c08 => // Data from liquidctl : new QuadraticEfficiency(6.244705156199815e-05f, 1.0234738310580973f, 15.293509559389241f),
            new ExtrapolatedEfficiency(
                // Data from https://www.cybenetics.com/evaluations/psus/98/
                [19.675f,39.789f,59.896f,79.765f,119.813f,239.658f,359.822f,479.510f,599.552f,719.369f,839.298f,959.177f,1079.164f,1199.026f,1318.895f],
                [30.299f,51.018f,71.569f,92.517f,134.036f,259.393f,387.671f,517.009f,650.329f,785.183f,925.472f,1066.745f,1211.110f,1360.632f,1510.588f]
            ),

            // HX1200i CP-9020281
            0x1c23 => // Data from liquidctl : new QuadraticEfficiency(6.244705156199815e-05f, 1.0234738310580973f, 15.293509559389241f),
            new ExtrapolatedEfficiency(
                // Data from https://www.cybenetics.com/evaluations/psus/2510/
                [19.994f, 39.994f, 59.995f, 79.932f, 119.983f, 239.934f, 359.088f, 479.471f, 599.248f, 719.792f, 839.505f, 959.493f, 1079.35f, 1199.372f, 1319.985f],
                [27.647f, 50.518f, 73.141f, 94.257f, 135.123f, 261.431f, 388.348f, 519.029f, 650.7f, 791.467f, 930.678f, 1071.804f, 1215.15f, 1360.671f, 1511.922f]
            ),

            // RM650i CP-9020081
            0x1c0a => //new QuadraticEfficiency(0.00017323493381072683f, 1.0047044721686030f, 12.376592422281606f),
            new ExtrapolatedEfficiency(
                // Data from https://www.cybenetics.com/evaluations/psus/92/
                [19.710f, 39.781f, 59.950f, 64.802f, 79.789f, 129.814f, 194.942f, 259.831f, 324.784f, 389.783f, 454.746f, 519.703f, 584.742f, 649.657f, 714.585f, 134.336f, 664.504f],
                [29.212f, 50.444f, 71.925f, 77.069f, 93.253f, 146.695f, 216.489f, 286.807f, 358.772f, 432.445f, 507.109f, 583.509f, 661.197f, 741.433f, 824.573f, 163.157f, 755.633f]
            ),

            // RM750i CP-9020082
            0x1c0b => // new QuadraticEfficiency(0.00015013694263596336f, 1.0047044721686027f, 14.280683564171110f),
            new ExtrapolatedEfficiency(
                // Data from https://www.cybenetics.com/evaluations/psus/91/
                [19.658f,39.837f,59.893f,74.811f,79.806f,149.775f,224.909f,299.797f,374.762f,449.697f,524.717f,599.658f,674.686f,749.551f,824.440f],
                [29.300f,50.064f,72.049f,88.056f,93.390f,167.807f,248.589f,330.356f,413.992f,499.599f,586.511f,675.369f,766.104f,859.618f,955.479f]
            ),

            // RM850i CP-9020083
            0x1c0c => // new QuadraticEfficiency(0.00012280002467981107f, 1.0159421430340847f, 13.555472968718759f),
            new ExtrapolatedEfficiency(
                // Data from https://www.cybenetics.com/evaluations/psus/90/
                [19.671f,39.786f,59.902f,79.818f,84.786f,169.696f,254.927f,339.792f,424.695f,509.695f,594.634f,679.617f,764.614f,849.470f,934.407f],
                [28.960f,50.274f,71.870f,93.286f,98.558f,188.662f,280.300f,373.191f,467.998f,565.242f,664.124f,766.194f,869.876f,977.755f,1088.648f]
            ),

            // RM1000i CP-9020084
            0x1c0d =>// new QuadraticEfficiency(0.00010018433053123574f, 1.0272313660072225f, 14.092187353321624f),
            new ExtrapolatedEfficiency(
                // Data from https://www.cybenetics.com/evaluations/psus/89/
                [19.651f,39.806f,59.878f,79.812f,99.827f,199.653f,299.892f,399.723f,499.693f,599.624f,699.598f,799.451f,899.509f,999.270f,1099.263f,151.627f,1005.614f],
                [30.033f,50.287f,72.963f,94.206f,115.166f,220.515f,327.910f,436.679f,548.063f,662.584f,780.288f,899.737f,1021.441f,1145.852f,1275.426f,183.610f,1148.872f]
            ),

            // HX1000i (2022) CP-9020214
            0x1c1e => // new QuadraticEfficiency(1000,[0.00012038623467957958, 0.9899868099948035, 13.125601514017152]),
            new ExtrapolatedEfficiency(
                //Data from official Corsair documentation [20, 100, 200, 500, 1000], [33, 113, 216, 538, 1123]),
                //Data from https://www.cybenetics.com/evaluations/psus/1911/
                [19.994f, 39.993f, 59.993f, 79.953f, 100.006f, 199.953f, 300.001f, 399.607f, 499.29f, 599.743f, 699.461f, 799.469f, 899.245f, 999.233f, 1099.85f],
                [27.065f, 49.607f, 71.897f, 91.633f, 112.481f, 217.795f, 324.305f, 431.679f, 541.473f, 654.482f, 770.221f, 886.644f, 1007.44f, 1128.247f, 1254.387f]

                // HX1000i CP-9020259
                // Data from https://www.cybenetics.com/evaluations/psus/2513/
                // [19.992f,39.994f,59.994f,79.927f,99.978f,199.925	f,299.962f,399.458f,499.199f,599.735f,699.471f,799.496f,899.321f,999.292f,1099.914f],
                // [26.602f,51.063f,80.02f ,91.075f,112.21f,216.557f,322.097f,430.25f,539.771f,652.013f,769.092f,889.992f,1013.35f,1132.6f,1259.7f]
            ),

            // HX1500i # CP-9020215
            0x1c1f => //new QuadraticEfficiency(1300,[6.605968230747892e-05, 1.0125991461405333, 17.96728350708451]),
            new ExtrapolatedEfficiency(
                // Data from Corsair doc : [26, 130, 260, 650, 1300], [44, 151, 286, 704, 1446]
                // Data from https://www.cybenetics.com/evaluations/psus/1913/
                [19.996f,39.995f,59.995f,79.96f,150.012f,299.973f,449.628f,599.572f,749.645f,899.77f,1049.889f,1199.526f,1349.73f,1499.44f,1649.663f],
                [29.395f,52.675f,75.208f,98.562f,166.588f,324.39f,485.71f,650.528f,818.733f,994.699f,1172.91f,1353.659f,1540.197f,1732.76f,1938.496f]

                // Data from https://www.cybenetics.com/evaluations/psus/2099/
                //[19.987f, 39.986f, 59.986f, 79.935f, 149.98f, 299.928f, 449.452f, 599.421f, 749.556f, 899.664f, 1049.761f, 1199.38f, 1349.561f, 1499.308f, 1649.467f],
                //[27.957f, 50.957f, 73.413f, 96.313f, 167.629f, 324.297f, 484.13f, 646.536f, 812.991f, 989.899f, 1165.726f, 1342.752f, 1524.381f, 1709.8f, 1905.663f]
            ),
            _ => new QuadraticEfficiency(0, 1 / 0.9f, 0)
        };
    }

    internal static EfficiencyCurve GetInputCurve230V(int id)
    {
        return id switch
        {
            // HX550i
            0x1c03 => new QuadraticEfficiency(9.268856467314546e-05f, 1.0183515407387007f, 8.279822175342481f),
            // HX650i
            0x1c04 => new QuadraticEfficiency(9.268856467314546e-05f, 1.0183515407387007f, 8.279822175342481f),

            // HX750i CP-9020072
            0x1c05 => // new QuadraticEfficiency(9.268856467314546e-05f, 1.0183515407387007f, 8.279822175342481f),
            new ExtrapolatedEfficiency(
            // Data for HX750, (HX750i has not been tested in 230v)
            // Data from https://www.cybenetics.com/evaluations/psus/1893/
            [20.017f,40.018f,60.02f,75.025f,79.996f,150.005f,225.028f,300.132f,374.851f,449.722f,524.686f,599.897f,674.919f,750.148f,825.158f],
            [25.865f,47.834f,68.467f,84.171f,89.123f,162.982f,242.011f,321.834f,402.456f,488.391f,571.593f,656.184f,739.675f,826.252f,914.701f]
            ),
            // HX850i CP-9020073
            0x1c06 => // new QuadraticEfficiency(8.126644224872423e-05f, 1.0176256272095185f, 10.290640442373850f),
            new ExtrapolatedEfficiency(
            // Data for HX850, (HX850i has not been tested in 230v)
            // Data from https://www.cybenetics.com/evaluations/psus/1892/
            [19.994f,39.994f,59.995f,79.943f,84.999f,169.938f,254.937f,340.02f,424.89f,509.361f,594.705f,679.533f,764.955f,849.697f,934.315f],
            [26.642f,49.285f,69.545f,90.149f,95.445f,184.378f,273.999f,364.558f,456.395f,553.486f,648.663f,742.933f,840.144f,937.425f,1036.94f]
            ),

            // HX1000i CP-9020074
            0x1c07 => // new QuadraticEfficiency(9.649987544008507e-05f, 1.0018241767296636f, 12.759957859756842f),
            new ExtrapolatedEfficiency(
            // HX1000 (HX1000i has not been tested in 230v)
            // Data from https://www.cybenetics.com/evaluations/psus/1911/
            [19.649f,39.787f,59.887f,79.833f,99.827f,199.706f,299.918f,399.712f,499.676f,599.627f,699.588f,799.479f,899.475f,999.259f,1099.130f],
            [29.031f,49.839f,70.614f,91.666f,112.455f,215.976f,321.406f,427.450f,535.085f,644.292f,754.811f,867.326f,981.344f,1097.675f,1216.394f]
            ),

            // HX1200i CP-9020070
            0x1c08 => //new QuadraticEfficiency(5.9413179794350966e-05f, 1.0023670927127724f, 15.886126793547152f),
            // Data from https://www.cybenetics.com/evaluations/psus/2510/
            new ExtrapolatedEfficiency(
            [20.004f, 40f, 59.999f, 79.939f, 119.988f, 239.945f, 359.122f, 479.49f, 599.268f, 719.796f, 839.524f, 959.519f, 1079.332f, 1199.367f, 1319.991f],
            [27.544f, 49.87f, 72.175f, 92.835f, 133.491f, 258.161f, 382.303f, 512.676f, 644.047f, 774.115f, 905.595f, 1039.304f, 1173.994f, 1313.841f, 1454.036f]
            ),

            // HX1200i CP-9020281
            0x1c23 => //new QuadraticEfficiency(5.9413179794350966e-05f, 1.0023670927127724f, 15.886126793547152f),
            // Data from https://www.cybenetics.com/evaluations/psus/2510/
            new ExtrapolatedEfficiency(
            [20.004f, 40f, 59.999f, 79.939f, 119.988f, 239.945f, 359.122f, 479.49f, 599.268f, 719.796f, 839.524f, 959.519f, 1079.332f, 1199.367f, 1319.991f],
            [27.544f, 49.87f, 72.175f, 92.835f, 133.491f, 258.161f, 382.303f, 512.676f, 644.047f, 774.115f, 905.595f, 1039.304f, 1173.994f, 1313.841f, 1454.036f]
            ),

            // RM650i
            0x1c0a => //new QuadraticEfficiency(0.00012413136310310370f, 1.0284317478987164f, 9.465259079360674f),
            new ExtrapolatedEfficiency(
            // Data for RM650x, (RM650i has not been tested in 230v)
            // Data from https://www.cybenetics.com/evaluations/psus/1734/
            [19.984f, 39.973f, 60.003f, 64.953f, 79.954f, 130.004f, 194.998f, 259.992f, 325.023f, 389.286f, 454.608f, 519.928f, 584.820f, 649.651f, 714.432f, 134.543f, 661.094f],
            [25.045f, 46.895f, 68.590f, 74.061f, 89.940f, 143.715f, 210.931f, 280.093f, 350.738f, 421.920f, 495.165f, 569.655f, 644.175f, 720.400f, 795.947f, 159.308f, 727.697f]
            ),

            // RM750i CP-9020082
            0x1c0b => // new QuadraticEfficiency(0.00010460621468919797f, 1.0173089573727216f, 11.495900706372142f),
            new ExtrapolatedEfficiency(
            // Data for RM750x, (RM750i has not been tested in 230v)
            // Data from https://www.cybenetics.com/evaluations/psus/1801/
            [19.989f,39.979f,60.008f,74.959f,79.958f,150.031f,225.037f,300.045f,374.594f,449.515f,524.858f,600.165f,674.714f,749.938f,825.169f,151.044f,760.207f],
            [25.023f,46.652f,68.288f,85.941f,91.165f,164.916f,242.732f,323.105f,404.691f,487.677f,572.419f,658.345f,744.361f,832.890f,922.732f,180.598f,839.955f]
            ),

            // RM850i CP-9020083
            0x1c0c => //new QuadraticEfficiency(8.816054254801031e-05f, 1.0234738318592156f, 10.832902491655597f),
            new ExtrapolatedEfficiency(
            // Data for RM850x, (RM850i has not been tested in 230v)
            // Data from https://www.cybenetics.com/evaluations/psus/1846/
            [19.995f,39.986f,60.014f,79.963f,84.963f,170.042f,255.046f,340.066f,425.085f,509.409f,594.932f,679.855f,765.080f,849.927f,934.794f],
            [25.107f,47.223f,68.726f,90.942f,96.218f,184.910f,273.826f,365.500f,458.755f,552.377f,649.909f,747.131f,846.720f,948.883f,1053.191f]
            ),

            // RM1000i CP-9020084
            0x1c0d => // new QuadraticEfficiency(8.600634771656125e-05f, 1.0289245073649413f, 13.701515390258626f),
            new ExtrapolatedEfficiency(
                // RM1000i CP-9020084
                // Data from https://www.cybenetics.com/evaluations/psus/89/
            [19.963f,39.951f,59.982f,79.816f,99.979f,200.006f,299.981f,399.651f,499.805f,599.932f,699.654f,800.176f,899.468f,999.877f,1099.870f],
            [30.172f,50.946f,71.224f,92.071f,113.055f,217.207f,322.529f,428.553f,536.834f,646.181f,756.653f,869.793f,983.003f,1100.407f,1219.007f]
            ),

            // HX1000i (2022) CP-9020214
            0x1c1e => // new QuadraticEfficiency(1000,[8.725695209710315e-05, 1.0017598021499974,  9.789546063300154]),
            new ExtrapolatedEfficiency(
                //Data from official Corsair documentation [20, 100, 200, 500, 1000], [30, 111, 214, 532, 1099]
                //Data from https://www.cybenetics.com/evaluations/psus/1911
                [19.985f, 39.987f, 59.987f, 79.923f, 99.973f, 199.905f, 299.935f, 399.377f, 499.16f, 599.713f, 699.465f, 799.499f, 899.283f, 999.302f, 1099.927f],
                [27.073f, 49.616f, 72.163f, 89.626f, 110.56f, 215.236f, 321.004f, 426.188f, 533.277f, 641.522f, 755.565f, 867.709f, 978.727f, 1092.937f, 1213.938f]

                // HX1000i CP-9020259
                // Data from https://www.cybenetics.com/evaluations/psus/2513/
                // [19.991f,39.994f,59.994f,79.927f,99.978f,199.925f,299.964f,399.45f,499.196f,599.743f,699.469f,799.498f,899.305f,999.314f,1099.931f]
                // [27.318f,49.639f,72.258f,89.602f,110.074f,214.335f,319.113f,424.75f,531.35f,647.943f,754.663f,866.357f,978.909f,1097.959f,1215.77f]
            ),

            // HX1500i # CP-9020215
            0x1c1f => // new QuadraticEfficiency(1500, [4.634428233657273e-05, 1.0183515407387007, 16.559644350684962]),
            new ExtrapolatedEfficiency(//[30, 150, 300, 750, 1500], [41, 172, 329, 814, 1659]),
                // Data from https://www.cybenetics.com/evaluations/psus/1913/
                [19.986f,39.988f,59.988f,79.927f,149.973f,299.918f,449.503f,599.481f,749.616f,899.729f,1049.853f,1199.491f,1349.717f,1499.435f,1649.662f],
                [29.06f,51.421f,73.93f,96.191f,164.557f,319.792f,476.879f,638.739f,803.408f,968.337f,1135.787f,1305.062f,1477.829f,1653.66f,1833.565f]

                // Data from https://www.cybenetics.com/evaluations/psus/2099/
                //[19.989f,39.987f,59.987f,79.944f,149.995f,299.95f,449.443f,599.379f,749.493f,899.57f,1049.679f,1199.303f,1349.49f,1499.206f,1649.422f],
                //[27.867f,50.563f,73.129f,95.299f,165.553f,320.595f,476.703f,639.463f,804.682f,968.279f,1134.241f,1302.338f,1472.616f,1649.709f,1826.445f],

            ),
        };
    }

    class QuadraticEfficiency(float a, float b, float c) : EfficiencyCurve
    {
        public override float GetInputPower(float outputPower) => outputPower * outputPower * a + outputPower * b + c;
    }

    class ExtrapolatedEfficiency(float[] powerOut, float[] powerIn) : EfficiencyCurve
    {
        public override float GetInputPower(float power)
        {
            float pOut0 = 0;
            float pIn0 = 0;
            for (int i = 0; i < powerOut.Length; i++)
            {
                float pOut1 = powerOut[i];
                float pIn1 = powerIn[i];
                if (power < pOut1)
                    return (pIn1 - pIn0) * (power - pOut0) / (pOut1 - pOut0) + pIn0;
                pOut0 = pOut1;
                pIn0 = pIn1;
            }
            return powerIn.Last() * power / powerOut.Last();
        }
    }
}

