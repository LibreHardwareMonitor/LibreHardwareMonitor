// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware.Storage;

internal delegate float GetStorageDeviceSensorValue(DiskInfoToolkit.Storage storage);

internal class StorageDeviceSensor : Sensor
{
    private readonly GetStorageDeviceSensorValue _getValue;

    public StorageDeviceSensor(string name, int index, bool defaultHidden, SensorType sensorType, Hardware hardware, ISettings settings, GetStorageDeviceSensorValue getValue)
        : base(name, index, defaultHidden, sensorType, hardware, null, settings)
    {
        _getValue = getValue;
    }

    public void Update(DiskInfoToolkit.Storage storage)
    {
        var value = _getValue(storage);

        Value = value;
    }
}
