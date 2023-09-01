// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;

namespace LibreHardwareMonitor.Hardware;

internal class Sensor : ISensor
{
    private readonly string _defaultName;
    private readonly Hardware _hardware;
    private readonly ISettings _settings;
    private readonly bool _trackMinMax;
    private readonly List<SensorValue> _values = new();
    private int _count;
    private float? _currentValue;
    private string _name;
    private float _sum;
    private readonly AverageAccumulator _average = new();

    private TimeSpan _valuesTimeWindow = TimeSpan.FromDays(1.0);

    private class AverageAccumulator
    {
        private TimeSpan _timeSpan = TimeSpan.Zero;
        private DateTime? _lastTime;
        private double _sum = double.NaN;
        private float _lastValue;

        public float? Average => double.IsNaN(_sum) ? null : (float)_sum;

        public void AddValue(float value, DateTime time)
        {
            if (float.IsNaN(value))
                return;

            if (_lastTime.HasValue)
            {
                var delta = time - _lastTime.Value;
                if (delta.TotalSeconds > 0) // can be negative if system time changed
                {
                    var weighted = _sum * _timeSpan.TotalSeconds + (_lastValue + value) * delta.TotalSeconds / 2;
                    _timeSpan += delta;
                    _sum = weighted / _timeSpan.TotalSeconds;
                }
            }
            else
            {
                _sum = value;
            }
            _lastTime = time;
            _lastValue = value;
        }

        public void Reset()
        {
            _timeSpan = TimeSpan.Zero;
            _lastTime = null;
            _sum = double.NaN;
        }
    }

    public Sensor(string name, int index, SensorType sensorType, Hardware hardware, ISettings settings) :
        this(name, index, sensorType, hardware, null, settings)
    { }

    public Sensor(string name, int index, SensorType sensorType, Hardware hardware, ParameterDescription[] parameterDescriptions, ISettings settings) :
        this(name, index, false, sensorType, hardware, parameterDescriptions, settings)
    { }

    public Sensor
    (
        string name,
        int index,
        bool defaultHidden,
        SensorType sensorType,
        Hardware hardware,
        ParameterDescription[] parameterDescriptions,
        ISettings settings,
        bool disableHistory = false)
    {
        Index = index;
        IsDefaultHidden = defaultHidden;
        SensorType = sensorType;
        _hardware = hardware;

        Parameter[] parameters = new Parameter[parameterDescriptions?.Length ?? 0];
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameterDescriptions != null)
                parameters[i] = new Parameter(parameterDescriptions[i], this, settings);
        }

        Parameters = parameters;

        _settings = settings;
        _defaultName = name;
        _name = settings.GetValue(new Identifier(Identifier, "name").ToString(), name);
        _trackMinMax = !disableHistory;
        if (disableHistory)
        {
            _valuesTimeWindow = TimeSpan.Zero;
        }

        GetSensorValuesFromSettings();

        hardware.Closing += delegate { SetSensorValuesToSettings(); };
    }

    public IControl Control { get; internal set; }

    public IHardware Hardware
    {
        get { return _hardware; }
    }

    public Identifier Identifier
    {
        get { return new Identifier(_hardware.Identifier, SensorType.ToString().ToLowerInvariant(), Index.ToString(CultureInfo.InvariantCulture)); }
    }

    public int Index { get; }

    public bool IsDefaultHidden { get; }

    public float? Max { get; private set; }

    public float? Min { get; private set; }

    public float? Average => _average.Average;

    public string Name
    {
        get { return _name; }
        set
        {
            _name = !string.IsNullOrEmpty(value) ? value : _defaultName;

            _settings.SetValue(new Identifier(Identifier, "name").ToString(), _name);
        }
    }

    public IReadOnlyList<IParameter> Parameters { get; }

    public SensorType SensorType { get; }

    public virtual float? Value
    {
        get { return _currentValue; }
        set
        {
            DateTime now = DateTime.UtcNow;
            if (_valuesTimeWindow != TimeSpan.Zero)
            {
                while (_values.Count > 0 && now - _values[0].Time > _valuesTimeWindow)
                    _values.RemoveAt(0);

                if (value.HasValue)
                {
                    _sum += value.Value;
                    _count++;
                    if (_count == 4)
                    {
                        AppendValue(_sum / _count, now);
                        _sum = 0;
                        _count = 0;
                    }
                }
            }

            _currentValue = value;
            if (_trackMinMax)
            {
                if (!Min.HasValue || Min > value)
                    Min = value;

                if (!Max.HasValue || Max < value)
                    Max = value;

                if (null != value)
                    _average.AddValue((float)value, now);
            }
        }
    }

    public IEnumerable<SensorValue> Values
    {
        get { return _values; }
    }

    public TimeSpan ValuesTimeWindow
    {
        get { return _valuesTimeWindow; }
        set
        {
            _valuesTimeWindow = value;
            if (value == TimeSpan.Zero)
                ClearValues();
        }
    }

    public void ResetMin()
    {
        Min = null;
    }

    public void ResetMax()
    {
        Max = null;
    }

    public void ResetAverage() => _average.Reset();

    public void ClearValues() => _values.Clear();

    public void Accept(IVisitor visitor)
    {
        if (visitor == null)
            throw new ArgumentNullException(nameof(visitor));

        visitor.VisitSensor(this);
    }

    public void Traverse(IVisitor visitor)
    {
        foreach (IParameter parameter in Parameters)
            parameter.Accept(visitor);
    }

    private void SetSensorValuesToSettings()
    {
        using MemoryStream memoryStream = new();
        using GZipStream gZipStream = new(memoryStream, CompressionMode.Compress);
        using BufferedStream outputStream = new(gZipStream, 65536);
        using (BinaryWriter binaryWriter = new(outputStream))
        {
            long t = 0;

            foreach (SensorValue sensorValue in _values)
            {
                long v = sensorValue.Time.ToBinary();
                binaryWriter.Write(v - t);
                t = v;
                binaryWriter.Write(sensorValue.Value);
            }

            binaryWriter.Flush();
        }

        _settings.SetValue(new Identifier(Identifier, "values").ToString(), Convert.ToBase64String(memoryStream.ToArray()));
    }

    private void GetSensorValuesFromSettings()
    {
        string name = new Identifier(Identifier, "values").ToString();
        string s = _settings.GetValue(name, null);

        if (!string.IsNullOrEmpty(s))
        {
            try
            {
                byte[] array = Convert.FromBase64String(s);
                DateTime now = DateTime.UtcNow;

                using MemoryStream memoryStream = new(array);
                using GZipStream gZipStream = new(memoryStream, CompressionMode.Decompress);
                using MemoryStream destination = new();

                gZipStream.CopyTo(destination);
                destination.Seek(0, SeekOrigin.Begin);

                using BinaryReader reader = new(destination);
                try
                {
                    long t = 0;
                    long readLen = reader.BaseStream.Length - reader.BaseStream.Position;
                    while (readLen > 0)
                    {
                        t += reader.ReadInt64();
                        DateTime time = DateTime.FromBinary(t);
                        if (time > now)
                            break;

                        float value = reader.ReadSingle();
                        AppendValue(value, time);
                        readLen = reader.BaseStream.Length - reader.BaseStream.Position;
                    }
                }
                catch (EndOfStreamException)
                { }
            }
            catch
            {
                // Ignored.
            }
        }

        if (_values.Count > 0)
            AppendValue(float.NaN, DateTime.UtcNow);

        //remove the value string from the settings to reduce memory usage
        _settings.Remove(name);
    }

    private void AppendValue(float value, DateTime time)
    {
        if (_values.Count >= 2 && _values[_values.Count - 1].Value == value && _values[_values.Count - 2].Value == value)
            _values[_values.Count - 1] = new SensorValue(value, time);
        else
            _values.Add(new SensorValue(value, time));
    }
}
