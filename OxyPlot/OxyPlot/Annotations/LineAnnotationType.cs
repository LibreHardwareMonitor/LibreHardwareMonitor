// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LineAnnotationType.cs" company="OxyPlot">
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
//   The line annotation type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Annotations
{
    /// <summary>
    /// Specifies the definition of the line in a <see cref="LineAnnotation"/>.
    /// </summary>
    public enum LineAnnotationType
    {
        /// <summary>
        /// Horizontal line given by the Y property
        /// </summary>
        Horizontal,

        /// <summary>
        /// Vertical line given by the X property
        /// </summary>
        Vertical,

        /// <summary>
        /// Linear equation y=mx+b given by the Slope and Intercept properties
        /// </summary>
        LinearEquation,

        /// <summary>
        /// Curve equation x=f(y) given by the Equation property
        /// </summary>
        EquationX,

        /// <summary>
        /// Curve equation y=f(x) given by the Equation property
        /// </summary>
        EquationY
    }
}