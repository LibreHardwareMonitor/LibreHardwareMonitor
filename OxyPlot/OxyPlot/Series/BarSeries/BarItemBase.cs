// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BarItemBase.cs" company="OxyPlot">
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
//   Represents an item used in the BarSeriesBase.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Series
{
    /// <summary>
    /// Represents an item used in the BarSeriesBase.
    /// </summary>
    public abstract class BarItemBase : CategorizedItem, ICodeGenerating
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BarItemBase"/> class. Initializes a new instance of the <see cref="BarItem"/> class.
        /// </summary>
        protected BarItemBase()
        {
            // Label = null;
            this.Value = double.NaN;
            this.Color = null;
        }

        /// <summary>
        /// Gets or sets the color of the item.
        /// </summary>
        /// <remarks>
        /// If the color is not specified (default), the color of the series will be used.
        /// </remarks>
        public OxyColor Color { get; set; }

        /// <summary>
        /// Gets or sets the value of the item.
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Returns c# code that generates this instance.
        /// </summary>
        /// <returns>
        /// C# code.
        /// </returns>
        public virtual string ToCode()
        {
            if (this.Color != null)
            {
                return CodeGenerator.FormatConstructor(
                    this.GetType(), "{0},{1},{2}", this.Value, this.CategoryIndex, this.Color.ToCode());
            }

            if (this.CategoryIndex != -1)
            {
                return CodeGenerator.FormatConstructor(this.GetType(), "{0},{1}", this.Value, this.CategoryIndex);
            }

            return CodeGenerator.FormatConstructor(this.GetType(), "{0}", this.Value);
        }

    }
}