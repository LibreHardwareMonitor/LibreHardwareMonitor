// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace LibreHardwareMonitor.Hardware.Lpc
{
    internal class LMSensors
    {
        private readonly List<LMChip> _lmChips = new List<LMChip>();

        public LMSensors()
        {
            string[] basePaths = Directory.GetDirectories("/sys/class/hwmon/");
            foreach (string basePath in basePaths)
            {
                foreach (string devicePath in new[] { "/device", "" })
                {
                    string path = basePath + devicePath;

                    string name = null;
                    try
                    {
                        using (StreamReader reader = new StreamReader(path + "/name"))
                            name = reader.ReadLine();
                    }
                    catch (IOException) { }

                    switch (name)
                    {
                        case "atk0110":
                            _lmChips.Add(new LMChip(Chip.ATK0110, path)); break;

                        case "f71858fg":
                            _lmChips.Add(new LMChip(Chip.F71858, path)); break;
                        case "f71862fg":
                            _lmChips.Add(new LMChip(Chip.F71862, path)); break;
                        case "f71869":
                            _lmChips.Add(new LMChip(Chip.F71869, path)); break;
                        case "f71869a":
                            _lmChips.Add(new LMChip(Chip.F71869A, path)); break;
                        case "f71882fg":
                            _lmChips.Add(new LMChip(Chip.F71882, path)); break;
                        case "f71889a":
                            _lmChips.Add(new LMChip(Chip.F71889AD, path)); break;
                        case "f71878ad":
                            _lmChips.Add(new LMChip(Chip.F71878AD, path)); break;
                        case "f71889ed":
                            _lmChips.Add(new LMChip(Chip.F71889ED, path)); break;
                        case "f71889fg":
                            _lmChips.Add(new LMChip(Chip.F71889F, path)); break;
                        case "f71808e":
                            _lmChips.Add(new LMChip(Chip.F71808E, path)); break;

                        case "it8705":
                            _lmChips.Add(new LMChip(Chip.IT8705F, path)); break;
                        case "it8712":
                            _lmChips.Add(new LMChip(Chip.IT8712F, path)); break;
                        case "it8716":
                            _lmChips.Add(new LMChip(Chip.IT8716F, path)); break;
                        case "it8718":
                            _lmChips.Add(new LMChip(Chip.IT8718F, path)); break;
                        case "it8720":
                            _lmChips.Add(new LMChip(Chip.IT8720F, path)); break;

                        case "nct6775":
                            _lmChips.Add(new LMChip(Chip.NCT6771F, path)); break;
                        case "nct6776":
                            _lmChips.Add(new LMChip(Chip.NCT6776F, path)); break;
                        case "nct6779":
                            _lmChips.Add(new LMChip(Chip.NCT6779D, path)); break;
                        case "nct6791":
                            _lmChips.Add(new LMChip(Chip.NCT6791D, path)); break;
                        case "nct6792":
                            _lmChips.Add(new LMChip(Chip.NCT6792D, path)); break;
                        case "nct6793":
                            _lmChips.Add(new LMChip(Chip.NCT6793D, path)); break;
                        case "nct6795":
                            _lmChips.Add(new LMChip(Chip.NCT6795D, path)); break;
                        case "nct6796":
                            _lmChips.Add(new LMChip(Chip.NCT6796D, path)); break;
                        case "nct6797":
                            _lmChips.Add(new LMChip(Chip.NCT6797D, path)); break;
                        case "nct6798":
                            _lmChips.Add(new LMChip(Chip.NCT6798D, path)); break;

                        case "w83627ehf":
                            _lmChips.Add(new LMChip(Chip.W83627EHF, path)); break;
                        case "w83627dhg":
                            _lmChips.Add(new LMChip(Chip.W83627DHG, path)); break;
                        case "w83667hg":
                            _lmChips.Add(new LMChip(Chip.W83667HG, path)); break;
                        case "w83627hf":
                            _lmChips.Add(new LMChip(Chip.W83627HF, path)); break;
                        case "w83627thf":
                            _lmChips.Add(new LMChip(Chip.W83627THF, path)); break;
                        case "w83687thf":
                            _lmChips.Add(new LMChip(Chip.W83687THF, path)); break;
                    }
                }
            }
        }

        public void Close()
        {
            foreach (LMChip lmChip in _lmChips)
                lmChip.Close();
        }

        public ISuperIO[] SuperIO
        {
            get
            {
                return _lmChips.ToArray();
            }
        }

        private class LMChip : ISuperIO
        {
            private string _path;
            private readonly Chip _chip;

            private readonly float?[] _voltages;
            private readonly float?[] _temperatures;
            private readonly float?[] _fans;
            private readonly float?[] _controls;

            private readonly FileStream[] _voltageStreams;
            private readonly FileStream[] _temperatureStreams;
            private readonly FileStream[] _fanStreams;

            public Chip Chip { get { return _chip; } }
            public float?[] Voltages { get { return _voltages; } }
            public float?[] Temperatures { get { return _temperatures; } }
            public float?[] Fans { get { return _fans; } }
            public float?[] Controls { get { return _controls; } }

            public LMChip(Chip chip, string path)
            {
                _path = path;
                _chip = chip;

                string[] voltagePaths = Directory.GetFiles(path, "in*_input");
                _voltages = new float?[voltagePaths.Length];
                _voltageStreams = new FileStream[voltagePaths.Length];
                for (int i = 0; i < voltagePaths.Length; i++)
                    _voltageStreams[i] = new FileStream(voltagePaths[i], FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                string[] temperaturePaths = Directory.GetFiles(path, "temp*_input");
                _temperatures = new float?[temperaturePaths.Length];
                _temperatureStreams = new FileStream[temperaturePaths.Length];
                for (int i = 0; i < temperaturePaths.Length; i++)
                    _temperatureStreams[i] = new FileStream(temperaturePaths[i], FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                string[] fanPaths = Directory.GetFiles(path, "fan*_input");
                _fans = new float?[fanPaths.Length];
                _fanStreams = new FileStream[fanPaths.Length];
                for (int i = 0; i < fanPaths.Length; i++)
                    _fanStreams[i] = new FileStream(fanPaths[i], FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                _controls = new float?[0];
            }

            public byte? ReadGPIO(int index)
            {
                return null;
            }

            public void WriteGPIO(int index, byte value) { }

            public string GetReport()
            {
                return null;
            }

            public void SetControl(int index, byte? value) { }

            private string ReadFirstLine(Stream stream)
            {
                StringBuilder sb = new StringBuilder();
                try
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    int b = stream.ReadByte();
                    while (b != -1 && b != 10)
                    {
                        sb.Append((char)b);
                        b = stream.ReadByte();
                    }
                }
                catch { }
                return sb.ToString();
            }

            public void Update()
            {
                for (int i = 0; i < _voltages.Length; i++)
                {
                    string s = ReadFirstLine(_voltageStreams[i]);
                    try
                    {
                        _voltages[i] = 0.001f *
                          long.Parse(s, CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        _voltages[i] = null;
                    }
                }

                for (int i = 0; i < _temperatures.Length; i++)
                {
                    string s = ReadFirstLine(_temperatureStreams[i]);
                    try
                    {
                        _temperatures[i] = 0.001f *
                          long.Parse(s, CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        _temperatures[i] = null;
                    }
                }

                for (int i = 0; i < _fans.Length; i++)
                {
                    string s = ReadFirstLine(_fanStreams[i]);
                    try
                    {
                        _fans[i] = long.Parse(s, CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        _fans[i] = null;
                    }
                }
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
}
