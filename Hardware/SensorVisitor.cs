// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;

namespace LibreHardwareMonitor.Hardware
{
    public class SensorVisitor : IVisitor
    {
        private readonly SensorEventHandler _handler;

        public SensorVisitor(SensorEventHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException("handler");
            _handler = handler;
        }

        public void VisitComputer(IComputer computer)
        {
            if (computer == null)
                throw new ArgumentNullException("computer");
            computer.Traverse(this);
        }

        public void VisitHardware(IHardware hardware)
        {
            if (hardware == null)
                throw new ArgumentNullException("hardware");
            hardware.Traverse(this);
        }

        public void VisitSensor(ISensor sensor)
        {
            _handler(sensor);
        }

        public void VisitParameter(IParameter parameter) { }
    }
}
