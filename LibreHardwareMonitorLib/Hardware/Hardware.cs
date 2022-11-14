// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace LibreHardwareMonitor.Hardware;

/// <summary>
/// Object representing a component of the computer.
/// <para>
/// Individual information can be read from the <see cref="Sensors"/>.
/// </para>
/// </summary>
public abstract class Hardware : IHardware
{
    protected readonly HashSet<ISensor> _active = new();
    protected readonly string _name;
    protected readonly ISettings _settings;
    private string _customName;

    /// <summary>
    /// Creates a new <see cref="Hardware"/> instance based on the data provided.
    /// </summary>
    /// <param name="name">Component name.</param>
    /// <param name="identifier">Identifier that will be assigned to the device. Based on <see cref="Identifier"/></param>
    /// <param name="settings">Additional settings passed by the <see cref="IComputer"/>.</param>
    protected Hardware(string name, Identifier identifier, ISettings settings)
    {
        _settings = settings;
        _name = name;
        Identifier = identifier;
        _customName = settings.GetValue(new Identifier(Identifier, "name").ToString(), name);
    }

    /// <summary>
    /// Event triggered when <see cref="Hardware"/> is closing.
    /// </summary>
    public event HardwareEventHandler Closing;

    /// <inheritdoc />
    public abstract HardwareType HardwareType { get; }

    /// <inheritdoc />
    public Identifier Identifier { get; }

    /// <inheritdoc />
    public string Name
    {
        get { return _customName; }
        set
        {
            _customName = !string.IsNullOrEmpty(value) ? value : _name;

            _settings.SetValue(new Identifier(Identifier, "name").ToString(), _customName);
        }
    }

    /// <inheritdoc />
    public virtual IHardware Parent
    {
        get { return null; }
    }

    /// <inheritdoc />
    public virtual IDictionary<string, string> Properties => new SortedDictionary<string, string>();

    /// <inheritdoc />
    public virtual ISensor[] Sensors
    {
        get { return _active.ToArray(); }
    }

    /// <inheritdoc />
    public IHardware[] SubHardware
    {
        get { return Array.Empty<IHardware>(); }
    }

    /// <inheritdoc />
    public virtual string GetReport()
    {
        return null;
    }

    /// <inheritdoc />
    public abstract void Update();

    /// <inheritdoc />
    public void Accept(IVisitor visitor)
    {
        if (visitor == null)
            throw new ArgumentNullException(nameof(visitor));

        visitor.VisitHardware(this);
    }

    /// <inheritdoc />
    public virtual void Traverse(IVisitor visitor)
    {
        foreach (ISensor sensor in _active)
            sensor.Accept(visitor);
    }

    /// <inheritdoc />
    protected virtual void ActivateSensor(ISensor sensor)
    {
        if (_active.Add(sensor))
            SensorAdded?.Invoke(sensor);
    }

    /// <inheritdoc />
    protected virtual void DeactivateSensor(ISensor sensor)
    {
        if (_active.Remove(sensor))
            SensorRemoved?.Invoke(sensor);
    }

    /// <inheritdoc />
    public virtual void Close()
    {
        Closing?.Invoke(this);
    }

#pragma warning disable 67
    public event SensorEventHandler SensorAdded;

    public event SensorEventHandler SensorRemoved;
#pragma warning restore 67
}