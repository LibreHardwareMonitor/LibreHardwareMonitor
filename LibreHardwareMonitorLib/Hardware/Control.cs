// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System.Globalization;

namespace LibreHardwareMonitor.Hardware
{
    internal delegate void ControlEventHandler(Control control);

    internal class Control : IControl
    {
        private readonly ISettings _settings;
        private ControlMode _mode;
        private float _softwareValue;

        public Control(ISensor sensor, ISettings settings, float minSoftwareValue,
          float maxSoftwareValue)
        {
            Identifier = new Identifier(sensor.Identifier, "control");
            _settings = settings;
            MinSoftwareValue = minSoftwareValue;
            MaxSoftwareValue = maxSoftwareValue;

            if (!float.TryParse(settings.GetValue(new Identifier(Identifier, "value").ToString(), "0"), NumberStyles.Float, CultureInfo.InvariantCulture, out _softwareValue))
                _softwareValue = 0;

            int mode;
            if (!int.TryParse(settings.GetValue(new Identifier(Identifier, "mode").ToString(), ((int)ControlMode.Undefined).ToString(CultureInfo.InvariantCulture)), NumberStyles.Integer, CultureInfo.InvariantCulture, out mode))
            {
                _mode = ControlMode.Undefined;
            }
            else
                _mode = (ControlMode)mode;
        }

        public Identifier Identifier { get; private set; }

        public ControlMode ControlMode
        {
            get
            {
                return _mode;
            }
            private set
            {
                if (_mode != value)
                {
                    _mode = value;
                    if (ControlModeChanged != null)
                        ControlModeChanged(this);
                    _settings.SetValue(new Identifier(Identifier, "mode").ToString(), ((int)_mode).ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        public float SoftwareValue
        {
            get
            {
                return _softwareValue;
            }
            private set
            {
                if (_softwareValue != value)
                {
                    _softwareValue = value;
                    if (SoftwareControlValueChanged != null)
                        SoftwareControlValueChanged(this);
                    _settings.SetValue(new Identifier(Identifier, "value").ToString(), value.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        public void SetDefault()
        {
            ControlMode = ControlMode.Default;
        }

        public float MinSoftwareValue { get; private set; }

        public float MaxSoftwareValue { get; private set; }

        public void SetSoftware(float value)
        {
            ControlMode = ControlMode.Software;
            SoftwareValue = value;
        }

        internal event ControlEventHandler ControlModeChanged;
        internal event ControlEventHandler SoftwareControlValueChanged;
    }
}
