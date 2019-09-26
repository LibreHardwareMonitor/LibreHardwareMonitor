// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DataPointSeries.cs" company="OxyPlot">
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
//   Base class for series that contain a collection of IDataPoints.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Series
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// Provides an abstract base class for series that contain a collection of <see cref="IDataPoint"/>s.
    /// </summary>
    public abstract class DataPointSeries : XYAxisSeries
    {
        /// <summary>
        /// The list of data points.
        /// </summary>
        private IList<IDataPoint> points = new List<IDataPoint>();

        /// <summary>
        /// Initializes a new instance of the <see cref = "DataPointSeries" /> class.
        /// </summary>
        protected DataPointSeries()
        {
            this.DataFieldX = null;
            this.DataFieldY = null;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the tracker can interpolate points.
        /// </summary>
        public bool CanTrackerInterpolatePoints { get; set; }

        /// <summary>
        /// Gets or sets the data field X.
        /// </summary>
        /// <value>The data field X.</value>
        public string DataFieldX { get; set; }

        /// <summary>
        /// Gets or sets the data field Y.
        /// </summary>
        /// <value>The data field Y.</value>
        public string DataFieldY { get; set; }

        /// <summary>
        /// Gets or sets the mapping delegate.
        /// Example: series1.Mapping = item => new DataPoint(((MyType)item).Time,((MyType)item).Value);
        /// </summary>
        /// <value>The mapping.</value>
        public Func<object, IDataPoint> Mapping { get; set; }

        /// <summary>
        /// Gets or sets the points list.
        /// </summary>
        /// <value>The points list.</value>
        public IList<IDataPoint> Points
        {
            get
            {
                return this.points;
            }

            set
            {
                this.points = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref = "DataPointSeries" /> is smooth.
        /// </summary>
        /// <value><c>true</c> if smooth; otherwise, <c>false</c>.</value>
        public bool Smooth { get; set; }

        /// <summary>
        /// Gets the point on the series that is nearest the specified point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="interpolate">Interpolate the series if this flag is set to <c>true</c>.</param>
        /// <returns>
        /// A TrackerHitResult for the current hit.
        /// </returns>
        public override TrackerHitResult GetNearestPoint(ScreenPoint point, bool interpolate)
        {
            if (interpolate && !this.CanTrackerInterpolatePoints)
            {
                return null;
            }

            if (interpolate)
            {
                return this.GetNearestInterpolatedPointInternal(this.Points, point);
            }

            return this.GetNearestPointInternal(this.Points, point);
        }

        /// <summary>
        /// Gets the item at the specified index.
        /// </summary>
        /// <param name="i">The index of the item.</param>
        /// <returns>The item of the index.</returns>
        protected override object GetItem(int i)
        {
            if (this.ItemsSource == null && this.Points != null && i < this.Points.Count)
            {
                return this.Points[i];
            }

            return base.GetItem(i);
        }

        /// <summary>
        /// The update data.
        /// </summary>
        protected internal override void UpdateData()
        {
            if (this.ItemsSource == null)
            {
                return;
            }

            this.AddDataPoints(this.Points);
        }

        /// <summary>
        /// Updates the max/min from the datapoints.
        /// </summary>
        protected internal override void UpdateMaxMin()
        {
            base.UpdateMaxMin();
            this.InternalUpdateMaxMin(this.Points);
        }

        /// <summary>
        /// The add data points.
        /// </summary>
        /// <param name="pts">
        /// The points.
        /// </param>
        protected void AddDataPoints(IList<IDataPoint> pts)
        {
            pts.Clear();

            // Use the mapping to generate the points
            if (this.Mapping != null)
            {
                foreach (var item in this.ItemsSource)
                {
                    pts.Add(this.Mapping(item));
                }

                return;
            }

            // Get DataPoints from the items in ItemsSource
            // if they implement IDataPointProvider
            // If DataFields are set, this is not used
            if (this.DataFieldX == null || this.DataFieldY == null)
            {
                foreach (var item in this.ItemsSource)
                {
                    var dp = item as IDataPoint;
                    if (dp != null)
                    {
                        pts.Add(dp);
                        continue;
                    }

                    var idpp = item as IDataPointProvider;
                    if (idpp == null)
                    {
                        continue;
                    }

                    pts.Add(idpp.GetDataPoint());
                }
            }
            else
            {
                // TODO: is there a better way to do this?
                // http://msdn.microsoft.com/en-us/library/bb613546.aspx

                // Using reflection on DataFieldX and DataFieldY
                this.AddDataPoints((IList)pts, this.ItemsSource, this.DataFieldX, this.DataFieldY);
            }
        }

        /// <summary>
        /// The add data points.
        /// </summary>
        /// <param name="dest">
        /// The dest.
        /// </param>
        /// <param name="itemsSource">
        /// The items source.
        /// </param>
        /// <param name="dataFieldX">
        /// The data field x.
        /// </param>
        /// <param name="dataFieldY">
        /// The data field y.
        /// </param>
        protected void AddDataPoints(IList dest, IEnumerable itemsSource, string dataFieldX, string dataFieldY)
        {
            PropertyInfo pix = null;
            PropertyInfo piy = null;
            Type t = null;

            foreach (var o in itemsSource)
            {
                if (pix == null || o.GetType() != t)
                {
                    t = o.GetType();
                    pix = t.GetProperty(dataFieldX);
                    piy = t.GetProperty(dataFieldY);
                    if (pix == null)
                    {
                        throw new InvalidOperationException(
                            string.Format("Could not find data field {0} on type {1}", this.DataFieldX, t));
                    }

                    if (piy == null)
                    {
                        throw new InvalidOperationException(
                            string.Format("Could not find data field {0} on type {1}", this.DataFieldY, t));
                    }
                }

                double x = this.ToDouble(pix.GetValue(o, null));
                double y = this.ToDouble(piy.GetValue(o, null));

                var pp = new DataPoint(x, y);
                dest.Add(pp);
            }

            //var filler = new ListFiller<DataPoint>();
            //filler.Add(dataFieldX, (item, value) => item.X = this.ToDouble(value));
            //filler.Add(dataFieldY, (item, value) => item.Y = this.ToDouble(value));
            //filler.Fill(dest, itemsSource);
        }

        /// <summary>
        /// Updates the Max/Min limits from the specified point list.
        /// </summary>
        /// <param name="pts">
        /// The points.
        /// </param>
        protected void InternalUpdateMaxMin(IList<IDataPoint> pts)
        {
            if (pts == null || pts.Count == 0)
            {
                return;
            }

            double minx = this.MinX;
            double miny = this.MinY;
            double maxx = this.MaxX;
            double maxy = this.MaxY;

            foreach (var pt in pts)
            {
                if (!this.IsValidPoint(pt, this.XAxis, this.YAxis))
                {
                    continue;
                }

                double x = pt.X;
                double y = pt.Y;
                if (x < minx || double.IsNaN(minx))
                {
                    minx = x;
                }

                if (x > maxx || double.IsNaN(maxx))
                {
                    maxx = x;
                }

                if (y < miny || double.IsNaN(miny))
                {
                    miny = y;
                }

                if (y > maxy || double.IsNaN(maxy))
                {
                    maxy = y;
                }
            }

            this.MinX = minx;
            this.MinY = miny;
            this.MaxX = maxx;
            this.MaxY = maxy;
        }
    }
}