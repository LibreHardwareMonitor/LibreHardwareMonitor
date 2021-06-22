// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Collections.Generic;

namespace LibreHardwareMonitor.Hardware
{
    public delegate void SensorEventHandler(ISensor sensor);

    /// <summary>
    /// Reflects what category the device is.
    /// </summary>
    public enum HardwareType
    {
        Motherboard,
        SuperIO,
        Cpu,
        Memory,
        GpuNvidia,
        GpuAmd,
        Storage,
        Network,
        Cooler,
        EmbeddedController,
        Psu
    }

    /// <summary>
    /// An abstract object that stores information about a device. All sensors are available as an array of <see cref="Sensors"/>.
    /// </summary>
    public interface IHardware : IElement
    {
        /// <summary>
        /// <inheritdoc cref="LibreHardwareMonitor.Hardware.HardwareType"/>
        /// </summary>
        HardwareType HardwareType { get; }

        /// <summary>
        /// Gets unique hardware identifier obtained from the computer.
        /// </summary>
        Identifier Identifier { get; }

        /// <summary>
        /// Gets or sets device name.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets the device that is the parent of the current hardware. For example, the motherboard is the parent of SuperIO.
        /// </summary>
        IHardware Parent { get; }

        /// <summary>
        /// Gets an array of all sensors such as temperature, clocks, load etc.
        /// </summary>
        ISensor[] Sensors { get; }

        /// <summary>
        /// Gets child devices, e.g. SuperIO of the motherboard.
        /// </summary>
        IHardware[] SubHardware { get; }

        /// <summary>
        /// Report containing most of the known information about the current device.
        /// </summary>
        /// <returns>A formatted text string with hardware information.</returns>
        string GetReport();

        /// <summary>
        /// Refreshes the information stored in <see cref="Sensors"/> array.
        /// </summary>
        void Update();

        /// <summary>
        /// An <see langword="event"/> that will be triggered when a new sensor appears.
        /// </summary>
        event SensorEventHandler SensorAdded;

        /// <summary>
        /// An <see langword="event"/> that will be triggered when one of the sensors is removed.
        /// 
        /// </summary>
        event SensorEventHandler SensorRemoved;

        /// <summary>
        /// Rarely changed hardware properties that can't be represented as sensors.
        /// </summary>
        IDictionary<string, string> Properties { get; }
    }
}
