// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.

using System;
using System.Collections.Generic;

namespace LibreHardwareMonitor.Hardware.PresentMon;

internal sealed class PresentMonGroup : IGroup
{
    private readonly PresentMonHardware _hardware;

    public PresentMonGroup(ISettings settings, Func<uint> getProcessId)
    {
        if (!Software.OperatingSystem.IsUnix)
            _hardware = new PresentMonHardware(settings, getProcessId);

        Hardware = _hardware == null ? Array.Empty<IHardware>() : new IHardware[] { _hardware };
    }

    public IReadOnlyList<IHardware> Hardware { get; }

    public string GetReport() => _hardware?.GetReport() ?? "PresentMon requires Windows.";

    public void Close() => _hardware?.Close();
}
