// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ScreenPoint.cs" company="OxyPlot">
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
//   Describes a point defined in the screen coordinate system.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System;

    /// <summary>
    /// Represents a point defined in the screen coordinate system.
    /// </summary>
    /// <remarks>
    /// The rendering methods transforms <see cref="DataPoint"/>s to <see cref="ScreenPoint"/>s.
    /// </remarks>
    public struct ScreenPoint
    {
        /// <summary>
        /// The undefined point.
        /// </summary>
        public static readonly ScreenPoint Undefined = new ScreenPoint(double.NaN, double.NaN);

        /// <summary>
        /// The x-coordinate.
        /// </summary>
        internal double x;

        /// <summary>
        /// The y-coordinate.
        /// </summary>
        internal double y;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScreenPoint"/> struct.
        /// </summary>
        /// <param name="x">
        /// The x-coordinate.
        /// </param>
        /// <param name="y">
        /// The y-coordinate.
        /// </param>
        public ScreenPoint(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        /// <summary>
        /// Gets or sets the x-coordinate.
        /// </summary>
        /// <value> The x-coordinate. </value>
        public double X
        {
            get
            {
                return this.x;
            }

            set
            {
                this.x = value;
            }
        }

        /// <summary>
        /// Gets or sets the y-coordinate.
        /// </summary>
        /// <value> The y-coordinate. </value>
        public double Y
        {
            get
            {
                return this.y;
            }

            set
            {
                this.y = value;
            }
        }

        /// <summary>
        /// Determines whether the specified point is undefined.
        /// </summary>
        /// <param name="point">
        /// The point.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified point is undefined; otherwise, <c>false</c> .
        /// </returns>
        public static bool IsUndefined(ScreenPoint point)
        {
            return double.IsNaN(point.X) && double.IsNaN(point.Y);
        }

        /// <summary>
        /// Gets the distance to the specified point.
        /// </summary>
        /// <param name="point">
        /// The point.
        /// </param>
        /// <returns>
        /// The distance.
        /// </returns>
        public double DistanceTo(ScreenPoint point)
        {
            double dx = point.x - this.x;
            double dy = point.y - this.y;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }

        /// <summary>
        /// Gets the squared distance to the specified point.
        /// </summary>
        /// <param name="point">
        /// The point.
        /// </param>
        /// <returns>
        /// The squared distance.
        /// </returns>
        public double DistanceToSquared(ScreenPoint point)
        {
            double dx = point.x - this.x;
            double dy = point.y - this.y;
            return (dx * dx) + (dy * dy);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.x + " " + this.y;
        }

        /// <summary>
        /// Translates a <see cref="ScreenPoint"/> by a <see cref="ScreenVector"/>.
        /// </summary>
        /// <param name="p1"> The point. </param>
        /// <param name="p2"> The vector. </param>
        /// <returns> The translated point. </returns>
        public static ScreenPoint operator +(ScreenPoint p1, ScreenVector p2)
        {
            return new ScreenPoint(p1.x + p2.x, p1.y + p2.y);
        }

        /// <summary>
        /// Subtracts a <see cref="ScreenPoint"/> from a <see cref="ScreenPoint"/>
        /// and returns the result as a <see cref="ScreenVector"/>.
        /// </summary>
        /// <param name="p1"> The point on which to perform the subtraction. </param>
        /// <param name="p2"> The point to subtract from p1. </param>
        /// <returns> A <see cref="ScreenVector"/> structure that represents the difference between p1 and p2. </returns>
        public static ScreenVector operator -(ScreenPoint p1, ScreenPoint p2)
        {
            return new ScreenVector(p1.x - p2.x, p1.y - p2.y);
        }

        /// <summary>
        /// Subtracts a <see cref="ScreenVector"/> from a <see cref="ScreenPoint"/> 
        /// and returns the result as a <see cref="ScreenPoint"/>.
        /// </summary>
        /// <param name="point"> The point on which to perform the subtraction. </param>
        /// <param name="vector"> The vector to subtract from p1. </param>
        /// <returns> A <see cref="ScreenPoint"/> that represents point translated by the negative vector. </returns>
        public static ScreenPoint operator -(ScreenPoint point, ScreenVector vector)
        {
            return new ScreenPoint(point.x - vector.x, point.y - vector.y);
        }
    }
}