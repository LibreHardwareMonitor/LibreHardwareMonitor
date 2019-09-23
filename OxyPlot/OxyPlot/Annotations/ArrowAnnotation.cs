// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArrowAnnotation.cs" company="OxyPlot">
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
//   Represents an arrow annotation.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Annotations
{
    /// <summary>
    /// Represents an arrow annotation.
    /// </summary>
    public class ArrowAnnotation : TextualAnnotation
    {
        /// <summary>
        /// The end point in screen coordinates.
        /// </summary>
        private ScreenPoint screenEndPoint;

        /// <summary>
        /// The start point in screen coordinates.
        /// </summary>
        private ScreenPoint screenStartPoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrowAnnotation"/> class.
        /// </summary>
        public ArrowAnnotation()
        {
            this.HeadLength = 10;
            this.HeadWidth = 3;
            this.Color = OxyColors.Blue;
            this.StrokeThickness = 2;
            this.LineStyle = LineStyle.Solid;
            this.LineJoin = OxyPenLineJoin.Miter;
        }

        /// <summary>
        /// Gets or sets the arrow direction.
        /// </summary>
        /// <remarks>
        /// Setting this property overrides the StartPoint property.
        /// </remarks>
        public ScreenVector ArrowDirection { get; set; }

        /// <summary>
        /// Gets or sets the color of the arrow.
        /// </summary>
        public OxyColor Color { get; set; }

        /// <summary>
        /// Gets or sets the end point.
        /// </summary>
        public DataPoint EndPoint { get; set; }

        /// <summary>
        /// Gets or sets the length of the head (relative to the stroke thickness) (the default value is 10).
        /// </summary>
        /// <value> The length of the head. </value>
        public double HeadLength { get; set; }

        /// <summary>
        /// Gets or sets the width of the head (relative to the stroke thickness) (the default value is 3).
        /// </summary>
        /// <value> The width of the head. </value>
        public double HeadWidth { get; set; }

        /// <summary>
        /// Gets or sets the line join type.
        /// </summary>
        /// <value> The line join type. </value>
        public OxyPenLineJoin LineJoin { get; set; }

        /// <summary>
        /// Gets or sets the line style.
        /// </summary>
        /// <value> The line style. </value>
        public LineStyle LineStyle { get; set; }

        /// <summary>
        /// Gets or sets the start point.
        /// </summary>
        /// <remarks>
        /// This property is overridden by the ArrowDirection property, if set.
        /// </remarks>
        public DataPoint StartPoint { get; set; }

        /// <summary>
        /// Gets or sets the stroke thickness (the default value is 2).
        /// </summary>
        /// <value> The stroke thickness. </value>
        public double StrokeThickness { get; set; }

        /// <summary>
        /// Gets or sets the 'veeness' of the arrow head (relative to thickness) (the default value is 0).
        /// </summary>
        /// <value> The 'veeness'. </value>
        public double Veeness { get; set; }

        /// <summary>
        /// Renders the arrow annotation.
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

            this.screenEndPoint = this.Transform(this.EndPoint);

            if (!this.ArrowDirection.x.IsZero() || !this.ArrowDirection.y.IsZero())
            {
                this.screenStartPoint = this.screenEndPoint - this.ArrowDirection;
            }
            else
            {
                this.screenStartPoint = this.Transform(this.StartPoint);
            }

            var d = this.screenEndPoint - this.screenStartPoint;
            d.Normalize();
            var n = new ScreenVector(d.Y, -d.X);

            var p1 = this.screenEndPoint - (d * this.HeadLength * this.StrokeThickness);
            var p2 = p1 + (n * this.HeadWidth * this.StrokeThickness);
            var p3 = p1 - (n * this.HeadWidth * this.StrokeThickness);
            var p4 = p1 + (d * this.Veeness * this.StrokeThickness);

            OxyRect clippingRect = this.GetClippingRect();
            const double MinimumSegmentLength = 4;

            rc.DrawClippedLine(
                new[] { this.screenStartPoint, p4 },
                clippingRect,
                MinimumSegmentLength * MinimumSegmentLength,
                this.GetSelectableColor(this.Color),
                this.StrokeThickness,
                this.LineStyle,
                this.LineJoin,
                false);

            rc.DrawClippedPolygon(
                new[] { p3, this.screenEndPoint, p2, p4 },
                clippingRect,
                MinimumSegmentLength * MinimumSegmentLength,
                this.GetSelectableColor(this.Color),
                null);

            if (!string.IsNullOrEmpty(this.Text))
            {
                var ha = d.X < 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right;
                var va = d.Y < 0 ? VerticalAlignment.Top : VerticalAlignment.Bottom;

                var textPoint = this.screenStartPoint;
                rc.DrawClippedText(
                    clippingRect,
                    textPoint,
                    this.Text,
                    this.ActualTextColor,
                    this.ActualFont,
                    this.ActualFontSize,
                    this.ActualFontWeight,
                    0,
                    ha,
                    va);
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
            if ((point - this.screenStartPoint).Length < tolerance)
            {
                return new HitTestResult(this.screenStartPoint, null, 1);
            }

            if ((point - this.screenEndPoint).Length < tolerance)
            {
                return new HitTestResult(this.screenEndPoint, null, 2);
            }

            var p = ScreenPointHelper.FindPointOnLine(point, this.screenStartPoint, this.screenEndPoint);
            if ((p - point).Length < tolerance)
            {
                return new HitTestResult(p);
            }

            return null;
        }
    }
}