// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.cs" company="OxyPlot">
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
//   The string extensions.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Reporting
{
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// The string extensions.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// The repeat.
        /// </summary>
        /// <param name="source">
        /// The source.
        /// </param>
        /// <param name="n">
        /// The n.
        /// </param>
        /// <returns>
        /// The repeat.
        /// </returns>
        public static string Repeat(this string source, int n)
        {
            var sb = new StringBuilder(n * source.Length);
            for (int i = 0; i < n; i++)
            {
                sb.Append(source);
            }

            return sb.ToString();
        }

        /// <summary>
        /// The split lines.
        /// </summary>
        /// <param name="s">
        /// The s.
        /// </param>
        /// <param name="lineLength">
        /// The line length.
        /// </param>
        /// <returns>
        /// </returns>
        public static string[] SplitLines(this string s, int lineLength = 80)
        {
            var lines = new List<string>();

            int i = 0;
            while (i < s.Length)
            {
                int len = FindLineLength(s, i, lineLength);
                lines.Add(len == 0 ? s.Substring(i).Trim() : s.Substring(i, len).Trim());
                i += len;
                if (len == 0)
                {
                    break;
                }
            }

            return lines.ToArray();
        }

        /// <summary>
        /// The find line length.
        /// </summary>
        /// <param name="text">
        /// The text.
        /// </param>
        /// <param name="i">
        /// The i.
        /// </param>
        /// <param name="maxLineLength">
        /// The max line length.
        /// </param>
        /// <returns>
        /// The find line length.
        /// </returns>
        private static int FindLineLength(string text, int i, int maxLineLength)
        {
            int i2 = i + 1;
            int len = 0;
            while (i2 < i + maxLineLength && i2 < text.Length)
            {
                i2 = text.IndexOfAny(" \n\r".ToCharArray(), i2 + 1);
                if (i2 == -1)
                {
                    i2 = text.Length;
                }

                if (i2 - i < maxLineLength)
                {
                    len = i2 - i;
                }
            }

            return len;
        }

    }
}