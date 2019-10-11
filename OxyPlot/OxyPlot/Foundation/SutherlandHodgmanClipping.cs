// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SutherlandHodgmanClipping.cs" company="OxyPlot">
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
//   Polygon clipping by the sutherland-hodgman algortihm.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Provides polygon clipping by the Sutherland-Hodgman algortihm.
    /// </summary>
    public static class SutherlandHodgmanClipping
    {
        /// <summary>
        /// The rectangle edge.
        /// </summary>
        private enum RectangleEdge
        {
            /// <summary>
            /// The left.
            /// </summary>
            Left,

            /// <summary>
            /// The right.
            /// </summary>
            Right,

            /// <summary>
            /// The top.
            /// </summary>
            Top,

            /// <summary>
            /// The bottom.
            /// </summary>
            Bottom
        }

        /// <summary>
        /// The Sutherland-Hodgman polygon clipping algorithm.
        /// </summary>
        /// <remarks>
        /// See http://ezekiel.vancouver.wsu.edu/~cs442/lectures/clip/clip/index.html
        /// </remarks>
        /// <param name="bounds">
        /// The bounds.
        /// </param>
        /// <param name="v">
        /// The polygon points.
        /// </param>
        /// <returns>
        /// The clipped points.
        /// </returns>
        public static List<ScreenPoint> ClipPolygon(OxyRect bounds, IList<ScreenPoint> v)
        {
            List<ScreenPoint> p1 = ClipOneAxis(bounds, RectangleEdge.Left, v);
            List<ScreenPoint> p2 = ClipOneAxis(bounds, RectangleEdge.Right, p1);
            List<ScreenPoint> p3 = ClipOneAxis(bounds, RectangleEdge.Top, p2);
            return ClipOneAxis(bounds, RectangleEdge.Bottom, p3);
        }

        /// <summary>
        /// Clips to one axis.
        /// </summary>
        /// <param name="bounds">
        /// The bounds.
        /// </param>
        /// <param name="edge">
        /// The edge.
        /// </param>
        /// <param name="v">
        /// The points of the polygon.
        /// </param>
        /// <returns>
        /// The clipped points.
        /// </returns>
        private static List<ScreenPoint> ClipOneAxis(OxyRect bounds, RectangleEdge edge, IList<ScreenPoint> v)
        {
            if (v.Count == 0)
            {
                return new List<ScreenPoint>();
            }

            var polygon = new List<ScreenPoint>(v.Count);

            var s = v[v.Count - 1];

            for (int i = 0; i < v.Count; ++i)
            {
                var p = v[i];
                bool pin = IsInside(bounds, edge, p);
                bool sin = IsInside(bounds, edge, s);

                if (sin && pin)
                {
                    // case 1: inside -> inside
                    polygon.Add(p);
                }
                else if (sin)
                {
                    // case 2: inside -> outside
                    polygon.Add(LineIntercept(bounds, edge, s, p));
                }
                else if (!pin)
                {
                    // case 3: outside -> outside
                    // emit nothing
                }
                else
                {
                    // case 4: outside -> inside
                    polygon.Add(LineIntercept(bounds, edge, s, p));
                    polygon.Add(p);
                }

                s = p;
            }

            return polygon;
        }

        /// <summary>
        /// Determines whether the specified point is inside the edge/bounds.
        /// </summary>
        /// <param name="bounds">The bounds.</param>
        /// <param name="edge">The edge to test.</param>
        /// <param name="p">The point.</param>
        /// <returns>
        ///  <c>true</c> if the specified point is inside; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsInside(OxyRect bounds, RectangleEdge edge, ScreenPoint p)
        {
            switch (edge)
            {
                case RectangleEdge.Left:
                    return !(p.X < bounds.Left);

                case RectangleEdge.Right:
                    return !(p.X >= bounds.Right);

                case RectangleEdge.Top:
                    return !(p.Y < bounds.Top);

                case RectangleEdge.Bottom:
                    return !(p.Y >= bounds.Bottom);

                default:
                    throw new ArgumentException("edge");
            }
        }

        /// <summary>
        /// Fines the edge interception.
        /// </summary>
        /// <param name="bounds">The bounds.</param>
        /// <param name="edge">The edge.</param>
        /// <param name="a">The first point.</param>
        /// <param name="b">The second point.</param>
        /// <returns>The interception.</returns>
        private static ScreenPoint LineIntercept(OxyRect bounds, RectangleEdge edge, ScreenPoint a, ScreenPoint b)
        {
            if (a.x == b.x && a.y == b.y)
            {
                return a;
            }

            switch (edge)
            {
                case RectangleEdge.Bottom:
                    if (b.Y == a.Y)
                    {
                        throw new ArgumentException("no intercept found");
                    }

                    return new ScreenPoint(a.X + (((b.X - a.X) * (bounds.Bottom - a.Y)) / (b.Y - a.Y)), bounds.Bottom);

                case RectangleEdge.Left:
                    if (b.X == a.X)
                    {
                        throw new ArgumentException("no intercept found");
                    }

                    return new ScreenPoint(bounds.Left, a.Y + (((b.Y - a.Y) * (bounds.Left - a.X)) / (b.X - a.X)));

                case RectangleEdge.Right:
                    if (b.X == a.X)
                    {
                        throw new ArgumentException("no intercept found");
                    }

                    return new ScreenPoint(bounds.Right, a.Y + (((b.Y - a.Y) * (bounds.Right - a.X)) / (b.X - a.X)));

                case RectangleEdge.Top:
                    if (b.Y == a.Y)
                    {
                        throw new ArgumentException("no intercept found");
                    }

                    return new ScreenPoint(a.X + (((b.X - a.X) * (bounds.Top - a.Y)) / (b.Y - a.Y)), bounds.Top);
            }

            throw new ArgumentException("no intercept found");
        }

    }
}