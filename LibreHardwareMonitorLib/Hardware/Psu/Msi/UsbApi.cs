// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) 2024 demorfi<demorfi@gmail.com>
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Linq;
using System.Text;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Psu.Msi;

internal static class UsbApi
{
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

    public static FirmwareInfo FwInfo(HidStream stream)
    {
        if (!Request(stream, new byte[2] { 0xFA, 0x51 }, out byte[] productArr, 1))
            throw new ProtocolError(stream.Device, "Can't read product name");

        static string ArrayToString(byte[] ar)
        {
            return Encoding.ASCII.GetString(ar.TakeWhile(x => x != 0).ToArray());
        }

        return new FirmwareInfo { Vendor = "MSI", Product = ArrayToString(productArr) };
    }

    private static float Linear11ToFloat32(ushort val)
    {
        int exp = (short)val >> 11;
        int mant = ((short)(val & 0x7ff) << 5) >> 5;
        return mant * (float)Math.Pow(2, exp);
    }

    private static bool Request(HidStream stream, byte[] command, out byte[] response, int offset = 0)
    {
        byte[] buffer = new byte[64];
        Array.Copy(command, 0, buffer, 1, 2);
        stream.Write(buffer);

        byte[] reply = stream.Read();
        response = new byte[42];
        Array.Copy(reply, 2 + offset, response, 0, 42);

        return reply[0] == buffer[0] && reply[1] == buffer[1];
    }

    public static float[] InfoList(HidStream stream)
    {
        int length = Enum.GetNames(typeof(IndexInfo)).Length;
        float[] info = new float[length];

        if (!Request(stream, new byte[2] { 0x51, 0xE0 }, out byte[] basic))
            throw new ProtocolError(stream.Device, "Can't read basic info");

        // basic has information only about the first 20 sensors
        for (int i = 0; i < 20; i++)
        {
            byte[] replyData = new byte[4];
            Array.Copy(basic, (i * 2) + 1, replyData, 0, 2);
            info[i] = Linear11ToFloat32((ushort)BitConverter.ToInt32(replyData, 0));
        }

        // runtime info
        Request(stream, new byte[2] { 0x51, 0xD1 }, out byte[] runtime);
        info[(int)IndexInfo.RUNTIME] = BitConverter.ToInt32(runtime, 0) / 100;

        return info;
    }

    public struct FirmwareInfo
    {
        public string Vendor;
        public string Product;
    }
}
