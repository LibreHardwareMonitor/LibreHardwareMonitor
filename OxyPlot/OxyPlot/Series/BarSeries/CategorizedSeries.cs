// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CategorizedSeries.cs" company="OxyPlot">
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
//   Base class for series where the items are categorized.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Series
{
    using System.Collections.Generic;

    using OxyPlot.Axes;

    /// <summary>
    /// Base class for series where the items are categorized.
    /// </summary>
    public abstract class CategorizedSeries : XYAxisSeries
    {
        /// <summary>
        /// Gets or sets the width/height of the columns/bars (as a fraction of the available space).
        /// </summary>
        /// <returns>
        /// The fractional width.
        /// </returns>
        /// <value>
        /// The width of the bars.
        /// </value>
        /// <remarks>
        /// The available space will be determined by the GapWidth of the CategoryAxis used by this series.
        /// </remarks>
        internal abstract double GetBarWidth();

        /// <summary>
        /// Gets the items of this series.
        /// </summary>
        /// <returns>
        /// The items.
        /// </returns>
        protected internal abstract IList<CategorizedItem> GetItems();

        /// <summary>
        /// Gets the actual bar width/height of the items in this series.
        /// </summary>
        /// <returns>
        /// The width or height.
        /// </returns>
        /// <remarks>
        /// The actual width is also influenced by the GapWidth of the CategoryAxis used by this series.
        /// </remarks>
        protected abstract double GetActualBarWidth();

        /// <summary>
        /// Gets the category axis.
        /// </summary>
        /// <returns>
        /// The category axis.
        /// </returns>
        protected abstract CategoryAxis GetCategoryAxis();

    }
}