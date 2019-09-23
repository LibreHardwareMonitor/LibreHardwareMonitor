// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeAxis.cs" company="OxyPlot">
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
//   Represents a DateTime axis.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Axes
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Represents a axis presenting <see cref="System.DateTime"/> values.
    /// </summary>
    /// <remarks>
    /// The actual numeric values on the axis are days since 1900/01/01.
    /// Use the static ToDouble and ToDateTime to convert numeric values to DateTimes.
    /// The StringFormat value can be used to force formatting of the axis values
    /// "yyyy-MM-dd" shows date
    /// "w" or "ww" shows week number
    /// "h:mm" shows hours and minutes
    /// </remarks>
    public class DateTimeAxis : LinearAxis
    {
        /// <summary>
        /// The time origin.
        /// </summary>
        /// <remarks>
        /// Same date values as Excel
        /// </remarks>
        private static DateTime timeOrigin = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// The actual interval type.
        /// </summary>
        private DateTimeIntervalType actualIntervalType;

        /// <summary>
        /// The actual minor interval type.
        /// </summary>
        private DateTimeIntervalType actualMinorIntervalType;

        /// <summary>
        /// Initializes a new instance of the <see cref = "DateTimeAxis" /> class.
        /// </summary>
        public DateTimeAxis()
        {
            this.Position = AxisPosition.Bottom;
            this.IntervalType = DateTimeIntervalType.Auto;
            this.FirstDayOfWeek = DayOfWeek.Monday;
            this.CalendarWeekRule = CalendarWeekRule.FirstFourDayWeek;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeAxis"/> class.
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
        /// <param name="intervalType">
        /// The interval type.
        /// </param>
        public DateTimeAxis(
            AxisPosition pos = AxisPosition.Bottom,
            string title = null,
            string format = null,
            DateTimeIntervalType intervalType = DateTimeIntervalType.Auto)
            : base(pos, title)
        {
            this.FirstDayOfWeek = DayOfWeek.Monday;
            this.CalendarWeekRule = CalendarWeekRule.FirstFourDayWeek;

            this.StringFormat = format;
            this.IntervalType = intervalType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeAxis"/> class.
        /// </summary>
        /// <param name="firstDateTime">
        /// The first date/time on the axis.
        /// </param>
        /// <param name="lastDateTime">
        /// The last date/time on the axis.
        /// </param>
        /// <param name="pos">
        /// The position of the axis.
        /// </param>
        /// <param name="title">
        /// The axis title.
        /// </param>
        /// <param name="format">
        /// The string format for the axis values.
        /// </param>
        /// <param name="intervalType">
        /// The interval type.
        /// </param>
        [Obsolete]
        public DateTimeAxis(
            DateTime firstDateTime,
            DateTime lastDateTime,
            AxisPosition pos = AxisPosition.Bottom,
            string title = null,
            string format = null,
            DateTimeIntervalType intervalType = DateTimeIntervalType.Auto)
            : this(pos, title, format, intervalType)
        {
            this.Minimum = ToDouble(firstDateTime);
            this.Maximum = ToDouble(lastDateTime);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeAxis" /> class.
        /// </summary>
        /// <param name="pos">The position of the axis.</param>
        /// <param name="firstDateTime">The first date/time on the axis.</param>
        /// <param name="lastDateTime">The last date/time on the axis.</param>
        /// <param name="title">The axis title.</param>
        /// <param name="format">The string format for the axis values.</param>
        /// <param name="intervalType">The interval type.</param>
        public DateTimeAxis(
        AxisPosition pos,
        DateTime firstDateTime,
        DateTime lastDateTime,
        string title = null,
        string format = null,
        DateTimeIntervalType intervalType = DateTimeIntervalType.Auto)
            : this(pos, title, format, intervalType)
        {
            this.Minimum = ToDouble(firstDateTime);
            this.Maximum = ToDouble(lastDateTime);
        }

        /// <summary>
        /// Gets or sets CalendarWeekRule.
        /// </summary>
        public CalendarWeekRule CalendarWeekRule { get; set; }

        /// <summary>
        /// Gets or sets FirstDayOfWeek.
        /// </summary>
        public DayOfWeek FirstDayOfWeek { get; set; }

        /// <summary>
        /// Gets or sets IntervalType.
        /// </summary>
        public DateTimeIntervalType IntervalType { get; set; }

        /// <summary>
        /// Gets or sets MinorIntervalType.
        /// </summary>
        public DateTimeIntervalType MinorIntervalType { get; set; }

        /// <summary>
        /// Creates a data point.
        /// </summary>
        /// <param name="x">
        /// The x value.
        /// </param>
        /// <param name="y">
        /// The y value.
        /// </param>
        /// <returns>
        /// A data point.
        /// </returns>
        public static DataPoint CreateDataPoint(DateTime x, double y)
        {
            return new DataPoint(ToDouble(x), y);
        }

        /// <summary>
        /// Creates a data point.
        /// </summary>
        /// <param name="x">
        /// The x value.
        /// </param>
        /// <param name="y">
        /// The y value. 
        /// </param>
        /// <returns>
        /// A data point.
        /// </returns>
        public static DataPoint CreateDataPoint(DateTime x, DateTime y)
        {
            return new DataPoint(ToDouble(x), ToDouble(y));
        }

        /// <summary>
        /// Creates a data point.
        /// </summary>
        /// <param name="x">
        /// The x value.
        /// </param>
        /// <param name="y">
        /// The y value.
        /// </param>
        /// <returns>
        /// A data point.
        /// </returns>
        public static DataPoint CreateDataPoint(double x, DateTime y)
        {
            return new DataPoint(x, ToDouble(y));
        }

        /// <summary>
        /// Converts a numeric representation of the date (number of days after the time origin) to a DateTime structure.
        /// </summary>
        /// <param name="value">
        /// The number of days after the time origin.
        /// </param>
        /// <returns>
        /// A date/time structure.
        /// </returns>
        public static DateTime ToDateTime(double value)
        {
            if (double.IsNaN(value))
            {
                return new DateTime();
            }

            return timeOrigin.AddDays(value - 1);
        }

        /// <summary>
        /// Converts a DateTime to days after the time origin.
        /// </summary>
        /// <param name="value">
        /// The date/time structure.
        /// </param>
        /// <returns>
        /// The number of days after the time origin.
        /// </returns>
        public static double ToDouble(DateTime value)
        {
            var span = value - timeOrigin;
            return span.TotalDays + 1;
        }

        /// <summary>
        /// Formats the specified value by the axis' ActualStringFormat.
        /// </summary>
        /// <param name="x">
        /// The x.
        /// </param>
        /// <returns>
        /// The formatted DateTime value
        /// </returns>
        public override string FormatValue(double x)
        {
            // convert the double value to a DateTime
            var time = ToDateTime(x);

            string fmt = this.ActualStringFormat;
            if (fmt == null)
            {
                return time.ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern);
            }

            int week = this.GetWeek(time);
            fmt = fmt.Replace("ww", week.ToString("00"));
            fmt = fmt.Replace("w", week.ToString(CultureInfo.InvariantCulture));
            return time.ToString(fmt, this.ActualCulture);
        }

        /// <summary>
        /// Gets the tick values.
        /// </summary>
        /// <param name="majorLabelValues">
        /// The major label values.
        /// </param>
        /// <param name="majorTickValues">
        /// The major tick values.
        /// </param>
        /// <param name="minorTickValues">
        /// The minor tick values.
        /// </param>
        public override void GetTickValues(
            out IList<double> majorLabelValues, out IList<double> majorTickValues, out IList<double> minorTickValues)
        {
            minorTickValues = this.CreateDateTimeTickValues(
                this.ActualMinimum, this.ActualMaximum, this.ActualMinorStep, this.actualMinorIntervalType);
            majorTickValues = this.CreateDateTimeTickValues(
                this.ActualMinimum, this.ActualMaximum, this.ActualMajorStep, this.actualIntervalType);
            majorLabelValues = majorTickValues;
        }

        /// <summary>
        /// Gets the value from an axis coordinate, converts from double to the correct data type if necessary.
        /// e.g. DateTimeAxis returns the DateTime and CategoryAxis returns category strings.
        /// </summary>
        /// <param name="x">
        /// The coordinate.
        /// </param>
        /// <returns>
        /// The value.
        /// </returns>
        public override object GetValue(double x)
        {
            var time = ToDateTime(x);

            return time;
        }

        /// <summary>
        /// Updates the intervals.
        /// </summary>
        /// <param name="plotArea">
        /// The plot area.
        /// </param>
        internal override void UpdateIntervals(OxyRect plotArea)
        {
            base.UpdateIntervals(plotArea);
            switch (this.actualIntervalType)
            {
                case DateTimeIntervalType.Years:
                    this.ActualMinorStep = 31;
                    this.actualMinorIntervalType = DateTimeIntervalType.Years;
                    if (this.ActualStringFormat == null)
                    {
                        this.ActualStringFormat = "yyyy";
                    }

                    break;
                case DateTimeIntervalType.Months:
                    this.actualMinorIntervalType = DateTimeIntervalType.Months;
                    if (this.ActualStringFormat == null)
                    {
                        this.ActualStringFormat = "yyyy-MM-dd";
                    }

                    break;
                case DateTimeIntervalType.Weeks:
                    this.actualMinorIntervalType = DateTimeIntervalType.Days;
                    this.ActualMajorStep = 7;
                    this.ActualMinorStep = 1;
                    if (this.ActualStringFormat == null)
                    {
                        this.ActualStringFormat = "yyyy/ww";
                    }

                    break;
                case DateTimeIntervalType.Days:
                    this.ActualMinorStep = this.ActualMajorStep;
                    if (this.ActualStringFormat == null)
                    {
                        this.ActualStringFormat = "yyyy-MM-dd";
                    }

                    break;
                case DateTimeIntervalType.Hours:
                    this.ActualMinorStep = this.ActualMajorStep;
                    if (this.ActualStringFormat == null)
                    {
                        this.ActualStringFormat = "HH:mm";
                    }

                    break;
                case DateTimeIntervalType.Minutes:
                    this.ActualMinorStep = this.ActualMajorStep;
                    if (this.ActualStringFormat == null)
                    {
                        this.ActualStringFormat = "HH:mm";
                    }

                    break;
                case DateTimeIntervalType.Seconds:
                    this.ActualMinorStep = this.ActualMajorStep;
                    if (this.ActualStringFormat == null)
                    {
                        this.ActualStringFormat = "HH:mm:ss";
                    }

                    break;
                case DateTimeIntervalType.Manual:
                    break;
                case DateTimeIntervalType.Auto:
                    break;
            }
        }

        /// <summary>
        /// Calculates the actual interval.
        /// </summary>
        /// <param name="availableSize">
        /// Size of the available area.
        /// </param>
        /// <param name="maxIntervalSize">
        /// Maximum length of the intervals.
        /// </param>
        /// <returns>
        /// The calculate actual interval.
        /// </returns>
        protected override double CalculateActualInterval(double availableSize, double maxIntervalSize)
        {
            const double Year = 365.25;
            const double Month = 30.5;
            const double Week = 7;
            const double Day = 1.0;
            const double Hour = Day / 24;
            const double Minute = Hour / 60;
            const double Second = Minute / 60;

            double range = Math.Abs(this.ActualMinimum - this.ActualMaximum);

            var goodIntervals = new[]
                                    {
                                        Second, 2 * Second, 5 * Second, 10 * Second, 30 * Second, Minute, 2 * Minute, 
                                        5 * Minute, 10 * Minute, 30 * Minute, Hour, 4 * Hour, 8 * Hour, 12 * Hour, Day, 
                                        2 * Day, 5 * Day, Week, 2 * Week, Month, 2 * Month, 3 * Month, 4 * Month, 
                                        6 * Month, Year
                                    };

            double interval = goodIntervals[0];

            int maxNumberOfIntervals = Math.Max((int)(availableSize / maxIntervalSize), 2);

            while (true)
            {
                if (range / interval < maxNumberOfIntervals)
                {
                    break;
                }

                double nextInterval = goodIntervals.FirstOrDefault(i => i > interval);
                if (Math.Abs(nextInterval) < double.Epsilon)
                {
                    nextInterval = interval * 2;
                }

                interval = nextInterval;
            }

            this.actualIntervalType = this.IntervalType;
            this.actualMinorIntervalType = this.MinorIntervalType;

            if (this.IntervalType == DateTimeIntervalType.Auto)
            {
                this.actualIntervalType = DateTimeIntervalType.Seconds;
                if (interval >= 1.0 / 24 / 60)
                {
                    this.actualIntervalType = DateTimeIntervalType.Minutes;
                }

                if (interval >= 1.0 / 24)
                {
                    this.actualIntervalType = DateTimeIntervalType.Hours;
                }

                if (interval >= 1)
                {
                    this.actualIntervalType = DateTimeIntervalType.Days;
                }

                if (interval >= 30)
                {
                    this.actualIntervalType = DateTimeIntervalType.Months;
                }

                if (range >= 365.25)
                {
                    this.actualIntervalType = DateTimeIntervalType.Years;
                }
            }

            if (this.actualIntervalType == DateTimeIntervalType.Months)
            {
                double monthsRange = range / 30.5;
                interval = this.CalculateActualInterval(availableSize, maxIntervalSize, monthsRange);
            }

            if (this.actualIntervalType == DateTimeIntervalType.Years)
            {
                double yearsRange = range / 365.25;
                interval = this.CalculateActualInterval(availableSize, maxIntervalSize, yearsRange);
            }

            if (this.actualMinorIntervalType == DateTimeIntervalType.Auto)
            {
                switch (this.actualIntervalType)
                {
                    case DateTimeIntervalType.Years:
                        this.actualMinorIntervalType = DateTimeIntervalType.Months;
                        break;
                    case DateTimeIntervalType.Months:
                        this.actualMinorIntervalType = DateTimeIntervalType.Days;
                        break;
                    case DateTimeIntervalType.Weeks:
                        this.actualMinorIntervalType = DateTimeIntervalType.Days;
                        break;
                    case DateTimeIntervalType.Days:
                        this.actualMinorIntervalType = DateTimeIntervalType.Hours;
                        break;
                    case DateTimeIntervalType.Hours:
                        this.actualMinorIntervalType = DateTimeIntervalType.Minutes;
                        break;
                    default:
                        this.actualMinorIntervalType = DateTimeIntervalType.Days;
                        break;
                }
            }

            return interval;
        }

        /// <summary>
        /// Creates the date tick values.
        /// </summary>
        /// <param name="min">
        /// The min.
        /// </param>
        /// <param name="max">
        /// The max.
        /// </param>
        /// <param name="step">
        /// The step.
        /// </param>
        /// <param name="intervalType">
        /// Type of the interval.
        /// </param>
        /// <returns>
        /// Date tick values.
        /// </returns>
        private IList<double> CreateDateTickValues(
            double min, double max, double step, DateTimeIntervalType intervalType)
        {
            DateTime start = ToDateTime(min);
            switch (intervalType)
            {
                case DateTimeIntervalType.Weeks:

                    // make sure the first tick is at the 1st day of a week
                    start = start.AddDays(-(int)start.DayOfWeek + (int)this.FirstDayOfWeek);
                    break;
                case DateTimeIntervalType.Months:

                    // make sure the first tick is at the 1st of a month
                    start = new DateTime(start.Year, start.Month, 1);
                    break;
                case DateTimeIntervalType.Years:

                    // make sure the first tick is at Jan 1st
                    start = new DateTime(start.Year, 1, 1);
                    break;
            }

            // Adds a tick to the end time to make sure the end DateTime is included.
            DateTime end = ToDateTime(max).AddTicks(1);

            DateTime current = start;
            var values = new Collection<double>();
            double eps = step * 1e-3;
            DateTime minDateTime = ToDateTime(min - eps);
            DateTime maxDateTime = ToDateTime(max + eps);
            while (current < end)
            {
                if (current > minDateTime && current < maxDateTime)
                {
                    values.Add(ToDouble(current));
                }

                switch (intervalType)
                {
                    case DateTimeIntervalType.Months:
                        current = current.AddMonths((int)Math.Ceiling(step));
                        break;
                    case DateTimeIntervalType.Years:
                        current = current.AddYears((int)Math.Ceiling(step));
                        break;
                    default:
                        current = current.AddDays(step);
                        break;
                }
            }

            return values;
        }

        /// <summary>
        /// Creates date/time tick values.
        /// </summary>
        /// <param name="min">
        /// The min.
        /// </param>
        /// <param name="max">
        /// The max.
        /// </param>
        /// <param name="interval">
        /// The interval.
        /// </param>
        /// <param name="intervalType">
        /// The interval type.
        /// </param>
        /// DateTime tick values.
        /// <returns>
        /// DateTime tick values.
        /// </returns>
        private IList<double> CreateDateTimeTickValues(
            double min, double max, double interval, DateTimeIntervalType intervalType)
        {
            // If the step size is more than 7 days (e.g. months or years) we use a specialized tick generation method that adds tick values with uneven spacing...
            if (intervalType > DateTimeIntervalType.Days)
            {
                return this.CreateDateTickValues(min, max, interval, intervalType);
            }

            // For shorter step sizes we use the method from Axis
            return CreateTickValues(min, max, interval);
        }

        /// <summary>
        /// Gets the week number for the specified date.
        /// </summary>
        /// <param name="date">
        /// The date.
        /// </param>
        /// <returns>
        /// The week number for the current culture.
        /// </returns>
        private int GetWeek(DateTime date)
        {
            return this.ActualCulture.Calendar.GetWeekOfYear(date, this.CalendarWeekRule, this.FirstDayOfWeek);
        }
    }
}