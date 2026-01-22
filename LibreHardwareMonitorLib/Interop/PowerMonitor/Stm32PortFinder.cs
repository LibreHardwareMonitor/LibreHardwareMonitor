// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static LibreHardwareMonitor.Interop.SetupAPI;

namespace LibreHardwareMonitor.Interop.PowerMonitor;

/// <summary>
/// Helper class to find STM32 COM ports based on VID and PID.
/// </summary>
public static class Stm32PortFinder
{
    static Guid GUID_DEVCLASS_PORTS = new Guid("4D36E978-E325-11CE-BFC1-08002BE10318");

    static readonly IntPtr InvalidHandle = new IntPtr(-1);

    const int BufferSize = 1024;

    /// <summary>
    /// Finds the names of all available COM ports that match the specified USB vendor ID (VID) and product ID (PID).
    /// </summary>
    /// <param name="vid">The USB vendor ID to match.</param>
    /// <param name="pid">The USB product ID to match.</param>
    /// <returns>A list of strings containing the names of matching COM ports.<br/>
    /// The list is empty if no matching ports are found.</returns>
    public static List<string> FindMatchingComPorts(uint vid, uint pid)
    {
        return FindMatchingComPorts($"{vid:X4}", $"{pid:X4}");
    }

    /// <summary>
    /// <inheritdoc cref="FindMatchingComPorts(uint, uint)"/>
    /// </summary>
    /// <param name="vid"><inheritdoc cref="FindMatchingComPorts(uint, uint)"/></param>
    /// <param name="pid"><inheritdoc cref="FindMatchingComPorts(uint, uint)"/></param>
    /// <returns><inheritdoc cref="FindMatchingComPorts(uint, uint)"/></returns>
    public static List<string> FindMatchingComPorts(string vid, string pid)
    {
        var result = new List<string>();

        //Setup
        var devInfo = SetupAPI.SetupDiGetClassDevs(ref GUID_DEVCLASS_PORTS, IntPtr.Zero, IntPtr.Zero, SetupAPI.DIGCF_PRESENT);

        //Check handle
        if (devInfo == IntPtr.Zero || devInfo == InvalidHandle)
        {
            return result;
        }

        try
        {
            SP_DEVINFO_DATA devInfoData = new SP_DEVINFO_DATA();
            devInfoData.cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>();

            uint index = 0;
            while (SetupAPI.SetupDiEnumDeviceInfo(devInfo, index, ref devInfoData))
            {
                ++index;

                //Get hardware ID
                string hwID = GetProperty(devInfo, SetupAPI.SPDRP_HARDWAREID, ref devInfoData);
                if (string.IsNullOrWhiteSpace(hwID))
                {
                    continue;
                }

                //Check if hardware ID contains VID and PID
                if (!hwID.Contains($"VID_{vid}", StringComparison.OrdinalIgnoreCase)
                 || !hwID.Contains($"PID_{pid}", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                //Get friendly name
                string friendlyName = GetProperty(devInfo, SetupAPI.SPDRP_FRIENDLYNAME, ref devInfoData);

                //Extract COM port from friendly name
                int start = friendlyName.LastIndexOf("(COM", StringComparison.OrdinalIgnoreCase);
                int end = friendlyName.LastIndexOf(')');

                //Check if valid and add to result
                if (start >= 0 && end > start)
                {
                    string com = friendlyName.Substring(start + 1, end - start - 1);
                    result.Add(com);
                }
            }
        }
        finally
        {
            //Cleanup
            SetupAPI.SetupDiDestroyDeviceInfoList(devInfo);
        }

        return result;
    }

    static string GetProperty(IntPtr hDevInfo, uint property, ref SP_DEVINFO_DATA devInfoData)
    {
        var buffer = new char[BufferSize];

        if (SetupAPI.SetupDiGetDeviceRegistryProperty(hDevInfo, ref devInfoData, property, out _, buffer, (uint)buffer.Length, out _))
        {
            //Take first string only, no need for the rest
            return NormalizeString(new string(buffer));
        }

        return null;
    }

    static string NormalizeString(string str)
    {
        var end = str.IndexOf('\0');

        if (end == -1)
        {
            end = 0;
        }

        return str.Substring(0, end);
    }
}
