// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware;

/// <summary>
/// Base interface for creating observers who call to devices.
/// </summary>
public interface IVisitor
{
    /// <summary>
    /// Refreshes the values of all <see cref="ISensor"/> in all <see cref="IHardware"/> on selected <see cref="IComputer"/>.
    /// </summary>
    /// <param name="computer">Instance of the computer to be revisited.</param>
    void VisitComputer(IComputer computer);

    /// <summary>
    /// Refreshes the values of all <see cref="ISensor"/> on selected <see cref="IHardware"/>.
    /// </summary>
    /// <param name="hardware">Instance of the hardware to be revisited.</param>
    void VisitHardware(IHardware hardware);

    /// <summary>
    /// Refreshes the values on selected <see cref="ISensor"/>.
    /// </summary>
    /// <param name="sensor">Instance of the sensor to be revisited.</param>
    void VisitSensor(ISensor sensor);

    /// <summary>
    /// Refreshes the values on selected <see cref="IParameter"/>.
    /// </summary>
    /// <param name="parameter">Instance of the parameter to be revisited.</param>
    void VisitParameter(IParameter parameter);
}