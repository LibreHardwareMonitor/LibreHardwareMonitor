// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using LibreHardwareMonitor.Hardware.Motherboard.Lpc;

namespace LibreHardwareMonitor.Hardware.Motherboard;

internal sealed class SuperIOHardware : Hardware
{
    private readonly List<Sensor> _controls = new();
    private readonly List<Sensor> _fans = new();
    private readonly Motherboard _motherboard;

    private readonly UpdateDelegate _postUpdate;
    private readonly ReadValueDelegate _readControl;
    private readonly ReadValueDelegate _readFan;
    private readonly ReadValueDelegate _readTemperature;
    private readonly ReadValueDelegate _readVoltage;

    private readonly ISuperIO _superIO;
    private readonly List<Sensor> _temperatures = new();
    private readonly List<Sensor> _voltages = new();

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
                                      out IList<Control> c,
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

    private void CreateControlSensors(ISuperIO superIO, ISettings settings, IList<Control> c)
    {
        foreach (Control ctrl in c)
        {
            int index = ctrl.Index;
            if (index < superIO.Controls.Length)
            {
                Sensor sensor = new(ctrl.Name, index, SensorType.Control, this, settings);
                LibreHardwareMonitor.Hardware.Control control = new(sensor, settings, 0, 100);
                control.ControlModeChanged += cc =>
                {
                    switch (cc.ControlMode)
                    {
                        case ControlMode.Undefined:
                            return;
                        case ControlMode.Default:
                            superIO.SetControl(index, null);
                            break;
                        case ControlMode.Software:
                            superIO.SetControl(index, GetSoftwareValueAsByte(cc));
                            break;
                        default:
                            return;
                    }
                };

                control.SoftwareControlValueChanged += cc =>
                {
                    if (cc.ControlMode == ControlMode.Software)
                        superIO.SetControl(index, GetSoftwareValueAsByte(cc));
                };

                switch (control.ControlMode)
                {
                    case ControlMode.Undefined:
                        break;
                    case ControlMode.Default:
                        superIO.SetControl(index, null);
                        break;
                    case ControlMode.Software:
                        superIO.SetControl(index, GetSoftwareValueAsByte(control));
                        break;
                }

                sensor.Control = control;
                _controls.Add(sensor);
                ActivateSensor(sensor);
            }
        }
    }

    private static byte GetSoftwareValueAsByte(LibreHardwareMonitor.Hardware.Control control)
    {
        const float percentToByteRatio = 2.55f;
        float value = control.SoftwareValue * percentToByteRatio;
        return (byte)value;
    }

    private void CreateFanSensors(ISuperIO superIO, ISettings settings, IList<Fan> f)
    {
        foreach (Fan fan in f)
        {
            if (fan.Index < superIO.Fans.Length)
            {
                Sensor sensor = new(fan.Name, fan.Index, SensorType.Fan, this, settings);
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
                Sensor sensor = new(temperature.Name,
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
                Sensor sensor = new(voltage.Name,
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
        out IList<Control> c,
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
        c = new List<Control>();

        switch (superIO.Chip)
        {
            case Chip.IT8705F:
            case Chip.IT8712F:
            case Chip.IT8716F:
            case Chip.IT8718F:
            case Chip.IT8720F:
            case Chip.IT8726F:
                GetIteConfigurationsA(superIO, manufacturer, model, v, t, f, c, ref readFan, ref postUpdate, ref mutex);
                break;

            case Chip.IT8613E:
            case Chip.IT8620E:
            case Chip.IT8625E:
            case Chip.IT8628E:
            case Chip.IT8631E:
            case Chip.IT8655E:
            case Chip.IT8665E:
            case Chip.IT8686E:
            case Chip.IT8688E:
            case Chip.IT8689E:
            case Chip.IT8721F:
            case Chip.IT8728F:
            case Chip.IT8771E:
            case Chip.IT8772E:
                GetIteConfigurationsB(superIO, manufacturer, model, v, t, f, c);
                break;

            case Chip.IT87952E:
            case Chip.IT8792E:
            case Chip.IT8790E:
                GetIteConfigurationsC(superIO, manufacturer, model, v, t, f, c);
                break;

            case Chip.F71858:
                v.Add(new Voltage("VCC3V", 0, 150, 150));
                v.Add(new Voltage("VSB3V", 1, 150, 150));
                v.Add(new Voltage("Battery", 2, 150, 150));

                for (int i = 0; i < superIO.Temperatures.Length; i++)
                    t.Add(new Temperature("Temperature #" + (i + 1), i));

                for (int i = 0; i < superIO.Fans.Length; i++)
                    f.Add(new Fan("Fan #" + (i + 1), i));

                break;

            case Chip.F71808E:
            case Chip.F71862:
            case Chip.F71869:
            case Chip.F71869A:
            case Chip.F71882:
            case Chip.F71889AD:
            case Chip.F71889ED:
            case Chip.F71889F:
                GetFintekConfiguration(superIO, manufacturer, model, v, t, f, c);
                break;

            case Chip.W83627EHF:
                GetWinbondConfigurationEhf(manufacturer, model, v, t, f, c);
                break;

            case Chip.W83627DHG:
            case Chip.W83627DHGP:
            case Chip.W83667HG:
            case Chip.W83667HGB:
                GetWinbondConfigurationHg(manufacturer, model, v, t, f, c);
                break;

            case Chip.W83627HF:
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
                c.Add(new Control("Fan 1", 0));
                c.Add(new Control("Fan 2", 1));
                break;

            case Chip.W83627THF:
            case Chip.W83687THF:
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
                c.Add(new Control("System Fan", 0));
                c.Add(new Control("CPU Fan", 1));
                c.Add(new Control("Auxiliary Fan", 2));
                break;

            case Chip.NCT6771F:
            case Chip.NCT6776F:
                GetNuvotonConfigurationF(superIO, manufacturer, model, v, t, f, c);
                break;

            case Chip.NCT610XD:
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
                    c.Add(new Control("Fan #" + (i + 1), i));

                break;

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
            case Chip.NCT6799D:
            case Chip.NCT6683D:
                GetNuvotonConfigurationD(superIO, manufacturer, model, v, t, f, c);
                break;

            case Chip.NCT6686D:
            case Chip.NCT6687D:
                switch (manufacturer)
                {
                    case Manufacturer.ASRock when model == Model.Z790_Taichi:
                        t.Add(new Temperature("CPU", 0));
                        t.Add(new Temperature("Motherboard", 1));
                        t.Add(new Temperature("MOS", 2));

                        f.Add(new Fan("CPU Fan #1", 0));
                        f.Add(new Fan("Chassis Fan #4", 1));
                        f.Add(new Fan("CPU Fan #2", 2));
                        f.Add(new Fan("Chassis Fan #2", 3));
                        f.Add(new Fan("Chassis Fan #1", 4));
                        f.Add(new Fan("MOS Fan #1", 5));

                        c.Add(new Control("CPU Fan #1", 0));
                        c.Add(new Control("Chassis Fan #4", 1));
                        c.Add(new Control("CPU Fan #2", 2));
                        c.Add(new Control("Chassis Fan #2", 3));
                        c.Add(new Control("Chassis Fan #1", 4));
                        c.Add(new Control("MOS Fan #1", 5));
                        break;
                    default:
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

                        c.Add(new Control("CPU Fan", 0));
                        c.Add(new Control("Pump Fan", 1));
                        c.Add(new Control("System Fan #1", 2));
                        c.Add(new Control("System Fan #2", 3));
                        c.Add(new Control("System Fan #3", 4));
                        c.Add(new Control("System Fan #4", 5));
                        c.Add(new Control("System Fan #5", 6));
                        c.Add(new Control("System Fan #6", 7));

                        break;
                }

                break;

            case Chip.IPMI:
                Ipmi ipmi = (Ipmi)superIO;

                foreach (Temperature temperature in ipmi.GetTemperatures())
                    t.Add(temperature);

                foreach (Fan fan in ipmi.GetFans())
                    f.Add(fan);

                foreach (Voltage voltage in ipmi.GetVoltages())
                    v.Add(voltage);

                foreach (Control control in ipmi.GetControls())
                    c.Add(control);

                break;

            default:
                GetDefaultConfiguration(superIO, v, t, f, c);
                break;
        }
    }

    private static void GetDefaultConfiguration(ISuperIO superIO, ICollection<Voltage> v, ICollection<Temperature> t, ICollection<Fan> f, ICollection<Control> c)
    {
        for (int i = 0; i < superIO.Voltages.Length; i++)
            v.Add(new Voltage("Voltage #" + (i + 1), i, true));

        for (int i = 0; i < superIO.Temperatures.Length; i++)
            t.Add(new Temperature("Temperature #" + (i + 1), i));

        for (int i = 0; i < superIO.Fans.Length; i++)
            f.Add(new Fan("Fan #" + (i + 1), i));

        for (int i = 0; i < superIO.Controls.Length; i++)
            c.Add(new Control("Fan #" + (i + 1), i));
    }

    private static void GetIteConfigurationsA
    (
        ISuperIO superIO,
        Manufacturer manufacturer,
        Model model,
        IList<Voltage> v,
        IList<Temperature> t,
        IList<Fan> f,
        ICollection<Control> c,
        ref ReadValueDelegate readFan,
        ref UpdateDelegate postUpdate,
        ref Mutex mutex)
    {
        switch (manufacturer)
        {
            case Manufacturer.ASUS:
                switch (model)
                {
                    case Model.CROSSHAIR_III_FORMULA: // IT8720F
                        v.Add(new Voltage("VBat", 8));
                        t.Add(new Temperature("CPU", 0));

                        for (int i = 0; i < superIO.Fans.Length; i++)
                            f.Add(new Fan("Fan #" + (i + 1), i));

                        break;

                    case Model.M2N_SLI_Deluxe:
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

                    case Model.M4A79XTD_EVO: // IT8720F
                        v.Add(new Voltage("+5V", 3, 6.8f, 10));
                        v.Add(new Voltage("VBat", 8));
                        t.Add(new Temperature("CPU", 0));
                        t.Add(new Temperature("Motherboard", 1));
                        f.Add(new Fan("CPU Fan", 0));
                        f.Add(new Fan("Chassis Fan #1", 1));
                        f.Add(new Fan("Chassis Fan #2", 2));

                        break;

                    case Model.PRIME_X370_PRO: // IT8665E
                    case Model.TUF_X470_PLUS_GAMING:
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

                    case Model.ROG_ZENITH_EXTREME: // IT8665E
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
                            c.Add(new Control("Fan #" + (i + 1), i));

                        break;

                    default:
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
                            c.Add(new Control("Fan #" + (i + 1), i));

                        break;
                }

                break;
            case Manufacturer.ASRock:
                switch (model)
                {
                    case Model.P55_Deluxe: // IT8720F
                        GetASRockConfiguration(superIO,
                                               v,
                                               t,
                                               f,
                                               ref readFan,
                                               ref postUpdate,
                                               out mutex);

                        break;

                    default:
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

                break;
            case Manufacturer.DFI:
                switch (model)
                {
                    case Model.LP_BI_P45_T2RS_Elite: // IT8718F
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

                    case Model.LP_DK_P55_T3EH9: // IT8720F
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

                    default:
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
                            c.Add(new Control("Fan #" + (i + 1), i));

                        break;
                }

                break;
            case Manufacturer.Gigabyte:
                switch (model)
                {
                    case Model._965P_S3: // IT8718F
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

                    case Model.EP45_DS3R: // IT8718F
                    case Model.EP45_UD3R:
                    case Model.X38_DS5:
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

                    case Model.EX58_EXTREME: // IT8720F
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

                    case Model.P35_DS3: // IT8718F
                    case Model.P35_DS3L: // IT8718F
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

                    case Model.P55_UD4: // IT8720F
                    case Model.P55A_UD3: // IT8720F
                    case Model.P55M_UD4: // IT8720F
                    case Model.H55_USB3: // IT8720F
                    case Model.EX58_UD3R: // IT8720F
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

                    case Model.H55N_USB3: // IT8720F
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

                    case Model.G41M_COMBO: // IT8718F
                    case Model.G41MT_S2: // IT8718F
                    case Model.G41MT_S2P: // IT8718F
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

                    case Model._970A_UD3: // IT8720F
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
                        c.Add(new Control("PWM #1", 0));
                        c.Add(new Control("PWM #2", 1));
                        c.Add(new Control("PWM #3", 2));

                        break;

                    case Model.MA770T_UD3: // IT8720F
                    case Model.MA770T_UD3P: // IT8720F
                    case Model.MA790X_UD3P: // IT8720F
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

                    case Model.MA78LM_S2H: // IT8718F
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

                    case Model.MA785GM_US2H: // IT8718F
                    case Model.MA785GMT_UD2H: // IT8718F
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

                    case Model.X58A_UD3R: // IT8720F
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

                    default:
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
                            c.Add(new Control("Fan #" + (i + 1), i));

                        break;
                }

                break;

            default:
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
                    c.Add(new Control("Fan #" + (i + 1), i));

                break;
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

    private static void GetIteConfigurationsB(ISuperIO superIO, Manufacturer manufacturer, Model model, IList<Voltage> v, IList<Temperature> t, IList<Fan> f, IList<Control> c)
    {
        switch (manufacturer)
        {
            case Manufacturer.ASUS:
                switch (model)
                {
                    case Model.ROG_STRIX_X470_I: // IT8665E
                        v.Add(new Voltage("Vcore", 0));
                        v.Add(new Voltage("SB 2.5V", 1));
                        v.Add(new Voltage("+12V", 2, 5, 1));
                        v.Add(new Voltage("+5V", 3, 1.5f, 1));
                        v.Add(new Voltage("+3.3V", 7, 10, 10));
                        v.Add(new Voltage("VBat", 8, 10, 10));
                        t.Add(new Temperature("CPU", 0));
                        t.Add(new Temperature("Motherboard", 1));
                        t.Add(new Temperature("T_Sensor", 2));
                        t.Add(new Temperature("PCIe x16", 3));
                        t.Add(new Temperature("VRM", 4));
                        t.Add(new Temperature("Temperature #6", 5));

                        f.Add(new Fan("CPU Fan", 0));

                        //Does not work when in AIO pump mode (shows 0). I don't know how to fix it.
                        f.Add(new Fan("Chassis Fan #1", 1));
                        f.Add(new Fan("Chassis Fan #2", 2));

                        for (int i = 0; i < superIO.Controls.Length; i++)
                            c.Add(new Control("Fan #" + i, i));

                        break;

                    default:
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
                            c.Add(new Control("Fan #" + (i + 1), i));

                        break;
                }

                break;
            case Manufacturer.ECS:
                switch (model)
                {
                    case Model.A890GXM_A: // IT8721F
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

                    default:
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
                            c.Add(new Control("Fan #" + (i + 1), i));

                        break;
                }

                break;
            case Manufacturer.Gigabyte:
                switch (model)
                {
                    case Model.H61M_DS2_REV_1_2: // IT8728F
                    case Model.H61M_USB3_B3_REV_2_0: // IT8728F
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

                    case Model.H67A_UD3H_B3: // IT8728F
                    case Model.H67A_USB3_B3: // IT8728F
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

                    case Model.B75M_D3H: // IT8728F
                        v.Add(new Voltage("VTT", 0));
                        v.Add(new Voltage("+3.3V", 1, 6.49f, 10));
                        v.Add(new Voltage("+5V", 3, 15, 10));
                        v.Add(new Voltage("+12V", 2, 10, 2));
                        v.Add(new Voltage("iGPU VAXG", 4));
                        v.Add(new Voltage("Vcore", 5));
                        v.Add(new Voltage("DIMM", 6));
                        v.Add(new Voltage("3VSB", 7, 10, 10));
                        v.Add(new Voltage("VBat", 8, 10, 10));
                        t.Add(new Temperature("System", 0));
                        t.Add(new Temperature("CPU", 2));
                        f.Add(new Fan("CPU Fan", 0));
                        f.Add(new Fan("System Fan", 1));
                        c.Add(new Control("CPU Fan", 2));
                        c.Add(new Control("System Fan", 1));

                        break;

                    case Model.H81M_HD3: //IT8620E
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
                        c.Add(new Control("CPU Fan", 0));
                        c.Add(new Control("System Fan", 1));

                        break;

                    case Model.Z170N_WIFI: // ITE IT8628E
                        v.Add(new Voltage("Vcore", 0, 0, 1));
                        v.Add(new Voltage("+3.3V", 1, 6.5F, 10));
                        v.Add(new Voltage("+12V", 2, 5, 1));
                        v.Add(new Voltage("+5V", 3, 1.5F, 1));
                        // NO DIMM CD channels on this motherboard; gives a very tiny voltage reading
                        // v.Add(new Voltage("DIMM CD", 4, 0, 1));
                        v.Add(new Voltage("iGPU VAXG", 5, 0, 1));
                        v.Add(new Voltage("DIMM AB", 6, 0, 1));
                        v.Add(new Voltage("3VSB", 7, 10, 10));
                        v.Add(new Voltage("VBat", 8, 10, 10));
                        v.Add(new Voltage("AVCC3", 9, 54, 10));

                        t.Add(new Temperature("System #1", 0));
                        t.Add(new Temperature("PCH", 1));
                        t.Add(new Temperature("CPU", 2));
                        t.Add(new Temperature("PCIe x16", 3));
                        t.Add(new Temperature("VRM", 4));
                        t.Add(new Temperature("System #2", 5));

                        f.Add(new Fan("CPU Fan", 0));
                        f.Add(new Fan("System Fan", 1));

                        c.Add(new Control("CPU Fan", 0));
                        c.Add(new Control("System Fan", 1));

                        break;

                    case Model.AX370_Gaming_K7: // IT8686E
                    case Model.AX370_Gaming_5:
                    case Model.AB350_Gaming_3: // IT8686E
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

                    case Model.X399_AORUS_Gaming_7: // ITE IT8686E
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
                            c.Add(new Control("Fan #" + (i + 1), i));

                        break;

                    case Model.X470_AORUS_GAMING_7_WIFI: // ITE IT8686E
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
                            c.Add(new Control("Fan #" + (i + 1), i));

                        break;

                    case Model.B560M_AORUS_ELITE: // IT8689E
                    case Model.B560M_AORUS_PRO:
                    case Model.B560M_AORUS_PRO_AX:
                        v.Add(new Voltage("Vcore", 0));
                        v.Add(new Voltage("+3.3V", 1, 29.4f, 45.3f));
                        v.Add(new Voltage("+12V", 2, 10f, 2f));
                        v.Add(new Voltage("+5V", 3, 15f, 10f));
                        v.Add(new Voltage("iGPU VAGX", 4));
                        v.Add(new Voltage("VCCSA", 5));
                        v.Add(new Voltage("DRAM", 6));
                        v.Add(new Voltage("3VSB", 7, 10f, 10f));
                        v.Add(new Voltage("VBat", 8, 10f, 10f));
                        v.Add(new Voltage("AVCC3", 9, 59.9f, 9.8f));
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
                        f.Add(new Fan("CPU Optional Fan", 4));
                        c.Add(new Control("CPU Fan", 0));
                        c.Add(new Control("System Fan #1", 1));
                        c.Add(new Control("System Fan #2", 2));
                        c.Add(new Control("System Fan #3", 3));
                        c.Add(new Control("CPU Optional Fan", 4));

                        break;

                    case Model.B360_AORUS_GAMING_3_WIFI_CF: // IT8688E
                        v.Add(new Voltage("Vcore", 0));
                        v.Add(new Voltage("+3.3V", 1, 29.4f, 45.3f));
                        v.Add(new Voltage("+12V", 2, 10f, 2f));
                        v.Add(new Voltage("+5V", 3, 15f, 10f));
                        v.Add(new Voltage("CPU Vcore", 4, 0, 1));
                        v.Add(new Voltage("CPU VCCSA", 5, 0, 1));
                        v.Add(new Voltage("DIMM AB", 6, 0, 1));
                        v.Add(new Voltage("3VSB", 7, 1, 1));
                        v.Add(new Voltage("VBat", 8, 1, 1));
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
                        f.Add(new Fan("CPU Optional Fan", 4));
                        c.Add(new Control("CPU Fan", 0));
                        c.Add(new Control("System Fan #1", 1));
                        c.Add(new Control("System Fan #2", 2));
                        c.Add(new Control("PCH Fan", 3));
                        c.Add(new Control("CPU Optional Fan", 4));

                        break;

                    case Model.X570_AORUS_MASTER: // IT8688E
                    case Model.X570_AORUS_ULTRA:
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
                        f.Add(new Fan("CPU Optional Fan", 4));
                        c.Add(new Control("CPU Fan", 0));
                        c.Add(new Control("System Fan #1", 1));
                        c.Add(new Control("System Fan #2", 2));
                        c.Add(new Control("PCH Fan", 3));
                        c.Add(new Control("CPU Optional Fan", 4));

                        break;

                    case Model.X570_GAMING_X: // IT8688E
                        v.Add(new Voltage("Vcore", 0));
                        v.Add(new Voltage("+3.3V", 1, 29.4f, 45.3f));
                        v.Add(new Voltage("+12V", 2, 10f, 2f));
                        v.Add(new Voltage("+5V", 3, 15f, 10f));
                        v.Add(new Voltage("Vcore SoC", 4));
                        v.Add(new Voltage("VDDP", 5));
                        v.Add(new Voltage("DIMM AB", 6));
                        t.Add(new Temperature("System #1", 0));
                        t.Add(new Temperature("System #2", 1));
                        t.Add(new Temperature("CPU", 2));
                        t.Add(new Temperature("PCIe x16", 3));
                        t.Add(new Temperature("VRM MOS", 4));
                        t.Add(new Temperature("PCH", 5));
                        f.Add(new Fan("CPU Fan", 0));
                        f.Add(new Fan("System Fan #1", 1));
                        f.Add(new Fan("System Fan #2", 2));
                        f.Add(new Fan("PCH Fan", 3));
                        f.Add(new Fan("CPU Optional Fan", 4));
                        c.Add(new Control("CPU Fan", 0));
                        c.Add(new Control("System Fan #1", 1));
                        c.Add(new Control("System Fan #2", 2));
                        c.Add(new Control("PCH Fan", 3));
                        c.Add(new Control("CPU Optional Fan", 4));

                        break;

                    case Model.Z390_M_GAMING: // IT8688E
                    case Model.Z390_AORUS_ULTRA:
                    case Model.Z390_UD:
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
                        c.Add(new Control("CPU Fan", 0));
                        c.Add(new Control("System Fan #1", 1));
                        c.Add(new Control("System Fan #2", 2));
                        c.Add(new Control("System Fan #3", 3));

                        break;

                    case Model.Z390_AORUS_PRO:
                        v.Add(new Voltage("Vcore", 0));
                        v.Add(new Voltage("+3.3V", 1, 6.49f, 10));
                        v.Add(new Voltage("+12V", 2, 5f, 1));
                        v.Add(new Voltage("+5V", 3, 1.5f, 1));
                        v.Add(new Voltage("CPU VCCGT", 4));
                        v.Add(new Voltage("CPU VCCSA", 5));
                        v.Add(new Voltage("DDR", 6));
                        v.Add(new Voltage("Voltage #7", 7, true));
                        v.Add(new Voltage("3VSB", 8, 1f, 1f, -0.312f));
                        v.Add(new Voltage("VBat", 9, 6f, 1f, 0.01f));
                        v.Add(new Voltage("AVCC3", 10, 6f, 1f, 0.048f));
                        t.Add(new Temperature("System #1", 0));
                        t.Add(new Temperature("PCH", 1));
                        t.Add(new Temperature("CPU", 2));
                        t.Add(new Temperature("PCIe x16", 3));
                        t.Add(new Temperature("VRM MOS", 4));
                        t.Add(new Temperature("EC_TEMP1/System #2", 5));
                        f.Add(new Fan("CPU Fan", 0));
                        f.Add(new Fan("System Fan #1", 1));
                        f.Add(new Fan("System Fan #2", 2));
                        f.Add(new Fan("System Fan #3", 3));
                        f.Add(new Fan("CPU Optional Fan", 4));
                        c.Add(new Control("CPU Fan", 0));
                        c.Add(new Control("System Fan #1", 1));
                        c.Add(new Control("System Fan #2", 2));
                        c.Add(new Control("System Fan #3", 3));
                        c.Add(new Control("CPU Optional Fan", 4));

                        break;

                    case Model.Z690_AORUS_PRO:
                    case Model.Z690_AORUS_ULTRA: // ITE IT8689E
                        v.Add(new Voltage("Vcore", 0));
                        v.Add(new Voltage("+3.3V", 1, 6.49f, 10));
                        v.Add(new Voltage("+12V", 2, 5f, 1));
                        v.Add(new Voltage("+5V", 3, 1.5f, 1));
                        v.Add(new Voltage("iGPU VAXG", 4));
                        v.Add(new Voltage("CPU VCCIN_AUX", 5));
                        v.Add(new Voltage("Voltage #6", 6, true));
                        v.Add(new Voltage("3VSB", 7, 1f, 1f));
                        v.Add(new Voltage("VBat", 8, 1f, 1f));
                        v.Add(new Voltage("AVCC3", 9, true));
                        t.Add(new Temperature("System #1", 0));
                        t.Add(new Temperature("PCH", 1));
                        t.Add(new Temperature("CPU", 2));
                        t.Add(new Temperature("PCIe x16", 3));
                        t.Add(new Temperature("VRM MOS", 4));
                        t.Add(new Temperature("EC_TEMP1", 5));
                        f.Add(new Fan("CPU Fan", 0));
                        f.Add(new Fan("System Fan #1", 1));
                        f.Add(new Fan("System Fan #2", 2));
                        f.Add(new Fan("System Fan #3", 3));
                        f.Add(new Fan("CPU Optional Fan", 4));
                        c.Add(new Control("CPU Fan", 0));
                        c.Add(new Control("System Fan #1", 1));
                        c.Add(new Control("System Fan #2", 2));
                        c.Add(new Control("System Fan #3", 3));
                        c.Add(new Control("CPU Optional Fan", 4));
                        break;

                    case Model.Z690_GAMING_X_DDR4:
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
                        f.Add(new Fan("CPU Optional Fan", 4));
                        f.Add(new Fan("System Fan #4 / Pump", 5));
                        c.Add(new Control("CPU Fan", 0));
                        c.Add(new Control("System Fan #1", 1));
                        c.Add(new Control("System Fan #2", 2));
                        c.Add(new Control("System Fan #3", 3));
                        c.Add(new Control("CPU Optional Fan", 4));
                        c.Add(new Control("System Fan #4 / Pump", 5));
                        break;

                    case Model.Z68A_D3H_B3: // IT8728F
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

                    case Model.P67A_UD3_B3: // IT8728F
                    case Model.P67A_UD3R_B3: // IT8728F
                    case Model.P67A_UD4_B3: // IT8728F
                    case Model.Z68AP_D3: // IT8728F
                    case Model.Z68X_UD3H_B3: // IT8728F
                    case Model.Z68XP_UD3R: // IT8728F
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

                    case Model.Z68X_UD7_B3: // IT8728F
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

                    case Model.X79_UD3: // IT8728F
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

                    case Model.B550_AORUS_PRO:
                        v.Add(new Voltage("Vcore", 0, 0, 1));
                        v.Add(new Voltage("+3.3V", 1, 6.5F, 10));
                        v.Add(new Voltage("+12V", 2, 5, 1));
                        v.Add(new Voltage("+5V", 3, 1.5F, 1));
                        v.Add(new Voltage("Vcore SoC", 4, 0, 1));
                        v.Add(new Voltage("VDDP", 5, 0, 1));
                        v.Add(new Voltage("DRAM", 6, 0, 1));
                        v.Add(new Voltage("3VSB", 7, 10, 10));
                        v.Add(new Voltage("VBat", 8, 10, 10));
                        t.Add(new Temperature("System #1", 0));
                        t.Add(new Temperature("Chipset", 1));
                        t.Add(new Temperature("CPU", 2));
                        t.Add(new Temperature("PCIe x16", 3));
                        t.Add(new Temperature("VRM", 4));
                        t.Add(new Temperature("PCH", 5));
                        f.Add(new Fan("CPU Fan", 0));
                        f.Add(new Fan("System Fan #1", 1));
                        f.Add(new Fan("System Fan #2", 2));
                        f.Add(new Fan("System Fan #3", 3));
                        f.Add(new Fan("CPU Optional Fan", 4));
                        c.Add(new Control("CPU Fan", 0));
                        c.Add(new Control("System Fan #1", 1));
                        c.Add(new Control("System Fan #2", 2));
                        c.Add(new Control("System Fan #3", 3));
                        c.Add(new Control("CPU Optional Fan", 4));

                        break;

                    case Model.B660M_DS3H_AX_DDR4:
                        v.Add(new Voltage("Vcore",0));
                        v.Add(new Voltage("VAXG",1));
                        v.Add(new Voltage("VCCIN AUX",2));
                        v.Add(new Voltage("DIMM AB",3));
                        v.Add(new Voltage("+12V",4));
                        v.Add(new Voltage("+3.3V",5));
                        v.Add(new Voltage("+5V",6));
                        t.Add(new Temperature("CPU",0));
                        t.Add(new Temperature("PCH",1));
                        t.Add(new Temperature("PCIEX16",2));
                        t.Add(new Temperature("System #1",3));
                        t.Add(new Temperature("System #2",4));
                        t.Add(new Temperature("VRAM MOS",5));
                        f.Add(new Fan("CPU Fan",0));
                        f.Add(new Fan("System Fan #1",2));
                        f.Add(new Fan("System Fan #2",3));
                        f.Add(new Fan("System Fan #3",4));
                        c.Add(new Control("CPU Fan",0));
                        c.Add(new Control("System Fan #1",2));
                        c.Add(new Control("System Fan #2",3));
                        c.Add(new Control("System Fan #3",4));
                        break;

                    default:
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
                            c.Add(new Control("Fan #" + (i + 1), i));

                        break;
                }

                break;
            case Manufacturer.Biostar:
                switch (model)
                {
                    case Model.B660GTN: //IT8613E
                        // This board has some problems with their app controlling fans that I was able to replicate here so I guess is a BIOS problem with the pins.
                        // Biostar is aware so expect changes in the control pins with new bios.
                        // In the meantime, it's possible to control CPUFAN and CPUOPT1m but not SYSFAN1.
                        // The parameters are extracted from the Biostar app config file.
                        v.Add(new Voltage("Vcore", 0, 0, 1));
                        v.Add(new Voltage("DIMM", 1, 0, 1));
                        v.Add(new Voltage("+12V", 2, 5, 1)); // Reads higher than it should.
                        v.Add(new Voltage("+5V", 3, 147, 100)); // Reads higher than it should.
                        // Commented because I don't know if it makes sense.
                        //v.Add(new Voltage("VCC ST", 4)); // Reads 4.2V.
                        //v.Add(new Voltage("VCCIN AUX", 5)); // Reads 2.2V.
                        //v.Add(new Voltage("CPU GT", 6)); // Reads 2.6V.
                        //v.Add(new Voltage("3VSB", 7, 10, 10)); // Reads 5.8V ?
                        v.Add(new Voltage("VBat", 8, 10, 10)); // Reads higher than it should at 3.4V.
                        t.Add(new Temperature("System 1", 0));
                        t.Add(new Temperature("System 2", 1)); // Not sure what sensor is this.
                        t.Add(new Temperature("CPU", 2));
                        f.Add(new Fan("CPU Fan", 1));
                        f.Add(new Fan("CPU Optional fan", 2));
                        f.Add(new Fan("System Fan", 4));
                        c.Add(new Control("CPU Fan", 1));
                        c.Add(new Control("CPU Optional Fan", 2));
                        c.Add(new Control("System Fan", 4));

                        break;

                    case Model.X670E_Valkyrie: //IT8625E
                        v.Add(new Voltage("Vcore", 0));
                        v.Add(new Voltage("DIMM", 1));
                        v.Add(new Voltage("+12V", 2, 10, 2));
                        // Voltage of unknown use
                        v.Add(new Voltage("Voltage #4", 3, true));
                        // The biostar utility shows CPU MISC Voltage.
                        v.Add(new Voltage("Voltage #5", 4));
                        v.Add(new Voltage("VDDP", 5));
                        v.Add(new Voltage("VSOC", 6));

                        t.Add(new Temperature("CPU", 0));
                        t.Add(new Temperature("VRM", 1));
                        t.Add(new Temperature("System", 2));

                        f.Add(new Fan("CPU Fan", 0));
                        f.Add(new Fan("CPU Optional Fan", 1));
                        for (int i = 2; i < superIO.Fans.Length; i++)
                            f.Add(new Fan($"System Fan #{i - 1}", i));

                        c.Add(new Control("CPU Fan", 0));
                        c.Add(new Control("CPU Optional Fan", 1));
                        for (int i = 2; i < superIO.Controls.Length; i++)
                            c.Add(new Control($"System Fan #{i - 1}", i));

                        break;

                    default:
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
                            c.Add(new Control("Fan #" + (i + 1), i));

                        break;
                }

                break;
            case Manufacturer.Shuttle:
                switch (model)
                {
                    case Model.FH67: // IT8772E
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

                    default:
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
                            c.Add(new Control("Fan #" + (i + 1), i));

                        break;
                }

                break;

            default:
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
                    c.Add(new Control("Fan #" + (i + 1), i));

                break;
        }
    }

    private static void GetIteConfigurationsC(ISuperIO superIO, Manufacturer manufacturer, Model model, IList<Voltage> v, IList<Temperature> t, IList<Fan> f, IList<Control> c)
    {
        switch (manufacturer)
        {
            case Manufacturer.Gigabyte:
                switch (model)
                {
                    case Model.X570_AORUS_MASTER: // IT879XE
                    case Model.X570_AORUS_ULTRA:
                    case Model.B550_AORUS_PRO:
                        v.Add(new Voltage("VIN0", 0));
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
                        c.Add(new Control("System Fan #5 Pump", 0));
                        c.Add(new Control("System Fan #6 Pump", 1));
                        c.Add(new Control("System Fan #4", 2));

                        break;

                    case Model.X470_AORUS_GAMING_7_WIFI: // ITE IT8792
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
                            c.Add(new Control("Fan #" + (i + 1), i));

                        break;

                    case Model.Z390_AORUS_PRO: // IT879XE
                        v.Add(new Voltage("VCore", 0));
                        v.Add(new Voltage("DDRVTT AB", 1));
                        v.Add(new Voltage("Chipset Core", 2));
                        v.Add(new Voltage("VIN3", 3, true));
                        v.Add(new Voltage("VCCIO", 4));
                        v.Add(new Voltage("Voltage #7", 5, true));
                        v.Add(new Voltage("DDR VPP", 6));
                        v.Add(new Voltage("3VSB", 7, 1f, 1f));
                        v.Add(new Voltage("VBat", 8, 1f, 1f));
                        t.Add(new Temperature("PCIe x8", 0));
                        t.Add(new Temperature("EC_TEMP2", 1));
                        t.Add(new Temperature("System #2", 2));
                        f.Add(new Fan("System Fan #5 Pump", 0));
                        f.Add(new Fan("System Fan #6 Pump", 1));
                        f.Add(new Fan("System Fan #4", 2));
                        c.Add(new Control("Fan #5", 0));
                        c.Add(new Control("Fan #6", 1));
                        c.Add(new Control("Fan #4", 2));

                        break;

                    case Model.Z690_AORUS_PRO:
                        t.Add(new Temperature("PCIe x4", 0));
                        t.Add(new Temperature("EC_TEMP2", 1));
                        t.Add(new Temperature("System #2", 2));
                        f.Add(new Fan("System Fan #5 Pump", 0));
                        f.Add(new Fan("System Fan #6 Pump", 1));
                        f.Add(new Fan("System Fan #4", 2));
                        c.Add(new Control("System Fan #5 Pump", 0));
                        c.Add(new Control("System Fan #6 Pump", 1));
                        c.Add(new Control("System Fan #4", 2));
                        break;

                    default:
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
                            c.Add(new Control("Fan #" + (i + 1), i));

                        break;
                }

                break;

            default:
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
                    c.Add(new Control("Fan #" + (i + 1), i));

                break;
        }
    }

    private static void GetFintekConfiguration(ISuperIO superIO, Manufacturer manufacturer, Model model, IList<Voltage> v, IList<Temperature> t, IList<Fan> f, IList<Control> c)
    {
        switch (manufacturer)
        {
            case Manufacturer.EVGA:
                switch (model)
                {
                    case Model.X58_SLI_Classified: // F71882
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

                    case Model.X58_3X_SLI: // F71882
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
                        f.Add(new Fan("Chipset Fan", 3));
                        c.Add(new Control("CPU Fan", 0));
                        c.Add(new Control("Power Fan", 1));
                        c.Add(new Control("Chassis Fan", 2));
                        c.Add(new Control("Chipset Fan", 3));

                        break;

                    default:
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

                break;
            case Manufacturer.MSI:
                switch (model)
                {
                    case Model.Z77_MS7751: // F71889AD
                        v.Add(new Voltage("VCC3V", 0, 150, 150));
                        v.Add(new Voltage("Vcore", 1));
                        v.Add(new Voltage("iGPU", 2));
                        v.Add(new Voltage("+5V", 3, 20, 4.7f));
                        v.Add(new Voltage("+12V", 4, 68, 6.8f));
                        v.Add(new Voltage("DIMM", 5, 150, 150));
                        v.Add(new Voltage("CPU I/O", 6));
                        v.Add(new Voltage("+3.3V", 7, 150, 150));
                        v.Add(new Voltage("VBat", 8, 150, 150));

                        t.Add(new Temperature("CPU", 0));
                        t.Add(new Temperature("Probe", 1));
                        t.Add(new Temperature("System", 2));

                        f.Add(new Fan("CPU Fan", 0));
                        for (int i = 1; i < superIO.Fans.Length; i++)
                            f.Add(new Fan("System Fan #" + i, i));

                        c.Add(new Control("CPU Fan", 0));
                        for (int i = 1; i < superIO.Controls.Length; i++)
                            c.Add(new Control("System Fan #" + i, i));

                        break;
                    default:
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
                            c.Add(new Control("Fan Control #" + (i + 1), i));

                        break;
                }

                break;

            default:
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
                    c.Add(new Control("Fan #" + (i + 1), i));

                break;
        }
    }

    private static void GetNuvotonConfigurationF(ISuperIO superIO, Manufacturer manufacturer, Model model, IList<Voltage> v, IList<Temperature> t, IList<Fan> f, IList<Control> c)
    {
        switch (manufacturer)
        {
            case Manufacturer.ASUS:
                switch (model)
                {
                    case Model.P8P67: // NCT6776F
                    case Model.P8P67_EVO: // NCT6776F
                    case Model.P8P67_PRO: // NCT6776F
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
                        c.Add(new Control("Chassis Fan #2", 0));
                        c.Add(new Control("CPU Fan", 1));
                        c.Add(new Control("Chassis Fan #1", 2));

                        break;

                    case Model.P8P67_M_PRO: // NCT6776F
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

                    case Model.P8Z68_V_PRO: // NCT6776F
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
                            c.Add(new Control("Fan #" + (i + 1), i));

                        break;

                    case Model.P9X79: // NCT6776F
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
                            c.Add(new Control("Fan #" + (i + 1), i));

                        break;

                    default:
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
                            c.Add(new Control("Fan #" + (i + 1), i));

                        break;
                }

                break;
            case Manufacturer.ASRock:
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
                            c.Add(new Control("Chassis Fan #2", 0));
                            c.Add(new Control("CPU Fan", 1));
                            c.Add(new Control("Chassis Fan #1", 2));
                        }

                        break;
                    case Model.Z77Pro4M: //NCT6776F
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
                            c.Add(new Control("Fan #" + (i + 1), i));

                        break;

                    default:
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
                            c.Add(new Control("Fan #" + (i + 1), i));

                        break;
                }

                break;

            default:
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
                    c.Add(new Control("Fan #" + (i + 1), i));

                break;
        }
    }

    private static void GetNuvotonConfigurationD(ISuperIO superIO, Manufacturer manufacturer, Model model, IList<Voltage> v, IList<Temperature> t, IList<Fan> f, IList<Control> c)
    {
        switch (manufacturer)
        {
            case Manufacturer.ASRock:
                switch (model)
                {
                    case Model.A320M_HDV: //NCT6779D
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
                            c.Add(new Control("Fan #" + (i + 1), i));

                        break;

                    case Model.AB350_Pro4: //NCT6779D
                    case Model.AB350M_Pro4:
                    case Model.AB350M:
                    case Model.Fatal1ty_AB350_Gaming_K4:
                    case Model.AB350M_HDV:
                    case Model.B450_Steel_Legend:
                    case Model.B450M_Steel_Legend:
                    case Model.B450_Pro4:
                    case Model.B450M_Pro4:
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
                            c.Add(new Control("Fan #" + (i + 1), i));

                        break;

                    case Model.X399_Phantom_Gaming_6: //NCT6779D
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
                            c.Add(new Control("Fan #" + (i + 1), i));

                        break;

                    case Model.X570_Taichi:
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

                        t.Add(new Temperature("Motherboard", 2));
                        t.Add(new Temperature("CPU", 8));
                        t.Add(new Temperature("SB (Chipset)", 9));

                        f.Add(new Fan("Chassis #3", 0));
                        f.Add(new Fan("CPU #1", 1));
                        f.Add(new Fan("CPU #2", 2));
                        f.Add(new Fan("Chassis #1", 3));
                        f.Add(new Fan("Chassis #2", 4));
                        f.Add(new Fan("SB Fan", 5));
                        f.Add(new Fan("Chassis #4", 6));

                        c.Add(new Control("Chassis #3", 0));
                        c.Add(new Control("CPU #1", 1));
                        c.Add(new Control("CPU #2", 2));
                        c.Add(new Control("Chassis #1", 3));
                        c.Add(new Control("Chassis #2", 4));
                        c.Add(new Control("SB Fan", 5));
                        c.Add(new Control("Chassis #4", 6));

                        break;

                    case Model.X570_Phantom_Gaming_ITX:
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

                        t.Add(new Temperature("Motherboard", 0));
                        //t.Add(new Temperature("System", 1)); //Unused
                        t.Add(new Temperature("CPU", 2));
                        t.Add(new Temperature("SB (Chipset)", 3));
                        f.Add(new Fan("CPU Fan #1", 0)); //CPU_FAN1
                        f.Add(new Fan("Chassis Fan #1", 1)); //CHA_FAN1/WP
                        f.Add(new Fan("CPU Fan #2", 2)); //CPU_FAN2 (WP)
                        f.Add(new Fan("Chipset Fan", 3));

                        c.Add(new Control("CPU Fan #1", 0));
                        c.Add(new Control("Chassis Fan", 1));
                        c.Add(new Control("CPU Fan #2", 2));
                        c.Add(new Control("Chipset Fan", 3));
                        break;

                    case Model.Z790_Taichi:
                        v.Add(new Voltage("1.8V CPU Voltage", 0));
                        v.Add(new Voltage("0.82V Chipset Voltage", 1));
                        v.Add(new Voltage("+3.3V", 3, 34, 34));
                        v.Add(new Voltage("1.05V CPU Voltage", 4));
                        v.Add(new Voltage("1.05V Chipset Voltage", 12, 5, 100));

                        f.Add(new Fan("Chassis Fan #5", 0));
                        f.Add(new Fan("Chassis Fan #6", 1));
                        f.Add(new Fan("Chassis Fan #3", 6));

                        c.Add(new Control("Chassis Fan #5", 0));
                        c.Add(new Control("Chassis Fan #6", 1));
                        c.Add(new Control("Chassis Fan #3", 6));
                        break;

                    default:
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
                            c.Add(new Control("Fan #" + (i + 1), i));

                        break;
                }

                break;
            case Manufacturer.ASUS:
                string[] fanControlNames;
                switch (model)
                {
                    case Model.P8Z77_V: // NCT6779D
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
                        c.Add(new Control("Chassis Fan #1", 0));
                        c.Add(new Control("CPU Fan", 1));
                        c.Add(new Control("Chassis Fan #2", 2));
                        c.Add(new Control("Chassis Fan #3", 3));

                        break;

                    case Model.ROG_MAXIMUS_X_APEX: // NCT6793D
                        v.Add(new Voltage("Vcore", 0, 2, 2));
                        v.Add(new Voltage("+5V", 1, 4, 1));
                        v.Add(new Voltage("AVSB", 2, 34, 34));
                        v.Add(new Voltage("+3.3V", 3, 34, 34));
                        v.Add(new Voltage("+12V", 4, 11, 1));
                        v.Add(new Voltage("Voltage #6", 5, true));
                        v.Add(new Voltage("CPU Graphics", 6, 2, 2));
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
                        c.Add(new Control("Chassis Fan #1", 0));
                        c.Add(new Control("CPU Fan", 1));
                        c.Add(new Control("Chassis Fan #2", 2));
                        c.Add(new Control("Chassis Fan #3", 3));
                        c.Add(new Control("AIO Pump", 4));

                        break;

                    case Model.Z170_A: //NCT6793D
                        v.Add(new Voltage("Vcore", 0, 2, 2));
                        v.Add(new Voltage("+5V", 1, 4, 1));
                        v.Add(new Voltage("AVSB", 2, 34, 34));
                        v.Add(new Voltage("+3.3V", 3, 34, 34));
                        v.Add(new Voltage("+12V", 4, 11, 1));
                        v.Add(new Voltage("Voltage #6", 5, 0, 1, 0, true));
                        v.Add(new Voltage("CPU Graphics", 6, 2, 2));
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

                        // CPU Fan Optional uses the same fancontrol as CPU Fan.
                        // Water Pump speed can only be read from the EC.
                        string[] fanNames = { "Chassis Fan 1", "CPU Fan", "Chassis Fan 2", "Chassis Fan 3", "Chassis Fan 4", "CPU Fan Optional" };
                        fanControlNames = new[] { "Chassis Fan 1", "CPU Fan", "Chassis Fan 2", "Chassis Fan 3", "Chassis Fan 4", "Water Pump" };

                        for (int i = 0; i < fanNames.Length; i++)
                            f.Add(new Fan(fanNames[i], i));

                        for (int i = 0; i < fanControlNames.Length; i++)
                            c.Add(new Control(fanControlNames[i], i));

                        break;

                    case Model.TUF_GAMING_B550M_PLUS_WIFI: //NCT6798D
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
                        t.Add(new Temperature("PECI 0", 0));
                        t.Add(new Temperature("CPU", 1));
                        t.Add(new Temperature("System", 2));
                        t.Add(new Temperature("AUX 0", 3));
                        t.Add(new Temperature("AUX 1", 4));
                        t.Add(new Temperature("AUX 2", 5));
                        t.Add(new Temperature("AUX 3", 6));
                        t.Add(new Temperature("AUX 4", 7));
                        t.Add(new Temperature("SMBus 0", 8));
                        t.Add(new Temperature("SMBus 1", 9));
                        t.Add(new Temperature("PECI 1", 10));
                        t.Add(new Temperature("PCH Chip CPU Max", 11));
                        t.Add(new Temperature("PCH Chip", 12));
                        t.Add(new Temperature("PCH CPU", 13));
                        t.Add(new Temperature("PCH MCH", 14));
                        t.Add(new Temperature("Agent 0 DIMM 0", 15));
                        t.Add(new Temperature("Agent 0 DIMM 1", 16));
                        t.Add(new Temperature("Agent 1 DIMM 0", 17));
                        t.Add(new Temperature("Agent 1 DIMM 1", 18));
                        t.Add(new Temperature("Device 0", 19));
                        t.Add(new Temperature("Device 1", 20));
                        t.Add(new Temperature("PECI 0 Calibrated", 21));
                        t.Add(new Temperature("PECI 1 Calibrated", 22));
                        t.Add(new Temperature("Virtual", 23));

                        for (int i = 0; i < superIO.Fans.Length; i++)
                            f.Add(new Fan("Fan #" + (i + 1), i));

                        for (int i = 0; i < superIO.Controls.Length; i++)
                            c.Add(new Control("Fan #" + (i + 1), i));

                        break;

                    case Model.ROG_CROSSHAIR_VIII_HERO: // NCT6798D
                    case Model.ROG_CROSSHAIR_VIII_HERO_WIFI: // NCT6798D
                    case Model.ROG_CROSSHAIR_VIII_DARK_HERO: // NCT6798D
                    case Model.ROG_CROSSHAIR_VIII_FORMULA: // NCT6798D
                        v.Add(new Voltage("Vcore", 0));
                        v.Add(new Voltage("Voltage #2", 1, true));
                        v.Add(new Voltage("AVCC", 2, 34, 34));
                        v.Add(new Voltage("+3.3V", 3, 34, 34));
                        v.Add(new Voltage("Voltage #5", 4, true));
                        v.Add(new Voltage("Voltage #6", 5, true));
                        v.Add(new Voltage("CPU SoC", 6));
                        v.Add(new Voltage("3VSB", 7, 34, 34));
                        v.Add(new Voltage("VBat", 8, 34, 34));
                        v.Add(new Voltage("VTT", 9));
                        v.Add(new Voltage("Voltage #11", 10, true));
                        v.Add(new Voltage("Voltage #12", 11, true));
                        v.Add(new Voltage("Voltage #13", 12, true));
                        v.Add(new Voltage("DRAM", 13));
                        v.Add(new Voltage("Voltage #15", 14, true));
                        t.Add(new Temperature("PECI 0", 0));
                        t.Add(new Temperature("CPU", 1));
                        t.Add(new Temperature("Motherboard", 2));
                        t.Add(new Temperature("AUX 0", 3));
                        t.Add(new Temperature("AUX 1", 4));
                        t.Add(new Temperature("AUX 2", 5));
                        t.Add(new Temperature("AUX 3", 6));
                        t.Add(new Temperature("AUX 4", 7));
                        t.Add(new Temperature("SMBus 0", 8));
                        t.Add(new Temperature("SMBus 1", 9));
                        t.Add(new Temperature("PECI 1", 10));
                        t.Add(new Temperature("PCH Chip CPU Max", 11));
                        t.Add(new Temperature("PCH Chip", 12));
                        t.Add(new Temperature("PCH CPU", 13));
                        t.Add(new Temperature("PCH MCH", 14));
                        t.Add(new Temperature("Agent 0 DIMM 0", 15));
                        t.Add(new Temperature("Agent 0 DIMM 1", 16));
                        t.Add(new Temperature("Agent 1 DIMM 0", 17));
                        t.Add(new Temperature("Agent 1 DIMM 1", 18));
                        t.Add(new Temperature("Device 0", 19));
                        t.Add(new Temperature("Device 1", 20));
                        t.Add(new Temperature("PECI 0 Calibrated", 21));
                        t.Add(new Temperature("PECI 1 Calibrated", 22));
                        t.Add(new Temperature("Virtual", 23));

                        fanControlNames = new[] { "Chassis Fan 1", "CPU Fan", "Chassis Fan 2", "Chassis Fan 3", "High Amp Fan", "Waterpump", "AIO Pump" };
                        System.Diagnostics.Debug.Assert(fanControlNames.Length == superIO.Fans.Length,
                                                        $"Expected {fanControlNames.Length} fan register in the SuperIO chip");

                        System.Diagnostics.Debug.Assert(superIO.Fans.Length == superIO.Controls.Length,
                                                        "Expected counts of cans controls and fan speed registers to be equal");

                        for (int i = 0; i < fanControlNames.Length; i++)
                            f.Add(new Fan(fanControlNames[i], i));

                        for (int i = 0; i < fanControlNames.Length; i++)
                            c.Add(new Control(fanControlNames[i], i));

                        break;

                    case Model.ROG_MAXIMUS_X_HERO_WIFI_AC: //NCT6793D
                        v.Add(new Voltage("Vcore", 0, 2, 2));
                        v.Add(new Voltage("+5V", 1, 4, 1));
                        v.Add(new Voltage("AVSB", 2, 34, 34));
                        v.Add(new Voltage("+3.3V", 3, 34, 34));
                        v.Add(new Voltage("+12V", 4, 11, 1));
                        v.Add(new Voltage("Voltage #6", 5, true));
                        v.Add(new Voltage("CPU Graphics", 6, 2, 2));
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
                        t.Add(new Temperature("Motherboard", 2)); //Verified via BIOS and HWinfo. HWinfo had T1 and Motherboard, but thye were the same.
                        t.Add(new Temperature("Temperature #3", 4));
                        t.Add(new Temperature("Temperature #4", 5));
                        t.Add(new Temperature("Temperature #5", 6));

                        // note: CPU_Opt, W_Pump+, EXT_FAN 1 & 2 are on the ASUS EC controller. Together with VRM og PCH temperatures. And additional voltages and power
                        fanControlNames = new[] { "Chassis Fan 1", "CPU Fan", "Chassis Fan 2", "Chassis Fan 3", "AIO Pump", "HAMP" };

                        for (int i = 0; i < fanControlNames.Length; i++)
                            f.Add(new Fan(fanControlNames[i], i));

                        for (int i = 0; i < fanControlNames.Length; i++)
                            c.Add(new Control(fanControlNames[i], i));

                        break;

                    case Model.ROG_MAXIMUS_Z690_FORMULA: //NCT6798D
                        v.Add(new Voltage("Vcore", 0));
                        v.Add(new Voltage("+5V", 1, 4, 1));
                        v.Add(new Voltage("AVSB", 2, 34, 34));
                        v.Add(new Voltage("3VCC", 3, 34, 34));
                        v.Add(new Voltage("+12V", 4, 11, 1));
                        v.Add(new Voltage("IVR Atom L2 Cluster #1", 5));
                        v.Add(new Voltage("Voltage #7", 6));
                        v.Add(new Voltage("3VSB", 7, 34, 34));
                        v.Add(new Voltage("VBat", 8, 34, 34));
                        v.Add(new Voltage("VTT", 9, 1, 1));
                        v.Add(new Voltage("Voltage #11", 10));
                        v.Add(new Voltage("IVR Atom L2 Cluster #0", 11, 1, 1));
                        v.Add(new Voltage("PCH", 12));
                        v.Add(new Voltage("Voltage #14", 13));
                        v.Add(new Voltage("Voltage #15", 14));

                        t.Add(new Temperature("Temperature #1", 0));
                        t.Add(new Temperature("CPU", 1));
                        t.Add(new Temperature("Motherboard", 2));
                        t.Add(new Temperature("Temperature #4", 4));
                        t.Add(new Temperature("Temperature #5", 5));
                        t.Add(new Temperature("Temperature #6", 6));
                        t.Add(new Temperature("Temperature #7", 7));
                        t.Add(new Temperature("PCH", 12));
                        t.Add(new Temperature("Temperature #9", 21));

                        fanControlNames = new[] { "Chassis Fan 1", "CPU Fan", "Chassis Fan 2", "Chassis Fan 3", "Chassis Fan 4", "Waterpump", "AIO Pump" };

                        System.Diagnostics.Debug.Assert(fanControlNames.Length == superIO.Fans.Length,
                                                        $"Expected {fanControlNames.Length} fan register in the SuperIO chip");

                        System.Diagnostics.Debug.Assert(superIO.Fans.Length == superIO.Controls.Length,
                                                        "Expected counts of cans controls and fan speed registers to be equal");

                        for (int i = 0; i < fanControlNames.Length; i++)
                            f.Add(new Fan(fanControlNames[i], i));

                        for (int i = 0; i < fanControlNames.Length; i++)
                            c.Add(new Control(fanControlNames[i], i));

                        break;

                    case Model.ROG_MAXIMUS_Z690_HERO: //NCT6798D
                        v.Add(new Voltage("Vcore", 0));
                        v.Add(new Voltage("+5V", 1, 4, 1));
                        v.Add(new Voltage("AVSB", 2, 34, 34));
                        v.Add(new Voltage("3VCC", 3, 34, 34));
                        v.Add(new Voltage("+12V", 4, 11, 1));
                        v.Add(new Voltage("IVR Atom L2 Cluster #1", 5));
                        v.Add(new Voltage("Voltage #7", 6));
                        v.Add(new Voltage("3VSB", 7, 34, 34));
                        v.Add(new Voltage("VBat", 8, 34, 34));
                        v.Add(new Voltage("VTT", 9, 1, 1));
                        v.Add(new Voltage("Voltage #11", 10));
                        v.Add(new Voltage("IVR Atom L2 Cluster #0", 11, 1, 1));
                        v.Add(new Voltage("PCH", 12));
                        v.Add(new Voltage("Voltage #14 ", 13));
                        v.Add(new Voltage("Voltage #15", 14));

                        t.Add(new Temperature("CPU Package", 0)); // Matches CPU Package in HWinfo & Armoury Crate.
                        t.Add(new Temperature("CPU Weighted",
                                              1)); // Unsure about this one. HWinfo & Armoury Crate doesn't have anything that match my values. Varies from 34 (idle) to 42C (under load). Hwinfo is 31-32C for same.

                        t.Add(new Temperature("Motherboard", 2)); // Matches MB in HWinfo & Armoury Crate.
                        //t.Add(new Temperature("Temperature #4", 4));  // Constant at 15C
                        //t.Add(new Temperature("Temperature #5", 5));  // Varies from 15C to 123C. Probably bogus
                        //t.Add(new Temperature("Temperature #6", 6));  // Constant at 32C
                        //t.Add(new Temperature("Temperature #7", 7));  // Varies from 14C to 124C. Probably bogus
                        t.Add(new Temperature("PCH", 12)); // Chipset. Match HWinfo & Armoury Crate
                        t.Add(new Temperature("CPU", 21)); // Matches CPU in HWinfo & Armoury Crate.

                        // note that CPU Opt Fan is on the ASUS EC controller. Together with VRM, T_Sensor, WaterIn, WaterOut and WaterFlow + additional sensors.
                        fanControlNames = new[] { "Chassis Fan 1", "CPU Fan", "Chassis Fan 2", "Chassis Fan 3", "Chassis Fan 4", "Waterpump", "AIO Pump" };

                        System.Diagnostics.Debug.Assert(fanControlNames.Length == superIO.Fans.Length,
                                                        $"Expected {fanControlNames.Length} fan register in the SuperIO chip");

                        System.Diagnostics.Debug.Assert(superIO.Fans.Length == superIO.Controls.Length,
                                                        "Expected counts of cans controls and fan speed registers to be equal");

                        for (int i = 0; i < fanControlNames.Length; i++)
                            f.Add(new Fan(fanControlNames[i], i));

                        for (int i = 0; i < fanControlNames.Length; i++)
                            c.Add(new Control(fanControlNames[i], i));

                        break;

                    case Model.ROG_MAXIMUS_Z690_EXTREME_GLACIAL: //NCT6798D
                        v.Add(new Voltage("Vcore", 0));
                        v.Add(new Voltage("+5V", 1, 4, 1));
                        v.Add(new Voltage("AVSB", 2, 34, 34));
                        v.Add(new Voltage("3VCC", 3, 34, 34));
                        v.Add(new Voltage("+12V", 4, 11, 1));
                        v.Add(new Voltage("IVR Atom L2 Cluster #1", 5));
                        v.Add(new Voltage("Voltage #7", 6));
                        v.Add(new Voltage("3VSB", 7, 34, 34));
                        v.Add(new Voltage("VBat", 8, 34, 34));
                        v.Add(new Voltage("VTT", 9, 1, 1));
                        v.Add(new Voltage("Voltage #11", 10));
                        v.Add(new Voltage("IVR Atom L2 Cluster #0", 11, 1, 1));
                        v.Add(new Voltage("PCH", 12));
                        v.Add(new Voltage("Voltage #14", 13));
                        v.Add(new Voltage("Voltage #15", 14));

                        t.Add(new Temperature("Temperature #1", 0));
                        t.Add(new Temperature("CPU", 1));
                        t.Add(new Temperature("Motherboard", 2));
                        //t.Add(new Temperature("Temperature 03", 3));
                        t.Add(new Temperature("Temperature #4", 4));
                        t.Add(new Temperature("Temperature #5", 5));
                        t.Add(new Temperature("Temperature #6", 6));
                        t.Add(new Temperature("Temperature #7", 7));
                        //t.Add(new Temperature("Temperature 08", 8));
                        //t.Add(new Temperature("Temperature 09", 9));
                        //t.Add(new Temperature("Temperature 10", 10));
                        //t.Add(new Temperature("Temperature 11", 11));
                        t.Add(new Temperature("PCH", 12));
                        //t.Add(new Temperature("Temperature 13", 13));
                        //t.Add(new Temperature("Temperature 14", 14));
                        //t.Add(new Temperature("Temperature 15", 15));
                        //t.Add(new Temperature("Temperature 16", 16));
                        //t.Add(new Temperature("Temperature 17", 17));
                        //t.Add(new Temperature("Temperature 18", 18));
                        //t.Add(new Temperature("Temperature 19", 19));
                        //t.Add(new Temperature("Temperature 20", 20));
                        t.Add(new Temperature("Temperature #9", 21));

                        fanControlNames = new[] { "Chassis Fan 1", "CPU Fan", "Radiator Fan 1", "Radiator Fan 2", "Chassis Fan 2", "Water Pump 1", "Water Pump 2" };
                        System.Diagnostics.Debug.Assert(fanControlNames.Length == superIO.Fans.Length,
                                                        $"Expected {fanControlNames.Length} fan register in the SuperIO chip");

                        System.Diagnostics.Debug.Assert(superIO.Fans.Length == superIO.Controls.Length,
                                                        "Expected counts of fan controls and fan speed registers to be equal");

                        for (int i = 0; i < fanControlNames.Length; i++)
                            f.Add(new Fan(fanControlNames[i], i));

                        for (int i = 0; i < fanControlNames.Length; i++)
                            c.Add(new Control(fanControlNames[i], i));

                        break;

                    case Model.ROG_MAXIMUS_Z790_HERO: //NCT6798D
                        t.Add(new Temperature("CPU Package", 0));
                        t.Add(new Temperature("VRM", 1));
                        t.Add(new Temperature("Motherboard", 2));
                        //t.Add(new Temperature("Temperature #3", 3));
                        //t.Add(new Temperature("Temperature #4", 4));
                        //t.Add(new Temperature("Temperature #5", 5));
                        //t.Add(new Temperature("Temperature #6", 6));
                        //t.Add(new Temperature("Temperature #7", 7));
                        t.Add(new Temperature("Temperature #8", 8));
                        t.Add(new Temperature("Temperature #9", 9));
                        t.Add(new Temperature("Temperature #10", 10));
                        t.Add(new Temperature("Temperature #11", 11));
                        t.Add(new Temperature("Chipset", 12));
                        t.Add(new Temperature("Temperature #13", 13));
                        t.Add(new Temperature("Temperature #14", 14));
                        t.Add(new Temperature("Temperature #15", 15));
                        t.Add(new Temperature("Temperature #16", 16));
                        t.Add(new Temperature("Temperature #17", 17));
                        t.Add(new Temperature("Temperature #18", 18));
                        t.Add(new Temperature("Temperature #19", 19));
                        t.Add(new Temperature("Temperature #20", 20));
                        t.Add(new Temperature("CPU", 21));

                        for (int i = 0; i < superIO.Fans.Length; i++)
                            f.Add(new Fan("Fan #" + (i + 1), i));

                        for (int i = 0; i < superIO.Controls.Length; i++)
                            c.Add(new Control("Fan #" + (i + 1), i));

                        break;

                    case Model.ROG_STRIX_B550_I_GAMING: //NCT6798D
                        v.Add(new Voltage("Vcore", 0, 10, 10));
                        v.Add(new Voltage("+5V", 1, 4, 1)); //Probably not updating properly
                        v.Add(new Voltage("AVCC", 2, 10, 10));
                        v.Add(new Voltage("+3.3V", 3, 34, 34));
                        v.Add(new Voltage("+12V", 4, 11, 1)); //Probably not updating properly
                        //v.Add(new Voltage("#Unused #5", 5, 0, 1, 0, true));
                        //v.Add(new Voltage("#Unused #6", 6, 0, 1, 0, true));
                        v.Add(new Voltage("3VSB", 7, 34, 34));
                        v.Add(new Voltage("VBat", 8, 34, 34));
                        v.Add(new Voltage("VTT", 9));
                        //v.Add(new Voltage("#Unused #9", 9, 0, 1, 0, true));
                        //v.Add(new Voltage("#Unused #10", 10, 0, 1, 0, true));
                        //v.Add(new Voltage("#Unused #11", 11, 0, 1, 0, true));
                        //v.Add(new Voltage("#Unused #12", 12, 0, 1, 0, true));
                        //v.Add(new Voltage("#Unused #13", 13, 0, 1, 0, true));
                        //v.Add(new Voltage("#Unused #14", 14, 0, 1, 0, true));

                        t.Add(new Temperature("CPU", 1));
                        t.Add(new Temperature("Motherboard", 2));
                        //t.Add(new Temperature("AUX 0", 3)); //No software from Asus reports this temperature ~82C
                        //t.Add(new Temperature("#Unused 4", 4));
                        //t.Add(new Temperature("#Unused 5", 5));
                        //t.Add(new Temperature("#Unused 6", 6));
                        //t.Add(new Temperature("#Unused 7", 7));
                        //t.Add(new Temperature("#Unused 8", 8));
                        //t.Add(new Temperature("#Unused 9", 9));
                        //t.Add(new Temperature("#Unused 10", 10));
                        t.Add(new Temperature("PCH Chip CPU Max", 11));
                        t.Add(new Temperature("PCH Chip", 12));
                        t.Add(new Temperature("PCH CPU", 13));
                        t.Add(new Temperature("PCH MCH", 14));
                        t.Add(new Temperature("Agent 0 DIMM 0", 15));
                        //t.Add(new Temperature("Agent 0 DIMM 1", 16));
                        t.Add(new Temperature("Agent 1 DIMM 0", 17));
                        //t.Add(new Temperature("Agent 1 DIMM 1", 18));
                        t.Add(new Temperature("Device 0", 19));
                        t.Add(new Temperature("Device 1", 20));
                        t.Add(new Temperature("PECI 0 Calibrated", 21));
                        t.Add(new Temperature("PECI 1 Calibrated", 22));
                        t.Add(new Temperature("Virtual", 23));

                        for (int i = 0; i < superIO.Fans.Length; i++)
                        {
                            switch (i)
                            {
                                case 0:
                                    f.Add(new Fan("Chassis Fan", 0));
                                    break;
                                case 1:
                                    f.Add(new Fan("CPU Fan", 1));
                                    break;
                                case 4:
                                    f.Add(new Fan("AIO Pump", 4));
                                    break;
                            }
                        }

                        for (int i = 0; i < superIO.Controls.Length; i++)
                        {
                            switch (i)
                            {
                                case 0:
                                    c.Add(new Control("Chassis Fan", 0));
                                    break;
                                case 1:
                                    c.Add(new Control("CPU Fan", 1));
                                    break;
                                case 4:
                                    c.Add(new Control("AIO Pump", 4));
                                    break;
                            }
                        }

                        break;

                    case Model.ROG_ZENITH_II_EXTREME: // NCT6798D
                        // Voltage = value + (value - Vf) * Ri / Rf.
                        v.Add(new Voltage("Vcore", 0));
                        v.Add(new Voltage("+5V", 1, 4, 1));
                        v.Add(new Voltage("+3.3V", 3, 34, 34));
                        v.Add(new Voltage("+12V", 4, 6, 1));
                        v.Add(new Voltage("DIMM Voltage CD", 11, 10, 10));
                        v.Add(new Voltage("DIMM Voltage AB", 13));
                        v.Add(new Voltage("1.8V PPL Voltage", 14));

                        t.Add(new Temperature("CPU", 1));
                        t.Add(new Temperature("Motherboard", 2));
                        t.Add(new Temperature("Temperature #3", 3));
                        t.Add(new Temperature("Temperature #4", 4));
                        t.Add(new Temperature("Temperature #5", 5));
                        t.Add(new Temperature("Temperature #6", 6));
                        t.Add(new Temperature("Temperature #7", 7));
                        t.Add(new Temperature("Temperature #21", 21));

                        for (int i = 0; i < superIO.Fans.Length; i++)
                        {
                            switch (i)
                            {
                                case 0:
                                    f.Add(new Fan("Chassis Fan", 0));
                                    break;
                                case 1:
                                    f.Add(new Fan("CPU Fan", 1));
                                    break;
                                case 2:
                                    f.Add(new Fan("CPU Optional Fan", 2));
                                    break;
                                case 4:
                                    f.Add(new Fan("AIO Pump", 4));
                                    break;
                            }
                        }

                        for (int i = 0; i < superIO.Controls.Length; i++)
                        {
                            switch (i)
                            {
                                case 0:
                                    c.Add(new Control("Chassis Fan", 0));
                                    break;
                                case 1:
                                    c.Add(new Control("CPU Fan", 1));
                                    break;
                                case 2:
                                    c.Add(new Control("CPU Optional Fan", 2));
                                    break;
                                case 4:
                                    c.Add(new Control("AIO Pump", 4));
                                    break;
                            }
                        }

                        break;

                    case Model.ROG_STRIX_X570_I_GAMING: //NCT6798D
                        v.Add(new Voltage("Vcore", 0, 10, 10));
                        v.Add(new Voltage("+5V", 1, 4, 1)); //Probably not updating properly
                        v.Add(new Voltage("AVCC", 2, 10, 10));
                        v.Add(new Voltage("+3.3V", 3, 34, 34));
                        v.Add(new Voltage("+12V", 4, 11, 1)); //Probably not updating properly
                        v.Add(new Voltage("3VSB", 7, 34, 34));
                        v.Add(new Voltage("VBat", 8, 34, 34));
                        v.Add(new Voltage("VTT", 9));
                        t.Add(new Temperature("CPU", 1));
                        t.Add(new Temperature("Motherboard", 2));
                        t.Add(new Temperature("Temperature #3", 3));
                        t.Add(new Temperature("Temperature #4", 4));
                        t.Add(new Temperature("Temperature #5", 5));
                        t.Add(new Temperature("Temperature #6", 6));
                        t.Add(new Temperature("Temperature #7", 7));
                        t.Add(new Temperature("Temperature #21", 21));

                        for (int i = 0; i < superIO.Fans.Length; i++)
                        {
                            switch (i)
                            {
                                case 0:
                                    f.Add(new Fan("Chassis Fan", 0));
                                    break;
                                case 1:
                                    f.Add(new Fan("CPU Fan", 1));
                                    break;
                                case 4:
                                    f.Add(new Fan("AIO Pump", 4));
                                    break;
                            }
                        }

                        for (int i = 0; i < superIO.Controls.Length; i++)
                        {
                            switch (i)
                            {
                                case 0:
                                    c.Add(new Control("Chassis Fan", 0));
                                    break;
                                case 1:
                                    c.Add(new Control("CPU Fan", 1));
                                    break;
                                case 4:
                                    c.Add(new Control("AIO Pump", 4));
                                    break;
                            }
                        }

                        break;

                    case Model.ROG_STRIX_B550_F_GAMING_WIFI: // NCT6798D-R
                        v.Add(new Voltage("Vcore", 0, 2, 2));
                        v.Add(new Voltage("+5V", 1, 4, 1));
                        v.Add(new Voltage("AVCC", 2, 34, 34));
                        v.Add(new Voltage("+3.3V", 3, 34, 34));
                        v.Add(new Voltage("+12V", 4, 11, 1));
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

                        for (int i = 0; i < superIO.Fans.Length; i++)
                            f.Add(new Fan("Fan #" + (i + 1), i));

                        for (int i = 0; i < superIO.Controls.Length; i++)
                            c.Add(new Control("Fan #" + (i + 1), i));

                        break;

                    case Model.ROG_CROSSHAIR_X670E_GENE: // NCT6799D
                        v.Add(new Voltage("Vcore", 0, 2, 2)); // This is wrong
                        v.Add(new Voltage("+5V", 1, 4, 1));
                        v.Add(new Voltage("AVCC", 2, 34, 34));
                        v.Add(new Voltage("+3.3V", 3, 34, 34));
                        v.Add(new Voltage("+12V", 4, 11, 1));
                        v.Add(new Voltage("Voltage #6", 5, true));
                        v.Add(new Voltage("Voltage #7", 6, true));
                        v.Add(new Voltage("3VSB", 7, 34, 34));
                        v.Add(new Voltage("VBat", 8, 34, 34));
                        v.Add(new Voltage("VTT", 9)); // This is wrong
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
                        t.Add(new Temperature("T Sensor", 24));

                        for (int i = 0; i < superIO.Fans.Length; i++)
                            f.Add(new Fan("Fan #" + (i + 1), i));

                        for (int i = 0; i < superIO.Controls.Length; i++)
                            c.Add(new Control("Fan #" + (i + 1), i));

                        break;

                    default:
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
                            c.Add(new Control("Fan #" + (i + 1), i));

                        break;
                }

                break;
            case Manufacturer.MSI:
                switch (model)
                {
                    case Model.B360M_PRO_VDH: // NCT6797D
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
                        c.Add(new Control("CPU Fan", 1));
                        c.Add(new Control("System Fan #1", 2));
                        c.Add(new Control("System Fan #2", 3));

                        break;

                    case Model.B450A_PRO: // NCT6797D
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
                        //t.Add(new Temperature("CPU", 0));
                        t.Add(new Temperature("CPU", 1));
                        t.Add(new Temperature("System", 2));
                        t.Add(new Temperature("VRM MOS", 3));
                        t.Add(new Temperature("PCH", 5));
                        t.Add(new Temperature("SMBus 0", 8));
                        f.Add(new Fan("Pump Fan", 0));
                        f.Add(new Fan("CPU Fan", 1));
                        f.Add(new Fan("System Fan #1", 2));
                        f.Add(new Fan("System Fan #2", 3));
                        f.Add(new Fan("System Fan #3", 4));
                        f.Add(new Fan("System Fan #4", 5));
                        c.Add(new Control("Pump Fan", 0));
                        c.Add(new Control("CPU Fan", 1));
                        c.Add(new Control("System Fan #1", 2));
                        c.Add(new Control("System Fan #2", 3));
                        c.Add(new Control("System Fan #3", 4));
                        c.Add(new Control("System Fan #4", 5));

                        break;

                    case Model.Z270_PC_MATE: // NCT6795D
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
                        c.Add(new Control("Pump Fan", 0));
                        c.Add(new Control("CPU Fan", 1));
                        c.Add(new Control("System Fan #1", 2));
                        c.Add(new Control("System Fan #2", 3));
                        c.Add(new Control("System Fan #3", 4));
                        c.Add(new Control("System Fan #4", 5));

                        break;

                    default:
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
                            c.Add(new Control("Fan #" + (i + 1), i));

                        break;
                }

                break;

            default:
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
                    c.Add(new Control("Fan #" + (i + 1), i));

                break;
        }
    }

    private static void GetWinbondConfigurationEhf(Manufacturer manufacturer, Model model, IList<Voltage> v, IList<Temperature> t, IList<Fan> f, IList<Control> c)
    {
        switch (manufacturer)
        {
            case Manufacturer.ASRock:
                switch (model)
                {
                    case Model.AOD790GX_128M: // W83627EHF
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
                        c.Add(new Control("System Fan", 0));
                        c.Add(new Control("CPU Fan", 1));
                        c.Add(new Control("Auxiliary Fan", 2));

                        break;

                    default:
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
                        c.Add(new Control("System Fan", 0));
                        c.Add(new Control("CPU Fan", 1));
                        c.Add(new Control("Auxiliary Fan", 2));

                        break;
                }

                break;

            default:
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
                c.Add(new Control("System Fan", 0));
                c.Add(new Control("CPU Fan", 1));
                c.Add(new Control("Auxiliary Fan", 2));

                break;
        }
    }

    private static void GetWinbondConfigurationHg(Manufacturer manufacturer, Model model, IList<Voltage> v, IList<Temperature> t, IList<Fan> f, IList<Control> c)
    {
        switch (manufacturer)
        {
            case Manufacturer.ASRock:
                switch (model)
                {
                    case Model._880GMH_USB3: // W83627DHG-P
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
                        c.Add(new Control("System Fan", 0));
                        c.Add(new Control("CPU Fan", 1));
                        c.Add(new Control("Auxiliary Fan", 2));

                        break;

                    default:
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
                        c.Add(new Control("System Fan", 0));
                        c.Add(new Control("CPU Fan", 1));
                        c.Add(new Control("Auxiliary Fan", 2));

                        break;
                }

                break;
            case Manufacturer.ASUS:
                switch (model)
                {
                    case Model.P6T: // W83667HG
                    case Model.P6X58D_E: // W83667HG
                    case Model.RAMPAGE_II_GENE: // W83667HG
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
                        c.Add(new Control("System Fan", 0));
                        c.Add(new Control("CPU Fan", 1));
                        c.Add(new Control("Auxiliary Fan", 2));

                        break;

                    case Model.RAMPAGE_EXTREME: // W83667HG
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
                        c.Add(new Control("System Fan", 0));
                        c.Add(new Control("CPU Fan", 1));
                        c.Add(new Control("Auxiliary Fan", 2));

                        break;

                    default:
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
                        c.Add(new Control("System Fan", 0));
                        c.Add(new Control("CPU Fan", 1));
                        c.Add(new Control("Auxiliary Fan", 2));

                        break;
                }

                break;

            default:
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
                c.Add(new Control("System Fan", 0));
                c.Add(new Control("CPU Fan", 1));
                c.Add(new Control("Auxiliary Fan", 2));

                break;
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
                sensor.Value = value + ((value - sensor.Parameters[2].Value) * sensor.Parameters[0].Value / sensor.Parameters[1].Value);
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
            sensor.Value = _readControl(sensor.Index);
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
}
