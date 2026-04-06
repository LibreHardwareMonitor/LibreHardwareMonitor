// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) Florian K. (Blacktempel)
// All Rights Reserved.

using StorageDeviceDIT = DiskInfoToolkit.StorageDevice;

namespace LibreHardwareMonitor.Hardware.Storage;

internal delegate float GetStorageDeviceSensorValue(StorageDeviceDIT storage);

internal class StorageDeviceSensor : Sensor
{
    private readonly GetStorageDeviceSensorValue _getValue;

    public StorageDeviceSensor(string name, int index, bool defaultHidden, SensorType sensorType, Hardware hardware, ISettings settings, GetStorageDeviceSensorValue getValue)
        : base(name, index, defaultHidden, sensorType, hardware, null, settings)
    {
        _getValue = getValue;
    }

    public void Update(StorageDeviceDIT storage)
    {
        var value = _getValue(storage);

        Value = value;
    }
}
