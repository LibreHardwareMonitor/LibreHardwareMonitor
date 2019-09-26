// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VerticalAxisRenderer.cs" company="OxyPlot">
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
//   Gets the rotated alignments given the specified angle.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;

namespace OxyPlot
{
    public class VerticalAxisRendererBase : AxisRendererBase
    {
        public VerticalAxisRendererBase(IRenderContext rc, PlotModel plot)
            : base(rc, plot)
        {
        }

        public override void Render(Axis axis)
        {
            base.Render(axis);

            var perpendicularAxis = Plot.DefaultXAxis;
            bool isHorizontal = true;

            // Axis position (x or y screen coordinate)
            double apos = 0;

            switch (axis.Position)
            {
                case AxisPosition.Left:
                    apos = Plot.PlotArea.Left;
                    isHorizontal = false;
                    break;
                case AxisPosition.Right:
                    apos = Plot.PlotArea.Right;
                    isHorizontal = false;
                    break;
                case AxisPosition.Top:
                    apos = Plot.PlotArea.Top;
                    perpendicularAxis = Plot.DefaultYAxis;
                    break;
                case AxisPosition.Bottom:
                    apos = Plot.PlotArea.Bottom;
                    perpendicularAxis = Plot.DefaultYAxis;
                    break;
            }

            if (axis.PositionAtZeroCrossing)
            {
                apos = perpendicularAxis.Transform(0);
            }

            double a0, a1;

            if (axis.ShowMinorTicks)
            {
                GetTickPositions(axis, axis.TickStyle, axis.MinorTickSize, axis.Position, out a0, out a1);

                foreach (double value in MinorTickValues)
                {
                    if (value < axis.ActualMinimum || value > axis.ActualMaximum)
                    {
                        continue;
                    }

                    if (MajorTickValues.Contains(value))
                    {
                        continue;
                    }

                    double transformedValue = axis.Transform(value);

                    if (MinorPen != null)
                    {
                        if (isHorizontal)
                        {
                            rc.DrawLine(transformedValue, Plot.PlotArea.Top, transformedValue, Plot.PlotArea.Bottom, MinorPen);

                        }
                        else
                        {
                            rc.DrawLine(Plot.PlotArea.Left, transformedValue, Plot.PlotArea.Right, transformedValue, MinorPen);
                        }
                    }
                    if (isHorizontal)
                    {
                        rc.DrawLine(transformedValue, apos + a0, transformedValue, apos + a1, MinorTickPen);

                    }
                    else
                    {
                        rc.DrawLine(apos + a0, transformedValue, apos + a1, transformedValue, MinorTickPen);
                    }
                }
            }

            GetTickPositions(axis, axis.TickStyle, axis.MajorTickSize, axis.Position, out a0, out a1);

            double maxWidth = 0;
            double maxHeight = 0;

            foreach (double value in MajorTickValues)
            {
                if (value < axis.ActualMinimum || value > axis.ActualMaximum)
                    continue;

                double transformedValue = axis.Transform(value);

                if (MajorPen != null)
                {
                    if (isHorizontal)
                    {
                        rc.DrawLine(transformedValue, Plot.PlotArea.Top, transformedValue, Plot.PlotArea.Bottom, MajorPen);

                    }
                    else
                    {
                        rc.DrawLine(Plot.PlotArea.Left, transformedValue, Plot.PlotArea.Right, transformedValue, MajorPen);
                    }
                }

                if (isHorizontal)
                {
                    rc.DrawLine(transformedValue, apos + a0, transformedValue, apos + a1, MajorTickPen);

                }
                else
                {
                    rc.DrawLine(apos + a0, transformedValue, apos + a1, transformedValue, MajorTickPen);
                }

                if (value == 0 && axis.PositionAtZeroCrossing)
                    continue;

                var pt = new ScreenPoint();
                var ha = HorizontalTextAlign.Right;
                var va = VerticalTextAlign.Middle;
                switch (axis.Position)
                {
                    case AxisPosition.Left:
                        pt = new ScreenPoint(apos + a1 - TICK_DIST, transformedValue);
                        GetRotatedAlignments(axis.Angle, HorizontalTextAlign.Right, VerticalTextAlign.Middle, out ha, out va);
                        break;
                    case AxisPosition.Right:
                        pt = new ScreenPoint(apos + a1 + TICK_DIST, transformedValue);
                        GetRotatedAlignments(axis.Angle, HorizontalTextAlign.Left, VerticalTextAlign.Middle, out ha, out va);
                        break;
                    case AxisPosition.Top:
                        pt = new ScreenPoint(transformedValue, apos + a1 - TICK_DIST);
                        GetRotatedAlignments(axis.Angle, HorizontalTextAlign.Center, VerticalTextAlign.Bottom, out ha, out va);
                        break;
                    case AxisPosition.Bottom:
                        pt = new ScreenPoint(transformedValue, apos + a1 + TICK_DIST);
                        GetRotatedAlignments(axis.Angle, HorizontalTextAlign.Center, VerticalTextAlign.Top, out ha, out va);
                        break;

                }

                string text = axis.FormatValue(value);
                var size = rc.DrawMathText(pt, text, Plot.TextColor,
                             axis.FontFamily, axis.FontSize, axis.FontWeight,
                             axis.Angle, ha, va);

                maxWidth = Math.Max(maxWidth, size.Width);
                maxHeight = Math.Max(maxHeight, size.Height);
            }

            if (axis.PositionAtZeroCrossing)
            {
                double t0 = axis.Transform(0);
                if (isHorizontal)
                {
                    rc.DrawLine(t0, Plot.PlotArea.Top, t0, Plot.PlotArea.Bottom, ZeroPen);

                }
                else
                {
                    rc.DrawLine(Plot.PlotArea.Left, t0, Plot.PlotArea.Right, t0, ZeroPen);
                }
            }

            if (axis.ExtraGridlines != null)
            {
                foreach (double value in axis.ExtraGridlines)
                {
                    if (!IsWithin(value, axis.ActualMinimum, axis.ActualMaximum))
                        continue;

                    double transformedValue = axis.Transform(value);
                    if (isHorizontal)
                    {
                        rc.DrawLine(transformedValue, Plot.PlotArea.Top, transformedValue, Plot.PlotArea.Bottom, ExtraPen);

                    }
                    else
                    {
                        rc.DrawLine(Plot.PlotArea.Left, transformedValue, Plot.PlotArea.Right, transformedValue, ExtraPen);
                    }
                }
            }
            if (isHorizontal)
            {
                rc.DrawLine(Plot.PlotArea.Left, apos, Plot.PlotArea.Right, apos, MajorPen);

            }
            else
            {
                rc.DrawLine(apos, Plot.PlotArea.Top, apos, Plot.PlotArea.Bottom, MajorPen);
            }

            if (!String.IsNullOrWhiteSpace(axis.Title))
            {
                // Axis legend
                double ymid = axis.Transform((axis.ActualMinimum + axis.ActualMaximum) / 2);
                double angle = -90;
                var lpt = new ScreenPoint();

                var halign = HorizontalTextAlign.Center;
                var valign = VerticalTextAlign.Top;

                if (axis.PositionAtZeroCrossing)
                {
                    ymid = perpendicularAxis.Transform(perpendicularAxis.ActualMaximum);
                    // valign = axis.IsReversed ? VerticalTextAlign.Top : VerticalTextAlign.Bottom;
                }

                switch (axis.Position)
                {
                    case AxisPosition.Left:
                        lpt = new ScreenPoint(AXIS_LEGEND_DIST, ymid);
                        break;
                    case AxisPosition.Right:
                        lpt = new ScreenPoint(rc.Width - AXIS_LEGEND_DIST, ymid);
                        valign = VerticalTextAlign.Bottom;
                        break;
                    case AxisPosition.Top:
                        lpt = new ScreenPoint(ymid, AXIS_LEGEND_DIST);
                        halign = HorizontalTextAlign.Center;
                        valign = VerticalTextAlign.Top;
                        angle = 0;
                        break;
                    case AxisPosition.Bottom:
                        lpt = new ScreenPoint(ymid, rc.Height - AXIS_LEGEND_DIST);
                        halign = HorizontalTextAlign.Center;
                        valign = VerticalTextAlign.Bottom;
                        angle = 0;
                        break;
                }

                rc.DrawText(lpt, axis.Title, Plot.TextColor,
                            axis.FontFamily, axis.FontSize, axis.FontWeight,
                            angle, halign, valign);
            }
        }

        /// <summary>
        /// Gets the rotated alignments given the specified angle.
        /// </summary>
        /// <param name="angle">The angle.</param>
        /// <param name="defaultHorizontalAlignment">The default horizontal alignment.</param>
        /// <param name="defaultVerticalAlignment">The default vertical alignment.</param>
        /// <param name="ha">The rotated horizontal alignment.</param>
        /// <param name="va">The rotated vertical alignment.</param>
        private static void GetRotatedAlignments(double angle, HorizontalTextAlign defaultHorizontalAlignment, VerticalTextAlign defaultVerticalAlignment,
            out HorizontalTextAlign ha, out VerticalTextAlign va)
        {
            ha = defaultHorizontalAlignment;
            va = defaultVerticalAlignment;

            Debug.Assert(angle <= 180 && angle >= -180, "Axis angle should be in the interval [-180,180] degrees.");

            if (angle > -45 && angle < 45)
                return;
            if (angle > 135 || angle < -135)
            {
                ha = (HorizontalTextAlign)(-(int)defaultHorizontalAlignment);
                va = (VerticalTextAlign)(-(int)defaultVerticalAlignment);
                return;
            }
            if (angle > 45)
            {
                ha = (HorizontalTextAlign)((int)defaultVerticalAlignment);
                va = (VerticalTextAlign)(-(int)defaultHorizontalAlignment);
                return;
            }
            if (angle < -45)
            {
                ha = (HorizontalTextAlign)(-(int)defaultVerticalAlignment);
                va = (VerticalTextAlign)((int)defaultHorizontalAlignment);
                return;
            }
        }
    }
}