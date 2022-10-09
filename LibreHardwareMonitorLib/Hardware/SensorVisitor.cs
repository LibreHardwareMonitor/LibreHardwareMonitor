// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;

namespace LibreHardwareMonitor.Hardware;

/// <summary>
/// Observer making calls to selected component <see cref="ISensor"/>'s.
/// </summary>
public class SensorVisitor : IVisitor
{
    private readonly SensorEventHandler _handler;

    /// <summary>
    /// Creates a new observer instance.
    /// </summary>
    /// <param name="handler">Instance of the <see cref="SensorEventHandler"/> that triggers events during visiting the <see cref="ISensor"/>.</param>
    public SensorVisitor(SensorEventHandler handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <summary>
    /// Goes through all the components of the specified <see cref="IComputer"/> with its <see cref="IElement.Traverse(IVisitor)"/>.
    /// </summary>
    /// <param name="computer">Computer class instance that is derived from the <see cref="IComputer"/> interface.</param>
    public void VisitComputer(IComputer computer)
    {
        if (computer == null)
            throw new ArgumentNullException(nameof(computer));

        computer.Traverse(this);
    }

    /// <summary>
    /// Goes through all the components of the specified <see cref="IHardware"/> with its <see cref="IElement.Traverse(IVisitor)"/>.
    /// </summary>
    /// <param name="hardware">Hardware class instance that is derived from the <see cref="IHardware"/> interface.</param>
    public void VisitHardware(IHardware hardware)
    {
        if (hardware == null)
            throw new ArgumentNullException(nameof(hardware));

        hardware.Traverse(this);
    }

    /// <summary>
    /// Goes through all the components of the specified <see cref="ISensor"/> using <see cref="SensorEventHandler"/>.
    /// </summary>
    /// <param name="sensor">Sensor class instance that is derived from the <see cref="ISensor"/> interface.</param>
    public void VisitSensor(ISensor sensor)
    {
        _handler(sensor);
    }

    /// <summary>
    /// Goes through all the components of the specified <see cref="IParameter"/>.
    /// <para>
    /// <see cref="NotImplementedException"/>
    /// </para>
    /// </summary>
    /// <param name="parameter">Parameter class instance that is derived from the <see cref="IParameter"/> interface.</param>
    public void VisitParameter(IParameter parameter)
    { }
}