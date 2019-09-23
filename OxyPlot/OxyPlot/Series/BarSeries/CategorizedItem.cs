// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CategorizedItem.cs" company="OxyPlot">
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
//   Represents an item in a CategorizedSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Series
{
    /// <summary>
    /// Represents an item in a CategorizedSeries.
    /// </summary>
    public abstract class CategorizedItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CategorizedItem"/> class. Initializes a new instance of the <see cref="CategorizedItem"/> class.
        /// </summary>
        protected CategorizedItem()
        {
            this.CategoryIndex = -1;
        }

        /// <summary>
        /// Gets or sets the index of the category.
        /// </summary>
        /// <value>
        /// The index of the category.
        /// </value>
        public int CategoryIndex { get; set; }

        /// <summary>
        /// Gets the index of the category.
        /// </summary>
        /// <param name="defaultIndex">
        /// The default index.
        /// </param>
        /// <returns>
        /// The index.
        /// </returns>
        internal int GetCategoryIndex(int defaultIndex)
        {
            if (this.CategoryIndex < 0)
            {
                return defaultIndex;
            }

            return this.CategoryIndex;
        }

    }
}