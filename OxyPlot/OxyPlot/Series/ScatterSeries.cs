// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ScatterSeries.cs" company="OxyPlot">
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
//   Represents a series for scatter plots.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Series
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    using OxyPlot.Axes;

    /// <summary>
    /// Represents a series for scatter plots.
    /// </summary>
    /// <remarks>
    /// See http://en.wikipedia.org/wiki/Scatter_plot
    /// </remarks>
    public class ScatterSeries : DataPointSeries
    {
        /// <summary>
        /// The default fill color.
        /// </summary>
        private OxyColor defaultMarkerFillColor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScatterSeries"/> class.
        /// </summary>
        /// <param name="title">
        /// The title.
        /// </param>
        /// <param name="markerFill">
        /// The marker fill color.
        /// </param>
        /// <param name="markerSize">
        /// Size of the markers (If ScatterPoint.Size is set, this value will be overridden).
        /// </param>
        public ScatterSeries(string title, OxyColor markerFill = null, double markerSize = 5)
            : this()
        {
            this.MarkerFill = markerFill;
            this.MarkerSize = markerSize;
            this.Title = title;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScatterSeries"/> class.
        /// </summary>
        public ScatterSeries()
        {
            this.DataFieldSize = null;
            this.DataFieldValue = null;

            this.MarkerFill = null;
            this.MarkerSize = 5;
            this.MarkerType = MarkerType.Square;
            this.MarkerStroke = null;
            this.MarkerStrokeThickness = 1.0;
        }

        /// <summary>
        /// Gets or sets the screen resolution. If this number is greater than 1, bins of that size is created for both x and y directions. Only one point will be drawn in each bin.
        /// </summary>
        public int BinSize { get; set; }

        /// <summary>
        /// Gets or sets the color map.
        /// </summary>
        /// <value> The color map. </value>
        /// <remarks>
        /// This is used to map scatter point values to colors.
        /// </remarks>
        public ColorAxis ColorAxis { get; set; }

        /// <summary>
        /// Gets or sets the color axis key.
        /// </summary>
        /// <value> The color axis key. </value>
        public string ColorAxisKey { get; set; }

        /// <summary>
        /// Gets or sets the data field for the size.
        /// </summary>
        /// <value> The size data field. </value>
        public string DataFieldSize { get; set; }

        /// <summary>
        /// Gets or sets the tag data field.
        /// </summary>
        /// <value> The tag data field. </value>
        public string DataFieldTag { get; set; }

        /// <summary>
        /// Gets or sets the value data field.
        /// </summary>
        /// <value> The value data field. </value>
        public string DataFieldValue { get; set; }

        /// <summary>
        /// Gets or sets the marker fill color. If null, this color will be automatically set.
        /// </summary>
        /// <value> The marker fill color. </value>
        public OxyColor MarkerFill { get; set; }

        /// <summary>
        /// Gets the actual fill color.
        /// </summary>
        /// <value>The actual color.</value>
        public OxyColor ActualMarkerFillColor
        {
            get { return this.MarkerFill ?? this.defaultMarkerFillColor; }
        }

        /// <summary>
        /// Gets or sets the marker outline polygon. Set MarkerType to Custom to use this.
        /// </summary>
        /// <value> The marker outline. </value>
        public ScreenPoint[] MarkerOutline { get; set; }

        /// <summary>
        /// Gets or sets the size of the marker (same size for all items).
        /// </summary>
        /// <value> The size of the markers. </value>
        public double MarkerSize { get; set; }

        /// <summary>
        /// Gets or sets the marker stroke.
        /// </summary>
        /// <value> The marker stroke. </value>
        public OxyColor MarkerStroke { get; set; }

        /// <summary>
        /// Gets or sets the marker stroke thickness.
        /// </summary>
        /// <value> The marker stroke thickness. </value>
        public double MarkerStrokeThickness { get; set; }

        /// <summary>
        /// Gets or sets the type of the marker.
        /// </summary>
        /// <value> The type of the marker. </value>
        /// <remarks>
        /// If MarkerType.Custom is used, the MarkerOutline property must be specified.
        /// </remarks>
        public MarkerType MarkerType { get; set; }

        /// <summary>
        /// Gets the max value of the points.
        /// </summary>
        public double MaxValue { get; private set; }

        /// <summary>
        /// Gets the min value of the points.
        /// </summary>
        public double MinValue { get; private set; }

        /// <summary>
        /// Gets the nearest point.
        /// </summary>
        /// <param name="point">
        /// The point.
        /// </param>
        /// <param name="interpolate">
        /// interpolate if set to <c>true</c> .
        /// </param>
        /// <returns>
        /// A TrackerHitResult for the current hit.
        /// </returns>
        public override TrackerHitResult GetNearestPoint(ScreenPoint point, bool interpolate)
        {
            if (this.XAxis == null || this.YAxis == null)
            {
                return null;
            }

            if (interpolate)
            {
                return null;
            }

            TrackerHitResult result = null;
            double minimumDistance = double.MaxValue;
            int i = 0;

            var xaxisTitle = this.XAxis.Title ?? "X";
            var yaxisTitle = this.YAxis.Title ?? "Y";
            var colorAxisTitle = (this.ColorAxis != null ? this.ColorAxis.Title : null) ?? "Z";

            var formatString = TrackerFormatString;
            if (string.IsNullOrEmpty(this.TrackerFormatString))
            {
                // Create a default format string
                formatString = "{1}: {2}\n{3}: {4}";
                if (this.ColorAxis != null)
                {
                    formatString += "\n{5}: {6}";
                }
            }

            foreach (var p in this.Points)
            {
                if (p.X < this.XAxis.ActualMinimum || p.X > this.XAxis.ActualMaximum || p.Y < this.YAxis.ActualMinimum || p.Y > this.YAxis.ActualMaximum)
                {
                    i++;
                    continue;
                }

                var dp = new DataPoint(p.X, p.Y);
                var sp = Axis.Transform(dp, this.XAxis, this.YAxis);
                double dx = sp.x - point.x;
                double dy = sp.y - point.y;
                double d2 = (dx * dx) + (dy * dy);

                if (d2 < minimumDistance)
                {
                    var item = this.GetItem(i);

                    object xvalue = this.XAxis.GetValue(dp.X);
                    object yvalue = this.YAxis.GetValue(dp.Y);
                    object zvalue = null;

                    var scatterPoint = p as ScatterPoint;
                    if (scatterPoint != null)
                    {
                        if (!double.IsNaN(scatterPoint.Value) && !double.IsInfinity(scatterPoint.Value))
                        {
                            zvalue = scatterPoint.Value;
                        }
                    }

                    var text = StringHelper.Format(
                        this.ActualCulture,
                        formatString,
                        item,
                        this.Title,
                        xaxisTitle,
                        xvalue,
                        yaxisTitle,
                        yvalue,
                        colorAxisTitle,
                        zvalue);

                    result = new TrackerHitResult(this, dp, sp, item, i, text);

                    minimumDistance = d2;
                }

                i++;
            }

            return result;
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
        public virtual bool IsValidPoint(ScatterPoint pt, Axis xaxis, Axis yaxis)
        {
            return !double.IsNaN(pt.X) && !double.IsInfinity(pt.X) && !double.IsNaN(pt.Y) && !double.IsInfinity(pt.Y)
                   && (xaxis != null && xaxis.IsValidValue(pt.X)) && (yaxis != null && yaxis.IsValidValue(pt.Y));
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

            OxyRect clippingRect = this.GetClippingRect();

            var points = this.Points;
            int n = points.Count;
            var groupPoints = new Dictionary<int, IList<ScreenPoint>>();
            var groupSizes = new Dictionary<int, IList<double>>();

            ScreenPoint[] allPoints = null;
            double[] markerSizes = null;

            if (this.ColorAxis == null)
            {
                allPoints = new ScreenPoint[n];
                markerSizes = new double[n];
            }

            // Transform all points to screen coordinates
            for (int i = 0; i < n; i++)
            {
                var dp = new DataPoint(points[i].X, points[i].Y);
                double size = double.NaN;
                double value = double.NaN;

                var scatterPoint = points[i] as ScatterPoint;
                if (scatterPoint != null)
                {
                    size = scatterPoint.Size;
                    value = scatterPoint.Value;
                }

                if (double.IsNaN(size))
                {
                    size = this.MarkerSize;
                }

                var screenPoint = this.XAxis.Transform(dp.X, dp.Y, this.YAxis);

                if (this.ColorAxis != null)
                {
                    if (!double.IsNaN(value))
                    {
                        int group = this.ColorAxis.GetPaletteIndex(value);
                        if (!groupPoints.ContainsKey(group))
                        {
                            groupPoints.Add(group, new List<ScreenPoint>());
                            groupSizes.Add(group, new List<double>());
                        }

                        groupPoints[group].Add(screenPoint);
                        groupSizes[group].Add(size);
                    }
                }
                else
                {
                    // ReSharper disable PossibleNullReferenceException
                    allPoints[i] = screenPoint;
                    markerSizes[i] = size;
                    // ReSharper restore PossibleNullReferenceException
                }
            }

            var binOffset = this.XAxis.Transform(this.MinX, this.MaxY, this.YAxis);

            // Draw the markers
            if (this.ColorAxis != null)
            {
                var markerIsStrokedOnly = this.MarkerType == MarkerType.Plus || this.MarkerType == MarkerType.Star
                                          || this.MarkerType == MarkerType.Cross;
                foreach (var group in groupPoints)
                {
                    var color = this.ColorAxis.GetColor(group.Key);
                    rc.DrawMarkers(
                        group.Value,
                        clippingRect,
                        this.MarkerType,
                        this.MarkerOutline,
                        groupSizes[group.Key],
                        color,
                        markerIsStrokedOnly ? color : this.MarkerStroke,
                        this.MarkerStrokeThickness,
                        this.BinSize,
                        binOffset);
                }
            }
            else
            {
                rc.DrawMarkers(
                    allPoints,
                    clippingRect,
                    this.MarkerType,
                    this.MarkerOutline,
                    markerSizes,
                    this.ActualMarkerFillColor,
                    this.MarkerStroke,
                    this.MarkerStrokeThickness,
                    this.BinSize,
                    binOffset);
            }
        }

        /// <summary>
        /// Renders the legend symbol for the line series on the specified rendering context.
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

            var midpt = new ScreenPoint(xmid, ymid);
            rc.DrawMarker(
                midpt,
                legendBox,
                this.MarkerType,
                this.MarkerOutline,
                this.MarkerSize,
                this.ActualMarkerFillColor,
                this.MarkerStroke,
                this.MarkerStrokeThickness);
        }

        /// <summary>
        /// Ensures that the axes of the series is defined.
        /// </summary>
        protected internal override void EnsureAxes()
        {
            base.EnsureAxes();

            this.ColorAxis = PlotModel.GetAxisOrDefault(this.ColorAxisKey, PlotModel.DefaultColorAxis) as ColorAxis;
        }

        /// <summary>
        /// Sets the default values.
        /// </summary>
        /// <param name="model">
        /// The model.
        /// </param>
        protected internal override void SetDefaultValues(PlotModel model)
        {
            if (this.MarkerFill == null)
            {
                this.defaultMarkerFillColor = model.GetDefaultColor();
            }
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

            var points = this.Points;
            points.Clear();

            // Use the mapping to generate the points
            if (this.Mapping != null)
            {
                foreach (var item in this.ItemsSource)
                {
                    points.Add(this.Mapping(item));
                }

                return;
            }

            // Get DataPoints from the items in ItemsSource
            // if they implement IScatterPointProvider
            // If DataFields are set, this is not used
            /*if (DataFieldX == null || DataFieldY == null)
            {
                foreach (var item in ItemsSource)
                {
                    var idpp = item as IScatterPointProvider;
                    if (idpp == null)
                    {
                        continue;
                    }

                    points.Add(idpp.GetScatterPoint());
                }

                return;
            }*/

            var dest = new List<IDataPoint>();

            // Using reflection to add points
            var filler = new ListFiller<ScatterPoint>();
            filler.Add(this.DataFieldX, (item, value) => item.X = Convert.ToDouble(value));
            filler.Add(this.DataFieldY, (item, value) => item.Y = Convert.ToDouble(value));
            filler.Add(this.DataFieldSize, (item, value) => item.Size = Convert.ToDouble(value));
            filler.Add(this.DataFieldValue, (item, value) => item.Value = Convert.ToDouble(value));
            filler.Add(this.DataFieldTag, (item, value) => item.Tag = value);
            filler.Fill(dest, this.ItemsSource);

            this.Points = dest;
        }

        /// <summary>
        /// Updates the max/min from the data points.
        /// </summary>
        protected internal override void UpdateMaxMin()
        {
            base.UpdateMaxMin();
            this.InternalUpdateMaxMinValue(this.Points);
        }

        /// <summary>
        /// Adds scatter points specified by a items source and data fields.
        /// </summary>
        /// <param name="dest">
        /// The destination collection.
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
        /// <param name="dataFieldSize">
        /// The data field size.
        /// </param>
        /// <param name="dataFieldValue">
        /// The data field value.
        /// </param>
        /// <param name="dataFieldTag">
        /// The data field tag.
        /// </param>
        protected void AddScatterPoints(
            IList<ScatterPoint> dest,
            IEnumerable itemsSource,
            string dataFieldX,
            string dataFieldY,
            string dataFieldSize,
            string dataFieldValue,
            string dataFieldTag)
        {
            var filler = new ListFiller<ScatterPoint>();
            filler.Add(dataFieldX, (item, value) => item.X = Convert.ToDouble(value));
            filler.Add(dataFieldY, (item, value) => item.Y = Convert.ToDouble(value));
            filler.Add(dataFieldSize, (item, value) => item.Size = Convert.ToDouble(value));
            filler.Add(dataFieldValue, (item, value) => item.Value = Convert.ToDouble(value));
            filler.Add(dataFieldTag, (item, value) => item.Tag = value);
            filler.FillT(dest, itemsSource);
        }

        /// <summary>
        /// Updates the Max/Min limits from the values in the specified point list.
        /// </summary>
        /// <param name="pts">
        /// The points.
        /// </param>
        protected void InternalUpdateMaxMinValue(IList<IDataPoint> pts)
        {
            if (pts == null || pts.Count == 0)
            {
                return;
            }

            double minvalue = double.NaN;
            double maxvalue = double.NaN;

            foreach (var pt in pts)
            {
                if (!(pt is ScatterPoint))
                {
                    continue;
                }

                double value = ((ScatterPoint)pt).value;

                if (value < minvalue || double.IsNaN(minvalue))
                {
                    minvalue = value;
                }

                if (value > maxvalue || double.IsNaN(maxvalue))
                {
                    maxvalue = value;
                }
            }

            this.MinValue = minvalue;
            this.MaxValue = maxvalue;

            if (this.ColorAxis != null)
            {
                this.ColorAxis.Include(this.MinValue);
                this.ColorAxis.Include(this.MaxValue);
            }
        }
    }
}