// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalBarSeries.cs" company="OxyPlot">
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
//   Represents a series for bar charts defined by to/from values.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Series
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using OxyPlot.Axes;

    /// <summary>
    /// Represents a series for bar charts defined by to/from values.
    /// </summary>
    public class IntervalBarSeries : CategorizedSeries, IStackableSeries
    {
        /// <summary>
        /// The default fill color.
        /// </summary>
        private OxyColor defaultFillColor;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntervalBarSeries"/> class.
        /// </summary>
        public IntervalBarSeries()
        {
            this.Items = new List<IntervalBarItem>();

            this.StrokeColor = OxyColors.Black;
            this.StrokeThickness = 1;
            this.BarWidth = 1;

            this.TrackerFormatString = "{0}";
            this.LabelMargin = 4;

            this.LabelFormatString = "{2}"; // title

            // this.LabelFormatString = "{0}-{1}"; // Minimum-Maximum
        }

        /// <summary>
        /// Gets or sets the width of the bars (as a fraction of the available width). The default value is 0.5 (50%)
        /// </summary>
        /// <value>
        /// The width of the bars.
        /// </value>
        public double BarWidth { get; set; }

        /// <summary>
        /// Gets or sets the default color of the interior of the Maximum bars.
        /// </summary>
        /// <value>
        /// The color.
        /// </value>
        public OxyColor FillColor { get; set; }

        /// <summary>
        /// Gets the actual fill color.
        /// </summary>
        /// <value>The actual color.</value>
        public OxyColor ActualFillColor
        {
            get { return this.FillColor ?? this.defaultFillColor; }
        }

        /// <summary>
        /// Gets a value indicating whether IsStacked.
        /// </summary>
        public bool IsStacked
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the range bar items.
        /// </summary>
        public IList<IntervalBarItem> Items { get; private set; }

        /// <summary>
        /// Gets or sets the label color.
        /// </summary>
        public OxyColor LabelColor { get; set; }

        /// <summary>
        /// Gets or sets the label field.
        /// </summary>
        public string LabelField { get; set; }

        /// <summary>
        /// Gets or sets the format string for the maximum labels.
        /// </summary>
        public string LabelFormatString { get; set; }

        /// <summary>
        /// Gets or sets the label margins.
        /// </summary>
        public double LabelMargin { get; set; }

        /// <summary>
        /// Gets or sets the maximum value field.
        /// </summary>
        public string MaximumField { get; set; }

        /// <summary>
        /// Gets or sets the minimum value field.
        /// </summary>
        public string MinimumField { get; set; }

        /// <summary>
        /// Gets StackGroup.
        /// </summary>
        public string StackGroup
        {
            get
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets or sets the color of the border around the bars.
        /// </summary>
        /// <value>
        /// The color of the stroke.
        /// </value>
        public OxyColor StrokeColor { get; set; }

        /// <summary>
        /// Gets or sets the thickness of the bar border strokes.
        /// </summary>
        /// <value>
        /// The stroke thickness.
        /// </value>
        public double StrokeThickness { get; set; }

        /// <summary>
        /// Gets or sets the actual rectangles for the maximum bars.
        /// </summary>
        protected internal IList<OxyRect> ActualBarRectangles { get; set; }

        /// <summary>
        /// Gets or sets the valid items
        /// </summary>
        protected internal IList<IntervalBarItem> ValidItems { get; set; }

        /// <summary>
        /// Gets or sets the dictionary which stores the index-inversion for the valid items
        /// </summary>
        protected internal Dictionary<int, int> ValidItemsIndexInversion { get; set; }

        /// <summary>
        /// Gets the point in the dataset that is nearest the specified point.
        /// </summary>
        /// <param name="point">
        /// The point.
        /// </param>
        /// <param name="interpolate">
        /// The interpolate.
        /// </param>
        /// <returns>
        /// A TrackerHitResult for the current hit.
        /// </returns>
        public override TrackerHitResult GetNearestPoint(ScreenPoint point, bool interpolate)
        {
            for (int i = 0; i < this.ActualBarRectangles.Count; i++)
            {
                var r = this.ActualBarRectangles[i];
                if (r.Contains(point))
                {
                    var item = (IntervalBarItem)this.GetItem(this.ValidItemsIndexInversion[i]);
                    var categoryIndex = item.GetCategoryIndex(i);
                    double value = (this.ValidItems[i].Start + this.ValidItems[i].End) / 2;
                    var dp = new DataPoint(categoryIndex, value);
                    var text = StringHelper.Format(
                        this.ActualCulture,
                        this.TrackerFormatString,
                        item,
                        this.Items[i].Start,
                        this.Items[i].End,
                        this.Items[i].Title);
                    return new TrackerHitResult(this, dp, point, item, i, text);
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if the specified value is valid.
        /// </summary>
        /// <param name="v">
        /// The value.
        /// </param>
        /// <param name="yaxis">
        /// The y axis.
        /// </param>
        /// <returns>
        /// True if the value is valid.
        /// </returns>
        public virtual bool IsValidPoint(double v, Axis yaxis)
        {
            return !double.IsNaN(v) && !double.IsInfinity(v);
        }

        /// <summary>
        /// Renders the Series on the specified rendering context.
        /// </summary>
        /// <param name="rc">
        /// The rendering context.
        /// </param>
        /// <param name="model">
        /// The model.
        /// </param>
        public override void Render(IRenderContext rc, PlotModel model)
        {
            this.ActualBarRectangles = new List<OxyRect>();

            if (this.ValidItems.Count == 0)
            {
                return;
            }

            var clippingRect = this.GetClippingRect();
            var categoryAxis = this.GetCategoryAxis();

            var actualBarWidth = this.GetActualBarWidth();
            var stackIndex = categoryAxis.StackIndexMapping[this.StackGroup];

            for (var i = 0; i < this.ValidItems.Count; i++)
            {
                var item = this.ValidItems[i];

                var categoryIndex = item.GetCategoryIndex(i);
                double categoryValue = categoryAxis.GetCategoryValue(categoryIndex, stackIndex, actualBarWidth);

                var p0 = this.Transform(item.Start, categoryValue);
                var p1 = this.Transform(item.End, categoryValue + actualBarWidth);

                var rectangle = OxyRect.Create(p0.X, p0.Y, p1.X, p1.Y);

                this.ActualBarRectangles.Add(rectangle);

                rc.DrawClippedRectangleAsPolygon(
                    rectangle,
                    clippingRect,
                    this.GetSelectableFillColor(item.Color ?? this.ActualFillColor),
                    this.StrokeColor,
                    this.StrokeThickness);

                if (this.LabelFormatString != null)
                {
                    var s = StringHelper.Format(
                        this.ActualCulture, this.LabelFormatString, this.GetItem(i), item.Start, item.End, item.Title);

                    var pt = new ScreenPoint(
                        (rectangle.Left + rectangle.Right) / 2, (rectangle.Top + rectangle.Bottom) / 2);

                    rc.DrawClippedText(
                        clippingRect,
                        pt,
                        s,
                        this.ActualTextColor,
                        this.ActualFont,
                        this.ActualFontSize,
                        this.ActualFontWeight,
                        0,
                        HorizontalAlignment.Center,
                        VerticalAlignment.Middle);
                }
            }
        }

        /// <summary>
        /// Renders the legend symbol on the specified rendering context.
        /// </summary>
        /// <param name="rc">
        /// The rendering context.
        /// </param>
        /// <param name="legendBox">
        /// The legend rectangle.
        /// </param>
        public override void RenderLegend(IRenderContext rc, OxyRect legendBox)
        {
            double xmid = (legendBox.Left + legendBox.Right) / 2;
            double ymid = (legendBox.Top + legendBox.Bottom) / 2;
            double height = (legendBox.Bottom - legendBox.Top) * 0.8;
            double width = height;
            rc.DrawRectangleAsPolygon(
                new OxyRect(xmid - (0.5 * width), ymid - (0.5 * height), width, height),
                this.GetSelectableFillColor(this.ActualFillColor),
                this.StrokeColor,
                this.StrokeThickness);
        }

        /// <summary>
        /// Gets or sets the width/height of the columns/bars (as a fraction of the available space).
        /// </summary>
        /// <returns>
        /// The fractional width.
        /// </returns>
        /// <value>
        /// The width of the bars.
        /// </value>
        /// <remarks>
        /// The available space will be determined by the GapWidth of the CategoryAxis used by this series.
        /// </remarks>
        internal override double GetBarWidth()
        {
            return this.BarWidth;
        }

        /// <summary>
        /// Gets the items of this series.
        /// </summary>
        /// <returns>
        /// The items.
        /// </returns>
        protected internal override IList<CategorizedItem> GetItems()
        {
            return this.Items.Cast<CategorizedItem>().ToList();
        }

        /// <summary>
        /// Check if the data series is using the specified axis.
        /// </summary>
        /// <param name="axis">
        /// An axis which should be checked if used
        /// </param>
        /// <returns>
        /// True if the axis is in use.
        /// </returns>
        protected internal override bool IsUsing(Axis axis)
        {
            return this.XAxis == axis || this.YAxis == axis;
        }

        /// <summary>
        /// The set default values.
        /// </summary>
        /// <param name="model">
        /// The model.
        /// </param>
        protected internal override void SetDefaultValues(PlotModel model)
        {
            if (this.FillColor == null)
            {
                this.defaultFillColor = model.GetDefaultColor();
            }
        }

        /// <summary>
        /// Updates the axis maximum and minimum values.
        /// </summary>
        protected internal override void UpdateAxisMaxMin()
        {
            this.XAxis.Include(this.MinX);
            this.XAxis.Include(this.MaxX);
        }

        /// <summary>
        /// Updates the data.
        /// </summary>
        protected internal override void UpdateData()
        {
            if (this.ItemsSource != null)
            {
                this.Items.Clear();

                var filler = new ListFiller<IntervalBarItem>();
                filler.Add(this.MinimumField, (item, value) => item.Start = Convert.ToDouble(value));
                filler.Add(this.MaximumField, (item, value) => item.End = Convert.ToDouble(value));
                filler.FillT(this.Items, this.ItemsSource);
            }
        }

        /// <summary>
        /// Updates the maximum/minimum value on the value axis from the bar values.
        /// </summary>
        protected internal override void UpdateMaxMin()
        {
            base.UpdateMaxMin();

            if (this.ValidItems == null || this.ValidItems.Count == 0)
            {
                return;
            }

            double minValue = double.MaxValue;
            double maxValue = double.MinValue;

            foreach (var item in this.ValidItems)
            {
                minValue = Math.Min(minValue, item.Start);
                minValue = Math.Min(minValue, item.End);
                maxValue = Math.Max(maxValue, item.Start);
                maxValue = Math.Max(maxValue, item.End);
            }

            this.MinX = minValue;
            this.MaxX = maxValue;
        }

        /// <summary>
        /// Updates the valid items
        /// </summary>
        protected internal override void UpdateValidData()
        {
            this.ValidItems = new List<IntervalBarItem>();
            this.ValidItemsIndexInversion = new Dictionary<int, int>();
            var valueAxis = this.GetValueAxis();

            for (var i = 0; i < this.Items.Count; i++)
            {
                var item = this.Items[i];
                if (valueAxis.IsValidValue(item.Start) && valueAxis.IsValidValue(item.End))
                {
                    this.ValidItemsIndexInversion.Add(this.ValidItems.Count, i);
                    this.ValidItems.Add(item);
                }
            }
        }

        /// <summary>
        /// Gets the actual width/height of the items of this series.
        /// </summary>
        /// <returns>
        /// The width or height.
        /// </returns>
        /// <remarks>
        /// The actual width is also influenced by the GapWidth of the CategoryAxis used by this series.
        /// </remarks>
        protected override double GetActualBarWidth()
        {
            var categoryAxis = this.GetCategoryAxis();
            return this.BarWidth / (1 + categoryAxis.GapWidth) / categoryAxis.MaxWidth;
        }

        /// <summary>
        /// Gets the category axis.
        /// </summary>
        /// <returns>
        /// The category axis.
        /// </returns>
        protected override CategoryAxis GetCategoryAxis()
        {
            var categoryAxis = this.YAxis as CategoryAxis;
            if (categoryAxis == null)
            {
                throw new InvalidOperationException("No category axis defined.");
            }

            return categoryAxis;
        }

        /// <summary>
        /// Gets the item at the specified index.
        /// </summary>
        /// <param name="i">
        /// The index of the item.
        /// </param>
        /// <returns>
        /// The item of the index.
        /// </returns>
        protected override object GetItem(int i)
        {
            if (this.ItemsSource != null || this.Items == null || this.Items.Count == 0)
            {
                return base.GetItem(i);
            }

            return this.Items[i];
        }

        /// <summary>
        /// Gets the value axis.
        /// </summary>
        /// <returns>
        /// The value axis.
        /// </returns>
        private Axis GetValueAxis()
        {
            return this.XAxis;
        }

    }
}