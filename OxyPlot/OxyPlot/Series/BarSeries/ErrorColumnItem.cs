// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ErrorColumnItem.cs" company="OxyPlot">
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
//   Represents an item used in the ErrorColumnSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Series
{
    /// <summary>
    /// Represents an item used in the ErrorColumnSeries.
    /// </summary>
    public class ErrorColumnItem : ColumnItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorColumnItem"/> class.
        /// </summary>
        public ErrorColumnItem()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorColumnItem"/> class.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <param name="error">
        /// The error.
        /// </param>
        /// <param name="categoryIndex">
        /// Index of the category.
        /// </param>
        /// <param name="color">
        /// The color.
        /// </param>
        public ErrorColumnItem(double value, double error, int categoryIndex = -1, OxyColor color = null)
        {
            this.Value = value;
            this.Error = error;
            this.CategoryIndex = categoryIndex;
            this.Color = color;
        }

        /// <summary>
        /// Gets or sets the error of the item.
        /// </summary>
        public double Error { get; set; }

        /// <summary>
        /// Returns c# code that generates this instance.
        /// </summary>
        /// <returns>
        /// C# code.
        /// </returns>
        public override string ToCode()
        {
            if (this.Color != null)
            {
                return CodeGenerator.FormatConstructor(
                    this.GetType(), "{0},{1},{2},{3}", this.Value, this.Error, this.CategoryIndex, this.Color.ToCode());
            }

            if (this.CategoryIndex != -1)
            {
                return CodeGenerator.FormatConstructor(
                    this.GetType(), "{0},{1},{2}", this.Value, this.Error, this.CategoryIndex);
            }

            return CodeGenerator.FormatConstructor(this.GetType(), "{0},{1}", this.Value, this.Error);
        }

    }
}