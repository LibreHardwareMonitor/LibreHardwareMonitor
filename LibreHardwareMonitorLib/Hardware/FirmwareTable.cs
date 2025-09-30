// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Win32;
using Windows.Win32.System.SystemInformation;

namespace LibreHardwareMonitor.Hardware;

internal static class FirmwareTable
{
    public static byte[] GetTable(FIRMWARE_TABLE_PROVIDER provider, string table)
    {
        uint id = (uint)((table[3] << 24) | (table[2] << 16) | (table[1] << 8) | table[0]);
        return GetTable(provider, id);
    }

    public static byte[] GetTable(FIRMWARE_TABLE_PROVIDER provider, uint table)
    {
        uint size;

        try
        {
            size = PInvoke.GetSystemFirmwareTable(provider, table, null);
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            return null;
        }

        if (size <= 0)
            return null;

        byte[] buffer = new byte[size];

        PInvoke.GetSystemFirmwareTable(provider, table, buffer.AsSpan());
        if (Marshal.GetLastWin32Error() != 0)
            return null;

        return buffer;
    }

    public static unsafe string[] EnumerateTables(FIRMWARE_TABLE_PROVIDER provider)
    {
        uint size;

        try
        {
            size = PInvoke.EnumSystemFirmwareTables(provider, (byte*)IntPtr.Zero, 0);
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            return null;
        }

        byte[] buffer = new byte[size];
        PInvoke.EnumSystemFirmwareTables(provider, buffer.AsSpan());

        string[] result = new string[size / 4];

        for (int i = 0; i < result.Length; i++)
            result[i] = Encoding.ASCII.GetString(buffer, 4 * i, 4);

        return result;
    }
}
