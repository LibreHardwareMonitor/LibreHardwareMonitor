// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using OpenHardwareMonitor.Hardware;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.WindowsForms;
using OxyPlot.Series;

namespace OpenHardwareMonitor.GUI
{
    public class PlotPanel : UserControl
    {
        private readonly PersistentSettings _settings;
        private readonly UnitManager _unitManager;
        private readonly Plot _plot;
        private readonly PlotModel _model;
        private readonly TimeSpanAxis _timeAxis = new TimeSpanAxis();
        private readonly SortedDictionary<SensorType, LinearAxis> _axes = new SortedDictionary<SensorType, LinearAxis>();
        private UserOption _stackedAxes;
        private DateTime _now;
        private float _dpiX;
        private float _dpiY;
        private double _dpiXscale = 1;
        private double _dpiYscale = 1;

        public PlotPanel(PersistentSettings settings, UnitManager unitManager)
        {
            _settings = settings;
            _unitManager = unitManager;

            SetDPI();
            _model = CreatePlotModel();

            _plot = new Plot();
            _plot.Dock = DockStyle.Fill;
            _plot.Model = _model;
            _plot.BackColor = Color.White;
            _plot.ContextMenu = CreateMenu();

            UpdateAxesPosition();

            SuspendLayout();
            Controls.Add(_plot);
            ResumeLayout(true);
        }

        public void SetCurrentSettings()
        {
            _settings.SetValue("plotPanel.MinTimeSpan", (float)_timeAxis.ViewMinimum);
            _settings.SetValue("plotPanel.MaxTimeSpan", (float)_timeAxis.ViewMaximum);
            foreach (var axis in _axes.Values)
            {
                _settings.SetValue("plotPanel.Min" + axis.Key, (float)axis.ViewMinimum);
                _settings.SetValue("plotPanel.Max" + axis.Key, (float)axis.ViewMaximum);
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

            MenuItem timeWindow = new MenuItem("Time Window");
            MenuItem[] timeWindowMenuItems =
                { new MenuItem("Auto", (s, e) => { _timeAxis.Zoom(0, double.NaN); InvalidatePlot(); }),
                new MenuItem("5 min", (s, e) => { _timeAxis.Zoom(0, 5 * 60); InvalidatePlot(); }),
                new MenuItem("10 min", (s, e) => { _timeAxis.Zoom(0, 10 * 60); InvalidatePlot(); }),
                new MenuItem("20 min", (s, e) => { _timeAxis.Zoom(0, 20 * 60); InvalidatePlot(); }),
                new MenuItem("30 min", (s, e) => { _timeAxis.Zoom(0, 30 * 60); InvalidatePlot(); }),
                new MenuItem("45 min", (s, e) => { _timeAxis.Zoom(0, 45 * 60); InvalidatePlot(); }),
                new MenuItem("1 h", (s, e) => { _timeAxis.Zoom(0, 60 * 60); InvalidatePlot(); }),
                new MenuItem("1.5 h", (s, e) => { _timeAxis.Zoom(0, 1.5 * 60 * 60); InvalidatePlot(); }),
                new MenuItem("2 h", (s, e) => { _timeAxis.Zoom(0, 2 * 60 * 60); InvalidatePlot(); }),
                new MenuItem("3 h", (s, e) => { _timeAxis.Zoom(0, 3 * 60 * 60); InvalidatePlot(); }),
                new MenuItem("6 h", (s, e) => { _timeAxis.Zoom(0, 6 * 60 * 60); InvalidatePlot(); }),
                new MenuItem("12 h", (s, e) => { _timeAxis.Zoom(0, 12 * 60 * 60); InvalidatePlot(); }),
                new MenuItem("24 h", (s, e) => { _timeAxis.Zoom(0, 24 * 60 * 60); InvalidatePlot(); }) };
            foreach (MenuItem mi in timeWindowMenuItems)
                timeWindow.MenuItems.Add(mi);
            menu.MenuItems.Add(timeWindow);
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

            var units = new Dictionary<SensorType, string>();
            units.Add(SensorType.Voltage, "V");
            units.Add(SensorType.Clock, "MHz");
            units.Add(SensorType.Temperature, "°C");
            units.Add(SensorType.Load, "%");
            units.Add(SensorType.Fan, "RPM");
            units.Add(SensorType.Flow, "L/h");
            units.Add(SensorType.Control, "%");
            units.Add(SensorType.Level, "%");
            units.Add(SensorType.Factor, "1");
            units.Add(SensorType.Power, "W");
            units.Add(SensorType.Data, "GB");
            units.Add(SensorType.Frequency, "Hz");

            foreach (SensorType type in Enum.GetValues(typeof(SensorType)))
            {
                var axis = new LinearAxis();
                axis.Position = AxisPosition.Left;
                axis.MajorGridlineStyle = LineStyle.Solid;
                axis.MajorGridlineThickness = 1;
                axis.MajorGridlineColor = _timeAxis.MajorGridlineColor;
                axis.MinorGridlineStyle = LineStyle.Solid;
                axis.MinorGridlineThickness = 1;
                axis.MinorGridlineColor = _timeAxis.MinorGridlineColor;
                axis.AxislineStyle = LineStyle.Solid;
                axis.Title = type.ToString();
                axis.Key = type.ToString();

                axis.Zoom(
                  _settings.GetValue("plotPanel.Min" + axis.Key, float.NaN),
                  _settings.GetValue("plotPanel.Max" + axis.Key, float.NaN));

                if (units.ContainsKey(type))
                    axis.Unit = units[type];
                _axes.Add(type, axis);
            }

            var model = new PlotModel(_dpiXscale, _dpiYscale);
            model.Axes.Add(_timeAxis);
            foreach (var axis in _axes.Values)
                model.Axes.Add(axis);
            model.PlotMargins = new OxyThickness(0);
            model.IsLegendVisible = false;

            return model;
        }

        private void SetDPI()
        {
            // https://msdn.microsoft.com/en-us/library/windows/desktop/dn469266(v=vs.85).aspx
            const int _default_dpi = 96;
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
            {
                _dpiXscale = _dpiX / _default_dpi;
            }
            if (_dpiY > 0)
            {
                _dpiYscale = _dpiY / _default_dpi;
            }
        }

        public void SetSensors(List<ISensor> sensors, IDictionary<ISensor, Color> colors)
        {
            _model.Series.Clear();
            var types = new System.Collections.Generic.HashSet<SensorType>();

            foreach (ISensor sensor in sensors)
            {
                var series = new LineSeries();
                if (sensor.SensorType == SensorType.Temperature)
                {
                    series.ItemsSource = sensor.Values.Select(value => new DataPoint
                    {
                        X = (_now - value.Time).TotalSeconds,
                        Y = _unitManager.TemperatureUnit == TemperatureUnit.Celsius ?
                        value.Value : UnitManager.CelsiusToFahrenheit(value.Value).Value
                    });
                }
                else
                {
                    series.ItemsSource = sensor.Values.Select(value => new DataPoint
                    {
                        X = (_now - value.Time).TotalSeconds,
                        Y = value.Value
                    });
                }
                series.Color = colors[sensor].ToOxyColor();
                series.StrokeThickness = 1;
                series.YAxisKey = _axes[sensor.SensorType].Key;
                series.Title = sensor.Hardware.Name + " " + sensor.Name;
                _model.Series.Add(series);

                types.Add(sensor.SensorType);
            }

            foreach (var pair in _axes.Reverse())
            {
                var axis = pair.Value;
                var type = pair.Key;
                axis.IsAxisVisible = types.Contains(type);
            }
            UpdateAxesPosition();
            InvalidatePlot();
        }

        private void UpdateAxesPosition()
        {
            if (_stackedAxes.Value)
            {
                var count = _axes.Values.Count(axis => axis.IsAxisVisible);
                var start = 0.0;
                foreach (var pair in _axes.Reverse())
                {
                    var axis = pair.Value;
                    var type = pair.Key;
                    axis.StartPosition = start;
                    var delta = axis.IsAxisVisible ? 1.0 / count : 0;
                    start += delta;
                    axis.EndPosition = start;
                    axis.PositionTier = 0;
                    axis.MajorGridlineStyle = LineStyle.Solid;
                    axis.MinorGridlineStyle = LineStyle.Solid;
                }
            }
            else
            {
                var tier = 0;
                foreach (var pair in _axes.Reverse())
                {
                    var axis = pair.Value;
                    var type = pair.Key;
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
            foreach (var pair in _axes)
            {
                var axis = pair.Value;
                var type = pair.Key;
                if (type == SensorType.Temperature)
                    axis.Unit = _unitManager.TemperatureUnit == TemperatureUnit.Celsius ?
                    "°C" : "°F";
            }
            _plot.InvalidatePlot(true);
        }
    }
}
