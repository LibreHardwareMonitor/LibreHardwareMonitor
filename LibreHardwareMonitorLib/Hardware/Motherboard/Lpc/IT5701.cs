// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Globalization;
using System.Text;

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc;

/// <summary>
/// Fan controller run by the Gigabyte EC firmware, e.g. SYS_FAN7_PUMP and SYS_FAN8_PUMP on X670E and X870E AORUS MASTER boards.
/// It has no LPC interface of its own; its registers live in EC memory at <see cref="BaseAddress" />.
/// Register layout: https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/issues/2582
/// </summary>
internal class IT5701 : ISuperIO
{
    private const ushort BaseAddress = 0xC00;
    private const int FanCount = 2;

    // Register offsets from BaseAddress, one byte per fan or sensor unless noted.
    private const ushort VersionRegister = 0x00; // Major, then minor.
    private const ushort TemperatureRegister = 0x02; // °C; 0x00 or 0xFF when no sensor is connected.
    private const ushort DutyRegister = 0x06; // 0 hands the fan back to the BIOS curve.
    private const ushort PwmRegister = 0x08; // Applied duty; always reads 0 on some boards.
    private const ushort FanSpeedRegister = 0x0A; // RPM, 16-bit little-endian.
    private const int RegisterCount = FanSpeedRegister + 2 * FanCount;

    private const ushort ControllerEnableRegister = 0x947;

    private readonly IGigabyteController _controller;
    private readonly IT879xEcioPort _ecioPort;
    private readonly LpcPort _ecioSession;
    private readonly bool[] _restoreDefaultFanControlRequired = new bool[FanCount];

    private IT5701()
    {
        Controls = new float?[FanCount];
        Fans = new float?[FanCount];
        Temperatures = new float?[FanCount];
    }

    private IT5701(LpcPort ecioSession) : this()
    {
        _ecioSession = ecioSession;
        _ecioPort = new IT879xEcioPort(ecioSession);
    }

    private IT5701(IGigabyteController controller) : this()
    {
        _controller = controller;
    }

    public Chip Chip => Chip.IT5701;

    public float?[] Controls { get; }

    public float?[] Fans { get; }

    public float?[] Temperatures { get; }

    public float?[] Voltages { get; } = Array.Empty<float?>();

    /// <summary>
    /// Detects an IT5701 by its version register, through a dedicated ECIO session where ECIO is available
    /// (see <see cref="CreateEcioSession" />), or otherwise through <paramref name="controller" />, e.g. the MMIO route on
    /// IT87952E boards.
    /// </summary>
    public static IT5701 TryCreate(IGigabyteController controller, bool useEcio)
    {
        // Check compatibility by querying its version.
        if (useEcio)
        {
            LpcPort ecioSession = CreateEcioSession();
            if (new IT879xEcioPort(ecioSession).Read(BaseAddress + VersionRegister, out byte majorVersion) && majorVersion == 1)
                return new IT5701(ecioSession);

            ecioSession.Close();
        }

        byte[] version = new byte[1];
        if (controller != null && controller.Read(BaseAddress + VersionRegister, version) && version[0] == 1)
            return new IT5701(controller);

        return null;
    }

    public byte? ReadGpio(int index)
    {
        return null;
    }

    public void WriteGpio(int index, byte value)
    { }

    public void SetControl(int index, byte? value)
    {
        if (index < 0 || index >= Controls.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (!Mutexes.WaitIsaBus(10))
            return;

        try
        {
            // Only hand back fans this instance has taken, so another program's settings are left alone.
            if (!value.HasValue && !_restoreDefaultFanControlRequired[index])
                return;

            Write((ushort)(DutyRegister + index), value ?? 0);
            _restoreDefaultFanControlRequired[index] = value.HasValue;

            // Duty writes are only applied while the Gigabyte controller is enabled (EC 0x947).
            EnableController();
        }
        finally
        {
            Mutexes.ReleaseIsaBus();
        }
    }

    public void Update()
    {
        if (!Mutexes.WaitIsaBus(10))
            return;

        try
        {
            byte[] registers = new byte[RegisterCount];
            bool valid = Read(registers);

            for (int i = 0; i < Fans.Length; i++)
            {
                Fans[i] = valid ? registers[FanSpeedRegister + 2 * i] | (registers[FanSpeedRegister + 2 * i + 1] << 8) : null;

                // Where the applied duty reads 0 (e.g. X870E AORUS MASTER X3D ICE), report the written one.
                // Both 0 means the BIOS curve is in control.
                byte duty = registers[PwmRegister + i] != 0 ? registers[PwmRegister + i] : registers[DutyRegister + i];
                Controls[i] = valid && duty != 0 ? (float)Math.Round(duty * 100.0 / 255.0) : null;
            }

            for (int i = 0; i < Temperatures.Length; i++)
            {
                byte temperature = registers[TemperatureRegister + i];
                Temperatures[i] = valid && temperature is not (0x00 or 0xFF) ? temperature : null;
            }
        }
        finally
        {
            Mutexes.ReleaseIsaBus();
        }
    }

    public string GetReport()
    {
        StringBuilder r = new();

        r.AppendLine("LPC " + GetType().Name);
        r.AppendLine();
        r.Append("Access: ");
        r.AppendLine(_controller?.GetType().Name ?? "ECIO");
        r.AppendLine();

        if (!Mutexes.WaitIsaBus(100))
            return r.ToString();

        byte[] registers = new byte[0x20];
        bool valid = Read(registers);
        Mutexes.ReleaseIsaBus();

        r.AppendLine("      00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F");
        r.AppendLine();
        for (int i = 0; i < registers.Length; i += 0x10)
        {
            r.Append(' ');
            r.Append((BaseAddress + i).ToString("X3", CultureInfo.InvariantCulture));
            r.Append(' ');
            for (int j = 0; j < 0x10; j++)
            {
                r.Append(' ');
                r.Append(valid ? registers[i + j].ToString("X2", CultureInfo.InvariantCulture) : "??");
            }

            r.AppendLine();
        }

        r.AppendLine();
        return r.ToString();
    }

    public void Close()
    {
        // SuperIOHardware.Close has already handed any fans this instance took back to the BIOS curve.
        // In controller mode the controller belongs to IT87XX, which disposes it.
        _ecioSession?.Close();
    }

    // On IT8792E boards the shared 0x4E session is left in configuration mode (chip ID 0x8883) after detection,
    // where the ECIO window (0x3F0/0x3F4) isn't reliably mapped. Entering again on a fresh handle maps it.
    private static LpcPort CreateEcioSession()
    {
        LpcPort port = new(0x4E, 0x4F);
        port.WriteIoPort(0x4E, 0x77);
        port.WriteIoPort(0x4E, 0x77);
        port.FindBars();
        return port;
    }

    private void EnableController()
    {
        if (_controller != null)
            _controller.Enable(true);
        else if (_ecioPort.Read(ControllerEnableRegister, out byte enabled) && enabled == 0)
            _ecioPort.Write(ControllerEnableRegister, 1);
    }

    private bool Read(byte[] buffer)
    {
        if (_controller != null)
            return _controller.Read(BaseAddress, buffer);

        for (int i = 0; i < buffer.Length; i++)
        {
            if (!_ecioPort.Read((ushort)(BaseAddress + i), out buffer[i]))
                return false;
        }

        return true;
    }

    private void Write(ushort register, byte value)
    {
        ushort offset = (ushort)(BaseAddress + register);
        if (_controller != null)
            _controller.Write(offset, value);
        else
            _ecioPort.Write(offset, value);
    }
}
