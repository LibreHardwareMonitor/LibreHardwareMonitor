// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Linq;

namespace LibreHardwareMonitor.Hardware;

internal class CompositeSensor : Sensor
{
    private readonly ISensor[] _components;
    private readonly Func<float, ISensor, float> _reducer;
    private readonly float _seedValue;

    public CompositeSensor
    (
        string name,
        int index,
        SensorType sensorType,
        Hardware hardware,
        ISettings settings,
        ISensor[] components,
        Func<float, ISensor, float> reducer,
        float seedValue = .0f)
        : base(name, index, sensorType, hardware, settings)
    {
        _components = components;
        _reducer = reducer;
        _seedValue = seedValue;
    }

    public override float? Value
    {
        get { return _components.Aggregate(_seedValue, _reducer); }
        set => throw new NotImplementedException();
    }
}