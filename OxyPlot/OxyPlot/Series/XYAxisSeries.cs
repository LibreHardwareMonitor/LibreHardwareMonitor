// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XYAxisSeries.cs" company="OxyPlot">
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
//   Abstract base class for series that contains an X-axis and Y-axis.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Series
{
    using System;
    using System.Collections.Generic;

    using OxyPlot.Axes;

    /// <summary>
    /// Provides an abstract base class for series that are related to an X-axis and a Y-axis.
    /// </summary>
    public abstract class XYAxisSeries : ItemsSeries
    {
        /// <summary>
        /// Gets or sets the maximum x-coordinate of the dataset.
        /// </summary>
        /// <value> The maximum x-coordinate. </value>
        public double MaxX { get; protected set; }

        /// <summary>
        /// Gets or sets the maximum y-coordinate of the dataset.
        /// </summary>
        /// <value> The maximum y-coordinate. </value>
        public double MaxY { get; protected set; }

        /// <summary>
        /// Gets or sets the minimum x-coordinate of the dataset.
        /// </summary>
        /// <value> The minimum x-coordinate. </value>
        public double MinX { get; protected set; }

        /// <summary>
        /// Gets or sets the minimum y-coordinate of the dataset.
        /// </summary>
        /// <value> The minimum y-coordinate. </value>
        public double MinY { get; protected set; }

        /// <summary>
        /// Gets the x-axis.
        /// </summary>
        /// <value> The x-axis. </value>
        public Axis XAxis { get; private set; }

        /// <summary>
        /// Gets or sets the x-axis key.
        /// </summary>
        /// <value> The x-axis key. </value>
        public string XAxisKey { get; set; }

        /// <summary>
        /// Gets the y-axis.
        /// </summary>
        /// <value> The y-axis. </value>
        public Axis YAxis { get; private set; }

        /// <summary>
        /// Gets or sets the y-axis key.
        /// </summary>
        /// <value> The y-axis key. </value>
        public string YAxisKey { get; set; }

        /// <summary>
        /// Gets the rectangle the series uses on the screen (screen coordinates).
        /// </summary>
        /// <returns>
        /// The rectangle.
        /// </returns>
        public OxyRect GetScreenRectangle()
        {
            return this.GetClippingRect();
        }

        /// <summary>
        /// Renders the legend symbol on the specified rendering context.
        /// </summary>
        /// <param name="rc">
        /// The rendering context.
        /// </param>
        /// <param name="legendBox">
        /// The legend rectangle.
        /// </param>
        public override void RenderLegend(IRenderContext rc, OxyRect legendBox)
        {
        }

        /// <summary>
        /// Transforms from a screen point to a data point by the axes of this series.
        /// </summary>
        /// <param name="p">
        /// The screen point.
        /// </param>
        /// <returns>
        /// A data point.
        /// </returns>
        public DataPoint InverseTransform(ScreenPoint p)
        {
            return this.XAxis.InverseTransform(p.X, p.Y, this.YAxis);
        }

        /// <summary>
        /// Transforms the specified coordinates to a screen point by the axes of this series.
        /// </summary>
        /// <param name="x">
        /// The x coordinate.
        /// </param>
        /// <param name="y">
        /// The y coordinate.
        /// </param>
        /// <returns>
        /// A screen point.
        /// </returns>
        public ScreenPoint Transform(double x, double y)
        {
            return this.XAxis.Transform(x, y, this.YAxis);
        }

        /// <summary>
        /// Transforms the specified data point to a screen point by the axes of this series.
        /// </summary>
        /// <param name="p">
        /// The point.
        /// </param>
        /// <returns>
        /// A screen point.
        /// </returns>
        public ScreenPoint Transform(IDataPoint p)
        {
            return this.XAxis.Transform(p.X, p.Y, this.YAxis);
        }

        /// <summary>
        /// Check if this data series requires X/Y axes. (e.g. Pie series do not require axes)
        /// </summary>
        /// <returns>
        /// The are axes required.
        /// </returns>
        protected internal override bool AreAxesRequired()
        {
            return true;
        }

        /// <summary>
        /// Ensures that the axes of the series is defined.
        /// </summary>
        protected internal override void EnsureAxes()
        {
            this.XAxis = PlotModel.GetAxisOrDefault(this.XAxisKey, PlotModel.DefaultXAxis);
            this.YAxis = PlotModel.GetAxisOrDefault(this.YAxisKey, PlotModel.DefaultYAxis);
        }

        /// <summary>
        /// Check if the data series is using the specified axis.
        /// </summary>
        /// <param name="axis">
        /// An axis.
        /// </param>
        /// <returns>
        /// True if the axis is in use.
        /// </returns>
        protected internal override bool IsUsing(Axis axis)
        {
            return false;
        }

        /// <summary>
        /// Sets default values from the plot model.
        /// </summary>
        /// <param name="model">
        /// The plot model.
        /// </param>
        protected internal override void SetDefaultValues(PlotModel model)
        {
        }

        /// <summary>
        /// Updates the axes to include the max and min of this series.
        /// </summary>
        protected internal override void UpdateAxisMaxMin()
        {
            this.XAxis.Include(this.MinX);
            this.XAxis.Include(this.MaxX);
            this.YAxis.Include(this.MinY);
            this.YAxis.Include(this.MaxY);
        }

        /// <summary>
        /// Updates the data.
        /// </summary>
        protected internal override void UpdateData()
        {
        }

        /// <summary>
        /// Updates the max/minimum values.
        /// </summary>
        protected internal override void UpdateMaxMin()
        {
            this.MinX = this.MinY = this.MaxX = this.MaxY = double.NaN;
        }

        /// <summary>
        /// Gets the clipping rectangle.
        /// </summary>
        /// <returns>
        /// The clipping rectangle.
        /// </returns>
        protected OxyRect GetClippingRect()
        {
            double minX = Math.Min(this.XAxis.ScreenMin.X, this.XAxis.ScreenMax.X);
            double minY = Math.Min(this.YAxis.ScreenMin.Y, this.YAxis.ScreenMax.Y);
            double maxX = Math.Max(this.XAxis.ScreenMin.X, this.XAxis.ScreenMax.X);
            double maxY = Math.Max(this.YAxis.ScreenMin.Y, this.YAxis.ScreenMax.Y);

            return new OxyRect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// Gets the point on the curve that is nearest the specified point.
        /// </summary>
        /// <param name="points">
        /// The point list.
        /// </param>
        /// <param name="point">
        /// The point.
        /// </param>
        /// <returns>
        /// A tracker hit result if a point was found.
        /// </returns>
        protected TrackerHitResult GetNearestInterpolatedPointInternal(IList<IDataPoint> points, ScreenPoint point)
        {
            if (this.XAxis == null || this.YAxis == null || points == null)
            {
                return null;
            }

            var spn = default(ScreenPoint);
            var dpn = default(DataPoint);
            double index = -1;

            double minimumDistance = double.MaxValue;

            for (int i = 0; i + 1 < points.Count; i++)
            {
                var p1 = points[i];
                var p2 = points[i + 1];
                if (!this.IsValidPoint(p1, this.XAxis, this.YAxis) || !this.IsValidPoint(p2, this.XAxis, this.YAxis))
                {
                    continue;
                }

                var sp1 = this.Transform(p1);
                var sp2 = this.Transform(p2);

                // Find the nearest point on the line segment.
                var spl = ScreenPointHelper.FindPointOnLine(point, sp1, sp2);

                if (ScreenPoint.IsUndefined(spl))
                {
                    // P1 && P2 coincident
                    continue;
                }

                double l2 = (point - spl).LengthSquared;

                if (l2 < minimumDistance)
                {
                    double u = (spl - sp1).Length / (sp2 - sp1).Length;
                    dpn = new DataPoint(p1.X + (u * (p2.X - p1.X)), p1.Y + (u * (p2.Y - p1.Y)));
                    spn = spl;
                    minimumDistance = l2;
                    index = i + u;
                }
            }

            if (minimumDistance < double.MaxValue)
            {
                object item = this.GetItem((int)index);
                return new TrackerHitResult(this, dpn, spn, item) { Index = index };
            }

            return null;
        }

        /// <summary>
        /// Gets the nearest point.
        /// </summary>
        /// <param name="points">
        /// The points (data coordinates).
        /// </param>
        /// <param name="point">
        /// The point (screen coordinates).
        /// </param>
        /// <returns>
        /// A <see cref="TrackerHitResult"/> if a point was found, null otherwise.
        /// </returns>
        protected TrackerHitResult GetNearestPointInternal(IEnumerable<IDataPoint> points, ScreenPoint point)
        {
            var spn = default(ScreenPoint);
            IDataPoint dpn = default(DataPoint);
            double index = -1;

            double minimumDistance = double.MaxValue;
            int i = 0;
            foreach (var p in points)
            {
                if (!this.IsValidPoint(p, this.XAxis, this.YAxis))
                {
                    continue;
                }

                var sp = Axis.Transform(p, this.XAxis, this.YAxis);
                double d2 = (sp - point).LengthSquared;

                if (d2 < minimumDistance)
                {
                    dpn = p;
                    spn = sp;
                    minimumDistance = d2;
                    index = i;
                }

                i++;
            }

            if (minimumDistance < double.MaxValue)
            {
                object item = this.GetItem((int)index);
                return new TrackerHitResult(this, dpn, spn, item) { Index = index };
            }

            return null;
        }

        /// <summary>
        /// Determines whether the specified point is valid.
        /// </summary>
        /// <param name="pt">
        /// The point.
        /// </param>
        /// <param name="xaxis">
        /// The x axis.
        /// </param>
        /// <param name="yaxis">
        /// The y axis.
        /// </param>
        /// <returns>
        /// <c>true</c> if the point is valid; otherwise, <c>false</c> .
        /// </returns>
        protected virtual bool IsValidPoint(IDataPoint pt, Axis xaxis, Axis yaxis)
        {
            return !double.IsNaN(pt.X) && !double.IsInfinity(pt.X) && !double.IsNaN(pt.Y) && !double.IsInfinity(pt.Y)
                   && (xaxis != null && xaxis.IsValidValue(pt.X)) && (yaxis != null && yaxis.IsValidValue(pt.Y));
        }

        /// <summary>
        /// Converts the value of the specified object to a double precision floating point number. DateTime objects are converted using DateTimeAxis.ToDouble and TimeSpan objects are converted using TimeSpanAxis.ToDouble
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <returns>
        /// The floating point number value.
        /// </returns>
        protected virtual double ToDouble(object value)
        {
            if (value is DateTime)
            {
                return DateTimeAxis.ToDouble((DateTime)value);
            }

            if (value is TimeSpan)
            {
                return ((TimeSpan)value).TotalSeconds;
            }

            return Convert.ToDouble(value);
        }

        /// <summary>
        /// Verifies that both axes are defined.
        /// </summary>
        protected void VerifyAxes()
        {
            if (this.XAxis == null)
            {
                throw new InvalidOperationException("XAxis not defined.");
            }

            if (this.YAxis == null)
            {
                throw new InvalidOperationException("YAxis not defined.");
            }
        }
    }
}