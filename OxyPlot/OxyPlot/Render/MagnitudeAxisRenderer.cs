// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MagnitudeAxisRenderer.cs" company="OxyPlot">
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
//   The magnitude axis renderer.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System;
    using System.Collections.Generic;

    using OxyPlot.Axes;

    /// <summary>
    /// Provides functionality to render <see cref="MagnitudeAxis"/>.
    /// </summary>
    public class MagnitudeAxisRenderer : AxisRendererBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MagnitudeAxisRenderer"/> class.
        /// </summary>
        /// <param name="rc">
        /// The render context.
        /// </param>
        /// <param name="plot">
        /// The plot.
        /// </param>
        public MagnitudeAxisRenderer(IRenderContext rc, PlotModel plot)
            : base(rc, plot)
        {
        }

        /// <summary>
        /// Renders the specified axis.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <param name="pass">The pass.</param>
        /// <exception cref="System.NullReferenceException">Angle axis should not be null.</exception>
        public override void Render(Axis axis, int pass)
        {
            base.Render(axis, pass);

            var angleAxis = this.Plot.DefaultAngleAxis as Axis;
            if (axis.RelatedAxis != null)
            {
                angleAxis = axis.RelatedAxis;
            }

            if (angleAxis == null)
            {
                throw new NullReferenceException("Angle axis should not be null.");
            }

            if (axis.ShowMinorTicks)
            {
                // GetVerticalTickPositions(axis, axis.TickStyle, axis.MinorTickSize, out y0, out y1);

                foreach (double xValue in this.MinorTickValues)
                {
                    if (xValue < axis.ActualMinimum || xValue > axis.ActualMaximum)
                    {
                        continue;
                    }

                    if (this.MajorTickValues.Contains(xValue))
                    {
                        continue;
                    }

                    var pts = new List<ScreenPoint>();
                    for (double th = angleAxis.ActualMinimum;
                         th <= angleAxis.ActualMaximum + angleAxis.MinorStep * 0.01;
                         th += angleAxis.MinorStep * 0.1)
                    {
                        pts.Add(axis.Transform(xValue, th, angleAxis));
                    }

                    if (this.MinorPen != null)
                    {
                        this.rc.DrawLine(pts, this.MinorPen.Color, this.MinorPen.Thickness, this.MinorPen.DashArray);
                    }

                    // RenderGridline(x, y + y0, x, y + y1, minorTickPen);
                }
            }

            // GetVerticalTickPositions(axis, axis.TickStyle, axis.MajorTickSize, out y0, out y1);

            foreach (double xValue in this.MajorTickValues)
            {
                if (xValue < axis.ActualMinimum || xValue > axis.ActualMaximum)
                {
                    continue;
                }

                var pts = new List<ScreenPoint>();
                for (double th = angleAxis.ActualMinimum;
                     th <= angleAxis.ActualMaximum + angleAxis.MinorStep * 0.01;
                     th += angleAxis.MinorStep * 0.1)
                {
                    pts.Add(axis.Transform(xValue, th, angleAxis));
                }

                if (this.MajorPen != null)
                {
                    this.rc.DrawLine(pts, this.MajorPen.Color, this.MajorPen.Thickness, this.MajorPen.DashArray);
                }

                // RenderGridline(x, y + y0, x, y + y1, majorTickPen);
                // var pt = new ScreenPoint(x, istop ? y + y1 - TICK_DIST : y + y1 + TICK_DIST);
                // string text = axis.FormatValue(xValue);
                // double h = rc.MeasureText(text, axis.Font, axis.FontSize, axis.FontWeight).Height;
                // rc.DrawText(pt, text, axis.LabelColor ?? plot.TextColor,
                // axis.Font, axis.FontSize, axis.FontWeight,
                // axis.Angle,
                // HorizontalAlignment.Center, istop ? VerticalAlignment.Bottom : VerticalAlignment.Top);
                // maxh = Math.Max(maxh, h);
            }
        }

    }
}