// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc;

/// <summary>
/// Fans of HP OMEN desktops. They are driven by HP firmware instead of a Super I/O chip and are exposed through the
/// HP BIOS WMI interface (root\wmi hpqBIntM, command 0x20008), the interface OMEN Gaming Hub uses.
/// </summary>
/// <remarks>
/// Command types used: 0x28 system design data, 0x2C fan types, 0x2D fan speeds, 0x32 fan duty, 0x1A fan mode.
/// The notebook fan command 0x2E must never be sent to these desktops: on an OMEN 45L (board 8D2C) writing 0xFF
/// through it held every fan at full speed through reboots, power cycles and BIOS defaults until the BIOS was
/// reflashed. Nothing in this class uses it.
/// </remarks>
internal class HpOmenWmi : ISuperIO
{
    // ReSharper disable InconsistentNaming
    private const uint COMMAND_GAMING = 0x20008;
    private const uint COMMAND_TYPE_FAN_MODE = 0x1A;
    private const uint COMMAND_TYPE_SYSTEM_DESIGN_DATA = 0x28;
    private const uint COMMAND_TYPE_FAN_TYPES = 0x2C;
    private const uint COMMAND_TYPE_FAN_SPEEDS = 0x2D;
    private const uint COMMAND_TYPE_FAN_LEVELS = 0x32;

    private const byte FAN_MODE_DEFAULT = 0x20;
    private const byte LEVEL_UNCHANGED = 0xFF;
    private const int MAX_FANS = 8;

    private const int FAN_TYPE_CPU = 1;
    private const int FAN_TYPE_PUMP = 4;
    // ReSharper restore InconsistentNaming

    // Boards on which reading, control and the hand-back to the BIOS have been verified.
    private static readonly string[] VerifiedBoards = { "8D2C" };

    // Duty levels are re-sent periodically while any fan is under software control, as a safeguard.
    private static readonly TimeSpan ResendInterval = TimeSpan.FromSeconds(60);

    private readonly bool _controlAllowed;
    private readonly Func<uint, byte[], int, byte[]> _execute;
    private readonly int[] _fanTypes;
    private readonly byte[] _levels;
    private readonly object _lock = new();
    private readonly IDisposable _transport;

    private bool _closed;
    private byte[] _lastWritten;
    private DateTime _lastWriteUtc = DateTime.MinValue;
    private bool _possiblyOverridden;
    private bool _releasePending;

    public HpOmenWmi() : this(new BiosWmi())
    { }

    private HpOmenWmi(BiosWmi bios) : this(bios.Execute, bios)
    { }

    internal HpOmenWmi(Func<uint, byte[], int, byte[]> execute, IDisposable transport = null)
    {
        Chip = Chip.HpOmenWmi;
        _execute = execute;
        _transport = transport;

        try
        {
            _fanTypes = ParseFanTypes(_execute(COMMAND_TYPE_FAN_TYPES, new byte[4], 128));

            try
            {
                _controlAllowed = AllowsSoftwareControl(_execute(COMMAND_TYPE_SYSTEM_DESIGN_DATA, new byte[4], 128));
            }
            catch (Exception)
            {
                _controlAllowed = false;
            }
        }
        catch
        {
            _transport?.Dispose();
            throw;
        }

        _levels = Enumerable.Repeat(LEVEL_UNCHANGED, _fanTypes.Length).ToArray();

        Fans = new float?[_fanTypes.Length];
        Controls = new float?[_controlAllowed ? _fanTypes.Length : 0];
        Temperatures = Array.Empty<float?>();
        Voltages = Array.Empty<float?>();

        Update();
    }

    public Chip Chip { get; }

    public float?[] Controls { get; }

    public float?[] Fans { get; }

    public float?[] Temperatures { get; }

    public float?[] Voltages { get; }

    public static bool IsVerifiedBoard(Motherboard motherboard)
    {
        string product = motherboard.SMBios?.Board?.ProductName?.Trim();
        return motherboard.Manufacturer == Manufacturer.HP && product != null && VerifiedBoards.Contains(product, StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<Fan> GetFans()
    {
        for (int i = 0; i < _fanTypes.Length; i++)
            yield return new Fan(GetFanName(i), i);
    }

    public IEnumerable<Control> GetControls()
    {
        for (int i = 0; i < Controls.Length; i++)
            yield return new Control(GetFanName(i), i);
    }

    public string GetReport()
    {
        lock (_lock)
        {
            StringBuilder r = new();

            r.AppendLine("HP OMEN WMI");
            r.AppendLine();
            r.AppendLine("Fan types: " + string.Join(", ", _fanTypes.Select((_, i) => GetFanName(i))));
            r.AppendLine("Software fan control: " + (_controlAllowed ? "allowed" : "not allowed by the firmware"));
            AppendCommandReport(r, "System design data (0x28)", COMMAND_TYPE_SYSTEM_DESIGN_DATA);
            AppendCommandReport(r, "Fan types (0x2C)", COMMAND_TYPE_FAN_TYPES);
            AppendCommandReport(r, "Fan speeds (0x2D)", COMMAND_TYPE_FAN_SPEEDS);
            r.AppendLine("Duty levels set: " + BitConverter.ToString(_levels));
            r.AppendLine();

            return r.ToString();
        }
    }

    public void SetControl(int index, byte? value)
    {
        lock (_lock)
        {
            if (_closed || !_controlAllowed || index < 0 || index >= _levels.Length)
                return;

            if (value.HasValue)
            {
                int percent = (int)Math.Round(value.Value / 2.55f);
                _levels[index] = (byte)Math.Max(GetMinimumPercent(_fanTypes[index]), Math.Min(100, percent));
                Controls[index] = _levels[index];
            }
            else
            {
                // A fan that was never set is already under BIOS control; avoid the fan mode write.
                if (_levels[index] == LEVEL_UNCHANGED && !_releasePending)
                    return;

                _levels[index] = LEVEL_UNCHANGED;
                Controls[index] = null;
                _releasePending = true;
            }

            try
            {
                Flush();
            }
            catch (Exception)
            {
                // Retried on the next update: a failed release stays pending and a failed write keeps the
                // fans marked as possibly overridden, so Close still hands them back.
            }
        }
    }

    public void Update()
    {
        lock (_lock)
        {
            if (_closed)
                return;

            try
            {
                byte[] speeds = _execute(COMMAND_TYPE_FAN_SPEEDS, new byte[4], 128);
                if (speeds == null || speeds.Length < Fans.Length)
                    throw new InvalidDataException("Truncated fan speed response");

                for (int i = 0; i < Fans.Length; i++)
                    Fans[i] = speeds[i] * 100f;
            }
            catch (Exception)
            {
                for (int i = 0; i < Fans.Length; i++)
                    Fans[i] = null;
            }

            try
            {
                Flush();
            }
            catch (Exception)
            {
                // Retried on the next update.
            }
        }
    }

    public void Close()
    {
        lock (_lock)
        {
            if (_closed)
                return;

            _closed = true;

            for (int i = 0; i < _levels.Length; i++)
                _levels[i] = LEVEL_UNCHANGED;

            try
            {
                if (_possiblyOverridden || _releasePending)
                    ReleaseToBios();
            }
            catch (Exception)
            {
                // Nothing else can be done; the firmware clears the override at the next boot.
            }

            _transport?.Dispose();
        }
    }

    public byte? ReadGpio(int index)
    {
        return null;
    }

    public void WriteGpio(int index, byte value)
    { }

    internal static int[] ParseFanTypes(byte[] data)
    {
        if (data == null || data.Length < MAX_FANS / 2)
            throw new InvalidDataException("Truncated fan type response");

        // One nibble per fan, low nibble first: 1 CPU, 2 GPU, 3 exhaust, 4 pump, 5 intake, 6 VRM.
        int[] types = new int[MAX_FANS];
        for (int i = 0; i < MAX_FANS; i++)
            types[i] = (data[i / 2] >> ((i % 2) * 4)) & 0x0F;

        int count = Array.FindLastIndex(types, t => t != 0) + 1;
        if (count == 0 || types.Take(count).Any(t => t < 1 || t > 6))
            throw new InvalidDataException("Unsupported fan type table");

        return types.Take(count).ToArray();
    }

    internal static bool AllowsSoftwareControl(byte[] data)
    {
        // Byte 4 bit 3 set means that only the BIOS may control the fans.
        return data != null && data.Length >= 5 && (data[4] & 0x08) == 0;
    }

    private static int GetMinimumPercent(int fanType)
    {
        // Keep the pump and the CPU fan from being stopped by a low software curve.
        return fanType switch
        {
            FAN_TYPE_PUMP => 50,
            FAN_TYPE_CPU => 20,
            _ => 0
        };
    }

    private static string GetFanTypeName(int fanType)
    {
        return fanType switch
        {
            1 => "CPU Fan",
            2 => "GPU Fan",
            3 => "Exhaust Fan",
            4 => "Pump",
            5 => "Intake Fan",
            6 => "VRM Fan",
            _ => "Fan"
        };
    }

    private string GetFanName(int index)
    {
        int type = _fanTypes[index];
        string name = GetFanTypeName(type);

        if (_fanTypes.Count(t => t == type) == 1)
            return name;

        return name + " #" + _fanTypes.Take(index + 1).Count(t => t == type);
    }

    private void AppendCommandReport(StringBuilder r, string name, uint commandType)
    {
        try
        {
            byte[] data = _execute(commandType, new byte[4], 128);
            r.AppendLine(name + ": " + (data == null ? "null" : BitConverter.ToString(data, 0, Math.Min(16, data.Length))));
        }
        catch (Exception e)
        {
            r.AppendLine(name + ": " + e.Message);
        }
    }

    // Applies a pending hand-back to the BIOS, then writes the duty table when it changed or is due for a resend.
    private void Flush()
    {
        if (_releasePending)
            ReleaseToBios();

        if (_levels.All(l => l == LEVEL_UNCHANGED))
            return;

        bool changed = _lastWritten == null || !_lastWritten.SequenceEqual(_levels);
        if (!changed && DateTime.UtcNow - _lastWriteUtc < ResendInterval)
            return;

        // 0xFF keeps a fan as it is; it does not hand the fan back to the BIOS.
        byte[] table = Enumerable.Repeat(LEVEL_UNCHANGED, 128).ToArray();
        Array.Copy(_levels, table, _levels.Length);

        // The firmware may apply the write even if the call reports an error, so cleanup is needed either way.
        _possiblyOverridden = true;
        _execute(COMMAND_TYPE_FAN_LEVELS, table, 0);

        _lastWritten = (byte[])_levels.Clone();
        _lastWriteUtc = DateTime.UtcNow;
    }

    // The firmware has no per-fan hand-back: the default fan mode releases every fan, after which Flush re-applies
    // the fans that are still under software control.
    private void ReleaseToBios()
    {
        _execute(COMMAND_TYPE_FAN_MODE, new byte[] { 0xFF, FAN_MODE_DEFAULT, 0, 0 }, 0);
        _releasePending = false;
        _possiblyOverridden = false;
        _lastWritten = null;
    }

    private sealed class BiosWmi : IDisposable
    {
        private readonly ManagementObject _bios;
        private readonly ManagementClass _dataIn;

        public BiosWmi()
        {
            ManagementScope scope = new(@"\\.\root\wmi");
            scope.Connect();

            _dataIn = new ManagementClass(scope, new ManagementPath("hpqBDataIn"), null);

            using ManagementObjectSearcher searcher = new(scope, new ObjectQuery("SELECT * FROM hpqBIntM"));
            _bios = searcher.Get().OfType<ManagementObject>().FirstOrDefault();

            if (_bios == null)
            {
                _dataIn.Dispose();
                throw new InvalidOperationException("HP BIOS WMI interface (hpqBIntM) not found");
            }
        }

        public byte[] Execute(uint commandType, byte[] input, int outputSize)
        {
            string method = outputSize switch
            {
                0 => "hpqBIOSInt0",
                <= 4 => "hpqBIOSInt4",
                _ => "hpqBIOSInt128"
            };

            using ManagementObject inData = _dataIn.CreateInstance();
            inData["Sign"] = new byte[] { 0x53, 0x45, 0x43, 0x55 }; // "SECU"
            inData["Command"] = COMMAND_GAMING;
            inData["CommandType"] = commandType;
            inData["Size"] = (uint)input.Length;
            inData["hpqBData"] = input;

            using ManagementBaseObject inParams = _bios.GetMethodParameters(method);
            inParams["InData"] = inData;

            using ManagementBaseObject outParams = _bios.InvokeMethod(method, inParams, null);
            ManagementBaseObject outData = (ManagementBaseObject)outParams["OutData"];

            uint returnCode = (uint)outData["rwReturnCode"];
            if (returnCode != 0)
                throw new InvalidOperationException("HP BIOS command 0x" + commandType.ToString("X2", CultureInfo.InvariantCulture) + " returned " + returnCode.ToString(CultureInfo.InvariantCulture));

            return outputSize == 0 ? Array.Empty<byte>() : (byte[])outData["Data"];
        }

        public void Dispose()
        {
            _bios.Dispose();
            _dataIn.Dispose();
        }
    }
}
