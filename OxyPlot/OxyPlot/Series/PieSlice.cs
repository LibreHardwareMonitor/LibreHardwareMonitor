// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PieSlice.cs" company="OxyPlot">
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
//   Represent a slice of a PieSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Series
{
    /// <summary>
    /// Represent a slice of a <see cref="PieSeries"/>.
    /// </summary>
    public class PieSlice
    {
        /// <summary>
        /// Initializes a new instance of the <see cref = "PieSlice" /> class.
        /// </summary>
        public PieSlice()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PieSlice"/> class.
        /// </summary>
        /// <param name="label">
        /// The label.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <param name="fill">
        /// The fill.
        /// </param>
        public PieSlice(string label, double value, OxyColor fill = null)
        {
            this.Label = label;
            this.Value = value;
            this.Fill = fill;
        }

        /// <summary>
        /// Gets or sets Fill.
        /// </summary>
        public OxyColor Fill { get; set; }

        /// <summary>
        /// Gets the actual fill color.
        /// </summary>
        /// <value>The actual color.</value>
        public OxyColor ActualFillColor
        {
            get { return this.Fill ?? this.DefaultFillColor; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether IsExploded.
        /// </summary>
        public bool IsExploded { get; set; }

        /// <summary>
        /// Gets or sets Label.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets Value.
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Gets or sets the default fill color.
        /// </summary>
        /// <value>The default fill color.</value>
        internal OxyColor DefaultFillColor { get; set; }

    }
}