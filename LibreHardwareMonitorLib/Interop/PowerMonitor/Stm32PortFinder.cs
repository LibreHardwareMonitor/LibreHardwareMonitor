// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Win32;
using Windows.Win32.Devices.DeviceAndDriverInstallation;
using Windows.Win32.Foundation;

namespace LibreHardwareMonitor.Interop.PowerMonitor;

/// <summary>
/// Helper class to find STM32 COM ports based on VID and PID.
/// </summary>
public static class Stm32PortFinder
{
    private const int BufferSize = 1024;

    /// <summary>
    /// Finds the names of all available COM ports that match the specified USB vendor ID (VID) and product ID (PID).
    /// </summary>
    /// <param name="vid">The USB vendor ID to match.</param>
    /// <param name="pid">The USB product ID to match.</param>
    /// <returns>
    /// A list of strings containing the names of matching COM ports.<br />
    /// The list is empty if no matching ports are found.
    /// </returns>
    public static List<string> FindMatchingComPorts(uint vid, uint pid)
    {
        return FindMatchingComPorts($"{vid:X4}", $"{pid:X4}");
    }

    /// <summary>
    ///     <inheritdoc cref="FindMatchingComPorts(uint, uint)" />
    /// </summary>
    /// <param name="vid">
    ///     <inheritdoc cref="FindMatchingComPorts(uint, uint)" />
    /// </param>
    /// <param name="pid">
    ///     <inheritdoc cref="FindMatchingComPorts(uint, uint)" />
    /// </param>
    /// <returns>
    ///     <inheritdoc cref="FindMatchingComPorts(uint, uint)" />
    /// </returns>
    public static List<string> FindMatchingComPorts(string vid, string pid)
    {
        var result = new List<string>();

        //Setup
        SetupDiDestroyDeviceInfoListSafeHandle devInfo = PInvoke.SetupDiGetClassDevs(PInvoke.GUID_DEVCLASS_PORTS, null, HWND.Null, SETUP_DI_GET_CLASS_DEVS_FLAGS.DIGCF_PRESENT);

        //Check handle
        if (devInfo.IsInvalid)
        {
            return result;
        }

        try
        {
            SP_DEVINFO_DATA devInfoData = new() { cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>() };

            uint index = 0;
            while (PInvoke.SetupDiEnumDeviceInfo(devInfo, index++, ref devInfoData))
            {
                //Get hardware ID
                string hwId = GetProperty(devInfo, SETUP_DI_REGISTRY_PROPERTY.SPDRP_HARDWAREID, devInfoData);
                if (string.IsNullOrWhiteSpace(hwId))
                {
                    continue;
                }

                //Check if hardware ID contains VID and PID
                if (!hwId.Contains($"VID_{vid}", StringComparison.OrdinalIgnoreCase) ||
                    !hwId.Contains($"PID_{pid}", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                //Get friendly name
                string friendlyName = GetProperty(devInfo, SETUP_DI_REGISTRY_PROPERTY.SPDRP_FRIENDLYNAME, devInfoData);

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
            devInfo.Dispose();
        }

        return result;

        static string GetProperty(SafeHandle hDevInfo, SETUP_DI_REGISTRY_PROPERTY property, SP_DEVINFO_DATA devInfoData)
        {
            byte[] buffer = new byte[BufferSize];

            if (PInvoke.SetupDiGetDeviceRegistryProperty(hDevInfo, devInfoData, property, buffer))
            {
                //Take first string only, no need for the rest
                return NormalizeString(Encoding.Unicode.GetString(buffer));
            }

            return null;
        }
    }

    private static string NormalizeString(string str)
    {
        int end = str.IndexOf('\0');
        if (end == -1)
        {
            end = 0;
        }

        return str.Substring(0, end);
    }
}
