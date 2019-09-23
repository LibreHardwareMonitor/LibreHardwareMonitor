// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HitTestResult.cs" company="OxyPlot">
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
//   Represents a hit test result.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    /// <summary>
    /// Represents a hit test result.
    /// </summary>
    public class HitTestResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HitTestResult"/> class.
        /// </summary>
        public HitTestResult()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HitTestResult"/> class.
        /// </summary>
        /// <param name="nhp">The nearest hit point.</param>
        /// <param name="item">The item.</param>
        /// <param name="index">The index.</param>
        public HitTestResult(ScreenPoint nhp, object item = null, double index = 0)
        {
            this.NearestHitPoint = nhp;
            this.Item = item;
            this.Index = index;
        }

        /// <summary>
        /// Gets or sets the index of the hit (if available).
        /// </summary>
        /// <value> The index. </value>
        /// <remarks>
        /// If the hit was in the middle between point 1 and 2, index = 1.5.
        /// </remarks>
        public double Index { get; set; }

        /// <summary>
        /// Gets or sets the item of the hit.
        /// </summary>
        /// <value> The item. </value>
        public object Item { get; set; }

        /// <summary>
        /// Gets or sets the position of the nearest hit point.
        /// </summary>
        /// <value> The nearest hit point. </value>
        public ScreenPoint NearestHitPoint { get; set; }

    }
}