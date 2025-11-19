// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc.EC;

public class EmbeddedControllerSource(string name, SensorType type, ushort register, byte size = 1, float factor = 1.0f, float offset = 0.0f, int blank = int.MaxValue, bool isLittleEndian = false)
{
    public int Blank { get; } = blank;

    public float Factor { get; } = factor;

    public bool IsLittleEndian { get; } = isLittleEndian;

    public string Name { get; } = name;

    public float Offset { get; } = offset;

    public ushort Register { get; } = register;

    public byte Size { get; } = size;

    public SensorType Type { get; } = type;
}
