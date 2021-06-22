// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc.EC
{
    public abstract class EmbeddedController : Hardware
    {
        private readonly List<EmbeddedControllerSensor> _sensors;

        protected EmbeddedController(List<EmbeddedControllerSource> sources, ISettings settings) : base("Embedded Controller", new Identifier("lpc", "ec"), settings)
        {
            var indices = new Dictionary<SensorType, int>();
            foreach (SensorType t in Enum.GetValues(typeof(SensorType)))
            {
                indices.Add(t, 0);
            }

            _sensors = new List<EmbeddedControllerSensor>();
            foreach (EmbeddedControllerSource s in sources)
            {
                int index = indices[s.Type];
                indices[s.Type] = index + 1;
                _sensors.Add(new EmbeddedControllerSensor(s, index, this, settings));

                ActivateSensor(_sensors[_sensors.Count - 1]);
            }
        }

        public override HardwareType HardwareType => HardwareType.EmbeddedController;

        internal static EmbeddedController Create(Model model, ISettings settings)
        {
            var sources = new List<EmbeddedControllerSource>();

            switch (model)
            {
                case Model.ROG_STRIX_X570_E_GAMING:
                case Model.ROG_CROSSHAIR_VIII_HERO:
                {
                    sources.AddRange(new EmbeddedControllerSource[]
                    {
                        new("Chipset", 0x3A, SensorType.Temperature, ReadByte),
                        new("CPU", 0x3B, SensorType.Temperature, ReadByte),
                        new("Motherboard", 0x3C, SensorType.Temperature, ReadByte),
                        new("T Sensor", 0x3D, SensorType.Temperature, ReadByte),
                        new("VRM", 0x3E, SensorType.Temperature, ReadByte),
                        new("CPU Opt", 0xB0, SensorType.Fan, ReadWordBE),
                        new("Chipset", 0xB4, SensorType.Fan, ReadWordBE),
                        new("CPU", 0xF4, SensorType.Current, ReadByte)
                    });

                    break;
                }
            }

            switch (model)
            {
                case Model.ROG_CROSSHAIR_VIII_HERO:
                {
                    // TODO: "why 42?" is a silly question, I know, but still, why? On the serious side, it might be 41.6(6)
                    sources.Add(new EmbeddedControllerSource("Flow Rate", 0xBC, SensorType.Flow, (ecIO, port) => ecIO.ReadWordBE(port) / 42f * 60f));
                    break;
                }
            }

            if (sources.Count > 0)
            {
                return Environment.OSVersion.Platform switch
                {
                    PlatformID.Win32NT => new WindowsEmbeddedController(sources, settings),
                    _ => null
                };
            }

            return null;
        }

        public override void Update()
        {
            try
            {
                using IEmbeddedControllerIO embeddedControllerIO = AcquireIOInterface();

                foreach (EmbeddedControllerSensor sensor in _sensors)
                {
                    sensor.Update(embeddedControllerIO);
                }
            }
            catch (WindowsEmbeddedControllerIO.BusMutexLockingFailedException)
            {
                // just skip this update cycle?
            }
        }

        public override string GetReport()
        {
            StringBuilder r = new();

            r.AppendLine("EC " + GetType().Name);
            r.AppendLine("Embedded Controller Registers");
            r.AppendLine();
            r.AppendLine("      00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F");
            r.AppendLine();

            try
            {
                using IEmbeddedControllerIO embeddedControllerIO = AcquireIOInterface();

                for (int i = 0; i <= 0xF; ++i)
                {
                    r.Append(" ");
                    r.Append((i << 4).ToString("X2", CultureInfo.InvariantCulture));
                    r.Append("  ");
                    for (int j = 0; j <= 0xF; ++j)
                    {
                        byte address = (byte)(i << 4 | j);
                        r.Append(" ");
                        r.Append(embeddedControllerIO.ReadByte(address).ToString("X2", CultureInfo.InvariantCulture));
                    }

                    r.AppendLine();
                }
            }
            catch (WindowsEmbeddedControllerIO.BusMutexLockingFailedException e)
            {
                r.AppendLine(e.Message);
            }

            return r.ToString();
        }

        public static float ReadByte(IEmbeddedControllerIO ecIO, byte port)
        {
            return ecIO.ReadByte(port);
        }

        public static float ReadWordLE(IEmbeddedControllerIO ecIO, byte port)
        {
            return ecIO.ReadWordLE(port);
        }

        public static float ReadWordBE(IEmbeddedControllerIO ecIO, byte port)
        {
            return ecIO.ReadWordBE(port);
        }

        protected abstract IEmbeddedControllerIO AcquireIOInterface();

        private class EmbeddedControllerSensor : Sensor
        {
            readonly byte _port;
            readonly EmbeddedControllerReader _reader;

            public EmbeddedControllerSensor(EmbeddedControllerSource embeddedControllerSource, int index, EmbeddedController hardware, ISettings settings)
                : base(embeddedControllerSource.Name, index, embeddedControllerSource.Type, hardware, settings)
            {
                _port = embeddedControllerSource.Port;
                _reader = embeddedControllerSource.Reader;
            }

            public void Update(IEmbeddedControllerIO ecIO)
            {
                Value = _reader(ecIO, _port);
            }
        }
    }
}
