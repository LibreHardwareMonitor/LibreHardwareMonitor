// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OxyPen.cs" company="OxyPlot">
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
//   Describes a pen in terms of color, thickness, line style and line join type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System;

    /// <summary>
    /// Describes a pen in terms of color, thickness, line style and line join type.
    /// </summary>
    public class OxyPen
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OxyPen"/> class.
        /// </summary>
        /// <param name="color">
        /// The color.
        /// </param>
        /// <param name="thickness">
        /// The thickness.
        /// </param>
        /// <param name="lineStyle">
        /// The line style.
        /// </param>
        /// <param name="lineJoin">
        /// The line join.
        /// </param>
        public OxyPen(
            OxyColor color,
            double thickness = 1.0,
            LineStyle lineStyle = LineStyle.Solid,
            OxyPenLineJoin lineJoin = OxyPenLineJoin.Miter)
        {
            this.Color = color;
            this.Thickness = thickness;
            this.DashArray = LineStyleHelper.GetDashArray(lineStyle);
            this.LineStyle = lineStyle;
            this.LineJoin = lineJoin;
        }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        /// <value>The color.</value>
        public OxyColor Color { get; set; }

        /// <summary>
        /// Gets or sets the dash array.
        /// </summary>
        /// <value>The dash array.</value>
        public double[] DashArray { get; set; }

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
        /// Gets or sets the thickness.
        /// </summary>
        /// <value>The thickness.</value>
        public double Thickness { get; set; }

        /// <summary>
        /// Creates the specified pen.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <param name="thickness">The thickness.</param>
        /// <param name="lineStyle">The line style.</param>
        /// <param name="lineJoin">The line join.</param>
        /// <returns>A pen.</returns>
        public static OxyPen Create(
            OxyColor color,
            double thickness,
            LineStyle lineStyle = LineStyle.Solid,
            OxyPenLineJoin lineJoin = OxyPenLineJoin.Miter)
        {
            if (color == null || lineStyle == LineStyle.None || Math.Abs(thickness) < double.Epsilon)
            {
                return null;
            }

            return new OxyPen(color, thickness, lineStyle, lineJoin);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int result = this.Color.GetHashCode();
                result = (result * 397) ^ this.Thickness.GetHashCode();
                result = (result * 397) ^ this.LineStyle.GetHashCode();
                result = (result * 397) ^ this.LineJoin.GetHashCode();
                return result;
            }
        }

    }
}