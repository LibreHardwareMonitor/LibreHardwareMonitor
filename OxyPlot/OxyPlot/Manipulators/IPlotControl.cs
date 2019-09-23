// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IPlotControl.cs" company="OxyPlot">
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
//   Interface for Plot controls.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using OxyPlot.Annotations;
    using OxyPlot.Axes;
    using OxyPlot.Series;

    /// <summary>
    /// Defines functionality in the Plot controls.
    /// </summary>
    public interface IPlotControl
    {
        /// <summary>
        /// Gets the actual model.
        /// </summary>
        /// <value>The actual model.</value>
        PlotModel ActualModel { get; }

        /// <summary>
        /// Gets the axes from a point.
        /// </summary>
        /// <param name="pt">
        /// The point.
        /// </param>
        /// <param name="xaxis">
        /// The x-axis.
        /// </param>
        /// <param name="yaxis">
        /// The y-axis.
        /// </param>
        void GetAxesFromPoint(ScreenPoint pt, out Axis xaxis, out Axis yaxis);

        /// <summary>
        /// Gets the series from point.
        /// </summary>
        /// <param name="pt">
        /// The point (screen coordinates).
        /// </param>
        /// <param name="limit">
        /// The maximum allowed distance.
        /// </param>
        /// <returns>
        /// The series.
        /// </returns>
        Series.Series GetSeriesFromPoint(ScreenPoint pt, double limit = 100);

        /// <summary>
        /// Hides the tracker.
        /// </summary>
        void HideTracker();

        /// <summary>
        /// Hides the zoom rectangle.
        /// </summary>
        void HideZoomRectangle();

        /// <summary>
        /// Invalidate the plot (not blocking the UI thread)
        /// </summary>
        /// <param name="updateData">
        /// if set to <c>true</c>, all data collections will be updated.
        /// </param>
        void InvalidatePlot(bool updateData = true);

        /// <summary>
        /// Pans the specified axis.
        /// </summary>
        /// <param name="axis">
        /// The axis.
        /// </param>
        /// <param name="ppt">
        /// The previous point (screen coordinates).
        /// </param>
        /// <param name="cpt">
        /// The current point (screen coordinates).
        /// </param>
        void Pan(Axis axis, ScreenPoint ppt, ScreenPoint cpt);

        /// <summary>
        /// Refresh the plot immediately (blocking UI thread)
        /// </summary>
        /// <param name="updateData">
        /// if set to <c>true</c>, all data collections will be updated.
        /// </param>
        void RefreshPlot(bool updateData = true);

        /// <summary>
        /// Resets the specified axis.
        /// </summary>
        /// <param name="axis">
        /// The axis.
        /// </param>
        void Reset(Axis axis);

        /// <summary>
        /// Sets the cursor type.
        /// </summary>
        /// <param name="cursorType">
        /// The cursor type.
        /// </param>
        void SetCursorType(CursorType cursorType);

        /// <summary>
        /// Shows the tracker.
        /// </summary>
        /// <param name="trackerHitResult">
        /// The tracker data.
        /// </param>
        void ShowTracker(TrackerHitResult trackerHitResult);

        /// <summary>
        /// Shows the zoom rectangle.
        /// </summary>
        /// <param name="r">
        /// The rectangle.
        /// </param>
        void ShowZoomRectangle(OxyRect r);

        /// <summary>
        /// Zooms the specified axis to the specified values.
        /// </summary>
        /// <param name="axis">
        /// The axis.
        /// </param>
        /// <param name="p1">
        /// The new minimum value.
        /// </param>
        /// <param name="p2">
        /// The new maximum value.
        /// </param>
        void Zoom(Axis axis, double p1, double p2);

        /// <summary>
        /// Zooms at the specified position.
        /// </summary>
        /// <param name="axis">
        /// The axis.
        /// </param>
        /// <param name="factor">
        /// The zoom factor.
        /// </param>
        /// <param name="x">
        /// The position to zoom at.
        /// </param>
        void ZoomAt(Axis axis, double factor, double x);

    }
}