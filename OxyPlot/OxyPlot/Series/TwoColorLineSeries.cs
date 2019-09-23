// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TwoColorLineSeries.cs" company="OxyPlot">
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
//   Represents a two-color line series.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Series
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a two-color line series.
    /// </summary>
    public class TwoColorLineSeries : LineSeries
    {
        /// <summary>
        /// The default second color.
        /// </summary>
        private OxyColor defaultColor2;

        /// <summary>
        /// Initializes a new instance of the <see cref = "TwoColorLineSeries" /> class.
        /// </summary>
        public TwoColorLineSeries()
        {
            this.Limit = 0.0;
            this.Color2 = OxyColors.Blue;
            this.LineStyle2 = LineStyle.Solid;
        }

        /// <summary>
        /// Gets or sets the color for the part of the line that is below the limit.
        /// </summary>
        public OxyColor Color2 { get; set; }

        /// <summary>
        /// Gets the actual second color.
        /// </summary>
        /// <value>The actual color.</value>
        public OxyColor ActualColor2
        {
            get { return this.Color2 ?? this.defaultColor2; }
        }

        /// <summary>
        /// Gets or sets the limit.
        /// </summary>
        /// <remarks>
        /// The parts of the line that is below this limit will be rendered with Color2.
        /// The parts of the line that is above the limit will be rendered with Color.
        /// </remarks>
        public double Limit { get; set; }

        /// <summary>
        /// Gets or sets the line style for the part of the line that is below the limit.
        /// </summary>
        /// <value>The line style.</value>
        public LineStyle LineStyle2 { get; set; }

        /// <summary>
        /// Gets the actual line style for the part of the line that is below the limit.
        /// </summary>
        /// <value>The line style.</value>
        public LineStyle ActualLineStyle2
        {
            get
            {
                return this.LineStyle2 != LineStyle.Undefined ? this.LineStyle2 : LineStyle.Solid;
            }
        }

        /// <summary>
        /// The set default values.
        /// </summary>
        /// <param name="model">
        /// The model.
        /// </param>
        protected internal override void SetDefaultValues(PlotModel model)
        {
            if (this.Color2 == null)
            {
                this.LineStyle2 = model.GetDefaultLineStyle();
                this.defaultColor2 = model.GetDefaultColor();
            }
        }

        /// <summary>
        /// Renders the smoothed line.
        /// </summary>
        /// <param name="rc">
        /// The render context.
        /// </param>
        /// <param name="clippingRect">
        /// The clipping rect.
        /// </param>
        /// <param name="pointsToRender">
        /// The points.
        /// </param>
        protected override void RenderSmoothedLine(IRenderContext rc, OxyRect clippingRect, IList<ScreenPoint> pointsToRender)
        {
            double bottom = clippingRect.Bottom;

            // todo: this does not work when y axis is reversed
            double y = this.YAxis.Transform(this.Limit);

            if (y < clippingRect.Top)
            {
                y = clippingRect.Top;
            }

            if (y > clippingRect.Bottom)
            {
                y = clippingRect.Bottom;
            }

            clippingRect.Bottom = y;
            rc.DrawClippedLine(
                pointsToRender,
                clippingRect,
                this.MinimumSegmentLength * this.MinimumSegmentLength,
                this.GetSelectableColor(this.ActualColor),
                this.StrokeThickness,
                this.ActualLineStyle,
                this.LineJoin,
                false);
            clippingRect.Top = y;
            clippingRect.Height = bottom - y;
            rc.DrawClippedLine(
                pointsToRender,
                clippingRect,
                this.MinimumSegmentLength * this.MinimumSegmentLength,
                this.GetSelectableColor(this.ActualColor2),
                this.StrokeThickness,
                this.ActualLineStyle2,
                this.LineJoin,
                false);
        }

    }
}