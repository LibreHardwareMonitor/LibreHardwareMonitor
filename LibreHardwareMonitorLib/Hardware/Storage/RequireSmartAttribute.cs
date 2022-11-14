// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;

namespace LibreHardwareMonitor.Hardware.Storage;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
internal class RequireSmartAttribute : Attribute
{
    public RequireSmartAttribute(byte attributeId)
    {
        AttributeId = attributeId;
    }

    public byte AttributeId { get; }
}