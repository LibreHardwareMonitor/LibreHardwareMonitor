// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ErrorColumnSeries.cs" company="OxyPlot">
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
//   Represents a series for clustered or stacked column charts with an error value.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Series
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents a series for clustered or stacked column charts with an error value.
    /// </summary>
    public class ErrorColumnSeries : ColumnSeries
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorColumnSeries"/> class.
        /// </summary>
        public ErrorColumnSeries()
        {
            this.ErrorWidth = 0.4;
            this.ErrorStrokeThickness = 1;
            this.TrackerFormatString = "{0}, {1}: {2}, Error: {Error}";
        }

        /// <summary>
        /// Gets or sets the stroke thickness of the error line.
        /// </summary>
        /// <value>
        /// The stroke thickness of the error line.
        /// </value>
        public double ErrorStrokeThickness { get; set; }

        /// <summary>
        /// Gets or sets the width of the error end lines.
        /// </summary>
        /// <value>
        /// The width of the error end lines.
        /// </value>
        public double ErrorWidth { get; set; }

        /// <summary>
        /// Updates the maximum/minimum value on the value axis from the bar values.
        /// </summary>
        protected internal override void UpdateMaxMin()
        {
            base.UpdateMaxMin();

            //// Todo: refactor (lots of duplicate code here)
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
                    var items = this.ValidItems.Where(item => item.GetCategoryIndex(j++) == i).ToList();
                    var values = items.Select(item => item.Value).Concat(new[] { 0d }).ToList();
                    var minTemp = values.Where(v => v <= 0).Sum();
                    var maxTemp = values.Where(v => v >= 0).Sum() + ((ErrorColumnItem)items.Last()).Error;

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
                var valuesMin =
                    this.ValidItems.Select(item => item.Value - ((ErrorColumnItem)item).Error).Concat(new[] { 0d }).
                        ToList();
                var valuesMax =
                    this.ValidItems.Select(item => item.Value + ((ErrorColumnItem)item).Error).Concat(new[] { 0d }).
                        ToList();
                minValue = valuesMin.Min();
                maxValue = valuesMax.Max();
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
        protected override void RenderItem(
            IRenderContext rc,
            OxyRect clippingRect,
            double topValue,
            double categoryValue,
            double actualBarWidth,
            BarItemBase item,
            OxyRect rect)
        {
            base.RenderItem(rc, clippingRect, topValue, categoryValue, actualBarWidth, item, rect);

            var errorItem = item as ErrorColumnItem;
            if (errorItem == null)
            {
                return;
            }

            // Render the error
            var lowerValue = topValue - errorItem.Error;
            var upperValue = topValue + errorItem.Error;
            var left = 0.5 - this.ErrorWidth / 2;
            var right = 0.5 + this.ErrorWidth / 2;
            var leftValue = categoryValue + (left * actualBarWidth);
            var middleValue = categoryValue + (0.5 * actualBarWidth);
            var rightValue = categoryValue + (right * actualBarWidth);

            var lowerErrorPoint = this.Transform(middleValue, lowerValue);
            var upperErrorPoint = this.Transform(middleValue, upperValue);
            rc.DrawClippedLine(
                new List<ScreenPoint> { lowerErrorPoint, upperErrorPoint },
                clippingRect,
                0,
                this.StrokeColor,
                this.ErrorStrokeThickness,
                LineStyle.Solid,
                OxyPenLineJoin.Miter,
                true);

            if (this.ErrorWidth > 0)
            {
                var lowerLeftErrorPoint = this.Transform(leftValue, lowerValue);
                var lowerRightErrorPoint = this.Transform(rightValue, lowerValue);
                rc.DrawClippedLine(
                    new List<ScreenPoint> { lowerLeftErrorPoint, lowerRightErrorPoint },
                    clippingRect,
                    0,
                    this.StrokeColor,
                    this.ErrorStrokeThickness,
                    LineStyle.Solid,
                    OxyPenLineJoin.Miter,
                    true);

                var upperLeftErrorPoint = this.Transform(leftValue, upperValue);
                var upperRightErrorPoint = this.Transform(rightValue, upperValue);
                rc.DrawClippedLine(
                    new List<ScreenPoint> { upperLeftErrorPoint, upperRightErrorPoint },
                    clippingRect,
                    0,
                    this.StrokeColor,
                    this.ErrorStrokeThickness,
                    LineStyle.Solid,
                    OxyPenLineJoin.Miter,
                    true);
            }
        }

    }
}