/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2009-2012 Michael Möller <mmoeller@openhardwaremonitor.org>
	
*/

using System;
using System.Collections.Generic;

namespace OpenHardwareMonitor.Hardware
{

  public enum SensorType {
    Voltage, // V
    Clock, // MHz
    Temperature, // °C
    Load, // %
    Frequency, // Hz
    Fan, // RPM
    Flow, // L/h
    Control, // %
    Level, // %
    Factor, // 1
    Power, // W
    Data, // GB = 2^30 Bytes    
    SmallData, // MB = 2^20 Bytes
    Throughput, // B/s
    }

  public struct SensorValue {
    private readonly float value;
    private readonly DateTime time;

    public SensorValue(float value, DateTime time) {
      this.value = value;
      this.time = time;
    }

    public float Value { get { return value; } }
    public DateTime Time { get { return time; } }
  }

  public interface ISensor : IElement {

    IHardware Hardware { get; }

    SensorType SensorType { get; }
    Identifier Identifier { get; }

    string Name { get; set; }
    int Index { get; }

    bool IsDefaultHidden { get; }

    IReadOnlyList<IParameter> Parameters { get; }

    float? Value { get; }
    float? Min { get; }
    float? Max { get; }

    void ResetMin();
    void ResetMax();

    IEnumerable<SensorValue> Values { get; }
    TimeSpan ValuesTimeWindow { get; set; }

    IControl Control { get; }
  }

}
