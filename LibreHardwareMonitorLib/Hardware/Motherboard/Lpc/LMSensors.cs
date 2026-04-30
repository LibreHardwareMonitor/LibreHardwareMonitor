// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael M�ller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

// ReSharper disable once InconsistentNaming

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc;

// Linux runtime dependencies for this implementation:
// 1) Kernel hwmon/sysfs support exposed under /sys/class/hwmon.
// 2) Matching kernel drivers for supported Super I/O chips.
// 3) Read/write permissions for hwmon nodes if fan control is used.
// Notes:
// - No dependency on the userspace `sensors` command at runtime.
// - No extra NuGet package is required specifically for this class.
internal class LMSensors
{
    private const string HwMonPath = "/sys/class/hwmon/";
    private readonly List<ISuperIO> _superIOs = [];

    public LMSensors()
    {
        if (!Directory.Exists(HwMonPath))
            return;

        foreach (string basePath in Directory.GetDirectories(HwMonPath))
        {
            foreach (string devicePath in new[] { "/device", string.Empty })
            {
                string path = basePath + devicePath;
                string name = null;

                try
                {
                    using StreamReader reader = new(path + "/name");
                    name = reader.ReadLine();
                }
                catch (IOException)
                { }

                switch (name)
                {
                    case "atk0110":
                        _superIOs.Add(new LMChip(Chip.ATK0110, path));
                        break;

                    case "f71858fg":
                        _superIOs.Add(new LMChip(Chip.F71858, path));
                        break;
                    case "f71862fg":
                        _superIOs.Add(new LMChip(Chip.F71862, path));
                        break;
                    case "f71869":
                        _superIOs.Add(new LMChip(Chip.F71869, path));
                        break;
                    case "f71869a":
                        _superIOs.Add(new LMChip(Chip.F71869A, path));
                        break;
                    case "f71882fg":
                        _superIOs.Add(new LMChip(Chip.F71882, path));
                        break;
                    case "f71889a":
                        _superIOs.Add(new LMChip(Chip.F71889AD, path));
                        break;
                    case "f71878ad":
                        _superIOs.Add(new LMChip(Chip.F71878AD, path));
                        break;
                    case "f71889ed":
                        _superIOs.Add(new LMChip(Chip.F71889ED, path));
                        break;
                    case "f71889fg":
                        _superIOs.Add(new LMChip(Chip.F71889F, path));
                        break;
                    case "f71808e":
                        _superIOs.Add(new LMChip(Chip.F71808E, path));
                        break;

                    case "it8705":
                        _superIOs.Add(new LMChip(Chip.IT8705F, path));
                        break;
                    case "it8712":
                        _superIOs.Add(new LMChip(Chip.IT8712F, path));
                        break;
                    case "it8716":
                        _superIOs.Add(new LMChip(Chip.IT8716F, path));
                        break;
                    case "it8718":
                        _superIOs.Add(new LMChip(Chip.IT8718F, path));
                        break;
                    case "it8720":
                        _superIOs.Add(new LMChip(Chip.IT8720F, path));
                        break;

                    case "nct6775":
                        _superIOs.Add(new LMChip(Chip.NCT6771F, path));
                        break;
                    case "nct6776":
                        _superIOs.Add(new LMChip(Chip.NCT6776F, path));
                        break;
                    case "nct6779":
                        _superIOs.Add(new LMChip(Chip.NCT6779D, path));
                        break;
                    case "nct6791":
                        _superIOs.Add(new LMChip(Chip.NCT6791D, path));
                        break;
                    case "nct6792":
                        _superIOs.Add(new LMChip(Chip.NCT6792D, path));
                        break;
                    case "nct6793":
                        _superIOs.Add(new LMChip(Chip.NCT6793D, path));
                        break;
                    case "nct6795":
                        _superIOs.Add(new LMChip(Chip.NCT6795D, path));
                        break;
                    case "nct6796":
                        _superIOs.Add(new LMChip(Chip.NCT6796D, path));
                        break;
                    case "nct6797":
                        _superIOs.Add(new LMChip(Chip.NCT6797D, path));
                        break;
                    case "nct6798":
                        _superIOs.Add(new LMChip(Chip.NCT6798D, path));
                        break;
                    case "nct6799":
                        _superIOs.Add(new LMChip(Chip.NCT6799D, path));
                        break;

                    case "w83627ehf":
                        _superIOs.Add(new LMChip(Chip.W83627EHF, path));
                        break;
                    case "w83627dhg":
                        _superIOs.Add(new LMChip(Chip.W83627DHG, path));
                        break;
                    case "w83667hg":
                        _superIOs.Add(new LMChip(Chip.W83667HG, path));
                        break;
                    case "w83627hf":
                        _superIOs.Add(new LMChip(Chip.W83627HF, path));
                        break;
                    case "w83627thf":
                        _superIOs.Add(new LMChip(Chip.W83627THF, path));
                        break;
                    case "w83687thf":
                        _superIOs.Add(new LMChip(Chip.W83687THF, path));
                        break;
                }
            }
        }
    }

    public IReadOnlyList<ISuperIO> SuperIO
    {
        get { return _superIOs; }
    }

    public void Close()
    {
        foreach (ISuperIO superIO in _superIOs)
        {
            if (superIO is LMChip lmChip)
                lmChip.Close();
        }
    }

    private class LMChip : ISuperIO
    {
        private readonly FileStream[] _fanStreams;
        private readonly string[] _pwmEnablePaths;
        private readonly string[] _pwmPaths;
        private readonly string[] _restorePwmEnableValues;
        private readonly FileStream[] _temperatureStreams;

        private readonly FileStream[] _voltageStreams;

        public LMChip(Chip chip, string path)
        {
            Chip = chip;

            string[] voltagePaths = Directory.GetFiles(path, "in*_input");
            Voltages = new float?[voltagePaths.Length];
            _voltageStreams = new FileStream[voltagePaths.Length];
            for (int i = 0; i < voltagePaths.Length; i++)
                _voltageStreams[i] = new FileStream(voltagePaths[i], FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            string[] temperaturePaths = Directory.GetFiles(path, "temp*_input");
            Temperatures = new float?[temperaturePaths.Length];
            _temperatureStreams = new FileStream[temperaturePaths.Length];
            for (int i = 0; i < temperaturePaths.Length; i++)
                _temperatureStreams[i] = new FileStream(temperaturePaths[i], FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            string[] fanPaths = Directory.GetFiles(path, "fan*_input");
            Fans = new float?[fanPaths.Length];
            _fanStreams = new FileStream[fanPaths.Length];
            for (int i = 0; i < fanPaths.Length; i++)
                _fanStreams[i] = new FileStream(fanPaths[i], FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            var pwmPaths = GetIndexedPaths(path, @"^pwm(?<index>\d+)$");
            var pwmEnablePaths = GetIndexedPaths(path, @"^pwm(?<index>\d+)_enable$");
            int controlCount = Math.Max(pwmPaths.Length, pwmEnablePaths.Length);

            Controls = controlCount > 0 ? new float?[controlCount] : Array.Empty<float?>();
            _pwmPaths = controlCount > 0 ? pwmPaths : Array.Empty<string>();
            _pwmEnablePaths = controlCount > 0 ? pwmEnablePaths : Array.Empty<string>();
            _restorePwmEnableValues = new string[_pwmEnablePaths.Length];

            for (int i = 0; i < _pwmEnablePaths.Length; i++)
            {
                _restorePwmEnableValues[i] = ReadWholeFile(_pwmEnablePaths[i])?.Trim();
            }
        }

        public Chip Chip { get; }

        public float?[] Controls { get; }

        public float?[] Fans { get; }

        public float?[] Temperatures { get; }

        public float?[] Voltages { get; }

        public byte? ReadGpio(int index)
        {
            return null;
        }

        public void WriteGpio(int index, byte value)
        { }

        public string GetReport()
        {
            return null;
        }

        public void SetControl(int index, byte? value)
        {
            if (index < 0 || index >= _pwmPaths.Length)
                return;

            var pwmPath = _pwmPaths[index];
            if (string.IsNullOrEmpty(pwmPath))
                return;

            try
            {
                if (value.HasValue)
                {
                    SetManualControlMode(index);
                    File.WriteAllText(pwmPath, value.Value.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    RestoreAutomaticControlMode(index);
                }
            }
            catch
            {
                // Ignore write errors. hwmon permissions and mode support vary by distro/chip.
            }
        }

        public void Update()
        {
            for (int i = 0; i < Voltages.Length; i++)
            {
                string s = ReadFirstLine(_voltageStreams[i]);
                try
                {
                    Voltages[i] = 0.001f *
                                  long.Parse(s, CultureInfo.InvariantCulture);
                }
                catch
                {
                    Voltages[i] = null;
                }
            }

            for (int i = 0; i < Temperatures.Length; i++)
            {
                string s = ReadFirstLine(_temperatureStreams[i]);
                try
                {
                    Temperatures[i] = 0.001f *
                                      long.Parse(s, CultureInfo.InvariantCulture);
                }
                catch
                {
                    Temperatures[i] = null;
                }
            }

            for (int i = 0; i < Fans.Length; i++)
            {
                string s = ReadFirstLine(_fanStreams[i]);
                try
                {
                    Fans[i] = long.Parse(s, CultureInfo.InvariantCulture);
                }
                catch
                {
                    Fans[i] = null;
                }
            }

            for (int i = 0; i < Controls.Length; i++)
            {
                var pwmPath = i < _pwmPaths.Length ? _pwmPaths[i] : null;
                if (string.IsNullOrEmpty(pwmPath))
                {
                    Controls[i] = null;
                    continue;
                }

                var raw = ReadWholeFile(pwmPath);
                if (int.TryParse(raw?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                    Controls[i] = (float)Math.Round(value * 100.0f / 255.0f, 0);
                else
                    Controls[i] = null;
            }
        }

        private static string[] GetIndexedPaths(string basePath, string pattern)
        {
            var regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
            var indexed = Directory.GetFiles(basePath, "pwm*")
                .Select(file =>
                {
                    var match = regex.Match(Path.GetFileName(file));
                    if (!match.Success)
                        return (ok: false, index: -1, path: (string)null);

                    return int.TryParse(match.Groups["index"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index)
                        ? (ok: true, index, path: file)
                        : (ok: false, index: -1, path: (string)null);
                })
                .Where(x => x.ok)
                .ToArray();

            if (indexed.Length == 0)
                return Array.Empty<string>();

            int maxIndex = indexed.Max(x => x.index);
            var paths = new string[maxIndex + 1];
            foreach (var item in indexed)
                paths[item.index] = item.path;

            return paths;
        }

        private static string ReadWholeFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            try
            {
                return File.ReadAllText(path);
            }
            catch
            {
                return null;
            }
        }

        private void SetManualControlMode(int index)
        {
            if (index < 0 || index >= _pwmEnablePaths.Length)
                return;

            var pwmEnablePath = _pwmEnablePaths[index];
            if (string.IsNullOrEmpty(pwmEnablePath))
                return;

            try
            {
                var currentMode = ReadWholeFile(pwmEnablePath)?.Trim();
                if (currentMode != "1")
                    File.WriteAllText(pwmEnablePath, "1");
            }
            catch
            {
                // Ignore mode-switch errors. Some channels are read-only or use different mode values.
            }
        }

        private void RestoreAutomaticControlMode(int index)
        {
            if (index < 0 || index >= _pwmEnablePaths.Length)
                return;

            var pwmEnablePath = _pwmEnablePaths[index];
            if (string.IsNullOrEmpty(pwmEnablePath))
                return;

            var restoreValue = _restorePwmEnableValues[index];
            if (string.IsNullOrEmpty(restoreValue))
                return;

            try
            {
                File.WriteAllText(pwmEnablePath, restoreValue);
            }
            catch
            {
                // Ignore restore errors for chips that block mode changes at runtime.
            }
        }

        private static string ReadFirstLine(Stream stream)
        {
            StringBuilder sb = new();
            try
            {
                stream.Seek(0, SeekOrigin.Begin);
                int b = stream.ReadByte();
                while (b is not -1 and not 10)
                {
                    sb.Append((char)b);
                    b = stream.ReadByte();
                }
            }
            catch
            { }

            return sb.ToString();
        }

        public void Close()
        {
            foreach (FileStream stream in _voltageStreams)
                stream.Close();

            foreach (FileStream stream in _temperatureStreams)
                stream.Close();

            foreach (FileStream stream in _fanStreams)
                stream.Close();
        }
    }
}
