// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ITrackableSeries.cs" company="OxyPlot">
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
//   Interface for Series that can be 'tracked'
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Series
{
    /// <summary>
    /// Provides functionality to return data for a tracker control.
    /// </summary>
    /// <remarks>
    /// The plot control will show a tracker with the current value when moving the mouse over the data.
    /// </remarks>
    public interface ITrackableSeries
    {
        /// <summary>
        /// Gets a format string used for the tracker.
        /// </summary>
        /// <remarks>
        /// The fields that can be used in the format string depends on the series.
        /// </remarks>
        string TrackerFormatString { get; }

        /// <summary>
        /// Gets the tracker key.
        /// </summary>
        string TrackerKey { get; }

        /// <summary>
        /// Gets the point on the series that is nearest the specified point.
        /// </summary>
        /// <param name="point">
        /// The point.
        /// </param>
        /// <param name="interpolate">
        /// Interpolate the series if this flag is set to <c>true</c>.
        /// </param>
        /// <returns>
        /// A TrackerHitResult for the current hit.
        /// </returns>
        TrackerHitResult GetNearestPoint(ScreenPoint point, bool interpolate);
    }
}