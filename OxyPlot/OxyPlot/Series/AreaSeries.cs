// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AreaSeries.cs" company="OxyPlot">
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
//   Represents an area series that fills the polygon defined by one or two sets of points.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Series
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents an area series that fills the polygon defined by two sets of points or one set of points and a constant.
    /// </summary>
    public class AreaSeries : LineSeries
    {
        /// <summary>
        /// The second list of points.
        /// </summary>
        private readonly List<IDataPoint> points2 = new List<IDataPoint>();

        /// <summary>
        /// Initializes a new instance of the <see cref = "AreaSeries" /> class.
        /// </summary>
        public AreaSeries()
        {
            this.Reverse2 = true;
        }

        /// <summary>
        /// Gets or sets a constant value for the area definition.
        /// This is used if DataFieldBase and BaselineValues are null.
        /// </summary>
        /// <value>The baseline.</value>
        public double ConstantY2 { get; set; }

        /// <summary>
        /// Gets or sets the second X data field.
        /// </summary>
        public string DataFieldX2 { get; set; }

        /// <summary>
        /// Gets or sets the second Y data field.
        /// </summary>
        public string DataFieldY2 { get; set; }

        /// <summary>
        /// Gets or sets the area fill color.
        /// </summary>
        /// <value>The fill.</value>
        public OxyColor Fill { get; set; }

        /// <summary>
        /// Gets the second list of points.
        /// </summary>
        /// <value>The second list of points.</value>
        public List<IDataPoint> Points2
        {
            get
            {
                return this.points2;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the second
        /// data collection should be reversed.
        /// The first dataset is not reversed, and normally
        /// the second dataset should be reversed to get a
        /// closed polygon.
        /// </summary>
        public bool Reverse2 { get; set; }

        /// <summary>
        /// Gets the nearest point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="interpolate">interpolate if set to <c>true</c> .</param>
        /// <returns>A TrackerHitResult for the current hit.</returns>
        public override TrackerHitResult GetNearestPoint(ScreenPoint point, bool interpolate)
        {
            if (interpolate)
            {
                var r1 = this.GetNearestInterpolatedPointInternal(this.Points, point);
                if (r1 != null)
                {
                    return r1;
                }

                var r2 = this.GetNearestInterpolatedPointInternal(this.points2, point);
                if (r2 != null)
                {
                    return r2;
                }
            }
            else
            {
                var result1 = this.GetNearestPointInternal(this.Points, point);
                var result2 = this.GetNearestPointInternal(this.points2, point);

                if (result1 != null && result2 != null)
                {
                    double dist1 = result1.Position.DistanceTo(point);
                    double dist2 = result2.Position.DistanceTo(point);
                    return dist1 < dist2 ? result1 : result2;
                }

                if (result1 != null)
                {
                    return result1;
                }

                if (result2 != null)
                {
                    return result2;
                }
            }

            return null;
        }

        /// <summary>
        /// Renders the series on the specified rendering context.
        /// </summary>
        /// <param name="rc">The rendering context.</param>
        /// <param name="model">The owner plot model.</param>
        public override void Render(IRenderContext rc, PlotModel model)
        {
            if (this.Points.Count == 0)
            {
                return;
            }

            base.VerifyAxes();

            double minDistSquared = this.MinimumSegmentLength * this.MinimumSegmentLength;

            var clippingRect = this.GetClippingRect();

            // Transform all points to screen coordinates
            var points = this.Points;
            int n0 = points.Count;
            IList<ScreenPoint> pts0 = new ScreenPoint[n0];
            for (int i = 0; i < n0; i++)
            {
                pts0[i] = this.XAxis.Transform(points[i].X, points[i].Y, this.YAxis);
            }

            int n1 = this.points2.Count;
            IList<ScreenPoint> pts1 = new ScreenPoint[n1];
            for (int i = 0; i < n1; i++)
            {
                int j = this.Reverse2 ? n1 - 1 - i : i;
                pts1[j] = this.XAxis.Transform(this.points2[i].X, this.points2[i].Y, this.YAxis);
            }

            if (this.Smooth)
            {
                var rpts0 = ScreenPointHelper.ResamplePoints(pts0, this.MinimumSegmentLength);
                var rpts1 = ScreenPointHelper.ResamplePoints(pts1, this.MinimumSegmentLength);

                pts0 = CanonicalSplineHelper.CreateSpline(rpts0, 0.5, null, false, 0.25);
                pts1 = CanonicalSplineHelper.CreateSpline(rpts1, 0.5, null, false, 0.25);
            }

            // draw the clipped lines
            rc.DrawClippedLine(
                pts0,
                clippingRect,
                minDistSquared,
                this.GetSelectableColor(this.ActualColor),
                this.StrokeThickness,
                this.ActualLineStyle,
                this.LineJoin,
                false);
            rc.DrawClippedLine(
                pts1,
                clippingRect,
                minDistSquared,
                this.GetSelectableColor(this.ActualColor),
                this.StrokeThickness,
                this.ActualLineStyle,
                this.LineJoin,
                false);

            // combine the two lines and draw the clipped area
            var pts = new List<ScreenPoint>();
            pts.AddRange(pts1);
            pts.AddRange(pts0);

            // pts = SutherlandHodgmanClipping.ClipPolygon(clippingRect, pts);
            rc.DrawClippedPolygon(pts, clippingRect, minDistSquared, this.GetSelectableFillColor(this.Fill), null);

            // draw the markers on top
            rc.DrawMarkers(
                pts0,
                clippingRect,
                this.MarkerType,
                null,
                new[] { this.MarkerSize },
                this.MarkerFill,
                this.MarkerStroke,
                this.MarkerStrokeThickness,
                1);
            rc.DrawMarkers(
                pts1,
                clippingRect,
                this.MarkerType,
                null,
                new[] { this.MarkerSize },
                this.MarkerFill,
                this.MarkerStroke,
                this.MarkerStrokeThickness,
                1);
        }

        /// <summary>
        /// Renders the legend symbol for the line series on the
        /// specified rendering context.
        /// </summary>
        /// <param name="rc">
        /// The rendering context.
        /// </param>
        /// <param name="legendBox">
        /// The bounding rectangle of the legend box.
        /// </param>
        public override void RenderLegend(IRenderContext rc, OxyRect legendBox)
        {
            double y0 = (legendBox.Top * 0.2) + (legendBox.Bottom * 0.8);
            double y1 = (legendBox.Top * 0.4) + (legendBox.Bottom * 0.6);
            double y2 = (legendBox.Top * 0.8) + (legendBox.Bottom * 0.2);

            var pts0 = new[] { new ScreenPoint(legendBox.Left, y0), new ScreenPoint(legendBox.Right, y0) };
            var pts1 = new[] { new ScreenPoint(legendBox.Right, y2), new ScreenPoint(legendBox.Left, y1) };
            var pts = new List<ScreenPoint>();
            pts.AddRange(pts0);
            pts.AddRange(pts1);
            var color = this.GetSelectableColor(this.ActualColor);
            rc.DrawLine(pts0, color, this.StrokeThickness, LineStyleHelper.GetDashArray(this.ActualLineStyle));
            rc.DrawLine(pts1, color, this.StrokeThickness, LineStyleHelper.GetDashArray(this.ActualLineStyle));
            rc.DrawPolygon(pts, this.GetSelectableFillColor(this.Fill), null);
        }

        /// <summary>
        /// The update data.
        /// </summary>
        protected internal override void UpdateData()
        {
            base.UpdateData();

            if (this.ItemsSource == null)
            {
                return;
            }

            this.points2.Clear();

            // Using reflection on DataFieldX2 and DataFieldY2
            this.AddDataPoints(this.points2, this.ItemsSource, this.DataFieldX2, this.DataFieldY2);
        }

        /// <summary>
        /// The update max min.
        /// </summary>
        protected internal override void UpdateMaxMin()
        {
            base.UpdateMaxMin();
            this.InternalUpdateMaxMin(this.points2);
        }

    }
}