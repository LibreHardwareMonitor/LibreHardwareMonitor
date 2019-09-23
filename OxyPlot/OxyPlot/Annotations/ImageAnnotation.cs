// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ImageAnnotation.cs" company="OxyPlot">
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
//   Represents a text object annotation.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Annotations
{
    /// <summary>
    /// Represents a text annotation.
    /// </summary>
    public class ImageAnnotation : Annotation
    {
        /// <summary>
        /// The actual bounds of the rendered image.
        /// </summary>
        private OxyRect actualBounds;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageAnnotation" /> class.
        /// </summary>
        public ImageAnnotation()
        {
            this.X = new PlotLength(0.5, PlotLengthUnit.RelativeToPlotArea);
            this.Y = new PlotLength(0.5, PlotLengthUnit.RelativeToPlotArea);
            this.OffsetX = new PlotLength(0, PlotLengthUnit.ScreenUnits);
            this.OffsetY = new PlotLength(0, PlotLengthUnit.ScreenUnits);
            this.Width = new PlotLength(double.NaN, PlotLengthUnit.ScreenUnits);
            this.Height = new PlotLength(double.NaN, PlotLengthUnit.ScreenUnits);
            this.Opacity = 1.0;
            this.Interpolate = true;
            this.HorizontalAlignment = OxyPlot.HorizontalAlignment.Center;
            this.VerticalAlignment = OxyPlot.VerticalAlignment.Middle;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageAnnotation"/> class.
        /// </summary>
        /// <param name="image">
        /// The image.
        /// </param>
        /// <param name="position">
        /// The position in screen coordinates.
        /// </param>
        /// <param name="horizontalAlignment">
        /// The horizontal alignment.
        /// </param>
        /// <param name="verticalAlignment">
        /// The vertical alignment.
        /// </param>
        public ImageAnnotation(
            OxyImage image, 
            ScreenPoint position, 
            HorizontalAlignment horizontalAlignment = OxyPlot.HorizontalAlignment.Center, 
            VerticalAlignment verticalAlignment = OxyPlot.VerticalAlignment.Middle)
            : this()
        {
            this.ImageSource = image;
            this.X = new PlotLength(position.X, PlotLengthUnit.ScreenUnits);
            this.Y = new PlotLength(position.Y, PlotLengthUnit.ScreenUnits);
            this.HorizontalAlignment = horizontalAlignment;
            this.VerticalAlignment = verticalAlignment;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageAnnotation"/> class.
        /// </summary>
        /// <param name="image">
        /// The image.
        /// </param>
        /// <param name="position">
        /// The position in data coordinates.
        /// </param>
        /// <param name="horizontalAlignment">
        /// The horizontal alignment.
        /// </param>
        /// <param name="verticalAlignment">
        /// The vertical alignment.
        /// </param>
        public ImageAnnotation(
            OxyImage image, 
            IDataPoint position, 
            HorizontalAlignment horizontalAlignment = OxyPlot.HorizontalAlignment.Center, 
            VerticalAlignment verticalAlignment = OxyPlot.VerticalAlignment.Middle)
            : this()
        {
            this.ImageSource = image;
            this.X = new PlotLength(position.X, PlotLengthUnit.Data);
            this.Y = new PlotLength(position.Y, PlotLengthUnit.Data);
            this.HorizontalAlignment = horizontalAlignment;
            this.VerticalAlignment = verticalAlignment;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageAnnotation"/> class.
        /// </summary>
        /// <param name="image">
        /// The image.
        /// </param>
        /// <param name="relativeX">
        /// The x-coordinate relative to the plot area (0-1).
        /// </param>
        /// <param name="relativeY">
        /// The y-coordinate relative to the plot area (0-1).
        /// </param>
        /// <param name="horizontalAlignment">
        /// The horizontal alignment.
        /// </param>
        /// <param name="verticalAlignment">
        /// The vertical alignment.
        /// </param>
        public ImageAnnotation(
            OxyImage image, 
            double relativeX, 
            double relativeY, 
            HorizontalAlignment horizontalAlignment = OxyPlot.HorizontalAlignment.Center, 
            VerticalAlignment verticalAlignment = OxyPlot.VerticalAlignment.Middle)
            : this()
        {
            this.ImageSource = image;
            this.X = new PlotLength(relativeX, PlotLengthUnit.RelativeToPlotArea);
            this.Y = new PlotLength(relativeY, PlotLengthUnit.RelativeToPlotArea);
            this.HorizontalAlignment = horizontalAlignment;
            this.VerticalAlignment = verticalAlignment;
        }

        /// <summary>
        /// Gets or sets the image source.
        /// </summary>
        /// <value>
        /// The image source.
        /// </value>
        public OxyImage ImageSource { get; set; }

        /// <summary>
        /// Gets or sets the horizontal alignment.
        /// </summary>
        /// <value> The horizontal alignment. </value>
        public HorizontalAlignment HorizontalAlignment { get; set; }

        /// <summary>
        /// Gets or sets the X position of the image.
        /// </summary>
        /// <value>
        /// The X.
        /// </value>
        public PlotLength X { get; set; }

        /// <summary>
        /// Gets or sets the Y position of the image.
        /// </summary>
        /// <value>
        /// The Y.
        /// </value>
        public PlotLength Y { get; set; }

        /// <summary>
        /// Gets or sets the X offset.
        /// </summary>
        /// <value>
        /// The offset X.
        /// </value>
        public PlotLength OffsetX { get; set; }

        /// <summary>
        /// Gets or sets the Y offset.
        /// </summary>
        /// <value>
        /// The offset Y.
        /// </value>
        public PlotLength OffsetY { get; set; }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        /// <value>
        /// The width.
        /// </value>
        public PlotLength Width { get; set; }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        /// <value>
        /// The height.
        /// </value>
        public PlotLength Height { get; set; }

        /// <summary>
        /// Gets or sets the opacity (0-1).
        /// </summary>
        /// <value>
        /// The opacity value.
        /// </value>
        public double Opacity { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to apply smooth interpolation to the image.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the image should be interpolated (using a high-quality bi-cubic interpolation); <c>false</c> if the nearest neighbor should be used.
        /// </value>
        public bool Interpolate { get; set; }

        /// <summary>
        /// Gets or sets the vertical alignment.
        /// </summary>
        /// <value> The vertical alignment. </value>
        public VerticalAlignment VerticalAlignment { get; set; }

        /// <summary>
        /// Renders the image annotation.
        /// </summary>
        /// <param name="rc">
        /// The render context.
        /// </param>
        /// <param name="model">
        /// The plot model.
        /// </param>
        public override void Render(IRenderContext rc, PlotModel model)
        {
            base.Render(rc, model);

            var p = this.GetPoint(this.X, this.Y, rc, model);
            var o = this.GetVector(this.OffsetX, this.OffsetY, rc, model);
            var position = p + o;

            var clippingRect = this.GetClippingRect();

            var imageInfo = rc.GetImageInfo(this.ImageSource);
            if (imageInfo == null)
            {
                return;
            }

            var s = this.GetVector(this.Width, this.Height, rc, model);

            var width = s.X;
            var height = s.Y;

            if (double.IsNaN(width) && double.IsNaN(height))
            {
                width = imageInfo.Width;
                height = imageInfo.Height;
            }

            if (double.IsNaN(width))
            {
                width = height / imageInfo.Height * imageInfo.Width;
            }

            if (double.IsNaN(height))
            {
                height = width / imageInfo.Width * imageInfo.Height;
            }

            double x = position.X;
            double y = position.Y;

            if (this.HorizontalAlignment == HorizontalAlignment.Center)
            {
                x -= width * 0.5;
            }

            if (this.HorizontalAlignment == HorizontalAlignment.Right)
            {
                x -= width;
            }

            if (this.VerticalAlignment == VerticalAlignment.Middle)
            {
                y -= height * 0.5;
            }

            if (this.VerticalAlignment == VerticalAlignment.Bottom)
            {
                y -= height;
            }

            this.actualBounds = new OxyRect(x, y, width, height);

            if (this.X.Unit == PlotLengthUnit.Data || this.Y.Unit == PlotLengthUnit.Data)
            {
                rc.DrawClippedImage(clippingRect, this.ImageSource, x, y, width, height, this.Opacity, this.Interpolate);
            }
            else
            {
                rc.DrawImage(this.ImageSource, x, y, width, height, this.Opacity, this.Interpolate);
            }
        }

        /// <summary>
        /// Tests if the plot element is hit by the specified point.
        /// </summary>
        /// <param name="point">
        /// The point.
        /// </param>
        /// <param name="tolerance">
        /// The tolerance.
        /// </param>
        /// <returns>
        /// A hit test result.
        /// </returns>
        protected internal override HitTestResult HitTest(ScreenPoint point, double tolerance)
        {
            if (this.actualBounds.Contains(point))
            {
                return new HitTestResult(point);
            }

            return null;
        }

        /// <summary>
        /// Gets the point.
        /// </summary>
        /// <param name="x">
        /// The x.
        /// </param>
        /// <param name="y">
        /// The y.
        /// </param>
        /// <param name="rc">
        /// The render context.
        /// </param>
        /// <param name="model">
        /// The model.
        /// </param>
        /// <returns>
        /// The point in screen coordinates.
        /// </returns>
        protected ScreenPoint GetPoint(PlotLength x, PlotLength y, IRenderContext rc, PlotModel model)
        {
            if (x.Unit == PlotLengthUnit.Data || y.Unit == PlotLengthUnit.Data)
            {
                return this.XAxis.Transform(x.Value, y.Value, this.YAxis);
            }

            double sx;
            double sy;
            switch (x.Unit)
            {
                case PlotLengthUnit.RelativeToPlotArea:
                    sx = model.PlotArea.Left + (model.PlotArea.Width * x.Value);
                    break;
                case PlotLengthUnit.RelativeToViewport:
                    sx = model.Width * x.Value;
                    break;
                default:
                    sx = x.Value;
                    break;
            }

            switch (y.Unit)
            {
                case PlotLengthUnit.RelativeToPlotArea:
                    sy = model.PlotArea.Top + (model.PlotArea.Height * y.Value);
                    break;
                case PlotLengthUnit.RelativeToViewport:
                    sy = model.Height * y.Value;
                    break;
                default:
                    sy = y.Value;
                    break;
            }

            return new ScreenPoint(sx, sy);
        }

        /// <summary>
        /// Gets the vector.
        /// </summary>
        /// <param name="x">
        /// The x component.
        /// </param>
        /// <param name="y">
        /// The y component.
        /// </param>
        /// <param name="rc">
        /// The render context.
        /// </param>
        /// <param name="model">
        /// The model.
        /// </param>
        /// <returns>
        /// The vector in screen coordinates.
        /// </returns>
        protected ScreenVector GetVector(PlotLength x, PlotLength y, IRenderContext rc, PlotModel model)
        {
            double sx;
            double sy;

            switch (x.Unit)
            {
                case PlotLengthUnit.Data:
                    sx = this.XAxis.Transform(x.Value) - this.XAxis.Transform(0);
                    break;
                case PlotLengthUnit.RelativeToPlotArea:
                    sx = model.PlotArea.Width * x.Value;
                    break;
                case PlotLengthUnit.RelativeToViewport:
                    sx = model.Width * x.Value;
                    break;
                default:
                    sx = x.Value;
                    break;
            }

            switch (y.Unit)
            {
                case PlotLengthUnit.Data:
                    sy = this.YAxis.Transform(y.Value) - this.YAxis.Transform(0);
                    break;
                case PlotLengthUnit.RelativeToPlotArea:
                    sy = model.PlotArea.Height * y.Value;
                    break;
                case PlotLengthUnit.RelativeToViewport:
                    sy = model.Height * y.Value;
                    break;
                default:
                    sy = y.Value;
                    break;
            }

            return new ScreenVector(sx, sy);
        }
    }
}