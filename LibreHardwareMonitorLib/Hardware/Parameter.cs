// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Globalization;

namespace LibreHardwareMonitor.Hardware;

internal class Parameter : IParameter
{
    private readonly ISettings _settings;
    private readonly ParameterDescription _description;
    private bool _isDefault;
    private float _value;

    public Parameter(ParameterDescription description, ISensor sensor, ISettings settings)
    {
        Sensor = sensor;
        _description = description;
        _settings = settings;
        _isDefault = !settings.Contains(Identifier.ToString());
        _value = description.DefaultValue;
        if (!_isDefault && !float.TryParse(settings.GetValue(Identifier.ToString(), "0"), NumberStyles.Float, CultureInfo.InvariantCulture, out _value))
        {
            _value = description.DefaultValue;
        }
    }

    public float DefaultValue
    {
        get { return _description.DefaultValue; }
    }

    public string Description
    {
        get { return _description.Description; }
    }

    public Identifier Identifier
    {
        get { return new Identifier(Sensor.Identifier, "parameter", Name.Replace(" ", string.Empty).ToLowerInvariant()); }
    }

    public bool IsDefault
    {
        get { return _isDefault; }
        set
        {
            _isDefault = value;
            if (value)
            {
                _value = _description.DefaultValue;
                _settings.Remove(Identifier.ToString());
            }
        }
    }

    public string Name
    {
        get { return _description.Name; }
    }

    public ISensor Sensor { get; }

    public float Value
    {
        get { return _value; }
        set
        {
            _isDefault = false;
            _value = value;
            _settings.SetValue(Identifier.ToString(), value.ToString(CultureInfo.InvariantCulture));
        }
    }

    public void Accept(IVisitor visitor)
    {
        if (visitor == null)
            throw new ArgumentNullException(nameof(visitor));

        visitor.VisitParameter(this);
    }

    public void Traverse(IVisitor visitor)
    { }
}