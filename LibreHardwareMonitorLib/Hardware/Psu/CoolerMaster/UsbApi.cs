// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.IO;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Psu.CoolerMaster;

/// <summary>
/// HID protocol of the Cooler Master XG Plus series (vendor interface MI_00, 64 byte reports).
/// Every message is <c>80 &lt;command&gt; &lt;kind&gt; ...</c>, kind: 01 = read request, 02 = write, 03 = response.
/// Only read requests are ever sent.
/// </summary>
internal static class UsbApi
{
    private const byte Header = 0x80;
    private const byte KindRead = 0x01;
    private const byte KindResponse = 0x03;

    // A response can be preceded by replies to requests of other programs (e.g. MasterPlus).
    private const int MaxReads = 4;

    public enum Command : byte
    {
        FIRMWARE = 0x01,
        TELEMETRY = 0x06
    }

    public enum IndexInfo : int
    {
        FAN_RPM = 0,
        POWER = 1,
        TEMP = 2
    }

    public static bool IsVendorInterface(HidDevice device)
    {
        // MI_00 is the only interface with both 64 byte input and output reports
        // (MI_01: input only, MI_02: output only, MI_03: keyboard).
        try
        {
            return device.GetMaxInputReportLength() >= 64 && device.GetMaxOutputReportLength() >= 64;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static byte FirmwareVersion(HidStream stream)
    {
        if (!Request(stream, Command.FIRMWARE, out byte[] response))
            throw new ProtocolError(stream.Device, "Can't read firmware version");

        return response[0];
    }

    /// <summary>
    /// Reads fan speed (RPM), power (W) and temperature (°C), indexed by <see cref="IndexInfo" />.
    /// Returns <c>null</c> if the PSU didn't answer or the values are not plausible.
    /// </summary>
    public static float[] InfoList(HidStream stream)
    {
        if (!Request(stream, Command.TELEMETRY, out byte[] response))
            return null;

        float[] info = new float[Enum.GetNames(typeof(IndexInfo)).Length];
        info[(int)IndexInfo.FAN_RPM] = BitConverter.ToUInt16(response, 0);
        info[(int)IndexInfo.POWER] = BitConverter.ToUInt16(response, 2);
        info[(int)IndexInfo.TEMP] = BitConverter.ToUInt16(response, 4);

        // Discard values that can't come from a PSU of this series.
        if (info[(int)IndexInfo.FAN_RPM] >= 5000 || info[(int)IndexInfo.POWER] >= 2000 || info[(int)IndexInfo.TEMP] >= 150)
            return null;

        return info;
    }

    private static bool Request(HidStream stream, Command command, out byte[] response)
    {
        response = null;

        // Report ID 0 followed by the request, zero padded.
        byte[] buffer = new byte[stream.Device.GetMaxOutputReportLength()];
        buffer[1] = Header;
        buffer[2] = (byte)command;
        buffer[3] = KindRead;

        try
        {
            stream.Write(buffer);

            for (int i = 0; i < MaxReads; i++)
            {
                byte[] reply = stream.Read();
                if (reply.Length >= 10 && reply[1] == Header && reply[2] == (byte)command && reply[3] == KindResponse)
                {
                    response = new byte[reply.Length - 4];
                    Array.Copy(reply, 4, response, 0, response.Length);
                    return true;
                }
            }
        }
        catch (TimeoutException)
        { }
        catch (IOException)
        { }

        return false;
    }
}
