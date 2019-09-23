// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlotElement.cs" company="OxyPlot">
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
//   Abstract base class for all plottable elements (Axes, Annotations, Series).
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System;

    /// <summary>
    /// Provides an abstract base class for elements contained in a <see cref="PlotModel"/>.
    /// </summary>
    public abstract class PlotElement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlotElement"/> class.
        /// </summary>
        protected PlotElement()
        {
            this.Font = null;
            this.FontSize = double.NaN;
            this.FontWeight = FontWeights.Normal;
        }

        /// <summary>
        /// Gets or sets the font.
        /// </summary>
        /// <value> The font. </value>
        /// <remarks>
        /// If the value is null, the parent PlotModel's DefaultFont will be used.
        /// </remarks>
        public string Font { get; set; }

        /// <summary>
        /// Gets or sets the size of the font.
        /// </summary>
        /// <value> The size of the font. </value>
        /// <remarks>
        /// If the value is NaN, the parent PlotModel's DefaultFontSize will be used.
        /// </remarks>
        public double FontSize { get; set; }

        /// <summary>
        /// Gets or sets the font weight.
        /// </summary>
        /// <value> The font weight. </value>
        public double FontWeight { get; set; }

        /// <summary>
        /// Gets the parent plot model.
        /// </summary>
        public PlotModel PlotModel { get; internal set; }

        /// <summary>
        /// Gets or sets an arbitrary object value that can be used to store custom information about this plot element.
        /// </summary>
        /// <value> The intended value. This property has no default value. </value>
        /// <remarks>
        /// This property is analogous to Tag properties in other Microsoft programming models. Tag is intended to provide a pre-existing property location where you can store some basic custom information about any PlotElement without requiring you to subclass an element.
        /// </remarks>
        public object Tag { get; set; }

        /// <summary>
        /// Gets or sets the color of the text.
        /// </summary>
        /// <value> The color of the text. </value>
        /// <remarks>
        /// If the value is null, the TextColor of the parent PlotModel will be used.
        /// </remarks>
        public OxyColor TextColor { get; set; }

        /// <summary>
        /// Gets the actual font.
        /// </summary>
        protected internal string ActualFont
        {
            get
            {
                return this.Font ?? this.PlotModel.DefaultFont;
            }
        }

        /// <summary>
        /// Gets the actual size of the font.
        /// </summary>
        /// <value> The actual size of the font. </value>
        protected internal double ActualFontSize
        {
            get
            {
                return !double.IsNaN(this.FontSize) ? this.FontSize : this.PlotModel.DefaultFontSize;
            }
        }

        /// <summary>
        /// Gets the actual font weight.
        /// </summary>
        protected internal double ActualFontWeight
        {
            get
            {
                return this.FontWeight;
            }
        }

        /// <summary>
        /// Gets the actual color of the text.
        /// </summary>
        /// <value> The actual color of the text. </value>
        protected internal OxyColor ActualTextColor
        {
            get
            {
                return this.TextColor ?? this.PlotModel.TextColor;
            }
        }
    }
}