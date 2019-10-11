// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BarSeriesBase.cs" company="OxyPlot">
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
//   Base class for BarSeries and ColumnSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Series
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using OxyPlot.Axes;

    /// <summary>
    /// Base class for BarSeries and ColumnSeries.
    /// </summary>
    public abstract class BarSeriesBase : CategorizedSeries, IStackableSeries
    {
        /// <summary>
        /// The default fill color.
        /// </summary>
        private OxyColor defaultFillColor;

        /// <summary>
        /// Initializes a new instance of the <see cref="BarSeriesBase"/> class.
        /// </summary>
        protected BarSeriesBase()
        {
            this.StrokeColor = OxyColors.Black;
            this.StrokeThickness = 0;
            this.TrackerFormatString = "{0}, {1}: {2}";
            this.LabelMargin = 2;
            this.StackGroup = string.Empty;
        }

        /// <summary>
        /// Gets or sets the base value.
        /// </summary>
        /// <value>
        /// The base value.
        /// </value>
        public double BaseValue { get; set; }

        /// <summary>
        /// Gets or sets the color field.
        /// </summary>
        public string ColorField { get; set; }

        /// <summary>
        /// Gets or sets the color of the interior of the bars.
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
        /// Gets or sets a value indicating whether this bar series is stacked.
        /// </summary>
        public bool IsStacked { get; set; }

        /// <summary>
        /// Gets or sets the label format string.
        /// </summary>
        /// <value>
        /// The label format string.
        /// </value>
        public string LabelFormatString { get; set; }

        /// <summary>
        /// Gets or sets the label margins.
        /// </summary>
        public double LabelMargin { get; set; }

        /// <summary>
        /// Gets or sets label placements.
        /// </summary>
        public LabelPlacement LabelPlacement { get; set; }

        /// <summary>
        /// Gets or sets the color of the interior of the bars when the value is negative.
        /// </summary>
        /// <value>
        /// The color.
        /// </value>
        public OxyColor NegativeFillColor { get; set; }

        /// <summary>
        /// Gets or sets the stack index indication to which stack the series belongs. Default is 0. Hence, all stacked series belong to the same stack.
        /// </summary>
        public string StackGroup { get; set; }

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
        /// Gets or sets the value field.
        /// </summary>
        public string ValueField { get; set; }

        /// <summary>
        /// Gets or sets the valid items
        /// </summary>
        protected internal IList<BarItemBase> ValidItems { get; set; }

        /// <summary>
        /// Gets or sets the dictionary which stores the index-inversion for the valid items
        /// </summary>
        protected internal Dictionary<int, int> ValidItemsIndexInversion { get; set; }

        /// <summary>
        /// Gets or sets the actual rectangles for the bars.
        /// </summary>
        protected IList<OxyRect> ActualBarRectangles { get; set; }

        /// <summary>
        /// Gets the nearest point.
        /// </summary>
        /// <param name="point">
        /// The point.
        /// </param>
        /// <param name="interpolate">
        /// interpolate if set to <c>true</c> .
        /// </param>
        /// <returns>
        /// A TrackerHitResult for the current hit.
        /// </returns>
        public override TrackerHitResult GetNearestPoint(ScreenPoint point, bool interpolate)
        {
            if (this.ActualBarRectangles == null || this.ValidItems == null)
            {
                return null;
            }

            var i = 0;
            foreach (var rectangle in this.ActualBarRectangles)
            {
                if (rectangle.Contains(point))
                {
                    var categoryIndex = this.ValidItems[i].GetCategoryIndex(i);

                    var dp = new DataPoint(categoryIndex, this.ValidItems[i].Value);
                    var item = this.GetItem(this.ValidItemsIndexInversion[i]);
                    var text = this.GetTrackerText(item, categoryIndex);
                    return new TrackerHitResult(this, dp, point, item, i, text);
                }

                i++;
            }

            return null;
        }

        /// <summary>
        /// Renders the series on the specified rendering context.
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

            if (this.ValidItems == null || this.ValidItems.Count == 0)
            {
                return;
            }

            var clippingRect = this.GetClippingRect();
            var categoryAxis = this.GetCategoryAxis();

            var actualBarWidth = this.GetActualBarWidth();
            var stackIndex = this.IsStacked ? categoryAxis.StackIndexMapping[this.StackGroup] : 0;
            for (var i = 0; i < this.ValidItems.Count; i++)
            {
                var item = this.ValidItems[i];
                var categoryIndex = this.ValidItems[i].GetCategoryIndex(i);

                var value = item.Value;

                // Get base- and topValue
                var baseValue = double.NaN;
                if (this.IsStacked)
                {
                    baseValue = value < 0
                                    ? categoryAxis.NegativeBaseValues[stackIndex, categoryIndex]
                                    : categoryAxis.PositiveBaseValues[stackIndex, categoryIndex];
                }

                if (double.IsNaN(baseValue))
                {
                    baseValue = this.BaseValue;
                }

                var topValue = this.IsStacked ? baseValue + value : value;

                // Calculate offset
                double categoryValue;
                if (this.IsStacked)
                {
                    categoryValue = categoryAxis.GetCategoryValue(categoryIndex, stackIndex, actualBarWidth);
                }
                else
                {
                    categoryValue = categoryIndex - 0.5 + categoryAxis.BarOffset[categoryIndex];
                }

                if (this.IsStacked)
                {
                    if (value < 0)
                    {
                        categoryAxis.NegativeBaseValues[stackIndex, categoryIndex] = topValue;
                    }
                    else
                    {
                        categoryAxis.PositiveBaseValues[stackIndex, categoryIndex] = topValue;
                    }
                }

                var rect = this.GetRectangle(baseValue, topValue, categoryValue, categoryValue + actualBarWidth);
                this.ActualBarRectangles.Add(rect);

                this.RenderItem(rc, clippingRect, topValue, categoryValue, actualBarWidth, item, rect);

                if (this.LabelFormatString != null)
                {
                    this.RenderLabel(rc, clippingRect, rect, value, i);
                }

                if (!this.IsStacked)
                {
                    categoryAxis.BarOffset[categoryIndex] += actualBarWidth;
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
            var xmid = (legendBox.Left + legendBox.Right) / 2;
            var ymid = (legendBox.Top + legendBox.Bottom) / 2;
            var height = (legendBox.Bottom - legendBox.Top) * 0.8;
            var width = height;
            rc.DrawRectangleAsPolygon(
                new OxyRect(xmid - (0.5 * width), ymid - (0.5 * height), width, height),
                this.GetSelectableColor(this.ActualFillColor),
                this.StrokeColor,
                this.StrokeThickness);
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
        /// The update axis max min.
        /// </summary>
        protected internal override void UpdateAxisMaxMin()
        {
            var valueAxis = this.GetValueAxis();
            if (valueAxis.IsVertical())
            {
                valueAxis.Include(this.MinY);
                valueAxis.Include(this.MaxY);
            }
            else
            {
                valueAxis.Include(this.MinX);
                valueAxis.Include(this.MaxX);
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

            var categoryAxis = this.GetCategoryAxis();

            double minValue = double.MaxValue, maxValue = double.MinValue;
            if (this.IsStacked)
            {
                var labels = this.GetCategoryAxis().Labels;
                for (var i = 0; i < labels.Count; i++)
                {
                    int j = 0;
                    var values =
                        this.ValidItems.Where(item => item.GetCategoryIndex(j++) == i).Select(item => item.Value).Concat(new[] { 0d }).ToList();
                    var minTemp = values.Where(v => v <= 0).Sum();
                    var maxTemp = values.Where(v => v >= 0).Sum();

                    int stackIndex = categoryAxis.StackIndexMapping[this.StackGroup];
                    var stackedMinValue = categoryAxis.MinValue[stackIndex, i];
                    if (!double.IsNaN(stackedMinValue))
                    {
                        minTemp += stackedMinValue;
                    }

                    categoryAxis.MinValue[stackIndex, i] = minTemp;

                    var stackedMaxValue = categoryAxis.MaxValue[stackIndex, i];
                    if (!double.IsNaN(stackedMaxValue))
                    {
                        maxTemp += stackedMaxValue;
                    }

                    categoryAxis.MaxValue[stackIndex, i] = maxTemp;

                    minValue = Math.Min(minValue, minTemp + this.BaseValue);
                    maxValue = Math.Max(maxValue, maxTemp + this.BaseValue);
                }
            }
            else
            {
                var values = this.ValidItems.Select(item => item.Value).Concat(new[] { 0d }).ToList();
                minValue = values.Min();
                maxValue = values.Max();
                if (this.BaseValue < minValue)
                {
                    minValue = this.BaseValue;
                }

                if (this.BaseValue > maxValue)
                {
                    maxValue = this.BaseValue;
                }
            }

            var valueAxis = this.GetValueAxis();
            if (valueAxis.IsVertical())
            {
                this.MinY = minValue;
                this.MaxY = maxValue;
            }
            else
            {
                this.MinX = minValue;
                this.MaxX = maxValue;
            }
        }

        /// <summary>
        /// Updates the valid items
        /// </summary>
        protected internal override void UpdateValidData()
        {
            this.ValidItems = new List<BarItemBase>();
            this.ValidItemsIndexInversion = new Dictionary<int, int>();
            var categories = this.GetCategoryAxis().Labels.Count;
            var valueAxis = this.GetValueAxis();

            int i = 0;
            foreach (var item in this.GetItems())
            {
                var barSeriesItem = item as BarItemBase;

                if (barSeriesItem != null && item.GetCategoryIndex(i) < categories
                    && valueAxis.IsValidValue(barSeriesItem.Value))
                {
                    this.ValidItemsIndexInversion.Add(this.ValidItems.Count, i);
                    this.ValidItems.Add(barSeriesItem);
                }

                i++;
            }
        }

        /// <summary>
        /// Gets the rectangle for the specified values.
        /// </summary>
        /// <param name="baseValue">
        /// The base value of the bar
        /// </param>
        /// <param name="topValue">
        /// The top value of the bar
        /// </param>
        /// <param name="beginValue">
        /// The begin value of the bar
        /// </param>
        /// <param name="endValue">
        /// The end value of the bar
        /// </param>
        /// <returns>
        /// The rectangle.
        /// </returns>
        protected abstract OxyRect GetRectangle(double baseValue, double topValue, double beginValue, double endValue);

        /// <summary>
        /// Gets the tracker text for the specified item.
        /// </summary>
        /// <param name="item">
        /// The item.
        /// </param>
        /// <param name="categoryIndex">
        /// Category index of the item.
        /// </param>
        /// <returns>
        /// The tracker text.
        /// </returns>
        protected virtual string GetTrackerText(object item, int categoryIndex)
        {
            var barItem = item as BarItemBase;
            if (barItem == null)
            {
                return null;
            }

            var categoryAxis = this.GetCategoryAxis();

            var text = StringHelper.Format(
                this.ActualCulture,
                this.TrackerFormatString,
                item,
                this.Title,
                categoryAxis.FormatValueForTracker(categoryIndex),
                barItem.Value);
            return text;
        }

        /// <summary>
        /// Gets the value axis.
        /// </summary>
        /// <returns>
        /// The value axis.
        /// </returns>
        protected abstract Axis GetValueAxis();

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
        protected virtual bool IsValidPoint(double v, Axis yaxis)
        {
            return !double.IsNaN(v) && !double.IsInfinity(v);
        }

        /// <summary>
        /// Renders the bar/column item.
        /// </summary>
        /// <param name="rc">
        /// The render context.
        /// </param>
        /// <param name="clippingRect">
        /// The clipping rectangle.
        /// </param>
        /// <param name="topValue">
        /// The end value of the bar.
        /// </param>
        /// <param name="categoryValue">
        /// The category value.
        /// </param>
        /// <param name="actualBarWidth">
        /// The actual width of the bar.
        /// </param>
        /// <param name="item">
        /// The item.
        /// </param>
        /// <param name="rect">
        /// The rectangle of the bar.
        /// </param>
        protected virtual void RenderItem(
            IRenderContext rc,
            OxyRect clippingRect,
            double topValue,
            double categoryValue,
            double actualBarWidth,
            BarItemBase item,
            OxyRect rect)
        {
            // Get the color of the item
            var actualFillColor = item.Color;
            if (actualFillColor == null)
            {
                actualFillColor = this.ActualFillColor;
                if (item.Value < 0 && this.NegativeFillColor != null)
                {
                    actualFillColor = this.NegativeFillColor;
                }
            }

            rc.DrawClippedRectangleAsPolygon(
                rect, clippingRect, this.GetSelectableFillColor(actualFillColor), this.StrokeColor, this.StrokeThickness);
        }

        /// <summary>
        /// Renders the item label.
        /// </summary>
        /// <param name="rc">
        /// The render context
        /// </param>
        /// <param name="clippingRect">
        /// The clipping rectangle
        /// </param>
        /// <param name="rect">
        /// The rectangle of the item.
        /// </param>
        /// <param name="value">
        /// The value of the label.
        /// </param>
        /// <param name="index">
        /// The index of the bar item.
        /// </param>
        protected abstract void RenderLabel(
            IRenderContext rc, OxyRect clippingRect, OxyRect rect, double value, int index);

    }
}