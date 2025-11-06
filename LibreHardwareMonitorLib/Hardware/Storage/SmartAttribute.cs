// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware.Storage;

public sealed class SmartAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SmartAttribute"/> class.
    /// </summary>
    /// <param name="smartAttribute">The SMART attribute.</param>
    /// <param name="sensorType">
    /// Type of the sensor or null if no sensor is to
    /// be created.
    /// </param>
    /// <param name="sensorChannel">
    /// If there exists more than one attribute with
    /// the same sensor channel and type, then a sensor is created only for the
    /// first attribute.
    /// </param>
    /// <param name="sensorName">
    /// The name to be used for the sensor, or null if
    /// no sensor is created.
    /// </param>
    /// <param name="defaultHiddenSensor">True to hide the sensor initially.</param>
    public SmartAttribute(DiskInfoToolkit.SmartAttribute smartAttribute, SensorType? sensorType, int sensorChannel, string sensorName, bool defaultHiddenSensor = false)
    {
        Attribute = smartAttribute;
        SensorType = sensorType;
        SensorChannel = sensorChannel;
        SensorName = sensorName ?? Name;
        IsHiddenByDefault = defaultHiddenSensor;
    }

    public DiskInfoToolkit.SmartAttribute Attribute { get; internal set; }

    public byte Id => Attribute.Info.ID;
    public string Name => Attribute.Info.Name;

    public SensorType? SensorType { get; }
    public int SensorChannel { get; }
    public string SensorName { get; }
    public bool IsHiddenByDefault { get; }

    public float Value => Attribute.Attribute.RawValueULong;
    public byte Threshold => Attribute.Attribute.Threshold;
}
