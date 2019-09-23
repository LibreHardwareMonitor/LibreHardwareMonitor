// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AxisRendererBase.cs" company="OxyPlot">
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
//   The axis renderer base.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System.Collections.Generic;

    using OxyPlot.Axes;

    /// <summary>
    /// Provides an abstract base class for axis renderers.
    /// </summary>
    public abstract class AxisRendererBase
    {
        /// <summary>
        /// The plot.
        /// </summary>
        protected readonly PlotModel Plot;

        /// <summary>
        /// The render context.
        /// </summary>
        protected readonly IRenderContext rc;

        /// <summary>
        /// The axis lines pen.
        /// </summary>
        protected OxyPen AxislinePen;

        /// <summary>
        /// The extra grid lines pen.
        /// </summary>
        protected OxyPen ExtraPen;

        /// <summary>
        /// The major label values.
        /// </summary>
        protected IList<double> MajorLabelValues;

        /// <summary>
        /// The major grid lines pen.
        /// </summary>
        protected OxyPen MajorPen;

        /// <summary>
        /// The major tick pen.
        /// </summary>
        protected OxyPen MajorTickPen;

        /// <summary>
        /// The major tick values.
        /// </summary>
        protected IList<double> MajorTickValues;

        /// <summary>
        /// The minor grid lines pen.
        /// </summary>
        protected OxyPen MinorPen;

        /// <summary>
        /// The minor tick pen.
        /// </summary>
        protected OxyPen MinorTickPen;

        /// <summary>
        /// The minor tick values.
        /// </summary>
        protected IList<double> MinorTickValues;

        /// <summary>
        /// The zero grid line pen.
        /// </summary>
        protected OxyPen ZeroPen;

        /// <summary>
        /// Initializes a new instance of the <see cref="AxisRendererBase"/> class.
        /// </summary>
        /// <param name="rc">
        /// The render context.
        /// </param>
        /// <param name="plot">
        /// The plot.
        /// </param>
        protected AxisRendererBase(IRenderContext rc, PlotModel plot)
        {
            this.Plot = plot;
            this.rc = rc;
        }

        /// <summary>
        /// Renders the specified axis.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <param name="pass">The pass.</param>
        public virtual void Render(Axis axis, int pass)
        {
            if (axis == null)
            {
                return;
            }

            axis.GetTickValues(out this.MajorLabelValues, out this.MajorTickValues, out this.MinorTickValues);
            this.CreatePens(axis);
        }

        /// <summary>
        /// The create pens.
        /// </summary>
        /// <param name="axis">
        /// The axis.
        /// </param>
        protected void CreatePens(Axis axis)
        {
            this.MinorPen = OxyPen.Create(axis.MinorGridlineColor, axis.MinorGridlineThickness, axis.MinorGridlineStyle);
            this.MajorPen = OxyPen.Create(axis.MajorGridlineColor, axis.MajorGridlineThickness, axis.MajorGridlineStyle);
            this.MinorTickPen = OxyPen.Create(axis.TicklineColor, axis.MinorGridlineThickness);
            this.MajorTickPen = OxyPen.Create(axis.TicklineColor, axis.MajorGridlineThickness);
            this.ZeroPen = OxyPen.Create(axis.TicklineColor, axis.MajorGridlineThickness);
            this.ExtraPen = OxyPen.Create(axis.ExtraGridlineColor, axis.ExtraGridlineThickness, axis.ExtraGridlineStyle);
            this.AxislinePen = OxyPen.Create(axis.AxislineColor, axis.AxislineThickness, axis.AxislineStyle);
        }

        /// <summary>
        /// The get tick positions.
        /// </summary>
        /// <param name="axis">
        /// The axis.
        /// </param>
        /// <param name="glt">
        /// The glt.
        /// </param>
        /// <param name="ticksize">
        /// The ticksize.
        /// </param>
        /// <param name="position">
        /// The position.
        /// </param>
        /// <param name="x0">
        /// The x 0.
        /// </param>
        /// <param name="x1">
        /// The x 1.
        /// </param>
        protected void GetTickPositions(
            Axis axis, TickStyle glt, double ticksize, AxisPosition position, out double x0, out double x1)
        {
            x0 = 0;
            x1 = 0;
            bool isTopOrLeft = position == AxisPosition.Top || position == AxisPosition.Left;
            double sign = isTopOrLeft ? -1 : 1;
            switch (glt)
            {
                case TickStyle.Crossing:
                    x0 = -ticksize * sign * 0.75;
                    x1 = ticksize * sign * 0.75;
                    break;
                case TickStyle.Inside:
                    x0 = -ticksize * sign;
                    break;
                case TickStyle.Outside:
                    x1 = ticksize * sign;
                    break;
            }
        }

        /// <summary>
        /// Determines whether the specified value is within the specified range.
        /// </summary>
        /// <param name="d">The value to check.</param>
        /// <param name="min">The minium value of the range.</param>
        /// <param name="max">The maximum value of the range.</param>
        /// <returns>
        ///  <c>true</c> if the specified value is within the range; otherwise, <c>false</c>.
        /// </returns>
        protected bool IsWithin(double d, double min, double max)
        {
            if (d < min)
            {
                return false;
            }

            if (d > max)
            {
                return false;
            }

            return true;
        }

    }
}