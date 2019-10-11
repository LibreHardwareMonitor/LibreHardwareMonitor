// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MathRenderingExtensions.cs" company="OxyPlot">
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
//   The math rendering extensions.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System;

    /// <summary>
    /// Provides functionality to render mathematic expressions (TeX syntax).
    /// </summary>
    public static class MathRenderingExtensions
    {
        /// <summary>
        /// Initializes static members of the <see cref = "MathRenderingExtensions" /> class.
        /// </summary>
        static MathRenderingExtensions()
        {
            SubAlignment = 0.6;
            SubSize = 0.62;
            SuperAlignment = 0;
            SuperSize = 0.62;
        }

        /// <summary>
        /// Gets or sets the subscript alignment.
        /// </summary>
        private static double SubAlignment { get; set; }

        /// <summary>
        /// Gets or sets the subscript size.
        /// </summary>
        private static double SubSize { get; set; }

        /// <summary>
        /// Gets or sets the superscript alignment.
        /// </summary>
        private static double SuperAlignment { get; set; }

        /// <summary>
        /// Gets or sets the superscript size.
        /// </summary>
        private static double SuperSize { get; set; }

        /// <summary>
        /// Draws or measures text containing sub- and superscript.
        /// </summary>
        /// <param name="rc">The render context.</param>
        /// <param name="pt">The point.</param>
        /// <param name="text">The text.</param>
        /// <param name="textColor">Color of the text.</param>
        /// <param name="fontFamily">The font family.</param>
        /// <param name="fontSize">The font size.</param>
        /// <param name="fontWeight">The font weight.</param>
        /// <param name="angle">The angle.</param>
        /// <param name="ha">The horizontal alignment.</param>
        /// <param name="va">The vertical alignment.</param>
        /// <param name="maxsize">The maximum size of the text.</param>
        /// <param name="measure">Measure the size of the text if set to <c>true</c>.</param>
        /// <returns>The size of the text.</returns>
        /// <example>
        /// Subscript: H_{2}O
        /// Superscript: E=mc^{2}
        /// Both: A^{2}_{i,j}
        /// </example>
        public static OxySize DrawMathText(
            this IRenderContext rc,
            ScreenPoint pt,
            string text,
            OxyColor textColor,
            string fontFamily,
            double fontSize,
            double fontWeight,
            double angle,
            HorizontalAlignment ha,
            VerticalAlignment va,
            OxySize? maxsize,
            bool measure)
        {
            if (string.IsNullOrEmpty(text))
            {
                return OxySize.Empty;
            }

            if (angle.Equals(0) && (text.Contains("^{") || text.Contains("_{")))
            {
                double x = pt.X;
                double y = pt.Y;

                // Measure
                var size = InternalDrawMathText(rc, x, y, text, textColor, fontFamily, fontSize, fontWeight, true);

                switch (ha)
                {
                    case HorizontalAlignment.Right:
                        x -= size.Width;
                        break;
                    case HorizontalAlignment.Center:
                        x -= size.Width * 0.5;
                        break;
                }

                switch (va)
                {
                    case VerticalAlignment.Bottom:
                        y -= size.Height;
                        break;
                    case VerticalAlignment.Middle:
                        y -= size.Height * 0.5;
                        break;
                }

                InternalDrawMathText(rc, x, y, text, textColor, fontFamily, fontSize, fontWeight, false);
                return measure ? size : OxySize.Empty;
            }

            rc.DrawText(pt, text, textColor, fontFamily, fontSize, fontWeight, angle, ha, va, maxsize);
            if (measure)
            {
                return rc.MeasureText(text, fontFamily, fontSize, fontWeight);
            }

            return OxySize.Empty;
        }

        /// <summary>
        /// Draws text containing sub- and superscript.
        /// </summary>
        /// <param name="rc">The render context.</param>
        /// <param name="pt">The point.</param>
        /// <param name="text">The text.</param>
        /// <param name="textColor">Color of the text.</param>
        /// <param name="fontFamily">The font family.</param>
        /// <param name="fontSize">The font size.</param>
        /// <param name="fontWeight">The font weight.</param>
        /// <param name="angle">The angle.</param>
        /// <param name="ha">The horizontal alignment.</param>
        /// <param name="va">The vertical alignment.</param>
        /// <param name="maxsize">The maximum size of the text.</param>
        /// <example>
        /// Subscript: H_{2}O
        /// Superscript: E=mc^{2}
        /// Both: A^{2}_{i,j}
        /// </example>
        public static void DrawMathText(
            this IRenderContext rc,
            ScreenPoint pt,
            string text,
            OxyColor textColor,
            string fontFamily,
            double fontSize,
            double fontWeight,
            double angle,
            HorizontalAlignment ha,
            VerticalAlignment va,
            OxySize? maxsize = null)
        {
            DrawMathText(rc, pt, text, textColor, fontFamily, fontSize, fontWeight, angle, ha, va, maxsize, false);
        }

        /// <summary>
        /// The measure math text.
        /// </summary>
        /// <param name="rc">
        /// The render context.
        /// </param>
        /// <param name="text">
        /// The text.
        /// </param>
        /// <param name="fontFamily">
        /// The font family.
        /// </param>
        /// <param name="fontSize">
        /// The font size.
        /// </param>
        /// <param name="fontWeight">
        /// The font weight.
        /// </param>
        /// <returns>
        /// The size of the text.
        /// </returns>
        public static OxySize MeasureMathText(
            this IRenderContext rc, string text, string fontFamily, double fontSize, double fontWeight)
        {
            if (text.Contains("^{") || text.Contains("_{"))
            {
                return InternalDrawMathText(rc, 0, 0, text, null, fontFamily, fontSize, fontWeight, true);
            }

            return rc.MeasureText(text, fontFamily, fontSize, fontWeight);
        }

        /// <summary>
        /// The internal draw math text.
        /// </summary>
        /// <param name="rc">
        /// The render context.
        /// </param>
        /// <param name="x">
        /// The x.
        /// </param>
        /// <param name="y">
        /// The y.
        /// </param>
        /// <param name="s">
        /// The s.
        /// </param>
        /// <param name="textColor">
        /// The text color.
        /// </param>
        /// <param name="fontFamily">
        /// The font family.
        /// </param>
        /// <param name="fontSize">
        /// The font size.
        /// </param>
        /// <param name="fontWeight">
        /// The font weight.
        /// </param>
        /// <param name="measureOnly">
        /// The measure only.
        /// </param>
        /// <returns>
        /// The size of the text.
        /// </returns>
        private static OxySize InternalDrawMathText(
            IRenderContext rc,
            double x,
            double y,
            string s,
            OxyColor textColor,
            string fontFamily,
            double fontSize,
            double fontWeight,
            bool measureOnly)
        {
            int i = 0;

            double currentX = x;
            double maximumX = x;
            double maxHeight = 0;

            // http://en.wikipedia.org/wiki/Subscript_and_superscript
            double superscriptY = y + fontSize * SuperAlignment;
            double superscriptFontSize = fontSize * SuperSize;
            double subscriptY = y + fontSize * SubAlignment;
            double subscriptFontSize = fontSize * SubSize;

            Func<double, double, string, double, OxySize> drawText = (xb, yb, text, fSize) =>
                {
                    if (!measureOnly)
                    {
                        rc.DrawText(new ScreenPoint(xb, yb), text, textColor, fontFamily, fSize, fontWeight);
                    }

                    return rc.MeasureText(text, fontFamily, fSize, fontWeight);
                };

            while (i < s.Length)
            {
                // Superscript
                if (i + 1 < s.Length && s[i] == '^' && s[i + 1] == '{')
                {
                    int i1 = s.IndexOf('}', i);
                    if (i1 != -1)
                    {
                        string supString = s.Substring(i + 2, i1 - i - 2);
                        i = i1 + 1;
                        OxySize size = drawText(currentX, superscriptY, supString, superscriptFontSize);
                        if (currentX + size.Width > maximumX)
                        {
                            maximumX = currentX + size.Width;
                        }

                        continue;
                    }
                }

                // Subscript
                if (i + 1 < s.Length && s[i] == '_' && s[i + 1] == '{')
                {
                    int i1 = s.IndexOf('}', i);
                    if (i1 != -1)
                    {
                        string subString = s.Substring(i + 2, i1 - i - 2);
                        i = i1 + 1;
                        OxySize size = drawText(currentX, subscriptY, subString, subscriptFontSize);
                        if (currentX + size.Width > maximumX)
                        {
                            maximumX = currentX + size.Width;
                        }

                        continue;
                    }
                }

                // Regular text
                int i2 = s.IndexOfAny("^_".ToCharArray(), i);
                string regularString;
                if (i2 == -1)
                {
                    regularString = s.Substring(i);
                    i = s.Length;
                }
                else
                {
                    regularString = s.Substring(i, i2 - i);
                    i = i2;
                }

                currentX = maximumX + 2;
                OxySize size2 = drawText(currentX, y, regularString, fontSize);
                currentX += size2.Width + 2;
                maxHeight = Math.Max(maxHeight, size2.Height);
                maximumX = currentX;
            }

            return new OxySize(maximumX - x, maxHeight);
        }

    }
}