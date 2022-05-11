// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc.EC
{
    public abstract class EmbeddedController : Hardware
    {
        private enum ECSensor
        {
            /// <summary>Chipset temperature [℃]</summary>
            TempChipset,
            /// <summary>CPU temperature [℃]</summary>
            TempCPU,
            /// motherboard temperature [℃]</summary>
            TempMB,
            /// <summary>"T_Sensor" temperature sensor reading [℃]</summary>
            TempTSensor,
            /// <summary>VRM temperature [℃]</summary>
            TempVrm,
            /// <summary>CPU Core voltage [mV]</summary>
            VoltageCPU,
            /// <summary>CPU_Opt fan [RPM]</summary>
            FanCPUOpt,
            /// <summary>VRM heat sink fan [RPM]</summary>
            FanVrmHS,
            /// <summary>Chipset fan [RPM]</summary>
            FanChipset,
            /// <summary>Water Pump [RPM]</summary>
            FanWaterPump,
            /// <summary>Water flow sensor reading [RPM]</summary>
            FanWaterFlow,
            /// <summary>CPU current [A]</summary>
            CurrCPU,
            /// <summary>"Water_In" temperature sensor reading [℃]</summary>
            TempWaterIn,
            /// <summary>"Water_Out" temperature sensor reading [℃]</summary>
            TempWaterOut,
            Max
        };

        private enum BoardFamily
        {
            Amd400,
            Amd500,
            Intel100,
            Intel600,
        }

        private struct BoardInfo
        {
            public BoardInfo(Model[] models, BoardFamily family, params ECSensor[] sensors)
            {
                Models = models;
                Family = family;
                Sensors = sensors;
            }

            public BoardInfo(Model model, BoardFamily family, params ECSensor[] sensors)
            {
                Models = new Model[] { model };
                Family = family;
                Sensors = sensors;
            }

            public Model[] Models { get; }
            public BoardFamily Family { get; }
            public ECSensor[] Sensors { get; }
        };

        private static readonly Dictionary<BoardFamily, Dictionary<ECSensor, EmbeddedControllerSource>> _knownSensors = new()
        {
            {
                BoardFamily.Amd400,
                new()  // no chipset fans in this generation
                {
                    { ECSensor.TempChipset, new EmbeddedControllerSource("Chipset", SensorType.Temperature, 0x003a) },
                    { ECSensor.TempCPU, new EmbeddedControllerSource("CPU", SensorType.Temperature, 0x003b) },
                    { ECSensor.TempMB, new EmbeddedControllerSource("Motherboard", SensorType.Temperature, 0x003c) },
                    { ECSensor.TempTSensor, new EmbeddedControllerSource("T Sensor", SensorType.Temperature, 0x003d, blank: -40) },
                    { ECSensor.TempVrm, new EmbeddedControllerSource("VRM", SensorType.Temperature, 0x003e) },
                    { ECSensor.VoltageCPU, new EmbeddedControllerSource("CPU Core", SensorType.Voltage, 0x00a2, 2, factor: 1e-3f) },
                    { ECSensor.FanCPUOpt, new EmbeddedControllerSource("CPU Optional Fan", SensorType.Fan, 0x00bc, 2) },
                    { ECSensor.FanVrmHS, new EmbeddedControllerSource("VRM Heat Sink Fan", SensorType.Fan, 0x00b2, 2) },
                    { ECSensor.FanWaterFlow, new EmbeddedControllerSource("Water flow", SensorType.Flow, 0x00b4, 2, factor: 1.0f / 42f * 60f) },
                    { ECSensor.CurrCPU, new EmbeddedControllerSource("CPU", SensorType.Current, 0x00f4) },
                    { ECSensor.TempWaterIn, new EmbeddedControllerSource("Water In", SensorType.Temperature, 0x010d, blank: -40) },
                    { ECSensor.TempWaterOut, new EmbeddedControllerSource("Water Out", SensorType.Temperature, 0x010b, blank: -40) },
                }
            },
            {
                BoardFamily.Amd500,
                new()
                {
                    { ECSensor.TempChipset, new EmbeddedControllerSource("Chipset", SensorType.Temperature, 0x003a) },
                    { ECSensor.TempCPU, new EmbeddedControllerSource("CPU", SensorType.Temperature, 0x003b) },
                    { ECSensor.TempMB, new EmbeddedControllerSource("Motherboard", SensorType.Temperature, 0x003c) },
                    { ECSensor.TempTSensor, new EmbeddedControllerSource("T Sensor", SensorType.Temperature, 0x003d, blank: -40) },
                    { ECSensor.TempVrm, new EmbeddedControllerSource("VRM", SensorType.Temperature, 0x003e) },
                    { ECSensor.VoltageCPU, new EmbeddedControllerSource("CPU Core", SensorType.Voltage, 0x00a2, 2, factor: 1e-3f) },
                    { ECSensor.FanCPUOpt, new EmbeddedControllerSource("CPU Optional Fan", SensorType.Fan, 0x00b0, 2) },
                    { ECSensor.FanVrmHS, new EmbeddedControllerSource("VRM Heat Sink Fan", SensorType.Fan, 0x00b2, 2) },
                    { ECSensor.FanChipset, new EmbeddedControllerSource("Chipset Fan", SensorType.Fan, 0x00b4, 2) },
                    // TODO: "why 42?" is a silly question, I know, but still, why? On the serious side, it might be 41.6(6)
                    { ECSensor.FanWaterFlow, new EmbeddedControllerSource("Water flow", SensorType.Flow, 0x00bc, 2, factor: 1.0f / 42f * 60f) },
                    { ECSensor.CurrCPU, new EmbeddedControllerSource("CPU", SensorType.Current, 0x00f4) },
                    { ECSensor.TempWaterIn, new EmbeddedControllerSource("Water In", SensorType.Temperature, 0x0100, blank: -40) },
                    { ECSensor.TempWaterOut, new EmbeddedControllerSource("Water Out", SensorType.Temperature, 0x0101, blank: -40) },
                }
            },
            {
                BoardFamily.Intel100,
                new() {
                    { ECSensor.TempChipset, new EmbeddedControllerSource("Chipset", SensorType.Temperature, 0x003a) },
                    { ECSensor.TempTSensor, new EmbeddedControllerSource("T Sensor", SensorType.Temperature, 0x003d, blank: -40) },
                    { ECSensor.FanWaterPump, new EmbeddedControllerSource("Water Pump", SensorType.Fan, 0x00bc, 2) },
                    { ECSensor.CurrCPU, new EmbeddedControllerSource("CPU", SensorType.Current, 0x00f4) },
                    { ECSensor.VoltageCPU, new EmbeddedControllerSource("CPU Core", SensorType.Voltage, 0x00a2, 2, factor: 1e-3f) },
                }
            },
            {
                BoardFamily.Intel600,
                new()
                {
                    { ECSensor.TempTSensor, new EmbeddedControllerSource("T Sensor", SensorType.Temperature, 0x003d, blank: -40) },
                    { ECSensor.TempVrm, new EmbeddedControllerSource("VRM", SensorType.Temperature, 0x003e) },
                }
            },
        };

        // If you are updating board information, please consider sharing your changes with the corresponding Linux driver.
        // You can do that at https://github.com/zeule/asus-ec-sensors or contribute directly to Linux HWMON.
        // If you are adding a new board, please share DSDT table for the board at https://github.com/zeule/asus-ec-sensors.
        // https://dortania.github.io/Getting-Started-With-ACPI/Manual/dump.html
        private static readonly BoardInfo[] _boards = new BoardInfo[]{
            new(Model.PRIME_X470_PRO, BoardFamily.Amd400,
               ECSensor.TempChipset, ECSensor.TempCPU, ECSensor.TempMB,
               ECSensor.TempVrm, ECSensor.TempVrm, ECSensor.FanCPUOpt,
               ECSensor.CurrCPU, ECSensor.VoltageCPU
            ),
            new (Model.PRIME_X570_PRO, BoardFamily.Amd500,
                ECSensor.TempChipset, ECSensor.TempCPU, ECSensor.TempMB,
                ECSensor.TempVrm, ECSensor.TempTSensor, ECSensor.FanChipset
            ),
            new(Model.PROART_X570_CREATOR_WIFI, BoardFamily.Amd500,
                ECSensor.TempChipset, ECSensor.TempCPU, ECSensor.TempMB,
                ECSensor.TempVrm, ECSensor.TempTSensor, ECSensor.FanCPUOpt, 
                ECSensor.CurrCPU, ECSensor.VoltageCPU
            ),
            new(Model.PRO_WS_X570_ACE, BoardFamily.Amd500,
                ECSensor.TempChipset, ECSensor.TempCPU, ECSensor.TempMB,
                ECSensor.TempVrm, ECSensor.FanChipset, ECSensor.CurrCPU, ECSensor.VoltageCPU
            ),
            new(new Model[] {Model.ROG_CROSSHAIR_VIII_HERO, Model.ROG_CROSSHAIR_VIII_HERO_WIFI, Model.ROG_CROSSHAIR_VIII_FORMULA }, BoardFamily.Amd500,
                ECSensor.TempChipset, ECSensor.TempCPU, ECSensor.TempMB,
                ECSensor.TempTSensor, ECSensor.TempVrm, ECSensor.TempWaterIn, ECSensor.TempWaterOut,
                ECSensor.FanCPUOpt, ECSensor.FanChipset, ECSensor.FanWaterFlow,
                ECSensor.CurrCPU, ECSensor.VoltageCPU
            ),
            new(Model.ROG_CROSSHAIR_VIII_DARK_HERO, BoardFamily.Amd500,
                ECSensor.TempChipset, ECSensor.TempCPU, ECSensor.TempMB,
                ECSensor.TempTSensor, ECSensor.TempVrm, ECSensor.TempWaterIn, ECSensor.TempWaterOut,
                ECSensor.FanCPUOpt, ECSensor.FanWaterFlow, ECSensor.CurrCPU, ECSensor.VoltageCPU
            ),
            new(Model.ROG_CROSSHAIR_VIII_IMPACT, BoardFamily.Amd500,
                ECSensor.TempChipset, ECSensor.TempCPU, ECSensor.TempMB,
                ECSensor.TempTSensor, ECSensor.TempVrm,
                ECSensor.FanChipset, ECSensor.CurrCPU, ECSensor.VoltageCPU
            ),
            new(Model.ROG_STRIX_B550_E_GAMING, BoardFamily.Amd500,
                ECSensor.TempChipset, ECSensor.TempCPU, ECSensor.TempMB,
                ECSensor.TempTSensor, ECSensor.TempVrm, ECSensor.FanCPUOpt
            ),
            new(Model.ROG_STRIX_B550_I_GAMING, BoardFamily.Amd500,
                ECSensor.TempChipset, ECSensor.TempCPU, ECSensor.TempMB,
                ECSensor.TempTSensor, ECSensor.TempVrm,
                ECSensor.FanVrmHS, ECSensor.CurrCPU, ECSensor.VoltageCPU
            ),
            new(Model.ROG_STRIX_X570_E_GAMING, BoardFamily.Amd500,
                ECSensor.TempChipset, ECSensor.TempCPU, ECSensor.TempMB,
                ECSensor.TempTSensor, ECSensor.TempVrm,
                ECSensor.FanChipset, ECSensor.CurrCPU, ECSensor.VoltageCPU
            ),
            new(Model.ROG_STRIX_X570_F_GAMING, BoardFamily.Amd500,
                ECSensor.TempChipset, ECSensor.TempCPU, ECSensor.TempMB,
                ECSensor.TempTSensor, ECSensor.FanChipset
            ),
            new(Model.ROG_STRIX_X570_I_GAMING, BoardFamily.Amd500,
                ECSensor.TempTSensor, ECSensor.FanVrmHS, ECSensor.FanChipset,
                ECSensor.CurrCPU, ECSensor.VoltageCPU
            ),
            new(Model.ROG_STRIX_Z690_A_GAMING_WIFI_D4, BoardFamily.Intel600,
                ECSensor.TempTSensor, ECSensor.TempVrm
            ),
            new(Model.Z170_A, BoardFamily.Intel100,
                ECSensor.TempTSensor, ECSensor.TempChipset, ECSensor.FanWaterPump, 
                ECSensor.CurrCPU, ECSensor.VoltageCPU
            ),
        };

        private readonly IReadOnlyList<EmbeddedControllerSource> _sources;
        private readonly List<Sensor> _sensors;
        private readonly ushort[] _registers;
        private readonly byte[] _data;

        protected EmbeddedController(IEnumerable<EmbeddedControllerSource> sources, ISettings settings) : base("Embedded Controller", new Identifier("lpc", "ec"), settings)
        {
            // sorting by address, which implies sorting by bank, for optimized EC access
            var sourcesList = sources.ToList();
            sourcesList.Sort((left, right) =>
            {
                return left.Register.CompareTo(right.Register);
            });
            _sources = sourcesList;
            var indices = new Dictionary<SensorType, int>();
            foreach (SensorType t in Enum.GetValues(typeof(SensorType)))
            {
                indices.Add(t, 0);
            }

            _sensors = new List<Sensor>();
            List<ushort> registers = new();
            foreach (EmbeddedControllerSource s in _sources)
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
            var boards = _boards.Where(b => b.Models.Contains(model)).ToList();
            if (boards.Count == 0)
                return null;
            if (boards.Count > 1)
                throw new MultipleBoardRecordsFoundException(model.ToString());
            BoardInfo board = boards[0];
            var sources = board.Sensors.Select(ecs => _knownSensors[board.Family][ecs]);

            return Environment.OSVersion.Platform switch
            {
                PlatformID.Win32NT => new WindowsEmbeddedController(sources, settings),
                _ => null
            };
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
                int val = _sources[si].Size switch
                {
                    1 => unchecked((sbyte)_data[readRegister]),
                    2 => unchecked((short)((_data[readRegister] << 8) + _data[readRegister + 1])),
                    _ => 0,
                };
                readRegister += _sources[si].Size;

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
            catch (IOException e)
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
            catch (IOException)
            {
                return false;
            }
        }

        public class IOException: System.IO.IOException {
            public IOException(string message): base($"ACPI embedded controller I/O error: {message}") { }
        }

        public class BadConfigurationException: System.Exception
        {
            public BadConfigurationException(string message): base(message) { }
        }

        public class MultipleBoardRecordsFoundException : BadConfigurationException
        {
            public MultipleBoardRecordsFoundException(string model) : base($"Multiple board records refer to the same model '{model}'") { }
        }
    }
}
