// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HighLowItem.cs" company="OxyPlot">
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
//   Represents an item in a HighLowSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Series
{
    /// <summary>
    /// Represents an item in a <see cref="HighLowSeries"/>.
    /// </summary>
    public class HighLowItem
    {
        /// <summary>
        /// The undefined.
        /// </summary>
        public static readonly HighLowItem Undefined = new HighLowItem(double.NaN, double.NaN, double.NaN);

        /// <summary>
        /// The close.
        /// </summary>
        private double close;

        /// <summary>
        /// The high.
        /// </summary>
        private double high;

        /// <summary>
        /// The low.
        /// </summary>
        private double low;

        /// <summary>
        /// The open.
        /// </summary>
        private double open;

        /// <summary>
        /// The x.
        /// </summary>
        private double x;

        /// <summary>
        /// Initializes a new instance of the <see cref="HighLowItem"/> class.
        /// </summary>
        public HighLowItem()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HighLowItem"/> struct.
        /// </summary>
        /// <param name="x">
        /// The x value.
        /// </param>
        /// <param name="high">
        /// The high value.
        /// </param>
        /// <param name="low">
        /// The low value.
        /// </param>
        /// <param name="open">
        /// The open value.
        /// </param>
        /// <param name="close">
        /// The close value.
        /// </param>
        public HighLowItem(double x, double high, double low, double open = double.NaN, double close = double.NaN)
        {
            this.x = x;
            this.high = high;
            this.low = low;
            this.open = open;
            this.close = close;
        }

        /// <summary>
        /// Gets or sets the close value.
        /// </summary>
        /// <value>The close value.</value>
        public double Close
        {
            get
            {
                return this.close;
            }

            set
            {
                this.close = value;
            }
        }

        /// <summary>
        /// Gets or sets the high value.
        /// </summary>
        /// <value>The high value.</value>
        public double High
        {
            get
            {
                return this.high;
            }

            set
            {
                this.high = value;
            }
        }

        /// <summary>
        /// Gets or sets the low value.
        /// </summary>
        /// <value>The low value.</value>
        public double Low
        {
            get
            {
                return this.low;
            }

            set
            {
                this.low = value;
            }
        }

        /// <summary>
        /// Gets or sets the open value.
        /// </summary>
        /// <value>The open value.</value>
        public double Open
        {
            get
            {
                return this.open;
            }

            set
            {
                this.open = value;
            }
        }

        /// <summary>
        /// Gets or sets the X value (time).
        /// </summary>
        /// <value>The X value.</value>
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

    }
}