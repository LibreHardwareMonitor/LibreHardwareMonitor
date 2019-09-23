// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalBarItem.cs" company="OxyPlot">
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
//   Represents an item in an IntervalBarSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Series
{
    /// <summary>
    /// Represents an item in an IntervalBarSeries.
    /// </summary>
    public class IntervalBarItem : CategorizedItem, ICodeGenerating
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IntervalBarItem"/> class.
        /// </summary>
        public IntervalBarItem()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IntervalBarItem"/> class.
        /// </summary>
        /// <param name="start">
        /// The start.
        /// </param>
        /// <param name="end">
        /// The end.
        /// </param>
        /// <param name="title">
        /// The title.
        /// </param>
        /// <param name="color">
        /// The color.
        /// </param>
        public IntervalBarItem(double start, double end, string title = null, OxyColor color = null)
        {
            this.Start = start;
            this.End = end;
            this.Title = title;
            this.Color = color;
        }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        public OxyColor Color { get; set; }

        /// <summary>
        /// Gets or sets the end value.
        /// </summary>
        public double End { get; set; }

        /// <summary>
        /// Gets or sets the start value.
        /// </summary>
        public double Start { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Returns c# code that generates this instance.
        /// </summary>
        /// <returns>
        /// C# code.
        /// </returns>
        public string ToCode()
        {
            if (this.Color != null)
            {
                return CodeGenerator.FormatConstructor(
                    this.GetType(), "{0},{1},{2},{3}", this.Start, this.End, this.Title, this.Color.ToCode());
            }

            if (this.Title != null)
            {
                return CodeGenerator.FormatConstructor(this.GetType(), "{0},{1},{2}", this.Start, this.End, this.Title);
            }

            return CodeGenerator.FormatConstructor(this.GetType(), "{0},{1}", this.Start, this.End);
        }

    }
}