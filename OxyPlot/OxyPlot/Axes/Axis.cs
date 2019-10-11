// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Axis.cs" company="OxyPlot">
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
//   Abstract base class for axes.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Axes
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;

    using OxyPlot.Series;

    /// <summary>
    /// Provides an abstract base class for axes.
    /// </summary>
    public abstract class Axis : PlotElement
    {
        /// <summary>
        /// Exponent function.
        /// </summary>
        protected static readonly Func<double, double> Exponent = x => Math.Round(Math.Log(Math.Abs(x), 10));

        /// <summary>
        /// Mantissa function. http://en.wikipedia.org/wiki/Mantissa
        /// </summary>
        protected static readonly Func<double, double> Mantissa = x => x / Math.Pow(10, Exponent(x));

        /// <summary>
        /// The offset.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate",
            Justification = "Reviewed. Suppression is OK here.")]
        protected double offset;

        /// <summary>
        /// The scale.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate",
            Justification = "Reviewed. Suppression is OK here.")]
        protected double scale;

        /// <summary>
        /// The position.
        /// </summary>
        private AxisPosition position;

        /// <summary>
        /// Initializes a new instance of the <see cref="Axis"/> class.
        /// </summary>
        protected Axis()
        {
            this.Position = AxisPosition.Left;
            this.PositionTier = 0;
            this.IsAxisVisible = true;
            this.Layer = AxisLayer.BelowSeries;

            this.ViewMaximum = double.NaN;
            this.ViewMinimum = double.NaN;

            this.AbsoluteMaximum = double.MaxValue;
            this.AbsoluteMinimum = double.MinValue;

            this.Minimum = double.NaN;
            this.Maximum = double.NaN;
            this.MinorStep = double.NaN;
            this.MajorStep = double.NaN;

            this.MinimumPadding = 0.01;
            this.MaximumPadding = 0.01;
            this.MinimumRange = 0;

            this.TickStyle = TickStyle.Outside;
            this.TicklineColor = OxyColors.Black;

            this.AxislineStyle = LineStyle.None;
            this.AxislineColor = OxyColors.Black;
            this.AxislineThickness = 1.0;

            this.MajorGridlineStyle = LineStyle.None;
            this.MajorGridlineColor = OxyColor.FromArgb(0x40, 0, 0, 0);
            this.MajorGridlineThickness = 1;

            this.MinorGridlineStyle = LineStyle.None;
            this.MinorGridlineColor = OxyColor.FromArgb(0x20, 0, 0, 0x00);
            this.MinorGridlineThickness = 1;

            this.ExtraGridlineStyle = LineStyle.Solid;
            this.ExtraGridlineColor = OxyColors.Black;
            this.ExtraGridlineThickness = 1;

            this.ShowMinorTicks = true;

            this.MinorTickSize = 4;
            this.MajorTickSize = 7;

            this.StartPosition = 0;
            this.EndPosition = 1;

            this.TitlePosition = 0.5;
            this.TitleFormatString = "{0} [{1}]";
            this.TitleClippingLength = 0.9;
            this.TitleColor = null;
            this.TitleFontSize = double.NaN;
            this.TitleFontWeight = FontWeights.Normal;
            this.ClipTitle = true;

            this.Angle = 0;

            this.IsZoomEnabled = true;
            this.IsPanEnabled = true;

            this.FilterMinValue = double.MinValue;
            this.FilterMaxValue = double.MaxValue;
            this.FilterFunction = null;

            this.IntervalLength = 60;

            this.AxisTitleDistance = 4;
            this.AxisTickToLabelDistance = 4;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Axis"/> class.
        /// </summary>
        /// <param name="pos">
        /// The position of the axis.
        /// </param>
        /// <param name="minimum">
        /// The minimum value.
        /// </param>
        /// <param name="maximum">
        /// The maximum value.
        /// </param>
        /// <param name="title">
        /// The axis title.
        /// </param>
        protected Axis(AxisPosition pos, double minimum, double maximum, string title = null)
            : this()
        {
            this.Position = pos;
            this.Minimum = minimum;
            this.Maximum = maximum;

            this.AbsoluteMaximum = double.NaN;
            this.AbsoluteMinimum = double.NaN;

            this.Title = title;
        }

        /// <summary>
        /// Occurs when the axis has been changed (by zooming, panning or resetting).
        /// </summary>
        public event EventHandler<AxisChangedEventArgs> AxisChanged;

        /// <summary>
        /// Gets or sets the absolute maximum. This is only used for the UI control. It will not be possible to zoom/pan beyond this limit.
        /// </summary>
        /// <value> The absolute maximum. </value>
        public double AbsoluteMaximum { get; set; }

        /// <summary>
        /// Gets or sets the absolute minimum. This is only used for the UI control. It will not be possible to zoom/pan beyond this limit.
        /// </summary>
        /// <value> The absolute minimum. </value>
        public double AbsoluteMinimum { get; set; }

        /// <summary>
        /// Gets the actual culture.
        /// </summary>
        /// <remarks>
        /// The culture is defined in the parent PlotModel.
        /// </remarks>
        public CultureInfo ActualCulture
        {
            get
            {
                return this.PlotModel != null ? this.PlotModel.ActualCulture : CultureInfo.CurrentCulture;
            }
        }

        /// <summary>
        /// Gets or sets the actual major step.
        /// </summary>
        public double ActualMajorStep { get; protected set; }

        /// <summary>
        /// Gets or sets the actual maximum value of the axis.
        /// </summary>
        /// <remarks>
        /// If ViewMaximum is not NaN, this value will be defined by ViewMaximum.
        /// Otherwise, if Maximum is not NaN, this value will be defined by Maximum.
        /// Otherwise, this value will be defined by the maximum (+padding) of the data.
        /// </remarks>
        public double ActualMaximum { get; protected set; }

        /// <summary>
        /// Gets or sets the actual minimum value of the axis.
        /// </summary>
        /// <remarks>
        /// If ViewMinimum is not NaN, this value will be defined by ViewMinimum.
        /// Otherwise, if Minimum is not NaN, this value will be defined by Minimum.
        /// Otherwise this value will be defined by the minimum (+padding) of the data.
        /// </remarks>
        public double ActualMinimum { get; protected set; }

        /// <summary>
        /// Gets or sets the maximum value of the data displayed on this axis.
        /// </summary>
        /// <value>The data maximum.</value>
        public double DataMaximum { get; protected set; }

        /// <summary>
        /// Gets or sets the minimum value of the data displayed on this axis.
        /// </summary>
        /// <value>The data minimum.</value>
        public double DataMinimum { get; protected set; }

        /// <summary>
        /// Gets or sets the actual minor step.
        /// </summary>
        public double ActualMinorStep { get; protected set; }

        /// <summary>
        /// Gets or sets the actual string format being used.
        /// </summary>
        public string ActualStringFormat { get; protected set; }

        /// <summary>
        /// Gets the actual title (including Unit if Unit is set).
        /// </summary>
        /// <value> The actual title. </value>
        public string ActualTitle
        {
            get
            {
                if (this.Unit != null)
                {
                    return string.Format(this.TitleFormatString, this.Title, this.Unit);
                }

                return this.Title;
            }
        }

        /// <summary>
        /// Gets or sets the angle for the axis values.
        /// </summary>
        public double Angle { get; set; }

        /// <summary>
        /// Gets or sets the distance from axis tick to number label.
        /// </summary>
        /// <value> The axis tick to label distance. </value>
        public double AxisTickToLabelDistance { get; set; }

        /// <summary>
        /// Gets or sets the distance from axis number to axis title.
        /// </summary>
        /// <value> The axis title distance. </value>
        public double AxisTitleDistance { get; set; }

        /// <summary>
        /// Gets or sets the color of the axis line.
        /// </summary>
        public OxyColor AxislineColor { get; set; }

        /// <summary>
        /// Gets or sets the axis line.
        /// </summary>
        public LineStyle AxislineStyle { get; set; }

        /// <summary>
        /// Gets or sets the axis line.
        /// </summary>
        public double AxislineThickness { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to clip the axis title.
        /// </summary>
        /// <remarks>
        /// The default value is true.
        /// </remarks>
        public bool ClipTitle { get; set; }

        /// <summary>
        /// Gets or sets the end position of the axis on the plot area. This is a fraction from 0(bottom/left) to 1(top/right).
        /// </summary>
        public double EndPosition { get; set; }

        /// <summary>
        /// Gets or sets the color of the extra gridlines.
        /// </summary>
        public OxyColor ExtraGridlineColor { get; set; }

        /// <summary>
        /// Gets or sets the extra gridlines line style.
        /// </summary>
        public LineStyle ExtraGridlineStyle { get; set; }

        /// <summary>
        /// Gets or sets the extra gridline thickness.
        /// </summary>
        public double ExtraGridlineThickness { get; set; }

        /// <summary>
        /// Gets or sets the values for extra gridlines.
        /// </summary>
        public double[] ExtraGridlines { get; set; }

        /// <summary>
        /// Gets or sets the filter function.
        /// </summary>
        /// <value> The filter function. </value>
        public Func<double, bool> FilterFunction { get; set; }

        /// <summary>
        /// Gets or sets the maximum value that can be shown using this axis. Values greater or equal to this value will not be shown.
        /// </summary>
        /// <value> The filter max value. </value>
        public double FilterMaxValue { get; set; }

        /// <summary>
        /// Gets or sets the minimum value that can be shown using this axis. Values smaller or equal to this value will not be shown.
        /// </summary>
        /// <value> The filter min value. </value>
        public double FilterMinValue { get; set; }

        /// <summary>
        /// Gets or sets the length of the interval (screen length). The available length of the axis will be divided by this length to get the approximate number of major intervals on the axis. The default value is 60.
        /// </summary>
        public double IntervalLength { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this axis is visible.
        /// </summary>
        public bool IsAxisVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether pan is enabled.
        /// </summary>
        public bool IsPanEnabled { get; set; }

        /// <summary>
        /// Gets a value indicating whether this axis is reversed. It is reversed if StartPosition>EndPosition.
        /// </summary>
        public bool IsReversed
        {
            get
            {
                return this.StartPosition > this.EndPosition;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether zoom is enabled.
        /// </summary>
        public bool IsZoomEnabled { get; set; }

        /// <summary>
        /// Gets or sets the key of the axis. This can be used to find an axis if you have defined multiple axes in a plot.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the layer.
        /// </summary>
        /// <value> The layer. </value>
        public AxisLayer Layer { get; set; }

        /// <summary>
        /// Gets or sets the color of the major gridline.
        /// </summary>
        public OxyColor MajorGridlineColor { get; set; }

        /// <summary>
        /// Gets or sets the major gridline style.
        /// </summary>
        public LineStyle MajorGridlineStyle { get; set; }

        /// <summary>
        /// Gets or sets the major gridline thickness.
        /// </summary>
        public double MajorGridlineThickness { get; set; }

        /// <summary>
        /// Gets or sets the major step. (the interval between large ticks with numbers).
        /// </summary>
        public double MajorStep { get; set; }

        /// <summary>
        /// Gets or sets the size of the major tick.
        /// </summary>
        public double MajorTickSize { get; set; }

        /// <summary>
        /// Gets or sets the maximum value of the axis.
        /// </summary>
        public double Maximum { get; set; }

        /// <summary>
        /// Gets or sets the 'padding' fraction of the maximum value. A value of 0.01 gives 1% more space on the maximum end of the axis. This property is not used if the Maximum property is set.
        /// </summary>
        public double MaximumPadding { get; set; }

        /// <summary>
        /// Gets or sets the minimum value of the axis.
        /// </summary>
        public double Minimum { get; set; }

        /// <summary>
        /// Gets or sets the 'padding' fraction of the minimum value. A value of 0.01 gives 1% more space on the minimum end of the axis. This property is not used if the Minimum property is set.
        /// </summary>
        public double MinimumPadding { get; set; }

        /// <summary>
        /// Gets or sets the minimum range of the axis. Setting this property ensures that ActualMaximum-ActualMinimum > MinimumRange.
        /// </summary>
        public double MinimumRange { get; set; }

        /// <summary>
        /// Gets or sets the color of the minor gridline.
        /// </summary>
        public OxyColor MinorGridlineColor { get; set; }

        /// <summary>
        /// Gets or sets the minor gridline style.
        /// </summary>
        public LineStyle MinorGridlineStyle { get; set; }

        /// <summary>
        /// Gets or sets the minor gridline thickness.
        /// </summary>
        public double MinorGridlineThickness { get; set; }

        /// <summary>
        /// Gets or sets the minor step (the interval between small ticks without number).
        /// </summary>
        public double MinorStep { get; set; }

        /// <summary>
        /// Gets or sets the size of the minor tick.
        /// </summary>
        public double MinorTickSize { get; set; }

        /// <summary>
        /// Gets or sets the offset. This is used to transform between data and screen coordinates.
        /// </summary>
        public double Offset
        {
            get
            {
                return this.offset;
            }

            protected set
            {
                this.offset = value;
            }
        }

        /// <summary>
        /// Gets or sets the position of the axis.
        /// </summary>
        public AxisPosition Position
        {
            get
            {
                return this.position;
            }

            set
            {
                this.position = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the axis should be positioned on the zero-crossing of the related axis.
        /// </summary>
        public bool PositionAtZeroCrossing { get; set; }

        /// <summary>
        /// Gets or sets the position tier which defines in which tier the axis is displayed.
        /// </summary>
        /// <remarks>
        /// The bigger the value the the further afar is the axis from the graph.
        /// </remarks>
        public int PositionTier { get; set; }

        /// <summary>
        /// Gets or sets the related axis. This is used for polar coordinate systems where the angle and magnitude axes are related.
        /// </summary>
        public Axis RelatedAxis { get; set; }

        /// <summary>
        /// Gets or sets the scaling factor of the axis. This is used to transform between data and screen coordinates.
        /// </summary>
        public double Scale
        {
            get
            {
                return this.scale;
            }

            protected set
            {
                this.scale = value;
            }
        }

        /// <summary>
        /// Gets or sets the screen coordinate of the Maximum point on the axis.
        /// </summary>
        public ScreenPoint ScreenMax { get; protected set; }

        /// <summary>
        /// Gets or sets the screen coordinate of the Minimum point on the axis.
        /// </summary>
        public ScreenPoint ScreenMin { get; protected set; }

        /// <summary>
        /// Gets or sets a value indicating whether minor ticks should be shown.
        /// </summary>
        public bool ShowMinorTicks { get; set; }

        /// <summary>
        /// Gets or sets the start position of the axis on the plot area. This is a fraction from 0(bottom/left) to 1(top/right).
        /// </summary>
        public double StartPosition { get; set; }

        /// <summary>
        /// Gets or sets the string format used for formatting the axis values.
        /// </summary>
        public string StringFormat { get; set; }

        /// <summary>
        /// Gets or sets the tick style (both for major and minor ticks).
        /// </summary>
        public TickStyle TickStyle { get; set; }

        /// <summary>
        /// Gets or sets the color of the ticks (both major and minor ticks).
        /// </summary>
        public OxyColor TicklineColor { get; set; }

        /// <summary>
        /// Gets or sets the title of the axis.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the length of the title clipping rectangle (fraction of the available length of the axis).
        /// </summary>
        /// <remarks>
        /// The default value is 0.9
        /// </remarks>
        public double TitleClippingLength { get; set; }

        /// <summary>
        /// Gets or sets the color of the title.
        /// </summary>
        /// <value> The color of the title. </value>
        /// <remarks>
        /// If TitleColor is null, the parent PlotModel's TextColor will be used.
        /// </remarks>
        public OxyColor TitleColor { get; set; }

        /// <summary>
        /// Gets or sets the title font.
        /// </summary>
        /// <value> The title font. </value>
        public string TitleFont { get; set; }

        /// <summary>
        /// Gets or sets the size of the title font.
        /// </summary>
        /// <value> The size of the title font. </value>
        public double TitleFontSize { get; set; }

        /// <summary>
        /// Gets or sets the title font weight.
        /// </summary>
        /// <value> The title font weight. </value>
        public double TitleFontWeight { get; set; }

        /// <summary>
        /// Gets or sets the format string used for formatting the title and unit when unit is defined. If unit is null, only Title is used. The default value is "{0} [{1}]", where {0} uses the Title and {1} uses the Unit.
        /// </summary>
        public string TitleFormatString { get; set; }

        /// <summary>
        /// Gets or sets the position of the title (0.5 is in the middle).
        /// </summary>
        public double TitlePosition { get; set; }

        /// <summary>
        /// Gets or sets the tool tip.
        /// </summary>
        /// <value> The tool tip. </value>
        public string ToolTip { get; set; }

        /// <summary>
        /// Gets or sets the unit of the axis.
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use superscript exponential format. This format will convert 1.5E+03 to 1.5·10^{3} and render the superscript properly If StringFormat is null, 1.0E+03 will be converted to 10^{3}
        /// </summary>
        public bool UseSuperExponentialFormat { get; set; }

        /// <summary>
        /// Gets or sets the position tier max shift.
        /// </summary>
        /// <value> The position tier max shift. </value>
        internal double PositionTierMaxShift { get; set; }

        /// <summary>
        /// Gets or sets the position tier min shift.
        /// </summary>
        /// <value> The position tier min shift. </value>
        internal double PositionTierMinShift { get; set; }

        /// <summary>
        /// Gets or sets the size of the position tier.
        /// </summary>
        /// <value> The size of the position tier. </value>
        internal double PositionTierSize { get; set; }

        /// <summary>
        /// Gets the actual color of the title.
        /// </summary>
        /// <value> The actual color of the title. </value>
        protected internal OxyColor ActualTitleColor
        {
            get
            {
                return this.TitleColor ?? this.PlotModel.TextColor;
            }
        }

        /// <summary>
        /// Gets the actual title font.
        /// </summary>
        protected internal string ActualTitleFont
        {
            get
            {
                return this.TitleFont ?? this.PlotModel.DefaultFont;
            }
        }

        /// <summary>
        /// Gets the actual size of the title font.
        /// </summary>
        /// <value> The actual size of the title font. </value>
        protected internal double ActualTitleFontSize
        {
            get
            {
                return !double.IsNaN(this.TitleFontSize) ? this.TitleFontSize : this.ActualFontSize;
            }
        }

        /// <summary>
        /// Gets the actual title font weight.
        /// </summary>
        protected internal double ActualTitleFontWeight
        {
            get
            {
                return !double.IsNaN(this.TitleFontWeight) ? this.TitleFontWeight : this.ActualFontWeight;
            }
        }

        /// <summary>
        /// Gets or sets the current view's maximum. This value is used when the user zooms or pans.
        /// </summary>
        /// <value> The view maximum. </value>
        public double ViewMaximum { get; protected set; }

        /// <summary>
        /// Gets or sets the current view's minimum. This value is used when the user zooms or pans.
        /// </summary>
        /// <value> The view minimum. </value>
        public double ViewMinimum { get; protected set; }

        /// <summary>
        /// Transforms the specified point to screen coordinates.
        /// </summary>
        /// <param name="p">
        /// The point.
        /// </param>
        /// <param name="xaxis">
        /// The x axis.
        /// </param>
        /// <param name="yaxis">
        /// The y axis.
        /// </param>
        /// <returns>
        /// The transformed point.
        /// </returns>
        public static ScreenPoint Transform(DataPoint p, Axis xaxis, Axis yaxis)
        {
            return xaxis.Transform(p.x, p.y, yaxis);
        }

        /// <summary>
        /// Transform the specified screen point to data coordinates.
        /// </summary>
        /// <param name="p">The point.</param>
        /// <param name="xaxis">The x axis.</param>
        /// <param name="yaxis">The y axis.</param>
        /// <returns>The data point.</returns>
        public static DataPoint InverseTransform(ScreenPoint p, Axis xaxis, Axis yaxis)
        {
            return xaxis.InverseTransform(p.x, p.y, yaxis);
        }

        /// <summary>
        /// Transforms the specified point to screen coordinates.
        /// </summary>
        /// <param name="p">
        /// The point.
        /// </param>
        /// <param name="xaxis">
        /// The x axis.
        /// </param>
        /// <param name="yaxis">
        /// The y axis.
        /// </param>
        /// <returns>
        /// The transformed point.
        /// </returns>
        public static ScreenPoint Transform(IDataPoint p, Axis xaxis, Axis yaxis)
        {
            return xaxis.Transform(p.X, p.Y, yaxis);
        }

        /// <summary>
        /// Coerces the actual maximum and minimum values.
        /// </summary>
        public virtual void CoerceActualMaxMin()
        {
            // Coerce actual minimum
            if (double.IsNaN(this.ActualMinimum) || double.IsInfinity(this.ActualMinimum))
            {
                this.ActualMinimum = 0;
            }

            // Coerce actual maximum
            if (double.IsNaN(this.ActualMaximum) || double.IsInfinity(this.ActualMaximum))
            {
                this.ActualMaximum = 100;
            }

            if (this.ActualMaximum <= this.ActualMinimum)
            {
                this.ActualMaximum = this.ActualMinimum + 100;
            }

            // Coerce the minimum range
            double range = this.ActualMaximum - this.ActualMinimum;
            if (range < this.MinimumRange)
            {
                double avg = (this.ActualMaximum + this.ActualMinimum) * 0.5;
                this.ActualMinimum = avg - (this.MinimumRange * 0.5);
                this.ActualMaximum = avg + (this.MinimumRange * 0.5);
            }

            if (this.AbsoluteMaximum <= this.AbsoluteMinimum)
            {
                throw new InvalidOperationException("AbsoluteMaximum should be larger than AbsoluteMinimum.");
            }
        }

        /// <summary>
        /// Formats the value to be used on the axis.
        /// </summary>
        /// <param name="x">
        /// The value.
        /// </param>
        /// <returns>
        /// The formatted value.
        /// </returns>
        public virtual string FormatValue(double x)
        {
            // The "SuperExponentialFormat" renders the number with superscript exponents. E.g. 10^2
            if (this.UseSuperExponentialFormat)
            {
                // if (x == 1 || x == 10 || x == -1 || x == -10)
                // return x.ToString();
                double exp = Exponent(x);
                double mantissa = Mantissa(x);
                string fmt;
                if (this.StringFormat == null)
                {
                    fmt = Math.Abs(mantissa - 1.0) < 1e-6 ? "10^{{{1:0}}}" : "{0}·10^{{{1:0}}}";
                }
                else
                {
                    fmt = "{0:" + this.StringFormat + "}·10^{{{1:0}}}";
                }

                return string.Format(this.ActualCulture, fmt, mantissa, exp);
            }

            string format = this.ActualStringFormat ?? this.StringFormat ?? string.Empty;
            return x.ToString(format, this.ActualCulture);
        }

        /// <summary>
        /// Formats the value to be used by the tracker.
        /// </summary>
        /// <param name="x">
        /// The value.
        /// </param>
        /// <returns>
        /// The formatted value.
        /// </returns>
        public virtual string FormatValueForTracker(double x)
        {
            return x.ToString(this.ActualCulture);
        }

        /// <summary>
        /// Gets the coordinates used to draw ticks and tick labels (numbers or category names).
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
        public virtual void GetTickValues(
            out IList<double> majorLabelValues, out IList<double> majorTickValues, out IList<double> minorTickValues)
        {
            minorTickValues = CreateTickValues(this.ActualMinimum, this.ActualMaximum, this.ActualMinorStep);
            majorTickValues = CreateTickValues(this.ActualMinimum, this.ActualMaximum, this.ActualMajorStep);
            majorLabelValues = majorTickValues;
        }

        /// <summary>
        /// Gets the value from an axis coordinate, converts from double to the correct data type if necessary. e.g. DateTimeAxis returns the DateTime and CategoryAxis returns category strings.
        /// </summary>
        /// <param name="x">
        /// The coordinate.
        /// </param>
        /// <returns>
        /// The value.
        /// </returns>
        public virtual object GetValue(double x)
        {
            return x;
        }

        /// <summary>
        /// Inverse transform the specified screen point.
        /// </summary>
        /// <param name="x">
        /// The x coordinate.
        /// </param>
        /// <param name="y">
        /// The y coordinate.
        /// </param>
        /// <param name="yaxis">
        /// The y-axis.
        /// </param>
        /// <returns>
        /// The data point.
        /// </returns>
        public virtual DataPoint InverseTransform(double x, double y, Axis yaxis)
        {
            return new DataPoint(this.InverseTransform(x), yaxis != null ? yaxis.InverseTransform(y) : 0);
        }

        /// <summary>
        /// Inverse transform the specified screen coordinate. This method can only be used with non-polar coordinate systems.
        /// </summary>
        /// <param name="sx">
        /// The screen coordinate.
        /// </param>
        /// <returns>
        /// The value.
        /// </returns>
        public virtual double InverseTransform(double sx)
        {
            return this.PostInverseTransform((sx / this.scale) + this.offset);
        }

        /// <summary>
        /// Determines whether this axis is horizontal.
        /// </summary>
        /// <returns>
        /// <c>true</c> if this axis is horizontal; otherwise, <c>false</c> .
        /// </returns>
        public bool IsHorizontal()
        {
            return this.position == AxisPosition.Top || this.position == AxisPosition.Bottom;
        }

        /// <summary>
        /// Determines whether the specified value is valid.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified value is valid; otherwise, <c>false</c> .
        /// </returns>
        public virtual bool IsValidValue(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value < this.FilterMaxValue
                   && value > this.FilterMinValue && (this.FilterFunction == null || this.FilterFunction(value));
        }

        /// <summary>
        /// Determines whether this axis is vertical.
        /// </summary>
        /// <returns>
        /// <c>true</c> if this axis is vertical; otherwise, <c>false</c> .
        /// </returns>
        public bool IsVertical()
        {
            return this.position == AxisPosition.Left || this.position == AxisPosition.Right;
        }

        /// <summary>
        /// Determines whether the axis is used for X/Y values.
        /// </summary>
        /// <returns>
        /// <c>true</c> if it is an XY axis; otherwise, <c>false</c> .
        /// </returns>
        public abstract bool IsXyAxis();

        /// <summary>
        /// Measures the size of the axis (maximum axis label width/height).
        /// </summary>
        /// <param name="rc">
        /// The render context.
        /// </param>
        /// <returns>
        /// The size of the axis.
        /// </returns>
        public virtual OxySize Measure(IRenderContext rc)
        {
            IList<double> majorTickValues;
            IList<double> minorTickValues;
            IList<double> majorLabelValues;

            this.GetTickValues(out majorLabelValues, out majorTickValues, out minorTickValues);

            var maximumTextSize = new OxySize();
            foreach (double v in majorLabelValues)
            {
                string s = this.FormatValue(v);
                var size = rc.MeasureText(s, this.ActualFont, this.ActualFontSize, this.ActualFontWeight);
                if (size.Width > maximumTextSize.Width)
                {
                    maximumTextSize.Width = size.Width;
                }

                if (size.Height > maximumTextSize.Height)
                {
                    maximumTextSize.Height = size.Height;
                }
            }

            var labelTextSize = rc.MeasureText(
                this.ActualTitle, this.ActualFont, this.ActualFontSize, this.ActualFontWeight);

            double width = 0;
            double height = 0;

            if (this.IsHorizontal())
            {
                switch (this.TickStyle)
                {
                    case TickStyle.Outside:
                        height += this.MajorTickSize;
                        break;
                    case TickStyle.Crossing:
                        height += this.MajorTickSize * 0.75;
                        break;
                }

                height += this.AxisTickToLabelDistance;
                height += maximumTextSize.Height;
                if (labelTextSize.Height > 0)
                {
                    height += this.AxisTitleDistance;
                    height += labelTextSize.Height;
                }
            }
            else
            {
                switch (this.TickStyle)
                {
                    case TickStyle.Outside:
                        width += this.MajorTickSize;
                        break;
                    case TickStyle.Crossing:
                        width += this.MajorTickSize * 0.75;
                        break;
                }

                width += this.AxisTickToLabelDistance;
                width += maximumTextSize.Width;
                if (labelTextSize.Height > 0)
                {
                    width += this.AxisTitleDistance;
                    width += labelTextSize.Height;
                }
            }

            return new OxySize(width, height);
        }

        /// <summary>
        /// Pans the specified axis.
        /// </summary>
        /// <param name="ppt">
        /// The previous point (screen coordinates).
        /// </param>
        /// <param name="cpt">
        /// The current point (screen coordinates).
        /// </param>
        public virtual void Pan(ScreenPoint ppt, ScreenPoint cpt)
        {
            if (!this.IsPanEnabled)
            {
                return;
            }

            bool isHorizontal = this.IsHorizontal();

            double dsx = isHorizontal ? cpt.X - ppt.X : cpt.Y - ppt.Y;
            this.Pan(dsx);
        }

        /// <summary>
        /// Pans the specified axis.
        /// </summary>
        /// <param name="delta">
        /// The delta.
        /// </param>
        public virtual void Pan(double delta)
        {
            if (!this.IsPanEnabled)
            {
                return;
            }

            double dx = delta / this.Scale;

            double newMinimum = this.ActualMinimum - dx;
            double newMaximum = this.ActualMaximum - dx;
            if (newMinimum < this.AbsoluteMinimum)
            {
                newMinimum = this.AbsoluteMinimum;
                newMaximum = newMinimum + this.ActualMaximum - this.ActualMinimum;
            }

            if (newMaximum > this.AbsoluteMaximum)
            {
                newMaximum = this.AbsoluteMaximum;
                newMinimum = newMaximum - (this.ActualMaximum - this.ActualMinimum);
            }

            this.ViewMinimum = newMinimum;
            this.ViewMaximum = newMaximum;
            this.UpdateActualMaxMin();

            this.OnAxisChanged(new AxisChangedEventArgs(AxisChangeTypes.Pan));
        }

        /// <summary>
        /// Renders the axis on the specified render context.
        /// </summary>
        /// <param name="rc">The render context.</param>
        /// <param name="model">The model.</param>
        /// <param name="axisLayer">The rendering order.</param>
        /// <param name="pass">The pass.</param>
        public virtual void Render(IRenderContext rc, PlotModel model, AxisLayer axisLayer, int pass)
        {
            var r = new HorizontalAndVerticalAxisRenderer(rc, model);
            r.Render(this, pass);
        }

        /// <summary>
        /// Resets the user's modification (zooming/panning) to minimum and maximum of this axis.
        /// </summary>
        public virtual void Reset()
        {
            this.ViewMinimum = double.NaN;
            this.ViewMaximum = double.NaN;
            this.UpdateActualMaxMin();
            this.OnAxisChanged(new AxisChangedEventArgs(AxisChangeTypes.Reset));
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}({1}, {2}, {3}, {4})",
                this.GetType().Name,
                this.Position,
                this.ActualMinimum,
                this.ActualMaximum,
                this.ActualMajorStep);
        }

        /// <summary>
        /// Transforms the specified point to screen coordinates.
        /// </summary>
        /// <param name="x">
        /// The x value (for the current axis).
        /// </param>
        /// <param name="y">
        /// The y value.
        /// </param>
        /// <param name="yaxis">
        /// The y axis.
        /// </param>
        /// <returns>
        /// The transformed point.
        /// </returns>
        public virtual ScreenPoint Transform(double x, double y, Axis yaxis)
        {
            if (yaxis == null)
            {
                throw new NullReferenceException("Y axis should not be null when transforming.");
            }

            return new ScreenPoint(this.Transform(x), yaxis.Transform(y));
        }

        /// <summary>
        /// Transforms the specified coordinate to screen coordinates. This method can only be used with non-polar coordinate systems.
        /// </summary>
        /// <param name="x">
        /// The value.
        /// </param>
        /// <returns>
        /// The transformed value (screen coordinate).
        /// </returns>
        public virtual double Transform(double x)
        {
            return (x - this.offset) * this.scale;

            // return (this.PreTransform(x) - this.Offset) * this.Scale;
        }

        /// <summary>
        /// Zoom to the specified scale.
        /// </summary>
        /// <param name="newScale">
        /// The new scale.
        /// </param>
        public virtual void Zoom(double newScale)
        {
            double sx1 = this.Transform(this.ActualMaximum);
            double sx0 = this.Transform(this.ActualMinimum);

            double sgn = Math.Sign(this.scale);
            double mid = (this.ActualMaximum + this.ActualMinimum) / 2;

            double dx = (this.offset - mid) * this.scale;
            this.scale = sgn * newScale;
            this.offset = (dx / this.scale) + mid;

            double newMaximum = this.InverseTransform(sx1);
            double newMinimum = this.InverseTransform(sx0);

            if (newMinimum < this.AbsoluteMinimum && newMaximum > this.AbsoluteMaximum)
            {
                newMinimum = this.AbsoluteMinimum;
                newMaximum = this.AbsoluteMaximum;
            }
            else
            {
                if (newMinimum < this.AbsoluteMinimum)
                {
                    double d = newMaximum - newMinimum;
                    newMinimum = this.AbsoluteMinimum;
                    newMaximum = this.AbsoluteMinimum + d;
                    if (newMaximum > this.AbsoluteMaximum)
                    {
                        newMaximum = this.AbsoluteMaximum;
                    }
                }
                else if (newMaximum > this.AbsoluteMaximum)
                {
                    double d = newMaximum - newMinimum;
                    newMaximum = this.AbsoluteMaximum;
                    newMinimum = this.AbsoluteMaximum - d;
                    if (newMinimum < this.AbsoluteMinimum)
                    {
                        newMinimum = this.AbsoluteMinimum;
                    }
                }
            }

            this.ViewMaximum = newMaximum;
            this.ViewMinimum = newMinimum;
            this.UpdateActualMaxMin();
        }

        /// <summary>
        /// Zooms the axis to the range [x0,x1].
        /// </summary>
        /// <param name="x0">
        /// The new minimum.
        /// </param>
        /// <param name="x1">
        /// The new maximum.
        /// </param>
        public virtual void Zoom(double x0, double x1)
        {
            if (!this.IsZoomEnabled)
            {
                return;
            }

            double newMinimum = Math.Max(Math.Min(x0, x1), this.AbsoluteMinimum);
            double newMaximum = Math.Min(Math.Max(x0, x1), this.AbsoluteMaximum);

            this.ViewMinimum = newMinimum;
            this.ViewMaximum = newMaximum;
            this.UpdateActualMaxMin();

            this.OnAxisChanged(new AxisChangedEventArgs(AxisChangeTypes.Zoom));
        }

        /// <summary>
        /// Zooms the axis at the specified coordinate.
        /// </summary>
        /// <param name="factor">
        /// The zoom factor.
        /// </param>
        /// <param name="x">
        /// The coordinate to zoom at.
        /// </param>
        public virtual void ZoomAt(double factor, double x)
        {
            if (!this.IsZoomEnabled)
            {
                return;
            }

            double dx0 = (this.ActualMinimum - x) * this.scale;
            double dx1 = (this.ActualMaximum - x) * this.scale;
            this.scale *= factor;

            double newMinimum = Math.Max((dx0 / this.scale) + x, this.AbsoluteMinimum);
            double newMaximum = Math.Min((dx1 / this.scale) + x, this.AbsoluteMaximum);

            this.ViewMinimum = newMinimum;
            this.ViewMaximum = newMaximum;
            this.UpdateActualMaxMin();

            this.OnAxisChanged(new AxisChangedEventArgs(AxisChangeTypes.Zoom));
        }

        /// <summary>
        /// Modifies the data range of the axis [DataMinimum,DataMaximum] to includes the specified value.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        public virtual void Include(double value)
        {
            if (!this.IsValidValue(value))
            {
                return;
            }

            this.DataMinimum = double.IsNaN(this.DataMinimum) ? value : Math.Min(this.DataMinimum, value);
            this.DataMaximum = double.IsNaN(this.DataMaximum) ? value : Math.Max(this.DataMaximum, value);
        }

        /// <summary>
        /// Applies a transformation after the inverse transform of the value. This is used in logarithmic axis.
        /// </summary>
        /// <param name="x">
        /// The value to transform.
        /// </param>
        /// <returns>
        /// The transformed value.
        /// </returns>
        internal virtual double PostInverseTransform(double x)
        {
            return x;
        }

        /// <summary>
        /// Applies a transformation before the transform the value. This is used in logarithmic axis.
        /// </summary>
        /// <param name="x">
        /// The value to transform.
        /// </param>
        /// <returns>
        /// The transformed value.
        /// </returns>
        internal virtual double PreTransform(double x)
        {
            return x;
        }

        /// <summary>
        /// Resets the data maximum and minimum.
        /// </summary>
        internal virtual void ResetDataMaxMin()
        {
            this.DataMaximum = this.DataMinimum = double.NaN;
        }

        /// <summary>
        /// Updates the actual maximum and minimum values. If the user has zoomed/panned the axis, the internal ViewMaximum/ViewMinimum values will be used. If Maximum or Minimum have been set, these values will be used. Otherwise the maximum and minimum values of the series will be used, including the 'padding'.
        /// </summary>
        internal virtual void UpdateActualMaxMin()
        {
            // Use the minimum/maximum of the data as default
            this.ActualMaximum = this.DataMaximum;
            this.ActualMinimum = this.DataMinimum;

            double range = this.ActualMaximum - this.ActualMinimum;
            double zeroRange = this.ActualMaximum > 0 ? this.ActualMaximum : 1;

            if (!double.IsNaN(this.ViewMaximum))
            {
                // Override the ActualMaximum by the ViewMaximum value (from zoom/pan)
                this.ActualMaximum = this.ViewMaximum;
            }
            else if (!double.IsNaN(this.Maximum))
            {
                // Override the ActualMaximum by the Maximum value
                this.ActualMaximum = this.Maximum;
            }
            else
            {
                if (range < double.Epsilon)
                {
                    this.ActualMaximum += zeroRange * 0.5;
                }

                if (!double.IsNaN(this.ActualMinimum) && !double.IsNaN(this.ActualMaximum))
                {
                    double x1 = this.PreTransform(this.ActualMaximum);
                    double x0 = this.PreTransform(this.ActualMinimum);
                    double dx = this.MaximumPadding * (x1 - x0);
                    this.ActualMaximum = this.PostInverseTransform(x1 + dx);
                }
            }

            if (!double.IsNaN(this.ViewMinimum))
            {
                this.ActualMinimum = this.ViewMinimum;
            }
            else if (!double.IsNaN(this.Minimum))
            {
                this.ActualMinimum = this.Minimum;
            }
            else
            {
                if (range < double.Epsilon)
                {
                    this.ActualMinimum -= zeroRange * 0.5;
                }

                if (!double.IsNaN(this.ActualMaximum) && !double.IsNaN(this.ActualMaximum))
                {
                    double x1 = this.PreTransform(this.ActualMaximum);
                    double x0 = this.PreTransform(this.ActualMinimum);
                    double dx = this.MinimumPadding * (x1 - x0);
                    this.ActualMinimum = this.PostInverseTransform(x0 - dx);
                }
            }

            this.CoerceActualMaxMin();
        }

        /// <summary>
        /// Updates the axis with information from the plot series.
        /// </summary>
        /// <param name="series">
        /// The series collection.
        /// </param>
        /// <remarks>
        /// This is used by the category axis that need to know the number of series using the axis.
        /// </remarks>
        internal virtual void UpdateFromSeries(IEnumerable<Series> series)
        {
        }

        /// <summary>
        /// Updates the actual minor and major step intervals.
        /// </summary>
        /// <param name="plotArea">
        /// The plot area rectangle.
        /// </param>
        internal virtual void UpdateIntervals(OxyRect plotArea)
        {
            double labelSize = this.IntervalLength;
            double length = this.IsHorizontal() ? plotArea.Width : plotArea.Height;
            length *= Math.Abs(this.EndPosition - this.StartPosition);

            this.ActualMajorStep = !double.IsNaN(this.MajorStep)
                                       ? this.MajorStep
                                       : this.CalculateActualInterval(length, labelSize);

            this.ActualMinorStep = !double.IsNaN(this.MinorStep)
                                       ? this.MinorStep
                                       : this.CalculateMinorInterval(this.ActualMajorStep);

            if (double.IsNaN(this.ActualMinorStep))
            {
                this.ActualMinorStep = 2;
            }

            if (double.IsNaN(this.ActualMajorStep))
            {
                this.ActualMajorStep = 10;
            }

            this.ActualStringFormat = this.StringFormat;

            // if (ActualStringFormat==null)
            // {
            // if (ActualMaximum > 1e6 || ActualMinimum < 1e-6)
            // ActualStringFormat = "#.#e-0";
            // }
        }

        /// <summary>
        /// Updates the scale and offset properties of the transform from the specified boundary rectangle.
        /// </summary>
        /// <param name="bounds">
        /// The bounds.
        /// </param>
        internal virtual void UpdateTransform(OxyRect bounds)
        {
            double x0 = bounds.Left;
            double x1 = bounds.Right;
            double y0 = bounds.Bottom;
            double y1 = bounds.Top;

            this.ScreenMin = new ScreenPoint(x0, y1);
            this.ScreenMax = new ScreenPoint(x1, y0);

            // this.MidPoint = new ScreenPoint((x0 + x1) / 2, (y0 + y1) / 2);

            // if (this.Position == AxisPosition.Angle)
            // {
            // this.scale = 2 * Math.PI / (this.ActualMaximum - this.ActualMinimum);
            // this.Offset = this.ActualMinimum;
            // return;
            // }

            // if (this.Position == AxisPosition.Magnitude)
            // {
            // this.ActualMinimum = 0;
            // double r = Math.Min(Math.Abs(x1 - x0), Math.Abs(y1 - y0));
            // this.scale = 0.5 * r / (this.ActualMaximum - this.ActualMinimum);
            // this.Offset = this.ActualMinimum;
            // return;
            // }
            double a0 = this.IsHorizontal() ? x0 : y0;
            double a1 = this.IsHorizontal() ? x1 : y1;

            double dx = a1 - a0;
            a1 = a0 + (this.EndPosition * dx);
            a0 = a0 + (this.StartPosition * dx);
            this.ScreenMin = new ScreenPoint(a0, a1);
            this.ScreenMax = new ScreenPoint(a1, a0);

            if (this.ActualMaximum - this.ActualMinimum < double.Epsilon)
            {
                this.ActualMaximum = this.ActualMinimum + 1;
            }

            double max = this.PreTransform(this.ActualMaximum);
            double min = this.PreTransform(this.ActualMinimum);

            double da = a0 - a1;
            if (Math.Abs(da) > double.Epsilon)
            {
                this.offset = (a0 / da * max) - (a1 / da * min);
            }
            else
            {
                this.offset = 0;
            }

            double range = max - min;
            if (Math.Abs(range) > double.Epsilon)
            {
                this.scale = (a1 - a0) / range;
            }
            else
            {
                this.scale = 1;
            }
        }

        /// <summary>
        /// Creates tick values at the specified interval.
        /// </summary>
        /// <param name="min">
        /// The minimum coordinate.
        /// </param>
        /// <param name="max">
        /// The maximum coordinate.
        /// </param>
        /// <param name="step">
        /// The interval.
        /// </param>
        /// <returns>
        /// A list of tick values.
        /// </returns>
        protected static IList<double> CreateTickValues(double min, double max, double step)
        {
            if (max <= min)
            {
                throw new ArgumentException("Axis: Maximum should be larger than minimum.", "max");
            }

            if (step <= 0)
            {
                throw new ArgumentException("Axis: Step cannot be zero or negative.", "step");
            }

            double x0 = Math.Round(min / step) * step;
            int n = Math.Max((int)((max - min) / step), 1);
            var values = new List<double>(n);

            // Limit the maximum number of iterations (in case something is wrong with the step size)
            int i = 0;
            const int MaxIterations = 1000;
            double x = x0;
            double eps = step * 1e-3;

            while (x <= max + eps && i < MaxIterations)
            {
                x = x0 + (i * step);
                i++;
                if (x >= min - eps && x <= max + eps)
                {
                    x = x.RemoveNoise();
                    values.Add(x);
                }
            }

            return values;
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
        protected virtual double CalculateActualInterval(double availableSize, double maxIntervalSize)
        {
            return this.CalculateActualInterval(availableSize, maxIntervalSize, this.ActualMaximum - this.ActualMinimum);
        }

        // alternative algorithm not in use
        /*        private double CalculateActualIntervalOldAlgorithm(double availableSize, double maxIntervalSize)
                {
                    const int minimumTags = 5;
                    const int maximumTags = 20;
                    var numberOfTags = (int) (availableSize/maxIntervalSize);
                    double range = ActualMaximum - ActualMinimum;
                    double interval = range/numberOfTags;
                    const int k1 = 10;
                    interval = Math.Log10(interval/k1);
                    interval = Math.Ceiling(interval);
                    interval = Math.Pow(10, interval)*k1;

                    if (range/interval > maximumTags) interval *= 5;
                    if (range/interval < minimumTags) interval *= 0.5;

                    if (interval <= 0) interval = 1;
                    return interval;
                }*/

        // ===
        // the following algorithm is from
        // System.Windows.Controls.DataVisualization.Charting.LinearAxis.cs

        // (c) Copyright Microsoft Corporation.
        // This source is subject to the Microsoft Public License (MIT).
        // Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
        // All other rights reserved.

        /// <summary>
        /// Returns the actual interval to use to determine which values are displayed in the axis.
        /// </summary>
        /// <param name="availableSize">
        /// The available size.
        /// </param>
        /// <param name="maxIntervalSize">
        /// The maximum interval size.
        /// </param>
        /// <param name="range">
        /// The range.
        /// </param>
        /// <returns>
        /// Actual interval to use to determine which values are displayed in the axis.
        /// </returns>
        protected double CalculateActualInterval(double availableSize, double maxIntervalSize, double range)
        {
            if (availableSize <= 0)
            {
                return maxIntervalSize;
            }

            Func<double, double> exponent = x => Math.Ceiling(Math.Log(x, 10));
            Func<double, double> mantissa = x => x / Math.Pow(10, exponent(x) - 1);

            // reduce intervals for horizontal axis.
            // double maxIntervals = Orientation == AxisOrientation.x ? MaximumAxisIntervalsPer200Pixels * 0.8 : MaximumAxisIntervalsPer200Pixels;
            // real maximum interval count
            double maxIntervalCount = availableSize / maxIntervalSize;

            range = Math.Abs(range);
            double interval = Math.Pow(10, exponent(range));
            double tempInterval = interval;

            // decrease interval until interval count becomes less than maxIntervalCount
            while (true)
            {
                var m = (int)mantissa(tempInterval);
                if (m == 5)
                {
                    // reduce 5 to 2
                    tempInterval = (tempInterval / 2.5).RemoveNoiseFromDoubleMath();
                }
                else if (m == 2 || m == 1 || m == 10)
                {
                    // reduce 2 to 1, 10 to 5, 1 to 0.5
                    tempInterval = (tempInterval / 2.0).RemoveNoiseFromDoubleMath();
                }
                else
                {
                    tempInterval = (tempInterval / 2.0).RemoveNoiseFromDoubleMath();
                }

                if (range / tempInterval > maxIntervalCount)
                {
                    break;
                }

                if (double.IsNaN(tempInterval) || double.IsInfinity(tempInterval))
                {
                    break;
                }

                interval = tempInterval;
            }

            return interval;
        }

        /// <summary>
        /// The calculate minor interval.
        /// </summary>
        /// <param name="majorInterval">
        /// The major interval.
        /// </param>
        /// <returns>
        /// The minor interval.
        /// </returns>
        protected double CalculateMinorInterval(double majorInterval)
        {
            // if major interval is 100, the minor interval will be 20.
            return majorInterval / 5;

            // The following obsolete code divided major intervals into 4 minor intervals, unless the major interval's mantissa was 5.
            // e.g. Major interval 100 => minor interval 25.

            // Func<double, double> exponent = x => Math.Ceiling(Math.Log(x, 10));
            // Func<double, double> mantissa = x => x / Math.Pow(10, exponent(x) - 1);
            // var m = (int)mantissa(majorInterval);
            // switch (m)
            // {
            // case 5:
            // return majorInterval / 5;
            // default:
            // return majorInterval / 4;
            // }
        }

        /// <summary>
        /// Raises the AxisChanged event.
        /// </summary>
        /// <param name="args">
        /// The <see cref="OxyPlot.Axes.AxisChangedEventArgs"/> instance containing the event data.
        /// </param>
        protected virtual void OnAxisChanged(AxisChangedEventArgs args)
        {
            this.UpdateActualMaxMin();

            var handler = this.AxisChanged;
            if (handler != null)
            {
                handler(this, args);
            }
        }
    }
}