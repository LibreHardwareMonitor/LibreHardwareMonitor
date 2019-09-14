// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Collections.Generic;
using System.Linq;

namespace LibreHardwareMonitor.Hardware
{
    public abstract class Hardware : IHardware
    {
        private readonly Identifier _identifier;
        protected readonly string name;
        private string _customName;
        protected readonly ISettings settings;
        protected readonly HashSet<ISensor> active = new HashSet<ISensor>();

        public Hardware(string name, Identifier identifier, ISettings settings)
        {
            this.settings = settings;
            this.name = name;
            _identifier = identifier;
            _customName = settings.GetValue(new Identifier(Identifier, "name").ToString(), name);
        }

        public IHardware[] SubHardware
        {
            get { return new IHardware[0]; }
        }

        public virtual IHardware Parent
        {
            get { return null; }
        }

        public virtual ISensor[] Sensors
        {
            get { return active.ToArray(); }
        }

        protected virtual void ActivateSensor(ISensor sensor)
        {
            if (active.Add(sensor))
                SensorAdded?.Invoke(sensor);
        }

        protected virtual void DeactivateSensor(ISensor sensor)
        {
            if (active.Remove(sensor))
                SensorRemoved?.Invoke(sensor);
        }

        public string Name
        {
            get
            {
                return _customName;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    _customName = value;
                else
                    _customName = name;
                settings.SetValue(new Identifier(Identifier, "name").ToString(), _customName);
            }
        }

        public Identifier Identifier
        {
            get
            {
                return _identifier;
            }
        }

#pragma warning disable 67
        public event SensorEventHandler SensorAdded;
        public event SensorEventHandler SensorRemoved;
#pragma warning restore 67

        public abstract HardwareType HardwareType { get; }

        public virtual string GetReport()
        {
            return null;
        }

        public abstract void Update();

        public event HardwareEventHandler Closing;

        public virtual void Close()
        {
            if (Closing != null)
                Closing(this);
        }

        public void Accept(IVisitor visitor)
        {
            if (visitor == null)
                throw new ArgumentNullException("visitor");
            visitor.VisitHardware(this);
        }

        public virtual void Traverse(IVisitor visitor)
        {
            foreach (ISensor sensor in active)
                sensor.Accept(visitor);
        }
    }
}
