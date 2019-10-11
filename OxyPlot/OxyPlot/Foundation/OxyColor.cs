// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OxyColor.cs" company="OxyPlot">
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
//   Describes a color in terms of alpha, red, green, and blue channels.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Describes a color in terms of alpha, red, green, and blue channels.
    /// </summary>
    public class OxyColor : ICodeGenerating
    {
        /// <summary>
        /// Gets or sets the alpha value.
        /// </summary>
        /// <value> The alpha value. </value>
        public byte A { get; set; }

        /// <summary>
        /// Gets or sets the blue value.
        /// </summary>
        /// <value> The blue value. </value>
        public byte B { get; set; }

        /// <summary>
        /// Gets or sets the green value.
        /// </summary>
        /// <value> The green value. </value>
        public byte G { get; set; }

        /// <summary>
        /// Gets or sets the red value.
        /// </summary>
        /// <value> The red value. </value>
        public byte R { get; set; }

        /// <summary>
        /// Parse a string.
        /// </summary>
        /// <param name="value">
        /// The string in the format "#FFFFFF00" or "255,200,180,50".
        /// </param>
        /// <returns>
        /// The OxyColor.
        /// </returns>
        /// <exception cref="System.FormatException">
        /// Invalid format.
        /// </exception>
        public static OxyColor Parse(string value)
        {
            value = value.Trim();
            if (value.StartsWith("#"))
            {
                value = value.Trim('#');
                var u = uint.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                if (value.Length < 8)
                {
                    // alpha value was not specified
                    u += 0xFF000000;
                }

                return FromUInt32(u);
            }

            var values = value.Split(',');
            if (values.Length < 3 || values.Length > 4)
            {
                throw new FormatException("Invalid format.");
            }

            var i = 0;

            byte alpha = 255;
            if (values.Length > 3)
            {
                alpha = byte.Parse(values[i++], CultureInfo.InvariantCulture);
            }

            var red = byte.Parse(values[i++], CultureInfo.InvariantCulture);
            var green = byte.Parse(values[i++], CultureInfo.InvariantCulture);
            var blue = byte.Parse(values[i], CultureInfo.InvariantCulture);
            return FromArgb(alpha, red, green, blue);
        }

        /// <summary>
        /// Calculates the difference between two <see cref="OxyColor"/>s
        /// </summary>
        /// <param name="c1">
        /// The first color.
        /// </param>
        /// <param name="c2">
        /// The second color.
        /// </param>
        /// <returns>
        /// L2-norm in RGBA space
        /// </returns>
        public static double ColorDifference(OxyColor c1, OxyColor c2)
        {
            // http://en.wikipedia.org/wiki/OxyColor_difference
            // http://mathworld.wolfram.com/L2-Norm.html
            double dr = (c1.R - c2.R) / 255.0;
            double dg = (c1.G - c2.G) / 255.0;
            double db = (c1.B - c2.B) / 255.0;
            double da = (c1.A - c2.A) / 255.0;
            double e = (dr * dr) + (dg * dg) + (db * db) + (da * da);
            return Math.Sqrt(e);
        }

        /// <summary>
        /// Convert an <see cref="uint"/> to a <see cref="OxyColor"/>.
        /// </summary>
        /// <param name="color">
        /// The unsigned integer color value.
        /// </param>
        /// <returns>
        /// The <see cref="OxyColor"/>.
        /// </returns>
        public static OxyColor FromUInt32(uint color)
        {
            var a = (byte)(color >> 24);
            var r = (byte)(color >> 16);
            var g = (byte)(color >> 8);
            var b = (byte)(color >> 0);
            return FromArgb(a, r, g, b);
        }

        /// <summary>
        /// Creates a OxyColor from the specified HSV array.
        /// </summary>
        /// <param name="hsv">
        /// The HSV value array.
        /// </param>
        /// <returns>
        /// A OxyColor.
        /// </returns>
        public static OxyColor FromHsv(double[] hsv)
        {
            if (hsv.Length != 3)
            {
                throw new InvalidOperationException("Wrong length of hsv array.");
            }

            return FromHsv(hsv[0], hsv[1], hsv[2]);
        }

        /// <summary>
        /// Convert from HSV to <see cref="OxyColor"/>
        /// http://en.wikipedia.org/wiki/HSL_Color_space
        /// </summary>
        /// <param name="hue">
        /// The hue value [0,1]
        /// </param>
        /// <param name="sat">
        /// The saturation value [0,1]
        /// </param>
        /// <param name="val">
        /// The intensity value [0,1]
        /// </param>
        /// <returns>
        /// The <see cref="OxyColor"/>.
        /// </returns>
        public static OxyColor FromHsv(double hue, double sat, double val)
        {
            double g, b;
            double r = g = b = 0;

            if (sat.Equals(0))
            {
                // Gray scale
                r = g = b = val;
            }
            else
            {
                if (hue.Equals(1))
                {
                    hue = 0;
                }

                hue *= 6.0;
                int i = (int)Math.Floor(hue);
                double f = hue - i;
                double aa = val * (1 - sat);
                double bb = val * (1 - (sat * f));
                double cc = val * (1 - (sat * (1 - f)));
                switch (i)
                {
                    case 0:
                        r = val;
                        g = cc;
                        b = aa;
                        break;
                    case 1:
                        r = bb;
                        g = val;
                        b = aa;
                        break;
                    case 2:
                        r = aa;
                        g = val;
                        b = cc;
                        break;
                    case 3:
                        r = aa;
                        g = bb;
                        b = val;
                        break;
                    case 4:
                        r = cc;
                        g = aa;
                        b = val;
                        break;
                    case 5:
                        r = val;
                        g = aa;
                        b = bb;
                        break;
                }
            }

            return FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }

        /// <summary>
        /// Calculate the difference in hue between two <see cref="OxyColor"/>s.
        /// </summary>
        /// <param name="c1">
        /// The first color.
        /// </param>
        /// <param name="c2">
        /// The second color.
        /// </param>
        /// <returns>
        /// The hue difference.
        /// </returns>
        public static double HueDifference(OxyColor c1, OxyColor c2)
        {
            var hsv1 = c1.ToHsv();
            var hsv2 = c2.ToHsv();
            double dh = hsv1[0] - hsv2[0];

            // clamp to [-0.5,0.5]
            if (dh > 0.5)
            {
                dh -= 1.0;
            }

            if (dh < -0.5)
            {
                dh += 1.0;
            }

            double e = dh * dh;
            return Math.Sqrt(e);
        }

        /// <summary>
        /// Creates a color defined by an alpha value and another color.
        /// </summary>
        /// <param name="a">
        /// Alpha value.
        /// </param>
        /// <param name="color">
        /// The original color.
        /// </param>
        /// <returns>
        /// A color.
        /// </returns>
        public static OxyColor FromAColor(byte a, OxyColor color)
        {
            return new OxyColor { A = a, R = color.R, G = color.G, B = color.B };
        }

        /// <summary>
        /// Creates a color from the specified ARGB values.
        /// </summary>
        /// <param name="a">
        /// The alpha value.
        /// </param>
        /// <param name="r">
        /// The red value.
        /// </param>
        /// <param name="g">
        /// The green value.
        /// </param>
        /// <param name="b">
        /// The blue value.
        /// </param>
        /// <returns>
        /// A color.
        /// </returns>
        public static OxyColor FromArgb(byte a, byte r, byte g, byte b)
        {
            return new OxyColor { A = a, R = r, G = g, B = b };
        }

        /// <summary>
        /// Creates a new <see cref="OxyColor"/> structure from the specified RGB values.
        /// </summary>
        /// <param name="r">
        /// The red value.
        /// </param>
        /// <param name="g">
        /// The green value.
        /// </param>
        /// <param name="b">
        /// The blue value.
        /// </param>
        /// <returns>
        /// A <see cref="OxyColor"/> structure with the specified values and an alpha channel value of 1.
        /// </returns>
        public static OxyColor FromRgb(byte r, byte g, byte b)
        {
            // ReSharper restore InconsistentNaming
            return new OxyColor { A = 255, R = r, G = g, B = b };
        }

        /// <summary>
        /// Interpolates the specified colors.
        /// </summary>
        /// <param name="color1">
        /// The color1.
        /// </param>
        /// <param name="color2">
        /// The color2.
        /// </param>
        /// <param name="t">
        /// The t.
        /// </param>
        /// <returns>
        /// The interpolated color
        /// </returns>
        public static OxyColor Interpolate(OxyColor color1, OxyColor color2, double t)
        {
            double a = (color1.A * (1 - t)) + (color2.A * t);
            double r = (color1.R * (1 - t)) + (color2.R * t);
            double g = (color1.G * (1 - t)) + (color2.G * t);
            double b = (color1.B * (1 - t)) + (color2.B * t);
            return FromArgb((byte)a, (byte)r, (byte)g, (byte)b);
        }

        /// <summary>
        /// Convert OxyColor to double string.
        /// </summary>
        /// <returns>
        /// A OxyColor string, e.g. "255,200,180,50".
        /// </returns>
        public string ToByteString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", this.A, this.R, this.G, this.B);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="System.Object"/> to compare with this instance.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c> .
        /// </returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != typeof(OxyColor))
            {
                return false;
            }

            return this.Equals((OxyColor)obj);
        }

        /// <summary>
        /// Determines whether the specified <see cref="OxyColor"/> is equal to this instance.
        /// </summary>
        /// <param name="other">
        /// The <see cref="OxyColor"/> to compare with this instance.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="OxyColor"/> is equal to this instance; otherwise, <c>false</c> .
        /// </returns>
        public bool Equals(OxyColor other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other.A == this.A && other.R == this.R && other.G == this.G && other.B == this.B;
        }

        /// <summary>
        /// Gets the color name.
        /// </summary>
        /// <returns>
        /// The color name.
        /// </returns>
        public string GetColorName()
        {
            var t = typeof(OxyColors);
            var colors = t.GetFields(BindingFlags.Public | BindingFlags.Static);
            var colorField = colors.FirstOrDefault(
                field =>
                {
                    var color = field.GetValue(null);
                    return this.Equals(color);
                });
            return colorField != null ? colorField.Name : null;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int result = this.A.GetHashCode();
                result = (result * 397) ^ this.R.GetHashCode();
                result = (result * 397) ^ this.G.GetHashCode();
                result = (result * 397) ^ this.B.GetHashCode();
                return result;
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture, "#{0:x2}{1:x2}{2:x2}{3:x2}", this.A, this.R, this.G, this.B);
        }

        /// <summary>
        /// Changes the opacity value.
        /// </summary>
        /// <param name="newAlpha">
        /// The new alpha.
        /// </param>
        /// <returns>
        /// The new color.
        /// </returns>
        public OxyColor ChangeAlpha(byte newAlpha)
        {
            return FromArgb(newAlpha, this.R, this.G, this.B);
        }

        /// <summary>
        /// Calculates the complementary OxyColor.
        /// </summary>
        /// <returns>
        /// The complementary OxyColor.
        /// </returns>
        public OxyColor Complementary()
        {
            // http://en.wikipedia.org/wiki/Complementary_Color
            var hsv = this.ToHsv();
            double newHue = hsv[0] - 0.5;

            // clamp to [0,1]
            if (newHue < 0)
            {
                newHue += 1.0;
            }

            return FromHsv(newHue, hsv[1], hsv[2]);
        }

        /// <summary>
        /// Converts from a <see cref="OxyColor"/> to HSV values (double)
        /// </summary>
        /// <returns>
        /// Array of [Hue,Saturation,Value] in the range [0,1]
        /// </returns>
        public double[] ToHsv()
        {
            byte r = this.R;
            byte g = this.G;
            byte b = this.B;

            byte min = Math.Min(Math.Min(r, g), b);
            byte v = Math.Max(Math.Max(r, g), b);
            double delta = v - min;

            double s = v.Equals(0) ? 0 : delta / v;
            double h = 0;

            if (s.Equals(0))
            {
                h = 0.0;
            }
            else
            {
                if (r == v)
                {
                    h = (g - b) / delta;
                }
                else if (g == v)
                {
                    h = 2 + ((b - r) / delta);
                }
                else if (b == v)
                {
                    h = 4 + ((r - g) / delta);
                }

                h *= 60;
                if (h < 0.0)
                {
                    h += 360;
                }
            }

            var hsv = new double[3];
            hsv[0] = h / 360.0;
            hsv[1] = s;
            hsv[2] = v / 255.0;
            return hsv;
        }

        /// <summary>
        /// Changes the intensity.
        /// </summary>
        /// <param name="factor">
        /// The factor.
        /// </param>
        /// <returns>
        /// The new OxyColor.
        /// </returns>
        public OxyColor ChangeIntensity(double factor)
        {
            var hsv = this.ToHsv();
            hsv[2] *= factor;
            if (hsv[2] > 1.0)
            {
                hsv[2] = 1.0;
            }

            return FromHsv(hsv);
        }

        /// <summary>
        /// Converts to an unsigned integer.
        /// </summary>
        /// <returns>
        /// The <see cref="uint"/>.
        /// </returns>
        public uint ToUint()
        {
            uint u = (uint)this.A << 24;
            u += (uint)this.R << 16;
            u += (uint)this.G << 8;
            u += this.B;
            return u;

            // (UInt32)((UInt32)c.A << 24 + (UInt32)c.R << 16 + (UInt32)c.G << 8 + (UInt32)c.B);
        }

        /// <summary>
        /// Returns C# code that generates this instance.
        /// </summary>
        /// <returns>
        /// The to code.
        /// </returns>
        public string ToCode()
        {
            string name = this.GetColorName();
            if (name != null)
            {
                return string.Format("OxyColors.{0}", name);
            }

            return string.Format("OxyColor.FromArgb({0}, {1}, {2}, {3})", this.A, this.R, this.G, this.B);
        }
    }
}