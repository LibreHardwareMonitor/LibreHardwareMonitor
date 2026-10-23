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

namespace LibreHardwareMonitor.Windows.Forms.Utilities;

public class Logger
{
    private const string FileNameFormat = "LibreHardwareMonitorLog-{0:yyyy-MM-dd}{1}.csv";

    private readonly IComputer _computer;

    private DateTime _day = DateTime.MinValue;
    private string _fileName;
    private string _logDirectory = string.Empty;
    private string _sessionFileName;
    private string[] _identifiers;
    private ISensor[] _sensors;
    private DateTime _lastLoggedTime = DateTime.MinValue;

    public LoggerFileRotation FileRotationMethod = LoggerFileRotation.PerSession;

    // Folder where log files are written. Empty means the application directory.
    public string LogDirectory
    {
        get => _logDirectory;
        set
        {
            if (_logDirectory == value)
                return;

            _logDirectory = value ?? string.Empty;
            // Force the next Log() call to open a file in the new folder.
            _fileName = null;
            _sessionFileName = null;
            _day = DateTime.MinValue;
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

    private string GetFileName(DateTime date, uint sessionNumber = 0)
    {
        string directory = string.IsNullOrWhiteSpace(LogDirectory) ? AppDomain.CurrentDomain.BaseDirectory : LogDirectory;
        return Path.Combine(directory, string.Format(FileNameFormat, date, sessionNumber == 0 ? "" : "-" + sessionNumber));
    }

    // Creates the log folder if it is missing (e.g. deleted, or a removable drive was re-attached).
    private bool EnsureLogDirectory()
    {
        if (string.IsNullOrWhiteSpace(LogDirectory))
            return true;

        try
        {
            Directory.CreateDirectory(LogDirectory);
            return true;
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is NotSupportedException)
        {
            return false;
        }
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

    public void Log()
    {
        DateTime now = DateTime.Now;

        if (_lastLoggedTime + LoggingInterval - new TimeSpan(5000000) > now)
            return;

        if (!EnsureLogDirectory())
            return;

        switch (FileRotationMethod)
        {
            case LoggerFileRotation.PerSession:
                // One file for the whole application run; only create a new one if there is none yet
                // (first log, folder changed) or it was deleted.
                if (_sessionFileName == null || !File.Exists(_sessionFileName))
                {
                    uint sessionNumber = 1;
                    do {
                        _fileName = GetFileName(DateTime.Now, sessionNumber);
                        sessionNumber++;
                    } while (File.Exists(_fileName));
                    CreateNewLogFile();
                    _sessionFileName = _fileName;
                }
                else
                {
                    _fileName = _sessionFileName;
                }
                break;
            case LoggerFileRotation.Daily:
                // Create a new file if the day has changed or the file does not exist
                if (_day != now.Date || !File.Exists(_fileName))
                {
                    _day = now.Date;
                    _fileName = GetFileName(_day);
                    if (!OpenExistingLogFile())
                        CreateNewLogFile();
                }
                break;
        }

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