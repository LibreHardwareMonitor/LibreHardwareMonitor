// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using LibreHardwareMonitor.Hardware;

namespace LibreHardwareMonitor.Utilities;

public class Logger
{
    private const string FileNameFormat = "LibreHardwareMonitorLog-{0:yyyy-MM-dd}{1}.csv";

    private readonly IComputer _computer;

    private DateTime _day = DateTime.MinValue;
    private string _fileName;
    private string[] _identifiers;
    private ISensor[] _sensors;
    private DateTime _lastLoggedTime = DateTime.MinValue;

    public LoggerFileRotation FileRotationMethod = LoggerFileRotation.PerSession;
    /* * Purpose: Manages the log directory path and triggers a reset of the 
     * filename whenever the path is updated to ensure logs are written to the new location.
     */
    private string _logPath;
    private bool _isInitialized = false;

    public string LogPath
    {
        get => _logPath;
        set
        {
            if (_logPath != value)
            {
                _logPath = value;
                
                // Reset filename to force path re-evaluation in the next Log() call
                _fileName = null; 
                
                // Mark as initialized to allow logging operations
                _isInitialized = true;
            }
        }
    }

    public Logger(IComputer computer)
    {
        _computer = computer;
        _computer.HardwareAdded += HardwareAdded;
        _computer.HardwareRemoved += HardwareRemoved;
    }

    private void HardwareRemoved(IHardware hardware)
    {
        hardware.SensorAdded -= SensorAdded;
        hardware.SensorRemoved -= SensorRemoved;

        foreach (ISensor sensor in hardware.Sensors)
            SensorRemoved(sensor);

        foreach (IHardware subHardware in hardware.SubHardware)
            HardwareRemoved(subHardware);
    }

    private void HardwareAdded(IHardware hardware)
    {
        foreach (ISensor sensor in hardware.Sensors)
            SensorAdded(sensor);

        hardware.SensorAdded += SensorAdded;
        hardware.SensorRemoved += SensorRemoved;

        foreach (IHardware subHardware in hardware.SubHardware)
            HardwareAdded(subHardware);
    }

    private void SensorAdded(ISensor sensor)
    {
        if (_sensors == null)
            return;

        for (int i = 0; i < _sensors.Length; i++)
        {
            if (sensor.Identifier.ToString() == _identifiers[i])
                _sensors[i] = sensor;
        }
    }

    private void SensorRemoved(ISensor sensor)
    {
        if (_sensors == null)
            return;

        for (int i = 0; i < _sensors.Length; i++)
        {
            if (sensor == _sensors[i])
                _sensors[i] = null;
        }
    }

    /* * Purpose: Resolve the log file path by prioritizing custom LogPath and 
     * ensuring the target directory exists.
     */
    private string GetFileName(DateTime date, uint sessionNumber = 0)
    {
        // Use custom path if available, otherwise fallback to EXE directory
        string folder = !string.IsNullOrEmpty(LogPath) ? LogPath : AppDomain.CurrentDomain.BaseDirectory;

        // Auto-create folder only if a custom path is set
        if (!string.IsNullOrEmpty(LogPath) && !Directory.Exists(folder))
        {
            try { Directory.CreateDirectory(folder); } catch { }
        }

        return Path.Combine(folder, string.Format(FileNameFormat, date, sessionNumber == 0 ? "" : "-" + sessionNumber));
    }

    private bool OpenExistingLogFile()
    {
        if (!File.Exists(_fileName))
            return false;

        try
        {
            string line;
            using (StreamReader reader = new StreamReader(_fileName))
                line = reader.ReadLine();

            if (string.IsNullOrEmpty(line))
                return false;

            _identifiers = line.Split(',').Skip(1).ToArray();
        }
        catch
        {
            _identifiers = null;
            return false;
        }

        if (_identifiers.Length == 0)
        {
            _identifiers = null;
            return false;
        }

        _sensors = new ISensor[_identifiers.Length];
        SensorVisitor visitor = new SensorVisitor(sensor =>
        {
            for (int i = 0; i < _identifiers.Length; i++)
                if (sensor.Identifier.ToString() == _identifiers[i])
                    _sensors[i] = sensor;
        });
        visitor.VisitComputer(_computer);
        return true;
    }

    private void CreateNewLogFile()
    {
        IList<ISensor> list = new List<ISensor>();
        SensorVisitor visitor = new SensorVisitor(sensor =>
        {
            list.Add(sensor);
        });
        visitor.VisitComputer(_computer);
        _sensors = list.ToArray();
        _identifiers = _sensors.Select(s => s.Identifier.ToString()).ToArray();

        using (StreamWriter writer = new StreamWriter(_fileName, false))
        {
            writer.Write(",");
            for (int i = 0; i < _sensors.Length; i++)
            {
                writer.Write(_sensors[i].Identifier);
                if (i < _sensors.Length - 1)
                    writer.Write(",");
                else
                    writer.WriteLine();
            }

            writer.Write("Time,");
            for (int i = 0; i < _sensors.Length; i++)
            {
                writer.Write('"');
                writer.Write(_sensors[i].Name);
                writer.Write('"');
                if (i < _sensors.Length - 1)
                    writer.Write(",");
                else
                    writer.WriteLine();
            }
        }
    }

    public TimeSpan LoggingInterval { get; set; }

   /* * Purpose: Handles log writing with protection against incorrect file locations.
     * Ensures logs are only written after initialization and within the correct directory.
     */
    public void Log()
    {
        // Safety guard: Prevent logging until initialization and path sync are complete
        if (!_isInitialized || string.IsNullOrEmpty(_logPath))
            return;

        DateTime now = DateTime.Now;

        if (_lastLoggedTime + LoggingInterval - new TimeSpan(5000000) > now)
            return;

        switch (FileRotationMethod)
        {
            case LoggerFileRotation.PerSession:
                // Reset filename if it's empty, missing, or pointing to an outdated path
                if (string.IsNullOrEmpty(_fileName) || !File.Exists(_fileName) || !_fileName.StartsWith(_logPath))
                {
                    uint sessionNumber = 1;
                    do {
                        _fileName = GetFileName(DateTime.Now, sessionNumber);
                        sessionNumber++;
                    } while (File.Exists(_fileName));
                    CreateNewLogFile();
                }
                break;
            case LoggerFileRotation.Daily:
                // Ensure filename matches current date and resides in the correct directory
                if (_day != now.Date || string.IsNullOrEmpty(_fileName) || !File.Exists(_fileName) || !_fileName.StartsWith(_logPath))
                {
                    _day = now.Date;
                    _fileName = GetFileName(_day);
                    if (!OpenExistingLogFile())
                        CreateNewLogFile();
                }
                break;
        }

        // Final validation before I/O operations
        if (string.IsNullOrEmpty(_fileName))
            return;
        try
        {
            using (StreamWriter writer = new StreamWriter(new FileStream(_fileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
            {
                writer.Write(now.ToString("G", CultureInfo.InvariantCulture));
                writer.Write(",");
                for (int i = 0; i < _sensors.Length; i++)
                {
                    if (_sensors[i] != null)
                    {
                        float? value = _sensors[i].Value;
                        if (value.HasValue)
                            writer.Write(value.Value.ToString("R", CultureInfo.InvariantCulture));
                    }
                    if (i < _sensors.Length - 1)
                        writer.Write(",");
                    else
                        writer.WriteLine();
                }
            }
        }
        catch (IOException) { }

        _lastLoggedTime = now;
    }
}