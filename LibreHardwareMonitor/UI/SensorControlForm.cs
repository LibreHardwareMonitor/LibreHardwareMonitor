using System;
using System.Collections.Generic;
using System.Windows.Forms;
using LibreHardwareMonitor.Hardware;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System.Linq;
using OxyPlot.Annotations;
using System.Globalization;
using OxyPlot.WindowsForms;

namespace LibreHardwareMonitor.GUI
{
    public partial class SensorControlForm : Form
    {
        private readonly PlotView mPlot;
        private readonly PlotModel mModel;
        private LineSeries lineSeries;
        private LinearAxis controlAxis;
        private LinearAxis sensorAxis;

        private readonly ISensor control;
        private readonly ISensor sensor;

        private float MinimumSensor;
        private float MaximumSensor;

        private TextAnnotation annotation;
        private Dictionary<SensorType, string> units;
        private string controlTypename = "X";
        private string sensorTypename = "Y";

        private const int PlotPadding = 5;

        public SensorControlForm(ISensor control, ISensor sensor, List<ISoftwareCurvePoint> points)
        {
            this.control = control;
            this.sensor = sensor;

            this.InitializeComponent();

            // Add plot
            mPlot = new PlotView();
            mModel = new PlotModel();
            mPlot.Height = panel1.Height;
            mPlot.Width = panel1.Width;
            mPlot.Padding = new Padding(PlotPadding);
            mModel.Padding = new OxyThickness(PlotPadding);
            mPlot.Model = mModel;
            panel1.Controls.Add(mPlot);

            //Line
            lineSeries = new LineSeries
            {
                StrokeThickness = 2,
                MarkerSize = 4,
                MarkerStroke = OxyColors.DarkGray,
                MarkerFill = OxyColors.DarkGray,
                MarkerType = MarkerType.Circle,
                Color = OxyColors.Gray,
                CanTrackerInterpolatePoints = false,
                Smooth = false,
            };
            mModel.Series.Add(lineSeries);

            // new or edit curve
            if (points != null)
            {
                MinimumSensor = points[0].SensorValue;
                MaximumSensor = points[points.Count - 1].SensorValue;

                foreach (var point in points)
                {
                    lineSeries.Points.Add(new DataPoint(point.SensorValue, point.ControlValue));
                }
            }
            else
            {
                if (sensor.SensorType == SensorType.Temperature)
                {
                    MinimumSensor = 20;
                    MaximumSensor = 105;
                }
                else
                {
                    MinimumSensor = sensor.Min.GetValueOrDefault();
                    MaximumSensor = sensor.Max.HasValue ? sensor.Max.Value : MinimumSensor + 100;
                }
                lineSeries.Points.Add(new DataPoint(MinimumSensor, control.Control.MinSoftwareValue));
                lineSeries.Points.Add(new DataPoint(MaximumSensor, control.Control.MaxSoftwareValue));
            }

            // Axes
            UpdateAxes();
            
            //Annotaion
            annotation = new TextAnnotation
            {
                StrokeThickness = 0,
                TextColor = OxyColors.Red,
                FontSize = 16,
                TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Center,
                TextVerticalAlignment = VerticalAlignment.Top,
                TextPosition = new DataPoint((MinimumSensor + MaximumSensor) / 2, control.Control.MaxSoftwareValue),
            };

            units = new Dictionary<SensorType, string>
            {
                {SensorType.Voltage, "V"},
                {SensorType.Clock, "MHz"},
                {SensorType.Temperature, "°C"},
                {SensorType.Load, "%"},
                {SensorType.Fan, "RPM"},
                {SensorType.Flow, "L/h"},
                {SensorType.Control, "%"},
                {SensorType.Level, "%"},
                {SensorType.Factor, "1"},
                {SensorType.Power, "W"},
                {SensorType.Data, "GB"}
            };

            if (units.ContainsKey(control.SensorType))
                controlTypename = units[control.SensorType];

            if (units.ContainsKey(sensor.SensorType))
                sensorTypename = units[sensor.SensorType];

            textBox1.Text = Convert.ToString(MinimumSensor, CultureInfo.CurrentCulture);
            textBox2.Text = Convert.ToString(MaximumSensor, CultureInfo.CurrentCulture);
            textBox1.TextChanged += textBox1_TextChanged;
            textBox2.TextChanged += textBox2_TextChanged;

            mPlot.MouseDown += MPlot_MouseDown;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (mPlot == null)
            {
                return;
            }
            mPlot.Height = panel1.Height;
            mPlot.Width = panel1.Width;
            mPlot.Refresh();
        }

        private void UpdateAxes()
        {
            mModel.Axes.Clear();

            controlAxis = new LinearAxis()
            {
                Position = AxisPosition.Left,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                Title = "Control",
                IsZoomEnabled = false,
                IsPanEnabled = false,
                Maximum = control.Control.MaxSoftwareValue,
                Minimum = control.Control.MinSoftwareValue
            };
            controlAxis.Title = control.Name;
            mModel.Axes.Add(controlAxis);

            sensorAxis = new LinearAxis()
            {
                Position = AxisPosition.Bottom,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                Title = "Sensor",
                IsZoomEnabled = false,
                IsPanEnabled = false,
                Maximum = MaximumSensor,
                Minimum = MinimumSensor
            };
            sensorAxis.Title = sensor.Name;
            mModel.Axes.Add(sensorAxis);

            // update first and last points
            var tempPoint = lineSeries.Points.ElementAt(0);
            tempPoint = new DataPoint(MinimumSensor, tempPoint.Y);
            lineSeries.Points[0] = tempPoint;


            var lastIndex = lineSeries.Points.Count - 1;
            tempPoint = lineSeries.Points.ElementAt(lastIndex);
            tempPoint = new DataPoint(MaximumSensor, tempPoint.Y);
            lineSeries.Points[lastIndex] = tempPoint;

            // remove too small or large points
            for (int i = lineSeries.Points.Count - 2; i > 0; i--)
            {
                var el = lineSeries.Points.ElementAt(i);
                if (el.X > sensorAxis.Maximum ||
                    el.X < sensorAxis.Minimum ||
                    el.Y > controlAxis.Maximum ||
                    el.Y < controlAxis.Minimum)
                {
                    lineSeries.Points.RemoveAt(i);
                }
            }
            
            ((IPlotModel)mModel).Update(true);
            mPlot.Refresh();
        }

        private int _selectedPointIndex = -1;

        private void MPlot_MouseDown(object sender, MouseEventArgs e)
        {
            // right click deletes the point at the mouse location
            if (e.Button == MouseButtons.Right)
            {
                DeletePoint(new ScreenPoint(e.X, e.Y));
            }
            // left button moves en existing point, or creates a new one if no points at location
            else if (e.Button == MouseButtons.Left)
            {
                int pointIndex = Search(new ScreenPoint(e.X, e.Y));
                // move point
                if (pointIndex >= 0)
                {
                    _selectedPointIndex = pointIndex;
                    AttachMouseMove();
                }
                // create new point
                else
                {
                    var point = Axis.InverseTransform(new ScreenPoint(e.X, e.Y), sensorAxis, controlAxis);
                    var newPoint = new DataPoint(point.X, point.Y);

                    // prevent creating points outside of range
                    if (newPoint.X < sensorAxis.Maximum &&
                        newPoint.X > sensorAxis.Minimum &&
                        newPoint.Y < controlAxis.Maximum &&
                        newPoint.Y > controlAxis.Minimum)
                        for (int i = 0; i < lineSeries.Points.Count; i++)
                        {
                            if (newPoint.X >= lineSeries.Points.ElementAt(i).X &&
                                newPoint.X <= lineSeries.Points.ElementAt(i + 1).X)
                            {
                                int idx = i + 1;
                                lineSeries.Points.Insert(idx, newPoint);
                                mPlot.Refresh();
                                _selectedPointIndex = idx;
                                AttachMouseMove();
                                return;
                            }
                        }
                }
            }
            mPlot.Refresh();
        }

        private void AttachMouseMove()
        {
            void MouseUpListener(object curveSelectSender, MouseEventArgs curveSelectEvent)
            {
                mPlot.MouseUp -= MouseUpListener;
                mPlot.MouseMove -= MPlot_MouseMove;
                _selectedPointIndex = -1;
                mModel.Annotations.Remove(annotation);
                ((IPlotModel)mModel).Update(true);
                mPlot.Refresh();
            }

            mModel.Annotations.Add(annotation);
            annotation.EnsureAxes();
            mPlot.MouseUp += MouseUpListener;
            mPlot.MouseMove += MPlot_MouseMove;
        }

        private void MPlot_MouseMove(object sender, MouseEventArgs e)
        {
            DataPoint mousePos = lineSeries.InverseTransform(new ScreenPoint(e.X, e.Y));

            if (_selectedPointIndex < 0)
            {
                return;
            }

            var currentPoint = lineSeries.Points[_selectedPointIndex];

            double newX = currentPoint.X;
            double newY = currentPoint.Y;
            
            if (_selectedPointIndex > 0 && _selectedPointIndex < lineSeries.Points.Count - 1)
            {
                var previousPoint = lineSeries.Points[_selectedPointIndex - 1];
                var nextPoint = lineSeries.Points[_selectedPointIndex + 1];
                if (mousePos.X <= previousPoint.X)
                {
                    newX = previousPoint.X;
                }
                else if (mousePos.X >= nextPoint.X)
                {
                    newX = nextPoint.X;
                }
                else
                {
                    newX = mousePos.X;
                }
            }

            if (mousePos.Y >= control.Control.MaxSoftwareValue)
            {
                newY = control.Control.MaxSoftwareValue;
            }
            else if (mousePos.Y <= control.Control.MinSoftwareValue)
            {
                newY = control.Control.MinSoftwareValue;
            }
            else
            {
                newY = mousePos.Y;
            }
            
            double sensorValue;

            if (sensor.SensorType == SensorType.Voltage)
                sensorValue = Math.Round(newX, 4);
            else if (sensor.SensorType == SensorType.Power)
                sensorValue = Math.Round(newX, 1);
            else if (sensor.SensorType == SensorType.Flow)
                sensorValue = Math.Round(newX, 1);
            else
                sensorValue = Math.Round(newX, 0);

            annotation.Text = Math.Round(newY, 0) + " " + controlTypename + " - " + sensorValue + " " + sensorTypename;

            lineSeries.Points[_selectedPointIndex] = new DataPoint(newX, newY);
            
            mPlot.Refresh();
        }

        private int Search(ScreenPoint point)
        {
            var mousePos = lineSeries.InverseTransform(point);
            return lineSeries.Points.FindIndex(p => Math.Abs(p.X - mousePos.X) < 2 && Math.Abs(p.Y - mousePos.Y) < 2);
        }

        private void DeletePoint(ScreenPoint point)
        {
            var mousePos = lineSeries.InverseTransform(point);
            int deleteIndex = lineSeries.Points.FindIndex(p => Math.Abs(p.X - mousePos.X) < 2 && Math.Abs(p.Y - mousePos.Y) < 2);
            if (deleteIndex < 0) return;
            if (deleteIndex != 0 && deleteIndex != lineSeries.Points.Count - 1)
                lineSeries.Points.RemoveAt(deleteIndex);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            bool isNum = float.TryParse(textBox1.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float input1);
            if (!isNum) return;
            
            MinimumSensor = input1;
            UpdateAxes();
        }
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            bool isNum = float.TryParse(textBox2.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float input2);
            if (!isNum) return;
            
            MaximumSensor = input2;
            UpdateAxes();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            List<ISoftwareCurvePoint> softwareCurvePoints = new List<ISoftwareCurvePoint>();

            foreach (var point in lineSeries.Points)
            {
                softwareCurvePoints.Add(new SoftwareCurvePoint
                {
                    SensorValue = (float)point.X,
                    ControlValue = (float)point.Y
                });
            }

            if (softwareCurvePoints.Count < 2)
            {
                MessageBox.Show("There are less than the required 2 points", "Error", MessageBoxButtons.OK);
                return;
            }

            control.Control.SetSoftwareCurve(softwareCurvePoints, sensor);
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }
    }

    public class SoftwareCurvePoint : ISoftwareCurvePoint
    {
        public float SensorValue { get; set; }
        public float ControlValue { get; set; }
    }
}
