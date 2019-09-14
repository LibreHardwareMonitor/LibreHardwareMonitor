// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

namespace LibreHardwareMonitor.Hardware.Lpc
{
    internal interface ISuperIO
    {
        Chip Chip { get; }

        // get voltage, temperature, fan and control channel values
        float?[] Voltages { get; }
        float?[] Temperatures { get; }
        float?[] Fans { get; }
        float?[] Controls { get; }

        // set control value, null = auto
        void SetControl(int index, byte? value);

        // read and write GPIO
        byte? ReadGPIO(int index);
        void WriteGPIO(int index, byte value);

        string GetReport();

        void Update();
    }
}
