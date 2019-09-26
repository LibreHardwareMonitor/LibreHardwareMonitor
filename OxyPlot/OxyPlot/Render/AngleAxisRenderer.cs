// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AngleAxisRenderer.cs" company="OxyPlot">
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
//   The angle axis renderer.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System;

    using OxyPlot.Axes;

    /// <summary>
    /// Provides functionality to render <see cref="AngleAxis"/>.
    /// </summary>
    public class AngleAxisRenderer : AxisRendererBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AngleAxisRenderer"/> class.
        /// </summary>
        /// <param name="rc">
        /// The render context.
        /// </param>
        /// <param name="plot">
        /// The plot.
        /// </param>
        public AngleAxisRenderer(IRenderContext rc, PlotModel plot)
            : base(rc, plot)
        {
        }

        /// <summary>
        /// Renders the specified axis.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <param name="pass">The render pass.</param>
        /// <exception cref="System.InvalidOperationException">Magnitude axis not defined.</exception>
        public override void Render(Axis axis, int pass)
        {
            base.Render(axis, pass);

            var magnitudeAxis = this.Plot.DefaultMagnitudeAxis;

            if (axis.RelatedAxis != null)
            {
                magnitudeAxis = axis.RelatedAxis as MagnitudeAxis;
            }

            if (magnitudeAxis == null)
            {
                throw new InvalidOperationException("Magnitude axis not defined.");
            }

            double eps = axis.MinorStep * 1e-3;

            if (axis.ShowMinorTicks)
            {
                foreach (double value in this.MinorTickValues)
                {
                    if (value < axis.ActualMinimum - eps || value > axis.ActualMaximum + eps)
                    {
                        continue;
                    }

                    if (this.MajorTickValues.Contains(value))
                    {
                        continue;
                    }

                    var pt = magnitudeAxis.Transform(magnitudeAxis.ActualMaximum, value, axis);

                    if (this.MinorPen != null)
                    {
                        this.rc.DrawLine(magnitudeAxis.MidPoint.x, magnitudeAxis.MidPoint.y, pt.x, pt.y, this.MinorPen, false);
                    }
                }
            }

            var angleAxis = (AngleAxis)axis;
            bool isFullCircle = Math.Abs(Math.Abs(angleAxis.EndAngle - angleAxis.StartAngle) - 360) < 1e-6;

            foreach (double value in this.MajorTickValues)
            {
                // skip the last value (overlapping with the first)
                if (isFullCircle && value > axis.ActualMaximum - eps)
                {
                    continue;
                }

                if (value < axis.ActualMinimum - eps || value > axis.ActualMaximum + eps)
                {
                    continue;
                }

                ScreenPoint pt = magnitudeAxis.Transform(magnitudeAxis.ActualMaximum, value, axis);
                if (this.MajorPen != null)
                {
                    this.rc.DrawLine(
                        magnitudeAxis.MidPoint.x, magnitudeAxis.MidPoint.y, pt.x, pt.y, this.MajorPen, false);
                }
            }

            foreach (double value in this.MajorLabelValues)
            {
                // skip the last value (overlapping with the first)
                if (isFullCircle && value > axis.ActualMaximum - eps)
                {
                    continue;
                }

                var pt = magnitudeAxis.Transform(magnitudeAxis.ActualMaximum, value, axis);
                double angle = Math.Atan2(pt.y - magnitudeAxis.MidPoint.y, pt.x - magnitudeAxis.MidPoint.x);

                // add some margin
                pt.x += Math.Cos(angle) * axis.AxisTickToLabelDistance;
                pt.y += Math.Sin(angle) * axis.AxisTickToLabelDistance;

                // Convert to degrees
                angle *= 180 / Math.PI;

                string text = axis.FormatValue(value);

                var ha = HorizontalAlignment.Left;
                var va = VerticalAlignment.Middle;

                if (Math.Abs(Math.Abs(angle) - 90) < 10)
                {
                    ha = HorizontalAlignment.Center;
                    va = angle > 90 ? VerticalAlignment.Top : VerticalAlignment.Bottom;
                    angle = 0;
                }
                else if (angle > 90 || angle < -90)
                {
                    angle -= 180;
                    ha = HorizontalAlignment.Right;
                }

                this.rc.DrawMathText(
                    pt, text, axis.ActualTextColor, axis.ActualFont, axis.ActualFontSize, axis.ActualFontWeight, angle, ha, va);
            }
        }
    }
}