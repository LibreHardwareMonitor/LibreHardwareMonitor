// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware.Storage;

public abstract class NVMeInfo
{
    public ushort ControllerId { get; protected set; }

    public byte[] IEEE { get; protected set; }

    public int Index { get; protected set; }

    public string Model { get; protected set; }

    public uint NumberNamespaces { get; protected set; }

    public string Revision { get; protected set; }

    public string Serial { get; protected set; }

    public ushort SSVID { get; protected set; }

    public ulong TotalCapacity { get; protected set; }

    public ulong UnallocatedCapacity { get; protected set; }

    public ushort VID { get; protected set; }
}