// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Collections.Generic;

namespace LibreHardwareMonitor.Hardware
{
    public enum ControlMode
    {
        Undefined,
        Software,
        Default,
        SoftwareCurve,
    }

    public interface IControl
    {
        ControlMode ControlMode { get; }
        
        ControlMode ActualControlMode { get; }

        Identifier Identifier { get; }

        float MaxSoftwareValue { get; }

        float MinSoftwareValue { get; }

        float SoftwareValue { get; }

        void SetDefault();

        void SetSoftware(float value);
        
        void SetSoftwareCurve(List<ISoftwareCurvePoint> points, ISensor sensor);
        SoftwareCurve GetSoftwareCurve();
        void NotifyHardwareAdded(List<IGroup> allHardware);
        void NotifyHardwareRemoved(IHardware hardware);
        void NotifyClosing();
    }

    public interface ISoftwareCurvePoint
    {
        float SensorValue { get; set; }
        float ControlValue { get; set; }
    }        
}
