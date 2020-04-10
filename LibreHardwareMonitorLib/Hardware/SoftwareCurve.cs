// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Timers;

namespace LibreHardwareMonitor.Hardware
{
    public class SoftwareCurve
    {
        internal delegate void SoftwareCurveValueHandler(SoftwareCurve softwareCurve);        
        internal event SoftwareCurveValueHandler SoftwareCurveValueChanged;
        internal event SoftwareCurveValueHandler SoftwareCurveAbort;
            
        public readonly List<ISoftwareCurvePoint> Points;
        public readonly ISensor Sensor;

        private Timer _timer;
        private bool _previousValueAssigend;
        private float _previousSensorValue;
        private byte _previousNoValue;
        internal float Value { get; private set; }

        internal static bool TryParse(string settings, out List<ISoftwareCurvePoint> points)
        {
            points = new List<ISoftwareCurvePoint>();
            if (settings.Length < 1)
                return false;

            var splitPoints = settings.Split(';');
            if (splitPoints.Length < 1)
                return false;
            
            for(int i = 0; i < splitPoints.Length - 1; i++)
            {
                var splitPoint = splitPoints[i].Split(':');
                if (splitPoint.Length < 2)
                    return false;

                if (!float.TryParse(splitPoint[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float xpoint))
                    return false;

                if (!float.TryParse(splitPoint[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float ypoint))
                    return false;

                points.Add(new SoftwareCurvePoint { SensorValue = xpoint, ControlValue = ypoint });
            }

            if (points.Count < 2)
                return false;

            return true;
        }
        
        internal static bool TryParse(string settings, out string sensorIdentifier)
        {
            sensorIdentifier = null;

            if (settings.Length < 1)
                return false;

            var split = settings.Split(';');
            if (split.Length < 1)
                return false;

            sensorIdentifier = split[split.Length - 1];

            return !string.IsNullOrEmpty(sensorIdentifier);
        }
        
        internal static ISensor FindSensor(IHardware hardware, string sensorIdentifier)
        {
            foreach (ISensor sensor in hardware.Sensors){
                Debug.WriteLine(sensor.Identifier.ToString() + " " + sensorIdentifier);
                if (sensor.Identifier.ToString() == sensorIdentifier)
                    return sensor;
            }
            
            foreach (IHardware subHardware in hardware.SubHardware)
                return FindSensor(subHardware, sensorIdentifier);

            return null;
        }

        internal SoftwareCurve(List<ISoftwareCurvePoint> points, ISensor sensor){
            Points = points;
            Sensor = sensor;
        }

        internal void Start()
        {
            if (_timer == null)
            {
                _timer = new Timer();
            }
            else
            {
                if (_timer.Enabled) return;
            }
            
            _timer.Elapsed += Tick;
            _timer.Interval = 1000;
            _timer.Start();
        }
        
        private void Tick(object s, ElapsedEventArgs e)
        { 
            if (Sensor.Value.HasValue){
                float sensorValue = Sensor.Value.Value;
                if (!_previousValueAssigend || sensorValue != _previousSensorValue)
                {
                    _previousSensorValue = sensorValue;

                    // As of writing this, a Control is controlled with percentages. Round away decimals
                    float newValue = (float)Math.Round(Calculate(sensorValue));
                    
                    if (Value == newValue && _previousValueAssigend)
                        return;

                    Value = newValue;
                    _previousValueAssigend = true;
                    SoftwareCurveValueChanged?.Invoke(this);
                }
            }
            else
            {
                _previousNoValue++;
                if (_previousNoValue > 3) SoftwareCurveAbort?.Invoke(this);
            }
        }
        private float Calculate(float sensorValue) {

            for (int i = 1; i < Points.Count; i++)
            {
                var point1 = Points[i - 1];
                var point2 = Points[i];

                if (sensorValue == point1.SensorValue)
                    return point1.ControlValue;

                if (sensorValue > point1.SensorValue && sensorValue < point2.SensorValue)
                {
                    var m = (point2.ControlValue - point1.ControlValue) / (point2.SensorValue - point1.SensorValue);
                    var b = point1.ControlValue - (m * point1.SensorValue);

                    return m * sensorValue + b;
                }
            }

            if (sensorValue <= Points[0].SensorValue)
                return Points[0].ControlValue;

            if (sensorValue >= Points[Points.Count - 1].SensorValue)
                return Points[Points.Count - 1].ControlValue;

            return -1;
        }
        internal void Stop()
        {
            if (_timer == null) return;
            _timer.Stop();
            _timer.Elapsed -= Tick;
        }
        internal void Dispose()
        {
            Stop();
            _timer?.Dispose();
        }
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            foreach (var point in Points)
            {
               builder.Append(point.SensorValue.ToString(CultureInfo.InvariantCulture));
               builder.Append(':');
               builder.Append(point.ControlValue.ToString(CultureInfo.InvariantCulture));
               builder.Append(';');
            }

            builder.Append(Sensor.Identifier.ToString());

            return builder.ToString();
        }

        private class SoftwareCurvePoint : ISoftwareCurvePoint
        {
            public float SensorValue { get; set; }
            public float ControlValue { get; set; }
        }
    }

}
