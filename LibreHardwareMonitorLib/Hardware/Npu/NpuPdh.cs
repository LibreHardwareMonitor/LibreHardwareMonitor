// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LibreHardwareMonitor.Hardware.Npu;

/// <summary>
/// Thin P/Invoke wrapper around the Windows Performance Data Helper (PDH) API,
/// scoped to the counter patterns used by NPU monitoring.
/// </summary>
/// <remarks>
/// PDH is the same API surface used internally by Windows Task Manager and
/// Process Explorer to surface NPU utilization and memory metrics.
/// The counter objects "NPU Engine Utilization" and "NPU Adapter Memory" are
/// available on Windows 11 24H2 (build 26100) and later.
/// </remarks>
internal static class NpuPdh
{
    private const uint PDH_NO_DATA = 0xC0000BF6;
    private const uint PDH_INVALID_DATA = 0xC0000BC6;
    private const uint PDH_FMT_DOUBLE = 0x00000200;
    private const uint PDH_MORE_DATA = 0x800007D2;

    // PDH_STATUS success code
    private const uint ERROR_SUCCESS = 0;

    [DllImport("pdh.dll", EntryPoint = "PdhOpenQueryW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern uint NativePdhOpenQuery(IntPtr dataSource, IntPtr userData, out IntPtr queryHandle);

    [DllImport("pdh.dll", EntryPoint = "PdhAddEnglishCounterW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern uint NativePdhAddEnglishCounter(IntPtr queryHandle, string counterPath, IntPtr userData, out IntPtr counterHandle);

    [DllImport("pdh.dll", EntryPoint = "PdhCollectQueryData", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern uint NativePdhCollectQueryData(IntPtr queryHandle);

    [DllImport("pdh.dll", EntryPoint = "PdhCloseQuery", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern uint NativePdhCloseQuery(IntPtr queryHandle);

    [DllImport("pdh.dll", EntryPoint = "PdhGetFormattedCounterValue", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern uint NativePdhGetFormattedCounterValue(IntPtr counterHandle, uint format, out uint type, out PdhFmtCounterValue value);

    [DllImport("pdh.dll", EntryPoint = "PdhEnumObjectItemsW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern uint NativePdhEnumObjectItems(
        string dataSource,
        string machineName,
        string objectName,
        char[] counterList,
        ref uint counterListSize,
        char[] instanceList,
        ref uint instanceListSize,
        uint detailLevel,
        uint flags);

    [StructLayout(LayoutKind.Sequential)]
    private struct PdhFmtCounterValue
    {
        public uint CStatus;
        // Union: double is the largest member (8 bytes); pad to align.
        public double DoubleValue;
    }

    // PDH detail level: PERF_DETAIL_WIZARD = 400
    private const uint PERF_DETAIL_WIZARD = 400;

    /// <summary>
    /// Opens a real-time PDH query. Returns the PDH status code.
    /// </summary>
    public static uint PdhOpenQuery(IntPtr dataSource, IntPtr userData, out IntPtr queryHandle)
        => NativePdhOpenQuery(dataSource, userData, out queryHandle);

    /// <summary>
    /// Adds a counter to an open PDH query using the English counter path.
    /// On failure <paramref name="counterHandle"/> is set to <see cref="IntPtr.Zero"/>.
    /// </summary>
    public static void PdhAddEnglishCounter(IntPtr queryHandle, string path, IntPtr userData, out IntPtr counterHandle)
    {
        uint status = NativePdhAddEnglishCounter(queryHandle, path, userData, out counterHandle);
        if (status != ERROR_SUCCESS)
            counterHandle = IntPtr.Zero;
    }

    /// <summary>Collects a snapshot of all counters in the query.</summary>
    public static uint PdhCollectQueryData(IntPtr queryHandle)
        => NativePdhCollectQueryData(queryHandle);

    /// <summary>Closes a PDH query and frees all associated resources.</summary>
    public static uint PdhCloseQuery(IntPtr queryHandle)
        => NativePdhCloseQuery(queryHandle);

    /// <summary>
    /// Tries to read the current formatted double value from a counter.
    /// </summary>
    /// <returns><see langword="true"/> when a valid value was obtained.</returns>
    public static bool TryGetDoubleValue(IntPtr counterHandle, out double value)
    {
        value = 0;
        if (counterHandle == IntPtr.Zero)
            return false;

        uint status = NativePdhGetFormattedCounterValue(counterHandle, PDH_FMT_DOUBLE, out _, out PdhFmtCounterValue fmtValue);
        if (status != ERROR_SUCCESS)
            return false;

        value = fmtValue.DoubleValue;
        return true;
    }

    /// <summary>
    /// Enumerates the instance names of a PDH performance object (e.g.
    /// "NPU Engine Utilization") on the local machine.
    /// </summary>
    /// <param name="objectName">The English object name to enumerate.</param>
    /// <returns>
    /// The list of instance names, or an empty list if the object is not
    /// present (e.g. Windows build older than 11 24H2, or no NPU installed).
    /// </returns>
    public static IReadOnlyList<string> EnumerateInstances(string objectName)
    {
        var result = new List<string>();

        try
        {
            uint counterListSize = 0;
            uint instanceListSize = 0;

            // First call: determine required buffer sizes.
            uint status = NativePdhEnumObjectItems(
                null, null, objectName,
                null, ref counterListSize,
                null, ref instanceListSize,
                PERF_DETAIL_WIZARD, 0);

            if (status != PDH_MORE_DATA && status != ERROR_SUCCESS)
                return result;

            if (instanceListSize == 0)
                return result;

            char[] counterBuffer = new char[counterListSize];
            char[] instanceBuffer = new char[instanceListSize];

            status = NativePdhEnumObjectItems(
                null, null, objectName,
                counterBuffer, ref counterListSize,
                instanceBuffer, ref instanceListSize,
                PERF_DETAIL_WIZARD, 0);

            if (status != ERROR_SUCCESS)
                return result;

            // The instance list is a double-null-terminated multi-string.
            int start = 0;
            for (int i = 0; i < instanceBuffer.Length; i++)
            {
                if (instanceBuffer[i] == '\0')
                {
                    if (i == start)
                        break; // double-null terminator

                    string name = new string(instanceBuffer, start, i - start);
                    result.Add(name);
                    start = i + 1;
                }
            }
        }
        catch
        {
            // Treat any failure as "no NPU counters available".
        }

        return result;
    }

    /// <summary>
    /// Returns <see langword="true"/> when the "NPU Engine Utilization" PDH
    /// performance object exists on the local machine (requires Windows 11 24H2+).
    /// </summary>
    public static bool IsNpuObjectAvailable()
        => EnumerateInstances("NPU Engine Utilization").Count > 0;
}
