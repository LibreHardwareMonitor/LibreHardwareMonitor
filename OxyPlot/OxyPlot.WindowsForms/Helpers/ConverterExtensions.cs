// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConverterExtensions.cs" company="OxyPlot">
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
//   Extension method used to convert to/from Windows/Windows.Media classes.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.WindowsForms
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    /// <summary>
    /// Extension method used to convert to/from Windows/Windows.Media classes.
    /// </summary>
    public static class ConverterExtensions
    {
        /// <summary>
        /// Calculate the distance between two points.
        /// </summary>
        /// <param name="p1">
        /// The first point.
        /// </param>
        /// <param name="p2">
        /// The second point.
        /// </param>
        /// <returns>
        /// The distance.
        /// </returns>
        public static double DistanceTo(this Point p1, Point p2)
        {
            double dx = p1.X - p2.X;
            double dy = p1.Y - p2.Y;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }

        /// <summary>
        /// Converts a color to a Brush.
        /// </summary>
        /// <param name="c">
        /// The color.
        /// </param>
        /// <returns>
        /// A SolidColorBrush.
        /// </returns>
        public static Brush ToBrush(this OxyColor c)
        {
            return new SolidBrush(c.ToColor());
        }

        /// <summary>
        /// Converts an OxyColor to a Color.
        /// </summary>
        /// <param name="c">
        /// The color.
        /// </param>
        /// <returns>
        /// A Color.
        /// </returns>
        public static Color ToColor(this OxyColor c)
        {
            return Color.FromArgb(c.A, c.R, c.G, c.B);
        }

        /// <summary>
        /// Converts a HorizontalAlignment to a HorizontalTextAlign.
        /// </summary>
        /// <param name="alignment">
        /// The alignment.
        /// </param>
        /// <returns>
        /// A HorizontalTextAlign.
        /// </returns>
        public static OxyPlot.HorizontalAlignment ToHorizontalTextAlign(this HorizontalAlignment alignment)
        {
            switch (alignment)
            {
                case HorizontalAlignment.Center:
                    return OxyPlot.HorizontalAlignment.Center;
                case HorizontalAlignment.Right:
                    return OxyPlot.HorizontalAlignment.Right;
                default:
                    return OxyPlot.HorizontalAlignment.Left;
            }
        }

        /// <summary>
        /// Converts a Color to an OxyColor.
        /// </summary>
        /// <param name="color">
        /// The color.
        /// </param>
        /// <returns>
        /// An OxyColor.
        /// </returns>
        public static OxyColor ToOxyColor(this Color color)
        {
            return OxyColor.FromArgb(color.A, color.R, color.G, color.B);
        }

        /// <summary>
        /// Converts a nullable Color to an OxyColor.
        /// </summary>
        /// <param name="color">
        /// The color.
        /// </param>
        /// <returns>
        /// An OxyColor.
        /// </returns>
        public static OxyColor ToOxyColor(this Color? color)
        {
            return color.HasValue ? color.Value.ToOxyColor() : null;
        }

        /// <summary>
        /// Converts a Brush to an OxyColor.
        /// </summary>
        /// <param name="brush">
        /// The brush.
        /// </param>
        /// <returns>
        /// An oxycolor.
        /// </returns>
        public static OxyColor ToOxyColor(this Brush brush)
        {
            var scb = brush as SolidBrush;
            return scb != null ? scb.Color.ToOxyColor() : null;
        }

        /// <summary>
        /// Converts a Thickness to an OxyThickness.
        /// </summary>
        /// <returns>
        /// An OxyPlot thickness.
        /// </returns>
        /// <summary>
        /// Converts a ScreenPoint to a Point.
        /// </summary>
        /// <param name="pt">
        /// The screen point.
        /// </param>
        /// <param name="aliased">
        /// use pixel alignment conversion if set to <c>true</c>.
        /// </param>
        /// <returns>
        /// A point.
        /// </returns>
        public static Point ToPoint(this ScreenPoint pt, bool aliased)
        {
            // adding 0.5 to get pixel boundary alignment, seems to work
            // http://weblogs.asp.net/mschwarz/archive/2008/01/04/silverlight-rectangles-paths-and-line-comparison.aspx
            // http://www.wynapse.com/Silverlight/Tutor/Silverlight_Rectangles_Paths_And_Lines_Comparison.aspx
            if (aliased)
            {
                return new Point((int)pt.X, (int)pt.Y);
            }

            return new Point((int)Math.Round(pt.X), (int)Math.Round(pt.Y));
        }

        /// <summary>
        /// Converts an OxyRect to a Rect.
        /// </summary>
        /// <param name="r">
        /// The rectangle.
        /// </param>
        /// <param name="aliased">
        /// use pixel alignment if set to <c>true</c>.
        /// </param>
        /// <returns>
        /// A rect.
        /// </returns>
        public static Rectangle ToRect(this OxyRect r, bool aliased)
        {
            if (aliased)
            {
                var x = (int)r.Left;
                var y = (int)r.Top;
                var ri = (int)r.Right;
                var bo = (int)r.Bottom;
                return new Rectangle(x, y, ri - x, bo - y);
            }

            return new Rectangle(
                (int)Math.Round(r.Left), (int)Math.Round(r.Top), (int)Math.Round(r.Width), (int)Math.Round(r.Height));
        }

        /// <summary>
        /// Converts a point to a ScreenPoint.
        /// </summary>
        /// <param name="pt">
        /// The point.
        /// </param>
        /// <returns>
        /// A screen point.
        /// </returns>
        public static ScreenPoint ToScreenPoint(this Point pt)
        {
            return new ScreenPoint(pt.X, pt.Y);
        }

        /// <summary>
        /// Converts a Point array to a ScreenPoint array.
        /// </summary>
        /// <param name="points">
        /// The points.
        /// </param>
        /// <returns>
        /// A ScreenPoint array.
        /// </returns>
        public static ScreenPoint[] ToScreenPointArray(this Point[] points)
        {
            if (points == null)
            {
                return null;
            }

            var pts = new ScreenPoint[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                pts[i] = points[i].ToScreenPoint();
            }

            return pts;
        }

    }
}