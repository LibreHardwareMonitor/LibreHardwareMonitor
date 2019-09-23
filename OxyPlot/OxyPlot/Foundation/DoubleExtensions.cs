// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DoubleExtensions.cs" company="OxyPlot">
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
//   Extension methods for double values.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Provides extension methods for the <see cref="Double"/> type.
    /// </summary>
    public static class DoubleExtensions
    {
        /// <summary>
        /// Squares the specified value.
        /// </summary>
        /// <param name="x">
        /// The value.
        /// </param>
        /// <returns>
        /// Squared value.
        /// </returns>
        public static double Squared(this double x)
        {
            return x * x;
        }

        /// <summary>
        /// Exponent function.
        /// </summary>
        /// <param name="x">
        /// The value.
        /// </param>
        /// <returns>
        /// The exponent.
        /// </returns>
        public static double GetExponent(this double x)
        {
            return Math.Round(Math.Log(Math.Abs(x), 10));
        }

        /// <summary>
        /// Mantissa function.
        /// http://en.wikipedia.org/wiki/Mantissa
        /// </summary>
        /// <param name="x">
        /// The value.
        /// </param>
        /// <returns>
        /// The mantissa.
        /// </returns>
        public static double GetMantissa(this double x)
        {
            return x / Math.Pow(10, x.GetExponent());
        }

        /// <summary>
        /// Removes the floating point noise.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <returns>
        /// A double without noise.
        /// </returns>
        public static double RemoveNoise2(this double value)
        {
            return (double)((decimal)value);
        }

        /// <summary>
        /// Removes the floating point noise.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <param name="maxDigits">
        /// The maximum number of digits.
        /// </param>
        /// <returns>
        /// A double without noise.
        /// </returns>
        public static double RemoveNoise(this double value, int maxDigits = 8)
        {
            return double.Parse(value.ToString("e" + maxDigits));
        }

        /// <summary>
        /// Removes the noise from double math.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <returns>
        /// A double without noise.
        /// </returns>
        public static double RemoveNoiseFromDoubleMath(this double value)
        {
            if (value.IsZero() || Math.Abs(Math.Log10(Math.Abs(value))) < 27)
            {
                return (double)((decimal)value);
            }

            return double.Parse(value.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Determines whether the specified value is zero.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// <c>true</c> if the specified value is zero; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsZero(this double value)
        {
            return Math.Abs(value) < double.Epsilon;
        }

        /// <summary>
        /// Calculates the nearest larger multiple of the specified value.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <param name="step">
        /// The multiplier.
        /// </param>
        /// <returns>
        /// The multiple value.
        /// </returns>
        public static double ToUpperMultiple(this double value, double step)
        {
            var i = (int)Math.Ceiling(value / step);
            return (step * i).RemoveNoise();
        }

        /// <summary>
        /// Calculates the nearest smaller multiple of the specified value.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <param name="step">
        /// The multiplier.
        /// </param>
        /// <returns>
        /// The multiple value.
        /// </returns>
        public static double ToLowerMultiple(this double value, double step)
        {
            var i = (int)Math.Floor(value / step);
            return (step * i).RemoveNoise();
        }

#if THISISNOTINUSE

    // <summary>
    // Gets the mantissa and exponent.
    // </summary>
    /// <remarks>
    /// From <see cref="http://stackoverflow.com/questions/389993/extracting-mantissa-and-exponent-from-double-in-c"/>
    /// </remarks>
    /// <param name="d">The d.</param>
    /// <param name="negative">if set to <c>true</c> [negative].</param>
    /// <param name="mantissa">The mantissa.</param>
    /// <param name="exponent">The exponent.</param>
        public static void GetMantissaAndExponent(this double d, out bool negative, out long mantissa, out int exponent)
        {
            // Translate the double into sign, exponent and mantissa.
            long bits = BitConverter.DoubleToInt64Bits(d);

// Note that the shift is sign-extended, hence the test against -1 not 1
            negative = (bits < 0);
            exponent = (int)((bits >> 52) & 0x7ffL);
            mantissa = bits & 0xfffffffffffffL;

            // Subnormal numbers; exponent is effectively one higher,
            // but there's no extra normalisation bit in the mantissa
            if (exponent == 0)
            {
                exponent++;
            }

// Normal numbers; leave exponent as it is but add extra
            // bit to the front of the mantissa
            else
            {
                mantissa = mantissa | (1L << 52);
            }

            // Bias the exponent. It's actually biased by 1023, but we're
            // treating the mantissa as m.0 rather than 0.m, so we need
            // to subtract another 52 from it.
            exponent -= 1075;

            if (mantissa == 0)
            {
                return;
            }

            /* Normalize */
            while ((mantissa & 1) == 0)
            {    /*  i.e., Mantissa is even */
                mantissa >>= 1;
                exponent++;
            }
        }
#endif
    }
}