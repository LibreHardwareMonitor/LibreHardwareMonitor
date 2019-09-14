// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Globalization;

namespace LibreHardwareMonitor.Hardware
{
    internal class Parameter : IParameter
    {
        private readonly ISensor _sensor;
        private ParameterDescription _description;
        private float _value;
        private bool _isDefault;
        private readonly ISettings _settings;

        public Parameter(ParameterDescription description, ISensor sensor, ISettings settings)
        {
            _sensor = sensor;
            _description = description;
            _settings = settings;
            _isDefault = !settings.Contains(Identifier.ToString());
            _value = description.DefaultValue;
            if (!_isDefault)
            {
                if (!float.TryParse(settings.GetValue(Identifier.ToString(), "0"), NumberStyles.Float, CultureInfo.InvariantCulture, out this._value))
                    this._value = description.DefaultValue;
            }
        }

        public ISensor Sensor
        {
            get
            {
                return _sensor;
            }
        }

        public Identifier Identifier
        {
            get
            {
                return new Identifier(_sensor.Identifier, "parameter", Name.Replace(" ", "").ToLowerInvariant());
            }
        }

        public string Name { get { return _description.Name; } }

        public string Description { get { return _description.Description; } }

        public float Value
        {
            get
            {
                return _value;
            }
            set
            {
                this._isDefault = false;
                this._value = value;
                this._settings.SetValue(Identifier.ToString(), value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public float DefaultValue
        {
            get { return _description.DefaultValue; }
        }

        public bool IsDefault
        {
            get { return _isDefault; }
            set
            {
                this._isDefault = value;
                if (value)
                {
                    _value = _description.DefaultValue;
                    _settings.Remove(Identifier.ToString());
                }
            }
        }

        public void Accept(IVisitor visitor)
        {
            if (visitor == null)
                throw new ArgumentNullException("visitor");
            visitor.VisitParameter(this);
        }

        public void Traverse(IVisitor visitor) { }
    }
}
