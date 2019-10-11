// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeIntervalType.cs" company="OxyPlot">
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
//   Defines the date time interval for DateTimeAxis.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Axes
{
    /// <summary>
    /// Specifies the date time interval for <see cref="DateTimeAxis"/>.
    /// </summary>
    public enum DateTimeIntervalType
    {
        /// <summary>
        /// Automatically determine interval.
        /// </summary>
        Auto = 0,

        /// <summary>
        /// Manual definition of intervals.
        /// </summary>
        Manual = 1,

        /// <summary>
        /// Interval type is milliseconds.
        /// </summary>
        Milliseconds = 2,

        /// <summary>
        /// Interval type is seconds.
        /// </summary>
        Seconds = 3,

        /// <summary>
        /// Interval type is minutes.
        /// </summary>
        Minutes = 4,

        /// <summary>
        /// Interval type is hours.
        /// </summary>
        Hours = 5,

        /// <summary>
        /// Interval type is days.
        /// </summary>
        Days = 6,

        /// <summary>
        /// Interval type is weeks.
        /// </summary>
        Weeks = 7,

        /// <summary>
        /// Interval type is months.
        /// </summary>
        Months = 8,

        /// <summary>
        /// Interval type is years.
        /// </summary>
        Years = 9,
    }
}