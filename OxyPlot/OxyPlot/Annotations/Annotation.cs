// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Annotation.cs" company="OxyPlot">
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
//   Annotation base class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Annotations
{
    using System;
    using System.Globalization;

    using OxyPlot.Axes;

    /// <summary>
    /// Provides an abstract base class for annotations.
    /// </summary>
    public abstract class Annotation : UIPlotElement
    {
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
        /// Gets or sets the layer.
        /// </summary>
        public AnnotationLayer Layer { get; set; }

        /// <summary>
        /// Gets or sets the X axis.
        /// </summary>
        /// <value>The X axis.</value>
        public Axis XAxis { get; set; }

        /// <summary>
        /// Gets or sets the X axis key.
        /// </summary>
        /// <value>The X axis key.</value>
        public string XAxisKey { get; set; }

        /// <summary>
        /// Gets or sets the Y axis.
        /// </summary>
        /// <value>The Y axis.</value>
        public Axis YAxis { get; set; }

        /// <summary>
        /// Gets or sets the Y axis key.
        /// </summary>
        /// <value>The Y axis key.</value>
        public string YAxisKey { get; set; }

        /// <summary>
        /// Ensures that the annotation axes are set.
        /// </summary>
        public void EnsureAxes()
        {
            this.XAxis = this.PlotModel.GetAxisOrDefault(this.XAxisKey, this.PlotModel.DefaultXAxis);
            this.YAxis = this.PlotModel.GetAxisOrDefault(this.YAxisKey, this.PlotModel.DefaultYAxis);
        }

        /// <summary>
        /// Renders the annotation on the specified context.
        /// </summary>
        /// <param name="rc">
        /// The render context.
        /// </param>
        /// <param name="model">
        /// The model.
        /// </param>
        public virtual void Render(IRenderContext rc, PlotModel model)
        {
        }

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
            return null;
        }

        /// <summary>
        /// Transforms the specified coordinates to a screen point.
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
        /// Transforms the specified data point to a screen point.
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
        /// Transforms the specified screen position to a data point.
        /// </summary>
        /// <param name="position">
        /// The position.
        /// </param>
        /// <returns>
        /// A data point
        /// </returns>
        public DataPoint InverseTransform(ScreenPoint position)
        {
            return Axis.InverseTransform(position, this.XAxis, this.YAxis);
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
    }
}