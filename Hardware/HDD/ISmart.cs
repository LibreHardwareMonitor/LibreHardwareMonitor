// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using OpenHardwareMonitor.Interop;

namespace OpenHardwareMonitor.Hardware.HDD {

  internal interface ISmart : IDisposable {
    bool IsValid { get; }
    void Close();
    bool EnableSmart();
    Kernel32.DriveAttributeValue[] ReadSmartData();
    Kernel32.DriveThresholdValue[] ReadSmartThresholds();
    bool ReadNameAndFirmwareRevision(out string name, out string firmwareRevision);
  }
}