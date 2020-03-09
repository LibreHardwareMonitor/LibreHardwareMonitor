// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BarSeries.cs" company="OxyPlot">
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
//   Represents a series for clustered or stacked bar charts.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Series
{
    using System;

    using OxyPlot.Axes;

    /// <summary>
    /// Represents a series for clustered or stacked bar charts.
    /// </summary>
    public class BarSeries : BarSeriesBase<BarItem>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BarSeries"/> class.
        /// </summary>
        public BarSeries()
        {
            this.BarWidth = 1;
        }

        /// <summary>
        /// Gets or sets the width (height) of the bars.
        /// </summary>
        /// <value>
        /// The width of the bars.
        /// </value>
        public double BarWidth { get; set; }

        /// <summary>
        /// Gets or sets the width of the columns/bars (as a fraction of the available space).
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
            if (!(this.YAxis is CategoryAxis))
            {
                throw new Exception(
                    "A BarSeries requires a CategoryAxis on the y-axis. Use a ColumnSeries if you want vertical bars.");
            }

            return this.YAxis as CategoryAxis;
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
        protected override OxyRect GetRectangle(double baseValue, double topValue, double beginValue, double endValue)
        {
            return OxyRect.Create(this.Transform(baseValue, beginValue), this.Transform(topValue, endValue));
        }

        /// <summary>
        /// Gets the value axis.
        /// </summary>
        /// <returns>
        /// The value axis.
        /// </returns>
        protected override Axis GetValueAxis()
        {
            return this.XAxis;
        }

        /// <summary>
        /// Draws the label.
        /// </summary>
        /// <param name="rc">
        /// The render context.
        /// </param>
        /// <param name="clippingRect">
        /// The clipping rect.
        /// </param>
        /// <param name="rect">
        /// The rect.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <param name="i">
        /// The i.
        /// </param>
        protected override void RenderLabel(IRenderContext rc, OxyRect clippingRect, OxyRect rect, double value, int i)
        {
            var s = StringHelper.Format(
                this.ActualCulture, this.LabelFormatString, this.GetItem(this.ValidItemsIndexInversion[i]), value);
            HorizontalAlignment ha;
            ScreenPoint pt;
            switch (this.LabelPlacement)
            {
                case LabelPlacement.Inside:
                    pt = new ScreenPoint(rect.Right - this.LabelMargin, (rect.Top + rect.Bottom) / 2);
                    ha = HorizontalAlignment.Right;
                    break;
                case LabelPlacement.Middle:
                    pt = new ScreenPoint((rect.Left + rect.Right) / 2, (rect.Top + rect.Bottom) / 2);
                    ha = HorizontalAlignment.Center;
                    break;
                case LabelPlacement.Base:
                    pt = new ScreenPoint(rect.Left + this.LabelMargin, (rect.Top + rect.Bottom) / 2);
                    ha = HorizontalAlignment.Left;
                    break;
                default: // Outside
                    pt = new ScreenPoint(rect.Right + this.LabelMargin, (rect.Top + rect.Bottom) / 2);
                    ha = HorizontalAlignment.Left;
                    break;
            }

            rc.DrawClippedText(
                clippingRect,
                pt,
                s,
                this.ActualTextColor,
                this.ActualFont,
                this.ActualFontSize,
                this.ActualFontWeight,
                0,
                ha,
                VerticalAlignment.Middle);
        }

    }
}