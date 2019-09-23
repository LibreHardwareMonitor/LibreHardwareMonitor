// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OxyThickness.cs" company="OxyPlot">
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
//   Describes the thickness of a frame around a rectangle. Four Double values describe the Left, Top, Right, and Bottom sides of the rectangle, respectively.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System.Globalization;

    /// <summary>
    /// Describes the thickness of a frame around a rectangle. Four <see cref="System.Double"/> values describe the left, top, right, and bottom sides of the rectangle, respectively.
    /// </summary>
    public struct OxyThickness : ICodeGenerating
    {
        /// <summary>
        /// The bottom.
        /// </summary>
        private double bottom;

        /// <summary>
        /// The left.
        /// </summary>
        private double left;

        /// <summary>
        /// The right.
        /// </summary>
        private double right;

        /// <summary>
        /// The top.
        /// </summary>
        private double top;

        /// <summary>
        /// Initializes a new instance of the <see cref="OxyThickness"/> struct.
        /// </summary>
        /// <param name="thickness">
        /// The thickness.
        /// </param>
        public OxyThickness(double thickness)
            : this(thickness, thickness, thickness, thickness)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OxyThickness"/> struct.
        /// </summary>
        /// <param name="left">
        /// The left.
        /// </param>
        /// <param name="top">
        /// The top.
        /// </param>
        /// <param name="right">
        /// The right.
        /// </param>
        /// <param name="bottom">
        /// The bottom.
        /// </param>
        public OxyThickness(double left, double top, double right, double bottom)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }

        /// <summary>
        /// Gets or sets the bottom thickness.
        /// </summary>
        /// <value>
        /// The bottom thickness.
        /// </value>
        public double Bottom
        {
            get
            {
                return this.bottom;
            }

            set
            {
                this.bottom = value;
            }
        }

        /// <summary>
        /// Gets the height.
        /// </summary>
        public double Height
        {
            get
            {
                return this.Bottom - this.Top;
            }
        }

        /// <summary>
        /// Gets or sets the left thickness.
        /// </summary>
        /// <value>
        /// The left thickness.
        /// </value>
        public double Left
        {
            get
            {
                return this.left;
            }

            set
            {
                this.left = value;
            }
        }

        /// <summary>
        /// Gets or sets the right thickness.
        /// </summary>
        /// <value>
        /// The right thickness.
        /// </value>
        public double Right
        {
            get
            {
                return this.right;
            }

            set
            {
                this.right = value;
            }
        }

        /// <summary>
        /// Gets or sets the top thickness.
        /// </summary>
        /// <value>
        /// The top thickness.
        /// </value>
        public double Top
        {
            get
            {
                return this.top;
            }

            set
            {
                this.top = value;
            }
        }

        /// <summary>
        /// Gets the width.
        /// </summary>
        public double Width
        {
            get
            {
                return this.Right - this.Left;
            }
        }

        /// <summary>
        /// Returns C# code that generates this instance.
        /// </summary>
        /// <returns>
        /// The to code.
        /// </returns>
        public string ToCode()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "new OxyThickness({0},{1},{2},{3})",
                this.Left,
                this.Top,
                this.Right,
                this.Bottom);
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
                CultureInfo.InvariantCulture, "({0}, {1}, {2}, {3})", this.left, this.top, this.right, this.bottom);
        }
    }
}