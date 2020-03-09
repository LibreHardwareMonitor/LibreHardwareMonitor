// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LabelPlacement.cs" company="OxyPlot">
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
//   Placement of the labels.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Series
{
    /// <summary>
    /// Placement of the labels.
    /// </summary>
    public enum LabelPlacement
    {
        /// <summary>
        /// Placed outside the bar.
        /// </summary>
        Outside,

        /// <summary>
        /// Placed inside the bar.
        /// </summary>
        Inside,

        /// <summary>
        /// Placed inside in the middle/center of the bar.
        /// </summary>
        Middle,

        /// <summary>
        /// Placed inside at the base of the bar.
        /// </summary>
        Base
    }
}