// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TimeSpanAxis.cs" company="OxyPlot">
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
//   Time axis.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Axes
{
    using System;
    using System.Linq;

    /// <summary>
    /// Represents an axis presenting <see cref="System.TimeSpan"/> values.
    /// </summary>
    /// <remarks>
    /// The values should be in seconds.
    /// The StringFormat value can be used to force formatting of the axis values
    /// "h:mm" shows hours and minutes
    /// "m:ss" shows minutes and seconds
    /// </remarks>
    public class TimeSpanAxis : LinearAxis
    {
        /// <summary>
        /// Initializes a new instance of the <see cref = "TimeSpanAxis" /> class.
        /// </summary>
        public TimeSpanAxis()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSpanAxis"/> class.
        /// </summary>
        /// <param name="pos">
        /// The position.
        /// </param>
        /// <param name="title">
        /// The axis title.
        /// </param>
        /// <param name="format">
        /// The string format for the axis values.
        /// </param>
        public TimeSpanAxis(AxisPosition pos, string title = null, string format = "m:ss")
            : base(pos, title)
        {
            this.StringFormat = format;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSpanAxis"/> class.
        /// </summary>
        /// <param name="pos">
        /// The position.
        /// </param>
        /// <param name="min">
        /// The min.
        /// </param>
        /// <param name="max">
        /// The max.
        /// </param>
        /// <param name="title">
        /// The axis title.
        /// </param>
        /// <param name="format">
        /// The string format for the axis values.
        /// </param>
        public TimeSpanAxis(
            AxisPosition pos = AxisPosition.Bottom,
            double min = double.NaN,
            double max = double.NaN,
            string title = null,
            string format = "m:ss")
            : base(pos, min, max, title)
        {
            this.StringFormat = format;
        }

        /// <summary>
        /// Converts a time span to a double.
        /// </summary>
        /// <param name="s">
        /// The time span.
        /// </param>
        /// <returns>
        /// A double value.
        /// </returns>
        public static double ToDouble(TimeSpan s)
        {
            return s.TotalSeconds;
        }

        /// <summary>
        /// Converts a double to a time span.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <returns>
        /// A time span.
        /// </returns>
        public static TimeSpan ToTimeSpan(double value)
        {
            return TimeSpan.FromSeconds(value);
        }

        /// <summary>
        /// Formats the value.
        /// </summary>
        /// <param name="x">
        /// The x.
        /// </param>
        /// <returns>
        /// The format value.
        /// </returns>
        public override string FormatValue(double x)
        {
            TimeSpan span = TimeSpan.FromSeconds(x);
            string s = this.ActualStringFormat ?? "h:mm:ss";

            s = s.Replace("mm", span.Minutes.ToString("00"));
            s = s.Replace("ss", span.Seconds.ToString("00"));
            s = s.Replace("hh", span.Hours.ToString("00"));
            s = s.Replace("msec", span.Milliseconds.ToString("000"));
            s = s.Replace("m", ((int)span.TotalMinutes).ToString("0"));
            s = s.Replace("s", ((int)span.TotalSeconds).ToString("0"));
            s = s.Replace("h", ((int)span.TotalHours).ToString("0"));
            return s;
        }

        /// <summary>
        /// Gets the value from an axis coordinate, converts from double to the correct data type if necessary. e.g. DateTimeAxis returns the DateTime and CategoryAxis returns category strings.
        /// </summary>
        /// <param name="x">The coordinate.</param>
        /// <returns>
        /// The value.
        /// </returns>
        public override object GetValue(double x)
        {
            return TimeSpan.FromSeconds(x);
        }

        /// <summary>
        /// Calculates the actual interval.
        /// </summary>
        /// <param name="availableSize">Size of the available area.</param>
        /// <param name="maxIntervalSize">Maximum length of the intervals.</param>
        /// <returns>
        /// The calculate actual interval.
        /// </returns>
        protected override double CalculateActualInterval(double availableSize, double maxIntervalSize)
        {
            double range = Math.Abs(this.ActualMinimum - this.ActualMaximum);
            double interval = 1;
            var goodIntervals = new[] { 1.0, 5, 10, 30, 60, 120, 300, 600, 900, 1200, 1800, 3600 };

            int maxNumberOfIntervals = Math.Max((int)(availableSize / maxIntervalSize), 2);

            while (true)
            {
                if (range / interval < maxNumberOfIntervals)
                {
                    return interval;
                }

                double nextInterval = goodIntervals.FirstOrDefault(i => i > interval);
                if (Math.Abs(nextInterval) < double.Epsilon)
                {
                    nextInterval = interval * 2;
                }

                interval = nextInterval;
            }
        }
    }
}