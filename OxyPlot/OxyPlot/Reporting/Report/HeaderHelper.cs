// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HeaderHelper.cs" company="OxyPlot">
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
//   The header helper.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Reporting
{
    /// <summary>
    /// The header helper.
    /// </summary>
    public class HeaderHelper
    {
        /// <summary>
        /// The header level.
        /// </summary>
        private readonly int[] headerLevel = new int[10];

        /// <summary>
        /// The get header.
        /// </summary>
        /// <param name="level">
        /// The level.
        /// </param>
        /// <returns>
        /// The get header.
        /// </returns>
        public string GetHeader(int level)
        {
            for (int i = level - 1; i > 0; i--)
            {
                if (this.headerLevel[i] == 0)
                {
                    this.headerLevel[i] = 1;
                }
            }

            this.headerLevel[level]++;
            for (int i = level + 1; i < 10; i++)
            {
                this.headerLevel[i] = 0;
            }

            string levelString = string.Empty;
            for (int i = 1; i <= level; i++)
            {
                if (i > 1)
                {
                    levelString += ".";
                }

                levelString += this.headerLevel[i];
            }

            return levelString;
        }

    }
}