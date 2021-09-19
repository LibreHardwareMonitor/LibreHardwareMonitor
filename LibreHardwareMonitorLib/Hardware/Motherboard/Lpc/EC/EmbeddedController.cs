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
        private readonly IReadOnlyList<EmbeddedControllerSource> _sources;
        private readonly List<Sensor> _sensors;
        private readonly ushort[] _registers;
        private readonly byte[] _data;

        protected EmbeddedController(List<EmbeddedControllerSource> sources, ISettings settings) : base("Embedded Controller", new Identifier("lpc", "ec"), settings)
        {
            _sources = sources;
            var indices = new Dictionary<SensorType, int>();
            foreach (SensorType t in Enum.GetValues(typeof(SensorType)))
            {
                indices.Add(t, 0);
            }

            _sensors = new List<Sensor>();
            List<ushort> registers = new();
             foreach (EmbeddedControllerSource s in sources)
            {
                int index = indices[s.Type];
                indices[s.Type] = index + 1;
                _sensors.Add(new Sensor(s.Name, index, s.Type, this, settings));
                for (int i = 0; i < s.Size; ++i)
                {
                    registers.Add((ushort)(s.Register + i));
                }

                ActivateSensor(_sensors[_sensors.Count - 1]);
            }

            _registers = registers.ToArray();
            _data = new byte[_registers.Length];
        }

        public override HardwareType HardwareType => HardwareType.EmbeddedController;

        internal static EmbeddedController Create(Model model, ISettings settings)
        {
            var sources = new List<EmbeddedControllerSource>();

            switch (model)
            {
                case Model.ROG_STRIX_X570_E_GAMING:
                case Model.ROG_CROSSHAIR_VIII_HERO:
                case Model.ROG_CROSSHAIR_VIII_DARK_HERO:
                {
                    sources.AddRange(new EmbeddedControllerSource[]
                    {
                        new("Chipset", SensorType.Temperature, 0x003A, 1),
                        new("CPU", SensorType.Temperature, 0x003B, 1),
                        new("Motherboard", SensorType.Temperature, 0x003C, 1),
                        new("T Sensor", SensorType.Temperature, 0x003D, 1, blank: 0xD8),
                        new("VRM", SensorType.Temperature, 0x003E, 1),
                        new("CPU Opt", SensorType.Fan, 0x00B0, 2),
                        new("CPU", SensorType.Current, 0x00F4, 1)
                    });

                    break;
                }
            }

            switch (model)
            {
                case Model.ROG_STRIX_X570_E_GAMING:
                case Model.ROG_CROSSHAIR_VIII_HERO:
                {
                    sources.Add(new EmbeddedControllerSource("Chipset", SensorType.Fan, 0x00B4, 2));
                    break;
                }
            }

            switch (model)
            {
                case Model.ROG_CROSSHAIR_VIII_HERO:
                case Model.ROG_CROSSHAIR_VIII_DARK_HERO:
                {
                    // TODO: "why 42?" is a silly question, I know, but still, why? On the serious side, it might be 41.6(6)
                    sources.Add(new EmbeddedControllerSource("Flow Rate", SensorType.Flow, 0x00BC, 2, 1.0f / 42f * 60f));
                    sources.Add(new EmbeddedControllerSource("Water In", SensorType.Temperature, 0x0100, 1, blank: 0xD8));
                    sources.Add(new EmbeddedControllerSource("Water Out", SensorType.Temperature, 0x0101, 1, blank: 0xD8));
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
            if (!TryUpdateData())
            {
                // just skip this update cycle?
                return;
            }
            
            int readRegister = 0;
            for (int si = 0; si < _sensors.Count; ++si)
            {
                int val = 0;
                for (int i = 0; i < _sources[si].Size; ++i, ++readRegister)
                {
                    val = (val << 8) + _data[readRegister];
                }
                
                _sensors[si].Value = val != _sources[si].Blank ? val * _sources[si].Factor : null;
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
                ushort[] src = new ushort[0x100];
                byte[] data = new byte[0x100];
                for (ushort i = 0; i < src.Length; ++i)
                {
                    src[i] = i;
                }
                embeddedControllerIO.Read(src, data);
                for (int i = 0; i <= 0xF; ++i)
                {
                    r.Append(" ");
                    r.Append((i << 4).ToString("X2", CultureInfo.InvariantCulture));
                    r.Append("  ");
                    for (int j = 0; j <= 0xF; ++j)
                    {
                        byte address = (byte)(i << 4 | j);
                        r.Append(" ");
                        r.Append(data[address].ToString("X2", CultureInfo.InvariantCulture));
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

        protected abstract IEmbeddedControllerIO AcquireIOInterface();

        private bool TryUpdateData()
        {
            try
            {
                using IEmbeddedControllerIO embeddedControllerIO = AcquireIOInterface();
                embeddedControllerIO.Read(_registers, _data);
                return true;
            }
            catch (WindowsEmbeddedControllerIO.BusMutexLockingFailedException)
            {
                return false;
            }
        }
    }
}
