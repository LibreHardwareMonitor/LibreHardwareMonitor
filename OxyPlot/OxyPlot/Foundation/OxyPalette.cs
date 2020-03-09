// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OxyPalette.cs" company="OxyPlot">
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
//   Represents a palette of colors.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a palette of colors.
    /// </summary>
    public class OxyPalette
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OxyPalette"/> class.
        /// </summary>
        public OxyPalette()
        {
            this.Colors = new List<OxyColor>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OxyPalette"/> class.
        /// </summary>
        /// <param name="colors">
        /// The colors.
        /// </param>
        public OxyPalette(params OxyColor[] colors)
        {
            this.Colors = new List<OxyColor>(colors);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OxyPalette"/> class.
        /// </summary>
        /// <param name="colors">
        /// The colors.
        /// </param>
        public OxyPalette(IEnumerable<OxyColor> colors)
        {
            this.Colors = new List<OxyColor>(colors);
        }

        /// <summary>
        /// Gets or sets the colors.
        /// </summary>
        /// <value> The colors. </value>
        public IList<OxyColor> Colors { get; set; }

        /// <summary>
        /// Interpolates the specified colors to a palette of the specified size.
        /// </summary>
        /// <param name="paletteSize">
        /// The size of the palette.
        /// </param>
        /// <param name="colors">
        /// The colors.
        /// </param>
        /// <returns>
        /// A palette.
        /// </returns>
        public static OxyPalette Interpolate(int paletteSize, params OxyColor[] colors)
        {
            var palette = new OxyColor[paletteSize];
            for (int i = 0; i < paletteSize; i++)
            {
                double y = (double)i / (paletteSize - 1);
                double x = y * (colors.Length - 1);
                int i0 = (int)x;
                int i1 = i0 + 1 < colors.Length ? i0 + 1 : i0;
                palette[i] = OxyColor.Interpolate(colors[i0], colors[i1], x - i0);
            }

            return new OxyPalette(palette);
        }
    }
}