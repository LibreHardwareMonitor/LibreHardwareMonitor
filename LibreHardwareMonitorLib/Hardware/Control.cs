// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace LibreHardwareMonitor.Hardware
{
    internal delegate void ControlEventHandler(Control control);

    internal class Control : IControl
    {
        private readonly ISettings _settings;
        private ISensor _parentSensor;
        private ControlMode _mode;
        private float _softwareValue;

        private float _softwareCurveValue;
        private bool _softwareCurveAttached;

        private string _sensorIdentifier;
        private bool _nonSoftwareCurve;
        private SoftwareCurve _softwareCurve;

        public Control
        (
            ISensor sensor,
            ISettings settings,
            float minSoftwareValue,
            float maxSoftwareValue)
        {
            Identifier = new Identifier(sensor.Identifier, "control");
            _settings = settings;
            _parentSensor = sensor;
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

        public ControlMode ControlMode
        {
            get
            {
                if (_mode == ControlMode.SoftwareCurve)
                {
                    if (!_softwareCurveAttached)
                    {
                        return ControlMode.Default;
                    }
                    return ControlMode.Software;
                }

                return _mode;
            }

            private set
            {
                DetachSoftwareCurve();

                if (_mode != value)
                {
                    _mode = value;
                    ControlModeChanged?.Invoke(this);
                    _settings.SetValue(new Identifier(Identifier, "mode").ToString(), ((int)_mode).ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        public ControlMode ActualControlMode
        {
            get
            {
                if (_mode == ControlMode.SoftwareCurve && !_softwareCurveAttached)
                {
                    return ControlMode.Default;
                }
                return _mode;
            }
        }
        
        public Identifier Identifier { get; }

        public float MaxSoftwareValue { get; }

        public float MinSoftwareValue { get; }

        public float SoftwareValue
        {
            get
            {
                return _mode == ControlMode.SoftwareCurve ? _softwareCurveValue : _softwareValue;
            }
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

        internal event ControlEventHandler ControlModeChanged;

        internal event ControlEventHandler SoftwareControlValueChanged;
        
        public void SetSoftwareCurve(List<ISoftwareCurvePoint> points, ISensor sensor)
        {
            _sensorIdentifier = null;
            _nonSoftwareCurve = false;

            ControlMode = ControlMode.SoftwareCurve;
            var softwareCurve = new SoftwareCurve(points, sensor);
            AttachSoftwareCurve(softwareCurve);

            _settings.SetValue(new Identifier(Identifier,
                    "curveValue").ToString(),
                    softwareCurve.ToString());

            Debug.WriteLine("softwareCurve.ToString(): " + softwareCurve.ToString());
        }

        public SoftwareCurve GetSoftwareCurve()
        {
            return _softwareCurve;
        }
        
        public void NotifyHardwareAdded(List<IGroup> allHardware)
        {
            if (_nonSoftwareCurve || _softwareCurve != null)
                return;

            if (_sensorIdentifier == null)
            {
                if (!SoftwareCurve.TryParse(_settings.GetValue(
                        new Identifier(Identifier, "curveValue").ToString(), ""),
                    out _sensorIdentifier))
                {
                    _nonSoftwareCurve = true;
                    return;
                }
            }

            foreach (var group in allHardware)
                foreach (var hardware in group.Hardware)
                    HardwareAdded(hardware);
        }

        private void HardwareAdded(IHardware hardware)
        {
            hardware.SensorAdded += SensorAdded;

            foreach (ISensor sensor in hardware.Sensors)
                SensorAdded(sensor);

            foreach (IHardware subHardware in hardware.SubHardware)
                HardwareAdded(subHardware);
        }

        private void SensorAdded(ISensor sensor)
        {
            if (_softwareCurve != null) return;

            if (sensor.Identifier.ToString() == _sensorIdentifier)
            {
                if (!SoftwareCurve.TryParse(_settings.GetValue(
                    new Identifier(Identifier, "curveValue").ToString(), ""),
                    out List<ISoftwareCurvePoint> points))
                {
                    return;
                }
                _softwareCurve = new SoftwareCurve(points, sensor);
                Debug.WriteLine("hardware added software curve created");
                if (_mode == ControlMode.SoftwareCurve)
                    AttachSoftwareCurve(_softwareCurve);
            }
        }
    
        public void NotifyHardwareRemoved(IHardware hardware)
        {
            if (_softwareCurve == null)
                return;

            Debug.WriteLine("notify hardware removed");

            foreach (ISensor sensor in hardware.Sensors)
                if (sensor.Identifier.ToString() == _sensorIdentifier)
                {
                    NotifyClosing();
                }

            foreach (IHardware subHardware in hardware.SubHardware)
                NotifyHardwareRemoved(subHardware);
        }

        public void NotifyClosing()
        {
            if (_softwareCurve == null)
                return;

            DetachSoftwareCurve();
            _softwareCurve.Dispose();
            _softwareCurve = null;
            ControlModeChanged?.Invoke(this);
            Debug.WriteLine("closing");
        }
    
        private void AttachSoftwareCurve(SoftwareCurve newCurve)
        {
            if (_softwareCurveAttached || _softwareCurve != null) DetachSoftwareCurve();

            _softwareCurve = newCurve;
            //this.softwareCurve.Sensor.Hardware.SensorRemoved += SensorRemoved;
            //this.parentSensor.Hardware.SensorRemoved += SensorRemoved;
            _softwareCurve.SoftwareCurveValueChanged += this.HandleSoftwareCurveValueChange;
            _softwareCurve.SoftwareCurveAbort += this.HandleSoftwareCurveAbort;
            _softwareCurve.Start();
            _softwareCurveAttached = true;
            Debug.WriteLine("attaching softwarecurve");
        }

        private void DetachSoftwareCurve()
        {
            if (!_softwareCurveAttached || _softwareCurve == null) return;

            _softwareCurve.Stop();
            //this.softwareCurve.Sensor.Hardware.SensorRemoved -= SensorRemoved;
            //this.parentSensor.Hardware.SensorRemoved -= SensorRemoved;
            _softwareCurve.SoftwareCurveValueChanged -= this.HandleSoftwareCurveValueChange;
            _softwareCurve.SoftwareCurveAbort -= this.HandleSoftwareCurveAbort;
            _softwareCurveAttached = false;
            Debug.WriteLine("detaching softwarecurve");
        }

        private void HandleSoftwareCurveValueChange(SoftwareCurve softwareCurve)
        {
            _softwareCurveValue = softwareCurve.Value;
            Debug.WriteLine("setting value from software curve: " + softwareCurve.Value);
            SoftwareControlValueChanged?.Invoke(this);
        }

        private void HandleSoftwareCurveAbort(SoftwareCurve softwareCurve)
        {
            DetachSoftwareCurve();
            ControlModeChanged?.Invoke(this); // until softwarecurve is started again, get value of ControlMode is Default
            Debug.WriteLine("softwarecurve abort!");
        }
    } 
}
