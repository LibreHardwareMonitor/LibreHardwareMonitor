// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HorizontalAndVerticalAxisRenderer.cs" company="OxyPlot">
//   The MIT License (MIT)
//   
//   Copyright (c) 2012 Oystein Bjorke
//   
//   Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
//   associated documentation files (the "Software"), to deal in the Software without restriction,
//   including without limitation the rights to use, copy, modify, merge, publish, distribute,
//   sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
//   furnished to do so, subject to the following conditions:
//   
//   The above copyright notice and this permission notice shall be included in all copies or substantial
//   portions of the Software.
//   
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
//   NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//   IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
//   WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//   SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
// <summary>
//   Rendering helper class for horizontal and vertical axes (both linear and logarithmic)
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using OxyPlot.Axes;

    /// <summary>
    /// Preovides functionality to render horizontal and vertical axes.
    /// </summary>
    public class HorizontalAndVerticalAxisRenderer : AxisRendererBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HorizontalAndVerticalAxisRenderer"/> class.
        /// </summary>
        /// <param name="rc">
        /// The render context.
        /// </param>
        /// <param name="plot">
        /// The plot.
        /// </param>
        public HorizontalAndVerticalAxisRenderer(IRenderContext rc, PlotModel plot)
            : base(rc, plot)
        {
        }

        /// <summary>
        /// Renders the specified axis.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <param name="pass">The pass.</param>
        public override void Render(Axis axis, int pass)
        {
            base.Render(axis, pass);

            double totalShift = axis.PositionTierMinShift;
            double tierSize = axis.PositionTierSize - this.Plot.AxisTierDistance;

            // store properties locally for performance
            double plotAreaLeft = this.Plot.PlotArea.Left;
            double plotAreaRight = this.Plot.PlotArea.Right;
            double plotAreaTop = this.Plot.PlotArea.Top;
            double plotAreaBottom = this.Plot.PlotArea.Bottom;

            // Axis position (x or y screen coordinate)
            double axisPosition = 0;
            double titlePosition = 0;

            switch (axis.Position)
            {
                case AxisPosition.Left:
                    axisPosition = plotAreaLeft - totalShift;
                    titlePosition = axisPosition - tierSize;
                    break;
                case AxisPosition.Right:
                    axisPosition = plotAreaRight + totalShift;
                    titlePosition = axisPosition + tierSize;
                    break;
                case AxisPosition.Top:
                    axisPosition = plotAreaTop - totalShift;
                    titlePosition = axisPosition - tierSize;
                    break;
                case AxisPosition.Bottom:
                    axisPosition = plotAreaBottom + totalShift;
                    titlePosition = axisPosition + tierSize;
                    break;
            }

            if (axis.PositionAtZeroCrossing)
            {
                var perpendicularAxis = axis.IsHorizontal() ? this.Plot.DefaultYAxis : this.Plot.DefaultXAxis;
                axisPosition = perpendicularAxis.Transform(0);
            }

            if (pass == 0)
            {
                this.RenderMinorItems(axis, axisPosition);
            }

            if (pass == 1)
            {
                this.RenderMajorItems(axis, axisPosition, titlePosition);
            }
        }

        /// <summary>
        /// Gets the axis title position, rotation and alignment.
        /// </summary>
        /// <param name="axis">
        /// The axis.
        /// </param>
        /// <param name="titlePosition">
        /// The title position.
        /// </param>
        /// <param name="angle">
        /// The angle.
        /// </param>
        /// <param name="halign">
        /// The horizontal alignment.
        /// </param>
        /// <param name="valign">
        /// The vertical alignment.
        /// </param>
        /// <returns>
        /// The <see cref="ScreenPoint"/>.
        /// </returns>
        protected virtual ScreenPoint GetAxisTitlePositionAndAlignment(
            Axis axis, 
            double titlePosition, 
            ref double angle, 
            ref HorizontalAlignment halign, 
            ref VerticalAlignment valign)
        {
            double middle = axis.IsHorizontal()
                                ? Lerp(axis.ScreenMin.X, axis.ScreenMax.X, axis.TitlePosition)
                                : Lerp(axis.ScreenMax.Y, axis.ScreenMin.Y, axis.TitlePosition);

            if (axis.PositionAtZeroCrossing)
            {
                var perpendicularAxis = axis.IsHorizontal() ? this.Plot.DefaultYAxis : this.Plot.DefaultXAxis;
                middle = perpendicularAxis.Transform(perpendicularAxis.ActualMaximum);
            }

            switch (axis.Position)
            {
                case AxisPosition.Left:
                    return new ScreenPoint(titlePosition, middle);
                case AxisPosition.Right:
                    valign = VerticalAlignment.Bottom;
                    return new ScreenPoint(titlePosition, middle);
                case AxisPosition.Top:
                    halign = HorizontalAlignment.Center;
                    valign = VerticalAlignment.Top;
                    angle = 0;
                    return new ScreenPoint(middle, titlePosition);
                case AxisPosition.Bottom:
                    halign = HorizontalAlignment.Center;
                    valign = VerticalAlignment.Bottom;
                    angle = 0;
                    return new ScreenPoint(middle, titlePosition);
                default:
                    throw new ArgumentOutOfRangeException("axis");
            }
        }

        /// <summary>
        /// Gets the alignments given the specified rotation angle.
        /// </summary>
        /// <param name="angle">
        /// The angle.
        /// </param>
        /// <param name="defaultHorizontalAlignment">
        /// The default horizontal alignment.
        /// </param>
        /// <param name="defaultVerticalAlignment">
        /// The default vertical alignment.
        /// </param>
        /// <param name="ha">
        /// The rotated horizontal alignment.
        /// </param>
        /// <param name="va">
        /// The rotated vertical alignment.
        /// </param>
        protected virtual void GetRotatedAlignments(
            double angle, 
            HorizontalAlignment defaultHorizontalAlignment, 
            VerticalAlignment defaultVerticalAlignment, 
            out HorizontalAlignment ha, 
            out VerticalAlignment va)
        {
            ha = defaultHorizontalAlignment;
            va = defaultVerticalAlignment;

            Debug.Assert(angle <= 180 && angle >= -180, "Axis angle should be in the interval [-180,180] degrees.");

            if (angle > -45 && angle < 45)
            {
                return;
            }

            if (angle > 135 || angle < -135)
            {
                ha = (HorizontalAlignment)(-(int)defaultHorizontalAlignment);
                va = (VerticalAlignment)(-(int)defaultVerticalAlignment);
                return;
            }

            if (angle > 45)
            {
                ha = (HorizontalAlignment)((int)defaultVerticalAlignment);
                va = (VerticalAlignment)(-(int)defaultHorizontalAlignment);
                return;
            }

            if (angle < -45)
            {
                ha = (HorizontalAlignment)(-(int)defaultVerticalAlignment);
                va = (VerticalAlignment)((int)defaultHorizontalAlignment);
            }
        }

        /// <summary>
        /// Linear interpolation
        /// http://en.wikipedia.org/wiki/Linear_interpolation
        /// </summary>
        /// <param name="x0">
        /// The x0.
        /// </param>
        /// <param name="x1">
        /// The x1.
        /// </param>
        /// <param name="f">
        /// The interpolation factor.
        /// </param>
        /// <returns>
        /// The interpolated value.
        /// </returns>
        private static double Lerp(double x0, double x1, double f)
        {
            return (x0 * (1 - f)) + (x1 * f);
        }

        /// <summary>
        /// Snaps v to value if it is within the the specified distance.
        /// </summary>
        /// <param name="target">
        /// The target value.
        /// </param>
        /// <param name="v">
        /// The value to snap.
        /// </param>
        /// <param name="eps">
        /// The distance tolerance.
        /// </param>
        private static void SnapTo(double target, ref double v, double eps = 0.5)
        {
            if (v > target - eps && v < target + eps)
            {
                v = target;
            }
        }

        /// <summary>
        /// Renders the axis title.
        /// </summary>
        /// <param name="axis">
        /// The axis.
        /// </param>
        /// <param name="titlePosition">
        /// The title position.
        /// </param>
        private void RenderAxisTitle(Axis axis, double titlePosition)
        {
            bool isHorizontal = axis.IsHorizontal();

            OxySize? maxSize = null;

            if (axis.ClipTitle)
            {
                // Calculate the title clipping dimensions
                double screenLength = isHorizontal
                                          ? Math.Abs(axis.ScreenMax.X - axis.ScreenMin.X)
                                          : Math.Abs(axis.ScreenMax.Y - axis.ScreenMin.Y);

                maxSize = new OxySize(screenLength * axis.TitleClippingLength, double.MaxValue);
            }

            double angle = -90;

            var halign = HorizontalAlignment.Center;
            var valign = VerticalAlignment.Top;

            var lpt = this.GetAxisTitlePositionAndAlignment(axis, titlePosition, ref angle, ref halign, ref valign);

            this.rc.SetToolTip(axis.ToolTip);
            this.rc.DrawMathText(
                lpt, 
                axis.ActualTitle, 
                axis.ActualTitleColor, 
                axis.ActualTitleFont, 
                axis.ActualTitleFontSize, 
                axis.ActualTitleFontWeight, 
                angle, 
                halign, 
                valign, 
                maxSize);
            this.rc.SetToolTip(null);
        }

        /// <summary>
        /// Renders the major items.
        /// </summary>
        /// <param name="axis">
        /// The axis.
        /// </param>
        /// <param name="axisPosition">
        /// The axis position.
        /// </param>
        /// <param name="titlePosition">
        /// The title position.
        /// </param>
        private void RenderMajorItems(Axis axis, double axisPosition, double titlePosition)
        {
            double eps = axis.ActualMinorStep * 1e-3;

            double actualMinimum = axis.ActualMinimum;
            double actualMaximum = axis.ActualMaximum;

            double plotAreaLeft = this.Plot.PlotArea.Left;
            double plotAreaRight = this.Plot.PlotArea.Right;
            double plotAreaTop = this.Plot.PlotArea.Top;
            double plotAreaBottom = this.Plot.PlotArea.Bottom;
            bool isHorizontal = axis.IsHorizontal();

            double a0;
            double a1;
            var majorSegments = new List<ScreenPoint>();
            var majorTickSegments = new List<ScreenPoint>();
            this.GetTickPositions(axis, axis.TickStyle, axis.MajorTickSize, axis.Position, out a0, out a1);

            foreach (double value in this.MajorTickValues)
            {
                if (value < actualMinimum - eps || value > actualMaximum + eps)
                {
                    continue;
                }

                if (axis.PositionAtZeroCrossing && Math.Abs(value) < eps)
                {
                    continue;
                }

                double transformedValue = axis.Transform(value);
                if (isHorizontal)
                {
                    SnapTo(plotAreaLeft, ref transformedValue);
                    SnapTo(plotAreaRight, ref transformedValue);
                }
                else
                {
                    SnapTo(plotAreaTop, ref transformedValue);
                    SnapTo(plotAreaBottom, ref transformedValue);
                }

                if (this.MajorPen != null)
                {
                    if (isHorizontal)
                    {
                        majorSegments.Add(new ScreenPoint(transformedValue, plotAreaTop));
                        majorSegments.Add(new ScreenPoint(transformedValue, plotAreaBottom));
                    }
                    else
                    {
                        majorSegments.Add(new ScreenPoint(plotAreaLeft, transformedValue));
                        majorSegments.Add(new ScreenPoint(plotAreaRight, transformedValue));
                    }
                }

                if (axis.TickStyle != TickStyle.None)
                {
                    if (isHorizontal)
                    {
                        majorTickSegments.Add(new ScreenPoint(transformedValue, axisPosition + a0));
                        majorTickSegments.Add(new ScreenPoint(transformedValue, axisPosition + a1));
                    }
                    else
                    {
                        majorTickSegments.Add(new ScreenPoint(axisPosition + a0, transformedValue));
                        majorTickSegments.Add(new ScreenPoint(axisPosition + a1, transformedValue));
                    }
                }
            }

            // Render the axis labels (numbers or category names)
            foreach (double value in this.MajorLabelValues)
            {
                if (value < actualMinimum - eps || value > actualMaximum + eps)
                {
                    continue;
                }

                if (axis.PositionAtZeroCrossing && Math.Abs(value) < eps)
                {
                    continue;
                }

                double transformedValue = axis.Transform(value);
                if (isHorizontal)
                {
                    SnapTo(plotAreaLeft, ref transformedValue);
                    SnapTo(plotAreaRight, ref transformedValue);
                }
                else
                {
                    SnapTo(plotAreaTop, ref transformedValue);
                    SnapTo(plotAreaBottom, ref transformedValue);
                }

                var pt = new ScreenPoint();
                var ha = HorizontalAlignment.Right;
                var va = VerticalAlignment.Middle;
                switch (axis.Position)
                {
                    case AxisPosition.Left:
                        pt = new ScreenPoint(axisPosition + a1 - axis.AxisTickToLabelDistance, transformedValue);
                        this.GetRotatedAlignments(
                            axis.Angle, HorizontalAlignment.Right, VerticalAlignment.Middle, out ha, out va);
                        break;
                    case AxisPosition.Right:
                        pt = new ScreenPoint(axisPosition + a1 + axis.AxisTickToLabelDistance, transformedValue);
                        this.GetRotatedAlignments(
                            axis.Angle, HorizontalAlignment.Left, VerticalAlignment.Middle, out ha, out va);
                        break;
                    case AxisPosition.Top:
                        pt = new ScreenPoint(transformedValue, axisPosition + a1 - axis.AxisTickToLabelDistance);
                        this.GetRotatedAlignments(
                            axis.Angle, HorizontalAlignment.Center, VerticalAlignment.Bottom, out ha, out va);
                        break;
                    case AxisPosition.Bottom:
                        pt = new ScreenPoint(transformedValue, axisPosition + a1 + axis.AxisTickToLabelDistance);
                        this.GetRotatedAlignments(
                            axis.Angle, HorizontalAlignment.Center, VerticalAlignment.Top, out ha, out va);
                        break;
                }

                string text = axis.FormatValue(value);
                this.rc.DrawMathText(
                    pt, 
                    text, 
                    axis.ActualTextColor, 
                    axis.ActualFont, 
                    axis.ActualFontSize, 
                    axis.ActualFontWeight, 
                    axis.Angle, 
                    ha, 
                    va);
            }

            // Draw the zero crossing line
            if (axis.PositionAtZeroCrossing && this.ZeroPen != null)
            {
                double t0 = axis.Transform(0);
                if (isHorizontal)
                {
                    this.rc.DrawLine(t0, plotAreaTop, t0, plotAreaBottom, this.ZeroPen);
                }
                else
                {
                    this.rc.DrawLine(plotAreaLeft, t0, plotAreaRight, t0, this.ZeroPen);
                }
            }

            // Draw extra grid lines
            if (axis.ExtraGridlines != null && this.ExtraPen != null)
            {
                foreach (double value in axis.ExtraGridlines)
                {
                    if (!this.IsWithin(value, actualMinimum, actualMaximum))
                    {
                        continue;
                    }

                    double transformedValue = axis.Transform(value);
                    if (isHorizontal)
                    {
                        this.rc.DrawLine(transformedValue, plotAreaTop, transformedValue, plotAreaBottom, this.ExtraPen);
                    }
                    else
                    {
                        this.rc.DrawLine(plotAreaLeft, transformedValue, plotAreaRight, transformedValue, this.ExtraPen);
                    }
                }
            }

            // Draw the axis line (across the tick marks)
            if (isHorizontal)
            {
                this.rc.DrawLine(
                    axis.Transform(actualMinimum), 
                    axisPosition, 
                    axis.Transform(actualMaximum), 
                    axisPosition, 
                    this.AxislinePen);
            }
            else
            {
                this.rc.DrawLine(
                    axisPosition, 
                    axis.Transform(actualMinimum), 
                    axisPosition, 
                    axis.Transform(actualMaximum), 
                    this.AxislinePen);
            }

            // Draw the axis title
            if (!string.IsNullOrEmpty(axis.ActualTitle))
            {
                this.RenderAxisTitle(axis, titlePosition);
            }

            if (this.MajorPen != null)
            {
                this.rc.DrawLineSegments(majorSegments, this.MajorPen);
            }

            if (this.MajorTickPen != null)
            {
                this.rc.DrawLineSegments(majorTickSegments, this.MajorTickPen);
            }
        }

        /// <summary>
        /// Renders the minor items.
        /// </summary>
        /// <param name="axis">
        /// The axis.
        /// </param>
        /// <param name="axisPosition">
        /// The axis position.
        /// </param>
        private void RenderMinorItems(Axis axis, double axisPosition)
        {
            double eps = axis.ActualMinorStep * 1e-3;
            double actualMinimum = axis.ActualMinimum;
            double actualMaximum = axis.ActualMaximum;

            double plotAreaLeft = this.Plot.PlotArea.Left;
            double plotAreaRight = this.Plot.PlotArea.Right;
            double plotAreaTop = this.Plot.PlotArea.Top;
            double plotAreaBottom = this.Plot.PlotArea.Bottom;
            bool isHorizontal = axis.IsHorizontal();

            double a0;
            double a1;
            var minorSegments = new List<ScreenPoint>();
            var minorTickSegments = new List<ScreenPoint>();
            this.GetTickPositions(axis, axis.TickStyle, axis.MinorTickSize, axis.Position, out a0, out a1);

            foreach (double value in this.MinorTickValues)
            {
                if (value < actualMinimum - eps || value > actualMaximum + eps)
                {
                    continue;
                }

                if (this.MajorTickValues.Contains(value))
                {
                    continue;
                }

                if (axis.PositionAtZeroCrossing && Math.Abs(value) < eps)
                {
                    continue;
                }

                double transformedValue = axis.Transform(value);

                if (isHorizontal)
                {
                    SnapTo(plotAreaLeft, ref transformedValue);
                    SnapTo(plotAreaRight, ref transformedValue);
                }
                else
                {
                    SnapTo(plotAreaTop, ref transformedValue);
                    SnapTo(plotAreaBottom, ref transformedValue);
                }

                // Draw the minor grid line
                if (this.MinorPen != null)
                {
                    if (isHorizontal)
                    {
                        minorSegments.Add(new ScreenPoint(transformedValue, plotAreaTop));
                        minorSegments.Add(new ScreenPoint(transformedValue, plotAreaBottom));
                    }
                    else
                    {
                        if (transformedValue < plotAreaTop || transformedValue > plotAreaBottom)
                        {
                        }

                        minorSegments.Add(new ScreenPoint(plotAreaLeft, transformedValue));
                        minorSegments.Add(new ScreenPoint(plotAreaRight, transformedValue));
                    }
                }

                // Draw the minor tick
                if (axis.TickStyle != TickStyle.None)
                {
                    if (isHorizontal)
                    {
                        minorTickSegments.Add(new ScreenPoint(transformedValue, axisPosition + a0));
                        minorTickSegments.Add(new ScreenPoint(transformedValue, axisPosition + a1));
                    }
                    else
                    {
                        minorTickSegments.Add(new ScreenPoint(axisPosition + a0, transformedValue));
                        minorTickSegments.Add(new ScreenPoint(axisPosition + a1, transformedValue));
                    }
                }
            }

            // Draw all the line segments);
            if (this.MinorPen != null)
            {
                this.rc.DrawLineSegments(minorSegments, this.MinorPen);
            }

            if (this.MinorTickPen != null)
            {
                this.rc.DrawLineSegments(minorTickSegments, this.MinorTickPen);
            }
        }
    }
}