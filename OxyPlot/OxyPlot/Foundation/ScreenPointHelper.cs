// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ScreenPointHelper.cs" company="OxyPlot">
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
//   Provides various algorithms for polygons and lines of ScreenPoint.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System.Collections.Generic;

    /// <summary>
    /// Provides algorithms for polygons and lines of <see cref="ScreenPoint"/>.
    /// </summary>
    public static class ScreenPointHelper
    {
        /// <summary>
        /// Finds the nearest point on the specified polyline.
        /// </summary>
        /// <param name="point">
        /// The point.
        /// </param>
        /// <param name="points">
        /// The points.
        /// </param>
        /// <returns>
        /// The nearest point.
        /// </returns>
        public static ScreenPoint FindNearestPointOnPolyline(ScreenPoint point, IList<ScreenPoint> points)
        {
            double minimumDistance = double.MaxValue;
            var nearestPoint = default(ScreenPoint);

            for (int i = 0; i + 1 < points.Count; i++)
            {
                var p1 = points[i];
                var p2 = points[i + 1];
                if (ScreenPoint.IsUndefined(p1) || ScreenPoint.IsUndefined(p2))
                {
                    continue;
                }

                // Find the nearest point on the line segment.
                var nearestPointOnSegment = FindPointOnLine(point, p1, p2);

                if (ScreenPoint.IsUndefined(nearestPointOnSegment))
                {
                    continue;
                }

                double l2 = (point - nearestPointOnSegment).LengthSquared;

                if (l2 < minimumDistance)
                {
                    nearestPoint = nearestPointOnSegment;
                    minimumDistance = l2;
                }
            }

            return nearestPoint;
        }

        /// <summary>
        /// Finds the point on line.
        /// </summary>
        /// <param name="p">
        /// The point.
        /// </param>
        /// <param name="p1">
        /// The first point on the line.
        /// </param>
        /// <param name="p2">
        /// The second point on the line.
        /// </param>
        /// <returns>
        /// The nearest point on the line.
        /// </returns>
        /// <remarks>
        /// See <a href="http://paulbourke.net/geometry/pointlineplane/">Bourke</a>.
        /// </remarks>
        public static ScreenPoint FindPointOnLine(ScreenPoint p, ScreenPoint p1, ScreenPoint p2)
        {
            double dx = p2.x - p1.x;
            double dy = p2.y - p1.y;
            double u = FindPositionOnLine(p, p1, p2);

            if (double.IsNaN(u))
            {
                u = 0;
            }

            if (u < 0)
            {
                u = 0;
            }

            if (u > 1)
            {
                u = 1;
            }

            return new ScreenPoint(p1.x + (u * dx), p1.y + (u * dy));
        }

        /// <summary>
        /// Finds the nearest point on line.
        /// </summary>
        /// <param name="p">
        /// The point.
        /// </param>
        /// <param name="p1">
        /// The start point on the line.
        /// </param>
        /// <param name="p2">
        /// The end point on the line.
        /// </param>
        /// <returns>
        /// The relative position of the nearest point.
        /// </returns>
        /// <remarks>
        /// See <a href="http://paulbourke.net/geometry/pointlineplane/">Bourke</a>.
        /// </remarks>
        public static double FindPositionOnLine(ScreenPoint p, ScreenPoint p1, ScreenPoint p2)
        {
            double dx = p2.x - p1.x;
            double dy = p2.y - p1.y;
            double u1 = ((p.x - p1.x) * dx) + ((p.y - p1.y) * dy);
            double u2 = (dx * dx) + (dy * dy);

            if (u2 < 1e-6)
            {
                return double.NaN;
            }

            return u1 / u2;
        }

        /// <summary>
        /// Determines whether the specified point is in the specified polygon.
        /// </summary>
        /// <param name="p">
        /// The point.
        /// </param>
        /// <param name="pts">
        /// The polygon points.
        /// </param>
        /// <returns>
        /// <c>true</c> if the point is in the polygon; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsPointInPolygon(ScreenPoint p, IList<ScreenPoint> pts)
        {
            int nvert = pts.Count;
            bool c = false;
            for (int i = 0, j = nvert - 1; i < nvert; j = i++)
            {
                if (((pts[i].Y > p.Y) != (pts[j].Y > p.Y))
                    && (p.X < ((pts[j].X - pts[i].X) * ((p.Y - pts[i].Y) / (pts[j].Y - pts[i].Y))) + pts[i].X))
                {
                    c = !c;
                }
            }

            return c;
        }

        /// <summary>
        /// Resamples the points with the specified point distance limit.
        /// </summary>
        /// <param name="allPoints">
        /// All points.
        /// </param>
        /// <param name="minimumDistance">
        /// The minimum squared distance.
        /// </param>
        /// <returns>
        /// List of resampled points.
        /// </returns>
        public static IList<ScreenPoint> ResamplePoints(IList<ScreenPoint> allPoints, double minimumDistance)
        {
            double minimumSquaredDistance = minimumDistance * minimumDistance;
            int n = allPoints.Count;
            var result = new List<ScreenPoint>(n);
            if (n > 0)
            {
                result.Add(allPoints[0]);
                int i0 = 0;
                for (int i = 1; i < n; i++)
                {
                    double distSquared = allPoints[i0].DistanceToSquared(allPoints[i]);
                    if (distSquared < minimumSquaredDistance && i != n - 1)
                    {
                        continue;
                    }

                    i0 = i;
                    result.Add(allPoints[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the centroid of the specified polygon.
        /// </summary>
        /// <param name="points">
        /// The points.
        /// </param>
        /// <returns>
        /// The centroid.
        /// </returns>
        public static ScreenPoint GetCentroid(IList<ScreenPoint> points)
        {
            double cx = 0;
            double cy = 0;
            double a = 0;

            for (int i = 0; i < points.Count; i++)
            {
                int i1 = (i + 1) % points.Count;
                double da = (points[i].x * points[i1].y) - (points[i1].x * points[i].y);
                cx += (points[i].x + points[i1].x) * da;
                cy += (points[i].y + points[i1].y) * da;
                a += da;
            }

            a *= 0.5;
            cx /= 6 * a;
            cy /= 6 * a;
            return new ScreenPoint(cx, cy);
        }
    }
}