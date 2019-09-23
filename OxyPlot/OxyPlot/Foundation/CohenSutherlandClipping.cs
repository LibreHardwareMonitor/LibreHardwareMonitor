// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CohenSutherlandClipping.cs" company="OxyPlot">
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
//   Line clipping algorithm.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    /// <summary>
    /// Provides a line clipping algorithm.
    /// </summary>
    /// <remarks>
    /// See http://en.wikipedia.org/wiki/Cohen%E2%80%93Sutherland
    /// </remarks>
    public class CohenSutherlandClipping
    {
        /// <summary>
        /// The bottom code.
        /// </summary>
        private const int Bottom = 4; // 0100

        /// <summary>
        /// The inside code.
        /// </summary>
        private const int Inside = 0; // 0000

        /// <summary>
        /// The left code.
        /// </summary>
        private const int Left = 1; // 0001

        /// <summary>
        /// The right code.
        /// </summary>
        private const int Right = 2; // 0010

        /// <summary>
        /// The top code.
        /// </summary>
        private const int Top = 8; // 1000

        /// <summary>
        /// The x maximum.
        /// </summary>
        private readonly double xmax;

        /// <summary>
        /// The x minimum.
        /// </summary>
        private readonly double xmin;

        /// <summary>
        /// The y maximum.
        /// </summary>
        private readonly double ymax;

        /// <summary>
        /// The y minimum.
        /// </summary>
        private readonly double ymin;

        /// <summary>
        /// Initializes a new instance of the <see cref="CohenSutherlandClipping"/> class.
        /// </summary>
        /// <param name="rect">
        /// The clipping rectangle.
        /// </param>
        public CohenSutherlandClipping(OxyRect rect)
        {
            this.xmin = rect.Left;
            this.xmax = rect.Right;
            this.ymin = rect.Top;
            this.ymax = rect.Bottom;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CohenSutherlandClipping"/> class.
        /// </summary>
        /// <param name="xmin">
        /// The xmin.
        /// </param>
        /// <param name="xmax">
        /// The xmax.
        /// </param>
        /// <param name="ymin">
        /// The ymin.
        /// </param>
        /// <param name="ymax">
        /// The ymax.
        /// </param>
        public CohenSutherlandClipping(double xmin, double xmax, double ymin, double ymax)
        {
            this.xmin = xmin;
            this.ymin = ymin;
            this.xmax = xmax;
            this.ymax = ymax;
        }

        /// <summary>
        /// Cohen–Sutherland clipping algorithm clips a line from
        /// P0 = (x0, y0) to P1 = (x1, y1) against a rectangle with
        /// diagonal from (xmin, ymin) to (xmax, ymax).
        /// </summary>
        /// <param name="x0">X coordinate of the first point.</param>
        /// <param name="y0">Y coordinate of the first point.</param>
        /// <param name="x1">X coordinate of the second point.</param>
        /// <param name="y1">Y coordinate of the second point.</param>
        /// <returns>
        /// true if the line is inside.
        /// </returns>
        public bool ClipLine(ref double x0, ref double y0, ref double x1, ref double y1)
        {
            // compute outcodes for P0, P1, and whatever point lies outside the clip rectangle
            int outcode0 = this.ComputeOutCode(x0, y0);
            int outcode1 = this.ComputeOutCode(x1, y1);
            bool accept = false;

            while (true)
            {
                if ((outcode0 | outcode1) == 0)
                {
                    // logical or is 0. Trivially accept and get out of loop
                    accept = true;
                    break;
                }

                if ((outcode0 & outcode1) != 0)
                {
                    // logical and is not 0. Trivially reject and get out of loop
                    break;
                }

                // failed both tests, so calculate the line segment to clip
                // from an outside point to an intersection with clip edge
                double x = 0, y = 0;

                // At least one endpoint is outside the clip rectangle; pick it.
                int outcodeOut = outcode0 != 0 ? outcode0 : outcode1;

                // Now find the intersection point;
                // use formulas y = y0 + slope * (x - x0), x = x0 + (1 / slope) * (y - y0)
                if ((outcodeOut & Top) != 0)
                {
                    // point is above the clip rectangle
                    x = x0 + ((x1 - x0) * (this.ymax - y0) / (y1 - y0));
                    y = this.ymax;
                }
                else if ((outcodeOut & Bottom) != 0)
                {
                    // point is below the clip rectangle
                    x = x0 + ((x1 - x0) * (this.ymin - y0) / (y1 - y0));
                    y = this.ymin;
                }
                else if ((outcodeOut & Right) != 0)
                {
                    // point is to the right of clip rectangle
                    y = y0 + ((y1 - y0) * (this.xmax - x0) / (x1 - x0));
                    x = this.xmax;
                }
                else if ((outcodeOut & Left) != 0)
                {
                    // point is to the left of clip rectangle
                    y = y0 + ((y1 - y0) * (this.xmin - x0) / (x1 - x0));
                    x = this.xmin;
                }

                // Now we move outside point to intersection point to clip
                // and get ready for next pass.
                if (outcodeOut == outcode0)
                {
                    x0 = x;
                    y0 = y;
                    outcode0 = this.ComputeOutCode(x0, y0);
                }
                else
                {
                    x1 = x;
                    y1 = y;
                    outcode1 = this.ComputeOutCode(x1, y1);
                }
            }

            return accept;
        }

        /// <summary>
        /// Cohen–Sutherland clipping algorithm clips a line from
        /// P0 = (x0, y0) to P1 = (x1, y1) against a rectangle with
        /// diagonal from (xmin, ymin) to (xmax, ymax).
        /// </summary>
        /// <param name="s0">
        /// The s 0.
        /// </param>
        /// <param name="s1">
        /// The s 1.
        /// </param>
        /// <returns>
        /// true if the line is inside
        /// </returns>
        public bool ClipLine(ref ScreenPoint s0, ref ScreenPoint s1)
        {
            return this.ClipLine(ref s0.x, ref s0.y, ref s1.x, ref s1.y);
        }

        /// <summary>
        /// Determines whether the specified point is inside the rectangle.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <returns>
        ///  <c>true</c> if the specified point is inside; otherwise, <c>false</c>.
        /// </returns>
        public bool IsInside(double x, double y)
        {
            return this.ComputeOutCode(x, y) == Inside;
        }

        /// <summary>
        /// Determines whether the specified point is inside the rectangle.
        /// </summary>
        /// <param name="s">The point.</param>
        /// <returns>
        ///  <c>true</c> if the specified point is inside; otherwise, <c>false</c>.
        /// </returns>
        public bool IsInside(ScreenPoint s)
        {
            return this.ComputeOutCode(s.X, s.Y) == Inside;
        }

        /// <summary>
        /// Computes the out code.
        /// </summary>
        /// <param name="x">
        /// The x.
        /// </param>
        /// <param name="y">
        /// The y.
        /// </param>
        /// <returns>
        /// The out code.
        /// </returns>
        /// <remarks>
        /// Compute the bit code for a point (x, y) using the clip rectangle
        /// bounded diagonally by (xmin, ymin), and (xmax, ymax)
        /// </remarks>
        private int ComputeOutCode(double x, double y)
        {
            int code = Inside; // initialized as being inside of clip window

            if (x < this.xmin)
            {
                // to the left of clip window
                code |= Left;
            }
            else if (x > this.xmax)
            {
                // to the right of clip window
                code |= Right;
            }

            if (y < this.ymin)
            {
                // below the clip window
                code |= Bottom;
            }
            else if (y > this.ymax)
            {
                // above the clip window
                code |= Top;
            }

            return code;
        }
    }
}