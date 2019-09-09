// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System.Collections.Generic;

namespace OpenHardwareMonitor.Hardware.HDD {
  [NamePrefix("PLEXTOR")]
  internal class SSDPlextor : ATAStorage {
    private static readonly IEnumerable<SmartAttribute> smartAttributes =
      new List<SmartAttribute> {
        new SmartAttribute(0x09, SmartNames.PowerOnHours, RawToInt),
        new SmartAttribute(0x0C, SmartNames.PowerCycleCount, RawToInt),
        new SmartAttribute(0xF1, SmartNames.HostWrites, RawToGb, SensorType.Data, 0, SmartNames.HostWrites),
        new SmartAttribute(0xF2, SmartNames.HostReads, RawToGb, SensorType.Data, 1, SmartNames.HostReads)
      };

    public SSDPlextor(StorageInfo storageInfo, ISmart smart, string name, string firmwareRevision, int index, ISettings settings)
      : base(storageInfo, smart, name, firmwareRevision, "ssd", index, smartAttributes, settings) { }

    private static float RawToGb(byte[] rawValue, byte value, IReadOnlyList<IParameter> parameters) {
      return RawToInt(rawValue, value, parameters) / 32;
    }
  }
}