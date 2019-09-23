// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TrackerHitResult.cs" company="OxyPlot">
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
//   Provides a data container for a tracker hit result.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using OxyPlot.Series;

    /// <summary>
    /// Provides a data container for a tracker hit result.
    /// </summary>
    /// <remarks>
    /// This is used as DataContext for the TrackerControl.
    /// The TrackerControl is visible when the user use the left mouse button to "track" points on the series.
    /// </remarks>
    public class TrackerHitResult
    {
        /// <summary>
        /// The default format string.
        /// </summary>
        private const string DefaultFormatString = "{0}\n{1}: {2}\n{3}: {4}";

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackerHitResult"/> class.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="dp">The data point.</param>
        /// <param name="sp">The screen point.</param>
        /// <param name="item">The item.</param>
        /// <param name="index">The index.</param>
        /// <param name="text">The text.</param>
        public TrackerHitResult(OxyPlot.Series.Series series, IDataPoint dp, ScreenPoint sp, object item = null, double index = -1, string text = null)
        {
            this.DataPoint = dp;
            this.Position = sp;
            this.Item = item;
            this.Index = index;
            this.Series = series;
            this.Text = text;
            var ds = series as DataPointSeries;
            if (ds != null)
            {
                this.XAxis = ds.XAxis;
                this.YAxis = ds.YAxis;
            }
        }

        /// <summary>
        /// Gets or sets the nearest or interpolated data point.
        /// </summary>
        public IDataPoint DataPoint { get; set; }

        /// <summary>
        /// Gets or sets the source item of the point.
        /// If the current point is from an ItemsSource and is not interpolated, this property will contain the item.
        /// </summary>
        public object Item { get; set; }

        /// <summary>
        /// Gets or sets the index for the Item.
        /// </summary>
        public double Index { get; set; }

        /// <summary>
        /// Gets or sets the horizontal/vertical line extents.
        /// </summary>
        public OxyRect LineExtents { get; set; }

        /// <summary>
        /// Gets or sets the plot model.
        /// </summary>
        public PlotModel PlotModel { get; set; }

        /// <summary>
        /// Gets or sets the position in screen coordinates.
        /// </summary>
        public ScreenPoint Position { get; set; }

        /// <summary>
        /// Gets or sets the series that is being tracked.
        /// </summary>
        public Series.Series Series { get; set; }

        /// <summary>
        /// Gets or sets the text shown in the tracker.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the X axis.
        /// </summary>
        public Axes.Axis XAxis { get; set; }

        /// <summary>
        /// Gets or sets the Y axis.
        /// </summary>
        public Axes.Axis YAxis { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (this.Text != null)
            {
                return this.Text;
            }

            var ts = this.Series as ITrackableSeries;
            string formatString = DefaultFormatString;
            if (ts != null && !string.IsNullOrEmpty(ts.TrackerFormatString))
            {
                formatString = ts.TrackerFormatString;
            }

            string xaxisTitle = (this.XAxis != null ? this.XAxis.Title : null) ?? "X";
            string yaxisTitle = (this.YAxis != null ? this.YAxis.Title : null) ?? "Y";
            object xvalue = this.XAxis != null ? this.XAxis.GetValue(this.DataPoint.X) : this.DataPoint.X;
            object yvalue = this.YAxis != null ? this.YAxis.GetValue(this.DataPoint.Y) : this.DataPoint.Y;

            return StringHelper.Format(
                this.Series.ActualCulture,
                formatString,
                this.Item,
                this.Series.Title,
                xaxisTitle,
                xvalue,
                yaxisTitle,
                yvalue,
                this.Item).Trim();
        }

    }
}