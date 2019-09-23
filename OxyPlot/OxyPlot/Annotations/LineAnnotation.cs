// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LineAnnotation.cs" company="OxyPlot">
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
//   Specify the orientation of the annotation text
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Annotations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using OxyPlot.Axes;

    /// <summary>
    /// Specifes the orientation of the annotation text
    /// </summary>
    public enum AnnotationTextOrientation
    {
        /// <summary>
        /// Horizontal text.
        /// </summary>
        Horizontal,

        /// <summary>
        /// Vertical text.
        /// </summary>
        Vertical,

        /// <summary>
        /// Oriented along the line.
        /// </summary>
        AlongLine
    }

    /// <summary>
    /// Represents a line annotation.
    /// </summary>
    public class LineAnnotation : TextualAnnotation
    {
        /// <summary>
        /// The points of the line, transformed to screen coordinates.
        /// </summary>
        private IList<ScreenPoint> screenPoints;

        /// <summary>
        /// Initializes a new instance of the <see cref = "LineAnnotation" /> class.
        /// </summary>
        public LineAnnotation()
        {
            this.Type = LineAnnotationType.LinearEquation;
            this.MinimumX = double.MinValue;
            this.MaximumX = double.MaxValue;
            this.MinimumY = double.MinValue;
            this.MaximumY = double.MaxValue;
            this.Color = OxyColors.Blue;
            this.StrokeThickness = 1;
            this.LineStyle = LineStyle.Dash;
            this.LineJoin = OxyPenLineJoin.Miter;
            this.ClipByXAxis = true;
            this.ClipByYAxis = true;

            this.TextPosition = 1;
            this.TextOrientation = AnnotationTextOrientation.AlongLine;
            this.TextMargin = 12;
            this.TextHorizontalAlignment = HorizontalAlignment.Right;
            this.TextVerticalAlignment = VerticalAlignment.Top;
        }

        /// <summary>
        /// Gets or sets the color of the line.
        /// </summary>
        public OxyColor Color { get; set; }

        /// <summary>
        /// Gets or sets the y=f(x) equation when Type is Equation.
        /// </summary>
        public Func<double, double> Equation { get; set; }

        /// <summary>
        /// Gets or sets the y-intercept when Type is LinearEquation.
        /// </summary>
        /// <value>The intercept value.</value>
        /// <remarks>
        /// Linear equation y-intercept (the b in y=mx+b).
        /// http://en.wikipedia.org/wiki/Linear_equation
        /// </remarks>
        public double Intercept { get; set; }

        /// <summary>
        /// Gets or sets the line join.
        /// </summary>
        /// <value>The line join.</value>
        public OxyPenLineJoin LineJoin { get; set; }

        /// <summary>
        /// Gets or sets the line style.
        /// </summary>
        /// <value>The line style.</value>
        public LineStyle LineStyle { get; set; }

        /// <summary>
        /// Gets or sets the maximum X coordinate for the line.
        /// </summary>
        public double MaximumX { get; set; }

        /// <summary>
        /// Gets or sets the maximum Y coordinate for the line.
        /// </summary>
        public double MaximumY { get; set; }

        /// <summary>
        /// Gets or sets the minimum X coordinate for the line.
        /// </summary>
        public double MinimumX { get; set; }

        /// <summary>
        /// Gets or sets the minimum Y coordinate for the line.
        /// </summary>
        public double MinimumY { get; set; }

        /// <summary>
        /// Gets or sets the slope when Type is LinearEquation.
        /// </summary>
        /// <value>The slope value.</value>
        /// <remarks>
        /// Linear equation slope (the m in y=mx+b)
        /// http://en.wikipedia.org/wiki/Linear_equation
        /// </remarks>
        public double Slope { get; set; }

        /// <summary>
        /// Gets or sets the stroke thickness.
        /// </summary>
        /// <value>The stroke thickness.</value>
        public double StrokeThickness { get; set; }

        /// <summary>
        /// Gets or sets the text horizontal alignment.
        /// </summary>
        /// <value>The text horizontal alignment.</value>
        public HorizontalAlignment TextHorizontalAlignment { get; set; }

        /// <summary>
        /// Gets or sets the text margin (along the line).
        /// </summary>
        /// <value>The text margin.</value>
        public double TextMargin { get; set; }

        /// <summary>
        /// Gets or sets the text padding (in the direction of the text).
        /// </summary>
        /// <value>The text padding.</value>
        public double TextPadding { get; set; }

        /// <summary>
        /// Gets or sets the text orientation.
        /// </summary>
        /// <value>The text orientation.</value>
        public AnnotationTextOrientation TextOrientation { get; set; }

        /// <summary>
        /// Gets or sets the text position fraction.
        /// </summary>
        /// <value>The text position in the interval [0,1].</value>
        /// <remarks>
        /// Positions smaller than 0.25 are left aligned at the start of the line
        /// Positions larger than 0.75 are right aligned at the end of the line
        /// Other positions are center aligned at the specified position
        /// </remarks>
        public double TextPosition { get; set; }

        /// <summary>
        /// Gets or sets the vertical alignment of text (above or below the line).
        /// </summary>
        public VerticalAlignment TextVerticalAlignment { get; set; }

        /// <summary>
        /// Gets or sets the type of line equation.
        /// </summary>
        public LineAnnotationType Type { get; set; }

        /// <summary>
        /// Gets or sets the X position for vertical lines (only for Type==Vertical).
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Gets or sets the Y position for horizontal lines (only for Type==Horizontal)
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to clip the annotation line by the X axis range.
        /// </summary>
        /// <value><c>true</c> if clipping by the X axis is enabled; otherwise, <c>false</c>.</value>
        public bool ClipByXAxis { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to clip the annotation line by the Y axis range.
        /// </summary>
        /// <value><c>true</c> if clipping by the Y axis is enabled; otherwise, <c>false</c>.</value>
        public bool ClipByYAxis { get; set; }

        /// <summary>
        /// Renders the line annotation.
        /// </summary>
        /// <param name="rc">
        /// The render context.
        /// </param>
        /// <param name="model">
        /// The plot model.
        /// </param>
        public override void Render(IRenderContext rc, PlotModel model)
        {
            base.Render(rc, model);

            bool aliased = false;

            double actualMinimumX = Math.Max(this.MinimumX, this.XAxis.ActualMinimum);
            double actualMaximumX = Math.Min(this.MaximumX, this.XAxis.ActualMaximum);
            double actualMinimumY = Math.Max(this.MinimumY, this.YAxis.ActualMinimum);
            double actualMaximumY = Math.Min(this.MaximumY, this.YAxis.ActualMaximum);

            if (!this.ClipByXAxis)
            {
                double right = XAxis.InverseTransform(PlotModel.PlotArea.Right);
                double left = XAxis.InverseTransform(PlotModel.PlotArea.Left);
                actualMaximumX = Math.Max(left, right);
                actualMinimumX = Math.Min(left, right);
            }

            if (!this.ClipByYAxis)
            {
                double bottom = YAxis.InverseTransform(PlotModel.PlotArea.Bottom);
                double top = YAxis.InverseTransform(PlotModel.PlotArea.Top);
                actualMaximumY = Math.Max(top, bottom);
                actualMinimumY = Math.Min(top, bottom);
            }

            // y=f(x)
            Func<double, double> fx = null;

            // x=f(y)
            Func<double, double> fy = null;

            switch (this.Type)
            {
                case LineAnnotationType.Horizontal:
                    fx = x => this.Y;
                    break;
                case LineAnnotationType.Vertical:
                    fy = y => this.X;
                    break;
                case LineAnnotationType.EquationY:
                    fx = this.Equation;
                    break;
                case LineAnnotationType.EquationX:
                    fy = this.Equation;
                    break;
                default:
                    fx = x => (this.Slope * x) + this.Intercept;
                    break;
            }

            var points = new List<DataPoint>();

            bool isCurvedLine = !(this.XAxis is LinearAxis) || !(this.YAxis is LinearAxis) || this.Type == LineAnnotationType.EquationY;

            if (!isCurvedLine)
            {
                // we only need to calculate two points if it is a straight line
                if (fx != null)
                {
                    points.Add(new DataPoint(actualMinimumX, fx(actualMinimumX)));
                    points.Add(new DataPoint(actualMaximumX, fx(actualMaximumX)));
                }
                else if (fy != null)
                {
                    points.Add(new DataPoint(fy(actualMinimumY), actualMinimumY));
                    points.Add(new DataPoint(fy(actualMaximumY), actualMaximumY));
                }

                if (this.Type == LineAnnotationType.Horizontal || this.Type == LineAnnotationType.Vertical)
                {
                    // use aliased line drawing for horizontal and vertical lines
                    aliased = true;
                }
            }
            else
            {
                if (fx != null)
                {
                    double x = actualMinimumX;

                    // todo: the step size should be adaptive
                    double dx = (actualMaximumX - actualMinimumX) / 100;
                    while (true)
                    {
                        points.Add(new DataPoint(x, fx(x)));
                        if (x > actualMaximumX)
                        {
                            break;
                        }

                        x += dx;
                    }
                }
                else if (fy != null)
                {
                    double y = actualMinimumY;

                    // todo: the step size should be adaptive
                    double dy = (actualMaximumY - actualMinimumY) / 100;
                    while (true)
                    {
                        points.Add(new DataPoint(fy(y), y));
                        if (y > actualMaximumY)
                        {
                            break;
                        }

                        y += dy;
                    }
                }
            }

            // transform to screen coordinates
            this.screenPoints = points.Select(p => this.Transform(p)).ToList();

            // clip to the area defined by the axes
            var clippingRectangle = OxyRect.Create(
                this.ClipByXAxis ? this.XAxis.ScreenMin.X : PlotModel.PlotArea.Left,
                this.ClipByYAxis ? this.YAxis.ScreenMin.Y : PlotModel.PlotArea.Top,
                this.ClipByXAxis ? this.XAxis.ScreenMax.X : PlotModel.PlotArea.Right,
                this.ClipByYAxis ? this.YAxis.ScreenMax.Y : PlotModel.PlotArea.Bottom);

            const double MinimumSegmentLength = 4;

            IList<ScreenPoint> clippedPoints = null;

            rc.DrawClippedLine(
               this.screenPoints,
               clippingRectangle,
               MinimumSegmentLength * MinimumSegmentLength,
               this.GetSelectableColor(this.Color),
               this.StrokeThickness,
               this.LineStyle,
                this.LineJoin,
                aliased,
                pts => clippedPoints = pts);

            ScreenPoint position;
            double angle;
            double margin = this.TextMargin;

            if (this.TextHorizontalAlignment == HorizontalAlignment.Center)
            {
                margin = 0;
            }
            else
            {
                margin *= this.TextPosition < 0.5 ? 1 : -1;
            }

            if (clippedPoints != null && GetPointAtRelativeDistance(clippedPoints, this.TextPosition, margin, out position, out angle))
            {
                if (angle < -90)
                {
                    angle += 180;
                }

                if (angle > 90)
                {
                    angle -= 180;
                }

                switch (this.TextOrientation)
                {
                    case AnnotationTextOrientation.Horizontal:
                        angle = 0;
                        break;
                    case AnnotationTextOrientation.Vertical:
                        angle = -90;
                        break;
                }

                // Apply 'padding' to the position
                var angleInRadians = angle / 180 * Math.PI;
                var f = 1;

                if (this.TextHorizontalAlignment == HorizontalAlignment.Right)
                {
                    f = -1;
                }

                if (this.TextHorizontalAlignment == HorizontalAlignment.Center)
                {
                    f = 0;
                }

                position.X += f * this.TextPadding * Math.Cos(angleInRadians);
                position.Y += f * this.TextPadding * Math.Sin(angleInRadians);

                var cs = new CohenSutherlandClipping(clippingRectangle);
                if (!string.IsNullOrEmpty(this.Text) && cs.IsInside(position))
                {
                    rc.DrawClippedText(
                        clippingRectangle,
                        position,
                        this.Text,
                        this.ActualTextColor,
                        this.ActualFont,
                        this.ActualFontSize,
                        this.ActualFontWeight,
                        angle,
                        this.TextHorizontalAlignment,
                        this.TextVerticalAlignment);
                }
            }
        }

        /// <summary>
        /// Tests if the plot element is hit by the specified point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>
        /// A hit test result.
        /// </returns>
        protected internal override HitTestResult HitTest(ScreenPoint point, double tolerance)
        {
            var nearestPoint = ScreenPointHelper.FindNearestPointOnPolyline(point, this.screenPoints);
            double dist = (point - nearestPoint).Length;
            if (dist < tolerance)
            {
                return new HitTestResult(nearestPoint);
            }

            return null;
        }

        /// <summary>
        /// Gets the point on a curve at the specified relative distance along the curve.
        /// </summary>
        /// <param name="pts">
        /// The curve points.
        /// </param>
        /// <param name="p">
        /// The relative distance along the curve.
        /// </param>
        /// <param name="margin">
        /// The margins.
        /// </param>
        /// <param name="position">
        /// The position.
        /// </param>
        /// <param name="angle">
        /// The angle.
        /// </param>
        /// <returns>
        /// True if a position was found.
        /// </returns>
        private static bool GetPointAtRelativeDistance(
            IList<ScreenPoint> pts, double p, double margin, out ScreenPoint position, out double angle)
        {
            if (pts == null || pts.Count == 0)
            {
                position = new ScreenPoint();
                angle = 0;
                return false;
            }

            double length = 0;
            for (int i = 1; i < pts.Count; i++)
            {
                length += (pts[i] - pts[i - 1]).Length;
            }

            double l = (length * p) + margin;
            length = 0;
            for (int i = 1; i < pts.Count; i++)
            {
                double dl = (pts[i] - pts[i - 1]).Length;
                if (l >= length && l <= length + dl)
                {
                    double f = (l - length) / dl;
                    double x = (pts[i].X * f) + (pts[i - 1].X * (1 - f));
                    double y = (pts[i].Y * f) + (pts[i - 1].Y * (1 - f));
                    position = new ScreenPoint(x, y);
                    double dx = pts[i].X - pts[i - 1].X;
                    double dy = pts[i].Y - pts[i - 1].Y;
                    angle = Math.Atan2(dy, dx) / Math.PI * 180;
                    return true;
                }

                length += dl;
            }

            position = pts[0];
            angle = 0;
            return false;
        }
    }
}