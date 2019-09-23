// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HeatMapSeries.cs" company="OxyPlot">
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
//   The heat map series.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Series
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using OxyPlot.Axes;

    /// <summary>
    /// The heat map series.
    /// </summary>
    /// <remarks>
    /// Does not work with Silverlight. Silverlight does not support bitmaps, only PNG and JPG.
    /// </remarks>
    public class HeatMapSeries : XYAxisSeries
    {
        /// <summary>
        /// The hash code of the current data.
        /// </summary>
        private int dataHash;

        /// <summary>
        /// The image
        /// </summary>
        private OxyImage image;

        /// <summary>
        /// Gets or sets the x 0.
        /// </summary>
        public double X0 { get; set; }

        /// <summary>
        /// Gets or sets the x 1.
        /// </summary>
        public double X1 { get; set; }

        /// <summary>
        /// Gets or sets the y 0.
        /// </summary>
        public double Y0 { get; set; }

        /// <summary>
        /// Gets or sets the y 1.
        /// </summary>
        public double Y1 { get; set; }

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        public double[,] Data { get; set; }

        /// <summary>
        /// Gets or sets the minimum value of the dataset.
        /// </summary>
        public double MinValue { get; protected set; }

        /// <summary>
        /// Gets or sets the maximum value of the dataset.
        /// </summary>
        public double MaxValue { get; protected set; }

        /// <summary>
        /// Gets or sets the color axis.
        /// </summary>
        /// <value>
        /// The color axis.
        /// </value>
        public ColorAxis ColorAxis { get; protected set; }

        /// <summary>
        /// Gets or sets the color axis key.
        /// </summary>
        /// <value> The color axis key. </value>
        public string ColorAxisKey { get; set; }

        /// <summary>
        /// Renders the series on the specified render context.
        /// </summary>
        /// <param name="rc">
        /// The rendering context.
        /// </param>
        /// <param name="model">
        /// The model.
        /// </param>
        public override void Render(IRenderContext rc, PlotModel model)
        {
            if (this.Data == null)
            {
                this.image = null;
                return;
            }

            int m = this.Data.GetLength(0);
            int n = this.Data.GetLength(1);
            double dx = (this.X1 - this.X0) / m;
            double left = this.X0 - (dx * 0.5);
            double right = this.X1 + (dx * 0.5);
            double dy = (this.Y1 - this.Y0) / n;
            double bottom = this.Y0 - (dy * 0.5);
            double top = this.Y1 + (dy * 0.5);
            var s00 = this.Transform(left, bottom);
            var s11 = this.Transform(right, top);
            var rect = OxyRect.Create(s00, s11);

            if (this.image == null || this.Data.GetHashCode() != this.dataHash)
            {
                this.UpdateImage();
                this.dataHash = this.Data.GetHashCode();
            }

            if (this.image != null)
            {
                var clip = this.GetClippingRect();
                rc.DrawClippedImage(clip, this.image, rect.Left, rect.Top, rect.Width, rect.Height, 1, true);
            }
        }

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
        public override TrackerHitResult GetNearestPoint(ScreenPoint point, bool interpolate)
        {
            return null;
        }

        /// <summary>
        /// Ensures that the axes of the series is defined.
        /// </summary>
        protected internal override void EnsureAxes()
        {
            base.EnsureAxes();

            this.ColorAxis =
                this.PlotModel.GetAxisOrDefault(this.ColorAxisKey, this.PlotModel.DefaultColorAxis) as ColorAxis;
        }

        /// <summary>
        /// Updates the max/minimum values.
        /// </summary>
        protected internal override void UpdateMaxMin()
        {
            base.UpdateMaxMin();
            
            this.MinX = Math.Min(this.X0, this.X1);
            this.MaxX = Math.Max(this.X0, this.X1);

            this.MinY = Math.Min(this.Y0, this.Y1);
            this.MaxY = Math.Max(this.Y0, this.Y1);

            this.MinValue = this.GetData().Min();
            this.MaxValue = this.GetData().Max();

            this.XAxis.Include(this.MinX);
            this.XAxis.Include(this.MaxX);

            this.YAxis.Include(this.MinY);
            this.YAxis.Include(this.MaxY);
            
            this.ColorAxis.Include(this.MinValue);
            this.ColorAxis.Include(this.MaxValue);
        }

        /// <summary>
        /// Gets the data as a sequence (LINQ-friendly).
        /// </summary>
        /// <returns>The sequence of data.</returns>
        protected IEnumerable<double> GetData()
        {
            int m = this.Data.GetLength(0);
            int n = this.Data.GetLength(1);
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    yield return this.Data[i, j];
                }
            }
        }

        /// <summary>
        /// Updates the image.
        /// </summary>
        private void UpdateImage()
        {
            int m = this.Data.GetLength(0);
            int n = this.Data.GetLength(1);
            var buffer = new OxyColor[n, m];
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    buffer[j, i] = this.ColorAxis.GetColor(this.Data[i, j]);
                }
            }
            
            this.image = OxyImage.PngFromArgb(buffer);
        }
    }
}