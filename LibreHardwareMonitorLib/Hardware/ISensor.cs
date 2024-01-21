// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;

namespace LibreHardwareMonitor.Hardware;

/// <summary>
/// Category of what type the selected sensor is.
/// </summary>
public enum SensorType
{
    Voltage, // V
    Current, // A
    Power, // W
    Clock, // MHz
    Temperature, // °C
    Load, // %
    Frequency, // Hz
    Fan, // RPM
    Flow, // L/h
    Control, // %
    Level, // %
    Factor, // 1
    Data, // GB = 2^30 Bytes
    SmallData, // MB = 2^20 Bytes
    Throughput, // B/s
    TimeSpan, // Seconds 
    Energy, // milliwatt-hour (mWh)
    Noise // dBA
}

/// <summary>
/// Stores the readed value and the time in which it was recorded.
/// </summary>
public struct SensorValue
{
    /// <param name="value"><see cref="Value"/> of the sensor.</param>
    /// <param name="time">The time code during which the <see cref="Value"/> was recorded.</param>
    public SensorValue(float value, DateTime time)
    {
        Value = value;
        Time = time;
    }

    /// <summary>
    /// Gets the value of the sensor
    /// </summary>
    public float Value { get; }

    /// <summary>
    /// Gets the time code during which the <see cref="Value"/> was recorded.
    /// </summary>
    public DateTime Time { get; }
}

/// <summary>
/// Stores information about the readed values and the time in which they were collected.
/// </summary>
public interface ISensor : IElement
{
    IControl Control { get; }

    /// <summary>
    /// <inheritdoc cref="IHardware"/>
    /// </summary>
    IHardware Hardware { get; }

    Identifier Identifier { get; }

    /// <summary>
    /// Gets the unique identifier of this sensor for a given <see cref="IHardware"/>.
    /// </summary>
    int Index { get; }

    bool IsDefaultHidden { get; }

    /// <summary>
    /// Gets a maximum value recorded for the given sensor.
    /// </summary>
    float? Max { get; }

    /// <summary>
    /// Gets a minimum value recorded for the given sensor.
    /// </summary>
    float? Min { get; }

    /// <summary>
    /// Gets or sets a sensor name.
    /// <para>By default determined by the library.</para>
    /// </summary>
    string Name { get; set; }

    IReadOnlyList<IParameter> Parameters { get; }

    /// <summary>
    /// <inheritdoc cref="LibreHardwareMonitor.Hardware.SensorType"/>
    /// </summary>
    SensorType SensorType { get; }

    /// <summary>
    /// Gets the last recorded value for the given sensor.
    /// </summary>
    float? Value { get; }

    /// <summary>
    /// Gets a list of recorded values for the given sensor.
    /// </summary>
    IEnumerable<SensorValue> Values { get; }

    TimeSpan ValuesTimeWindow { get; set; }

    /// <summary>
    /// Resets a value stored in <see cref="Min"/>.
    /// </summary>
    void ResetMin();

    /// <summary>
    /// Resets a value stored in <see cref="Max"/>.
    /// </summary>
    void ResetMax();

    /// <summary>
    /// Clears the values stored in <see cref="Values"/>.
    /// </summary>
    void ClearValues();
}
