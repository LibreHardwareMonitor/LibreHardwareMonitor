// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Globalization;

namespace LibreHardwareMonitor.Hardware;

internal delegate void ControlEventHandler(Control control);

internal class Control : IControl
{
    private readonly ISettings _settings;
    private ControlMode _mode;
    private float _softwareValue;

    public Control
    (
        ISensor sensor,
        ISettings settings,
        float minSoftwareValue,
        float maxSoftwareValue)
    {
        _settings = settings;

        Identifier = new Identifier(sensor.Identifier, "control");
        Sensor = sensor;
        MinSoftwareValue = minSoftwareValue;
        MaxSoftwareValue = maxSoftwareValue;

        if (!float.TryParse(settings.GetValue(new Identifier(Identifier, "value").ToString(), "0"), NumberStyles.Float, CultureInfo.InvariantCulture, out _softwareValue))
            _softwareValue = 0;

        if (!int.TryParse(settings.GetValue(new Identifier(Identifier, "mode").ToString(), ((int)ControlMode.Undefined).ToString(CultureInfo.InvariantCulture)),
                          NumberStyles.Integer,
                          CultureInfo.InvariantCulture,
                          out int mode))
        {
            _mode = ControlMode.Undefined;
        }
        else
            _mode = (ControlMode)mode;
    }

    internal event ControlEventHandler ControlModeChanged;

    internal event ControlEventHandler SoftwareControlValueChanged;

    public ControlMode ControlMode
    {
        get { return _mode; }
        private set
        {
            if (_mode != value)
            {
                _mode = value;
                ControlModeChanged?.Invoke(this);
                _settings.SetValue(new Identifier(Identifier, "mode").ToString(), ((int)_mode).ToString(CultureInfo.InvariantCulture));
            }
        }
    }

    public Identifier Identifier { get; }

    public float MaxSoftwareValue { get; }

    public float MinSoftwareValue { get; }

    public ISensor Sensor { get; }

    public float SoftwareValue
    {
        get { return _softwareValue; }
        private set
        {
            if (_softwareValue != value)
            {
                _softwareValue = value;
                SoftwareControlValueChanged?.Invoke(this);
                _settings.SetValue(new Identifier(Identifier, "value").ToString(), value.ToString(CultureInfo.InvariantCulture));
            }
        }
    }

    public void SetDefault()
    {
        ControlMode = ControlMode.Default;
    }

    public void SetSoftware(float value)
    {
        ControlMode = ControlMode.Software;
        SoftwareValue = value;
    }
}
