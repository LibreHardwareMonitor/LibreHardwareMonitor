﻿// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System.Collections.Generic;

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc.EC;

public class ChromeOSEmbeddedController : EmbeddedController
{
    private const byte EC_MEMMAP_TEMP_SENSOR = 0x00;
    private const byte EC_MEMMAP_FAN = 0x10;
    private const byte EC_MEMMAP_TEMP_SENSOR_B = 0x18;
    private const byte EC_TEMP_SENSOR_ENTRIES = 16;
    private const byte EC_TEMP_SENSOR_B_ENTRIES = 8;
    private const byte EC_TEMP_SENSOR_NOT_PRESENT = 0xff;
    private const byte EC_TEMP_SENSOR_ERROR = 0xfe;
    private const byte EC_TEMP_SENSOR_NOT_POWERED = 0xfd;
    private const byte EC_TEMP_SENSOR_NOT_CALIBRATED = 0xfc;
    /*
     * The offset of temperature value stored in mapped memory.  This allows
     * reporting a temperature range of 200K to 454K = -73C to 181C.
     */
    private const byte EC_TEMP_SENSOR_OFFSET = 200;
    private const byte EC_FAN_SPEED_ENTRIES = 4;
    private const ushort EC_FAN_SPEED_NOT_PRESENT = 0xffff;

    public ChromeOSEmbeddedController(IEnumerable<EmbeddedControllerSource> sources, ISettings settings) : base(sources, settings)
    { }

    public static ChromeOSEmbeddedController Create(ISettings settings)
    {
        List<EmbeddedControllerSource> sources = new List<EmbeddedControllerSource>();

        using IEmbeddedControllerIO embeddedControllerIO = new ChromeOSEmbeddedControllerIO();

        // Copy the first 0x20 bytes of the EC memory map
        ushort[] registers = new ushort[0x20];
        byte[] data = new byte[0x20];
        for (ushort i = 0; i < registers.Length; ++i)
        {
            registers[i] = i;
        }

        embeddedControllerIO.Read(registers, data);

        for (int i = 0; i < EC_TEMP_SENSOR_ENTRIES; i++)
        {
            byte temp = data[EC_MEMMAP_TEMP_SENSOR + i];
            if (temp == EC_TEMP_SENSOR_NOT_PRESENT)
            {
                break;
            }

            sources.Add(new EmbeddedControllerSource("Temp " + (i + 1), SensorType.Temperature,
                                                     (ushort)(EC_MEMMAP_TEMP_SENSOR + i),
                                                     offset: EC_TEMP_SENSOR_OFFSET - 273,
                                                     blank: EC_TEMP_SENSOR_NOT_PRESENT));
        }

        for (int i = 0; i < EC_FAN_SPEED_ENTRIES; i++)
        {
            ushort fan = (ushort)(data[EC_MEMMAP_FAN + i * 2] | (data[EC_MEMMAP_FAN + i * 2 + 1] << 8));
            if (fan == EC_FAN_SPEED_NOT_PRESENT)
            {
                break;
            }

            sources.Add(new EmbeddedControllerSource("Fan " + (i + 1), SensorType.Fan,
                                                     (ushort)(EC_MEMMAP_FAN + i * 2), 2, blank: EC_FAN_SPEED_NOT_PRESENT,
                                                     littleEndian: true));
        }

        for (int i = 0; i < EC_TEMP_SENSOR_B_ENTRIES; i++)
        {
            byte temp = data[EC_MEMMAP_TEMP_SENSOR_B + i];
            if (temp == EC_TEMP_SENSOR_NOT_PRESENT)
            {
                break;
            }

            sources.Add(new EmbeddedControllerSource("Temp " + (i + 17), SensorType.Temperature,
                                                     (ushort)(EC_MEMMAP_TEMP_SENSOR_B + i),
                                                     offset: EC_TEMP_SENSOR_OFFSET - 273,
                                                     blank: EC_TEMP_SENSOR_NOT_PRESENT));
        }

        return new ChromeOSEmbeddedController(sources, settings);
    }

    protected override IEmbeddedControllerIO AcquireIOInterface()
    {
        return new ChromeOSEmbeddedControllerIO();
    }
}
