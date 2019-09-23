// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ItemsSeries.cs" company="OxyPlot">
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
//   Abstract base class for series that can contain items.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Series
{
    using System.Collections;
    using System.Linq;

    /// <summary>
    /// Abstract base class for series that can contain items.
    /// </summary>
    public abstract class ItemsSeries : Series
    {
        /// <summary>
        /// Gets or sets the items source.
        /// </summary>
        /// <value> The items source. </value>
        [CodeGeneration(false)]
        public IEnumerable ItemsSource { get; set; }

        /// <summary>
        /// Updates the valid items
        /// </summary>
        protected internal override void UpdateValidData()
        {
        }

        /// <summary>
        /// Gets the item for the specified index.
        /// </summary>
        /// <param name="itemsSource"> The items source. </param>
        /// <param name="index"> The index. </param>
        /// <returns> The get item. </returns>
        /// <remarks>
        /// Returns null if ItemsSource is not set, or the index is outside the boundaries.
        /// </remarks>
        protected static object GetItem(IEnumerable itemsSource, int index)
        {
            if (itemsSource == null || index < 0)
            {
                return null;
            }

            var list = itemsSource as IList;
            if (list != null)
            {
                if (index < list.Count && index >= 0)
                {
                    return list[index];
                }

                return null;
            }

            var i = 0;
            return itemsSource.Cast<object>().FirstOrDefault(item => i++ == index);
        }

        /// <summary>
        /// Gets the item at the specified index.
        /// </summary>
        /// <param name="i"> The index of the item. </param>
        /// <returns> The item of the index. </returns>
        protected virtual object GetItem(int i)
        {
            return GetItem(this.ItemsSource, i);
        }

    }
}