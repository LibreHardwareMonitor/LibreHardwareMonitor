// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ScreenVector.cs" company="OxyPlot">
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
//   Represents a vector defined in the screen coordinate system.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System;

    /// <summary>
    /// Represents a vector defined in the screen coordinate system.
    /// </summary>
    public struct ScreenVector
    {
        /// <summary>
        /// The x-coordinate.
        /// </summary>
        internal double x;

        /// <summary>
        /// The y-coordinate.
        /// </summary>
        internal double y;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScreenVector"/> structure.
        /// </summary>
        /// <param name="x">
        /// The x-coordinate.
        /// </param>
        /// <param name="y">
        /// The y-coordinate.
        /// </param>
        public ScreenVector(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        /// <summary>
        /// Gets the length.
        /// </summary>
        public double Length
        {
            get
            {
                return Math.Sqrt((this.x * this.x) + (this.y * this.y));
            }
        }

        /// <summary>
        /// Gets the length squared.
        /// </summary>
        public double LengthSquared
        {
            get
            {
                return (this.x * this.x) + (this.y * this.y);
            }
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
        /// Normalizes this vector.
        /// </summary>
        public void Normalize()
        {
            double l = Math.Sqrt((this.x * this.x) + (this.y * this.y));
            if (l > 0)
            {
                this.x /= l;
                this.y /= l;
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
            return this.x + " " + this.y;
        }

        /// <summary>
        /// Implements the operator *.
        /// </summary>
        /// <param name="v"> The vector. </param>
        /// <param name="d"> The multiplication factor. </param>
        /// <returns> The result of the operator. </returns>
        public static ScreenVector operator *(ScreenVector v, double d)
        {
            return new ScreenVector(v.x * d, v.y * d);
        }
    }
}