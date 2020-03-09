// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Series.cs" company="OxyPlot">
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
//   Abstract base class for all series.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Series
{
    using System.Globalization;

    using OxyPlot.Axes;

    /// <summary>
    /// Provides an abstract base class for plot series.
    /// </summary>
    /// <remarks>
    /// This class contains internal methods that should be called only from the PlotModel.
    /// </remarks>
    public abstract class Series : UIPlotElement, ITrackableSeries
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Series" /> class.
        /// </summary>
        protected Series()
        {
            this.IsVisible = true;
        }

        /// <summary>
        /// Gets the actual culture.
        /// </summary>
        /// <remarks>
        /// The culture is defined in the parent PlotModel.
        /// </remarks>
        public CultureInfo ActualCulture
        {
            get
            {
                return this.PlotModel != null ? this.PlotModel.ActualCulture : CultureInfo.CurrentCulture;
            }
        }

        /// <summary>
        /// Gets or sets the background color of the series. The background area is defined by the x and y axes.
        /// </summary>
        /// <value> The background color. </value>
        public OxyColor Background { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this series is visible.
        /// </summary>
        public bool IsVisible { get; set; }

        /// <summary>
        /// Gets or sets the title of the Series.
        /// </summary>
        /// <value> The title. </value>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets a format string used for the tracker.
        /// </summary>
        public string TrackerFormatString { get; set; }

        /// <summary>
        /// Gets or sets the key for the tracker to use on this series.
        /// </summary>
        public string TrackerKey { get; set; }

        /// <summary>
        /// Gets the point on the series that is nearest the specified point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="interpolate">Interpolate the series if this flag is set to <c>true</c>.</param>
        /// <returns>
        /// A TrackerHitResult for the current hit.
        /// </returns>
        public abstract TrackerHitResult GetNearestPoint(ScreenPoint point, bool interpolate);

        /// <summary>
        /// Renders the series on the specified render context.
        /// </summary>
        /// <param name="rc">
        /// The rendering context.
        /// </param>
        /// <param name="model">
        /// The model.
        /// </param>
        public abstract void Render(IRenderContext rc, PlotModel model);

        /// <summary>
        /// Renders the legend symbol on the specified render context.
        /// </summary>
        /// <param name="rc">
        /// The rendering context.
        /// </param>
        /// <param name="legendBox">
        /// The legend rectangle.
        /// </param>
        public abstract void RenderLegend(IRenderContext rc, OxyRect legendBox);

        /// <summary>
        /// Tests if the plot element is hit by the specified point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>
        /// A hit test result.
        /// </returns>
        protected internal override HitTestResult HitTest(ScreenPoint point, double tolerance)
        {
            var thr = this.GetNearestPoint(point, true) ?? this.GetNearestPoint(point, false);

            if (thr != null)
            {
                double distance = thr.Position.DistanceTo(point);
                if (distance > tolerance)
                {
                    return null;
                }

                return new HitTestResult(thr.Position, thr.Item, thr.Index);
            }

            return null;
        }

        /// <summary>
        /// Check if this data series requires X/Y axes. (e.g. Pie series do not require axes)
        /// </summary>
        /// <returns>
        /// True if no axes are required.
        /// </returns>
        protected internal abstract bool AreAxesRequired();

        /// <summary>
        /// Ensures that the axes of the series is defined.
        /// </summary>
        protected internal abstract void EnsureAxes();

        /// <summary>
        /// Check if the data series is using the specified axis.
        /// </summary>
        /// <param name="axis">
        /// An axis which should be checked if used
        /// </param>
        /// <returns>
        /// True if the axis is in use.
        /// </returns>
        protected internal abstract bool IsUsing(Axis axis);

        /// <summary>
        /// Sets default values (colors, line style etc) from the plot model.
        /// </summary>
        /// <param name="model">
        /// A plot model.
        /// </param>
        protected internal abstract void SetDefaultValues(PlotModel model);

        /// <summary>
        /// Updates the axis maximum and minimum values.
        /// </summary>
        protected internal abstract void UpdateAxisMaxMin();

        /// <summary>
        /// Updates the data.
        /// </summary>
        protected internal abstract void UpdateData();

        /// <summary>
        /// Updates the valid data.
        /// </summary>
        protected internal abstract void UpdateValidData();

        /// <summary>
        /// Updates the maximum and minimum of the series.
        /// </summary>
        protected internal abstract void UpdateMaxMin();

    }
}