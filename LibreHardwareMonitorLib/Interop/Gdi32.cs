// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

using System.Runtime.InteropServices;

namespace LibreHardwareMonitor.Interop;

internal static class Gdi32
{
    internal const string DllName = "Gdi32.dll";

    [DllImport(DllName, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern uint D3DKMTCloseAdapter(ref D3dkmth.D3DKMT_CLOSEADAPTER closeAdapter);

    [DllImport(DllName, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern uint D3DKMTOpenAdapterFromDeviceName(ref D3dkmth.D3DKMT_OPENADAPTERFROMDEVICENAME openAdapterFromDeviceName);

    [DllImport(DllName, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern uint D3DKMTQueryAdapterInfo(ref D3dkmth.D3DKMT_QUERYADAPTERINFO queryAdapterInfo);

    [DllImport(DllName, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern uint D3DKMTQueryStatistics(ref D3dkmth.D3DKMT_QUERYSTATISTICS queryStatistics);
}