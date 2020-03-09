// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OxyPalettes.cs" company="OxyPlot">
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
//   Provides predefined palettes.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    /// <summary>
    /// Provides predefined palettes.
    /// </summary>
    public static class OxyPalettes
    {
        /// <summary>
        /// Initializes static members of the <see cref="OxyPalettes"/> class.
        /// </summary>
        static OxyPalettes()
        {
            BlueWhiteRed31 = BlueWhiteRed(31);
            Hot64 = Hot(64);
            Hue64 = Hue(64);
        }

        /// <summary>
        /// Gets the blue white red (31) palette.
        /// </summary>
        public static OxyPalette BlueWhiteRed31 { get; private set; }

        /// <summary>
        /// Gets the hot (64) palette.
        /// </summary>
        public static OxyPalette Hot64 { get; private set; }

        /// <summary>
        /// Gets the hue64 palette.
        /// </summary>
        public static OxyPalette Hue64 { get; private set; }

        /// <summary>
        /// Creates a black/white/red palette with the specified number of colors.
        /// </summary>
        /// <param name="numberOfColors">
        /// The number of colors to create for the palette.
        /// </param>
        /// <returns>
        /// A palette.
        /// </returns>
        public static OxyPalette BlackWhiteRed(int numberOfColors)
        {
            return OxyPalette.Interpolate(numberOfColors, OxyColors.Black, OxyColors.White, OxyColors.Red);
        }

        /// <summary>
        /// Creates a blue/white/red palette with the specified number of colors.
        /// </summary>
        /// <param name="numberOfColors">
        /// The number of colors to create for the palette.
        /// </param>
        /// <returns>
        /// A palette.
        /// </returns>
        public static OxyPalette BlueWhiteRed(int numberOfColors)
        {
            return OxyPalette.Interpolate(numberOfColors, OxyColors.Blue, OxyColors.White, OxyColors.Red);
        }

        /// <summary>
        /// Creates a 'cool' palette with the specified number of colors.
        /// </summary>
        /// <param name="numberOfColors">
        /// The number of colors to create for the palette.
        /// </param>
        /// <returns>
        /// A palette.
        /// </returns>
        public static OxyPalette Cool(int numberOfColors)
        {
            return OxyPalette.Interpolate(numberOfColors, OxyColors.Cyan, OxyColors.Magenta);
        }

        /// <summary>
        /// Creates a gray-scale palette with the specified number of colors.
        /// </summary>
        /// <param name="numberOfColors">
        /// The number of colors to create for the palette.
        /// </param>
        /// <returns>
        /// A palette.
        /// </returns>
        public static OxyPalette Gray(int numberOfColors)
        {
            return OxyPalette.Interpolate(numberOfColors, OxyColors.Black, OxyColors.White);
        }

        /// <summary>
        /// Creates a 'hot' palette with the specified number of colors.
        /// </summary>
        /// <param name="numberOfColors">
        /// The number of colors to create for the palette.
        /// </param>
        /// <returns>
        /// A palette.
        /// </returns>
        public static OxyPalette Hot(int numberOfColors)
        {
            return OxyPalette.Interpolate(
                numberOfColors,
                OxyColors.Black,
                OxyColor.FromRgb(127, 0, 0),
                OxyColor.FromRgb(255, 127, 0),
                OxyColor.FromRgb(255, 255, 127),
                OxyColors.White);
        }

        /// <summary>
        /// Creates a palette from the hue component of the HSV color model.
        /// </summary>
        /// <param name="numberOfColors">
        /// The number of colors.
        /// </param>
        /// <returns>
        /// The palette.
        /// </returns>
        /// <remarks>
        /// This palette is particularly appropriate for displaying periodic functions.
        /// </remarks>
        public static OxyPalette Hue(int numberOfColors)
        {
            return OxyPalette.Interpolate(
                numberOfColors,
                OxyColors.Red,
                OxyColors.Yellow,
                OxyColors.Green,
                OxyColors.Cyan,
                OxyColors.Blue,
                OxyColors.Magenta,
                OxyColors.Red);
        }

        /// <summary>
        /// Creates a 'jet' palette with the specified number of colors.
        /// </summary>
        /// <param name="numberOfColors">
        /// The number of colors to create for the palette.
        /// </param>
        /// <returns>
        /// A palette.
        /// </returns>
        /// <remarks>
        /// See http://www.mathworks.se/help/techdoc/ref/colormap.html.
        /// </remarks>
        public static OxyPalette Jet(int numberOfColors)
        {
            return OxyPalette.Interpolate(
                numberOfColors,
                OxyColors.DarkBlue,
                OxyColors.Cyan,
                OxyColors.Yellow,
                OxyColors.Orange,
                OxyColors.DarkRed);
        }

        /// <summary>
        /// Creates a rainbow palette with the specified number of colors.
        /// </summary>
        /// <param name="numberOfColors">
        /// The number of colors to create for the palette.
        /// </param>
        /// <returns>
        /// A palette.
        /// </returns>
        public static OxyPalette Rainbow(int numberOfColors)
        {
            return OxyPalette.Interpolate(
                numberOfColors,
                OxyColors.Violet,
                OxyColors.Indigo,
                OxyColors.Blue,
                OxyColors.Green,
                OxyColors.Yellow,
                OxyColors.Orange,
                OxyColors.Red);
        }
    }
}