// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using OpenHardwareMonitor.Interop;

namespace OpenHardwareMonitor.Hardware.HDD {
  internal interface INVMeDrive {
    SafeHandle Identify(StorageInfo _storageInfo);
    bool IdentifyController(SafeHandle hDevice, out Kernel32.NVMeIdentifyControllerData data);
    bool HealthInfoLog(SafeHandle hDevice, out Kernel32.NVMeHealthInfoLog data);
  }
}