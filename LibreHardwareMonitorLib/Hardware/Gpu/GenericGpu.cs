// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using LibreHardwareMonitor.Hardware.Gpu.PowerMonitor;

namespace LibreHardwareMonitor.Hardware.Gpu;

public abstract class GenericGpu : Hardware
{
    readonly List<Hardware> _subHardware = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericGpu" /> class.
    /// </summary>
    /// <param name="name">Component name.</param>
    /// <param name="identifier">Identifier that will be assigned to the device. Based on <see cref="Identifier" /></param>
    /// <param name="settings">Additional settings passed by the <see cref="IComputer" />.</param>
    protected GenericGpu(string name, Identifier identifier, ISettings settings) : base(name, identifier, settings)
    {
        TryAddSubHardware();
    }

    /// <summary>
    /// Gets the device identifier.
    /// </summary>
    public abstract string DeviceId { get; }

    public override IHardware[] SubHardware => _subHardware.ToArray();

    public override void Close()
    {
        _subHardware.ForEach(h => h.Close());

        base.Close();
    }

    private void TryAddSubHardware()
    {
        var devices = WireViewPro2.TryFindDevices(_settings);

        devices.ForEach(wvp2 =>
        {
            if (wvp2.IsConnected)
            {
                _subHardware.Add(wvp2);
            }
        });
    }
}
