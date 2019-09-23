// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LineStyleHelper.cs" company="OxyPlot">
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
//   Converts from LineStyle to stroke dash array.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    /// <summary>
    /// Provides functionality to convert from LineStyle to a stroke dash array.
    /// </summary>
    public static class LineStyleHelper
    {
        /// <summary>
        /// Gets the stroke dash array for a given <see cref="LineStyle"/>.
        /// </summary>
        /// <param name="style">
        /// The line style.
        /// </param>
        /// <returns>
        /// A dash array.
        /// </returns>
        public static double[] GetDashArray(this LineStyle style)
        {
            switch (style)
            {
                case LineStyle.Solid:
                    return null;
                case LineStyle.Dash:
                    return new double[] { 4, 1 };
                case LineStyle.Dot:
                    return new double[] { 1, 1 };
                case LineStyle.DashDot:
                    return new double[] { 4, 1, 1, 1 };
                case LineStyle.DashDashDot:
                    return new double[] { 4, 1, 4, 1, 1, 1 };
                case LineStyle.DashDotDot:
                    return new double[] { 4, 1, 1, 1, 1, 1 };
                case LineStyle.DashDashDotDot:
                    return new double[] { 4, 1, 4, 1, 1, 1, 1, 1 };
                case LineStyle.LongDash:
                    return new double[] { 10, 1 };
                case LineStyle.LongDashDot:
                    return new double[] { 10, 1, 1, 1 };
                case LineStyle.LongDashDotDot:
                    return new double[] { 10, 1, 1, 1, 1, 1 };
                default:
                    return null;
            }
        }

    }
}