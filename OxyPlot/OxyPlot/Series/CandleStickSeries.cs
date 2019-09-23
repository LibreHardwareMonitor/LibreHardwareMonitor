// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CandleStickSeries.cs" company="OxyPlot">
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
//   Represents a series for candlestick charts.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Series
{
    using System;

    /// <summary>
    /// Represents a series for candlestick charts.
    /// </summary>
    /// <remarks>
    /// http://en.wikipedia.org/wiki/Candlestick_chart
    /// http://www.mathworks.com/help/toolbox/finance/candle.html
    /// </remarks>
    public class CandleStickSeries : HighLowSeries
    {
        /// <summary>
        /// Initializes a new instance of the <see cref = "CandleStickSeries" /> class.
        /// </summary>
        public CandleStickSeries()
        {
            this.CandleWidth = 10;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CandleStickSeries"/> class.
        /// </summary>
        /// <param name="title">
        /// The title.
        /// </param>
        public CandleStickSeries(string title)
            : this()
        {
            this.Title = title;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CandleStickSeries"/> class.
        /// </summary>
        /// <param name="color">
        /// The color.
        /// </param>
        /// <param name="strokeThickness">
        /// The stroke thickness.
        /// </param>
        /// <param name="title">
        /// The title.
        /// </param>
        public CandleStickSeries(OxyColor color, double strokeThickness = 1, string title = null)
            : this()
        {
            this.Color = color;
            this.StrokeThickness = strokeThickness;
            this.Title = title;
        }

        /// <summary>
        /// Gets or sets the width of the candle.
        /// </summary>
        /// <value>The width of the candle.</value>
        public double CandleWidth { get; set; }

        /// <summary>
        /// Renders the series on the specified rendering context.
        /// </summary>
        /// <param name="rc">
        /// The rendering context.
        /// </param>
        /// <param name="model">
        /// The owner plot model.
        /// </param>
        public override void Render(IRenderContext rc, PlotModel model)
        {
            if (this.Items.Count == 0)
            {
                return;
            }

            this.VerifyAxes();

            var clippingRect = this.GetClippingRect();

            foreach (var v in this.Items)
            {
                if (!this.IsValidItem(v, this.XAxis, this.YAxis))
                {
                    continue;
                }

                if (this.StrokeThickness > 0 && this.LineStyle != LineStyle.None)
                {
                    var high = this.Transform(v.X, v.High);
                    var low = this.Transform(v.X, v.Low);

                    if (double.IsNaN(v.Open) || double.IsNaN(v.Close))
                    {
                        rc.DrawClippedLine(
                            new[] { low, high },
                            clippingRect,
                            0,
                            this.GetSelectableColor(this.ActualColor),
                            this.StrokeThickness,
                            this.LineStyle,
                            this.LineJoin,
                            false);
                    }
                    else
                    {
                        var open = this.Transform(v.X, v.Open);
                        var close = this.Transform(v.X, v.Close);
                        var max = new ScreenPoint(open.X, Math.Max(open.Y, close.Y));
                        var min = new ScreenPoint(open.X, Math.Min(open.Y, close.Y));

                        rc.DrawClippedLine(
                            new[] { high, min },
                            clippingRect,
                            0,
                            this.GetSelectableColor(this.ActualColor),
                            this.StrokeThickness,
                            this.LineStyle,
                            this.LineJoin,
                            true);

                        rc.DrawClippedLine(
                            new[] { max, low },
                            clippingRect,
                            0,
                            this.GetSelectableColor(this.ActualColor),
                            this.StrokeThickness,
                            this.LineStyle,
                            this.LineJoin,
                            true);
                        var openLeft = open;
                        openLeft.X -= this.CandleWidth * 0.5;
                        var closeRight = close;
                        closeRight.X += this.CandleWidth * 0.5;
                        var rect = new OxyRect(openLeft.X, min.Y, this.CandleWidth, max.Y - min.Y);
                        rc.DrawClippedRectangleAsPolygon(
                            rect, clippingRect, v.Open > v.Close ? this.GetSelectableFillColor(this.ActualColor) : null, this.GetSelectableColor(this.ActualColor), this.StrokeThickness);
                    }
                }
            }
        }

        /// <summary>
        /// Renders the legend symbol for the series on the specified rendering context.
        /// </summary>
        /// <param name="rc">
        /// The rendering context.
        /// </param>
        /// <param name="legendBox">
        /// The bounding rectangle of the legend box.
        /// </param>
        public override void RenderLegend(IRenderContext rc, OxyRect legendBox)
        {
            double xmid = (legendBox.Left + legendBox.Right) / 2;
            double yopen = legendBox.Top + ((legendBox.Bottom - legendBox.Top) * 0.7);
            double yclose = legendBox.Top + ((legendBox.Bottom - legendBox.Top) * 0.3);
            double[] dashArray = LineStyleHelper.GetDashArray(this.LineStyle);
            rc.DrawLine(
                new[] { new ScreenPoint(xmid, legendBox.Top), new ScreenPoint(xmid, legendBox.Bottom) },
                this.GetSelectableColor(this.ActualColor),
                this.StrokeThickness,
                dashArray,
                OxyPenLineJoin.Miter,
                true);
            rc.DrawRectangleAsPolygon(
                new OxyRect(xmid - (this.CandleWidth * 0.5), yclose, this.CandleWidth, yopen - yclose),
                this.GetSelectableFillColor(this.ActualColor),
                this.GetSelectableColor(this.ActualColor),
                this.StrokeThickness);
        }

    }
}