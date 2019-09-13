// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System.Drawing;
using System.Collections.Generic;
using OpenHardwareMonitor.Hardware;

namespace OpenHardwareMonitor.GUI
{
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
                case HardwareType.CPU:
                    image = Utilities.EmbeddedResources.GetImage("cpu.png");
                    break;
                case HardwareType.GpuNvidia:
                    image = Utilities.EmbeddedResources.GetImage("nvidia.png");
                    break;
                case HardwareType.GpuAti:
                    image = Utilities.EmbeddedResources.GetImage("ati.png");
                    break;
                case HardwareType.HDD:
                    image = Utilities.EmbeddedResources.GetImage("hdd.png");
                    break;
                case HardwareType.Heatmaster:
                    image = Utilities.EmbeddedResources.GetImage("bigng.png");
                    break;
                case HardwareType.Mainboard:
                    image = Utilities.EmbeddedResources.GetImage("mainboard.png");
                    break;
                case HardwareType.SuperIO:
                    image = Utilities.EmbeddedResources.GetImage("chip.png");
                    break;
                case HardwareType.TBalancer:
                    image = Utilities.EmbeddedResources.GetImage("bigng.png");
                    break;
                case HardwareType.RAM:
                    image = Utilities.EmbeddedResources.GetImage("ram.png");
                    break;
                case HardwareType.Aquacomputer:
                    image = Utilities.EmbeddedResources.GetImage("acicon.png");
                    break;
                case HardwareType.NIC:
                    image = Utilities.EmbeddedResources.GetImage("nic.png");
                    break;
                default:
                    image = new Bitmap(1, 1);
                    break;
            }
            _images.Add(hardwareType, image);
            return image;
        }
    }
}
