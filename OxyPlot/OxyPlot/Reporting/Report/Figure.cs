// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Figure.cs" company="OxyPlot">
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
//   Represents a figure (abstract base class for DrawingFigure, Image and PlotFigure).
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Reporting
{
    /// <summary>
    /// Represents a figure (abstract base class for DrawingFigure, Image and PlotFigure).
    /// </summary>
    public abstract class Figure : ReportItem
    {
        /// <summary>
        /// Gets or sets FigureNumber.
        /// </summary>
        public int FigureNumber { get; set; }

        /// <summary>
        /// Gets or sets FigureText.
        /// </summary>
        public string FigureText { get; set; }

        /// <summary>
        /// The get full caption.
        /// </summary>
        /// <param name="style">
        /// The style.
        /// </param>
        /// <returns>
        /// The get full caption.
        /// </returns>
        public string GetFullCaption(ReportStyle style)
        {
            return string.Format(style.FigureTextFormatString, this.FigureNumber, this.FigureText);
        }

    }
}