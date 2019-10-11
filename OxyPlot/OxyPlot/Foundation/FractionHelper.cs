// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FractionHelper.cs" company="OxyPlot">
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
//   Generates fraction strings from double values.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Provides functionality to generate fraction strings from double values.
    /// </summary>
    /// <remarks>
    /// Examples: "3/4", "PI/2"
    /// </remarks>
    public static class FractionHelper
    {
        /// <summary>
        /// Converts a double to a fraction string.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <param name="unit">
        /// The unit.
        /// </param>
        /// <param name="unitSymbol">
        /// The unit symbol.
        /// </param>
        /// <param name="eps">
        /// The tolerance.
        /// </param>
        /// <param name="formatProvider">
        /// The format Provider.
        /// </param>
        /// <returns>
        /// The convert to fraction string.
        /// </returns>
        public static string ConvertToFractionString(
            double value,
            double unit = 1,
            string unitSymbol = null,
            double eps = 1e-6,
            IFormatProvider formatProvider = null)
        {
            if (Math.Abs(value) < eps)
            {
                return "0";
            }

            // ½, ⅝, ¾
            value /= unit;

            // int whole = (int)(value - (int) value);
            // int N = 10000;
            // int frac = (int) ((value - whole)*N);
            // var d = GCF(N,frac);
            for (int d = 1; d <= 64; d++)
            {
                double n = value * d;
                var ni = (int)Math.Round(n);
                if (Math.Abs(n - ni) < eps)
                {
                    string nis = unitSymbol == null || ni != 1 ? ni.ToString(CultureInfo.InvariantCulture) : string.Empty;
                    if (d == 1)
                    {
                        return string.Format("{0}{1}", nis, unitSymbol);
                    }

                    return string.Format("{0}{1}/{2}", nis, unitSymbol, d);
                }
            }

            return string.Format(formatProvider ?? CultureInfo.CurrentCulture, "{0}{1}", value, unitSymbol);
        }

        /// <summary>
        /// Finds the greates common divisor.
        /// </summary>
        /// <param name="a">
        /// The a.
        /// </param>
        /// <param name="b">
        /// The b.
        /// </param>
        /// <returns>
        /// The gcd.
        /// </returns>
        public static int gcd(int a, int b)
        {
            if (b == 0)
            {
                return a;
            }

            return gcd(b, a % b);
        }

        /// <summary>
        /// Finds the greatest common factor.
        /// </summary>
        /// <param name="x">
        /// The x.
        /// </param>
        /// <param name="y">
        /// The y.
        /// </param>
        /// <returns>
        /// The gcf.
        /// </returns>
        private static int GCF(int x, int y)
        {
            x = Math.Abs(x);
            y = Math.Abs(y);
            int z;
            do
            {
                z = x % y;
                if (z == 0)
                {
                    return y;
                }

                x = y;
                y = z;
            }
            while (true);
        }
    }
}