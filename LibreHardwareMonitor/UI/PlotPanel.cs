// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
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
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.WindowsForms;
using OxyPlot.Series;
using LibreHardwareMonitor.UI.Themes;

namespace LibreHardwareMonitor.UI;

public class PlotPanel : UserControl
{
    private readonly PersistentSettings _settings;
    private readonly UnitManager _unitManager;
    private readonly PlotView _plot;
    private readonly PlotModel _model;
    private readonly TimeSpanAxis _timeAxis = new TimeSpanAxis();
    private readonly SortedDictionary<SensorType, LinearAxis> _axes = new SortedDictionary<SensorType, LinearAxis>();
    private readonly Dictionary<SensorType, LineAnnotation> _annotations = new Dictionary<SensorType, LineAnnotation>();
    private UserOption _stackedAxes;
    private UserOption _showAxesLabels;
    private UserOption _timeAxisEnableZoom;
    private UserOption _yAxesEnableZoom;
    private DateTime _now;
    private float _dpiX;
    private float _dpiY;
    private double _dpiXScale = 1;
    private double _dpiYScale = 1;
    private Point _rightClickEnter;
    private bool _cancelContextMenu = false;

    public PlotPanel(PersistentSettings settings, UnitManager unitManager)
    {
        _settings = settings;
        _unitManager = unitManager;

        SetDpi();
        _model = CreatePlotModel();

        _plot = new PlotView { Dock = DockStyle.Fill, Model = _model, BackColor = Color.Black, ContextMenuStrip = CreateMenu() };
        _plot.MouseDown += (sender, e) =>
        {
            if (e.Button == MouseButtons.Right)
            {
                _rightClickEnter = e.Location;
            }
        };
        _plot.MouseMove += (sender, e) =>
        {
            if (e.Button == MouseButtons.Right)
            {
                if (!_cancelContextMenu && e.Location.DistanceTo(_rightClickEnter) > 10.0f)
                {
                    _cancelContextMenu = true;
                }
            }
        };

        UpdateAxesPosition();

        SuspendLayout();
        ApplyTheme();
        Controls.Add(_plot);
        ResumeLayout(true);
    }

    public void ApplyTheme()
    {
        _model.Background = Theme.Current.PlotBackgroundColor.ToOxyColor();
        _model.PlotAreaBorderColor = Theme.Current.PlotBorderColor.ToOxyColor();
        foreach (Axis axis in _model.Axes)
        {
            axis.AxislineColor = Theme.Current.PlotBorderColor.ToOxyColor();
            axis.MajorGridlineColor = Theme.Current.PlotGridMajorColor.ToOxyColor();
            axis.MinorGridlineColor = Theme.Current.PlotGridMinorColor.ToOxyColor();
            axis.TextColor = Theme.Current.PlotTextColor.ToOxyColor();
            axis.TitleColor = Theme.Current.PlotTextColor.ToOxyColor();
            axis.MinorTicklineColor = Theme.Current.PlotBorderColor.ToOxyColor();
            axis.TicklineColor = Theme.Current.PlotBorderColor.ToOxyColor();
        }
        foreach (LineAnnotation annotation in _model.Annotations.Select(x => x as LineAnnotation).Where(x => x != null))
        {
            annotation.Color = Theme.Current.PlotBorderColor.ToOxyColor();
        }
    }

    public void SetCurrentSettings()
    {
        foreach (LinearAxis axis in _axes.Values)
        {
            _settings.SetValue("plotPanel.Min" + axis.Key, (float)axis.ActualMinimum);
            _settings.SetValue("plotPanel.Max" + axis.Key, (float)axis.ActualMaximum);
        }
        _settings.SetValue("plotPanel.MinTimeSpan", (float)_timeAxis.ActualMinimum);
        _settings.SetValue("plotPanel.MaxTimeSpan", (float)_timeAxis.ActualMaximum);
    }

    private ContextMenuStrip CreateMenu()
    {
        ContextMenuStrip menu = new ContextMenuStrip();
        menu.Renderer = new ThemedToolStripRenderer();
        menu.Opening += (sender, e) =>
        {
            if (_cancelContextMenu)
            {
                e.Cancel = true;
                _cancelContextMenu = false;
            }
        };

        ToolStripMenuItem stackedAxesMenuItem = new ToolStripMenuItem("Stacked Axes");
        _stackedAxes = new UserOption("stackedAxes", true, stackedAxesMenuItem, _settings);
        _stackedAxes.Changed += (sender, e) =>
        {
            UpdateAxesPosition();
            InvalidatePlot();
        };
        menu.Items.Add(stackedAxesMenuItem);

        ToolStripMenuItem showAxesLabelsMenuItem = new ToolStripMenuItem("Show Axes Labels");
        _showAxesLabels = new UserOption("showAxesLabels", true, showAxesLabelsMenuItem, _settings);
        _showAxesLabels.Changed += (sender, e) =>
        {
            if (_showAxesLabels.Value)
                _model.PlotMargins = new OxyThickness(double.NaN);
            else
                _model.PlotMargins = new OxyThickness(0);
        };
        menu.Items.Add(showAxesLabelsMenuItem);

        ToolStripMenuItem timeAxisMenuItem = new ToolStripMenuItem("Time Axis");
        ToolStripMenuItem[] timeAxisMenuItems =
        { new ToolStripMenuItem("Enable Zoom"),
            new ToolStripMenuItem("Auto", null, (s, e) => { TimeAxisZoom(0, double.NaN); }),
            new ToolStripMenuItem("5 min", null, (s, e) => { TimeAxisZoom(0, 5 * 60); }),
            new ToolStripMenuItem("10 min", null, (s, e) => { TimeAxisZoom(0, 10 * 60); }),
            new ToolStripMenuItem("20 min", null, (s, e) => { TimeAxisZoom(0, 20 * 60); }),
            new ToolStripMenuItem("30 min", null, (s, e) => { TimeAxisZoom(0, 30 * 60); }),
            new ToolStripMenuItem("45 min", null, (s, e) => { TimeAxisZoom(0, 45 * 60); }),
            new ToolStripMenuItem("1 h", null, (s, e) => { TimeAxisZoom(0, 60 * 60); }),
            new ToolStripMenuItem("1.5 h", null, (s, e) => { TimeAxisZoom(0, 1.5 * 60 * 60); }),
            new ToolStripMenuItem("2 h", null, (s, e) => { TimeAxisZoom(0, 2 * 60 * 60); }),
            new ToolStripMenuItem("3 h", null, (s, e) => { TimeAxisZoom(0, 3 * 60 * 60); }),
            new ToolStripMenuItem("6 h", null, (s, e) => { TimeAxisZoom(0, 6 * 60 * 60); }),
            new ToolStripMenuItem("12 h", null, (s, e) => { TimeAxisZoom(0, 12 * 60 * 60); }),
            new ToolStripMenuItem("24 h", null, (s, e) => { TimeAxisZoom(0, 24 * 60 * 60); }) };

        foreach (ToolStripItem mi in timeAxisMenuItems)
            timeAxisMenuItem.DropDownItems.Add(mi);
        menu.Items.Add(timeAxisMenuItem);

        _timeAxisEnableZoom = new UserOption("timeAxisEnableZoom", true, timeAxisMenuItems[0], _settings);
        _timeAxisEnableZoom.Changed += (sender, e) =>
        {
            _timeAxis.IsZoomEnabled = _timeAxisEnableZoom.Value;
        };

        ToolStripMenuItem yAxesMenuItem = new ToolStripMenuItem("Value Axes");
        ToolStripMenuItem[] yAxesMenuItems =
        { new ToolStripMenuItem("Enable Zoom"),
            new ToolStripMenuItem("Autoscale All", null, (s, e) => { AutoscaleAllYAxes(); }) };

        foreach (ToolStripItem mi in yAxesMenuItems)
            yAxesMenuItem.DropDownItems.Add(mi);
        menu.Items.Add(yAxesMenuItem);

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
            { SensorType.Current, "A" },
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
            { SensorType.Frequency, "Hz" },
            { SensorType.Energy, "mWh" },
            { SensorType.Noise, "dBA" }
        };

        foreach (SensorType type in Enum.GetValues(typeof(SensorType)))
        {
            string typeName = type.ToString();
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
                Title = typeName,
                Key = typeName,
            };

            var annotation = new LineAnnotation
            {
                Type = LineAnnotationType.Horizontal,
                ClipByXAxis = false,
                ClipByYAxis = false,
                LineStyle = LineStyle.Solid,
                Color = Theme.Current.PlotBorderColor.ToOxyColor(),
                YAxisKey = typeName,
                StrokeThickness = 2,
            };

            axis.AxisChanged += (sender, args) => annotation.Y = axis.ActualMinimum;
            axis.TransformChanged += (sender, args) => annotation.Y = axis.ActualMinimum;

            axis.Zoom(_settings.GetValue("plotPanel.Min" + axis.Key, float.NaN), _settings.GetValue("plotPanel.Max" + axis.Key, float.NaN));

            if (units.ContainsKey(type))
                axis.Unit = units[type];

            _axes.Add(type, axis);
            _annotations.Add(type, annotation);
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

    public void SetSensors(List<ISensor> sensors, IDictionary<ISensor, Color> colors, double strokeThickness)
    {
        _model.Series.Clear();
        var types = new HashSet<SensorType>();


        DataPoint CreateDataPoint(SensorType type, SensorValue value)
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
        }


        foreach (ISensor sensor in sensors)
        {
            var series = new LineSeries
            {
                ItemsSource = sensor.Values.Select(value => CreateDataPoint(sensor.SensorType, value)),
                Color = colors[sensor].ToOxyColor(),
                StrokeThickness = strokeThickness,
                YAxisKey = _axes[sensor.SensorType].Key,
                Title = sensor.Hardware.Name + " " + sensor.Name
            };

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

    public void UpdateStrokeThickness(double strokeThickness)
    {
        foreach (LineSeries series in _model.Series)
        {
            series.StrokeThickness = strokeThickness;
        }

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
                LineAnnotation annotation = _annotations[pair.Key];
                annotation.Y = axis.ActualMinimum;
                if (!_model.Annotations.Contains(annotation)) 
                    _model.Annotations.Add(annotation);
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
                LineAnnotation annotation = _annotations[pair.Key];
                if (_model.Annotations.Contains(annotation)) 
                    _model.Annotations.Remove(_annotations[pair.Key]);
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
                    
                if (!_stackedAxes.Value) 
                    continue;

                var annotation = _annotations[pair.Key];
                annotation.Y = axis.ActualMaximum;
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
