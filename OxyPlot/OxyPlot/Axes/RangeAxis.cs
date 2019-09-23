// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RangeAxis.cs" company="OxyPlot">
//   The MIT License (MIT)
//
//   Copyright (c) 2012 Oystein Bjorke
//
//   Permission is hereby granted, free of charge, to any person obtaining a
//   copy of this software and associated documentation files (the
//   "Software"), to deal in the Software without restriction, including
//   without limitation the rights to use, copy, modify, merge, publish,
//   distribute, sublicense, and/or sell copies of the Software, and to
//   permit persons to whom the Software is furnished to do so, subject to
//   the following conditions:
//
//   The above copyright notice and this permission notice shall be included
//   in all copies or substantial portions of the Software.
//
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
//   OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//   MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//   IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//   CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//   TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//   SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
// <summary>
//   Updates the minor/major step intervals if they are undefined.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;

namespace OxyPlot
{
    public class Axis : IAxis
    {
        public Axis()
        {
            Position = AxisPosition.Left;
            IsVisible = true;

            Minimum = double.NaN;
            Maximum = double.NaN;
            MinorStep = double.NaN;
            MajorStep = double.NaN;

            MinimumPadding = 0.01;
            MaximumPadding = 0.01;

            TickStyle = TickStyle.Inside;
            MajorGridlineStyle = LineStyle.None;
            MinorGridlineStyle = LineStyle.None;
            TicklineColor = Colors.Black;
            MajorGridlineColor = Color.FromARGB(0x40, 0, 0, 0);
            TicklineColor = Colors.Black;
            MinorGridlineColor = Color.FromARGB(0x20, 0, 0, 0x00);
            MajorGridlineThickness = 1;
            MinorGridlineThickness = 1;

            ExtraGridlineStyle = LineStyle.Solid;
            ExtraGridlineColor = Colors.Black;
            ExtraGridlineThickness = 1;

            ShowMinorTicks = true;

            FontFamily = "Segoe UI";
            FontSize = 12;

            MinorTickSize = 4;
            MajorTickSize = 7;

            StartPosition = 0;
            EndPosition = 1;

            Angle = 0;
        }

        public Axis(AxisPosition pos, double minimum, double maximum)
            : this()
        {
            Position = pos;
            Minimum = minimum;
            Maximum = maximum;
        }
        public string Key { get; set; }

        public AxisPosition Position { get; set; }
        public bool PositionAtZeroCrossing { get; set; }
        public bool IsHorizontal { get { return Position == AxisPosition.Top || Position == AxisPosition.Bottom; } }
        public bool IsVertical { get { return Position == AxisPosition.Left || Position == AxisPosition.Right; } }
        public bool IsPolar { get { return Position == AxisPosition.Magnitude || Position == AxisPosition.Angle; } }

        public bool IsVisible { get; set; }

        public double ActualMinimum { get; set; }
        public double ActualMaximum { get; set; }
        internal double ActualMinorStep { get; set; }
        internal double ActualMajorStep { get; set; }

        public double Minimum { get; set; }
        public double Maximum { get; set; }
        public double MinorStep { get; set; }
        public double MajorStep { get; set; }

        public double MinimumPadding { get; set; }
        public double MaximumPadding { get; set; }

        public TickStyle TickStyle { get; set; }
        public double MinorTickSize { get; set; }
        public double MajorTickSize { get; set; }
        public Color TicklineColor { get; set; }
        public bool ShowMinorTicks { get; set; }

        public LineStyle MajorGridlineStyle { get; set; }
        public LineStyle MinorGridlineStyle { get; set; }
        public Color MajorGridlineColor { get; set; }
        public Color MinorGridlineColor { get; set; }
        public double MajorGridlineThickness { get; set; }
        public double MinorGridlineThickness { get; set; }

        public double[] ExtraGridlines { get; set; }
        public LineStyle ExtraGridlineStyle { get; set; }
        public Color ExtraGridlineColor { get; set; }
        public double ExtraGridlineThickness { get; set; }

        public double Angle { get; set; }
        public string StringFormat { get; set; }
        internal string ActualStringFormat { get; set; }
        public string Title { get; set; }
        public string Unit { get; set; }

        public string FontFamily { get; set; }
        public double FontSize { get; set; }
        public double FontWeight { get; set; }

        public double StartPosition { get; set; }
        public double EndPosition { get; set; }

        public Axis RelatedAxis { get; set; }

        public bool IsReversed { get { return StartPosition > EndPosition; } }

        internal double Offset;
        internal double Scale;
        internal Point MidPoint;
        internal Point ScreenMin;
        internal Point ScreenMax;

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}({1}, {2}, {3}, {4})", GetType().Name, Position, ActualMinimum, ActualMaximum, ActualMajorStep);
        }

        public virtual void GetTickValues(out ICollection<double> majorValues, out ICollection<double> minorValues)
        {
            minorValues = CreateTickValues(ActualMinimum, ActualMaximum, ActualMinorStep);
            majorValues = CreateTickValues(ActualMinimum, ActualMaximum, ActualMajorStep);
        }

        public virtual string FormatValue(double x)
        {
            return x.ToString(ActualStringFormat, CultureInfo.InvariantCulture);
        }

        private static ICollection<double> CreateTickValues(double min, double max, double step)
        {
            if (max <= min)
                throw new InvalidOperationException("Axis: Maximum should be larger than minimum.");
            if (step <= 0)
                throw new InvalidOperationException("Axis: Step cannot be negative.");

            double x = (int)Math.Round(min / step) * step;

            var values = new Collection<double>();
            // Maximum number of iterations (in case of very small step size)
            int it = 0;
            const int maxit = 1000;
            double epsilon = Math.Abs(max - min) * 1e-6;
            while (x <= max + epsilon && it++ < maxit)
            {
                if (x >= min - epsilon && x <= max + epsilon)
                {
                    x = RemoveNoiseFromDoubleMath(x);
                    values.Add(x);
                }
                x += step;
            }
            return values;
        }

        protected virtual double PreTransform(double x)
        {
            return x;
        }

        protected virtual double PostInverseTransform(double x)
        {
            return x;
        }

        public virtual Point Transform(double x, double y, Axis yAxis)
        {
            Debug.Assert(yAxis != null);
            if (IsPolar)
            {
                double r = (x - Offset) * Scale;
                double th = yAxis != null ? (y - yAxis.Offset) * yAxis.Scale : double.NaN;
                return new Point(MidPoint.X + r * Math.Cos(th), MidPoint.Y + r * Math.Sin(th));
            }
            if (yAxis == null)
                return new Point();
            return new Point(TransformX(x), yAxis != null ? yAxis.TransformX(y) : double.NaN);
        }

        public double TransformX(double x)
        {
            return (PreTransform(x) - Offset) * Scale;
        }

        public virtual Point InverseTransform(double x, double y, Axis yAxis)
        {
            Debug.Assert(yAxis != null);
            if (IsPolar)
            {
                x -= MidPoint.X;
                y -= MidPoint.Y;
                double th = Math.Atan2(y, x);
                double r = Math.Sqrt(x * x + y * y);
                x = r / Scale + Offset;
                y = yAxis != null ? th / yAxis.Scale + yAxis.Offset : double.NaN;
                return new Point(x, y);
            }

            return new Point(InverseTransformX(x), yAxis.InverseTransformX(y));
        }

        public double InverseTransformX(double x)
        {
            return PostInverseTransform(x / Scale + Offset);
        }

        public double UpdateTransform(double x0, double x1, double y0, double y1)
        {
            ScreenMin = new Point(x0, y1);
            ScreenMax = new Point(x1, y0);

            if (Position == AxisPosition.Angle)
            {
                MidPoint = new Point((x0 + x1) / 2, (y0 + y1) / 2);
                Scale = 2 * Math.PI / (ActualMaximum - ActualMinimum);
                Offset = ActualMinimum;
                return Scale;
            }
            if (Position == AxisPosition.Magnitude)
            {
                ActualMinimum = 0;
                MidPoint = new Point((x0 + x1) / 2, (y0 + y1) / 2);
                double r = Math.Min(Math.Abs(x1 - x0), Math.Abs(y1 - y0));
                Scale = 0.5 * r / (ActualMaximum - ActualMinimum);
                Offset = ActualMinimum;
                return Scale;
            }
            double a0 = IsHorizontal ? x0 : y0;
            double a1 = IsHorizontal ? x1 : y1;

            double dx = a1 - a0;
            a1 = a0 + EndPosition * dx;
            a0 = a0 + StartPosition * dx;

            if (ActualMaximum - ActualMinimum < double.Epsilon)
                ActualMaximum = ActualMinimum + 1;

            double max = PreTransform(ActualMaximum);
            double min = PreTransform(ActualMinimum);

            const double eps = 1e-6;
            if (max - min < eps) max = min + 1;

            if (Math.Abs(a0 - a1) != 0)
                Offset = (a0 * max - min * a1) / (a0 - a1);
            else
                Offset = 0;

            Scale = (a1 - a0) / (max - min);

            return Scale;
        }

        public void SetScale(double scale)
        {
            double sgn = Math.Sign(Scale);
            double mid = (ActualMaximum + ActualMinimum) / 2;
            double dx = (Offset - mid) * Scale;
            Scale = sgn * scale;
            Offset = dx / Scale + mid;
        }

        public virtual void Pan(double dx)
        {
            Minimum = ActualMinimum + dx;
            Maximum = ActualMaximum + dx;
        }

        public virtual void ScaleAt(double factor, double x)
        {
            double dx0 = (ActualMinimum - x) * Scale;
            double dx1 = (ActualMaximum - x) * Scale;
            Scale *= factor;
            Minimum = dx0 / Scale + x;
            Maximum = dx1 / Scale + x;
        }

        public virtual void Zoom(double x0, double x1)
        {
            Minimum = Math.Min(x0, x1);
            Maximum = Math.Max(x0, x1);
        }

        public virtual void Reset()
        {
            Minimum = double.NaN;
            Maximum = double.NaN;
        }

        /// <summary>
        /// Updates the minor/major step intervals if they are undefined.
        /// </summary>
        public void UpdateIntervals(double dx, double dy)
        {
            double labelSize = GetLabelSize();
            double length = IsHorizontal ? dx : dy;

            if (!double.IsNaN(MajorStep))
                ActualMajorStep = MajorStep;
            else
                ActualMajorStep = CalculateActualInterval(length, labelSize);

            if (!double.IsNaN(MinorStep))
                ActualMinorStep = MinorStep;
            else
                ActualMinorStep = ActualMajorStep / 5;

            if (double.IsNaN(ActualMinorStep))
                ActualMinorStep = 2;
            if (double.IsNaN(ActualMajorStep))
                ActualMajorStep = 10;

            ActualStringFormat = StringFormat;
        }

        private double GetLabelSize()
        {
            if (IsHorizontal)
                return 100;
            if (IsVertical)
                return 30;
            if (Position == AxisPosition.Angle)
                return 50;
            if (Position == AxisPosition.Magnitude)
                return 100;
            return 50;
        }

        protected virtual double CalculateActualInterval(double availableSize, double maxIntervalSize)
        {
            return CalculateActualInterval2(availableSize, maxIntervalSize);
        }

        private double CalculateActualInterval1(double availableSize, double maxIntervalSize)
        {
            int minTags = 5;
            int maxTags = 20;
            int numberOfTags = (int)(availableSize / maxIntervalSize);
            double range = ActualMaximum - ActualMinimum;
            double interval = range / numberOfTags;
            const int k1 = 10;
            interval = Math.Log10(interval / k1);
            interval = Math.Ceiling(interval);
            interval = Math.Pow(10, interval) * k1;

            if (range / interval > maxTags) interval *= 5;
            if (range / interval < minTags) interval *= 0.5;

            if (interval <= 0) interval = 1;
            return interval;
        }

        /// <summary>
        /// Returns the actual interval to use to determine which values are
        /// displayed in the axis.
        /// </summary>
        /// <param name="availableSize">The available size.</param>
        /// <returns>Actual interval to use to determine which values are
        /// displayed in the axis.
        /// </returns>
        private double CalculateActualInterval2(double availableSize, double maxIntervalSize)
        {
            Func<double, double> Exponent = x => Math.Ceiling(Math.Log(x, 10));
            Func<double, double> Mantissa = x => x / Math.Pow(10, Exponent(x) - 1);

            // reduce intervals for horizontal axis.
            // double maxIntervals = Orientation == AxisOrientation.X ? MaximumAxisIntervalsPer200Pixels * 0.8 : MaximumAxisIntervalsPer200Pixels;
            // real maximum interval count
            double maxIntervalCount = availableSize / maxIntervalSize;

            double range = Math.Abs(ActualMinimum - ActualMaximum);
            double interval = Math.Pow(10, Exponent(range));
            double tempInterval = interval;

            // decrease interval until interval count becomes less than maxIntervalCount
            while (true)
            {
                int mantissa = (int)Mantissa(tempInterval);
                if (mantissa == 5)
                {
                    // reduce 5 to 2
                    tempInterval = RemoveNoiseFromDoubleMath(tempInterval / 2.5);
                }
                else if (mantissa == 2 || mantissa == 1 || mantissa == 10)
                {
                    // reduce 2 to 1,10 to 5,1 to 0.5
                    tempInterval = RemoveNoiseFromDoubleMath(tempInterval / 2.0);
                }

                if (range / tempInterval > maxIntervalCount)
                {
                    break;
                }

                interval = tempInterval;
            }
            return interval;
        }

        /// <summary>
        /// Removes the noise from double math.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>A double without a noise.</returns>
        internal static double RemoveNoiseFromDoubleMath(double value)
        {
            if (value == 0.0 || Math.Abs((Math.Log10(Math.Abs(value)))) < 27)
            {
                return (double)((decimal)value);
            }
            return Double.Parse(value.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        }

        public void Include(double p)
        {
            if (double.IsNaN(p) || double.IsInfinity(p))
                return;

            if (double.IsNaN(ActualMinimum))
                ActualMinimum = p;
            else
                ActualMinimum = Math.Min(ActualMinimum, p);

            if (double.IsNaN(ActualMaximum))
                ActualMaximum = p;
            else
                ActualMaximum = Math.Max(ActualMaximum, p);
        }

    }
}