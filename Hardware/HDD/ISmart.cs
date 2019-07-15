// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) 2011-2012 Michael Möller <mmoeller@openhardwaremonitor.org>

using System;

namespace OpenHardwareMonitor.Hardware.HDD {

  public interface ISmart : IDisposable {
    bool IsValid { get; }
    void Close();
    bool EnableSmart();
    Interop.DriveAttributeValue[] ReadSmartData();
    Interop.DriveThresholdValue[] ReadSmartThresholds();
    bool ReadNameAndFirmwareRevision(out string name, out string firmwareRevision);
  }
}