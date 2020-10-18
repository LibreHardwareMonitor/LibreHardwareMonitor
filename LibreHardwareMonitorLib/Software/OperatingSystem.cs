// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;

namespace LibreHardwareMonitor.Software
{
    public static class OperatingSystem
    {
        static OperatingSystem()
        {
            // The operating system doesn't change during execution so let's query it just one time.
            PlatformID platform = Environment.OSVersion.Platform;
            IsUnix = platform == PlatformID.Unix || platform == PlatformID.MacOSX;

            if (Environment.Is64BitOperatingSystem)
                Is64Bit = true;
        }

        public static bool Is64Bit { get; }

        public static bool IsUnix { get; }
    }
}
