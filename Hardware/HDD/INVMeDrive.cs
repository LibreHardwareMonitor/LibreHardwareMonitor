// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) 2016-2019 Sebastian Grams <https://github.com/sebastian-dev>
// Copyright (C) 2016-2019 Aqua Computer <https://github.com/aquacomputer, info@aqua-computer.de>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace OpenHardwareMonitor.Hardware.HDD {
  public interface INVMeDrive {
    SafeHandle Identify(StorageInfo _storageInfo);
    bool IdentifyController(SafeHandle hDevice, out Interop.NVMeIdentifyControllerData data);
    bool HealthInfoLog(SafeHandle hDevice, out Interop.NVMeHealthInfoLog data);
  }
}