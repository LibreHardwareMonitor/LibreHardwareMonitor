// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SvgRenderContext.cs" company="OxyPlot">
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
//   The svg render context.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Provides a render context for scalable vector graphics output.
    /// </summary>
    public class SvgRenderContext : RenderContextBase, IDisposable
    {
        /// <summary>
        /// The writer.
        /// </summary>
        private readonly SvgWriter w;

        /// <summary>
        /// The disposed flag.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SvgRenderContext" /> class.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="isDocument">Create an SVG document if set to <c>true</c>.</param>
        /// <param name="textMeasurer">The text measurer.</param>
        /// <param name="background">The background.</param>
        public SvgRenderContext(Stream s, double width, double height, bool isDocument, IRenderContext textMeasurer, OxyColor background)
        {
            if (textMeasurer == null)
            {
                throw new ArgumentNullException("textMeasurer", "A text measuring render context must be provided.");
            }

            this.w = new SvgWriter(s, width, height, isDocument);
            this.TextMeasurer = textMeasurer;
            if (background != null)
            {
                this.w.WriteRectangle(0, 0, width, height, this.w.CreateStyle(background, null, 0));
            }
        }

        /// <summary>
        /// Gets or sets the text measurer.
        /// </summary>
        /// <value>
        /// The text measurer.
        /// </value>
        public IRenderContext TextMeasurer { get; set; }

        /// <summary>
        /// Closes the svg writer.
        /// </summary>
        public void Close()
        {
            this.w.Close();
        }

        /// <summary>
        /// Completes the svg element.
        /// </summary>
        public void Complete()
        {
            this.w.Complete();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Draws an ellipse.
        /// </summary>
        /// <param name="rect">The rectangle.</param>
        /// <param name="fill">The fill color.</param>
        /// <param name="stroke">The stroke color.</param>
        /// <param name="thickness">The thickness.</param>
        public override void DrawEllipse(OxyRect rect, OxyColor fill, OxyColor stroke, double thickness)
        {
            this.w.WriteEllipse(
                rect.Left, rect.Top, rect.Width, rect.Height, this.w.CreateStyle(fill, stroke, thickness));
        }

        /// <summary>
        /// Draws the polyline from the specified points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="stroke">The stroke color.</param>
        /// <param name="thickness">The stroke thickness.</param>
        /// <param name="dashArray">The dash array.</param>
        /// <param name="lineJoin">The line join type.</param>
        /// <param name="aliased">if set to <c>true</c> the shape will be aliased.</param>
        public override void DrawLine(
            IList<ScreenPoint> points,
            OxyColor stroke,
            double thickness,
            double[] dashArray,
            OxyPenLineJoin lineJoin,
            bool aliased)
        {
            this.w.WritePolyline(points, this.w.CreateStyle(null, stroke, thickness, dashArray, lineJoin));
        }

        /// <summary>
        /// Draws the polygon from the specified points. The polygon can have stroke and/or fill.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="fill">The fill color.</param>
        /// <param name="stroke">The stroke color.</param>
        /// <param name="thickness">The stroke thickness.</param>
        /// <param name="dashArray">The dash array.</param>
        /// <param name="lineJoin">The line join type.</param>
        /// <param name="aliased">if set to <c>true</c> the shape will be aliased.</param>
        public override void DrawPolygon(
            IList<ScreenPoint> points,
            OxyColor fill,
            OxyColor stroke,
            double thickness,
            double[] dashArray,
            OxyPenLineJoin lineJoin,
            bool aliased)
        {
            this.w.WritePolygon(points, this.w.CreateStyle(fill, stroke, thickness, dashArray, lineJoin));
        }

        /// <summary>
        /// Draws the rectangle.
        /// </summary>
        /// <param name="rect">The rectangle.</param>
        /// <param name="fill">The fill color.</param>
        /// <param name="stroke">The stroke color.</param>
        /// <param name="thickness">The stroke thickness.</param>
        public override void DrawRectangle(OxyRect rect, OxyColor fill, OxyColor stroke, double thickness)
        {
            this.w.WriteRectangle(
                rect.Left, rect.Top, rect.Width, rect.Height, this.w.CreateStyle(fill, stroke, thickness));
        }

        /// <summary>
        /// Draws the text.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <param name="text">The text.</param>
        /// <param name="c">The c.</param>
        /// <param name="fontFamily">The font family.</param>
        /// <param name="fontSize">Size of the font.</param>
        /// <param name="fontWeight">The font weight.</param>
        /// <param name="rotate">The rotate.</param>
        /// <param name="halign">The horizontal alignment.</param>
        /// <param name="valign">The vertical alignment.</param>
        /// <param name="maxSize">Size of the max.</param>
        public override void DrawText(
            ScreenPoint p,
            string text,
            OxyColor c,
            string fontFamily,
            double fontSize,
            double fontWeight,
            double rotate,
            HorizontalAlignment halign,
            VerticalAlignment valign,
            OxySize? maxSize)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var lines = Regex.Split(text, "\r\n");
            if (valign == VerticalAlignment.Bottom)
            {
                for (var i = lines.Length - 1; i >= 0; i--)
                {
                    var line = lines[i];
                    var size = this.MeasureText(line, fontFamily, fontSize, fontWeight);
                    this.w.WriteText(p, line, c, fontFamily, fontSize, fontWeight, rotate, halign, valign);

                    p.X += Math.Sin(rotate / 180.0 * Math.PI) * size.Height;
                    p.Y -= Math.Cos(rotate / 180.0 * Math.PI) * size.Height;
                }
            }
            else
            {
                foreach (var line in lines)
                {
                    var size = this.MeasureText(line, fontFamily, fontSize, fontWeight);
                    this.w.WriteText(p, line, c, fontFamily, fontSize, fontWeight, rotate, halign, valign);

                    p.X -= Math.Sin(rotate / 180.0 * Math.PI) * size.Height;
                    p.Y += Math.Cos(rotate / 180.0 * Math.PI) * size.Height;
                }
            }
        }

        /// <summary>
        /// Flushes this instance.
        /// </summary>
        public void Flush()
        {
            this.w.Flush();
        }

        /// <summary>
        /// Measures the text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="fontFamily">The font family.</param>
        /// <param name="fontSize">Size of the font.</param>
        /// <param name="fontWeight">The font weight.</param>
        /// <returns>
        /// The text size.
        /// </returns>
        public override OxySize MeasureText(string text, string fontFamily, double fontSize, double fontWeight)
        {
            if (string.IsNullOrEmpty(text))
            {
                return OxySize.Empty;
            }

            return this.TextMeasurer.MeasureText(text, fontFamily, fontSize, fontWeight);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.w.Dispose();
                }
            }

            this.disposed = true;
        }
    }
}