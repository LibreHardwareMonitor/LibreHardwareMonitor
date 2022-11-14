// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System.Runtime.InteropServices;
using LibreHardwareMonitor.Interop;

namespace LibreHardwareMonitor.Hardware.Storage;

internal interface INVMeDrive
{
    SafeHandle Identify(StorageInfo storageInfo);

    bool IdentifyController(SafeHandle hDevice, out Kernel32.NVME_IDENTIFY_CONTROLLER_DATA data);

    bool HealthInfoLog(SafeHandle hDevice, out Kernel32.NVME_HEALTH_INFO_LOG data);
}