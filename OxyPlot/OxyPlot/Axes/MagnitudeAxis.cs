// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MagnitudeAxis.cs" company="OxyPlot">
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
//   Represents a magnitude axis for polar plots.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Axes
{
    using System;

    /// <summary>
    /// Represents a magnitude axis for polar plots.
    /// </summary>
    public class MagnitudeAxis : LinearAxis
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MagnitudeAxis"/> class.
        /// </summary>
        public MagnitudeAxis()
        {
            this.Position = AxisPosition.Bottom;
            this.IsPanEnabled = false;
            this.IsZoomEnabled = false;

            this.MajorGridlineStyle = LineStyle.Solid;
            this.MinorGridlineStyle = LineStyle.Solid;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MagnitudeAxis"/> class.
        /// </summary>
        /// <param name="minimum">
        /// The minimum.
        /// </param>
        /// <param name="maximum">
        /// The maximum.
        /// </param>
        /// <param name="majorStep">
        /// The major step.
        /// </param>
        /// <param name="minorStep">
        /// The minor step.
        /// </param>
        /// <param name="title">
        /// The title.
        /// </param>
        public MagnitudeAxis(
            double minimum = double.NaN,
            double maximum = double.NaN,
            double majorStep = double.NaN,
            double minorStep = double.NaN,
            string title = null)
            : this()
        {
            this.Minimum = minimum;
            this.Maximum = maximum;
            this.MajorStep = majorStep;
            this.MinorStep = minorStep;
            this.Title = title;
        }

        /// <summary>
        /// Gets or sets the midpoint (screen coordinates) of the plot area. This is used by polar coordinate systems.
        /// </summary>
        internal ScreenPoint MidPoint { get; set; }

        /// <summary>
        /// Inverse transform the specified screen point.
        /// </summary>
        /// <param name="x">
        /// The x coordinate.
        /// </param>
        /// <param name="y">
        /// The y coordinate.
        /// </param>
        /// <param name="yaxis">
        /// The y-axis.
        /// </param>
        /// <returns>
        /// The data point.
        /// </returns>
        public override DataPoint InverseTransform(double x, double y, Axis yaxis)
        {
            var angleAxis = yaxis as AngleAxis;
            if (angleAxis == null)
            {
                throw new InvalidOperationException("Polar angle axis not defined!");
            }

            x -= this.MidPoint.x;
            y -= this.MidPoint.y;
            double th = Math.Atan2(y, x);
            double r = Math.Sqrt((x * x) + (y * y));
            x = (r / this.scale) + this.offset;
            y = (th / angleAxis.Scale) + angleAxis.Offset;
            return new DataPoint(x, y);
        }

        /// <summary>
        /// Determines whether the axis is used for X/Y values.
        /// </summary>
        /// <returns>
        /// <c>true</c> if it is an XY axis; otherwise, <c>false</c> .
        /// </returns>
        public override bool IsXyAxis()
        {
            return false;
        }

        /// <summary>
        /// Renders the axis on the specified render context.
        /// </summary>
        /// <param name="rc">The render context.</param>
        /// <param name="model">The model.</param>
        /// <param name="axisLayer">The rendering order.</param>
        /// <param name="pass"></param>
        public override void Render(IRenderContext rc, PlotModel model, AxisLayer axisLayer, int pass)
        {
            if (this.Layer != axisLayer)
            {
                return;
            }

            var r = new MagnitudeAxisRenderer(rc, model);
            r.Render(this, pass);
        }

        /// <summary>
        /// Transforms the specified point to screen coordinates.
        /// </summary>
        /// <param name="x">
        /// The x value (for the current axis).
        /// </param>
        /// <param name="y">
        /// The y value.
        /// </param>
        /// <param name="yaxis">
        /// The y axis.
        /// </param>
        /// <returns>
        /// The transformed point.
        /// </returns>
        public override ScreenPoint Transform(double x, double y, Axis yaxis)
        {
            var angleAxis = yaxis as AngleAxis;
            if (angleAxis == null)
            {
                throw new InvalidOperationException("Polar angle axis not defined!");
            }

            double r = (x - this.Offset) * this.scale;
            double theta = (y - angleAxis.Offset) * angleAxis.Scale;

            return new ScreenPoint(this.MidPoint.x + (r * Math.Cos(theta)), this.MidPoint.y - (r * Math.Sin(theta)));
        }

        /// <summary>
        /// Updates the scale and offset properties of the transform from the specified boundary rectangle.
        /// </summary>
        /// <param name="bounds">
        /// The bounds.
        /// </param>
        internal override void UpdateTransform(OxyRect bounds)
        {
            double x0 = bounds.Left;
            double x1 = bounds.Right;
            double y0 = bounds.Bottom;
            double y1 = bounds.Top;

            this.ScreenMin = new ScreenPoint(x0, y1);
            this.ScreenMax = new ScreenPoint(x1, y0);

            this.MidPoint = new ScreenPoint((x0 + x1) / 2, (y0 + y1) / 2);

            this.ActualMinimum = 0;
            double r = Math.Min(Math.Abs(x1 - x0), Math.Abs(y1 - y0));
            this.scale = 0.5 * r / (this.ActualMaximum - this.ActualMinimum);
            this.Offset = this.ActualMinimum;
        }

    }
}