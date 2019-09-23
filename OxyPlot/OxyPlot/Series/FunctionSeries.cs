// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FunctionSeries.cs" company="OxyPlot">
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
//   Represents a line series that generates its dataset from a function.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Series
{
    using System;

    /// <summary>
    /// Represents a line series that generates its dataset from a function.
    /// </summary>
    /// <remarks>
    /// Define f(x) and make a plot on the range [x0,x1] or define fx(t) and fy(t) and make a plot on the range [t0,t1].
    /// </remarks>
    public class FunctionSeries : LineSeries
    {
        /// <summary>
        /// Initializes a new instance of the <see cref = "FunctionSeries" /> class.
        /// </summary>
        public FunctionSeries()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionSeries"/> class.
        /// </summary>
        /// <param name="f">
        /// The function f(x).
        /// </param>
        /// <param name="x0">
        /// The start x value.
        /// </param>
        /// <param name="x1">
        /// The end x value.
        /// </param>
        /// <param name="dx">
        /// The increment in x.
        /// </param>
        /// <param name="title">
        /// The title (optional).
        /// </param>
        public FunctionSeries(Func<double, double> f, double x0, double x1, double dx, string title = null)
        {
            this.Title = title;
            for (double x = x0; x <= x1 + (dx * 0.5); x += dx)
            {
                this.Points.Add(new DataPoint(x, f(x)));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionSeries"/> class.
        /// </summary>
        /// <param name="f">
        /// The function f(x).
        /// </param>
        /// <param name="x0">
        /// The start x value.
        /// </param>
        /// <param name="x1">
        /// The end x value.
        /// </param>
        /// <param name="n">
        /// The number of points.
        /// </param>
        /// <param name="title">
        /// The title (optional).
        /// </param>
        public FunctionSeries(Func<double, double> f, double x0, double x1, int n, string title = null)
            : this(f, x0, x1, (x1 - x0) / (n - 1), title)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionSeries"/> class.
        /// </summary>
        /// <param name="fx">
        /// The function fx(t).
        /// </param>
        /// <param name="fy">
        /// The function fy(t).
        /// </param>
        /// <param name="t0">
        /// The t0.
        /// </param>
        /// <param name="t1">
        /// The t1.
        /// </param>
        /// <param name="dt">
        /// The increment dt.
        /// </param>
        /// <param name="title">
        /// The title.
        /// </param>
        public FunctionSeries(Func<double, double> fx, Func<double, double> fy, double t0, double t1, double dt, string title = null)
        {
            this.Title = title;
            for (double t = t0; t <= t1 + (dt * 0.5); t += dt)
            {
                this.Points.Add(new DataPoint(fx(t), fy(t)));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionSeries"/> class.
        /// </summary>
        /// <param name="fx">
        /// The function fx(t).
        /// </param>
        /// <param name="fy">
        /// The function fy(t).
        /// </param>
        /// <param name="t0">
        /// The t0.
        /// </param>
        /// <param name="t1">
        /// The t1.
        /// </param>
        /// <param name="n">
        /// The number of points.
        /// </param>
        /// <param name="title">
        /// The title.
        /// </param>
        public FunctionSeries(
            Func<double, double> fx, Func<double, double> fy, double t0, double t1, int n, string title = null)
            : this(fx, fy, t0, t1, (t1 - t0) / (n - 1), title)
        {
        }

    }
}