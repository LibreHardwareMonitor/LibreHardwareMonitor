// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

using System.Runtime.InteropServices;

namespace LibreHardwareMonitor.Interop;

/// <summary>
/// Contains Win32 definitions for Windows NT.
/// </summary>
internal static class WinNt
{
    internal const int STATUS_SUCCESS = 0;

    /// <summary>
    /// Describes a local identifier for an adapter.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct LUID
    {
        public readonly uint LowPart;
        public readonly int HighPart;
    }

    /// <summary>
    /// Represents a 64-bit signed integer value.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    internal struct LARGE_INTEGER
    {
        [FieldOffset(0)]
        public long QuadPart;

        [FieldOffset(0)]
        public uint LowPart;

        [FieldOffset(4)]
        public int HighPart;
    }
}