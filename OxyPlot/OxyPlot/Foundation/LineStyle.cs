// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LineStyle.cs" company="OxyPlot">
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
//   Enumeration of line styles.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    /// <summary>
    /// Specifies the style of a line.
    /// </summary>
    public enum LineStyle
    {
        /// <summary>
        /// The solid line style.
        /// </summary>
        Solid,

        /// <summary>
        /// The dash line style.
        /// </summary>
        Dash,

        /// <summary>
        /// The dot line style.
        /// </summary>
        Dot,

        /// <summary>
        /// The dash dot line style.
        /// </summary>
        DashDot,

        /// <summary>
        /// The dash dash dot line style.
        /// </summary>
        DashDashDot,

        /// <summary>
        /// The dash dot dot line style.
        /// </summary>
        DashDotDot,

        /// <summary>
        /// The dash dash dot dot line style.
        /// </summary>
        DashDashDotDot,

        /// <summary>
        /// The long dash line style.
        /// </summary>
        LongDash,

        /// <summary>
        /// The long dash dot line style.
        /// </summary>
        LongDashDot,

        /// <summary>
        /// The long dash dot dot line style.
        /// </summary>
        LongDashDotDot,

        /// <summary>
        /// The hidden line style.
        /// </summary>
        None,

        /// <summary>
        /// The undefined line style.
        /// </summary>
        Undefined
    }
}