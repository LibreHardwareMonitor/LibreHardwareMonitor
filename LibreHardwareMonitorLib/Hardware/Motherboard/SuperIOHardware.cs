// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using LibreHardwareMonitor.Hardware.Motherboard.Lpc;

namespace LibreHardwareMonitor.Hardware.Motherboard
{
    internal sealed class SuperIOHardware : Hardware
    {
        private readonly List<Sensor> _controls = new List<Sensor>();
        private readonly List<Sensor> _fans = new List<Sensor>();
        private readonly Motherboard _motherboard;

        private readonly UpdateDelegate _postUpdate;
        private readonly ReadValueDelegate _readControl;
        private readonly ReadValueDelegate _readFan;
        private readonly ReadValueDelegate _readTemperature;
        private readonly ReadValueDelegate _readVoltage;

        private readonly ISuperIO _superIO;
        private readonly List<Sensor> _temperatures = new List<Sensor>();
        private readonly List<Sensor> _voltages = new List<Sensor>();

        public SuperIOHardware(Motherboard motherboard, ISuperIO superIO, Manufacturer manufacturer, Model model, ISettings settings)
            : base(ChipName.GetName(superIO.Chip), new Identifier("lpc", superIO.Chip.ToString().ToLowerInvariant()), settings)
        {
            _motherboard = motherboard;
            _superIO = superIO;

            GetBoardSpecificConfiguration(superIO,
                                          manufacturer,
                                          model,
                                          out IList<Voltage> v,
                                          out IList<Temperature> t,
                                          out IList<Fan> f,
                                          out IList<Ctrl> c,
                                          out _readVoltage,
                                          out _readTemperature,
                                          out _readFan,
                                          out _readControl,
                                          out _postUpdate,
                                          out _);

            CreateVoltageSensors(superIO, settings, v);
            CreateTemperatureSensors(superIO, settings, t);
            CreateFanSensors(superIO, settings, f);
            CreateControlSensors(superIO, settings, c);
        }

        public override HardwareType HardwareType
        {
            get { return HardwareType.SuperIO; }
        }

        public override IHardware Parent
        {
            get { return _motherboard; }
        }

        private void CreateControlSensors(ISuperIO superIO, ISettings settings, IList<Ctrl> c)
        {
            foreach (Ctrl ctrl in c)
            {
                int index = ctrl.Index;
                if (index < superIO.Controls.Length)
                {
                    Sensor sensor = new Sensor(ctrl.Name, index, SensorType.Control, this, settings);
                    Control control = new Control(sensor, settings, 0, 100);
                    control.ControlModeChanged += cc =>
                    {
                        switch (cc.ControlMode)
                        {
                            case ControlMode.Undefined:
                            {
                                return;
                            }
                            case ControlMode.Default:
                            {
                                superIO.SetControl(index, null);
                                break;
                            }
                            case ControlMode.Software:
                            {
                                superIO.SetControl(index, (byte)(cc.SoftwareValue * 2.55));
                                break;
                            }
                            default:
                            {
                                return;
                            }
                        }
                    };

                    control.SoftwareControlValueChanged += cc =>
                    {
                        if (cc.ControlMode == ControlMode.Software)
                            superIO.SetControl(index, (byte)(cc.SoftwareValue * 2.55));
                    };

                    switch (control.ControlMode)
                    {
                        case ControlMode.Undefined:
                        {
                            break;
                        }
                        case ControlMode.Default:
                        {
                            superIO.SetControl(index, null);

                            break;
                        }
                        case ControlMode.Software:
                        {
                            superIO.SetControl(index, (byte)(control.SoftwareValue * 2.55));

                            break;
                        }
                    }

                    sensor.Control = control;
                    _controls.Add(sensor);
                    ActivateSensor(sensor);
                }
            }
        }

        private void CreateFanSensors(ISuperIO superIO, ISettings settings, IList<Fan> f)
        {
            foreach (Fan fan in f)
            {
                if (fan.Index < superIO.Fans.Length)
                {
                    Sensor sensor = new Sensor(fan.Name, fan.Index, SensorType.Fan, this, settings);
                    _fans.Add(sensor);
                }
            }
        }

        private void CreateTemperatureSensors(ISuperIO superIO, ISettings settings, IList<Temperature> t)
        {
            foreach (Temperature temperature in t)
            {
                if (temperature.Index < superIO.Temperatures.Length)
                {
                    Sensor sensor = new Sensor(temperature.Name,
                                               temperature.Index,
                                               SensorType.Temperature,
                                               this,
                                               new[] { new ParameterDescription("Offset [°C]", "Temperature offset.", 0) },
                                               settings);

                    _temperatures.Add(sensor);
                }
            }
        }

        private void CreateVoltageSensors(ISuperIO superIO, ISettings settings, IList<Voltage> v)
        {
            const string formula = "Voltage = value + (value - Vf) * Ri / Rf.";
            foreach (Voltage voltage in v)
            {
                if (voltage.Index < superIO.Voltages.Length)
                {
                    Sensor sensor = new Sensor(voltage.Name,
                                               voltage.Index,
                                               voltage.Hidden,
                                               SensorType.Voltage,
                                               this,
                                               new[]
                                               {
                                                   new ParameterDescription("Ri [kΩ]", "Input resistance.\n" + formula, voltage.Ri),
                                                   new ParameterDescription("Rf [kΩ]", "Reference resistance.\n" + formula, voltage.Rf),
                                                   new ParameterDescription("Vf [V]", "Reference voltage.\n" + formula, voltage.Vf)
                                               },
                                               settings);

                    _voltages.Add(sensor);
                }
            }
        }

        private static void GetBoardSpecificConfiguration
        (
            ISuperIO superIO,
            Manufacturer manufacturer,
            Model model,
            out IList<Voltage> v,
            out IList<Temperature> t,
            out IList<Fan> f,
            out IList<Ctrl> c,
            out ReadValueDelegate readVoltage,
            out ReadValueDelegate readTemperature,
            out ReadValueDelegate readFan,
            out ReadValueDelegate readControl,
            out UpdateDelegate postUpdate,
            out Mutex mutex)
        {
            readVoltage = index => superIO.Voltages[index];
            readTemperature = index => superIO.Temperatures[index];
            readFan = index => superIO.Fans[index];
            readControl = index => superIO.Controls[index];

            postUpdate = () => { };
            mutex = null;

            v = new List<Voltage>();
            t = new List<Temperature>();
            f = new List<Fan>();
            c = new List<Ctrl>();

            switch (superIO.Chip)
            {
                case Chip.IT8705F:
                case Chip.IT8712F:
                case Chip.IT8716F:
                case Chip.IT8718F:
                case Chip.IT8720F:
                case Chip.IT8726F:
                {
                    GetIteConfigurationsA(superIO, manufacturer, model, v, t, f, c, ref readFan, ref postUpdate, ref mutex);

                    break;
                }
                case Chip.IT8620E:
                {
                    GetIteConfigurationsB(superIO, manufacturer, model, v, t, f, c);

                    break;
                }
                case Chip.IT8628E:
                case Chip.IT8655E:
                case Chip.IT8665E:
                case Chip.IT8686E:
                case Chip.IT8688E:
                case Chip.IT8721F:
                case Chip.IT8728F:
                case Chip.IT8771E:
                case Chip.IT8772E:
                {
                    GetIteConfigurationsB(superIO, manufacturer, model, v, t, f, c);

                    break;
                }
                case Chip.IT879XE:
                {
                    GetIteConfigurationsC(superIO, manufacturer, model, v, t, f, c);

                    break;
                }
                case Chip.F71858:
                {
                    v.Add(new Voltage("VCC3V", 0, 150, 150));
                    v.Add(new Voltage("VSB3V", 1, 150, 150));
                    v.Add(new Voltage("Battery", 2, 150, 150));

                    for (int i = 0; i < superIO.Temperatures.Length; i++)
                        t.Add(new Temperature("Temperature #" + (i + 1), i));

                    for (int i = 0; i < superIO.Fans.Length; i++)
                        f.Add(new Fan("Fan #" + (i + 1), i));

                    break;
                }
                case Chip.F71808E:
                case Chip.F71862:
                case Chip.F71869:
                case Chip.F71869A:
                case Chip.F71882:
                case Chip.F71889AD:
                case Chip.F71889ED:
                case Chip.F71889F:
                {
                    GetFintekConfiguration(superIO, manufacturer, model, v, t, f, c);

                    break;
                }
                case Chip.W83627EHF:
                {
                    GetWinbondConfigurationEhf(manufacturer, model, v, t, f);

                    break;
                }
                case Chip.W83627DHG:
                case Chip.W83627DHGP:
                case Chip.W83667HG:
                case Chip.W83667HGB:
                {
                    GetWinbondConfigurationHg(manufacturer, model, v, t, f);

                    break;
                }
                case Chip.W83627HF:
                case Chip.W83627THF:
                case Chip.W83687THF:
                {
                    v.Add(new Voltage("Vcore", 0));
                    v.Add(new Voltage("Voltage #2", 1, true));
                    v.Add(new Voltage("Voltage #3", 2, true));
                    v.Add(new Voltage("AVCC", 3, 34, 51));
                    v.Add(new Voltage("Voltage #5", 4, true));
                    v.Add(new Voltage("+5VSB", 5, 34, 51));
                    v.Add(new Voltage("VBat", 6));
                    t.Add(new Temperature("CPU", 0));
                    t.Add(new Temperature("Auxiliary", 1));
                    t.Add(new Temperature("System", 2));
                    f.Add(new Fan("System Fan", 0));
                    f.Add(new Fan("CPU Fan", 1));
                    f.Add(new Fan("Auxiliary Fan", 2));

                    break;
                }
                case Chip.NCT6771F:
                case Chip.NCT6776F:
                {
                    GetNuvotonConfigurationF(superIO, manufacturer, model, v, t, f, c);

                    break;
                }
                case Chip.NCT610XD:
                {
                    v.Add(new Voltage("Vcore", 0));
                    v.Add(new Voltage("Voltage #0", 1, true));
                    v.Add(new Voltage("AVCC", 2, 34, 34));
                    v.Add(new Voltage("+3.3V", 3, 34, 34));
                    v.Add(new Voltage("Voltage #1", 4, true));
                    v.Add(new Voltage("Voltage #2", 5, true));
                    v.Add(new Voltage("Reserved", 6, true));
                    v.Add(new Voltage("3VSB", 7, 34, 34));
                    v.Add(new Voltage("VBat", 8, 34, 34));
                    v.Add(new Voltage("Voltage #10", 9, true));
                    t.Add(new Temperature("System", 1));
                    t.Add(new Temperature("CPU Core", 2));
                    t.Add(new Temperature("Auxiliary", 3));

                    for (int i = 0; i < superIO.Fans.Length; i++)
                        f.Add(new Fan("Fan #" + (i + 1), i));

                    for (int i = 0; i < superIO.Controls.Length; i++)
                        c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                    break;
                }
                case Chip.NCT6779D:
                case Chip.NCT6791D:
                case Chip.NCT6792D:
                case Chip.NCT6792DA:
                case Chip.NCT6793D:
                case Chip.NCT6795D:
                case Chip.NCT6796D:
                case Chip.NCT6796DR:
                case Chip.NCT6797D:
                case Chip.NCT6798D:
                {
                    GetNuvotonConfigurationD(superIO, manufacturer, model, v, t, f, c);

                    break;
                }
                case Chip.NCT6687D:
                {
                    v.Add(new Voltage("+12V", 0));
                    v.Add(new Voltage("+5V", 1));
                    v.Add(new Voltage("Vcore", 2));
                    v.Add(new Voltage("Voltage #1", 3));
                    v.Add(new Voltage("DIMM", 4));
                    v.Add(new Voltage("CPU I/O", 5));
                    v.Add(new Voltage("CPU SA", 6));
                    v.Add(new Voltage("Voltage #2", 7));
                    v.Add(new Voltage("AVCC3", 8));
                    v.Add(new Voltage("VTT", 9));
                    v.Add(new Voltage("VRef", 10));
                    v.Add(new Voltage("VSB", 11));
                    v.Add(new Voltage("AVSB", 12));
                    v.Add(new Voltage("VBat", 13));

                    t.Add(new Temperature("CPU", 0));
                    t.Add(new Temperature("System", 1));
                    t.Add(new Temperature("VRM MOS", 2));
                    t.Add(new Temperature("PCH", 3));
                    t.Add(new Temperature("CPU Socket", 4));
                    t.Add(new Temperature("PCIe x1", 5));
                    t.Add(new Temperature("M2_1", 6));
                        
                    f.Add(new Fan("CPU Fan", 0));
                    f.Add(new Fan("Pump Fan", 1));
                    f.Add(new Fan("System Fan #1", 2));
                    f.Add(new Fan("System Fan #2", 3));
                    f.Add(new Fan("System Fan #3", 4));
                    f.Add(new Fan("System Fan #4", 5));
                    f.Add(new Fan("System Fan #5", 6));
                    f.Add(new Fan("System Fan #6", 7));

                    c.Add(new Ctrl("CPU Fan", 0));
                    c.Add(new Ctrl("Pump Fan", 1));
                    c.Add(new Ctrl("System Fan #1", 2));
                    c.Add(new Ctrl("System Fan #2", 3));
                    c.Add(new Ctrl("System Fan #3", 4));
                    c.Add(new Ctrl("System Fan #4", 5));
                    c.Add(new Ctrl("System Fan #5", 6));
                    c.Add(new Ctrl("System Fan #6", 7));

                    break;
                }
                default:
                {
                    GetDefaultConfiguration(superIO, v, t, f, c);

                    break;
                }
            }
        }

        private static void GetDefaultConfiguration(ISuperIO superIO, ICollection<Voltage> v, ICollection<Temperature> t, ICollection<Fan> f, ICollection<Ctrl> c)
        {
            for (int i = 0; i < superIO.Voltages.Length; i++)
                v.Add(new Voltage("Voltage #" + (i + 1), i, true));

            for (int i = 0; i < superIO.Temperatures.Length; i++)
                t.Add(new Temperature("Temperature #" + (i + 1), i));

            for (int i = 0; i < superIO.Fans.Length; i++)
                f.Add(new Fan("Fan #" + (i + 1), i));

            for (int i = 0; i < superIO.Controls.Length; i++)
                c.Add(new Ctrl("Fan Control #" + (i + 1), i));
        }

        private static void GetIteConfigurationsA
        (
            ISuperIO superIO,
            Manufacturer manufacturer,
            Model model,
            IList<Voltage> v,
            IList<Temperature> t,
            IList<Fan> f,
            ICollection<Ctrl> c,
            ref ReadValueDelegate readFan,
            ref UpdateDelegate postUpdate,
            ref Mutex mutex)
        {
            switch (manufacturer)
            {
                case Manufacturer.ASUS:
                {
                    switch (model)
                    {
                        case Model.CROSSHAIR_III_FORMULA: // IT8720F
                        {
                            v.Add(new Voltage("VBat", 8));
                            t.Add(new Temperature("CPU", 0));

                            for (int i = 0; i < superIO.Fans.Length; i++)
                                f.Add(new Fan("Fan #" + (i + 1), i));

                            break;
                        }
                        case Model.M2N_SLI_Deluxe:
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("+3.3V", 1));
                            v.Add(new Voltage("+5V", 3, 6.8f, 10));
                            v.Add(new Voltage("+12V", 4, 30, 10));
                            v.Add(new Voltage("+5VSB", 7, 6.8f, 10));
                            v.Add(new Voltage("VBat", 8));
                            t.Add(new Temperature("CPU", 0));
                            t.Add(new Temperature("Motherboard", 1));
                            f.Add(new Fan("CPU Fan", 0));
                            f.Add(new Fan("Chassis Fan #1", 1));
                            f.Add(new Fan("Power Fan", 2));

                            break;
                        }
                        case Model.M4A79XTD_EVO: // IT8720F
                        {
                            v.Add(new Voltage("+5V", 3, 6.8f, 10));
                            v.Add(new Voltage("VBat", 8));
                            t.Add(new Temperature("CPU", 0));
                            t.Add(new Temperature("Motherboard", 1));
                            f.Add(new Fan("CPU Fan", 0));
                            f.Add(new Fan("Chassis Fan #1", 1));
                            f.Add(new Fan("Chassis Fan #2", 2));

                            break;
                        }
                        case Model.PRIME_X370_PRO: // IT8665E
                        case Model.TUF_X470_PLUS_GAMING:
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("SB 2.5V", 1));
                            v.Add(new Voltage("+12V", 2, 5, 1));
                            v.Add(new Voltage("+5V", 3, 1.5f, 1));
                            v.Add(new Voltage("Voltage #4", 4, true));
                            v.Add(new Voltage("Voltage #6", 5, true));
                            v.Add(new Voltage("Voltage #7", 6, true));
                            v.Add(new Voltage("+3.3V", 7, 10, 10));
                            v.Add(new Voltage("VBat", 8, 10, 10));
                            v.Add(new Voltage("Voltage #10", 9, true));
                            t.Add(new Temperature("CPU", 0));
                            t.Add(new Temperature("Motherboard", 1));
                            t.Add(new Temperature("PCH", 2));

                            for (int i = 3; i < superIO.Temperatures.Length; i++)
                                t.Add(new Temperature("Temperature #" + (i + 1), i));

                            f.Add(new Fan("CPU Fan", 0));

                            for (int i = 1; i < superIO.Fans.Length; i++)
                                f.Add(new Fan("Fan #" + (i + 1), i));

                            break;
                        }
                        case Model.ROG_ZENITH_EXTREME: // IT8665E
                        {
                            v.Add(new Voltage("Vcore", 0, 10, 10));
                            v.Add(new Voltage("DIMM AB", 1, 10, 10));
                            v.Add(new Voltage("+12V", 2, 5, 1));
                            v.Add(new Voltage("+5V", 3, 1.5f, 1));
                            v.Add(new Voltage("SB 1.05V", 4, 10, 10));
                            v.Add(new Voltage("DIMM CD", 5, 10, 10));
                            v.Add(new Voltage("1.8V PLL", 6, 10, 10));
                            v.Add(new Voltage("+3.3V", 7, 10, 10));
                            v.Add(new Voltage("VBat", 8, 10, 10));
                            t.Add(new Temperature("CPU", 0));
                            t.Add(new Temperature("Motherboard", 1));
                            t.Add(new Temperature("CPU Socket", 2));
                            t.Add(new Temperature("Temperature #4", 3));
                            t.Add(new Temperature("Temperature #5", 4));
                            t.Add(new Temperature("VRM", 5));

                            f.Add(new Fan("CPU Fan", 0));
                            f.Add(new Fan("Chassis Fan #1", 1));
                            f.Add(new Fan("Chassis Fan #2", 2));
                            f.Add(new Fan("High Amp Fan", 3));
                            f.Add(new Fan("Fan 5", 4));
                            f.Add(new Fan("Fan 6", 5));

                            for (int i = 0; i < superIO.Controls.Length; i++)
                                c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                            break;
                        }
                        default:
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("Voltage #2", 1, true));
                            v.Add(new Voltage("Voltage #3", 2, true));
                            v.Add(new Voltage("Voltage #4", 3, true));
                            v.Add(new Voltage("Voltage #5", 4, true));
                            v.Add(new Voltage("Voltage #6", 5, true));
                            v.Add(new Voltage("Voltage #7", 6, true));
                            v.Add(new Voltage("Voltage #8", 7, true));
                            v.Add(new Voltage("VBat", 8));

                            for (int i = 0; i < superIO.Temperatures.Length; i++)
                                t.Add(new Temperature("Temperature #" + (i + 1), i));

                            for (int i = 0; i < superIO.Fans.Length; i++)
                                f.Add(new Fan("Fan #" + (i + 1), i));

                            for (int i = 0; i < superIO.Controls.Length; i++)
                                c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                            break;
                        }
                    }

                    break;
                }
                case Manufacturer.ASRock:
                {
                    switch (model)
                    {
                        case Model.P55_Deluxe: // IT8720F
                        {
                            GetASRockConfiguration(superIO,
                                                   v,
                                                   t,
                                                   f,
                                                   ref readFan,
                                                   ref postUpdate,
                                                   out mutex);

                            break;
                        }
                        default:
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("Voltage #2", 1, true));
                            v.Add(new Voltage("Voltage #3", 2, true));
                            v.Add(new Voltage("Voltage #4", 3, true));
                            v.Add(new Voltage("Voltage #5", 4, true));
                            v.Add(new Voltage("Voltage #6", 5, true));
                            v.Add(new Voltage("Voltage #7", 6, true));
                            v.Add(new Voltage("Voltage #8", 7, true));
                            v.Add(new Voltage("VBat", 8));

                            for (int i = 0; i < superIO.Temperatures.Length; i++)
                                t.Add(new Temperature("Temperature #" + (i + 1), i));

                            for (int i = 0; i < superIO.Fans.Length; i++)
                                f.Add(new Fan("Fan #" + (i + 1), i));

                            break;
                        }
                    }

                    break;
                }
                case Manufacturer.DFI:
                {
                    switch (model)
                    {
                        case Model.LP_BI_P45_T2RS_Elite: // IT8718F
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("VTT", 1));
                            v.Add(new Voltage("+3.3V", 2));
                            v.Add(new Voltage("+5V", 3, 6.8f, 10));
                            v.Add(new Voltage("+12V", 4, 30, 10));
                            v.Add(new Voltage("NB Core", 5));
                            v.Add(new Voltage("DIMM", 6));
                            v.Add(new Voltage("+5VSB", 7, 6.8f, 10));
                            v.Add(new Voltage("VBat", 8));
                            t.Add(new Temperature("CPU", 0));
                            t.Add(new Temperature("System", 1));
                            t.Add(new Temperature("Chipset", 2));
                            f.Add(new Fan("Fan #1", 0));
                            f.Add(new Fan("Fan #2", 1));
                            f.Add(new Fan("Fan #3", 2));

                            break;
                        }
                        case Model.LP_DK_P55_T3EH9: // IT8720F
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("VTT", 1));
                            v.Add(new Voltage("+3.3V", 2));
                            v.Add(new Voltage("+5V", 3, 6.8f, 10));
                            v.Add(new Voltage("+12V", 4, 30, 10));
                            v.Add(new Voltage("CPU PLL", 5));
                            v.Add(new Voltage("DIMM", 6));
                            v.Add(new Voltage("+5VSB", 7, 6.8f, 10));
                            v.Add(new Voltage("VBat", 8));
                            t.Add(new Temperature("Chipset", 0));
                            t.Add(new Temperature("CPU PWM", 1));
                            t.Add(new Temperature("CPU", 2));
                            f.Add(new Fan("Fan #1", 0));
                            f.Add(new Fan("Fan #2", 1));
                            f.Add(new Fan("Fan #3", 2));

                            break;
                        }
                        default:
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("VTT", 1, true));
                            v.Add(new Voltage("+3.3V", 2, true));
                            v.Add(new Voltage("+5V", 3, 6.8f, 10, 0, true));
                            v.Add(new Voltage("+12V", 4, 30, 10, 0, true));
                            v.Add(new Voltage("Voltage #6", 5, true));
                            v.Add(new Voltage("DIMM", 6, true));
                            v.Add(new Voltage("+5VSB", 7, 6.8f, 10, 0, true));
                            v.Add(new Voltage("VBat", 8));

                            for (int i = 0; i < superIO.Temperatures.Length; i++)
                                t.Add(new Temperature("Temperature #" + (i + 1), i));

                            for (int i = 0; i < superIO.Fans.Length; i++)
                                f.Add(new Fan("Fan #" + (i + 1), i));

                            for (int i = 0; i < superIO.Controls.Length; i++)
                                c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                            break;
                        }
                    }

                    break;
                }
                case Manufacturer.Gigabyte:
                {
                    switch (model)
                    {
                        case Model._965P_S3: // IT8718F
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("DIMM", 1));
                            v.Add(new Voltage("+3.3V", 2));
                            v.Add(new Voltage("+5V", 3, 6.8f, 10));
                            v.Add(new Voltage("+12V", 7, 24.3f, 8.2f));
                            v.Add(new Voltage("VBat", 8));
                            t.Add(new Temperature("System", 0));
                            t.Add(new Temperature("CPU", 1));
                            f.Add(new Fan("CPU Fan", 0));
                            f.Add(new Fan("System Fan", 1));

                            break;
                        }
                        case Model.EP45_DS3R: // IT8718F
                        case Model.EP45_UD3R:
                        case Model.X38_DS5:
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("DIMM", 1));
                            v.Add(new Voltage("+3.3V", 2));
                            v.Add(new Voltage("+5V", 3, 6.8f, 10));
                            v.Add(new Voltage("+12V", 7, 24.3f, 8.2f));
                            v.Add(new Voltage("VBat", 8));
                            t.Add(new Temperature("System", 0));
                            t.Add(new Temperature("CPU", 1));
                            f.Add(new Fan("CPU Fan", 0));
                            f.Add(new Fan("System Fan #2", 1));
                            f.Add(new Fan("Power Fan", 2));
                            f.Add(new Fan("System Fan #1", 3));

                            break;
                        }
                        case Model.EX58_EXTREME: // IT8720F
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("DIMM", 1));
                            v.Add(new Voltage("+5V", 3, 6.8f, 10));
                            v.Add(new Voltage("VBat", 8));
                            t.Add(new Temperature("System", 0));
                            t.Add(new Temperature("CPU", 1));
                            t.Add(new Temperature("Northbridge", 2));
                            f.Add(new Fan("CPU Fan", 0));
                            f.Add(new Fan("System Fan #2", 1));
                            f.Add(new Fan("Power Fan", 2));
                            f.Add(new Fan("System Fan #1", 3));

                            break;
                        }
                        case Model.P35_DS3: // IT8718F
                        case Model.P35_DS3L: // IT8718F
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("DIMM", 1));
                            v.Add(new Voltage("+3.3V", 2));
                            v.Add(new Voltage("+5V", 3, 6.8f, 10));
                            v.Add(new Voltage("+12V", 7, 24.3f, 8.2f));
                            v.Add(new Voltage("VBat", 8));
                            t.Add(new Temperature("System", 0));
                            t.Add(new Temperature("CPU", 1));
                            f.Add(new Fan("CPU Fan", 0));
                            f.Add(new Fan("System Fan #1", 1));
                            f.Add(new Fan("System Fan #2", 2));
                            f.Add(new Fan("Power Fan", 3));

                            break;
                        }
                        case Model.P55_UD4: // IT8720F
                        case Model.P55A_UD3: // IT8720F
                        case Model.P55M_UD4: // IT8720F
                        case Model.H55_USB3: // IT8720F
                        case Model.EX58_UD3R: // IT8720F
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("DIMM", 1));
                            v.Add(new Voltage("+3.3V", 2));
                            v.Add(new Voltage("+5V", 3, 6.8f, 10));
                            v.Add(new Voltage("+12V", 5, 24.3f, 8.2f));
                            v.Add(new Voltage("VBat", 8));
                            t.Add(new Temperature("System", 0));
                            t.Add(new Temperature("CPU", 2));
                            f.Add(new Fan("CPU Fan", 0));
                            f.Add(new Fan("System Fan #2", 1));
                            f.Add(new Fan("Power Fan", 2));
                            f.Add(new Fan("System Fan #1", 3));

                            break;
                        }
                        case Model.H55N_USB3: // IT8720F
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("DIMM", 1));
                            v.Add(new Voltage("+3.3V", 2));
                            v.Add(new Voltage("+5V", 3, 6.8f, 10));
                            v.Add(new Voltage("+12V", 5, 24.3f, 8.2f));
                            v.Add(new Voltage("VBat", 8));
                            t.Add(new Temperature("System", 0));
                            t.Add(new Temperature("CPU", 2));
                            f.Add(new Fan("CPU Fan", 0));
                            f.Add(new Fan("System Fan", 1));

                            break;
                        }
                        case Model.G41M_COMBO: // IT8718F
                        case Model.G41MT_S2: // IT8718F
                        case Model.G41MT_S2P: // IT8718F
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("DIMM", 1));
                            v.Add(new Voltage("+3.3V", 2));
                            v.Add(new Voltage("+5V", 3, 6.8f, 10));
                            v.Add(new Voltage("+12V", 7, 24.3f, 8.2f));
                            v.Add(new Voltage("VBat", 8));
                            t.Add(new Temperature("CPU", 2));
                            f.Add(new Fan("CPU Fan", 0));
                            f.Add(new Fan("System Fan", 1));

                            break;
                        }
                        case Model._970A_UD3: // IT8720F
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("DIMM", 1));
                            v.Add(new Voltage("+3.3V", 2));
                            v.Add(new Voltage("+5V", 3, 6.8f, 10));
                            v.Add(new Voltage("+12V", 4, 24.3f, 8.2f));
                            v.Add(new Voltage("VBat", 8));
                            t.Add(new Temperature("System", 0));
                            t.Add(new Temperature("CPU", 1));
                            f.Add(new Fan("CPU Fan", 0));
                            f.Add(new Fan("System Fan #1", 1));
                            f.Add(new Fan("System Fan #2", 2));
                            f.Add(new Fan("Power Fan", 4));
                            c.Add(new Ctrl("PWM #1", 0));
                            c.Add(new Ctrl("PWM #2", 1));
                            c.Add(new Ctrl("PWM #3", 2));

                            break;
                        }
                        case Model.MA770T_UD3: // IT8720F
                        case Model.MA770T_UD3P: // IT8720F
                        case Model.MA790X_UD3P: // IT8720F
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("DIMM", 1));
                            v.Add(new Voltage("+3.3V", 2));
                            v.Add(new Voltage("+5V", 3, 6.8f, 10));
                            v.Add(new Voltage("+12V", 4, 24.3f, 8.2f));
                            v.Add(new Voltage("VBat", 8));
                            t.Add(new Temperature("System", 0));
                            t.Add(new Temperature("CPU", 1));
                            f.Add(new Fan("CPU Fan", 0));
                            f.Add(new Fan("System Fan #1", 1));
                            f.Add(new Fan("System Fan #2", 2));
                            f.Add(new Fan("Power Fan", 3));

                            break;
                        }
                        case Model.MA78LM_S2H: // IT8718F
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("DIMM", 1));
                            v.Add(new Voltage("+3.3V", 2));
                            v.Add(new Voltage("+5V", 3, 6.8f, 10));
                            v.Add(new Voltage("+12V", 4, 24.3f, 8.2f));
                            v.Add(new Voltage("VBat", 8));
                            t.Add(new Temperature("System", 0));
                            t.Add(new Temperature("CPU", 1));
                            t.Add(new Temperature("VRM", 2));
                            f.Add(new Fan("CPU Fan", 0));
                            f.Add(new Fan("System Fan #1", 1));
                            f.Add(new Fan("System Fan #2", 2));
                            f.Add(new Fan("Power Fan", 3));

                            break;
                        }
                        case Model.MA785GM_US2H: // IT8718F
                        case Model.MA785GMT_UD2H: // IT8718F
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("DIMM", 1));
                            v.Add(new Voltage("+3.3V", 2));
                            v.Add(new Voltage("+5V", 3, 6.8f, 10));
                            v.Add(new Voltage("+12V", 4, 24.3f, 8.2f));
                            v.Add(new Voltage("VBat", 8));
                            t.Add(new Temperature("System", 0));
                            t.Add(new Temperature("CPU", 1));
                            f.Add(new Fan("CPU Fan", 0));
                            f.Add(new Fan("System Fan", 1));
                            f.Add(new Fan("NB Fan", 2));

                            break;
                        }
                        case Model.X58A_UD3R: // IT8720F
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("DIMM", 1));
                            v.Add(new Voltage("+3.3V", 2));
                            v.Add(new Voltage("+5V", 3, 6.8f, 10));
                            v.Add(new Voltage("+12V", 5, 24.3f, 8.2f));
                            v.Add(new Voltage("VBat", 8));
                            t.Add(new Temperature("System", 0));
                            t.Add(new Temperature("CPU", 1));
                            t.Add(new Temperature("Northbridge", 2));
                            f.Add(new Fan("CPU Fan", 0));
                            f.Add(new Fan("System Fan #2", 1));
                            f.Add(new Fan("Power Fan", 2));
                            f.Add(new Fan("System Fan #1", 3));

                            break;
                        }
                        default:
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("DIMM", 1, true));
                            v.Add(new Voltage("+3.3V", 2, true));
                            v.Add(new Voltage("+5V", 3, 6.8f, 10, 0, true));
                            v.Add(new Voltage("Voltage #5", 4, true));
                            v.Add(new Voltage("Voltage #6", 5, true));
                            v.Add(new Voltage("Voltage #7", 6, true));
                            v.Add(new Voltage("Voltage #8", 7, true));
                            v.Add(new Voltage("VBat", 8));

                            for (int i = 0; i < superIO.Temperatures.Length; i++)
                                t.Add(new Temperature("Temperature #" + (i + 1), i));

                            for (int i = 0; i < superIO.Fans.Length; i++)
                                f.Add(new Fan("Fan #" + (i + 1), i));

                            for (int i = 0; i < superIO.Controls.Length; i++)
                                c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                            break;
                        }
                    }

                    break;
                }
                default:
                {
                    v.Add(new Voltage("Vcore", 0));
                    v.Add(new Voltage("Voltage #2", 1, true));
                    v.Add(new Voltage("Voltage #3", 2, true));
                    v.Add(new Voltage("Voltage #4", 3, true));
                    v.Add(new Voltage("Voltage #5", 4, true));
                    v.Add(new Voltage("Voltage #6", 5, true));
                    v.Add(new Voltage("Voltage #7", 6, true));
                    v.Add(new Voltage("Voltage #8", 7, true));
                    v.Add(new Voltage("VBat", 8));

                    for (int i = 0; i < superIO.Temperatures.Length; i++)
                        t.Add(new Temperature("Temperature #" + (i + 1), i));

                    for (int i = 0; i < superIO.Fans.Length; i++)
                        f.Add(new Fan("Fan #" + (i + 1), i));

                    for (int i = 0; i < superIO.Controls.Length; i++)
                        c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                    break;
                }
            }
        }

        private static void GetASRockConfiguration
        (
            ISuperIO superIO,
            IList<Voltage> v,
            IList<Temperature> t,
            IList<Fan> f,
            ref ReadValueDelegate readFan,
            ref UpdateDelegate postUpdate,
            out Mutex mutex)
        {
            v.Add(new Voltage("Vcore", 0));
            v.Add(new Voltage("+3.3V", 2));
            v.Add(new Voltage("+12V", 4, 30, 10));
            v.Add(new Voltage("+5V", 5, 6.8f, 10));
            v.Add(new Voltage("VBat", 8));
            t.Add(new Temperature("CPU", 0));
            t.Add(new Temperature("Motherboard", 1));
            f.Add(new Fan("CPU Fan", 0));
            f.Add(new Fan("Chassis Fan #1", 1));

            // this mutex is also used by the official ASRock tool
            mutex = new Mutex(false, "ASRockOCMark");

            bool exclusiveAccess = false;
            try
            {
                exclusiveAccess = mutex.WaitOne(10, false);
            }
            catch (AbandonedMutexException)
            { }
            catch (InvalidOperationException)
            { }

            // only read additional fans if we get exclusive access
            if (exclusiveAccess)
            {
                f.Add(new Fan("Chassis Fan #2", 2));
                f.Add(new Fan("Chassis Fan #3", 3));
                f.Add(new Fan("Power Fan", 4));

                readFan = index =>
                {
                    if (index < 2)
                    {
                        return superIO.Fans[index];
                    }

                    // get GPIO 80-87
                    byte? gpio = superIO.ReadGpio(7);
                    if (!gpio.HasValue)
                        return null;


                    // read the last 3 fans based on GPIO 83-85
                    int[] masks = { 0x05, 0x03, 0x06 };
                    return ((gpio.Value >> 3) & 0x07) == masks[index - 2] ? superIO.Fans[2] : null;
                };

                int fanIndex = 0;

                postUpdate = () =>
                {
                    // get GPIO 80-87
                    byte? gpio = superIO.ReadGpio(7);
                    if (!gpio.HasValue)
                        return;


                    // prepare the GPIO 83-85 for the next update
                    int[] masks = { 0x05, 0x03, 0x06 };
                    superIO.WriteGpio(7, (byte)((gpio.Value & 0xC7) | (masks[fanIndex] << 3)));
                    fanIndex = (fanIndex + 1) % 3;
                };
            }
        }

        private static void GetIteConfigurationsB(ISuperIO superIO, Manufacturer manufacturer, Model model, IList<Voltage> v, IList<Temperature> t, IList<Fan> f, IList<Ctrl> c)
        {
            switch (manufacturer)
            {
                case Manufacturer.ECS:
                {
                    switch (model)
                    {
                        case Model.A890GXM_A: // IT8721F
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("DIMM", 1));
                            v.Add(new Voltage("NB Voltage", 2));
                            v.Add(new Voltage("AVCC", 3, 10, 10));
                            // v.Add(new Voltage("DIMM", 6, true));
                            v.Add(new Voltage("3VSB", 7, 10, 10));
                            v.Add(new Voltage("VBat", 8, 10, 10));
                            t.Add(new Temperature("CPU", 0));
                            t.Add(new Temperature("System", 1));
                            t.Add(new Temperature("Northbridge", 2));
                            f.Add(new Fan("CPU Fan", 0));
                            f.Add(new Fan("System Fan", 1));
                            f.Add(new Fan("Power Fan", 2));

                            break;
                        }
                        default:
                        {
                            v.Add(new Voltage("Voltage #1", 0, true));
                            v.Add(new Voltage("Voltage #2", 1, true));
                            v.Add(new Voltage("Voltage #3", 2, true));
                            v.Add(new Voltage("AVCC", 3, 10, 10, 0, true));
                            v.Add(new Voltage("Voltage #5", 4, true));
                            v.Add(new Voltage("Voltage #6", 5, true));
                            v.Add(new Voltage("Voltage #7", 6, true));
                            v.Add(new Voltage("3VSB", 7, 10, 10, 0, true));
                            v.Add(new Voltage("VBat", 8, 10, 10));

                            for (int i = 0; i < superIO.Temperatures.Length; i++)
                                t.Add(new Temperature("Temperature #" + (i + 1), i));

                            for (int i = 0; i < superIO.Fans.Length; i++)
                                f.Add(new Fan("Fan #" + (i + 1), i));

                            for (int i = 0; i < superIO.Controls.Length; i++)
                                c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                            break;
                        }
                    }

                    break;
                }
                case Manufacturer.Gigabyte:
                {
                    switch (model)
                    {
                        case Model.H61M_DS2_REV_1_2: // IT8728F
                        case Model.H61M_USB3_B3_REV_2_0: // IT8728F
                        {
                            v.Add(new Voltage("VTT", 0));
                            v.Add(new Voltage("+12V", 2, 30.9f, 10));
                            v.Add(new Voltage("Vcore", 5));
                            v.Add(new Voltage("DIMM", 6));
                            v.Add(new Voltage("3VSB", 7, 10, 10));
                            v.Add(new Voltage("VBat", 8, 10, 10));
                            t.Add(new Temperature("System", 0));
                            t.Add(new Temperature("CPU", 2));
                            f.Add(new Fan("CPU Fan", 0));
                            f.Add(new Fan("System Fan", 1));

                            break;
                        }
                        case Model.H67A_UD3H_B3: // IT8728F
                        case Model.H67A_USB3_B3: // IT8728F
                        {
                            v.Add(new Voltage("VTT", 0));
                            v.Add(new Voltage("+5V", 1, 15, 10));
                            v.Add(new Voltage("+12V", 2, 30.9f, 10));
                            v.Add(new Voltage("Vcore", 5));
                            v.Add(new Voltage("DIMM", 6));
                            v.Add(new Voltage("3VSB", 7, 10, 10));
                            v.Add(new Voltage("VBat", 8, 10, 10));
                            t.Add(new Temperature("System", 0));
                            t.Add(new Temperature("CPU", 2));
                            f.Add(new Fan("CPU Fan", 0));
                            f.Add(new Fan("System Fan #1", 1));
                            f.Add(new Fan("Power Fan", 2));
                            f.Add(new Fan("System Fan #2", 3));

                            break;
                        }
                        case Model.H81M_HD3: //IT8620E
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("Voltage #2", 1, true));
                            v.Add(new Voltage("Voltage #3", 2, true));
                            v.Add(new Voltage("Voltage #4", 3, true));
                            v.Add(new Voltage("iGPU", 4));
                            v.Add(new Voltage("CPU VRIN", 5));
                            v.Add(new Voltage("DIMM", 6));
                            v.Add(new Voltage("3VSB", 7, 10, 10));
                            v.Add(new Voltage("VBat", 8, 10, 10));
                            t.Add(new Temperature("CPU", 2));
                            t.Add(new Temperature("System", 0));
                            f.Add(new Fan("CPU Fan", 0));
                            f.Add(new Fan("System Fan", 1));
                            c.Add(new Ctrl("CPU Fan", 0));
                            c.Add(new Ctrl("System Fan", 1));

                            break;
                        }
                        case Model.AX370_Gaming_K7: // IT8686E
                        case Model.AX370_Gaming_5:
                        case Model.AB350_Gaming_3: // IT8686E
                        {
                            // Note: v3.3, v12, v5, and AVCC3 might be slightly off.
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("+3.3V", 1, 0.65f, 1));
                            v.Add(new Voltage("+12V", 2, 5, 1));
                            v.Add(new Voltage("+5V", 3, 1.5f, 1));
                            v.Add(new Voltage("VSOC", 4));
                            v.Add(new Voltage("VDDP", 5));
                            v.Add(new Voltage("DIMM", 6));
                            v.Add(new Voltage("3VSB", 7, 10, 10));
                            v.Add(new Voltage("VBat", 8, 10, 10));
                            v.Add(new Voltage("AVCC3", 9, 7.53f, 1));
                            t.Add(new Temperature("System", 0));
                            t.Add(new Temperature("Chipset", 1));
                            t.Add(new Temperature("CPU", 2));
                            t.Add(new Temperature("PCIe x16", 3));
                            t.Add(new Temperature("VRM MOS", 4));

                            for (int i = 0; i < superIO.Fans.Length; i++)
                                f.Add(new Fan("Fan #" + (i + 1), i));

                            break;
                        }
                        case Model.X399_AORUS_Gaming_7: // ITE IT8686E
                        {
                            v.Add(new Voltage("Vcore", 0, 0, 1));
                            v.Add(new Voltage("+3.3V", 1, 6.5F, 10));
                            v.Add(new Voltage("+12V", 2, 5, 1));
                            v.Add(new Voltage("+5V", 3, 1.5F, 1));
                            v.Add(new Voltage("DIMM CD", 4, 0, 1));
                            v.Add(new Voltage("Vcore SoC", 5, 0, 1));
                            v.Add(new Voltage("DIMM AB", 6, 0, 1));
                            v.Add(new Voltage("3VSB", 7, 10, 10));
                            v.Add(new Voltage("VBat", 8, 10, 10));
                            v.Add(new Voltage("AVCC3", 9, 54, 10));
                            t.Add(new Temperature("System #1", 0));
                            t.Add(new Temperature("Chipset", 1));
                            t.Add(new Temperature("CPU", 2));
                            t.Add(new Temperature("PCIe x16", 3));
                            t.Add(new Temperature("VRM", 4));

                            for (int i = 0; i < superIO.Fans.Length; i++)
                                f.Add(new Fan("Fan #" + (i + 1), i));

                            for (int i = 0; i < superIO.Controls.Length; i++)
                                c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                            break;
                        }
                        case Model.X470_AORUS_GAMING_7_WIFI: // ITE IT8686E
                        {
                            v.Add(new Voltage("Vcore", 0, 0, 1));
                            v.Add(new Voltage("+3.3V", 1, 6.5F, 10));
                            v.Add(new Voltage("+12V", 2, 5, 1));
                            v.Add(new Voltage("+5V", 3, 1.5F, 1));
                            v.Add(new Voltage("Vcore SoC", 4, 0, 1));
                            v.Add(new Voltage("VDDP", 5, 0, 1));
                            v.Add(new Voltage("DIMM AB", 6, 0, 1));
                            v.Add(new Voltage("3VSB", 7, 10, 10));
                            v.Add(new Voltage("VBat", 8, 10, 10));
                            v.Add(new Voltage("AVCC3", 9, 54, 10));
                            t.Add(new Temperature("System #1", 0));
                            t.Add(new Temperature("Chipset", 1));
                            t.Add(new Temperature("CPU", 2));
                            t.Add(new Temperature("PCIe x16", 3));
                            t.Add(new Temperature("VRM", 4));

                            for (int i = 0; i < superIO.Fans.Length; i++)
                                f.Add(new Fan("Fan #" + (i + 1), i));

                            for (int i = 0; i < superIO.Controls.Length; i++)
                                c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                            break;
                        }
                        case Model.X570_AORUS_MASTER: // IT8688E
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("+3.3V", 1, 29.4f, 45.3f));
                            v.Add(new Voltage("+12V", 2, 10f, 2f));
                            v.Add(new Voltage("+5V", 3, 15f, 10f));
                            v.Add(new Voltage("Vcore SoC", 4));
                            v.Add(new Voltage("VDDP", 5));
                            v.Add(new Voltage("DIMM AB", 6));
                            v.Add(new Voltage("3VSB", 7, 1f, 10f));
                            v.Add(new Voltage("VBat", 8, 1f, 10f));
                            t.Add(new Temperature("System #1", 0));
                            t.Add(new Temperature("EC_TEMP1", 1));
                            t.Add(new Temperature("CPU", 2));
                            t.Add(new Temperature("PCIe x16", 3));
                            t.Add(new Temperature("VRM MOS", 4));
                            t.Add(new Temperature("PCH", 5));
                            f.Add(new Fan("CPU Fan", 0));
                            f.Add(new Fan("System Fan #1", 1));
                            f.Add(new Fan("System Fan #2", 2));
                            f.Add(new Fan("PCH Fan", 3));
                            f.Add(new Fan("CPU OPT Fan", 4));
                            c.Add(new Ctrl("CPU Fan", 0));
                            c.Add(new Ctrl("System Fan #1", 1));
                            c.Add(new Ctrl("System Fan #2", 2));
                            c.Add(new Ctrl("PCH Fan", 3));
                            c.Add(new Ctrl("CPU OPT Fan", 4));

                            break;
                        }
                        case Model.Z390_M_GAMING: // IT8688E
                        case Model.Z390_AORUS_ULTRA:
                        case Model.Z390_UD:
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("+3.3V", 1, 6.49f, 10));
                            v.Add(new Voltage("+12V", 2, 5f, 1));
                            v.Add(new Voltage("+5V", 3, 1.5f, 1));
                            v.Add(new Voltage("CPU VCCGT", 4));
                            v.Add(new Voltage("CPU VCCSA", 5));
                            v.Add(new Voltage("VDDQ", 6));
                            v.Add(new Voltage("DDRVTT", 7));
                            v.Add(new Voltage("PCHCore", 8));
                            v.Add(new Voltage("CPU VCCIO", 9));
                            v.Add(new Voltage("DDRVPP", 10));
                            t.Add(new Temperature("System #1", 0));
                            t.Add(new Temperature("PCH", 1));
                            t.Add(new Temperature("CPU", 2));
                            t.Add(new Temperature("PCIe x16", 3));
                            t.Add(new Temperature("VRM MOS", 4));
                            t.Add(new Temperature("System #2", 5));
                            f.Add(new Fan("CPU Fan", 0));
                            f.Add(new Fan("System Fan #1", 1));
                            f.Add(new Fan("System Fan #2", 2));
                            f.Add(new Fan("System Fan #3", 3));

                            break;
                        }
                        case Model.Z68A_D3H_B3: // IT8728F
                        {
                            v.Add(new Voltage("VTT", 0));
                            v.Add(new Voltage("+3.3V", 1, 6.49f, 10));
                            v.Add(new Voltage("+12V", 2, 30.9f, 10));
                            v.Add(new Voltage("+5V", 3, 7.15f, 10));
                            v.Add(new Voltage("Vcore", 5));
                            v.Add(new Voltage("DIMM", 6));
                            v.Add(new Voltage("3VSB", 7, 10, 10));
                            v.Add(new Voltage("VBat", 8, 10, 10));
                            t.Add(new Temperature("System", 0));
                            t.Add(new Temperature("CPU", 2));
                            f.Add(new Fan("CPU Fan", 0));
                            f.Add(new Fan("System Fan #1", 1));
                            f.Add(new Fan("Power Fan", 2));
                            f.Add(new Fan("System Fan #2", 3));

                            break;
                        }
                        case Model.P67A_UD3_B3: // IT8728F
                        case Model.P67A_UD3R_B3: // IT8728F
                        case Model.P67A_UD4_B3: // IT8728F
                        case Model.Z68AP_D3: // IT8728F
                        case Model.Z68X_UD3H_B3: // IT8728F
                        case Model.Z68XP_UD3R: // IT8728F
                        {
                            v.Add(new Voltage("VTT", 0));
                            v.Add(new Voltage("+3.3V", 1, 6.49f, 10));
                            v.Add(new Voltage("+12V", 2, 30.9f, 10));
                            v.Add(new Voltage("+5V", 3, 7.15f, 10));
                            v.Add(new Voltage("Vcore", 5));
                            v.Add(new Voltage("DIMM", 6));
                            v.Add(new Voltage("3VSB", 7, 10, 10));
                            v.Add(new Voltage("VBat", 8, 10, 10));
                            t.Add(new Temperature("System", 0));
                            t.Add(new Temperature("CPU", 2));
                            f.Add(new Fan("CPU Fan", 0));
                            f.Add(new Fan("System Fan #2", 1));
                            f.Add(new Fan("Power Fan", 2));
                            f.Add(new Fan("System Fan #1", 3));

                            break;
                        }
                        case Model.Z68X_UD7_B3: // IT8728F
                        {
                            v.Add(new Voltage("VTT", 0));
                            v.Add(new Voltage("+3.3V", 1, 6.49f, 10));
                            v.Add(new Voltage("+12V", 2, 30.9f, 10));
                            v.Add(new Voltage("+5V", 3, 7.15f, 10));
                            v.Add(new Voltage("Vcore", 5));
                            v.Add(new Voltage("DIMM", 6));
                            v.Add(new Voltage("3VSB", 7, 10, 10));
                            v.Add(new Voltage("VBat", 8, 10, 10));
                            t.Add(new Temperature("System", 0));
                            t.Add(new Temperature("CPU", 1));
                            t.Add(new Temperature("System #3", 2));
                            f.Add(new Fan("CPU Fan", 0));
                            f.Add(new Fan("Power Fan", 1));
                            f.Add(new Fan("System Fan #1", 2));
                            f.Add(new Fan("System Fan #2", 3));
                            f.Add(new Fan("System Fan #3", 4));

                            break;
                        }
                        case Model.X79_UD3: // IT8728F
                        {
                            v.Add(new Voltage("VTT", 0));
                            v.Add(new Voltage("DIMM AB", 1));
                            v.Add(new Voltage("+12V", 2, 10, 2));
                            v.Add(new Voltage("+5V", 3, 15, 10));
                            v.Add(new Voltage("VIN4", 4));
                            v.Add(new Voltage("VCore", 5));
                            v.Add(new Voltage("DIMM CD", 6));
                            v.Add(new Voltage("+3V Standby", 7, 1, 1));
                            v.Add(new Voltage("VBat", 8, 1, 1));
                            t.Add(new Temperature("System", 0));
                            t.Add(new Temperature("CPU", 1));
                            t.Add(new Temperature("Northbridge", 2));
                            f.Add(new Fan("CPU Fan", 0));
                            f.Add(new Fan("System Fan #1", 1));
                            f.Add(new Fan("System Fan #2", 2));
                            f.Add(new Fan("System Fan #3", 3));

                            break;
                        }
                        default:
                        {
                            v.Add(new Voltage("Voltage #1", 0, true));
                            v.Add(new Voltage("Voltage #2", 1, true));
                            v.Add(new Voltage("Voltage #3", 2, true));
                            v.Add(new Voltage("Voltage #4", 3, true));
                            v.Add(new Voltage("Voltage #5", 4, true));
                            v.Add(new Voltage("Voltage #6", 5, true));
                            v.Add(new Voltage("Voltage #7", 6, true));
                            v.Add(new Voltage("3VSB", 7, 10, 10, 0, true));
                            v.Add(new Voltage("VBat", 8, 10, 10));

                            for (int i = 0; i < superIO.Temperatures.Length; i++)
                                t.Add(new Temperature("Temperature #" + (i + 1), i));

                            for (int i = 0; i < superIO.Fans.Length; i++)
                                f.Add(new Fan("Fan #" + (i + 1), i));

                            for (int i = 0; i < superIO.Controls.Length; i++)
                                c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                            break;
                        }
                    }

                    break;
                }
                case Manufacturer.Shuttle:
                {
                    switch (model)
                    {
                        case Model.FH67: // IT8772E
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("DIMM", 1));
                            v.Add(new Voltage("PCH VCCIO", 2));
                            v.Add(new Voltage("CPU VCCIO", 3));
                            v.Add(new Voltage("Graphic Voltage", 4));
                            v.Add(new Voltage("3VSB", 7, 10, 10));
                            v.Add(new Voltage("VBat", 8, 10, 10));
                            t.Add(new Temperature("System", 0));
                            t.Add(new Temperature("CPU", 1));
                            f.Add(new Fan("Fan #1", 0));
                            f.Add(new Fan("CPU Fan", 1));

                            break;
                        }
                        default:
                        {
                            v.Add(new Voltage("Voltage #1", 0, true));
                            v.Add(new Voltage("Voltage #2", 1, true));
                            v.Add(new Voltage("Voltage #3", 2, true));
                            v.Add(new Voltage("Voltage #4", 3, true));
                            v.Add(new Voltage("Voltage #5", 4, true));
                            v.Add(new Voltage("Voltage #6", 5, true));
                            v.Add(new Voltage("Voltage #7", 6, true));
                            v.Add(new Voltage("3VSB", 7, 10, 10, 0, true));
                            v.Add(new Voltage("VBat", 8, 10, 10));

                            for (int i = 0; i < superIO.Temperatures.Length; i++)
                                t.Add(new Temperature("Temperature #" + (i + 1), i));

                            for (int i = 0; i < superIO.Fans.Length; i++)
                                f.Add(new Fan("Fan #" + (i + 1), i));

                            for (int i = 0; i < superIO.Controls.Length; i++)
                                c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                            break;
                        }
                    }

                    break;
                }
                default:
                {
                    v.Add(new Voltage("Voltage #1", 0, true));
                    v.Add(new Voltage("Voltage #2", 1, true));
                    v.Add(new Voltage("Voltage #3", 2, true));
                    v.Add(new Voltage("Voltage #4", 3, true));
                    v.Add(new Voltage("Voltage #5", 4, true));
                    v.Add(new Voltage("Voltage #6", 5, true));
                    v.Add(new Voltage("Voltage #7", 6, true));
                    v.Add(new Voltage("3VSB", 7, 10, 10, 0, true));
                    v.Add(new Voltage("VBat", 8, 10, 10));

                    for (int i = 0; i < superIO.Temperatures.Length; i++)
                        t.Add(new Temperature("Temperature #" + (i + 1), i));

                    for (int i = 0; i < superIO.Fans.Length; i++)
                        f.Add(new Fan("Fan #" + (i + 1), i));

                    for (int i = 0; i < superIO.Controls.Length; i++)
                        c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                    break;
                }
            }
        }

        private static void GetIteConfigurationsC(ISuperIO superIO, Manufacturer manufacturer, Model model, IList<Voltage> v, IList<Temperature> t, IList<Fan> f, IList<Ctrl> c)
        {
            switch (manufacturer)
            {
                case Manufacturer.Gigabyte:
                {
                    switch (model)
                    {
                        case Model.X570_AORUS_MASTER: // IT879XE
                        {
                            v.Add(new Voltage("CPU VDD18", 0));
                            v.Add(new Voltage("DDRVTT AB", 1));
                            v.Add(new Voltage("Chipset Core", 2));
                            v.Add(new Voltage("Voltage #4", 3, true));
                            v.Add(new Voltage("CPU VDD18", 4));
                            v.Add(new Voltage("PM_CLDO12", 5));
                            v.Add(new Voltage("Voltage #7", 6, true));
                            v.Add(new Voltage("3VSB", 7, 1f, 1f));
                            v.Add(new Voltage("VBat", 8, 1f, 1f));
                            t.Add(new Temperature("PCIe x8", 0));
                            t.Add(new Temperature("EC_TEMP2", 1));
                            t.Add(new Temperature("System #2", 2));
                            f.Add(new Fan("System Fan #5 Pump", 0));
                            f.Add(new Fan("System Fan #6 Pump", 1));
                            f.Add(new Fan("System Fan #4", 2));

                            break;
                        }
                        case Model.X470_AORUS_GAMING_7_WIFI: // ITE IT8792
                        {
                            v.Add(new Voltage("VIN0", 0, 0, 1));
                            v.Add(new Voltage("DDR VTT", 1, 0, 1));
                            v.Add(new Voltage("Chipset Core", 2, 0, 1));
                            v.Add(new Voltage("VIN3", 3, 0, 1));
                            v.Add(new Voltage("CPU VDD18", 4, 0, 1));
                            v.Add(new Voltage("Chipset Core +2.5V", 5, 0.5F, 1));
                            v.Add(new Voltage("3VSB", 6, 1, 10));
                            v.Add(new Voltage("VBat", 7, 0.7F, 1));
                            t.Add(new Temperature("PCIe x8", 0));
                            t.Add(new Temperature("System #2", 2));

                            for (int i = 0; i < superIO.Fans.Length; i++)
                                f.Add(new Fan("Fan #" + (i + 1), i));

                            for (int i = 0; i < superIO.Controls.Length; i++)
                                c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                            break;
                        }
                        default:
                        {
                            v.Add(new Voltage("Voltage #1", 0, true));
                            v.Add(new Voltage("Voltage #2", 1, true));
                            v.Add(new Voltage("Voltage #3", 2, true));
                            v.Add(new Voltage("Voltage #4", 3, true));
                            v.Add(new Voltage("Voltage #5", 4, true));
                            v.Add(new Voltage("Voltage #6", 5, true));
                            v.Add(new Voltage("Voltage #7", 6, true));
                            v.Add(new Voltage("3VSB", 7, 10, 10, 0, true));
                            v.Add(new Voltage("VBat", 8, 10, 10));

                            for (int i = 0; i < superIO.Temperatures.Length; i++)
                                t.Add(new Temperature("Temperature #" + (i + 1), i));

                            for (int i = 0; i < superIO.Fans.Length; i++)
                                f.Add(new Fan("Fan #" + (i + 1), i));

                            for (int i = 0; i < superIO.Controls.Length; i++)
                                c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                            break;
                        }
                    }

                    break;
                }
                default:
                {
                    v.Add(new Voltage("Voltage #1", 0, true));
                    v.Add(new Voltage("Voltage #2", 1, true));
                    v.Add(new Voltage("Voltage #3", 2, true));
                    v.Add(new Voltage("Voltage #4", 3, true));
                    v.Add(new Voltage("Voltage #5", 4, true));
                    v.Add(new Voltage("Voltage #6", 5, true));
                    v.Add(new Voltage("Voltage #7", 6, true));
                    v.Add(new Voltage("3VSB", 7, 10, 10, 0, true));
                    v.Add(new Voltage("VBat", 8, 10, 10));

                    for (int i = 0; i < superIO.Temperatures.Length; i++)
                        t.Add(new Temperature("Temperature #" + (i + 1), i));

                    for (int i = 0; i < superIO.Fans.Length; i++)
                        f.Add(new Fan("Fan #" + (i + 1), i));

                    for (int i = 0; i < superIO.Controls.Length; i++)
                        c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                    break;
                }
            }
        }

        private static void GetFintekConfiguration(ISuperIO superIO, Manufacturer manufacturer, Model model, IList<Voltage> v, IList<Temperature> t, IList<Fan> f, IList<Ctrl> c)
        {
            switch (manufacturer)
            {
                case Manufacturer.EVGA:
                {
                    switch (model)
                    {
                        case Model.X58_SLI_Classified: // F71882
                        {
                            v.Add(new Voltage("VCC3V", 0, 150, 150));
                            v.Add(new Voltage("Vcore", 1, 47, 100));
                            v.Add(new Voltage("DIMM", 2, 47, 100));
                            v.Add(new Voltage("CPU VTT", 3, 24, 100));
                            v.Add(new Voltage("IOH Vcore", 4, 24, 100));
                            v.Add(new Voltage("+5V", 5, 51, 12));
                            v.Add(new Voltage("+12V", 6, 56, 6.8f));
                            v.Add(new Voltage("3VSB", 7, 150, 150));
                            v.Add(new Voltage("VBat", 8, 150, 150));
                            t.Add(new Temperature("CPU", 0));
                            t.Add(new Temperature("VREG", 1));
                            t.Add(new Temperature("System", 2));
                            f.Add(new Fan("CPU Fan", 0));
                            f.Add(new Fan("Power Fan", 1));
                            f.Add(new Fan("Chassis Fan", 2));

                            break;
                        }
                        default:
                        {
                            v.Add(new Voltage("VCC3V", 0, 150, 150));
                            v.Add(new Voltage("Vcore", 1));
                            v.Add(new Voltage("Voltage #3", 2, true));
                            v.Add(new Voltage("Voltage #4", 3, true));
                            v.Add(new Voltage("Voltage #5", 4, true));
                            v.Add(new Voltage("Voltage #6", 5, true));
                            v.Add(new Voltage("Voltage #7", 6, true));
                            v.Add(new Voltage("VSB3V", 7, 150, 150));
                            v.Add(new Voltage("VBat", 8, 150, 150));

                            for (int i = 0; i < superIO.Temperatures.Length; i++)
                                t.Add(new Temperature("Temperature #" + (i + 1), i));

                            for (int i = 0; i < superIO.Fans.Length; i++)
                                f.Add(new Fan("Fan #" + (i + 1), i));

                            break;
                        }
                    }

                    break;
                }
                default:
                {
                    v.Add(new Voltage("VCC3V", 0, 150, 150));
                    v.Add(new Voltage("Vcore", 1));
                    v.Add(new Voltage("Voltage #3", 2, true));
                    v.Add(new Voltage("Voltage #4", 3, true));
                    v.Add(new Voltage("Voltage #5", 4, true));
                    v.Add(new Voltage("Voltage #6", 5, true));
                    if (superIO.Chip != Chip.F71808E)
                        v.Add(new Voltage("Voltage #7", 6, true));

                    v.Add(new Voltage("VSB3V", 7, 150, 150));
                    v.Add(new Voltage("VBat", 8, 150, 150));

                    for (int i = 0; i < superIO.Temperatures.Length; i++)
                        t.Add(new Temperature("Temperature #" + (i + 1), i));

                    for (int i = 0; i < superIO.Fans.Length; i++)
                        f.Add(new Fan("Fan #" + (i + 1), i));

                    for (int i = 0; i < superIO.Controls.Length; i++)
                        c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                    break;
                }
            }
        }

        private static void GetNuvotonConfigurationF(ISuperIO superIO, Manufacturer manufacturer, Model model, IList<Voltage> v, IList<Temperature> t, IList<Fan> f, IList<Ctrl> c)
        {
            switch (manufacturer)
            {
                case Manufacturer.ASUS:
                {
                    switch (model)
                    {
                        case Model.P8P67: // NCT6776F
                        case Model.P8P67_EVO: // NCT6776F
                        case Model.P8P67_PRO: // NCT6776F
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("+12V", 1, 11, 1));
                            v.Add(new Voltage("AVCC", 2, 34, 34));
                            v.Add(new Voltage("+3.3V", 3, 34, 34));
                            v.Add(new Voltage("+5V", 4, 12, 3));
                            v.Add(new Voltage("3VSB", 7, 34, 34));
                            v.Add(new Voltage("VBat", 8, 34, 34));
                            t.Add(new Temperature("CPU", 0));
                            t.Add(new Temperature("Auxiliary", 2));
                            t.Add(new Temperature("Motherboard", 3));
                            f.Add(new Fan("Chassis Fan #1", 0));
                            f.Add(new Fan("CPU Fan", 1));
                            f.Add(new Fan("Power Fan", 2));
                            f.Add(new Fan("Chassis Fan #2", 3));
                            c.Add(new Ctrl("Chassis Fan #2", 0));
                            c.Add(new Ctrl("CPU Fan", 1));
                            c.Add(new Ctrl("Chassis Fan #1", 2));

                            break;
                        }
                        case Model.P8P67_M_PRO: // NCT6776F
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("+12V", 1, 11, 1));
                            v.Add(new Voltage("AVCC", 2, 34, 34));
                            v.Add(new Voltage("+3.3V", 3, 34, 34));
                            v.Add(new Voltage("+5V", 4, 12, 3));
                            v.Add(new Voltage("Voltage #6", 5, true));
                            v.Add(new Voltage("Voltage #7", 6, true));
                            v.Add(new Voltage("3VSB", 7, 34, 34));
                            v.Add(new Voltage("VBat", 8, 34, 34));
                            t.Add(new Temperature("CPU", 0));
                            t.Add(new Temperature("Motherboard", 3));
                            f.Add(new Fan("Chassis Fan #1", 0));
                            f.Add(new Fan("CPU Fan", 1));
                            f.Add(new Fan("Chassis Fan #2", 2));
                            f.Add(new Fan("Power Fan", 3));
                            f.Add(new Fan("Auxiliary Fan", 4));

                            break;
                        }
                        case Model.P8Z68_V_PRO: // NCT6776F
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("+12V", 1, 11, 1));
                            v.Add(new Voltage("AVCC", 2, 34, 34));
                            v.Add(new Voltage("+3.3V", 3, 34, 34));
                            v.Add(new Voltage("+5V", 4, 12, 3));
                            v.Add(new Voltage("3VSB", 7, 34, 34));
                            v.Add(new Voltage("VBat", 8, 34, 34));
                            t.Add(new Temperature("CPU", 0));
                            t.Add(new Temperature("Auxiliary", 2));
                            t.Add(new Temperature("Motherboard", 3));

                            for (int i = 0; i < superIO.Fans.Length; i++)
                                f.Add(new Fan("Fan #" + (i + 1), i));

                            for (int i = 0; i < superIO.Controls.Length; i++)
                                c.Add(new Ctrl("Fan #" + (i + 1), i));

                            break;
                        }
                        case Model.P9X79: // NCT6776F
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("+12V", 1, 11, 1));
                            v.Add(new Voltage("AVCC", 2, 34, 34));
                            v.Add(new Voltage("+3.3V", 3, 34, 34));
                            v.Add(new Voltage("+5V", 4, 12, 3));
                            v.Add(new Voltage("3VSB", 7, 34, 34));
                            v.Add(new Voltage("VBat", 8, 34, 34));
                            t.Add(new Temperature("CPU", 0));
                            t.Add(new Temperature("Motherboard", 3));

                            for (int i = 0; i < superIO.Fans.Length; i++)
                                f.Add(new Fan("Fan #" + (i + 1), i));

                            for (int i = 0; i < superIO.Controls.Length; i++)
                                c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                            break;
                        }
                        default:
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("Voltage #2", 1, true));
                            v.Add(new Voltage("AVCC", 2, 34, 34));
                            v.Add(new Voltage("+3.3V", 3, 34, 34));
                            v.Add(new Voltage("Voltage #5", 4, true));
                            v.Add(new Voltage("Voltage #6", 5, true));
                            v.Add(new Voltage("Voltage #7", 6, true));
                            v.Add(new Voltage("3VSB", 7, 34, 34));
                            v.Add(new Voltage("VBat", 8, 34, 34));
                            t.Add(new Temperature("CPU Core", 0));
                            t.Add(new Temperature("Temperature #1", 1));
                            t.Add(new Temperature("Temperature #2", 2));
                            t.Add(new Temperature("Temperature #3", 3));

                            for (int i = 0; i < superIO.Fans.Length; i++)
                                f.Add(new Fan("Fan #" + (i + 1), i));

                            for (int i = 0; i < superIO.Controls.Length; i++)
                                c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                            break;
                        }
                    }

                    break;
                }
                case Manufacturer.ASRock:
                {
                    switch (model)
                    {
                        case Model.B85M_DGS:
                        {
                            v.Add(new Voltage("Vcore", 0, 1, 1));
                            v.Add(new Voltage("+12V", 1, 56, 10));
                            v.Add(new Voltage("AVCC", 2, 34, 34));
                            v.Add(new Voltage("+3.3V", 3, 34, 34));
                            v.Add(new Voltage("VIN1", 4, true));
                            v.Add(new Voltage("+5V", 5, 12, 3));
                            v.Add(new Voltage("VIN3", 6, true));
                            v.Add(new Voltage("3VSB", 7, 34, 34));
                            t.Add(new Temperature("CPU", 0));
                            t.Add(new Temperature("Auxiliary", 2));
                            t.Add(new Temperature("Motherboard", 3));
                            f.Add(new Fan("Chassis Fan #1", 0));
                            f.Add(new Fan("CPU Fan", 1));
                            f.Add(new Fan("Power Fan", 2));
                            f.Add(new Fan("Chassis Fan #2", 3));
                            c.Add(new Ctrl("Chassis Fan #2", 0));
                            c.Add(new Ctrl("CPU Fan", 1));
                            c.Add(new Ctrl("Chassis Fan #1", 2));
                        }

                            break;
                        case Model.Z77Pro4M: //NCT6776F
                        {
                            v.Add(new Voltage("Vcore", 0, 0, 1));
                            v.Add(new Voltage("+12V", 1, 56, 10));
                            v.Add(new Voltage("AVCC", 2, 10, 10));
                            v.Add(new Voltage("+3.3V", 3, 10, 10));
                            //v.Add(new Voltage("#Unused #4", 4, 0, 1, 0, true));
                            v.Add(new Voltage("+5V", 5, 20, 10));
                            //v.Add(new Voltage("#Unused #6", 6, 0, 1, 0, true));
                            v.Add(new Voltage("3VSB", 7, 10, 10));
                            v.Add(new Voltage("VBat", 8, 10, 10));
                            t.Add(new Temperature("CPU Core", 0));
                            t.Add(new Temperature("CPU", 1));
                            t.Add(new Temperature("Auxiliary", 2));
                            t.Add(new Temperature("Motherboard", 3));

                            for (int i = 0; i < superIO.Fans.Length; i++)
                                f.Add(new Fan("Fan #" + (i + 1), i));

                            for (int i = 0; i < superIO.Controls.Length; i++)
                                c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                            break;
                        }
                        default:
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("Voltage #2", 1, true));
                            v.Add(new Voltage("AVCC", 2, 34, 34));
                            v.Add(new Voltage("+3.3V", 3, 34, 34));
                            v.Add(new Voltage("Voltage #5", 4, true));
                            v.Add(new Voltage("Voltage #6", 5, true));
                            v.Add(new Voltage("Voltage #7", 6, true));
                            v.Add(new Voltage("3VSB", 7, 34, 34));
                            v.Add(new Voltage("VBat", 8, 34, 34));
                            t.Add(new Temperature("CPU Core", 0));
                            t.Add(new Temperature("Temperature #1", 1));
                            t.Add(new Temperature("Temperature #2", 2));
                            t.Add(new Temperature("Temperature #3", 3));

                            for (int i = 0; i < superIO.Fans.Length; i++)
                                f.Add(new Fan("Fan #" + (i + 1), i));

                            for (int i = 0; i < superIO.Controls.Length; i++)
                                c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                            break;
                        }
                    }

                    break;
                }
                default:
                {
                    v.Add(new Voltage("Vcore", 0));
                    v.Add(new Voltage("Voltage #2", 1, true));
                    v.Add(new Voltage("AVCC", 2, 34, 34));
                    v.Add(new Voltage("+3.3V", 3, 34, 34));
                    v.Add(new Voltage("Voltage #5", 4, true));
                    v.Add(new Voltage("Voltage #6", 5, true));
                    v.Add(new Voltage("Voltage #7", 6, true));
                    v.Add(new Voltage("3VSB", 7, 34, 34));
                    v.Add(new Voltage("VBat", 8, 34, 34));
                    t.Add(new Temperature("CPU Core", 0));
                    t.Add(new Temperature("Temperature #1", 1));
                    t.Add(new Temperature("Temperature #2", 2));
                    t.Add(new Temperature("Temperature #3", 3));

                    for (int i = 0; i < superIO.Fans.Length; i++)
                        f.Add(new Fan("Fan #" + (i + 1), i));

                    for (int i = 0; i < superIO.Controls.Length; i++)
                        c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                    break;
                }
            }
        }

        private static void GetNuvotonConfigurationD(ISuperIO superIO, Manufacturer manufacturer, Model model, IList<Voltage> v, IList<Temperature> t, IList<Fan> f, IList<Ctrl> c)
        {
            switch (manufacturer)
            {
                case Manufacturer.ASRock:
                {
                    switch (model)
                    {
                        case Model.A320M_HDV: //NCT6779D
                        {
                            v.Add(new Voltage("Vcore", 0, 10, 10));
                            v.Add(new Voltage("Chipset 1.05V", 1, 0, 1));
                            v.Add(new Voltage("AVCC", 2, 10, 10));
                            v.Add(new Voltage("+3.3V", 3, 10, 10));
                            v.Add(new Voltage("+12V", 4, 56, 10));
                            v.Add(new Voltage("VcoreRef", 5, 0, 1));
                            v.Add(new Voltage("DIMM", 6, 0, 1));
                            v.Add(new Voltage("3VSB", 7, 10, 10));
                            v.Add(new Voltage("VBat", 8, 10, 10));
                            //v.Add(new Voltage("#Unused #9", 9, 0, 1, 0, true));
                            //v.Add(new Voltage("#Unused #10", 10, 0, 1, 0, true));
                            //v.Add(new Voltage("#Unused #11", 11, 34, 34, 0, true));
                            v.Add(new Voltage("+5V", 12, 20, 10));
                            //v.Add(new Voltage("#Unused #13", 13, 10, 10, 0, true));
                            //v.Add(new Voltage("#Unused #14", 14, 0, 1, 0, true));

                            //t.Add(new Temperature("#Unused #0", 0));
                            //t.Add(new Temperature("#Unused #1", 1));
                            t.Add(new Temperature("Motherboard", 2));
                            //t.Add(new Temperature("#Unused #3", 3));
                            //t.Add(new Temperature("#Unused #4", 4));
                            t.Add(new Temperature("Auxiliary", 5));

                            for (int i = 0; i < superIO.Fans.Length; i++)
                                f.Add(new Fan("Fan #" + (i + 1), i));

                            for (int i = 0; i < superIO.Controls.Length; i++)
                                c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                            break;
                        }

                        case Model.AB350_Pro4: //NCT6779D
                        case Model.AB350M_Pro4:
                        case Model.AB350M:
                        case Model.Fatal1ty_AB350_Gaming_K4:
                        case Model.AB350M_HDV:
                        case Model.B450_Steel_Legend:
                        case Model.B450M_Steel_Legend:
                        case Model.B450_Pro4:
                        case Model.B450M_Pro4:
                        {
                            v.Add(new Voltage("Vcore", 0, 10, 10));
                            //v.Add(new Voltage("#Unused", 1, 0, 1, 0, true));
                            v.Add(new Voltage("AVCC", 2, 10, 10));
                            v.Add(new Voltage("+3.3V", 3, 10, 10));
                            v.Add(new Voltage("+12V", 4, 28, 5));
                            v.Add(new Voltage("Vcore Refin", 5, 0, 1));
                            //v.Add(new Voltage("#Unused #6", 6, 0, 1, 0, true));
                            v.Add(new Voltage("3VSB", 7, 10, 10));
                            v.Add(new Voltage("VBat", 8, 34, 34));
                            //v.Add(new Voltage("#Unused #9", 9, 0, 1, 0, true));
                            //v.Add(new Voltage("#Unused #10", 10, 0, 1, 0, true));
                            v.Add(new Voltage("Chipset 1.05V", 11, 0, 1));
                            v.Add(new Voltage("+5V", 12, 20, 10));
                            //v.Add(new Voltage("#Unused #13", 13, 0, 1, 0, true));
                            v.Add(new Voltage("+1.8V", 14, 0, 1));
                            t.Add(new Temperature("CPU Core", 0));
                            t.Add(new Temperature("CPU", 1));
                            t.Add(new Temperature("Motherboard", 2));
                            t.Add(new Temperature("Auxiliary", 3));
                            t.Add(new Temperature("VRM", 4));
                            t.Add(new Temperature("AUXTIN2", 5));
                            //t.Add(new Temperature("Temperature #6", 6));

                            for (int i = 0; i < superIO.Fans.Length; i++)
                                f.Add(new Fan("Fan #" + (i + 1), i));

                            for (int i = 0; i < superIO.Controls.Length; i++)
                                c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                            break;
                        }

                        case Model.X399_Phantom_Gaming_6: //NCT6779D
                        {
                            v.Add(new Voltage("Vcore", 0, 10, 10));
                            v.Add(new Voltage("Chipset 1.05V", 1, 0, 1));
                            v.Add(new Voltage("AVCC", 2, 10, 10));
                            v.Add(new Voltage("+3.3V", 3, 10, 10));
                            v.Add(new Voltage("+12V", 4, 56, 10));
                            v.Add(new Voltage("VDDCR_SOC", 5, 0, 1));
                            v.Add(new Voltage("DIMM", 6, 0, 1));
                            v.Add(new Voltage("3VSB", 7, 10, 10));
                            v.Add(new Voltage("VBat", 8, 10, 10));
                            //v.Add(new Voltage("#Unused", 9, 0, 1, 0, true));
                            //v.Add(new Voltage("#Unused", 10, 0, 1, 0, true));
                            //v.Add(new Voltage("#Unused", 11, 0, 1, 0, true));
                            v.Add(new Voltage("+5V", 12, 20, 10));
                            v.Add(new Voltage("+1.8V", 13, 10, 10));
                            //v.Add(new Voltage("unused", 14, 34, 34, 0, true));
                            t.Add(new Temperature("CPU Core", 0));
                            t.Add(new Temperature("Motherboard", 1));
                            t.Add(new Temperature("Auxiliary", 2));
                            t.Add(new Temperature("Chipset", 3));
                            t.Add(new Temperature("Core VRM", 4));
                            t.Add(new Temperature("Core SoC", 5));

                            for (int i = 0; i < superIO.Fans.Length; i++)
                                f.Add(new Fan("Fan #" + (i + 1), i));

                            for (int i = 0; i < superIO.Controls.Length; i++)
                                c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                            break;
                        }
                        default:
                        {
                            v.Add(new Voltage("Vcore", 0, 10, 10));
                            v.Add(new Voltage("Voltage #2", 1, true));
                            v.Add(new Voltage("AVCC", 2, 34, 34));
                            v.Add(new Voltage("+3.3V", 3, 34, 34));
                            v.Add(new Voltage("Voltage #5", 4, true));
                            v.Add(new Voltage("Voltage #6", 5, true));
                            v.Add(new Voltage("Voltage #7", 6, true));
                            v.Add(new Voltage("3VSB", 7, 34, 34));
                            v.Add(new Voltage("VBat", 8, 34, 34));
                            v.Add(new Voltage("VTT", 9));
                            v.Add(new Voltage("Voltage #11", 10, true));
                            v.Add(new Voltage("Voltage #12", 11, true));
                            v.Add(new Voltage("Voltage #13", 12, true));
                            v.Add(new Voltage("Voltage #14", 13, true));
                            v.Add(new Voltage("Voltage #15", 14, true));
                            t.Add(new Temperature("CPU Core", 0));
                            t.Add(new Temperature("Temperature #1", 1));
                            t.Add(new Temperature("Temperature #2", 2));
                            t.Add(new Temperature("Temperature #3", 3));
                            t.Add(new Temperature("Temperature #4", 4));
                            t.Add(new Temperature("Temperature #5", 5));
                            t.Add(new Temperature("Temperature #6", 6));

                            for (int i = 0; i < superIO.Fans.Length; i++)
                                f.Add(new Fan("Fan #" + (i + 1), i));

                            for (int i = 0; i < superIO.Controls.Length; i++)
                                c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                            break;
                        }
                    }

                    break;
                }
                case Manufacturer.ASUS:
                {
                    switch (model)
                    {
                        case Model.P8Z77_V: // NCT6779D
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("Voltage #2", 1, true));
                            v.Add(new Voltage("AVCC", 2, 34, 34));
                            v.Add(new Voltage("+3.3V", 3, 34, 34));
                            v.Add(new Voltage("Voltage #5", 4, true));
                            v.Add(new Voltage("Voltage #6", 5, true));
                            v.Add(new Voltage("Voltage #7", 6, true));
                            v.Add(new Voltage("3VSB", 7, 34, 34));
                            v.Add(new Voltage("VBat", 8, 34, 34));
                            v.Add(new Voltage("VTT", 9));
                            v.Add(new Voltage("Voltage #11", 10, true));
                            v.Add(new Voltage("Voltage #12", 11, true));
                            v.Add(new Voltage("Voltage #13", 12, true));
                            v.Add(new Voltage("Voltage #14", 13, true));
                            v.Add(new Voltage("Voltage #15", 14, true));
                            t.Add(new Temperature("CPU Core", 0));
                            t.Add(new Temperature("Auxiliary", 1));
                            t.Add(new Temperature("Motherboard", 2));
                            f.Add(new Fan("Chassis Fan #1", 0));
                            f.Add(new Fan("CPU Fan", 1));
                            f.Add(new Fan("Chassis Fan #2", 2));
                            f.Add(new Fan("Chassis Fan #3", 3));
                            c.Add(new Ctrl("Chassis Fan #1", 0));
                            c.Add(new Ctrl("CPU  Fan", 1));
                            c.Add(new Ctrl("Chassis Fan #2", 2));
                            c.Add(new Ctrl("Chassis Fan #3", 3));

                            break;
                        }
                        case Model.ROG_MAXIMUS_X_APEX: // NCT6793D
                        {
                            v.Add(new Voltage("Vcore", 0, 2, 2));
                            v.Add(new Voltage("+5V", 1, 4, 1));
                            v.Add(new Voltage("AVSB", 2, 34, 34));
                            v.Add(new Voltage("+3.3V", 3, 34, 34));
                            v.Add(new Voltage("+12V", 4, 11, 1));
                            v.Add(new Voltage("Voltage #6", 5, true));
                            v.Add(new Voltage("CPU GFX", 6, 2, 2));
                            v.Add(new Voltage("3VSB", 7, 34, 34));
                            v.Add(new Voltage("VBat", 8, 34, 34));
                            v.Add(new Voltage("VTT", 9));
                            v.Add(new Voltage("DIMM", 10, 1, 1));
                            v.Add(new Voltage("VCCSA", 11));
                            v.Add(new Voltage("PCH Core", 12));
                            v.Add(new Voltage("CPU PLLs", 13));
                            v.Add(new Voltage("CPU VCCIO/IMC", 14));
                            t.Add(new Temperature("CPU (PECI)", 0));
                            t.Add(new Temperature("T2", 1));
                            t.Add(new Temperature("T1", 2));
                            t.Add(new Temperature("CPU", 3));
                            t.Add(new Temperature("PCH", 4));
                            t.Add(new Temperature("Temperature #4", 5));
                            t.Add(new Temperature("Temperature #5", 6));
                            f.Add(new Fan("Chassis Fan #1", 0));
                            f.Add(new Fan("CPU Fan", 1));
                            f.Add(new Fan("Chassis Fan #2", 2));
                            f.Add(new Fan("Chassis Fan #3", 3));
                            f.Add(new Fan("AIO Pump", 4));
                            c.Add(new Ctrl("Chassis Fan #1", 0));
                            c.Add(new Ctrl("CPU Fan", 1));
                            c.Add(new Ctrl("Chassis Fan #2", 2));
                            c.Add(new Ctrl("Chassis Fan #3", 3));
                            c.Add(new Ctrl("AIO Pump", 4));

                            break;
                        }
                        case Model.Z170_A: //NCT6793D
                        {
                            v.Add(new Voltage("Vcore", 0, 2, 2));
                            v.Add(new Voltage("+5V", 1, 4, 1));
                            v.Add(new Voltage("AVSB", 2, 34, 34));
                            v.Add(new Voltage("+3.3V", 3, 34, 34));
                            v.Add(new Voltage("+12V", 4, 11, 1));
                            v.Add(new Voltage("Voltage #6", 5, 0, 1, 0, true));
                            v.Add(new Voltage("CPU GFX", 6, 2, 2));
                            v.Add(new Voltage("3VSB_ATX", 7, 34, 34));
                            v.Add(new Voltage("VBat", 8, 34, 34));
                            v.Add(new Voltage("VTT", 9));
                            v.Add(new Voltage("DIMM", 10, 1, 1));
                            v.Add(new Voltage("VCCSA", 11));
                            v.Add(new Voltage("PCH Core", 12));
                            v.Add(new Voltage("CPU PLLs", 13));
                            v.Add(new Voltage("CPU VCCIO/IMC", 14));
                            t.Add(new Temperature("CPU (PECI)", 0));
                            t.Add(new Temperature("CPU", 1));
                            t.Add(new Temperature("Motherboard", 2));
                            t.Add(new Temperature("CPU", 3));
                            t.Add(new Temperature("PCH", 4));
                            t.Add(new Temperature("Temperature #4", 5));
                            t.Add(new Temperature("Temperature #5", 6));

                            for (int i = 0; i < superIO.Fans.Length; i++)
                                f.Add(new Fan("Fan #" + (i + 1), i));

                            for (int i = 0; i < superIO.Controls.Length; i++)
                                c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                            break;
                        }
                        default:
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("Voltage #2", 1, true));
                            v.Add(new Voltage("AVCC", 2, 34, 34));
                            v.Add(new Voltage("+3.3V", 3, 34, 34));
                            v.Add(new Voltage("Voltage #5", 4, true));
                            v.Add(new Voltage("Voltage #6", 5, true));
                            v.Add(new Voltage("Voltage #7", 6, true));
                            v.Add(new Voltage("3VSB", 7, 34, 34));
                            v.Add(new Voltage("VBat", 8, 34, 34));
                            v.Add(new Voltage("VTT", 9));
                            v.Add(new Voltage("Voltage #11", 10, true));
                            v.Add(new Voltage("Voltage #12", 11, true));
                            v.Add(new Voltage("Voltage #13", 12, true));
                            v.Add(new Voltage("Voltage #14", 13, true));
                            v.Add(new Voltage("Voltage #15", 14, true));
                            t.Add(new Temperature("CPU Core", 0));
                            t.Add(new Temperature("Temperature #1", 1));
                            t.Add(new Temperature("Temperature #2", 2));
                            t.Add(new Temperature("Temperature #3", 3));
                            t.Add(new Temperature("Temperature #4", 4));
                            t.Add(new Temperature("Temperature #5", 5));
                            t.Add(new Temperature("Temperature #6", 6));

                            for (int i = 0; i < superIO.Fans.Length; i++)
                                f.Add(new Fan("Fan #" + (i + 1), i));

                            for (int i = 0; i < superIO.Controls.Length; i++)
                                c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                            break;
                        }
                    }

                    break;
                }
                case Manufacturer.MSI:
                {
                    switch (model)
                    {
                        case Model.B360M_PRO_VDH: // NCT6797D
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("+5V", 1, 4, 1));
                            v.Add(new Voltage("AVCC", 2, 34, 34));
                            v.Add(new Voltage("+3.3V", 3, 34, 34));
                            v.Add(new Voltage("+12V", 4, 11, 1));
                            //v.Add(new Voltage("Voltage #6", 5, true));
                            v.Add(new Voltage("CPU I/O", 6));
                            v.Add(new Voltage("3VSB", 7, 34, 34));
                            v.Add(new Voltage("VTT", 9));
                            v.Add(new Voltage("CPU SA", 10));
                            //v.Add(new Voltage("Voltage #12", 11, true));
                            v.Add(new Voltage("NB/SoC", 12));
                            v.Add(new Voltage("DIMM", 13, 1, 1));
                            //v.Add(new Voltage("Voltage #15", 14, true));
                            t.Add(new Temperature("CPU", 0));
                            t.Add(new Temperature("Auxiliary", 1));
                            t.Add(new Temperature("Motherboard", 2));
                            t.Add(new Temperature("Temperature #1", 5));
                            f.Add(new Fan("CPU Fan", 1));
                            f.Add(new Fan("System Fan #1", 2));
                            f.Add(new Fan("System Fan #2", 3));
                            c.Add(new Ctrl("CPU Fan", 1));
                            c.Add(new Ctrl("System Fan #1", 2));
                            c.Add(new Ctrl("System Fan #2", 3));

                            break;
                        }
                        case Model.B450A_PRO: // NCT6797D
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("+5V", 1, 4, 1));
                            v.Add(new Voltage("AVCC", 2, 34, 34));
                            v.Add(new Voltage("+3.3V", 3, 34, 34));
                            v.Add(new Voltage("+12V", 4, 11, 1));
                            //v.Add(new Voltage("Voltage #6", 5, false));
                            //v.Add(new Voltage("CPU I/O", 6));
                            v.Add(new Voltage("3VSB", 7, 34, 34));
                            v.Add(new Voltage("VTT", 9));
                            v.Add(new Voltage("CPU SA", 10));
                            //v.Add(new Voltage("Voltage #12", 11, false));
                            v.Add(new Voltage("NB/SoC", 12));
                            v.Add(new Voltage("DIMM", 13, 1, 1));
                            //v.Add(new Voltage("Voltage #15", 14, false));
                            t.Add(new Temperature("CPU", 0));
                            t.Add(new Temperature("CPU (PECI)", 1));
                            t.Add(new Temperature("System", 2));
                            t.Add(new Temperature("VRM MOS", 3));
                            t.Add(new Temperature("PCH", 5));
                            f.Add(new Fan("Pump Fan", 0));
                            f.Add(new Fan("CPU Fan", 1));
                            f.Add(new Fan("System Fan #1", 2));
                            f.Add(new Fan("System Fan #2", 3));
                            f.Add(new Fan("System Fan #3", 4));
                            f.Add(new Fan("System Fan #4", 5));
                            c.Add(new Ctrl("Pump Fan", 0));
                            c.Add(new Ctrl("CPU Fan", 1));
                            c.Add(new Ctrl("System Fan #1", 2));
                            c.Add(new Ctrl("System Fan #2", 3));
                            c.Add(new Ctrl("System Fan #3", 4));
                            c.Add(new Ctrl("System Fan #4", 5));

                            break;
                        }
                        case Model.Z270_PC_MATE: // NCT6795D
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("+5V", 1, 4, 1));
                            v.Add(new Voltage("AVCC", 2, 34, 34));
                            v.Add(new Voltage("+3.3V", 3, 34, 34));
                            v.Add(new Voltage("+12V", 4, 11, 1));
                            v.Add(new Voltage("Voltage #6", 5, true));
                            v.Add(new Voltage("CPU I/O", 6));
                            v.Add(new Voltage("3VSB", 7, 34, 34));
                            v.Add(new Voltage("VTT", 9));
                            v.Add(new Voltage("CPU SA", 10));
                            v.Add(new Voltage("Voltage #12", 11, true));
                            v.Add(new Voltage("PCH", 12));
                            v.Add(new Voltage("DIMM", 13, 1, 1));
                            v.Add(new Voltage("Voltage #15", 14, true));
                            t.Add(new Temperature("CPU", 0));
                            t.Add(new Temperature("Auxiliary", 1));
                            t.Add(new Temperature("Motherboard", 2));
                            f.Add(new Fan("Pump Fan", 0));
                            f.Add(new Fan("CPU Fan", 1));
                            f.Add(new Fan("System Fan #1", 2));
                            f.Add(new Fan("System Fan #2", 3));
                            f.Add(new Fan("System Fan #3", 4));
                            f.Add(new Fan("System Fan #4", 5));
                            c.Add(new Ctrl("Pump Fan", 0));
                            c.Add(new Ctrl("CPU Fan", 1));
                            c.Add(new Ctrl("System Fan #1", 2));
                            c.Add(new Ctrl("System Fan #2", 3));
                            c.Add(new Ctrl("System Fan #3", 4));
                            c.Add(new Ctrl("System Fan #4", 5));

                            break;
                        }
                        default:
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("Voltage #2", 1, true));
                            v.Add(new Voltage("AVCC", 2, 34, 34));
                            v.Add(new Voltage("+3.3V", 3, 34, 34));
                            v.Add(new Voltage("Voltage #5", 4, true));
                            v.Add(new Voltage("Voltage #6", 5, true));
                            v.Add(new Voltage("Voltage #7", 6, true));
                            v.Add(new Voltage("3VSB", 7, 34, 34));
                            v.Add(new Voltage("VBat", 8, 34, 34));
                            v.Add(new Voltage("VTT", 9));
                            v.Add(new Voltage("Voltage #11", 10, true));
                            v.Add(new Voltage("Voltage #12", 11, true));
                            v.Add(new Voltage("Voltage #13", 12, true));
                            v.Add(new Voltage("Voltage #14", 13, true));
                            v.Add(new Voltage("Voltage #15", 14, true));
                            t.Add(new Temperature("CPU Core", 0));
                            t.Add(new Temperature("Temperature #1", 1));
                            t.Add(new Temperature("Temperature #2", 2));
                            t.Add(new Temperature("Temperature #3", 3));
                            t.Add(new Temperature("Temperature #4", 4));
                            t.Add(new Temperature("Temperature #5", 5));
                            t.Add(new Temperature("Temperature #6", 6));

                            for (int i = 0; i < superIO.Fans.Length; i++)
                                f.Add(new Fan("Fan #" + (i + 1), i));

                            for (int i = 0; i < superIO.Controls.Length; i++)
                                c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                            break;
                        }
                    }

                    break;
                }
                default:
                {
                    v.Add(new Voltage("Vcore", 0));
                    v.Add(new Voltage("Voltage #2", 1, true));
                    v.Add(new Voltage("AVCC", 2, 34, 34));
                    v.Add(new Voltage("+3.3V", 3, 34, 34));
                    v.Add(new Voltage("Voltage #5", 4, true));
                    v.Add(new Voltage("Voltage #6", 5, true));
                    v.Add(new Voltage("Voltage #7", 6, true));
                    v.Add(new Voltage("3VSB", 7, 34, 34));
                    v.Add(new Voltage("VBat", 8, 34, 34));
                    v.Add(new Voltage("VTT", 9));
                    v.Add(new Voltage("Voltage #11", 10, true));
                    v.Add(new Voltage("Voltage #12", 11, true));
                    v.Add(new Voltage("Voltage #13", 12, true));
                    v.Add(new Voltage("Voltage #14", 13, true));
                    v.Add(new Voltage("Voltage #15", 14, true));
                    t.Add(new Temperature("CPU Core", 0));
                    t.Add(new Temperature("Temperature #1", 1));
                    t.Add(new Temperature("Temperature #2", 2));
                    t.Add(new Temperature("Temperature #3", 3));
                    t.Add(new Temperature("Temperature #4", 4));
                    t.Add(new Temperature("Temperature #5", 5));
                    t.Add(new Temperature("Temperature #6", 6));

                    for (int i = 0; i < superIO.Fans.Length; i++)
                        f.Add(new Fan("Fan #" + (i + 1), i));

                    for (int i = 0; i < superIO.Controls.Length; i++)
                        c.Add(new Ctrl("Fan Control #" + (i + 1), i));

                    break;
                }
            }
        }

        private static void GetWinbondConfigurationEhf(Manufacturer manufacturer, Model model, IList<Voltage> v, IList<Temperature> t, IList<Fan> f)
        {
            switch (manufacturer)
            {
                case Manufacturer.ASRock:
                {
                    switch (model)
                    {
                        case Model.AOD790GX_128M: // W83627EHF
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("AVCC", 2, 34, 34));
                            v.Add(new Voltage("+3.3V", 4, 10, 10));
                            v.Add(new Voltage("+5V", 5, 20, 10));
                            v.Add(new Voltage("+12V", 6, 28, 5));
                            v.Add(new Voltage("3VSB", 7, 34, 34));
                            v.Add(new Voltage("VBat", 8, 34, 34));
                            t.Add(new Temperature("CPU", 0));
                            t.Add(new Temperature("Motherboard", 2));
                            f.Add(new Fan("CPU Fan", 0));
                            f.Add(new Fan("Chassis Fan", 1));

                            break;
                        }
                        default:
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("Voltage #2", 1, true));
                            v.Add(new Voltage("AVCC", 2, 34, 34));
                            v.Add(new Voltage("+3.3V", 3, 34, 34));
                            v.Add(new Voltage("Voltage #5", 4, true));
                            v.Add(new Voltage("Voltage #6", 5, true));
                            v.Add(new Voltage("Voltage #7", 6, true));
                            v.Add(new Voltage("3VSB", 7, 34, 34));
                            v.Add(new Voltage("VBat", 8, 34, 34));
                            v.Add(new Voltage("Voltage #10", 9, true));
                            t.Add(new Temperature("CPU", 0));
                            t.Add(new Temperature("Auxiliary", 1));
                            t.Add(new Temperature("System", 2));
                            f.Add(new Fan("System Fan", 0));
                            f.Add(new Fan("CPU Fan", 1));
                            f.Add(new Fan("Auxiliary Fan", 2));
                            f.Add(new Fan("CPU Fan #2", 3));
                            f.Add(new Fan("Auxiliary Fan #2", 4));

                            break;
                        }
                    }

                    break;
                }
                default:
                {
                    v.Add(new Voltage("Vcore", 0));
                    v.Add(new Voltage("Voltage #2", 1, true));
                    v.Add(new Voltage("AVCC", 2, 34, 34));
                    v.Add(new Voltage("+3.3V", 3, 34, 34));
                    v.Add(new Voltage("Voltage #5", 4, true));
                    v.Add(new Voltage("Voltage #6", 5, true));
                    v.Add(new Voltage("Voltage #7", 6, true));
                    v.Add(new Voltage("3VSB", 7, 34, 34));
                    v.Add(new Voltage("VBat", 8, 34, 34));
                    v.Add(new Voltage("Voltage #10", 9, true));
                    t.Add(new Temperature("CPU", 0));
                    t.Add(new Temperature("Auxiliary", 1));
                    t.Add(new Temperature("System", 2));
                    f.Add(new Fan("System Fan", 0));
                    f.Add(new Fan("CPU Fan", 1));
                    f.Add(new Fan("Auxiliary Fan", 2));
                    f.Add(new Fan("CPU Fan #2", 3));
                    f.Add(new Fan("Auxiliary Fan #2", 4));

                    break;
                }
            }
        }

        private static void GetWinbondConfigurationHg(Manufacturer manufacturer, Model model, IList<Voltage> v, IList<Temperature> t, IList<Fan> f)
        {
            switch (manufacturer)
            {
                case Manufacturer.ASRock:
                {
                    switch (model)
                    {
                        case Model._880GMH_USB3: // W83627DHG-P
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("+3.3V", 3, 34, 34));
                            v.Add(new Voltage("+5V", 5, 15, 7.5f));
                            v.Add(new Voltage("+12V", 6, 56, 10));
                            v.Add(new Voltage("3VSB", 7, 34, 34));
                            v.Add(new Voltage("VBat", 8, 34, 34));
                            t.Add(new Temperature("CPU", 0));
                            t.Add(new Temperature("Motherboard", 2));
                            f.Add(new Fan("Chassis Fan", 0));
                            f.Add(new Fan("CPU Fan", 1));
                            f.Add(new Fan("Power Fan", 2));

                            break;
                        }
                        default:
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("Voltage #2", 1, true));
                            v.Add(new Voltage("AVCC", 2, 34, 34));
                            v.Add(new Voltage("+3.3V", 3, 34, 34));
                            v.Add(new Voltage("Voltage #5", 4, true));
                            v.Add(new Voltage("Voltage #6", 5, true));
                            v.Add(new Voltage("Voltage #7", 6, true));
                            v.Add(new Voltage("3VSB", 7, 34, 34));
                            v.Add(new Voltage("VBat", 8, 34, 34));
                            t.Add(new Temperature("CPU", 0));
                            t.Add(new Temperature("Auxiliary", 1));
                            t.Add(new Temperature("System", 2));
                            f.Add(new Fan("System Fan", 0));
                            f.Add(new Fan("CPU Fan", 1));
                            f.Add(new Fan("Auxiliary Fan", 2));
                            f.Add(new Fan("CPU Fan #2", 3));
                            f.Add(new Fan("Auxiliary Fan #2", 4));

                            break;
                        }
                    }

                    break;
                }
                case Manufacturer.ASUS:
                {
                    switch (model)
                    {
                        case Model.P6T: // W83667HG
                        case Model.P6X58D_E: // W83667HG
                        case Model.RAMPAGE_II_GENE: // W83667HG
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("+12V", 1, 11.5f, 1.91f));
                            v.Add(new Voltage("AVCC", 2, 34, 34));
                            v.Add(new Voltage("+3.3V", 3, 34, 34));
                            v.Add(new Voltage("+5V", 4, 15, 7.5f));
                            v.Add(new Voltage("3VSB", 7, 34, 34));
                            v.Add(new Voltage("VBat", 8, 34, 34));
                            t.Add(new Temperature("CPU", 0));
                            t.Add(new Temperature("Motherboard", 2));
                            f.Add(new Fan("Chassis Fan #1", 0));
                            f.Add(new Fan("CPU Fan", 1));
                            f.Add(new Fan("Power Fan", 2));
                            f.Add(new Fan("Chassis Fan #2", 3));
                            f.Add(new Fan("Chassis Fan #3", 4));

                            break;
                        }
                        case Model.RAMPAGE_EXTREME: // W83667HG
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("+12V", 1, 12, 2));
                            v.Add(new Voltage("AVCC", 2, 34, 34));
                            v.Add(new Voltage("+3.3V", 3, 34, 34));
                            v.Add(new Voltage("+5V", 4, 15, 7.5f));
                            v.Add(new Voltage("3VSB", 7, 34, 34));
                            v.Add(new Voltage("VBat", 8, 34, 34));
                            t.Add(new Temperature("CPU", 0));
                            t.Add(new Temperature("Motherboard", 2));
                            f.Add(new Fan("Chassis Fan #1", 0));
                            f.Add(new Fan("CPU Fan", 1));
                            f.Add(new Fan("Power Fan", 2));
                            f.Add(new Fan("Chassis Fan #2", 3));
                            f.Add(new Fan("Chassis Fan #3", 4));

                            break;
                        }
                        default:
                        {
                            v.Add(new Voltage("Vcore", 0));
                            v.Add(new Voltage("Voltage #2", 1, true));
                            v.Add(new Voltage("AVCC", 2, 34, 34));
                            v.Add(new Voltage("+3.3V", 3, 34, 34));
                            v.Add(new Voltage("Voltage #5", 4, true));
                            v.Add(new Voltage("Voltage #6", 5, true));
                            v.Add(new Voltage("Voltage #7", 6, true));
                            v.Add(new Voltage("3VSB", 7, 34, 34));
                            v.Add(new Voltage("VBat", 8, 34, 34));
                            t.Add(new Temperature("CPU", 0));
                            t.Add(new Temperature("Auxiliary", 1));
                            t.Add(new Temperature("System", 2));
                            f.Add(new Fan("System Fan", 0));
                            f.Add(new Fan("CPU Fan", 1));
                            f.Add(new Fan("Auxiliary Fan", 2));
                            f.Add(new Fan("CPU Fan #2", 3));
                            f.Add(new Fan("Auxiliary Fan #2", 4));

                            break;
                        }
                    }

                    break;
                }
                default:
                {
                    v.Add(new Voltage("Vcore", 0));
                    v.Add(new Voltage("Voltage #2", 1, true));
                    v.Add(new Voltage("AVCC", 2, 34, 34));
                    v.Add(new Voltage("+3.3V", 3, 34, 34));
                    v.Add(new Voltage("Voltage #5", 4, true));
                    v.Add(new Voltage("Voltage #6", 5, true));
                    v.Add(new Voltage("Voltage #7", 6, true));
                    v.Add(new Voltage("3VSB", 7, 34, 34));
                    v.Add(new Voltage("VBat", 8, 34, 34));
                    t.Add(new Temperature("CPU", 0));
                    t.Add(new Temperature("Auxiliary", 1));
                    t.Add(new Temperature("System", 2));
                    f.Add(new Fan("System Fan", 0));
                    f.Add(new Fan("CPU Fan", 1));
                    f.Add(new Fan("Auxiliary Fan", 2));
                    f.Add(new Fan("CPU Fan #2", 3));
                    f.Add(new Fan("Auxiliary Fan #2", 4));

                    break;
                }
            }
        }

        public override string GetReport()
        {
            return _superIO.GetReport();
        }

        public override void Update()
        {
            _superIO.Update();

            foreach (Sensor sensor in _voltages)
            {
                float? value = _readVoltage(sensor.Index);
                if (value.HasValue)
                {
                    sensor.Value = value + (value - sensor.Parameters[2].Value) * sensor.Parameters[0].Value / sensor.Parameters[1].Value;
                    ActivateSensor(sensor);
                }
            }

            foreach (Sensor sensor in _temperatures)
            {
                float? value = _readTemperature(sensor.Index);
                if (value.HasValue)
                {
                    sensor.Value = value + sensor.Parameters[0].Value;
                    ActivateSensor(sensor);
                }
            }

            foreach (Sensor sensor in _fans)
            {
                float? value = _readFan(sensor.Index);
                if (value.HasValue)
                {
                    sensor.Value = value;
                    ActivateSensor(sensor);
                }
            }

            foreach (Sensor sensor in _controls)
            {
                float? value = _readControl(sensor.Index);
                sensor.Value = value;
            }

            _postUpdate();
        }

        public override void Close()
        {
            foreach (Sensor sensor in _controls)
            {
                // restore all controls back to default
                _superIO.SetControl(sensor.Index, null);
            }

            base.Close();
        }

        private delegate float? ReadValueDelegate(int index);

        private delegate void UpdateDelegate();

        private class Voltage
        {
            public readonly bool Hidden;
            public readonly int Index;
            public readonly string Name;
            public readonly float Rf;
            public readonly float Ri;
            public readonly float Vf;

            public Voltage(string name, int index, bool hidden = false) : this(name, index, 0, 1, 0, hidden)
            { }

            public Voltage(string name, int index, float ri, float rf, float vf = 0, bool hidden = false)
            {
                Name = name;
                Index = index;
                Ri = ri;
                Rf = rf;
                Vf = vf;
                Hidden = hidden;
            }
        }

        private class Temperature
        {
            public readonly int Index;
            public readonly string Name;

            public Temperature(string name, int index)
            {
                Name = name;
                Index = index;
            }
        }

        private class Fan
        {
            public readonly int Index;
            public readonly string Name;

            public Fan(string name, int index)
            {
                Name = name;
                Index = index;
            }
        }

        private class Ctrl
        {
            public readonly int Index;
            public readonly string Name;

            public Ctrl(string name, int index)
            {
                Name = name;
                Index = index;
            }
        }
    }
}
