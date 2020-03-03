﻿// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.Utilities;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.WindowsForms;
using OxyPlot.Series;

namespace LibreHardwareMonitor.UI
{
    public class PlotPanel : UserControl
    {
        private readonly PersistentSettings _settings;
        private readonly UnitManager _unitManager;
        private readonly PlotView _plot;
        private readonly PlotModel _model;
        private readonly TimeSpanAxis _timeAxis = new TimeSpanAxis();
        private readonly SortedDictionary<SensorType, LinearAxis> _axes = new SortedDictionary<SensorType, LinearAxis>();
        private UserOption _stackedAxes;
        private UserOption _showAxesLabels;
        private UserOption _timeAxisEnableZoom;
        private UserOption _yAxesEnableZoom;
        private DateTime _now;
        private float _dpiX;
        private float _dpiY;
        private double _dpiXScale = 1;
        private double _dpiYScale = 1;

        public PlotPanel(PersistentSettings settings, UnitManager unitManager)
        {
            _settings = settings;
            _unitManager = unitManager;

            SetDpi();
            _model = CreatePlotModel();

            _plot = new PlotView { Dock = DockStyle.Fill, Model = _model, BackColor = Color.White, ContextMenu = CreateMenu() };

            UpdateAxesPosition();

            SuspendLayout();
            Controls.Add(_plot);
            ResumeLayout(true);
        }

        public void SetCurrentSettings()
        {
            _settings.SetValue("plotPanel.MinTimeSpan", (float)_timeAxis.ActualMinimum);
            _settings.SetValue("plotPanel.MaxTimeSpan", (float)_timeAxis.ActualMaximum);
            foreach (LinearAxis axis in _axes.Values)
            {
                _settings.SetValue("plotPanel.Min" + axis.Key, (float)axis.ActualMinimum);
                _settings.SetValue("plotPanel.Max" + axis.Key, (float)axis.ActualMaximum);
            }
        }

        private ContextMenu CreateMenu()
        {
            ContextMenu menu = new ContextMenu();

            MenuItem stackedAxesMenuItem = new MenuItem("Stacked Axes");
            _stackedAxes = new UserOption("stackedAxes", true, stackedAxesMenuItem, _settings);
            _stackedAxes.Changed += (sender, e) =>
            {
                UpdateAxesPosition();
                InvalidatePlot();
            };
            menu.MenuItems.Add(stackedAxesMenuItem);

            MenuItem showAxesLabelsMenuItem = new MenuItem("Show Axes Labels");
            _showAxesLabels = new UserOption("showAxesLabels", true, showAxesLabelsMenuItem, _settings);
            _showAxesLabels.Changed += (sender, e) =>
            {
                if (_showAxesLabels.Value)
                    _model.PlotMargins = new OxyThickness(double.NaN);
                else
                    _model.PlotMargins = new OxyThickness(0);
            };
            menu.MenuItems.Add(showAxesLabelsMenuItem);

            MenuItem timeAxisMenuItem = new MenuItem("Time Axis");
            MenuItem[] timeAxisMenuItems =
                { new MenuItem("Enable Zoom"),
                new MenuItem("Auto", (s, e) => { TimeAxisZoom(0, double.NaN); }),
                new MenuItem("5 min", (s, e) => { TimeAxisZoom(0, 5 * 60); }),
                new MenuItem("10 min", (s, e) => { TimeAxisZoom(0, 10 * 60); }),
                new MenuItem("20 min", (s, e) => { TimeAxisZoom(0, 20 * 60); }),
                new MenuItem("30 min", (s, e) => { TimeAxisZoom(0, 30 * 60); }),
                new MenuItem("45 min", (s, e) => { TimeAxisZoom(0, 45 * 60); }),
                new MenuItem("1 h", (s, e) => { TimeAxisZoom(0, 60 * 60); }),
                new MenuItem("1.5 h", (s, e) => { TimeAxisZoom(0, 1.5 * 60 * 60); }),
                new MenuItem("2 h", (s, e) => { TimeAxisZoom(0, 2 * 60 * 60); }),
                new MenuItem("3 h", (s, e) => { TimeAxisZoom(0, 3 * 60 * 60); }),
                new MenuItem("6 h", (s, e) => { TimeAxisZoom(0, 6 * 60 * 60); }),
                new MenuItem("12 h", (s, e) => { TimeAxisZoom(0, 12 * 60 * 60); }),
                new MenuItem("24 h", (s, e) => { TimeAxisZoom(0, 24 * 60 * 60); }) };

            foreach (MenuItem mi in timeAxisMenuItems)
                timeAxisMenuItem.MenuItems.Add(mi);
            menu.MenuItems.Add(timeAxisMenuItem);

            _timeAxisEnableZoom = new UserOption("timeAxisEnableZoom", true, timeAxisMenuItems[0], _settings);
            _timeAxisEnableZoom.Changed += (sender, e) =>
            {
                _timeAxis.IsZoomEnabled = _timeAxisEnableZoom.Value;
            };

            MenuItem yAxesMenuItem = new MenuItem("Value Axes");
            MenuItem[] yAxesMenuItems =
                { new MenuItem("Enable Zoom"),
                new MenuItem("Autoscale All", (s, e) => { AutoscaleAllYAxes(); }) };

            foreach (MenuItem mi in yAxesMenuItems)
                yAxesMenuItem.MenuItems.Add(mi);
            menu.MenuItems.Add(yAxesMenuItem);

            _yAxesEnableZoom = new UserOption("yAxesEnableZoom", true, yAxesMenuItems[0], _settings);
            _yAxesEnableZoom.Changed += (sender, e) =>
            {
                foreach (LinearAxis axis in _axes.Values)
                    axis.IsZoomEnabled = _yAxesEnableZoom.Value;
            };

            return menu;
        }

        private PlotModel CreatePlotModel()
        {
            _timeAxis.Position = AxisPosition.Bottom;
            _timeAxis.MajorGridlineStyle = LineStyle.Solid;
            _timeAxis.MajorGridlineThickness = 1;
            _timeAxis.MajorGridlineColor = OxyColor.FromRgb(192, 192, 192);
            _timeAxis.MinorGridlineStyle = LineStyle.Solid;
            _timeAxis.MinorGridlineThickness = 1;
            _timeAxis.MinorGridlineColor = OxyColor.FromRgb(232, 232, 232);
            _timeAxis.StartPosition = 1;
            _timeAxis.EndPosition = 0;
            _timeAxis.MinimumPadding = 0;
            _timeAxis.MaximumPadding = 0;
            _timeAxis.AbsoluteMinimum = 0;
            _timeAxis.Minimum = 0;
            _timeAxis.AbsoluteMaximum = 24 * 60 * 60;
            _timeAxis.Zoom(
              _settings.GetValue("plotPanel.MinTimeSpan", 0.0f),
              _settings.GetValue("plotPanel.MaxTimeSpan", 10.0f * 60));
            _timeAxis.StringFormat = "h:mm";

            var units = new Dictionary<SensorType, string>
            {
                { SensorType.Voltage, "V" },
                { SensorType.Clock, "MHz" },
                { SensorType.Temperature, "°C" },
                { SensorType.Load, "%" },
                { SensorType.Fan, "RPM" },
                { SensorType.Flow, "L/h" },
                { SensorType.Control, "%" },
                { SensorType.Level, "%" },
                { SensorType.Factor, "1" },
                { SensorType.Power, "W" },
                { SensorType.Data, "GB" },
                { SensorType.Frequency, "Hz" }
            };

            foreach (SensorType type in Enum.GetValues(typeof(SensorType)))
            {
                var axis = new LinearAxis
                {
                    Position = AxisPosition.Left,
                    MajorGridlineStyle = LineStyle.Solid,
                    MajorGridlineThickness = 1,
                    MajorGridlineColor = _timeAxis.MajorGridlineColor,
                    MinorGridlineStyle = LineStyle.Solid,
                    MinorGridlineThickness = 1,
                    MinorGridlineColor = _timeAxis.MinorGridlineColor,
                    AxislineStyle = LineStyle.Solid,
                    Title = type.ToString(),
                    Key = type.ToString()
                };

                axis.Zoom(_settings.GetValue("plotPanel.Min" + axis.Key, float.NaN), _settings.GetValue("plotPanel.Max" + axis.Key, float.NaN));

                if (units.ContainsKey(type))
                    axis.Unit = units[type];
                _axes.Add(type, axis);
            }

            var model = new ScaledPlotModel(_dpiXScale, _dpiYScale);
            model.Axes.Add(_timeAxis);
            foreach (LinearAxis axis in _axes.Values)
                model.Axes.Add(axis);
            model.IsLegendVisible = false;

            return model;
        }

        private void SetDpi()
        {
            // https://msdn.microsoft.com/en-us/library/windows/desktop/dn469266(v=vs.85).aspx
            const int defaultDpi = 96;
            Graphics g = CreateGraphics();

            try
            {
                _dpiX = g.DpiX;
                _dpiY = g.DpiY;
            }
            finally
            {
                g.Dispose();
            }

            if (_dpiX > 0)
                _dpiXScale = _dpiX / defaultDpi;
            if (_dpiY > 0)
                _dpiYScale = _dpiY / defaultDpi;
        }

        public void SetSensors(List<ISensor> sensors, IDictionary<ISensor, Color> colors)
        {
            _model.Series.Clear();
            var types = new System.Collections.Generic.HashSet<SensorType>();


            Func<SensorType, SensorValue, DataPoint> createDataPoint = (SensorType type, SensorValue value) =>
            {
                float displayedValue;

                if (type == SensorType.Temperature && _unitManager.TemperatureUnit == TemperatureUnit.Fahrenheit)
                {
                    displayedValue = UnitManager.CelsiusToFahrenheit(value.Value).Value;
                }
                else
                {
                    displayedValue = value.Value;
                }
                return new DataPoint((_now - value.Time).TotalSeconds, displayedValue);
            };

            foreach (ISensor sensor in sensors)
            {
                var series = new LineSeries();

                series.ItemsSource = sensor.Values.Select(value => createDataPoint(sensor.SensorType, value));
                series.Color = colors[sensor].ToOxyColor();
                series.StrokeThickness = 1;
                series.YAxisKey = _axes[sensor.SensorType].Key;
                series.Title = sensor.Hardware.Name + " " + sensor.Name;
                _model.Series.Add(series);

                types.Add(sensor.SensorType);
            }

            foreach (KeyValuePair<SensorType, LinearAxis> pair in _axes.Reverse())
            {
                LinearAxis axis = pair.Value;
                SensorType type = pair.Key;
                axis.IsAxisVisible = types.Contains(type);
            }

            UpdateAxesPosition();
            InvalidatePlot();
        }

        private void UpdateAxesPosition()
        {
            if (_stackedAxes.Value)
            {
                int count = _axes.Values.Count(axis => axis.IsAxisVisible);
                double start = 0.0;
                foreach (KeyValuePair<SensorType, LinearAxis> pair in _axes.Reverse())
                {
                    LinearAxis axis = pair.Value;
                    axis.StartPosition = start;
                    double delta = axis.IsAxisVisible ? 1.0 / count : 0;
                    start += delta;
                    axis.EndPosition = start;
                    axis.PositionTier = 0;
                    axis.MajorGridlineStyle = LineStyle.Solid;
                    axis.MinorGridlineStyle = LineStyle.Solid;
                }
            }
            else
            {
                int tier = 0;

                foreach (KeyValuePair<SensorType, LinearAxis> pair in _axes.Reverse())
                {
                    LinearAxis axis = pair.Value;

                    if (axis.IsAxisVisible)
                    {
                        axis.StartPosition = 0;
                        axis.EndPosition = 1;
                        axis.PositionTier = tier;
                        tier++;
                    }
                    else
                    {
                        axis.StartPosition = 0;
                        axis.EndPosition = 0;
                        axis.PositionTier = 0;
                    }
                    axis.MajorGridlineStyle = LineStyle.None;
                    axis.MinorGridlineStyle = LineStyle.None;
                }
            }
        }

        public void InvalidatePlot()
        {
            _now = DateTime.UtcNow;

            if (_axes != null)
            {
                foreach (KeyValuePair<SensorType, LinearAxis> pair in _axes)
                {
                    LinearAxis axis = pair.Value;
                    SensorType type = pair.Key;
                    if (type == SensorType.Temperature)
                        axis.Unit = _unitManager.TemperatureUnit == TemperatureUnit.Celsius ? "°C" : "°F";
                }
            }

            _plot?.InvalidatePlot(true);
        }

        public void TimeAxisZoom(double min, double max)
        {
            bool timeAxisIsZoomEnabled = _timeAxis.IsZoomEnabled;

            _timeAxis.IsZoomEnabled = true;
            _timeAxis.Zoom(min, max);
            InvalidatePlot();
            _timeAxis.IsZoomEnabled = timeAxisIsZoomEnabled;
        }

        public void AutoscaleAllYAxes()
        {
            foreach (LinearAxis axis in _axes.Values)
                axis.Zoom(double.NaN, double.NaN);
        }
    }
}
