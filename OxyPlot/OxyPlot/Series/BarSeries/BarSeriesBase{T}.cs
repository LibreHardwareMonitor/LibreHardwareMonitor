// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BarSeriesBase{T}.cs" company="OxyPlot">
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
//   Generic base class that provides common properties and methods for the BarSeries and ColumnSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Series
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Generic base class that provides common properties and methods for the BarSeries and ColumnSeries.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the items.
    /// </typeparam>
    public abstract class BarSeriesBase<T> : BarSeriesBase
        where T : BarItemBase, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BarSeriesBase{T}"/> class. Initializes a new instance of the <see cref="BarSeriesBase&lt;T&gt;"/> class.
        /// </summary>
        protected BarSeriesBase()
        {
            this.Items = new List<T>();
        }

        /// <summary>
        /// Gets the items.
        /// </summary>
        /// <value>
        /// The items.
        /// </value>
        public IList<T> Items { get; private set; }

        /// <summary>
        /// Gets the items of this series.
        /// </summary>
        /// <returns>
        /// The items.
        /// </returns>
        protected internal override IList<CategorizedItem> GetItems()
        {
            return this.Items.Cast<CategorizedItem>().ToList();
        }

        /// <summary>
        /// Updates the data.
        /// </summary>
        protected internal override void UpdateData()
        {
            if (this.ItemsSource == null)
            {
                return;
            }

            var dest = new List<T>();

            // Using reflection to add points
            var filler = new ListFiller<T>();
            filler.Add(this.ValueField, (item, value) => item.Value = Convert.ToDouble(value));
            filler.Add(this.ColorField, (item, value) => item.Color = (OxyColor)value);
            filler.Fill(dest, this.ItemsSource);
            this.Items = dest;
        }

        /// <summary>
        /// Gets the item at the specified index.
        /// </summary>
        /// <param name="i">
        /// The index of the item.
        /// </param>
        /// <returns>
        /// The item of the index.
        /// </returns>
        protected override object GetItem(int i)
        {
            if (this.ItemsSource != null || this.Items == null || this.Items.Count == 0)
            {
                return base.GetItem(i);
            }

            return this.Items[i];
        }

    }
}