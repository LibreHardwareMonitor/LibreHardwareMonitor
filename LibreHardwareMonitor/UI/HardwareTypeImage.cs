// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Drawing;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;

namespace LibreHardwareMonitor.UI;

public class HardwareTypeImage
{
    private readonly IDictionary<HardwareType, Image> _images = new Dictionary<HardwareType, Image>();

    private HardwareTypeImage() { }

    public static HardwareTypeImage Instance { get; } = new HardwareTypeImage();

    public Image GetImage(HardwareType hardwareType)
    {
        if (_images.TryGetValue(hardwareType, out Image image))
            return image;


        switch (hardwareType)
        {
            case HardwareType.Cpu:
                image = Utilities.EmbeddedResources.GetImage("cpu.png");
                break;
            case HardwareType.GpuNvidia:
                image = Utilities.EmbeddedResources.GetImage("nvidia.png");
                break;
            case HardwareType.GpuAmd:
                image = Utilities.EmbeddedResources.GetImage("amd.png");
                break;
            case HardwareType.GpuIntel:
                image = Utilities.EmbeddedResources.GetImage("intel.png");
                break;
            case HardwareType.Storage:
                image = Utilities.EmbeddedResources.GetImage("hdd.png");
                break;
            case HardwareType.Motherboard:
                image = Utilities.EmbeddedResources.GetImage("mainboard.png");
                break;
            case HardwareType.SuperIO:
            case HardwareType.EmbeddedController:
                image = Utilities.EmbeddedResources.GetImage("chip.png");
                break;
            case HardwareType.Memory:
                image = Utilities.EmbeddedResources.GetImage("ram.png");
                break;
            case HardwareType.Network:
                image = Utilities.EmbeddedResources.GetImage("nic.png");
                break;
            case HardwareType.Cooler:
                image = Utilities.EmbeddedResources.GetImage("fan.png");
                break;
            case HardwareType.Psu:
                image = Utilities.EmbeddedResources.GetImage("power-supply.png");
                break;
            case HardwareType.Battery:
                image = Utilities.EmbeddedResources.GetImage("battery.png");
                break;
            default:
                image = new Bitmap(1, 1);
                break;
        }
        _images.Add(hardwareType, image);
        return image;
    }
}