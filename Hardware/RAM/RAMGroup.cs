// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) 2011-2012 Michael Möller <mmoeller@openhardwaremonitor.org>
// Copyright (C) 2016-2019 Sebastian Grams <https://github.com/sebastian-dev>
// Copyright (C) 2016-2019 Aqua Computer <https://github.com/aquacomputer, info@aqua-computer.de>

using System;
using System.Collections.Generic;

namespace OpenHardwareMonitor.Hardware.RAM {
  internal class RAMGroup : IGroup {

    private Hardware[] hardware;

    public RAMGroup(SMBIOS smbios, ISettings settings) {

      // No implementation for RAM on Unix systems
      if (Software.OperatingSystem.IsLinux) {
        hardware = new Hardware[0];
        return;
      }
      hardware = new Hardware[] { new GenericRAM("Generic Memory", settings) };
    }

    public string GetReport() {
      return null;
    }

    public IEnumerable<IHardware> Hardware => hardware;

    public void Close() {
      foreach (Hardware ram in hardware)
        ram.Close();
    }
  }
}
