// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LineSeries.cs" company="OxyPlot">
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
//   Represents a line series.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Series
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a line series.
    /// </summary>
    public class LineSeries : DataPointSeries
    {
        /// <summary>
        /// The divisor value used to calculate tolerance for line smoothing.
        /// </summary>
        private const double ToleranceDivisor = 200;

        /// <summary>
        /// The default color.
        /// </summary>
        private OxyColor defaultColor;

        /// <summary>
        /// The smoothed points.
        /// </summary>
        private IList<IDataPoint> smoothedPoints;

        /// <summary>
        /// Initializes a new instance of the <see cref = "LineSeries" /> class.
        /// </summary>
        public LineSeries()
        {
            this.MinimumSegmentLength = 2;
            this.StrokeThickness = 2;
            this.LineJoin = OxyPenLineJoin.Bevel;
            this.LineStyle = LineStyle.Undefined;
            this.MarkerSize = 3;
            this.MarkerStrokeThickness = 1;
            this.CanTrackerInterpolatePoints = true;
            this.LabelMargin = 6;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LineSeries"/> class.
        /// </summary>
        /// <param name="title">
        /// The title.
        /// </param>
        public LineSeries(string title)
            : this()
        {
            this.Title = title;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LineSeries"/> class.
        /// </summary>
        /// <param name="color">
        /// The color of the line stroke.
        /// </param>
        /// <param name="strokeThickness">
        /// The stroke thickness (optional).
        /// </param>
        /// <param name="title">
        /// The title (optional).
        /// </param>
        public LineSeries(OxyColor color, double strokeThickness = 1, string title = null)
            : this()
        {
            this.Color = color;
            this.StrokeThickness = strokeThickness;
            this.BrokenLineThickness = 0;
            this.Title = title;
        }

        /// <summary>
        /// Gets or sets the color of the curve.
        /// </summary>
        /// <value>The color.</value>
        public OxyColor Color { get; set; }

        /// <summary>
        /// Gets or sets the color of the broken line segments.
        /// </summary>
        /// <remarks>
        /// Add <c>DataPoint.Undefined</c> in the Points collection to create breaks in the line.
        /// </remarks>
        public OxyColor BrokenLineColor { get; set; }

        /// <summary>
        /// Gets or sets the broken line style.
        /// </summary>
        public LineStyle BrokenLineStyle { get; set; }

        /// <summary>
        /// Gets or sets the broken line thickness.
        /// </summary>
        public double BrokenLineThickness { get; set; }

        /// <summary>
        /// Gets or sets the dashes array.
        /// If this is not null it overrides the LineStyle property.
        /// </summary>
        /// <value>The dashes.</value>
        public double[] Dashes { get; set; }

        /// <summary>
        /// Gets or sets the label format string.
        /// </summary>
        /// <value> The label format string. </value>
        public string LabelFormatString { get; set; }

        /// <summary>
        /// Gets or sets the label margins.
        /// </summary>
        public double LabelMargin { get; set; }

        /// <summary>
        /// Gets or sets the line join.
        /// </summary>
        /// <value>The line join.</value>
        public OxyPenLineJoin LineJoin { get; set; }

        /// <summary>
        /// Gets or sets the line style.
        /// </summary>
        /// <value>The line style.</value>
        public LineStyle LineStyle { get; set; }

        /// <summary>
        /// Gets or sets a value specifying the position of a legend rendered on the line.
        /// </summary>
        /// <value>A value specifying the position of the legend.</value>
        public LineLegendPosition LineLegendPosition { get; set; }

        /// <summary>
        /// Gets or sets the marker fill color.
        /// </summary>
        /// <value>The marker fill.</value>
        public OxyColor MarkerFill { get; set; }

        /// <summary>
        /// Gets or sets the marker outline polygon.
        /// If this property is set, the MarkerType will not be used.
        /// </summary>
        /// <value>The marker outline.</value>
        public ScreenPoint[] MarkerOutline { get; set; }

        /// <summary>
        /// Gets or sets the size of the marker.
        /// </summary>
        /// <value>The size of the marker.</value>
        public double MarkerSize { get; set; }

        /// <summary>
        /// Gets or sets the marker stroke.
        /// </summary>
        /// <value>The marker stroke.</value>
        public OxyColor MarkerStroke { get; set; }

        /// <summary>
        /// Gets or sets the marker stroke thickness.
        /// </summary>
        /// <value>The marker stroke thickness.</value>
        public double MarkerStrokeThickness { get; set; }

        /// <summary>
        /// Gets or sets the type of the marker.
        /// </summary>
        /// <value>The type of the marker.</value>
        /// <remarks>
        /// If MarkerType.Custom is used, the MarkerOutline property must be specified.
        /// </remarks>
        public MarkerType MarkerType { get; set; }

        /// <summary>
        /// Gets or sets the minimum length of the segment.
        /// Increasing this number will increase performance,
        /// but make the curve less accurate.
        /// </summary>
        /// <value>The minimum length of the segment.</value>
        public double MinimumSegmentLength { get; set; }

        /// <summary>
        /// Gets or sets the thickness of the curve.
        /// </summary>
        /// <value>The stroke thickness.</value>
        public double StrokeThickness { get; set; }

        /// <summary>
        /// Gets the actual color.
        /// </summary>
        /// <value>The actual color.</value>
        protected OxyColor ActualColor
        {
            get
            {
                return this.Color ?? this.defaultColor;
            }
        }

        /// <summary>
        /// Gets the actual line style.
        /// </summary>
        /// <value>
        /// The actual line style.
        /// </value>
        protected LineStyle ActualLineStyle
        {
            get
            {
                return this.LineStyle != LineStyle.Undefined ? this.LineStyle : LineStyle.Solid;
            }
        }
        
        /// <summary>
        /// Gets the smoothed points.
        /// </summary>
        /// <value>The smoothed points.</value>
        protected IList<IDataPoint> SmoothedPoints
        {
            get
            {
                return this.smoothedPoints;
            }
        }

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
            if (interpolate)
            {
                // Cannot interpolate if there is no line
                if (this.ActualColor == null || this.StrokeThickness.IsZero())
                {
                    return null;
                }

                if (!this.CanTrackerInterpolatePoints)
                {
                    return null;
                }
            }

            if (interpolate && this.Smooth && this.SmoothedPoints != null)
            {
                return this.GetNearestInterpolatedPointInternal(this.SmoothedPoints, point);
            }

            return base.GetNearestPoint(point, interpolate);
        }

        /// <summary>
        /// Renders the series on the specified rendering context.
        /// </summary>
        /// <param name="rc">
        /// The rendering context.
        /// </param>
        /// <param name="model">
        /// The owner plot model.
        /// </param>
        public override void Render(IRenderContext rc, PlotModel model)
        {
            if (this.Points.Count == 0)
            {
                return;
            }

            this.VerifyAxes();

            var clippingRect = this.GetClippingRect();
            var transformedPoints = new List<ScreenPoint>();
            var lineBreakSegments = new List<ScreenPoint>();

            ScreenPoint lastValidPoint = default(ScreenPoint);
            bool isBroken = false;
            var renderBrokenLineSegments = this.BrokenLineThickness > 0 && this.BrokenLineStyle != LineStyle.None;

            // Transform all points to screen coordinates
            // Render the line when invalid points occur
            foreach (var point in this.Points)
            {
                if (!this.IsValidPoint(point, this.XAxis, this.YAxis))
                {
                    this.RenderPoints(rc, clippingRect, transformedPoints);
                    transformedPoints.Clear();
                    isBroken = true;
                    continue;
                }

                var pt = this.XAxis.Transform(point.X, point.Y, this.YAxis);
                transformedPoints.Add(pt);

                if (renderBrokenLineSegments)
                {
                    if (isBroken)
                    {
                        lineBreakSegments.Add(lastValidPoint);
                        lineBreakSegments.Add(pt);
                        isBroken = false;
                    }

                    lastValidPoint = pt;
                }
            }

            // Render the remaining points
            this.RenderPoints(rc, clippingRect, transformedPoints);

            if (renderBrokenLineSegments)
            {
                // Render line breaks
                rc.DrawClippedLineSegments(
                    lineBreakSegments, 
                    clippingRect, 
                    this.BrokenLineColor, 
                    this.BrokenLineThickness, 
                    this.BrokenLineStyle, 
                    this.LineJoin, 
                    false);
            }

            if (this.LabelFormatString != null)
            {
                // render point labels (not optimized for performance)
                this.RenderPointLabels(rc, clippingRect);
            }

            if (this.LineLegendPosition != LineLegendPosition.None && this.Points.Count > 0
                && !string.IsNullOrEmpty(this.Title))
            {
                // renders a legend on the line
                this.RenderLegendOnLine(rc, clippingRect);
            }
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
            double xmid = (legendBox.Left + legendBox.Right) / 2;
            double ymid = (legendBox.Top + legendBox.Bottom) / 2;
            var pts = new[] { new ScreenPoint(legendBox.Left, ymid), new ScreenPoint(legendBox.Right, ymid) };
            rc.DrawLine(
                pts, 
                this.GetSelectableColor(this.ActualColor), 
                this.StrokeThickness, 
                this.ActualLineStyle.GetDashArray());
            var midpt = new ScreenPoint(xmid, ymid);
            rc.DrawMarker(
                midpt, 
                legendBox, 
                this.MarkerType, 
                this.MarkerOutline, 
                this.MarkerSize, 
                this.MarkerFill, 
                this.MarkerStroke, 
                this.MarkerStrokeThickness);
        }

        /// <summary>
        /// The set default values.
        /// </summary>
        /// <param name="model">
        /// The model.
        /// </param>
        protected internal override void SetDefaultValues(PlotModel model)
        {
            // todo: should use ActualLineStyle
            if (this.Color == null)
            {
                if (this.LineStyle == LineStyle.Undefined)
                {
                    this.LineStyle = model.GetDefaultLineStyle();
                }

                this.defaultColor = model.GetDefaultColor();

                // And MarkerFill will be overridden if not set to null
                if (this.MarkerFill == null)
                {
                    this.MarkerFill = this.defaultColor;
                }
            }
        }

        /// <summary>
        /// Updates the axes to include the max and min of this series.
        /// </summary>
        protected internal override void UpdateMaxMin()
        {
            if (this.Smooth)
            {
                // Update the max/min from the control points
                base.UpdateMaxMin();

                // Make sure the smooth points are re-evaluated.
                this.ResetSmoothedPoints();

                // Update the max/min from the smoothed points
                foreach (var pt in this.SmoothedPoints)
                {
                    this.MinX = Math.Min(this.MinX, pt.X);
                    this.MinY = Math.Min(this.MinY, pt.Y);
                    this.MaxX = Math.Max(this.MaxX, pt.X);
                    this.MaxY = Math.Max(this.MaxY, pt.Y);
                }
            }
            else
            {
                base.UpdateMaxMin();
            }
        }

        /// <summary>
        /// Renders the point labels.
        /// </summary>
        /// <param name="rc">The render context.</param>
        /// <param name="clippingRect">The clipping rectangle.</param>
        protected void RenderPointLabels(IRenderContext rc, OxyRect clippingRect)
        {
            int index = -1;
            foreach (var point in this.Points)
            {
                index++;

                if (!this.IsValidPoint(point, this.XAxis, this.YAxis))
                {
                    continue;
                }

                var pt = this.XAxis.Transform(point.X, point.Y, this.YAxis);
                pt.Y -= this.LabelMargin;

                if (!clippingRect.Contains(pt))
                {
                    continue;
                }

                var s = StringHelper.Format(
                    this.ActualCulture, this.LabelFormatString, this.GetItem(index), point.X, point.Y);

#if SUPPORTLABELPLACEMENT
                    switch (this.LabelPlacement)
                    {
                        case LabelPlacement.Inside:
                            pt = new ScreenPoint(rect.Right - this.LabelMargin, (rect.Top + rect.Bottom) / 2);
                            ha = HorizontalAlignment.Right;
                            break;
                        case LabelPlacement.Middle:
                            pt = new ScreenPoint((rect.Left + rect.Right) / 2, (rect.Top + rect.Bottom) / 2);
                            ha = HorizontalAlignment.Center;
                            break;
                        case LabelPlacement.Base:
                            pt = new ScreenPoint(rect.Left + this.LabelMargin, (rect.Top + rect.Bottom) / 2);
                            ha = HorizontalAlignment.Left;
                            break;
                        default: // Outside
                            pt = new ScreenPoint(rect.Right + this.LabelMargin, (rect.Top + rect.Bottom) / 2);
                            ha = HorizontalAlignment.Left;
                            break;
                    }
#endif

                rc.DrawClippedText(
                    clippingRect, 
                    pt, 
                    s, 
                    this.ActualTextColor, 
                    this.ActualFont, 
                    this.ActualFontSize, 
                    this.ActualFontWeight, 
                    0, 
                    HorizontalAlignment.Center, 
                    VerticalAlignment.Bottom);
            }
        }

        /// <summary>
        /// Renders a legend on the line.
        /// </summary>
        /// <param name="rc">The render context.</param>
        /// <param name="clippingRect">The clipping rectangle.</param>
        protected void RenderLegendOnLine(IRenderContext rc, OxyRect clippingRect)
        {
            // Find the position
            IDataPoint point;
            var ha = HorizontalAlignment.Left;
            double dx;
            switch (this.LineLegendPosition)
            {
                case LineLegendPosition.Start:

                    // start position
                    point = this.Points[0];
                    ha = HorizontalAlignment.Right;
                    dx = -4;
                    break;
                default:

                    // end position
                    point = this.Points[this.Points.Count - 1];
                    dx = 4;
                    break;
            }

            var pt = this.XAxis.Transform(point.X, point.Y, this.YAxis);
            pt.X += dx;

            // Render the legend
            rc.DrawClippedText(
                clippingRect, 
                pt, 
                this.Title, 
                this.ActualTextColor, 
                this.ActualFont, 
                this.ActualFontSize, 
                this.ActualFontWeight, 
                0, 
                ha, 
                VerticalAlignment.Middle);
        }

        /// <summary>
        /// Renders the transformed points.
        /// </summary>
        /// <param name="rc">
        /// The render context.
        /// </param>
        /// <param name="clippingRect">
        /// The clipping rectangle.
        /// </param>
        /// <param name="pointsToRender">
        /// The points to render.
        /// </param>
        protected void RenderPoints(IRenderContext rc, OxyRect clippingRect, IList<ScreenPoint> pointsToRender)
        {
            var screenPoints = pointsToRender;
            if (this.Smooth)
            {
                // spline smoothing (should only be used on small datasets)
                var resampledPoints = ScreenPointHelper.ResamplePoints(pointsToRender, this.MinimumSegmentLength);
                screenPoints = CanonicalSplineHelper.CreateSpline(resampledPoints, 0.5, null, false, 0.25);
            }

            // clip the line segments with the clipping rectangle
            if (this.StrokeThickness > 0 && this.ActualLineStyle != LineStyle.None)
            {
                this.RenderSmoothedLine(rc, clippingRect, screenPoints);
            }

            if (this.MarkerType != MarkerType.None)
            {
                rc.DrawMarkers(
                    pointsToRender, 
                    clippingRect, 
                    this.MarkerType, 
                    this.MarkerOutline, 
                    new[] { this.MarkerSize }, 
                    this.MarkerFill, 
                    this.MarkerStroke, 
                    this.MarkerStrokeThickness);
            }
        }

        /// <summary>
        /// Renders the (smoothed) line.
        /// </summary>
        /// <param name="rc">
        /// The render context.
        /// </param>
        /// <param name="clippingRect">
        /// The clipping rectangle.
        /// </param>
        /// <param name="pointsToRender">
        /// The points to render.
        /// </param>
        protected virtual void RenderSmoothedLine(
            IRenderContext rc, OxyRect clippingRect, IList<ScreenPoint> pointsToRender)
        {
            rc.DrawClippedLine(
                pointsToRender, 
                clippingRect, 
                this.MinimumSegmentLength * this.MinimumSegmentLength, 
                this.GetSelectableColor(this.ActualColor), 
                this.StrokeThickness, 
                this.ActualLineStyle, 
                this.LineJoin, 
                false);
        }

        /// <summary>
        /// Force the smoothed points to be re-evaluated.
        /// </summary>
        protected void ResetSmoothedPoints()
        {
            double tolerance = Math.Abs(Math.Max(this.MaxX - this.MinX, this.MaxY - this.MinY) / ToleranceDivisor);
            this.smoothedPoints = CanonicalSplineHelper.CreateSpline(this.Points, 0.5, null, false, tolerance);
        }
    }
}