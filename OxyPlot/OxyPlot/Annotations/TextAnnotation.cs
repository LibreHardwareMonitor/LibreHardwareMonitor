// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TextAnnotation.cs" company="OxyPlot">
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
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a text annotation.
    /// </summary>
    public class TextAnnotation : TextualAnnotation
    {
        /// <summary>
        /// The actual bounds of the text.
        /// </summary>
        private IList<ScreenPoint> actualBounds;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextAnnotation" /> class.
        /// </summary>
        public TextAnnotation()
        {
            this.TextColor = OxyColors.Blue;
            this.Stroke = OxyColors.Black;
            this.Background = null;
            this.StrokeThickness = 1;
            this.Rotation = 0;
            this.HorizontalAlignment = OxyPlot.HorizontalAlignment.Center;
            this.VerticalAlignment = OxyPlot.VerticalAlignment.Bottom;
            this.Padding = new OxyThickness(4);
        }

        /// <summary>
        /// Gets or sets the fill color of the background rectangle.
        /// </summary>
        /// <value> The background. </value>
        public OxyColor Background { get; set; }

        /// <summary>
        /// Gets or sets the horizontal alignment.
        /// </summary>
        /// <value> The horizontal alignment. </value>
        public HorizontalAlignment HorizontalAlignment { get; set; }

        /// <summary>
        /// Gets or sets the position offset (screen coordinates).
        /// </summary>
        /// <value> The offset. </value>
        public ScreenVector Offset { get; set; }

        /// <summary>
        /// Gets or sets the padding of the background rectangle.
        /// </summary>
        /// <value> The padding. </value>
        public OxyThickness Padding { get; set; }

        /// <summary>
        /// Gets or sets the position of the text.
        /// </summary>
        public DataPoint Position { get; set; }

        /// <summary>
        /// Gets or sets the rotation angle (degrees).
        /// </summary>
        /// <value> The rotation. </value>
        public double Rotation { get; set; }

        /// <summary>
        /// Gets or sets the stroke color of the background rectangle.
        /// </summary>
        /// <value> The stroke color. </value>
        public OxyColor Stroke { get; set; }

        /// <summary>
        /// Gets or sets the stroke thickness of the background rectangle.
        /// </summary>
        /// <value> The stroke thickness. </value>
        public double StrokeThickness { get; set; }

        /// <summary>
        /// Gets or sets the vertical alignment.
        /// </summary>
        /// <value> The vertical alignment. </value>
        public VerticalAlignment VerticalAlignment { get; set; }

        /// <summary>
        /// Renders the text annotation.
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

            var position = this.Transform(this.Position);
            position.X += this.Offset.X;
            position.Y += this.Offset.Y;

            var clippingRect = this.GetClippingRect();

            var textSize = rc.MeasureText(this.Text, this.ActualFont, this.ActualFontSize, this.ActualFontWeight);

            const double MinDistSquared = 4;

            this.actualBounds = GetTextBounds(
                position, textSize, this.Padding, this.Rotation, this.HorizontalAlignment, this.VerticalAlignment);
            rc.DrawClippedPolygon(
                this.actualBounds, clippingRect, MinDistSquared, this.Background, this.Stroke, this.StrokeThickness);

            rc.DrawClippedText(
                clippingRect,
                position,
                this.Text,
                this.GetSelectableFillColor(this.ActualTextColor),
                this.ActualFont,
                this.ActualFontSize,
                this.ActualFontWeight,
                this.Rotation,
                this.HorizontalAlignment,
                this.VerticalAlignment);
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
            if (this.actualBounds == null)
            {
                return null;
            }

            // Todo: see if performance can be improved by checking rectangle (with rotation and alignment), not polygon
            return ScreenPointHelper.IsPointInPolygon(point, this.actualBounds) ? new HitTestResult(point) : null;
        }

        /// <summary>
        /// Gets the coordinates of the (rotated) background rectangle.
        /// </summary>
        /// <param name="position">
        /// The position.
        /// </param>
        /// <param name="size">
        /// The size.
        /// </param>
        /// <param name="padding">
        /// The padding.
        /// </param>
        /// <param name="rotation">
        /// The rotation.
        /// </param>
        /// <param name="horizontalAlignment">
        /// The horizontal alignment.
        /// </param>
        /// <param name="verticalAlignment">
        /// The vertical alignment.
        /// </param>
        /// <returns>
        /// The background rectangle coordinates.
        /// </returns>
        private static IList<ScreenPoint> GetTextBounds(
            ScreenPoint position,
            OxySize size,
            OxyThickness padding,
            double rotation,
            HorizontalAlignment horizontalAlignment,
            VerticalAlignment verticalAlignment)
        {
            double left, right, top, bottom;
            switch (horizontalAlignment)
            {
                case HorizontalAlignment.Center:
                    left = -size.Width * 0.5;
                    right = -left;
                    break;
                case HorizontalAlignment.Right:
                    left = -size.Width;
                    right = 0;
                    break;
                default:
                    left = 0;
                    right = size.Width;
                    break;
            }

            switch (verticalAlignment)
            {
                case VerticalAlignment.Middle:
                    top = -size.Height * 0.5;
                    bottom = -top;
                    break;
                case VerticalAlignment.Bottom:
                    top = -size.Height;
                    bottom = 0;
                    break;
                default:
                    top = 0;
                    bottom = size.Height;
                    break;
            }

            double cost = Math.Cos(rotation / 180 * Math.PI);
            double sint = Math.Sin(rotation / 180 * Math.PI);
            var u = new ScreenVector(cost, sint);
            var v = new ScreenVector(-sint, cost);
            var polygon = new ScreenPoint[4];
            polygon[0] = position + (u * (left - padding.Left)) + (v * (top - padding.Top));
            polygon[1] = position + (u * (right + padding.Right)) + (v * (top - padding.Top));
            polygon[2] = position + (u * (right + padding.Right)) + (v * (bottom + padding.Bottom));
            polygon[3] = position + (u * (left - padding.Left)) + (v * (bottom + padding.Bottom));
            return polygon;
        }
    }
}