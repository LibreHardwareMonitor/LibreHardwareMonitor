// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HighLowSeries.cs" company="OxyPlot">
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
//   Represents a series for high-low plots.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Series
{
    using System;
    using System.Collections.Generic;

    using OxyPlot.Axes;

    /// <summary>
    /// Represents a series for high-low plots.
    /// </summary>
    /// <remarks>
    /// See http://www.mathworks.com/help/toolbox/finance/highlowfts.html
    /// </remarks>
    public class HighLowSeries : XYAxisSeries
    {
        /// <summary>
        /// High/low items
        /// </summary>
        private IList<HighLowItem> items;

        /// <summary>
        /// The default color.
        /// </summary>
        private OxyColor defaultColor;

        /// <summary>
        /// Initializes a new instance of the <see cref = "HighLowSeries" /> class.
        /// </summary>
        public HighLowSeries()
        {
            this.items = new List<HighLowItem>();
            this.TickLength = 4;
            this.StrokeThickness = 1;
            this.TrackerFormatString = "X: {1:0.00}\nHigh: {2:0.00}\nLow: {3:0.00}\nOpen: {4:0.00}\nClose: {5:0.00}";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HighLowSeries"/> class.
        /// </summary>
        /// <param name="title">
        /// The title.
        /// </param>
        public HighLowSeries(string title)
            : this()
        {
            this.Title = title;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HighLowSeries"/> class.
        /// </summary>
        /// <param name="color">
        /// The color.
        /// </param>
        /// <param name="strokeThickness">
        /// The stroke thickness.
        /// </param>
        /// <param name="title">
        /// The title.
        /// </param>
        public HighLowSeries(OxyColor color, double strokeThickness = 1, string title = null)
            : this()
        {
            this.Color = color;
            this.StrokeThickness = strokeThickness;
            this.Title = title;
        }

        /// <summary>
        /// Gets or sets the color of the curve.
        /// </summary>
        /// <value>The color.</value>
        public OxyColor Color { get; set; }

        /// <summary>
        /// Gets the actual color.
        /// </summary>
        /// <value>The actual color.</value>
        public OxyColor ActualColor
        {
            get { return this.Color ?? this.defaultColor; }
        }

        /// <summary>
        /// Gets or sets the dashes array.
        /// If this is not null it overrides the LineStyle property.
        /// </summary>
        /// <value>The dashes.</value>
        public double[] Dashes { get; set; }

        /// <summary>
        /// Gets or sets the data field for the Close value.
        /// </summary>
        public string DataFieldClose { get; set; }

        /// <summary>
        /// Gets or sets the data field for the High value.
        /// </summary>
        public string DataFieldHigh { get; set; }

        /// <summary>
        /// Gets or sets the data field for the Low value.
        /// </summary>
        public string DataFieldLow { get; set; }

        /// <summary>
        /// Gets or sets the data field for the Open value.
        /// </summary>
        public string DataFieldOpen { get; set; }

        /// <summary>
        /// Gets or sets the x data field (time).
        /// </summary>
        public string DataFieldX { get; set; }

        /// <summary>
        /// Gets or sets the points.
        /// </summary>
        /// <value>The points.</value>
        public IList<HighLowItem> Items
        {
            get
            {
                return this.items;
            }

            set
            {
                this.items = value;
            }
        }

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
        /// Gets or sets the mapping deleagte.
        /// Example: series1.Mapping = item => new HighLowItem(((MyType)item).Time,((MyType)item).Value);
        /// </summary>
        /// <value>The mapping.</value>
        public Func<object, HighLowItem> Mapping { get; set; }

        /// <summary>
        /// Gets or sets the thickness of the curve.
        /// </summary>
        /// <value>The stroke thickness.</value>
        public double StrokeThickness { get; set; }

        /// <summary>
        /// Gets or sets the length of the open/close ticks (screen coordinates).
        /// </summary>
        /// <value>The length of the open/close ticks.</value>
        public double TickLength { get; set; }

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
            if (this.XAxis == null || this.YAxis == null)
            {
                return null;
            }

            if (interpolate)
            {
                return null;
            }

            double minimumDistance = double.MaxValue;
            var result = new TrackerHitResult(this, DataPoint.Undefined, ScreenPoint.Undefined);

            Action<DataPoint, HighLowItem, int> check = (p, item, index) =>
                {
                    var sp = this.Transform(p);
                    double dx = sp.x - point.x;
                    double dy = sp.y - point.y;
                    double d2 = (dx * dx) + (dy * dy);

                    if (d2 < minimumDistance)
                    {
                        result.DataPoint = p;
                        result.Position = sp;
                        result.Item = item;
                        result.Index = index;
                        if (this.TrackerFormatString != null)
                        {
                            result.Text = StringHelper.Format(
                                this.ActualCulture,
                                this.TrackerFormatString,
                                item,
                                this.Title,
                                this.XAxis.GetValue(p.X),
                                item.High,
                                item.Low,
                                item.Open,
                                item.Close);
                        }

                        minimumDistance = d2;
                    }
                };
            int i = 0;
            foreach (var item in this.items)
            {
                check(new DataPoint(item.X, item.High), item, i);
                check(new DataPoint(item.X, item.Low), item, i);
                check(new DataPoint(item.X, item.Open), item, i);
                check(new DataPoint(item.X, item.Close), item, i++);
            }

            if (minimumDistance < double.MaxValue)
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Determines whether the point is valid.
        /// </summary>
        /// <param name="pt">The point.</param>
        /// <param name="xaxis">The x axis.</param>
        /// <param name="yaxis">The y axis.</param>
        /// <returns>
        ///  <c>true</c> if [is valid point] [the specified pt]; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsValidItem(HighLowItem pt, Axis xaxis, Axis yaxis)
        {
            return !double.IsNaN(pt.X) && !double.IsInfinity(pt.X) && !double.IsNaN(pt.High)
                   && !double.IsInfinity(pt.High) && !double.IsNaN(pt.Low) && !double.IsInfinity(pt.Low);
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
            if (this.items.Count == 0)
            {
                return;
            }

            this.VerifyAxes();

            var clippingRect = this.GetClippingRect();

            foreach (var v in this.items)
            {
                if (!this.IsValidItem(v, this.XAxis, this.YAxis))
                {
                    continue;
                }

                if (this.StrokeThickness > 0 && this.LineStyle != LineStyle.None)
                {
                    ScreenPoint high = this.Transform(v.X, v.High);
                    ScreenPoint low = this.Transform(v.X, v.Low);

                    rc.DrawClippedLine(
                        new[] { low, high },
                        clippingRect,
                        0,
                        this.GetSelectableColor(this.ActualColor),
                        this.StrokeThickness,
                        this.LineStyle,
                        this.LineJoin,
                        true);
                    if (!double.IsNaN(v.Open))
                    {
                        ScreenPoint open = this.Transform(v.X, v.Open);
                        ScreenPoint openTick = open;
                        openTick.X -= this.TickLength;
                        rc.DrawClippedLine(
                            new[] { open, openTick },
                            clippingRect,
                            0,
                            this.GetSelectableColor(this.ActualColor),
                            this.StrokeThickness,
                            this.LineStyle,
                            this.LineJoin,
                            true);
                    }

                    if (!double.IsNaN(v.Close))
                    {
                        ScreenPoint close = this.Transform(v.X, v.Close);
                        ScreenPoint closeTick = close;
                        closeTick.X += this.TickLength;
                        rc.DrawClippedLine(
                            new[] { close, closeTick },
                            clippingRect,
                            0,
                            this.GetSelectableColor(this.ActualColor),
                            this.StrokeThickness,
                            this.LineStyle,
                            this.LineJoin,
                            true);
                    }
                }
            }
        }

        /// <summary>
        /// Renders the legend symbol for the series on the specified rendering context.
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
            double yopen = legendBox.Top + ((legendBox.Bottom - legendBox.Top) * 0.7);
            double yclose = legendBox.Top + ((legendBox.Bottom - legendBox.Top) * 0.3);
            double[] dashArray = LineStyleHelper.GetDashArray(this.LineStyle);
            var color = this.GetSelectableColor(this.ActualColor);
            rc.DrawLine(
                new[] { new ScreenPoint(xmid, legendBox.Top), new ScreenPoint(xmid, legendBox.Bottom) },
                color,
                this.StrokeThickness,
                dashArray,
                OxyPenLineJoin.Miter,
                true);
            rc.DrawLine(
                new[] { new ScreenPoint(xmid - this.TickLength, yopen), new ScreenPoint(xmid, yopen) },
                color,
                this.StrokeThickness,
                dashArray,
                OxyPenLineJoin.Miter,
                true);
            rc.DrawLine(
                new[] { new ScreenPoint(xmid + this.TickLength, yclose), new ScreenPoint(xmid, yclose) },
                color,
                this.StrokeThickness,
                dashArray,
                OxyPenLineJoin.Miter,
                true);
        }

        /// <summary>
        /// Sets the default values.
        /// </summary>
        /// <param name="model">
        /// The model.
        /// </param>
        protected internal override void SetDefaultValues(PlotModel model)
        {
            if (this.Color == null)
            {
                this.LineStyle = model.GetDefaultLineStyle();
                this.defaultColor = model.GetDefaultColor();
            }
        }

        /// <summary>
        /// Updates the data.
        /// </summary>
        protected internal override void UpdateData()
        {
            if (this.ItemsSource == null)
            {
                return;
            }

            this.items.Clear();

            // Use the mapping to generate the points
            if (this.Mapping != null)
            {
                foreach (var item in this.ItemsSource)
                {
                    this.items.Add(this.Mapping(item));
                }

                return;
            }

            var filler = new ListFiller<HighLowItem>();
            filler.Add(this.DataFieldX, (p, v) => p.X = this.ToDouble(v));
            filler.Add(this.DataFieldHigh, (p, v) => p.High = this.ToDouble(v));
            filler.Add(this.DataFieldLow, (p, v) => p.Low = this.ToDouble(v));
            filler.Add(this.DataFieldOpen, (p, v) => p.Open = this.ToDouble(v));
            filler.Add(this.DataFieldClose, (p, v) => p.Close = this.ToDouble(v));
            filler.FillT(this.items, this.ItemsSource);
        }

        /// <summary>
        /// Updates the max/min values.
        /// </summary>
        protected internal override void UpdateMaxMin()
        {
            base.UpdateMaxMin();
            this.InternalUpdateMaxMin(this.items);
        }

        /// <summary>
        /// Updates the Max/Min limits from the specified point list.
        /// </summary>
        /// <param name="pts">
        /// The PTS.
        /// </param>
        protected void InternalUpdateMaxMin(IList<HighLowItem> pts)
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
                if (!this.IsValidItem(pt, this.XAxis, this.YAxis))
                {
                    continue;
                }

                if (pt.X < minx || double.IsNaN(minx))
                {
                    minx = pt.X;
                }

                if (pt.X > maxx || double.IsNaN(maxx))
                {
                    maxx = pt.X;
                }

                if (pt.Low < miny || double.IsNaN(miny))
                {
                    miny = pt.Low;
                }

                if (pt.High > maxy || double.IsNaN(maxy))
                {
                    maxy = pt.High;
                }
            }

            this.MinX = minx;
            this.MinY = miny;
            this.MaxX = maxx;
            this.MaxY = maxy;
        }

    }
}