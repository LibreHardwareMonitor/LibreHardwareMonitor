// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;

namespace LibreHardwareMonitor.Hardware
{
    public enum SensorType
    {
        Voltage, // V
        Clock, // MHz
        Temperature, // °C
        Load, // %
        Frequency, // Hz
        Fan, // RPM
        Flow, // L/h
        Control, // %
        Level, // %
        Factor, // 1
        Power, // W
        Data, // GB = 2^30 Bytes
        SmallData, // MB = 2^20 Bytes
        Throughput // B/s
    }

    /// <summary>
    /// Stores the readed value and the time in which it was recorded
    /// </summary>
    public struct SensorValue
    {
        /// <param name="value">Value of the sensor</param>
        /// <param name="time">The time code during which the <see cref="Value"/> was recorded</param>
        public SensorValue(float value, DateTime time)
        {
            Value = value;
            Time = time;
        }

        /// <summary>
        /// Value of the sensor
        /// </summary>
        public float Value { get; }

        /// <summary>
        /// The time code during which the <see cref="Value"/> was recorded
        /// </summary>
        public DateTime Time { get; }
    }

    /// <summary>
    /// Stores information about the readed values and the time in which they were collected.
    /// </summary>
    public interface ISensor : IElement
    {
        IControl Control { get; }

        IHardware Hardware { get; }

        Identifier Identifier { get; }

        int Index { get; }

        bool IsDefaultHidden { get; }

        /// <summary>
        /// Maximum value recorded for the given sensor
        /// </summary>
        float? Max { get; }

        /// <summary>
        /// Minimum value recorded for the given sensor
        /// </summary>
        float? Min { get; }

        /// <summary>
        /// Sensor name determined by the library
        /// </summary>
        string Name { get; set; }

        IReadOnlyList<IParameter> Parameters { get; }

        SensorType SensorType { get; }

        /// <summary>
        /// The last recorded value for the given sensor
        /// </summary>
        float? Value { get; }

        /// <summary>
        /// List of recorded values for the given sensor
        /// </summary>
        IEnumerable<SensorValue> Values { get; }

        TimeSpan ValuesTimeWindow { get; set; }

        /// <summary>
        /// Resets a value stored in <see cref="Min"/>
        /// </summary>
        void ResetMin();

        /// <summary>
        /// Resets a value stored in <see cref="Max"/>
        /// </summary>
        void ResetMax();
    }
}
